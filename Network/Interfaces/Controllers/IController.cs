using Network.Interfaces.Features;

using System;
using System.Net;

namespace Network.Interfaces.Controllers
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

        void RemoveFeature<T>() where T : IFeature;

        T AddFeature<T>() where T : IFeature, new();
        T GetFeature<T>() where T : IFeature;

        Type[] GetFeatures();
    }
}