using Common.Extensions;
using Common.Reflection;
using Common.Logging;

using System;
using System.Net;
using System.Linq;
using System.Threading;

using Telepathy;

using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;
using Network.Features;

namespace Network.Tcp
{
    public class TcpClient : IClient
    {
        private Client client;
        private Timer timer;
        private TcpPeer peer;
        private LogOutput log;

        private ControllerFeatureManager features;

        private int tickRate = 100;

        private volatile int maxAttempts;
        private volatile int curAttempt;

        private volatile Timer connTimer;

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
        public IFeatureManager Features => features;

        public IPEndPoint Target { get; set; }

        public TcpClient() 
        {
            features = new ControllerFeatureManager();
        }

        public TcpClient(IPEndPoint endPoint)
        {
            Target = endPoint;
            features = new ControllerFeatureManager();
        }

        public void Send(byte[] data)
        {
            if (client is null || !client.Connected)
                throw new InvalidOperationException($"Attempted to send data over an unconnected socket.");

            client.Send(data.ToSegment());
        }

        public void Start()
        {
            if (IsRunning)
                Stop();

            if (Target is null)
                throw new InvalidOperationException($"Cannot start the client with a null target.");

            log = new LogOutput($"Client {Target.Port}").Setup();
            log.Info("Initializing KCP client");
                
            client = new Client(short.MaxValue * 100);

            client.NoDelay = true;

            client.OnConnected = OnConnectedHandler;
            client.OnData = OnDataHandler;
            client.OnDisconnected = OnDisconnectedHandler;

            Log.Info = log.Info;
            Log.Error = log.Error;
            Log.Warning = log.Warn;

            features.Enable(null);

            if (!IsManual)
                timer = new Timer(TickTimer, null, 0, TickRate);

            curAttempt = 0;
            maxAttempts = 30;

            TryConnect();
        }

        public void Stop()
        {
            if (client is null)
                throw new InvalidOperationException($"Attempted to stop an unconnected socket.");

            if (client.Connected)
                client.Disconnect();

            features.Disable();

            timer.Dispose();
            timer = null;

            connTimer?.Dispose();
            connTimer = null;

            curAttempt = 0;
            maxAttempts = 30;

            Log.Info = _ => { };
            Log.Error = _ => { };
            Log.Warning = _ => { };

            log.Dispose();
            log = null;

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
            if (peer != null)
                return;

            connTimer?.Dispose();
            connTimer = null;

            log.Debug($"Connected, timer disposed.");

            peer = new TcpPeer(0, this, Target);

            peer.Start();
            peer.Features?.Enable(features);

            OnConnected.Call(peer);

            log.Info($"Connected to server");

            curAttempt = 0;
            maxAttempts = 30;
        }

        private void OnDisconnectedHandler()
        {
            if (peer is null)
                return;

            OnDisconnected.Call(peer);

            peer?.Stop();
            peer = null;

            log.Warn("Disconnected from server, attempting to reconnect.");

            maxAttempts = 15;
            curAttempt = 0;

            TryConnect();
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

        private void TryConnect()
        {
            if (client is null || client.Connected)
                return;

            while (client.Connecting)
                continue;

            if (curAttempt >= maxAttempts)
            {
                log.Error($"Connection attempts count reached, retrying in 30 seconds.");

                connTimer = new Timer(_ =>
                {
                    curAttempt = 0;

                    connTimer.Dispose();
                    connTimer = null;

                    log.Debug($"Timer disposed, retrying connection.");

                    TryConnect();
                }, null, 30000, 30000);

                return;
            }

            client.Connect(Target.Address.ToString(), Target.Port,

            () =>
            {
                curAttempt = 0;
                maxAttempts = 30;

                log.Info("Succesfully connected!");
            },
            
            () =>
            {
                log.Error($"Connection failed, retrying ({curAttempt + 1} / {maxAttempts}) ..");

                curAttempt++;

                TryConnect();
            });
        }
    }
}