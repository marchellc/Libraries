using Network.Events;

namespace Network.Features
{
    public interface INetworkFeature
    {
        INetworkObject Controller { get; }

        bool HasPriority { get; }
        bool IsEnabled { get; }

        void OnInstalled(INetworkObject controller, INetworkEvents networkEvents);
        void OnUninstalled(INetworkObject controller, INetworkEvents networkEvents);

        void Disable();
        void Enable();

        bool Receive(object data);
    }
}
