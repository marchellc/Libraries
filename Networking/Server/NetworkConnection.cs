using Common.Extensions;
using Common.Logging;

using Common.Pooling.Pools;

using Common.IO.Collections;
using Common.IO.Data;

using Networking.Features;
using Networking.Utilities;
using Networking.Messages;

using System;
using System.Net;
using System.Threading;

namespace Networking.Server
{
    public class NetworkConnection
    {
        private volatile Timer ping;

        private LockedDictionary<Type, NetworkFeature> features = new LockedDictionary<Type, NetworkFeature>();

        public event Action OnPinged;
        public event Action<Type, object> OnMessage;

        public LogOutput Log { get; }

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

            foreach (var type in server.Features)
            {
                var feature = type.Construct();

                if (feature is null || feature is not NetworkFeature netFeature)
                    continue;

                features[type] = netFeature;
            }

            ping = new Timer(_ => UpdatePing(), null, 0, 500);

            foreach (var feature in features.Values)
            {
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

            var feature = typeof(T).Construct<T>();

            if (feature is null)
                return;

            feature.InternalStart(Network);
            feature.Log!.Name += $" ({EndPoint})";

            features[typeof(T)] = feature;
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (features.TryGetValue(typeof(T), out var feature))
                feature.InternalStop();

            features.Remove(typeof(T));
        }

        public void Send<T>(T message)
            => Send(writer => writer.WriteObject(message));

        public void Send(Action<DataWriter> writerFunction)
        {
            var writer = PoolablePool<DataWriter>.Shared.Rent();
            writerFunction(writer);
            Send(writer);
        }

        public void Send(DataWriter writer)
        {
            Server.ApiServer.Send(Id, writer.Data.ToSegment());
            PoolablePool<DataWriter>.Shared.Return(writer);
        }

        public void Disconnect()
            => Server.ApiServer.Disconnect(Id);

        internal void InternalReceive(byte[] data)
        {
            var reader = PoolablePool<DataReader>.Shared.Rent();

            reader.Set(data);

            try
            {
                var message = reader.ReadObject();

                if (message != null)
                {
                    var msgType = message.GetType();

                    if (message is NetworkPingMessage networkPingMessage)
                        ProcessPing(networkPingMessage);
                    else
                    {
                        Log.Verbose($"Processing message: {msgType.FullName}");

                        OnMessage.Call(msgType, message);

                        foreach (var feature in features.Values)
                        {
                            if (feature.IsRunning && feature.HasListener(msgType))
                            {
                                feature.Receive(message);
                                break;
                            }
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
            }

            PoolablePool<DataReader>.Shared.Return(reader);
        }

        internal void Stop()
        {
            ping?.Dispose();
            ping = null;

            foreach (var feature in features.Values)
                feature.InternalStop();

            features.Clear();

            Status = NetworkConnectionStatus.Disconnected;
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (pingMsg.IsFromServer)
                return;

            Latency.Update((pingMsg.Received - pingMsg.Sent).TotalMilliseconds);

            OnPinged.Call();
        }

        private void UpdatePing()
            => Send(new NetworkPingMessage(true, DateTime.Now, DateTime.Now));
    }
}
