using Common.Logging;
using Common.Extensions;
using Common.IO.Collections;

using Networking.Address;
using Networking.Data;
using Networking.Pooling;
using Networking.Features;

using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

using Networking.Utilities;

namespace Networking.Client
{
    public class NetworkClient
    {
        private Telepathy.Client client;

        private NetworkFunctions funcs;

        private Timer timer;
        private volatile Timer connTimer;

        private ConcurrentQueue<byte[]> inDataQueue;
        private ConcurrentQueue<byte[]> outDataQueue;

        private LockedList<Type> reqFeatures;
        private LockedDictionary<Type, NetworkFeature> features;

        private object processLock = new object();

        public int maxOutput = 10;
        public int maxInput = 10;
        public int maxReconnections = 15;

        public double latency = 0;

        public double maxLatency = 0;
        public double minLatency = 0;
        public double avgLatency = 0;

        public int reconnectionFailed = 0;

        public bool isRunning;
        public bool isDisconnecting;
        public bool isNoDelay = true;

        public bool wasEverConnected;

        public NetworkConnectionStatus status = NetworkConnectionStatus.Disconnected;

        public IPInfo target;

        public LogOutput log;

        public readonly WriterPool writers;
        public readonly ReaderPool readers;

        public event Action OnDisconnected;
        public event Action OnConnected;
        public event Action OnPinged;

        public event Action<ArraySegment<byte>> OnData;
        public event Action<Type, object> OnMessage;

        public static readonly Version version;
        public static readonly NetworkClient instance;
        
        static NetworkClient()
        {
            version = new Version(1, 0, 0, 0);
            instance = new NetworkClient();

            TypeLoader.Init();
        }

