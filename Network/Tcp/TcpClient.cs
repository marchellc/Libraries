using Common.Extensions;
using Common.Reflection;
using Common.IO.Collections;

using System;
using System.Net;
using System.Threading;

using Telepathy;

using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;
using System.Linq;
using Common.Logging;

namespace Network.Tcp
{
    public class TcpClient : IClient
    {
        private Client client;
        private Timer timer;
        private TcpPeer peer;
        private LogOutput log;

        private int tickRate = 100;
        private bool isManual;

        private LockedList<Type> features = new LockedList<Type>();

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

            client.Send(data.ToSegment());
        }

        public void Start()
        {
            if (IsRunning)
                Stop();

            log = new LogOutput($"CLIENT_{Target}").Setup();
            log.Info("Initializing KCP client");
                
            client = new Client(short.MaxValue * 100);
            client.NoDelay = true;
            client.OnConnected = OnConnectedHandler;
            client.OnData = OnDataHandler;
            client.OnDisconnected = OnDisconnectedHandler;

            Log.Info = LogOutput.Common.Info;
            Log.Error = LogOutput.Common.Error;
            Log.Warning = LogOutput.Common.Warn;

            if (!IsManual)
                timer = new Timer(TickTimer, null, 0, TickRate);

            log.Info("Connecting ..");

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

        public T AddFeature<T>() where T : IFeature, new()
        {
            if (!features.Contains(typeof(T)))
                features.Add(typeof(T));

            return peer != null ? peer.AddFeature<T>() : default;
        }

        public void RemoveFeature<T>() where T : IFeature
        {
            features.Remove(typeof(T));
            peer?.RemoveFeature<T>();
        }

        public T GetFeature<T>() where T : IFeature
            => peer != null ? peer.GetFeature<T>() : default;

        public Type[] GetFeatures()
            => features.ToArray();

        private void OnConnectedHandler()
        {
            peer = new TcpPeer(0, this, Target);

            var type = peer.GetType();
            var method = type.GetMethod("AddFeature");

            for (int i = 0; i < features.Count; i++)
            {
                var generic = method.MakeGenericMethod(features[i]);

                generic.Call(peer, null);
            }

            peer.Start();

            OnConnected.Call(peer);

            log.Info($"Connected to server");
        }

        private void OnDisconnectedHandler()
        {
            OnDisconnected.Call(peer);

            peer?.Stop();
            peer = null;

            log.Info("Disconnected from server");
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