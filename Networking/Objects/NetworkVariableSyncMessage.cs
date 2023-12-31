using Common.Extensions;

using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkVariableSyncMessage : IMessage
    {
        public int objectId;

        public ushort hash;

        public IMessage msg;

        public NetworkVariableSyncMessage(int objectId, ushort hash, IMessage msg)
        {
            this.objectId = objectId;
            this.hash = hash;
            this.msg = msg;
        }

        public void Deserialize(Reader reader)
        {
            objectId = reader.ReadInt();
            hash = reader.ReadUShort();

            msg = reader.ReadType().Construct() as IMessage;
            msg.Deserialize(reader);
        }

        public void Serialize(Writer writer)
        {
            writer.WriteInt(objectId);
            writer.WriteUShort(hash);

            msg.Serialize(writer);
        }
    }
}