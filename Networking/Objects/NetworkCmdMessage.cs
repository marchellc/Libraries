using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkCmdMessage : IMessage
    {
        public ushort typeHash;
        public ushort functionHash;

        public object[] args;

        public NetworkCmdMessage(ushort typeHash, ushort functionHash, object[] args)
        {
            this.typeHash = typeHash;
            this.functionHash = functionHash;
            this.args = args;
        }

        public void Deserialize(Reader reader)
        {
            typeHash = reader.ReadUShort();
            functionHash = reader.ReadUShort();
            args = reader.ReadAnonymousArray();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteUShort(typeHash);
            writer.WriteUShort(functionHash);
            writer.WriteAnonymousArray(args);
        }
    }
}