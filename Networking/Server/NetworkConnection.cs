using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using Networking.Data;
using Networking.Features;
using Networking.Pooling;

using System;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

namespace Networking.Server
{
    public class NetworkConnection
    {
        private Timer timer;
        private Timer ping;

        private ConcurrentQueue<byte[]> inDataQueue;
        private ConcurrentQueue<byte[]> outDataQueue;

        private LockedDictionary<Type, NetworkFeature> features;

        public LogOutput log;
        public WriterPool writers;
        public ReaderPool readers;
        public NetworkFunctions funcs;
        public NetworkServer server;

        public bool isNoDelay = true;

        public int maxOutput = 10;
        public int maxInput = 10;

        public double latency = 0;

        public double maxLatency = 0;
        public double minLatency = 0;
        public double avgLatency = 0;

        public NetworkConnectionStatus status = NetworkConnectionStatus.Disconnected;

        public readonly int id;
        public readonly IPEndPoint remote;

        public event Action OnPinged;
        public event Action<Type, object> OnMessage;

        public NetworkConnection(int id, NetworkServer server, bool isNoDelay)
        {
            this.id = id;
            this.server = server;
            this.remote = server.server.GetClientEndPoint(id);
            this.isNoDelay = isNoDelay;

            this.log = new LogOutput($"Network Connection ({this.remote})");
            this.log.Setup();

            this.writers = new WriterPool();
            this.writers.Initialize(20);

            this.readers = new ReaderPool();
            this.readers.Initialize(20);

            this.inDataQueue = new ConcurrentQueue<byte[]>();
            this.outDataQueue = new ConcurrentQueue<byte[]>();

            this.features = new LockedDictionary<Type, NetworkFeature>();

            this.funcs = new NetworkFunctions(
                () => { return writers.Next(); },

                netData => { return readers.Next(netData); },
                netWriter => { Send(netWriter); },

                false);

            this.status = NetworkConnectionStatus.Connected;

            log.Info($"Connection initialized on {remote}!");

            this.timer = new Timer(_ => UpdateDataQueue(), null, 0, 10);

            foreach (var type in server.requestFeatures)
            {
                log.Debug($"Caching server feature: {type.FullName}");

                var feature = type.Construct();

                if (feature is null || feature is not NetworkFeature netFeature)
                    continue;

                features[type] = netFeature;

                netFeature.net = this.funcs;

                log.Debug($"Cached server feature: {type.FullName}");
            }

            this.ping = new Timer(_ => UpdatePing(), null, 0, 500);

            foreach (var feature in features.Values)
            {
                log.Debug($"Starting feature: {feature.GetType().FullName}");

                try
                {
                    feature.InternalStart();
                    feature.log!.Name += $" ({this.remote})";
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to start feature '{feature.GetType().FullName}':\n{ex}");
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

            log.Debug($"Adding feature {typeof(T).FullName}");

            var feature = Activator.CreateInstance<T>();

            if (feature is null)
                return;

            feature.net = funcs;
            feature.InternalStart();
            feature.log!.Name += $" ({this.remote})";

            features[typeof(T)] = feature;

            log.Debug($"Added feature {typeof(T).FullName}");
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (features.TryGetValue(typeof(T), out var feature))
                feature.InternalStop();

            features.Remove(typeof(T));
        }

        public void Send(Action<Writer> writerFunction)
        {
            var writer = writers.Next();
            writerFunction.Call(writer);
            Send(writer);
        }

        public void Send(Writer writer)
        {
            if (isNoDelay)
            {
                server.server.Send(id, writer.Buffer.ToSegment());
            }
            else
            {
                inDataQueue.Enqueue(writer.Buffer);
            }

            writer.Return();
        }

        public void Disconnect()
            => server.server.Disconnect(id);

        internal void InternalReceive(byte[] data)
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

                log.Debug($"Processing message: {messages[i].GetType().FullName}");

                OnMessage.Call(messages[i].GetType(), messages[i]);

                foreach (var feature in features.Values)
                {
                    log.Debug($"Validating message for feature '{feature.GetType().FullName}'");

                    if (feature.isRunning && feature.HasListener(messages[i].GetType()))
                    {
                        log.Debug($"Feature contains a listener.");

                        feature.Receive(messages[i]);
                        break;
                    }
                }
            }

            readers.Return(reader);
        }

        internal void Receive(byte[] data)
        {
            if (isNoDelay)
            {
                InternalReceive(data);
                return;
            }

            inDataQueue.Enqueue(data);
        }

        internal void Stop()
        {
            this.timer?.Dispose();
            this.timer = null;

            this.ping?.Dispose();
            this.ping = null;

            this.outDataQueue?.Clear();
            this.inDataQueue?.Clear();

            this.outDataQueue = null;
            this.inDataQueue = null;

            if (this.features != null)
            {
                foreach (var feature in this.features.Values)
                {
                    feature.InternalStop();
                }
            }

            this.features?.Clear();
            this.features = null;

            this.writers.Clear();
            this.writers = null;

            this.readers.Clear();
            this.readers = null;

            this.funcs = null;
            this.server = null;

            this.status = NetworkConnectionStatus.Disconnected;

            this.log?.Dispose();
            this.log = null;
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (pingMsg.isServer)
                return;

            latency = ((pingMsg.recv - pingMsg.sent).TotalMilliseconds) / 10;

            if (latency > maxLatency)
                maxLatency = latency;

            if (minLatency == 0 || latency < minLatency)
                minLatency = latency;

            avgLatency = (minLatency + maxLatency) / 2;

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

            while (outDataQueue.TryDequeue(out var outData) && outProcessed <= maxOutput)
            {
                server.server.Send(id, outData.ToSegment());
                outProcessed++;
            }

            var inProcessed = 0;

            while (inDataQueue.TryDequeue(out var inData) && inProcessed <= maxInput)
            {
                InternalReceive(inData);
                inProcessed++;
            }
        }
    }
}
