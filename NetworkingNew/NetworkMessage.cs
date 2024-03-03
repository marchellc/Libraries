using Networking.Interfaces;

using System;

namespace Networking
{
    public struct NetworkMessage
    {
        public DateTime Time;
        public INetworkMessage Message;
    }
}