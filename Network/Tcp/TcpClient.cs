using Common.Extensions;
using Common.Reflection;

using System;
using System.Net;
using System.Threading;

using Telepathy;

namespace Network.Tcp
{
    public class TcpClient : IClient
    {
        private Client client;
        private Timer timer;
        private TcpPeer peer;

        private int tickRate = 100;
        private bool isManual;

        public event Action<IPeer> OnConnected;
        public event Action<IPeer> OnDisconnected;
        public event Action<IPeer, ITransport, byte[]> OnData;

        public bool IsRunning => client != null;
        public bool IsConnected => client != null && client.Connected;

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

        public IPeer Peer => peer;
        public ITransport Transport => peer?.Transport;

        public IPEndPoint Target { get; set; }

        public TcpClient() { }

        public TcpClient(IPEndPoint endPoint)
            => Target = endPoint;

        public void Send(byte[] data)
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Attempted to send data over an unconnected socket.");

            client.Send(new ArraySegment<byte>(data, 0, data.Length));
        }

        public void Start()
        {
            if (IsRunning)
                Stop();

            client = new Client(short.MaxValue * 100);
            client.NoDelay = true;
            client.OnConnected = OnConnectedHandler;
            client.OnData = OnDataHandler;
            client.OnDisconnected = OnDisconnectedHandler;

            if (!IsManual)
                timer = new Timer(TickTimer, null, 0, TickRate);

            client.Connect(Target.Address.ToString(), Target.Port);
        }

        public void Stop()
        {
            if (client is null)
                throw new InvalidOperationException($"Attempted to stop an unconnected socket.");

            if (client.Connected)
                client.Disconnect();

            timer.Dispose();
            timer = null;

            client.OnConnected = null;
            client.OnData = null;
            client.OnDisconnected = null;
            client = null;
        }
        
        public void Tick()
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Attempted to tick an unconnected socket.");

            if (!IsManual)
                throw new InvalidOperationException($"You need to first switch the client mode to manual before manually ticking.");

            client.Tick(TickRate * 100);
        }

        private void OnConnectedHandler()
        {
            peer = new TcpPeer(0, this, Target);
            peer.Start();

            OnConnected.Call(peer);
        }

        private void OnDisconnectedHandler()
        {
            OnDisconnected.Call(peer);

            peer?.Stop();
            peer = null;
        }

        private void OnDataHandler(ArraySegment<byte> data)
        {
            var bytes = data.ToArray();

            Transport?.Receive(bytes);
            OnData.Call(Peer, Transport, bytes);
        }

        private void TickTimer(object _)
        {
            client?.Tick(TickRate * 100);
        }
    }
}