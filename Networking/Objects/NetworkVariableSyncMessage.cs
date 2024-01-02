using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkVariableSyncMessage : IMessage
    {
        public ushort typeHash;
        public ushort hash;

        public IMessage msg;

        public NetworkVariableSyncMessage(ushort typeHash, ushort hash, IMessage msg)
        {
            this.typeHash = typeHash;
            this.hash = hash;
            this.msg = msg;
        }

        public void Deserialize(Reader reader)
        {
            typeHash = reader.ReadUShort();
            hash = reader.ReadUShort();
            msg = reader.ReadAnonymous() as IMessage;
        }

        public void Serialize(Writer writer)
        {
            writer.WriteUShort(typeHash);
            writer.WriteUShort(hash);
            writer.WriteAnonymous(msg);
        }
    }
}