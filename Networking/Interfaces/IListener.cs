using Common.IO.Data;

using Networking.Data;
using Networking.Enums;

namespace Networking.Interfaces
{
    public interface IListener<T> where T : IData
    {
        NetListener Listener { get; set; }

        ListenerResult Process(T message);

        void OnRegistered();
        void OnUnregistered();
    }
}