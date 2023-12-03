using Network.Interfaces.Transporting;
using Network.Interfaces.Controllers;

namespace Network.Interfaces.Features
{
    public interface IFeature
    {
        bool IsRunning { get; }

        IPeer Peer { get; }
        ITransport Transport { get; }
        IController Controller { get; }

        void Stop();
        void Start(IPeer peer);
    }
}