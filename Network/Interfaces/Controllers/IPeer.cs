using Network.Interfaces.Transporting;

namespace Network.Interfaces.Controllers
{
    public interface IPeer : IController
    {
        int Id { get; }

        bool IsConnected { get; }

        ITransport Transport { get; }

        void Disconnect();
    }
}