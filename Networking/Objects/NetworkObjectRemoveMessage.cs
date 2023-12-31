using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkObjectRemoveMessage : IMessage
    {
        public int objectId;

        public NetworkObjectRemoveMessage(int objectId)
            => this.objectId = objectId;

        public void Deserialize(Reader reader)
            => objectId = reader.ReadInt();

        public void Serialize(Writer writer)
            => writer.WriteInt(objectId);
    }
}