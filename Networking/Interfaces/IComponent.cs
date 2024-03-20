using Networking.Data;

namespace Networking.Interfaces
{
    public interface IComponent
    {
        IClient Client { get; set; }
        ISender Sender { get; set; }

        NetListener Listener { get; set; }

        bool IsRunning { get; set; }

        void Start();
        void Stop();
    }
}