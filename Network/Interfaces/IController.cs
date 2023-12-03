using System.Net;

namespace Network
{
    public interface IController 
    {
        bool IsRunning { get; }
        bool IsManual { get; set; }

        IPEndPoint Target { get; set; }

        int TickRate { get; set; }

        void Start();
        void Stop();
        void Tick();
    }
}