using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Reflection;
using Common.Utilities;

using Network.Attributes;
using Network.Extensions;
using Network.Interfaces.Transporting;
using Network.Interfaces.Controllers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Network.Tcp
{
    public class TcpTransport : ITransport
    {
        public const byte PING_REQ = 1;
        public const byte PING_RES = 2;

        public const byte SYNC_REQ = 3;
        public const byte SYNC_RES = 4;

        private IPeer peer;
        private IController controller;

        private long sent = 0;
        private long recv = 0;

        private bool isSynced;

        private Latency latency;

        private LogOutput log;

        private Timer latencyTimer;

        private LockedList<Type> syncedTypes = new LockedList<Type>();
        private LockedDictionary<byte, List<Action<BinaryReader>>> msgHandlers = new LockedDictionary<byte, List<Action<BinaryReader>>>();
        private LockedDictionary<Type, LockedList<WrappedAction<Delegate>>> typeHandlers;

        public TcpTransport(IPeer peer, IController controller)
        {
            this.peer = peer;
            this.controller = controller;
            this.log = new LogOutput($"TRANSPORT_{peer.Target}").Setup();
            this.typeHandlers = new LockedDictionary<Type, LockedList<WrappedAction<Delegate>>>();
            this.msgHandlers = new LockedDictionary<byte, List<Action<BinaryReader>>>();
            this.syncedTypes = new LockedList<Type>();
            this.latency = new Latency();
        }

        public IController Controller => controller;

        public Latency Latency => latency;

        public bool IsRunning => true;

        public long Sent => sent;
        public long Received => recv;

        public void Initialize()
        {
            CreateHandler(PING_REQ, HandleLatencyRequest);
            CreateHandler(PING_RES, HandleLatencyResponse);

            CreateHandler(SYNC_REQ, HandleSyncRequest);
            CreateHandler(SYNC_RES, HandleSyncResponse);

            if (controller is IServer)
                Synchronize();
        }

        public void Shutdown()
        {
            latencyTimer?.Dispose();
            latencyTimer = null;

            syncedTypes.Clear();
            syncedTypes = null;

            latency = default;

            msgHandlers.Clear();
            msgHandlers = null;

            typeHandlers.Clear();
            typeHandlers = null;

            log.Dispose();
            log = null;

            controller = null;
            peer = null;

            isSynced = false;

            recv = 0;
            sent = 0;
        }

        public void CreateHandler(byte msgId, Action<BinaryReader> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!msgHandlers.TryGetValue(msgId, out var list))
                list = msgHandlers[msgId] = new List<Action<BinaryReader>>();

            if (list.Contains(handler))
                return;

            list.Add(handler);
        }

        public void CreateHandler<T>(Action<T> handler)
        {
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (!typeHandlers.TryGetValue(type, out var list))
                list = typeHandlers[type] = new LockedList<WrappedAction<Delegate>>();

            list.Add(new WrappedAction<Delegate>
            {
                Type = type,
                Proxy = handler,

                Target = value =>
                {
                    if (value is null || value is not T t)
                        handler.Call(default);
                    else
                        handler.Call(t);
                }
            });
        }

        public void RemoveHandler(byte msgId, Action<BinaryReader> handler) 
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!msgHandlers.TryGetValue(msgId, out var list))
                return;

            list.Remove(handler);
        }

        public void RemoveHandler<T>(Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (!typeHandlers.TryGetValue(type, out var list))
                return;

            list.RemoveRange(wrap => wrap.Proxy is Action<T> act && act == handler);
        }

        public void Receive(byte[] data)
        {
            if (recv >= long.MaxValue || (recv + data.Length) >= long.MaxValue)
                recv = 0;

            recv += data.Length;

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                var messageId = br.ReadByte();

                if (msgHandlers.TryGetValue(messageId, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Call(br);
                    }

                    return;
                }

                var objects = br.ReadArray(false, () => br.ReadObject(this));

                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i] is null)
                    {
                        log.Warn($"Received a null object at index {i}, discarding");
                        continue;
                    }

                    var type = objects[i].GetType();

                    if (typeHandlers.TryGetValue(type, out var typeList))
                    {
                        for (int x = 0; x < list.Count; x++)
                            typeList[x].Proxy.Method.Call(typeList[x].Proxy.Target, null, objects[i]);
                    }
                    else
                    {
                        log.Warn($"Received a message with no handlers: {type.FullName}");
                    }
                }
            }
        }

        public void Send(byte[] data)
        {
            if (sent >= long.MaxValue || (sent + data.Length) >= long.MaxValue)
                sent = 0;

            sent += data.Length;

            if (controller is IServer server)
                server.Send(peer.Id, data);
            else if (controller is IClient client)
                client.Send(data);
        }

        public void Synchronize()
            => this.Send(SYNC_REQ, null);

        public short GetTypeId(Type type)
        {
            for (int i = 0; i < syncedTypes.Count; i++)
            {
                if (syncedTypes[i] == type)
                    return (short)i;
            }

            return -1;
        }

        public Type GetType(short typeId)
        {
            if (typeId < 0 || typeId >= syncedTypes.Count)
                return null;

            return syncedTypes[typeId];
        }

        private void HandleLatencyRequest(BinaryReader br)
        {
            latency.SentAt = br.ReadDate();
            latency.ReceivedAt = DateTime.Now;

            latency.Trip = (latency.ReceivedAt - latency.SentAt).TotalMilliseconds / TimeSpan.TicksPerMillisecond;

            if (latency.MinTrip <= 0 || latency.Trip < latency.MinTrip)
                latency.MinTrip = latency.Trip;

            if (latency.MaxTrip <= 0 || latency.Trip > latency.MaxTrip)
                latency.MaxTrip = latency.Trip;

            latency.AverageTrip = (latency.MaxTrip + latency.MinTrip) / 2;

            this.Send(PING_RES, bw =>
            {
                bw.WriteDate(latency.SentAt);
                bw.WriteDate(latency.ReceivedAt);
            });
        }

        private void HandleLatencyResponse(BinaryReader br)
        {
            latency.SentAt = br.ReadDate();
            latency.ReceivedAt = br.ReadDate();

            latency.Trip = (latency.ReceivedAt - latency.SentAt).TotalMilliseconds / TimeSpan.TicksPerMillisecond;

            if (latency.MinTrip <= 0 || latency.Trip < latency.MinTrip)
                latency.MinTrip = latency.Trip;

            if (latency.MaxTrip <= 0 || latency.Trip > latency.MaxTrip)
                latency.MaxTrip = latency.Trip;

            latency.AverageTrip = (latency.MaxTrip + latency.MinTrip) / 2;
        }

        private void HandleSyncRequest(BinaryReader br)
        {
            if (isSynced)
                return;

            isSynced = true;

            syncedTypes.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type == typeof(IMessage))
                        continue;

                    if (syncedTypes.Contains(type))
                        continue;

                    if (typeof(IMessage).IsAssignableFrom(type))
                    {
                        syncedTypes.Add(type);
                        log.Trace($"Added sync type '{type.FullName}' (interface)");
                    }
                    else if (type.IsDefined(typeof(NetworkTypeAttribute), true))
                    {
                        syncedTypes.Add(type);
                        log.Trace($"Added sync type '{type.FullName}' (attribute)");
                    }
                }
            }

            this.Send(SYNC_RES, bw => bw.WriteItems(syncedTypes, t => bw.WriteType(t)));
        }

        private void HandleSyncResponse(BinaryReader br)
        {
            if (isSynced)
                return;

            syncedTypes.Clear();

            latencyTimer = new Timer(UpdateLatency, null, 1500, 5000);

            try
            {
                var types = br.ReadArray(true, br.ReadType);

                syncedTypes.AddRange(types);
            }
            catch (Exception ex)
            {
                log.Fatal($"Failed to read sync types, disconnecting!\n{ex}");
                peer.Disconnect();
                return;
            }

            isSynced = true;
        }

        private void UpdateLatency(object _)
            => this.Send(PING_REQ, bw => bw.WriteDate(DateTime.Now));
    }
}