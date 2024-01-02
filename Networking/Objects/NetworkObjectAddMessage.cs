using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkObjectAddMessage : IMessage
    {
        public ushort typeHash;

        public NetworkObjectAddMessage(ushort typeHash)
            => this.typeHash = typeHash;

        public void Deserialize(Reader reader)
            => typeHash = reader.ReadUShort();

        public void Serialize(Writer writer)
            => writer.WriteUShort(typeHash);
    }
}