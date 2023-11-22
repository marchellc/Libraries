using Common.Extensions;
using Common.Reflection;

using Network.Attributes;
using Network.Data;
using Network.Extensions;
using Network.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Network.Tcp
{
    public class TcpPeer : NetworkPeer
    {
        private int id;

        private NetworkManager manager;

        private Timer latencyTimer;

        private List<Tuple<MessageId, Action<BinaryReader>>> basicHandlers;
        private List<Tuple<Type, Delegate>> handlers;
        private List<NetworkFeature> features;
        private List<MessageChannel> channels;
        private List<Type> types;

        private long beatSentAt = 0;
        private long beatReceivedAt = 0;

        private short beatLatency;

        private string address;

        public TcpPeer(int id, NetworkManager manager)
        {
            if (manager is TcpClient tcpClient)
                address = tcpClient.EndPoint.ToString();
            else if (manager is TcpServer tcpServer)
                address = tcpServer.GetAddress(id);
            else
                address = "unknown";

            this.id = id;
            this.manager = manager;

            this.features = new List<NetworkFeature>();
            this.channels = new List<MessageChannel>();

            var channels = manager.GetChannels();
            var features = manager.GetFeatures();

            if (channels != null)
            {
                for (int i = 0; i < channels.Length; i++)
                {
                    this.channels.Add(channels[i]);
                    NetworkLog.Add(NetworkLogLevel.Debug, $"PEER={Id}={address}", $"Added channel: {channels[i].Id}");
                }
            }

            if (features != null)
            {
                for (int i = 0; i < features.Length; i++)
                {
                    var feature = Activator.CreateInstance(features[i]) as NetworkFeature;

                    if (feature is null)
                        continue;

                    this.features.Add(feature);

                    NetworkLog.Add(NetworkLogLevel.Debug, $"PEER={Id}={address}", $"Added feature: {feature.GetType().FullName}");
                }

                this.features = this.features.OrderByDescending(f => f.Priority).ToList();

                for (int i = 0; i < this.features.Count; i++)
                {
                    try
                    {
                        this.features[i].StartInternal(this);
                        NetworkLog.Add(NetworkLogLevel.Info, $"PEER={Id}={address}", $"Started feature: {this.features[i].GetType().FullName}");
                    }
                    catch (Exception ex)
                    {
                        NetworkLog.Add(NetworkLogLevel.Error, $"PEER={Id}={address}", $"Failed to start feature '{this.features[i].GetType().FullName}' due to an exception:\n{ex}");
                    }
                }
            }
        }

        public override int Id => id;

        public override short Latency => beatLatency;

        public override bool IsConnected => manager.IsConnected;
        public override bool IsInitialized => manager.IsInitialized;
        public override bool IsRunning => manager.IsRunning;

        public override string Address => address;

        public override NetworkManager Manager => manager;

        public override IReadOnlyList<NetworkFeature> Features => features;
        public override IReadOnlyList<MessageChannel> Channels => channels;

        public override void Start()
        {
            Handle(new MessageId(0, MessageChannel.INTERNAL_REQUESTS, MessageId.TYPE_SYNC_ID), br =>
            {
                Send(new MessageId
                {
                    Channel = MessageChannel.INTERNAL_RESPONSES,
                    Header = 0,
                    Id = MessageId.TYPE_SYNC_ID,
                    IsInternal = true
                }, bw =>
                {
                    bw.WriteItems(types, bw.WriteType);
                    bw.WriteItems(channels, ch => bw.Write(ch.Id));
                });
            });

            Handle(new MessageId(0, MessageChannel.INTERNAL_RESPONSES, MessageId.TYPE_SYNC_ID), br =>
            {
                types = br.ReadList(br.ReadType);
                channels = br.ReadList(() => new MessageChannel(this, br.ReadByte()));
            });

            Handle(new MessageId(0, MessageChannel.INTERNAL_REQUESTS, MessageId.TYPE_BEAT_ID), br =>
            {
                beatReceivedAt = DateTime.Now.Ticks;
                beatSentAt = br.ReadInt64();
                beatLatency = (short)(Math.Floor((decimal)(beatReceivedAt - beatSentAt) / TimeSpan.TicksPerMillisecond) - 10);
                InvokeLatencyRequested(beatSentAt);

                Send(new MessageId
                {
                    Channel = MessageChannel.INTERNAL_RESPONSES,
                    Header = 0,
                    Id = MessageId.TYPE_BEAT_ID,
                    IsInternal = true
                }, bw => bw.Write(beatReceivedAt));
            });

            Handle(new MessageId(0, MessageChannel.INTERNAL_RESPONSES, MessageId.TYPE_BEAT_ID), br =>
            {
                beatSentAt = br.ReadInt64();
                beatReceivedAt = DateTime.Now.Ticks;
                beatLatency = (short)(Math.Floor((decimal)(beatReceivedAt - beatSentAt) / TimeSpan.TicksPerMillisecond) - 10);
                InvokeLatencyReceived(beatSentAt, beatLatency);
            });

            if (manager is TcpClient)
            {
                types = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type == typeof(IWritable) || type == typeof(IReadable))
                            continue;

                        if (types.Contains(type))
                            continue;

                        if (typeof(IWritable).IsAssignableFrom(type)
                            || typeof(IReadable).IsAssignableFrom(type)
                            || type.IsDefined(typeof(NetworkTypeAttribute), true))
                            types.Add(type);
                    }
                }
            }
            else
            {
                Send(MessageId.SYNC_MESSAGE);

                latencyTimer = new Timer(_ =>
                {
                    beatSentAt = DateTime.Now.Ticks;
                    beatReceivedAt = 0;
                    InvokeLatencyRequested(beatSentAt);
                    Send(MessageId.BEAT_MESSAGE, bw => bw.Write(beatSentAt));
                }, null, 2000, 2000);
            }

            base.Start();
        }

        public override void Receive(ArraySegment<byte> data)
        {
            base.Receive(data);

            try
            {
                using (var ms = new MemoryStream(data.Array, data.Offset, data.Count, false))
                using (var br = new BinaryReader(ms))
                {
                    var msg = br.ReadMessage();

                    NetworkLog.Add(NetworkLogLevel.Debug, "PEER", $"Received message: {msg.Id} {msg.Header} {msg.Channel}");

                    for (int i = 0; i < basicHandlers.Count; i++)
                    {
                        if (basicHandlers[i].Item1.Id == msg.Id
                            && basicHandlers[i].Item1.Header == msg.Header
                            && basicHandlers[i].Item1.Channel == msg.Channel
                            && basicHandlers[i].Item1.IsInternal == msg.IsInternal)
                        {
                            try
                            {
                                basicHandlers[i].Item2.Call(br);
                            }
                            catch (Exception ex)
                            {
                                NetworkLog.Add(NetworkLogLevel.Error, "PEER", $"Failed to invoke basic handler:\n{ex}");
                            }
                        }
                    }

                    if (msg.IsInternal)
                        return;

                    var msgValue = br.ReadObject(msg, this);
                    var msgType = types[msg.Id];
                    var msgData = new Message
                    {
                        Channel = channels.FirstOrDefault(c => c.Id == msg.Channel),
                        Id = msg,
                        Type = msgType,
                        Value = msgValue
                    };

                    msgData.Channel?.Receive(msgData);

                    for (int i = 0; i < features.Count; i++)
                    {
                        try
                        {
                            features[i].Receive(msgData);
                        }
                        catch (Exception ex)
                        {
                            NetworkLog.Add(NetworkLogLevel.Error, $"PEER={Id}={address}", $"Feature '{features[i].GetType().FullName}' failed to receive message:\n{ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NetworkLog.Add(NetworkLogLevel.Error, $"PEER={Id}={address}", $"Caught an exception while processing message:\n{ex}");
            }
        }

        public override void Send(Message message)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.WriteMessage(message.Id);
                bw.WriteObject(message.Value, message.Id, this);

                Send(new ArraySegment<byte>(ms.ToArray()));
            }
        }

        public override void Send(MessageId message)
        {
            using (var rMs = new MemoryStream())
            using (var rBw = new BinaryWriter(rMs))
            {
                rBw.WriteMessage(message); 

                Send(new ArraySegment<byte>(rMs.ToArray()));
            }
        }

        public override void Send(MessageId message, Action<BinaryWriter> write)
        {
            using (var rMs = new MemoryStream())
            using (var rBw = new BinaryWriter(rMs))
            {
                rBw.WriteMessage(message);
                write.Call(rBw);
                Send(new ArraySegment<byte>(rMs.ToArray()));
            }
        }

        public override void Send(ArraySegment<byte> data)
            => manager.Send(id, data);

        public override void Stop()
        {
            features = features.OrderBy(f => f.Priority).ToList();

            for (int i = 0; i < features.Count; i++)
            {
                try
                {
                    features[i].Stop();
                }
                catch (Exception ex)
                {
                    NetworkLog.Add(NetworkLogLevel.Error, $"PEER={Id}={address}", $"Failed to stop feature '{features[i].GetType().FullName}' due to an exception:\n{ex}");
                }
            }

            latencyTimer.Dispose();
            latencyTimer = null;

            channels.Clear();
            channels = null;

            types?.Clear();
            types = null;

            basicHandlers.Clear();
            basicHandlers = null;

            handlers.Clear();
            handlers = null;

            features.Clear();
            features = null;

            beatLatency = 0;
            beatReceivedAt = 0;
            beatSentAt = 0;

            id = 0;

            manager = null;
            address = null;

            base.Stop();
        }

        public override void AddFeature(NetworkFeature feature)
        {
            base.AddFeature(feature);
            features.Add(feature);
            feature.StartInternal(this);
        }

        public override void RemoveFeature(NetworkFeature feature)
        {
            base.RemoveFeature(feature);
            feature.StopInternal();
            features.Remove(feature);
        }

        public override void Handle(MessageId message, Action<BinaryReader> handler)
        {
            base.Handle(message, handler);
            basicHandlers.Add(new Tuple<MessageId, Action<BinaryReader>>(message, handler));
        }

        public override void Handle<TMessage>(Action<Message, TMessage> handler)
        {
            base.Handle(handler);
            handlers.Add(new Tuple<Type, Delegate>(typeof(TMessage), handler));
        }

        public override void RemoveHandle(Action<BinaryReader> handler)
        {
            base.RemoveHandle(handler);
            basicHandlers.RemoveAll(t => t.Item2 == handler);
        }

        public override void RemoveHandle<TMessage>(Action<Message, TMessage> handler)
        {
            base.RemoveHandle(handler);
            handlers.RemoveAll(t => t.Item2 == handler);
        }

        public override object ReadObject(BinaryReader reader)
        {
            if (types is null || types.Count < 1)
                throw new InvalidOperationException($"Cannot read object; types not received");

            return reader.ReadObject(this);
        }

        public override void WriteObject(BinaryWriter writer, object instance)
            => writer.WriteObject(instance, this);

        internal override List<Type> GetSyncTypes()
            => types;
    }
}