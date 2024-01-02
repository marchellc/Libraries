using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkObjectRemoveMessage : IMessage
    {
        public ushort typeHash;

        public NetworkObjectRemoveMessage(ushort typeHash)
            => this.typeHash = typeHash;

        public void Deserialize(Reader reader)
            => typeHash = reader.ReadUShort();

        public void Serialize(Writer writer)
            => writer.WriteUShort(typeHash);
    }
}