using Common.Logging;
using Common.Extensions;
using Common.Pooling.Pools;

using Common.IO.Collections;
using Common.IO.Data;

using Networking.Features;
using Networking.Utilities;
using Networking.Messages;

using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using Common.Utilities;

namespace Networking.Client
{
    public class NetworkClient
    {
        private Telepathy.Client client;

        private NetworkFunctions funcs;

        private volatile Timer connTimer;

        private LockedList<Type> reqFeatures = new LockedList<Type>();
        private LockedDictionary<Type, NetworkFeature> features = new LockedDictionary<Type, NetworkFeature>();

        private ConcurrentQueue<object> unhandledMessages = new ConcurrentQueue<object>();

        public bool IsRunning { get; private set; }

        public uint ReconnectionAttempts { get; private set; }
        public uint MaxReconnectionAttempts { get; set; } = 10;

        public NetworkConnectionStatus Status { get; private set; } = NetworkConnectionStatus.Disconnected;

        public IPEndPoint Target { get; set; }

        public LogOutput Log { get; }

        public LatencyInfo Latency { get; } = new LatencyInfo();

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
        }

        public NetworkClient()
        {
            Log = new LogOutput("Network Client");
            Log.Setup();

            Target = new IPEndPoint(IPAddress.Loopback, 8000);

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

        public void Send<T>(T message)
            => Send(writer => writer.WriteObject(message));

        public void Send(Action<DataWriter> writer)
            => funcs.Send(writer);

        public void Send(DataWriter writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            Send(writer.Data);

            PoolablePool<DataWriter>.Shared.Return(writer);
        }

        public void Send(byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(data));

            client.Send(data.ToSegment());
        }

        public void Connect(IPEndPoint endPoint = null)
        {
            if (IsRunning)
                Disconnect();

            if (Target is null && endPoint is null)
                throw new ArgumentNullException(nameof(endPoint));

            if (Target is null && endPoint != null || (Target != null && endPoint != null && Target != endPoint))
                Target = endPoint;

            Log.Name = $"Network Client ({Target})";
            Log.Info($"Client connecting to {Target} ..");

            try
            {
                client = new Telepathy.Client(ushort.MaxValue);

                client.OnConnected = HandleConnect;
                client.OnDisconnected = HandleDisconnect;
                client.OnData = HandleData;

                client.NoDelay = true;

                Status = NetworkConnectionStatus.Connecting;

                IsRunning = true;

                CodeUtils.WhileTrue(() => IsRunning, () => client.Tick(10), 50);

                connTimer?.Dispose();
                connTimer = null;

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

            if (ReconnectionAttempts >= MaxReconnectionAttempts)
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

            client.Connect(Target.Address.ToString(), Target.Port,

            () =>
            {
                ReconnectionAttempts = 0;
                HandleConnect();
            },

            () =>
            {
                Log.Error($"Connection failed, retrying ({ReconnectionAttempts} / {MaxReconnectionAttempts}) ..");
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

            unhandledMessages.Clear();

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
            
            var reader = PoolablePool<DataReader>.Shared.Rent();

            try
            {
                reader.Set(input.ToArray());

                var message = reader.ReadObject();

                ProcessMessage(message);
            }
            catch (Exception ex)
            {
                Log.Error($"HandleData caught an exception:\n{ex}");
            }

            PoolablePool<DataReader>.Shared.Return(reader);

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

        private void ProcessMessage(object message)
        {
            try
            {
                if (message != null)
                {
                    var type = message.GetType();

                    if (message is NetworkPingMessage networkPingMessage)
                        ProcessPing(networkPingMessage);
                    else
                    {
                        var isHandled = false;

                        Log.Verbose($"Processing message: {type.FullName}");

                        OnMessage.Call(type, message);

                        foreach (var feature in features.Values)
                        {
                            if (feature.IsRunning && feature.HasListener(type))
                            {
                                isHandled = true;

                                feature.Receive(message);
                                break;
                            }
                        }

                        if (!isHandled)
                        {
                            Log.Warn($"No message handlers are registered for message {type.FullName}.");
                            unhandledMessages.Enqueue(message);
                        }
                    }
                }
                else
                {
                    Log.Error($"Received a null message!");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while processing incoming data:\n{ex}");

                if (message != null)
                    unhandledMessages.Enqueue(message);
            }
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (!pingMsg.IsFromServer)
                return;

            Latency.Update((pingMsg.Received - pingMsg.Sent).TotalMilliseconds);

            pingMsg.IsFromServer = false;

            Send(pingMsg);

            OnPinged.Call();

            if (unhandledMessages.TryDequeue(out var message))
                ProcessMessage(message);
        }
    }
}