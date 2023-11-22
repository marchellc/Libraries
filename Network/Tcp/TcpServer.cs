using Network.Logging;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Telepathy;

namespace Network.Tcp
{
    public class TcpServer : NetworkManager
    {
        private readonly Dictionary<int, TcpPeer> peers = new Dictionary<int, TcpPeer>();

        private Server server;
        private Thread tickThread;

        private volatile bool tick;

        public override bool IsInitialized => server != null && tickThread != null;
        public override bool IsRunning => server != null && server.Active;
        public override bool IsConnected => server != null && peers.Count > 0;

        public int Port { get; set; } = 8080;

        public TcpServer(int port)
            => Port = port;

        public override void Start()
        {
            base.Start();

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Initializing TCP server");

            if (server != null)
                Stop();

            server = new Server(short.MaxValue);
            server.NoDelay = true;

            server.OnConnected = OnConnected;
            server.OnDisconnected = OnDisconnected;
            server.OnData = OnData;

            tickThread = new Thread(async () =>
            {
                while (tick)
                {
                    server.Tick(10);
                    await Task.Delay(10);
                }
            });

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Initialized TCP server");
        }

        public override void Stop()
        {
            base.Stop();

            if (server is null)
                throw new InvalidOperationException($"Server cannot be stopped; not initialized");

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Disposing TCP server");

            Disconnect();

            tick = false;
            tickThread = null;

            server.Stop();
            server = null;

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Disposed TCP server");
        }

        public override void Connect()
        {
            base.Connect();

            if (server is null || server.Active)
                throw new InvalidOperationException($"Server cannot be connected; not initialized or already started");

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Starting TCP server on port={Port}");

            tick = true;
            tickThread.Start();

            server.Start(Port);

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Started TCP server on port={Port}");
        }

        public override void Disconnect()
        {
            base.Disconnect();

            if (server is null || !server.Active)
                throw new InvalidOperationException($"Server cannot be disconnected; not initialized or not started");

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Stopping TCP server on port={Port}");

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Disconnecting peers ..");

            foreach (var connId in peers.Keys)
            {
                server.Disconnect(connId);
                NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Disconnected peer={connId}");
            }

            peers.Clear();

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Stopped TCP server on port={Port}");
        }

        public override void Send(int id, ArraySegment<byte> data)
        {
            base.Send(id, data);

            if (server is null || !server.Active)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SERVER", $"Cannot send data to {id}; server not initialized or inactive");
                return;
            }

            if (!peers.ContainsKey(id))
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SERVER", $"Cannot send data to {id}; invalid peer ID");
                return;
            }

            server.Send(id, data);
        }

        internal string GetAddress(int connId)
            => server.GetClientAddress(connId);

        private void OnConnected(int connId)
        {
            var peer = new TcpPeer(connId, this);

            peers[connId] = peer;

            peer.Start();

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Peer connected ID={connId} address={peer.Address}");
        }

        private void OnDisconnected(int connId)
        {
            if (!peers.ContainsKey(connId))
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SERVER", $"Peer ID={connId} disconnected, but was not present in the dictionary");
                return;
            }

            peers[connId].Stop();
            peers.Remove(connId);

            NetworkLog.Add(NetworkLogLevel.Info, "SERVER", $"Peer ID={connId} disconnected");
        }

        private void OnData(int connId, ArraySegment<byte> data)
        {
            if (!peers.ContainsKey(connId))
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SERVER", $"Received data for peer ID={connId}, but was not present in the dictionary");
                return;
            }

            peers[connId].Receive(data);
        }
    }
}