        public NetworkClient()
        {
            this.log = new LogOutput("Network Client");
            this.log.Setup();

            this.writers = new WriterPool();
            this.readers = new ReaderPool();

            this.writers.Initialize(20);
            this.readers.Initialize(20);

            this.inDataQueue = new ConcurrentQueue<byte[]>();
            this.outDataQueue = new ConcurrentQueue<byte[]>();

            this.features = new LockedDictionary<Type, NetworkFeature>();
            this.reqFeatures = new LockedList<Type>();

            this.target = new IPInfo(IPType.Remote, 8000, IPAddress.Loopback);

            this.funcs = new NetworkFunctions(
                () => { return writers.Next(); },

                netData => { return readers.Next(netData); },
                netWriter => { Send(netWriter); },

                Disconnect,

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

            reqFeatures.Add(typeof(T));

            log.Verbose($"Adding feature '{typeof(T).FullName}'");

            var feature = typeof(T).Construct() as T;

            feature.net = funcs;

            features[typeof(T)] = feature;

            log.Verbose($"Added feature '{typeof(T).FullName}'");

            if (status != NetworkConnectionStatus.Connected)
                return;

            log.Verbose($"Starting feature '{feature.GetType().FullName}'");

            try
            {
                feature.InternalStart();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            log.Verbose($"Started feature '{feature.GetType().FullName}'");
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

            if (!isNoDelay)
            {
                outDataQueue.Enqueue(data);
            }
            else
            {
                InternalSend(data);
            }
        }

        public void Connect(IPEndPoint endPoint = null)
        {
            if (isRunning)
                Disconnect();

            if (endPoint != null && this.target.endPoint != null && this.target.endPoint != endPoint)
                this.target = new IPInfo(IPType.Remote, endPoint.Port, endPoint.Address);
            else if (this.target.endPoint is null && endPoint != null)
                this.target = new IPInfo(IPType.Remote, endPoint.Port, endPoint.Address);

            log.Name = $"Network Client ({target})";
            log.Info($"Client connecting to {target} ..");

            try
            {
                this.outDataQueue.Clear();
                this.inDataQueue.Clear();

                this.client = new Telepathy.Client(1024 * 1024 * 10);

                this.client.OnConnected = HandleConnect;
                this.client.OnDisconnected = HandleDisconnect;
                this.client.OnData = HandleData;

                this.client.NoDelay = true;

                this.status = NetworkConnectionStatus.Connecting;

                this.isRunning = true;
                this.isDisconnecting = false;

                this.connTimer?.Dispose();
                this.connTimer = null;

                this.timer = new Timer(_ => UpdateDataQueue(), null, 0, 10);

                TryConnect();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void Disconnect()
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Cannot disconnect; socket is not connected");

            if (isDisconnecting)
                throw new InvalidOperationException($"Client is already disconnecting");

            isDisconnecting = true;

            client.Disconnect();

            log.Verbose($"Disconnect forced.");
        }

        private void TryConnect()
        {
            if (client is null || client.Connected)
                return;

            while (client.Connecting)
                continue;

            status = NetworkConnectionStatus.Disconnected;

            if (reconnectionFailed >= maxReconnections)
            {
                log.Error($"Connection attempts count reached, retrying in 30 seconds.");

                connTimer = new Timer(_ =>
                {
                    reconnectionFailed = 0;

                    connTimer.Dispose();
                    connTimer = null;

                    log.Verbose($"Timer disposed, retrying connection.");

                    TryConnect();
                }, null, 30000, 30000);

                return;
            }

            status = NetworkConnectionStatus.Connecting;

            client.Connect(target.raw, target.port,

            () =>
            {
                reconnectionFailed = 0;
                HandleConnect();
            },

            () =>
            {
                log.Error($"Connection failed, retrying ({reconnectionFailed} / {maxReconnections}) ..");
                reconnectionFailed++;
                TryConnect();
            });
        }

        private void HandleConnect()
        {
            try
            {
                this.connTimer?.Dispose();
                this.connTimer = null;

                this.wasEverConnected = true;
                this.status = NetworkConnectionStatus.Connected;

                if (this.reqFeatures.Count > 0 && this.features.Count == 0)
                {
                    foreach (var feature in reqFeatures)
                    {
                        log.Verbose($"Adding feature '{feature.FullName}'");

                        var featureObj = feature.Construct() as NetworkFeature;

                        featureObj.net = funcs;

                        features[feature] = featureObj;

                        log.Verbose($"Added feature '{feature.FullName}'");
                    }
                }

                InternalStart();

                OnConnected.Call();

                this.log.Info($"Client has connected to '{this.target}'!");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void HandleDisconnect()
        {
            this.log.Warn($"Client has disconnected from '{this.target}'!");

            this.isDisconnecting = false;

            this.connTimer?.Dispose();
            this.connTimer = null;

            this.timer?.Dispose();
            this.timer = null;

            this.outDataQueue.Clear();
            this.inDataQueue.Clear();

            this.status = NetworkConnectionStatus.Disconnected;

            this.isRunning = false;

            OnDisconnected.Call();

            try
            {
                foreach (var feature in features.Values)
                {
                    if (!feature.isRunning)
                        continue;

                    feature.InternalStop();
                }
            }
            catch { }

            this.features.Clear();
            this.reconnectionFailed = 0;

            TryConnect();
        }

        private void HandleData(ArraySegment<byte> input)
        {
            if (!isRunning)
                throw new InvalidOperationException($"The client needs to be running to process data");

            if (!isNoDelay)
                inDataQueue.Enqueue(input.ToArray());
            else
                InternalReceive(input.ToArray());

            OnData.Call(input);
        }

        private void InternalStart()
        {
            foreach (var feature in features)
            {
                log.Verbose($"Starting feature: {feature.GetType().FullName} ..");

                feature.Value.net = funcs;

                if (feature.Value.isRunning)
                    continue;

                try
                {
                    feature.Value.InternalStart();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }

                log.Verbose($"Started feature '{feature.GetType().FullName}'");
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
            var messages = reader.ReadAnonymousArray();

            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i] is NetworkPingMessage pingMsg)
                {
                    ProcessPing(pingMsg);
                    continue;
                }

                log.Verbose($"Processing message: {messages[i].GetType().FullName}");

                OnMessage.Call(messages[i].GetType(), messages[i]);

                foreach (var feature in features.Values)
                {
                    if (feature.isRunning && feature.HasListener(messages[i].GetType()))
                    {
                        feature.Receive(messages[i]);
                        break;
                    }
                }
            }

            readers.Return(reader);
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (!pingMsg.isServer)
                return;

            latency = (pingMsg.recv - pingMsg.sent).TotalMilliseconds;

            if (latency > maxLatency)
                maxLatency = latency;

            if (minLatency == 0 || latency < minLatency)
                minLatency = latency;

            avgLatency = (minLatency + maxLatency) / 2;

            Send(writer =>
            {
                pingMsg.isServer = false;
                writer.WriteAnonymousArray(pingMsg);
            });

            OnPinged.Call();
        }

        private void UpdateDataQueue()
        {
            lock (processLock)
            {
                try
                {
                    client?.Tick(100);

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
                catch (Exception ex)
                {
                    log.Error($"Data update loop failed!\n{ex}");
                }
            }
        }
    }
}