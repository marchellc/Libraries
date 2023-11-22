using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System;

using Telepathy;

using Network.Logging;

namespace Network.Tcp
{
    public class TcpClient : NetworkManager
    {
        private Client client;
        private TcpPeer peer;
        private Thread updateThread;
        private IPEndPoint target;

        private volatile bool update;

        public override bool IsInitialized => client != null && updateThread != null;
        public override bool IsRunning => client != null && (client.Connecting || client.Connected);
        public override bool IsConnected => client != null && client.Connected;

        public override NetworkPeer Peer => peer;

        public override IPEndPoint EndPoint => target;

        public TcpClient(IPEndPoint endPoint)
            => target = endPoint;

        public override void Start()
        {
            base.Start();

            if (client != null)
            {
                Disconnect();
                Stop();
            }

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Starting TCP client");

            client = new Client(short.MaxValue);
            client.NoDelay = true;

            client.OnConnected = OnConnected;
            client.OnDisconnected = OnDisconnected;
            client.OnData = OnData;

            updateThread = new Thread(async () =>
            {
                while (update)
                {
                    client.Tick(10);
                    await Task.Delay(10);
                }
            });

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Started TCP client");
        }

        public override void Stop()
        {
            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Stopping TCP client");

            update = false;
            updateThread = null;
            client = null;
            peer = null;

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Stopped TCP client");
        }

        public override void Connect()
        {
            if (client is null)
                throw new InvalidOperationException($"Cannot connect client; not initialized");

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Connecting to {target}");

            update = true;
            updateThread.Start();

            client.Connect(EndPoint.Address.ToString(), EndPoint.Port);
        }

        public override void Disconnect()
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Cannot disconnect client; not initialized or not connected");

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Disconnecting from {target}");

            client.Disconnect();
        }

        public override void Send(int id, ArraySegment<byte> data)
        {
            base.Send(id, data);

            if (client is null || !client.Connected)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "CLIENT", $"Cannot send data; client is not initialized or not connected");
                return;
            }

            client.Send(data);
        }

        private void OnConnected()
        {
            peer = new TcpPeer(0, this);
            peer.Start();

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Connected to {target}");
        }

        private void OnDisconnected()
        {
            if (peer != null)
            {
                peer.Stop();
                peer = null;
            }

            NetworkLog.Add(NetworkLogLevel.Info, "CLIENT", $"Disconnected from {target}");

            Stop();
        }

        private void OnData(ArraySegment<byte> data)
        {
            if (peer is null)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "CLIENT", $"Cannot receive data; peer is not initialized");
                return;
            }

            peer.Receive(data);
        }
    }
}