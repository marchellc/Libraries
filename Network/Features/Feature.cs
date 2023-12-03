using Common.Logging;

using Network.Interfaces.Controllers;
using Network.Interfaces.Features;
using Network.Interfaces.Transporting;

using System;

namespace Network.Features
{
    public class Feature : IFeature
    {
        private bool isRunning;

        private ITransport transport;
        private IPeer peer;
        private IController controller;

        public bool IsRunning => isRunning;

        public IPeer Peer => peer;
        public ITransport Transport => transport;
        public IController Controller => controller;

        public LogOutput Log;

        public virtual void OnStarted() { }
        public virtual void OnStopped() { }

        public void Start(IPeer peer)
        {
            if (isRunning)
            {
                Log.Warn($"Already running!");
                return;
            }

            this.isRunning = true;
            
            Log = new LogOutput($"{GetType().Name} :: {peer.Id}").Setup();

            try
            {
                this.peer = peer;
                this.transport = peer.Transport;
                this.controller = peer.Transport.Controller;;

                OnStarted();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start feature '{GetType().FullName}':\n{ex}");
            }
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            OnStopped();

            this.peer = null;
            this.transport = null;
            this.controller = null;
            this.isRunning = false;
        }
    }
}