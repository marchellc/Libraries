﻿using Common.Extensions;
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
        private DateTime handshakeSentAt;

        private Timer timer;
        private Timer ping;
        private Timer handshakeTimer;

        private ConcurrentQueue<byte[]> inDataQueue;
        private ConcurrentQueue<byte[]> outDataQueue;

        private LockedDictionary<Type, NetworkFeature> features;

        public LogOutput log;
        public WriterPool writers;
        public ReaderPool readers;
        public NetworkFunctions funcs;
        public NetworkServer server;

        public bool isNoDelay = true;
        public bool isAuthed;

        public int maxOutput = 10;
        public int maxInput = 10;

        public double latency = 0;

        public double maxLatency = 0;
        public double minLatency = 0;
        public double avgLatency = 0;

        public NetworkConnectionStatus status = NetworkConnectionStatus.Disconnected;
        public NetworkHandshakeResult handshake = NetworkHandshakeResult.TimedOut;

        public readonly int id;
        public readonly IPEndPoint remote;

        public event Action OnAuthorized;
        public event Action OnPinged;
        public event Action<Type, object> OnMessage;

        public NetworkConnection(int id, NetworkServer server)
        {
            this.id = id;
            this.server = server;
            this.remote = server.server.GetClientEndPoint(id);

            this.log = new LogOutput($"Network Connection ({this.remote})");
            this.log.Setup();

            this.writers = new WriterPool();
            this.writers.Initialize(20);

            this.readers = new ReaderPool();
            this.readers.Initialize(20);

            this.timer = new Timer(_ => UpdateDataQueue(), null, 100, 100);
            this.ping = new Timer(_ => UpdatePing(), null, 100, 500);

            this.funcs = new NetworkFunctions(
                () => { return writers.Next(); },

                netData => { return readers.Next(netData); },
                netWriter => { Send(netWriter); },

                false);

            this.status = NetworkConnectionStatus.Connected;

            foreach (var type in server.requestFeatures)
            {
                var feature = type.Construct();

                if (feature is null || feature is not NetworkFeature netFeature)
                    continue;

                features[type] = netFeature;

                netFeature.net = this.funcs;
            }

            Send(writer =>
            {
                writer.WriteVersion(NetworkServer.version);
                writer.WriteDate(DateTime.Now.ToLocalTime());
            });

            this.handshakeSentAt = DateTime.Now;
            this.handshakeTimer = new Timer(_ => UpdateHandshake(), null, 0, 200);
        }

        public T Get<T>() where T : NetworkFeature
        {
            if (features.TryGetValue(typeof(T), out var feature) && feature is T netFeature)
                return netFeature;

            return default;
        }

        public void Add<T>() where T : NetworkFeature
        {
            if (isAuthed)
            {
                if (features.ContainsKey(typeof(T)))
                    return;

                var feature = Activator.CreateInstance<T>();

                if (feature is null)
                    return;

                feature.net = funcs;
                feature.InternalStart();
                feature.log!.Name += $" ({this.remote})";

                features[typeof(T)] = feature;
            }
        }

        public void Remove<T>() where T : NetworkFeature
        {
            if (isAuthed)
            {
                if (features.TryGetValue(typeof(T), out var feature))
                    feature.InternalStop();

                features.Remove(typeof(T));
            }
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
                server.server.Send(id, writer.Buffer.ToSegment());
            else
                inDataQueue.Enqueue(writer.Buffer);

            writer.Return();
        }

        public void Disconnect()
            => server.server.Disconnect(id);

        internal void InternalReceive(byte[] data)
        {
            var reader = readers.Next(data);

            if (!isAuthed)
            {
                handshake = (NetworkHandshakeResult)reader.ReadByte();
                readers.Return(reader);
                return;
            }
            else
            {
                var messages = reader.ReadToEnd();

                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i] is NetworkPingMessage pingMsg)
                    {
                        ProcessPing(pingMsg);
                        continue;
                    }

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
                return;
            }
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

            this.handshakeTimer?.Dispose();
            this.handshakeTimer = null;

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
            this.handshake = NetworkHandshakeResult.TimedOut;

            this.log?.Dispose();
            this.log = null;

            this.isAuthed = false;
        }

        private void ProcessPing(NetworkPingMessage pingMsg)
        {
            if (pingMsg.isServer)
                return;

            latency = (pingMsg.recv - pingMsg.sent).TotalMilliseconds;

            if (latency > maxLatency)
                maxLatency = latency;

            if (minLatency == 0 || latency < minLatency)
                minLatency = latency;

            avgLatency = (minLatency + maxLatency) / 2;

            OnPinged.Call();
        }

        private void UpdateHandshake()
        {
            if ((DateTime.Now - handshakeSentAt).TotalSeconds >= 15)
            {
                handshake = NetworkHandshakeResult.TimedOut;
                return;
            }

            if (handshake != NetworkHandshakeResult.Confirmed)
            {
                Disconnect();
                return;
            }

            isAuthed = true;

            handshakeTimer.Dispose();
            handshakeTimer = null;

            foreach (var feature in features.Values)
            {
                feature.InternalStart();
                feature.log!.Name += $" ({this.remote})";
            }

            OnAuthorized.Call();
        }

        private void UpdatePing()
        {
            Send(writer =>
            {
                writer.WriteByte(0);
                writer.WriteDate(DateTime.Now);
            });

            OnPinged.Call();
        }

        private void UpdateDataQueue()
        {
            if (isNoDelay)
            {
                timer?.Dispose();
                timer = null;

                return;
            }

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
