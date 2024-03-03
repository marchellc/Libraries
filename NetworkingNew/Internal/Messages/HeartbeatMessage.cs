using Common.IO.Data;

using Networking.Interfaces;

namespace Networking.Internal.Messages
{
    public struct HeartbeatMessage : INetworkMessage
    {
        public ushort Id => 1;

        public void Deserialize(DataReader reader)
        {

        }

        public void Serialize(DataWriter writer)
        {

        }
    }
}