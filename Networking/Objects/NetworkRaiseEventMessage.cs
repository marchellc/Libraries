using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkRaiseEventMessage : IMessage
    {
        public ushort typeHash;
        public ushort eventHash;

        public object[] args;

        public NetworkRaiseEventMessage(ushort typeHash, ushort eventHash, object[] args)
        {
            this.typeHash = typeHash;
            this.eventHash = eventHash;
            this.args = args;
        }

        public void Deserialize(Reader reader)
        {
            typeHash = reader.ReadUShort();
            eventHash = reader.ReadUShort();

            args = reader.ReadAnonymousArray();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteUShort(typeHash);
            writer.WriteUShort(eventHash);

            writer.WriteAnonymousArray(args);
        }
    }
}