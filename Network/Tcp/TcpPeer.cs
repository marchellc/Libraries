using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;

using System;
using System.Net;

using Network.Features;

namespace Network.Tcp
{
    public class TcpPeer : IPeer
    {
        private bool isConnected;

        private IController controller;

        private TcpTransport transport;
        private IPEndPoint target;
        private PeerFeatureManager features;

        public int Id { get; }
        public int TickRate { get; set; }

        public bool IsConnected => isConnected;
        public bool IsRunning => isConnected;

        public bool IsManual { get; set; } = false;

        public ITransport Transport => transport;
        public IFeatureManager Features => features;

        public IPEndPoint Target { get => target; set => throw new InvalidOperationException(); }

        public TcpPeer(int connectionId, IController controller, IPEndPoint endPoint)
        {
            this.Id = connectionId;
            this.controller = controller;
            this.target = endPoint;
            this.transport = new TcpTransport(this, controller);
            this.features = new PeerFeatureManager(this);
        }

        public void Start()
        {
            if (isConnected)
                return;

            isConnected = true;

            transport.Initialize();
        }

        public void Stop()
        {
            features.Disable();
            features = null;

            isConnected = false;

            features = null;

            target = null;

            transport.Shutdown();
            transport = null;
        }

        public void Disconnect()
        {
            if (controller is IClient client)
                client.Stop();
            else if (controller is IServer server)
                server.Disconnect(Id);
            else
                throw new InvalidOperationException($"Unrecognized parent controller type!");
        }

        public void Tick() { }
    }
}