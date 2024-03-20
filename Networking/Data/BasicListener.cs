using Common.IO.Data;

using Networking.Enums;
using Networking.Interfaces;

namespace Networking.Data
{
    public class BasicListener<T> : IListener<T> 
        where T : IData
    {
        public NetListener Listener { get; set; }

        public IClient Client
        {
            get => Listener?.Client;
        }

        public virtual void OnRegistered() { }
        public virtual void OnUnregistered() { }

        public virtual ListenerResult Process(T message)
            => ListenerResult.Failed;
    }
}