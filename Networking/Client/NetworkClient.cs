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

        private volatile Timer timer;
        private volatile Timer connTimer;

        private ConcurrentQueue<byte[]> inDataQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> outDataQueue = new ConcurrentQueue<byte[]>();

        private LockedList<Type> reqFeatures = new LockedList<Type>();
        private LockedDictionary<Type, NetworkFeature> features = new LockedDictionary<Type, NetworkFeature>();

        private object processLock = new object();

        public bool IsRunning { get; private set; }

        public uint ReconnectionAttempts { get; private set; }

        public NetworkConnectionStatus Status { get; private set; } = NetworkConnectionStatus.Disconnected;

        public IPInfo Target { get; set; } = new IPInfo(IPType.Local, 8000, IPAddress.Loopback);

        public LogOutput Log { get; }

        public LatencyInfo Latency { get; } = new LatencyInfo();
        public ClientConfig Config { get; } = new ClientConfig();

        public event Action OnDisconnected;
        public event Action OnConnected;
        public event Action OnPinged;

        public event Action<ArraySegment<byte>> OnData;
        public event Action<Type, object> OnMessage;

        public static Version ClientVersion { get; }
        public static NetworkClient Instance { get; }
        
        static NetworkClient()
        {
            ClientVersion = new Version(1, 0, 0, 0);
            Instance = new NetworkClient();

            TypeLoader.Init();
        }

        public NetworkClient()
        {
            Log = new LogOutput("Network Client");
            Log.Setup();

            funcs = new NetworkFunctions(
                Send,
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

            Log.Verbose($"Adding feature '{typeof(T).FullName}'");

            var feature = typeof(T).Construct() as T;

            features[typeof(T)] = feature;

            Log.Verbose($"Added feature '{typeof(T).FullName}'");

            if (Status != NetworkConnectionStatus.Connected)
                return;

            Log.Verbose($"Starting feature '{feature.GetType().FullName}'");

            try
            {
                feature.InternalStart(funcs);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            Log.Verbose($"Started feature '{feature.GetType().FullName}'");
        }

        public void Remove<T>() where T : NetworkFeature, new()
        {
            if (!features.TryGetValue(typeof(T), out var feature))
                return;

            feature.InternalStop();

            features.Remove(typeof(T));
        }

        public void Send(Action<Writer> writer)
            => funcs.Send(writer);

        public void Send(Writer writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            outDataQueue.Enqueue(writer.Buffer);

            WriterPool.Shared.Return(writer);
        }

        public void Send(byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(data));

            outDataQueue.Enqueue(data);
        }

        public void Connect(IPEndPoint endPoint = null)
        {
            if (IsRunning)
                Disconnect();

            if (endPoint != null && Target.EndPoint != null && Target.EndPoint != endPoint)
                Target = new IPInfo(IPType.Remote, endPoint.Port, endPoint.Address);
            else if (Target.EndPoint is null && endPoint != null)
                Target = new IPInfo(IPType.Remote, endPoint.Port, endPoint.Address);

            Log.Name = $"Network Client ({Target})";

            Log.Info($"Client connecting to {Target} ..");

            try
            {
                outDataQueue.Clear();
                inDataQueue.Clear();

                client = new Telepathy.Client(1024 * 1024 * 10);

                client.OnConnected = HandleConnect;
                client.OnDisconnected = HandleDisconnect;
                client.OnData = HandleData;

                client.NoDelay = true;

                Status = NetworkConnectionStatus.Connecting;

                IsRunning = true;

                connTimer?.Dispose();
                connTimer = null;

                timer = new Timer(_ => UpdateDataQueue(), null, 0, 10);

                TryConnect();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Disconnect()
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Cannot disconnect; socket is not connected");

            client.Disconnect();

            Log.Verbose($"Disconnect forced.");
        }

        private void TryConnect()
        {
            if (client is null || client.Connected)
                return;

            while (client.Connecting)
                continue;

            Status = NetworkConnectionStatus.Disconnected;

            if (ReconnectionAttempts >= Config.MaxReconnectionAttempts)
            {
                Log.Error($"Connection attempts count reached, retrying in 30 seconds.");

                connTimer = new Timer(_ =>
                {
                    ReconnectionAttempts = 0;

                    connTimer.Dispose();
                    connTimer = null;

                    Log.Verbose($"Timer disposed, retrying connection.");

                    TryConnect();
                }, null, 30000, 30000);

                return;
            }

            Status = NetworkConnectionStatus.Connecting;

            client.Connect(Target.Raw, Target.Port,

            () =>
            {
                ReconnectionAttempts = 0;
                HandleConnect();
            },

            () =>
            {
                Log.Error($"Connection failed, retrying ({ReconnectionAttempts} / {Config.MaxReconnectionAttempts}) ..");
                ReconnectionAttempts++;
                TryConnect();
            });
        }

        private void HandleConnect()
        {
            try
            {
                connTimer?.Dispose();
                connTimer = null;

                Status = NetworkConnectionStatus.Connected;

                if (reqFeatures.Count > 0 && features.Count == 0)
                {
                    foreach (var feature in reqFeatures)
                    {
                        Log.Verbose($"Adding feature '{feature.FullName}'");

                        var featureObj = feature.Construct() as NetworkFeature;

                        features[feature] = featureObj;

                        Log.Verbose($"Added feature '{feature.FullName}'");
                    }
                }

                InternalStart();

                OnConnected.Call();

                Log.Info($"Client has connected to '{Target}'!");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void HandleDisconnect()
        {
            Log.Warn($"Client has disconnected from '{Target}'!");

            connTimer?.Dispose();
            connTimer = null;

            timer?.Dispose();
            timer = null;

            outDataQueue.Clear();
            inDataQueue.Clear();

            Status = NetworkConnectionStatus.Disconnected;

            IsRunning = false;

            OnDisconnected.Call();

            try
            {
                foreach (var feature in features.Values)
                {
                    if (!feature.IsRunning)
                        continue;

                    feature.InternalStop();
                }
            }
            catch { }

            features.Clear();

            ReconnectionAttempts = 0;

            TryConnect();
        }

        private void HandleData(ArraySegment<byte> input)
        {
            if (!IsRunning)
                throw new InvalidOperationException($"The client needs to be running to process data");

            inDataQueue.Enqueue(input.ToArray());

            OnData.Call(input);
        }

        private void InternalStart()
        {
            foreach (var feature in features)
            {
                if (feature.Value.IsRunning)
                    continue;

                Log.Verbose($"Starting feature: {feature.GetType().FullName} ..");

                try
                {
                    feature.Value.InternalStart(funcs);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                Log.Verbose($"Started feature '{feature.GetType().FullName}'");
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
            var reader = ReaderPool.Shared.Rent(data);

            try
            {
                var messages = reader.ReadAnonymousArray();

                for (int i = 0; i < messages.Length; i++)
                {
                    if (messages[i] is NetworkPingMessage pingMsg)
                    {
                        ProcessPing(pingMsg);
                        continue;
                    }

                    Log.Verbose($"Processing message: {messages[i].GetType().FullName}");

                    OnMessage.Call(messages[i].GetType(), messages[i]);

                    foreach (var feature in features.Values)
                    {
                        if (feature.IsRunning && feature.HasListener(messages[i].GetType()))
                        {
                            feature.Receive(messages[i]);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while processing incoming data:\n{ex}");
            }

            ReaderPool.Shared.Return(reader);
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (!pingMsg.isServer)
                return;

            Latency.Update((pingMsg.recv - pingMsg.sent).TotalMilliseconds);

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
                        && outProcessed <= Config.OutgoingAmount)
                    {
                        InternalSend(outData);
                        outProcessed++;
                    }

                    while (inDataQueue.TryDequeue(out var inData)
                        && inProcessed <= Config.IncomingAmount)
                    {
                        InternalReceive(inData);
                        inProcessed++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Data update loop failed!\n{ex}");
                }
            }
        }
    }
}