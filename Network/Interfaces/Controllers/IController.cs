using Network.Interfaces.Features;
using System.Net;

namespace Network.Interfaces.Controllers
{
    public interface IController 
    {
        bool IsRunning { get; }
        bool IsManual { get; set; }

        IPEndPoint Target { get; set; }

        IFeatureManager Features { get; }

        int TickRate { get; set; }

        void Start();
        void Stop();
        void Tick();
    }
}