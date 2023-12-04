using Common.Extensions;
using Common.IO.Collections;
using Common.Reflection;
using Common.Logging;

using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;
using Network.Features;

using System;
using System.Net;
using System.Threading;
using System.Linq;

using Telepathy;
using Common.Logging.Console;
using Common.Logging.File;

namespace Network.Tcp
{
    public class TcpServer : IServer
    {
        private Server server;
        private Timer timer;
        private LogOutput log;
        private ControllerFeatureManager features;

        private LockedDictionary<int, TcpPeer> peers;

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

        public IFeatureManager Features => features;

        public IPEndPoint Target { get; set; }

        public TcpServer()
        {
            features = new ControllerFeatureManager();
        }

        public TcpServer(IPEndPoint endPoint)
        {
            Target = endPoint;
            features = new ControllerFeatureManager();
        }

        public void Start()
        {
            if (IsRunning)
                Stop();

            if (Target is null)
                throw new InvalidOperationException($"Cannot bind the server to a null target!");

            peers = new LockedDictionary<int, TcpPeer>();
            log = new LogOutput($"Server {Target.Port}").AddConsoleIfPresent().AddFileWithPrefix($"Server_{Target.Port}");

            log.Info("Starting ..");

            server = new Server(short.MaxValue * 100);

            server.NoDelay = true;

            server.OnConnected = OnConnectedHandler;
            server.OnData = OnDataHandler;
            server.OnDisconnected = OnDisconnectedHandler;

            features.Enable(null);

            Log.Info = log.Info;
            Log.Error = log.Error;
            Log.Warning = log.Warn;

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

            features.Disable();

            timer.Dispose();
            timer = null;

            Log.Info = str => { };
            Log.Error = str => { };
            Log.Warning = str => { };

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

        private void OnConnectedHandler(int connectionId)
        {
            if (peers.ContainsKey(connectionId))
                return;

            var peer = peers[connectionId] = new TcpPeer(connectionId, this, server.GetClientEndPoint(connectionId));

            peer.Start();
            peer.Features?.Enable(features);

            OnConnected.Call(peer);

            log.Info($"Client connected from '{peer.Target}' as {connectionId}");
        }

        private void OnDisconnectedHandler(int connectionId)
        {
            if (!peers.TryGetValue(connectionId, out var peer))
                return;

            peer.Stop();

            peers.Remove(connectionId);

            OnDisconnected.Call(peer);
        }

        private void OnDataHandler(int connectionId, ArraySegment<byte> data)
        {
            if (!peers.TryGetValue(connectionId, out var peer))
                return;

            var bytes = data.ToArray();

            OnData.Call(peer, peer.Transport, bytes);

            peer.Transport?.Receive(bytes);
        }

        private void TickTimer(object _)
        {
            server?.Tick(tickRate * 100);
        }
    }
}
