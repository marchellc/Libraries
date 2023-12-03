using Common.Extensions;
using Common.IO.Collections;
using Common.Reflection;

using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;

using System;
using System.Net;
using System.Threading;
using System.Linq;

using Telepathy;
using Common.Logging;

namespace Network.Tcp
{
    public class TcpServer : IServer
    {
        private Server server;
        private Timer timer;
        private LogOutput log;

        private LockedList<TcpPeer> peers;
        private LockedList<Type> features = new LockedList<Type>();

        private int tickRate = 100;
        private bool isManual;

        public event Action<IPeer> OnConnected;
        public event Action<IPeer> OnDisconnected;
        public event Action<IPeer, ITransport, byte[]> OnData;

        public bool IsRunning => server != null;
        public bool IsActive => server != null && server.Active;

        public bool IsManual
        {
            get => isManual;
            set
            {
                isManual = value;

                if (isManual && timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
                else if (!isManual && timer is null)
                {
                    timer = new Timer(TickTimer, null, 0, TickRate);
                }
            }
        }

        public int TickRate
        {
            get => tickRate;
            set
            {
                tickRate = value;
                timer?.Change(0, tickRate);
            }
        }

        public IPEndPoint Target { get; set; }

        public void Start()
        {
            if (IsRunning)
                Stop();

            peers = new LockedList<TcpPeer>();
            log = new LogOutput($"SERVER :: {Target.Port}").Setup();

            log.Info("Starting ..");

            server = new Server(short.MaxValue * 100);
            server.NoDelay = true;
            server.OnConnected = OnConnectedHandler;
            server.OnData = OnDataHandler;
            server.OnDisconnected = OnDisconnectedHandler;

            Log.Info = LogOutput.Common.Info;
            Log.Error = LogOutput.Common.Error;
            Log.Warning = LogOutput.Common.Warn;

            if (!IsManual)
                timer = new Timer(TickTimer, null, 0, TickRate);

            server.Start(Target.Port);

            log.Info("Started");
        }

        public void Stop()
        {
            if (server is null)
                throw new InvalidOperationException($"Attempted to stop an unconnected socket.");

            if (server.Active)
                server.Stop();

            timer.Dispose();
            timer = null;

            log.Dispose();
            log = null;

            server.OnConnected = null;
            server.OnData = null;
            server.OnDisconnected = null;
            server = null;

            peers.Clear();
            peers = null;
        }

        public void Tick()
        {
            if (!IsActive)
                throw new InvalidOperationException($"Attempted to tick an unconnected socket.");

            if (!IsManual)
                throw new InvalidOperationException($"You need to first switch the server mode to manual before manually ticking.");

            server.Tick(TickRate * 100);
        }

        public void Send(int connectionId, byte[] data)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Attempted to tick an unconnected socket.");

            server.Send(connectionId, data.ToSegment());
        }

        public void Disconnect(int connectionId)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Attempted to disconnect an unconnected socket.");

            server.Disconnect(connectionId);
        }

        public T AddFeature<T>() where T : IFeature, new()
        {
            if (!features.Contains(typeof(T)))
                features.Add(typeof(T));

            return default;
        }

        public T GetFeature<T>() where T : IFeature
            => default;

        public void RemoveFeature<T>() where T : IFeature
        {
            features.Remove(typeof(T));
        }

        public Type[] GetFeatures()
            => features.ToArray();

        private void OnConnectedHandler(int connectionId)
        {
            if (peers.Any(p => p.Id == connectionId))
                return;

            var peer = new TcpPeer(connectionId, this, server.GetClientEndPoint(connectionId));

            peers.Add(peer);

            peer.Start();

            OnConnected.Call(peer);

            log.Info($"Client connected from '{peer.Target}' as {connectionId}");
        }

        private void OnDisconnectedHandler(int connectionId)
        {
            var removed = peers.RemoveRange(p => p.Id == connectionId);

            for (int i = 0; i < removed.Count; i++)
            {
                log.Info($"Client disconnected from {removed[i].Target}");

                OnDisconnected.Call(removed[i]);

                removed[i].Stop();
            }
        }

        private void OnDataHandler(int connectionId, ArraySegment<byte> data)
        {
            var bytes = data.ToArray();

            for (int i = 0; i < peers.Count; i++)
            {
                if (peers[i].Id == connectionId)
                {
                    peers[i].Transport?.Receive(bytes);
                    OnData.Call(peers[i], peers[i].Transport, bytes);
                }
            }
        }

        private void TickTimer(object _)
        {
            server?.Tick(tickRate * 100);
        }
    }
}
