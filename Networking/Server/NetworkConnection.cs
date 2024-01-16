using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using Networking.Data;
using Networking.Features;
using Networking.Pooling;
using Networking.Utilities;

using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

namespace Networking.Server
{
    public class NetworkConnection
    {
        private volatile Timer timer;
        private volatile Timer ping;

        private ConcurrentQueue<byte[]> inDataQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> outDataQueue = new ConcurrentQueue<byte[]>();

        private LockedDictionary<Type, NetworkFeature> features = new LockedDictionary<Type, NetworkFeature>();

        public event Action OnPinged;
        public event Action<Type, object> OnMessage;

        public LogOutput Log { get; }

        public ClientConfig Config { get; } = new ClientConfig();
        public LatencyInfo Latency { get; } = new LatencyInfo();

        public NetworkServer Server { get; }
        public NetworkFunctions Network { get; }

        public IPEndPoint EndPoint { get; }

        public NetworkConnectionStatus Status { get; private set; }

        public int Id { get; }

        public NetworkConnection(int id, NetworkServer server)
        {
            EndPoint = server.ApiServer.GetClientEndPoint(id);

            Log = new LogOutput($"Network Connection ({EndPoint})");
            Log.Setup();

            Id = id;
            Server = server;

            Network = new NetworkFunctions(
                Send,

                () => server.ApiServer.Disconnect(id),

                false);

            Status = NetworkConnectionStatus.Connected;

            Log.Info($"Connection initialized on {EndPoint}!");

            timer = new Timer(_ => UpdateDataQueue(), null, 0, 10);

            foreach (var type in server.Features)
            {
                Log.Verbose($"Caching server feature: {type.FullName}");

                var feature = type.Construct();

                if (feature is null || feature is not NetworkFeature netFeature)
                    continue;

                features[type] = netFeature;

                Log.Debug($"Cached server feature: {type.FullName}");
            }

            ping = new Timer(_ => UpdatePing(), null, 0, 500);

            foreach (var feature in features.Values)
            {
                Log.Debug($"Starting feature: {feature.GetType().FullName}");

                try
                {
                    feature.InternalStart(Network);
                    feature.Log!.Name += $" ({EndPoint})";
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to start feature '{feature.GetType().FullName}':\n{ex}");
                }
            }
        }

        public T Get<T>() where T : NetworkFeature
        {
            if (features.TryGetValue(typeof(T), out var feature) && feature is T netFeature)
                return netFeature;

            return default;
        }

        public void Add<T>() where T : NetworkFeature
        {
            if (features.ContainsKey(typeof(T)))
                return;

            Log.Debug($"Adding feature {typeof(T).FullName}");

            var feature = Activator.CreateInstance<T>();

            if (feature is null)
                return;

            feature.InternalStart(Network);
            feature.Log!.Name += $" ({EndPoint})";

            features[typeof(T)] = feature;

            Log.Debug($"Added feature {typeof(T).FullName}");
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (features.TryGetValue(typeof(T), out var feature))
                feature.InternalStop();

            features.Remove(typeof(T));
        }

        public void Send(Action<Writer> writerFunction)
        {
            var writer = WriterPool.Shared.Rent();
            writerFunction.Call(writer);
            Send(writer);
        }

        public void Send(Writer writer)
        {
            inDataQueue.Enqueue(writer.Buffer);
            WriterPool.Shared.Return(writer);
        }

        public void Disconnect()
            => Server.ApiServer.Disconnect(Id);

        internal void InternalReceive(byte[] data)
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
                        Log.Verbose($"Validating message for feature '{feature.GetType().FullName}'");

                        if (feature.IsRunning && feature.HasListener(messages[i].GetType()))
                        {
                            Log.Verbose($"Feature contains a listener.");

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

        internal void Stop()
        {
            timer?.Dispose();
            timer = null;

            ping?.Dispose();
            ping = null;

            outDataQueue?.Clear();
            inDataQueue?.Clear();

            outDataQueue = null;
            inDataQueue = null;

            foreach (var feature in features.Values)
                feature.InternalStop();

            features.Clear();

            Status = NetworkConnectionStatus.Disconnected;
        }

        internal void Receive(byte[] data)
            => inDataQueue.Enqueue(data);

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (pingMsg.isServer)
                return;

            Latency.Update((pingMsg.recv - pingMsg.sent).TotalMilliseconds / 10);

            OnPinged.Call();
        }

        private void UpdatePing()
        {
            Send(writer => writer.WriteAnonymousArray(new NetworkPingMessage(true, DateTime.Now, DateTime.MinValue)));
            OnPinged.Call();
        }

        private void UpdateDataQueue()
        {
            var outProcessed = 0;

            while (outDataQueue.TryDequeue(out var outData) && outProcessed <= Config.OutgoingAmount)
            {
                Server.ApiServer.Send(Id, outData.ToSegment());
                outProcessed++;
            }

            var inProcessed = 0;

            while (inDataQueue.TryDequeue(out var inData) && inProcessed <= Config.IncomingAmount)
            {
                InternalReceive(inData);
                inProcessed++;
            }
        }
    }
}
