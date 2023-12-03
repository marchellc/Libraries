namespace Network.Features
{
    public class Feature : IFeature
    {
        private ITransport transport;
        private IPeer peer;
        private IController controller;

        public ITransport Transport => transport;
        public IPeer Peer => peer;
        public IController Controller => controller;

        public virtual void OnStarted() { }
        public virtual void OnStopped() { }

        public void Start(IPeer peer)
        {
            this.peer = peer;
            this.transport = peer.Transport;
            this.controller = peer.Transport.Controller;

            OnStarted();
        }

        public void Stop()
        {
            OnStopped();

            this.peer = null;
            this.transport = null;
            this.controller = null;
        }
    }
}