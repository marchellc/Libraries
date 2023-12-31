using Common.Logging;
using Common.Extensions;
using Common.IO.Collections;

using Networking.Address;
using Networking.Data;
using Networking.Pooling;
using Networking.Utilities;
using Networking.Features;

using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Networking.Client
{
    public class NetworkClient
    {
        private Telepathy.Client client;

        private TypeLibrary typeLib;
        private NetworkFunctions funcs;

        private Timer timer;

        private ConcurrentQueue<byte[]> inDataQueue;
        private ConcurrentQueue<byte[]> outDataQueue;

        private LockedDictionary<Type, NetworkFeature> features;

        private object processLock = new object();

        public int maxOutput = 10;
        public int maxInput = 10;
        public int maxReconnections = 15;

        public int reconnectionReset = 1500;
        public int reconnectionResetMultiplier = 2;

        public double latency = 0;

        public double maxLatency = 0;
        public double minLatency = 0;
        public double avgLatency = 0;

        public int reconnectionTimeout = 1500;
        public int reconnectionFailed = 0;
        public int reconnectionResets = 0;

        public bool isRunning;
        public bool isDisconnecting;
        public bool isNoDelay = true;

        public bool wasEverConnected;

        public NetworkConnectionStatus status = NetworkConnectionStatus.Disconnected;
        public NetworkHandshakeResult handshake = NetworkHandshakeResult.TimedOut;

        public IPInfo target;

        public LogOutput log;

        public readonly WriterPool writers;
        public readonly ReaderPool readers;

        public event Action OnDisconnected;
        public event Action OnConnected;
        public event Action OnAuthorized;
        public event Action OnPinged;

        public event Action<ArraySegment<byte>> OnData;
        public event Action<Type, object> OnMessage;

        public static readonly Version version;
        public static readonly NetworkClient instance;
        
        static NetworkClient()
        {
            version = new Version(1, 0, 0, 0);
            instance = new NetworkClient();
        }

        public NetworkClient()
        {
            this.log = new LogOutput("Network Client");
            this.log.Setup();

            this.typeLib = new TypeLibrary();

            this.writers = new WriterPool(typeLib);
            this.readers = new ReaderPool(typeLib);

            this.writers.Initialize(20);
            this.readers.Initialize(20);

            this.inDataQueue = new ConcurrentQueue<byte[]>();
            this.outDataQueue = new ConcurrentQueue<byte[]>();

            this.target = new IPInfo(IPType.Remote, "127.0.0.1", 8000, IPAddress.Loopback);

            this.funcs = new NetworkFunctions(
                () => { return writers.Next(); },

                netData => { return readers.Next(netData); },
                netWriter => { Send(netWriter); },

                true);
        }

        public T Get<T>() where T : NetworkFeature, new()
        {
            if (features.TryGetValue(typeof(T), out var feature))
                return (T)feature;

            Add<T>();

            return (T)features[typeof(T)];
        }

        public void Add<T>() where T : NetworkFeature, new()
        {
            if (features.ContainsKey(typeof(T)))
                return;

            var feature = new T();

            feature.net = funcs;

            if (status is NetworkConnectionStatus.Connected)
                feature.InternalStart();

            features[typeof(T)] = feature;
        }

        public void Remove<T>() where T : NetworkFeature, new()
        {
            if (!features.TryGetValue(typeof(T), out var feature))
                return;

            feature.InternalStop();

            features.Remove(typeof(T));
        }

        public void Send(Action<Writer> writer)
            => this.funcs.Send(writer);

        public void Send(Writer writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            outDataQueue.Enqueue(writer.Buffer);

            if (writer.pool != null)
                writer.Return();
        }

        public void Send(byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(data));

            outDataQueue.Enqueue(data);

            log.Trace($"Enqueued {data.Length} bytes in send queue");
        }

        public void Connect(IPEndPoint endPoint)
        {
            if (isRunning)
                Disconnect();

            if (endPoint != null && this.target.endPoint != null && this.target.endPoint != endPoint)
                this.target = new IPInfo(IPType.Remote, endPoint.Address.ToString(), endPoint.Port, endPoint.Address);
            else if (this.target.endPoint is null && endPoint != null)
                this.target = new IPInfo(IPType.Remote, endPoint.Address.ToString(), endPoint.Port, endPoint.Address);

            this.typeLib.Reset();

            this.outDataQueue.Clear();
            this.inDataQueue.Clear();

            this.client = new Telepathy.Client(int.MaxValue - 10);

            this.client.OnConnected = HandleConnect;
            this.client.OnDisconnected = HandleDisconnect;
            this.client.OnData = HandleData;

            this.client.NoDelay = isNoDelay;

            this.status = NetworkConnectionStatus.Connecting;

            this.isRunning = true;
            this.isDisconnecting = false;

            this.client.Connect(this.target.address.ToString(), this.target.port, OnConnectionSuccess, OnConnectionFail);
        }

        public void Disconnect()
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Cannot disconnect; socket is not connected");

            if (isDisconnecting)
                throw new InvalidOperationException($"Client is already disconnecting");

            isDisconnecting = true;

            client.Disconnect();
        }

        private void HandleConnect()
        {
            this.wasEverConnected = true;
            this.status = NetworkConnectionStatus.Connected;
            this.timer = new Timer(_ => UpdateDataQueue(), null, 0, 150);

            OnConnected.Call();
        }

        private void HandleDisconnect()
        {
            this.isDisconnecting = false;

            this.timer?.Dispose();
            this.timer = null;

            this.typeLib.Reset();

            this.outDataQueue.Clear();
            this.inDataQueue.Clear();

            this.status = NetworkConnectionStatus.Disconnected;

            this.isRunning = false;

            OnDisconnected.Call();

            foreach (var feature in features.Values)
            {
                if (!feature.isRunning)
                    continue;

                feature.InternalStop();
            }
        }

        private void HandleData(ArraySegment<byte> input)
        {
            if (!isRunning)
                throw new InvalidOperationException($"The client needs to be running to process data");

            inDataQueue.Enqueue(input.ToArray());

            OnData.Call(input);
        }

        private void OnConnectionSuccess() { }

        private void OnConnectionFail()
        {
            status = NetworkConnectionStatus.Disconnected;

            // this is a reset
            if (reconnectionFailed >= maxReconnections)
            {
                reconnectionResets++;
                reconnectionFailed = 0;
                reconnectionTimeout *= reconnectionResetMultiplier;

                Task.Run(async () =>
                {
                    await Task.Delay(reconnectionTimeout);

                    reconnectionFailed = 0;
                    reconnectionTimeout = reconnectionResets * (reconnectionTimeout * reconnectionResetMultiplier);

                    OnConnectionFail();
                });
            }
            else
            {
                reconnectionFailed++;

                Task.Run(async () =>
                {
                    await Task.Delay(reconnectionTimeout);

                    this.status = NetworkConnectionStatus.Connecting;
                    this.client.Connect(this.target.address.ToString(), this.target.port, OnConnectionSuccess, OnConnectionFail);
                });
            }
        }

        private void InternalStart()
        {
            OnAuthorized.Call();

            foreach (var feature in features.Values)
            {
                feature.net = funcs;

                if (feature.isRunning)
                    continue;

                feature.InternalStart();
            }
        }

        private void InternalSend(byte[] data)
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Cannot send data over an unconnected socket");

            client.Send(data.ToSegment());
        }

        private void InternalReceive(byte[] data)
        {
            var reader = readers.Next(data);
            var channel = reader.ReadByte();

            // PING MSG
            if (channel == 0)
            {
                var sentDate = reader.ReadDate();
                var recvDate = DateTime.Now;
                var latency = (recvDate - sentDate).TotalMilliseconds;

                this.latency = latency;

                if (latency > maxLatency)
                    maxLatency = latency;

                if (latency < minLatency)
                    minLatency = latency;

                avgLatency = (maxLatency + minLatency) / 2;

                readers.Return(reader);

                Send(writer =>
                {
                    writer.WriteByte(0);
                    writer.WriteDate(recvDate);
                    writer.WriteDate(sentDate);
                });

                OnPinged.Call();
            }
            // AUTH MSG
            else if (channel == 1)
            {
                if (!typeLib.Verify(reader)
                    || reader.ReadVersion() != version
                    || reader.ReadDate().Hour != DateTime.Now.Hour)
                {
                    Send(writer =>
                    {
                        writer.WriteByte(1);
                        writer.WriteByte((byte)NetworkHandshakeResult.Rejected);
                    });

                    readers.Return(reader);
                }
                else
                {
                    Send(writer =>
                    {
                        writer.WriteByte(1);
                        writer.WriteByte((byte)NetworkHandshakeResult.Confirmed);
                    });

                    OnAuthorized.Call();

                    readers.Return(reader);
                }
            }
            else
            {
                var messages = reader.ReadObjects();

                if (messages.Length <= 0)
                {
                    readers.Return(reader);
                    return;
                }

                for (int i = 0; i < messages.Length; i++)
                {
                    if (messages[i] is null)
                        continue;

                    var messageType = messages[i].GetType();

                    OnMessage.Call(messageType, messages[i]);

                    foreach (var feature in features.Values)
                    {
                        if (feature.isRunning && feature.HasListener(messageType))
                            feature.Receive(messages[i]);
                    }
                }

                readers.Return(reader);
            }
        }

        private void UpdateDataQueue()
        {
            lock (processLock)
            {
                if (status != NetworkConnectionStatus.Connected
                    || outDataQueue is null
                    || inDataQueue is null
                    || !isRunning
                    || isNoDelay)
                    return;

                var outProcessed = 0;
                var inProcessed = 0;

                while (outDataQueue.TryDequeue(out var outData)
                    && outProcessed <= maxOutput)
                {
                    InternalSend(outData);
                    outProcessed++;
                }

                while (inDataQueue.TryDequeue(out var inData)
                    && inProcessed <= maxInput)
                {
                    InternalReceive(inData);
                    inProcessed++;
                }
            }
        }
    }
}