using Common.Logging;
using Network.Events;

namespace Network.Features
{
    public class NetworkFeatureBase : INetworkFeature
    {
        private INetworkObject _controller;
        private bool _enabled;

        public INetworkObject Controller => _controller;

        public LogOutput Log { get; }

        public virtual bool HasPriority { get; }
        public virtual bool IsEnabled => _enabled;

        public virtual void OnEnabled() { }
        public virtual void OnDisabled() { }

        public virtual void Install(INetworkEvents networkEvents) { }
        public virtual void Uninstall(INetworkEvents networkEvents) { }

        public NetworkFeatureBase()
        {
            Log = new LogOutput(GetType().Name);
            Log.Setup();
        }

        public void Disable()
        {
            if (!_enabled)
                return;

            _enabled = false;
            OnDisabled();
        }

        public void Enable()
        {
            if (_enabled)
                return;

            _enabled = true;
            OnEnabled();
        }

        public void OnInstalled(INetworkObject controller, INetworkEvents networkEvents)
        {
            _controller = controller;
            Install(networkEvents);
        }

        public void OnUninstalled(INetworkObject controller, INetworkEvents networkEvents)
        {
            Uninstall(networkEvents);
            _controller = null;
        }

        public bool Receive(object data)
            => false;
    }
}
