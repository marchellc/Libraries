using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkObjectAddMessage : ISerialize, IDeserialize
    {
        public short typeId;

        public NetworkObjectAddMessage(short typeId)
            => this.typeId = typeId;

        public void Deserialize(Reader reader)
            => typeId = reader.ReadShort();

        public void Serialize(Writer writer)
            => writer.WriteShort(typeId);
    }
}