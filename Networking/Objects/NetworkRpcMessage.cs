using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkRpcMessage : IMessage
    {
        public int objectId;
        public ushort functionHash;

        public object[] args;

        public NetworkRpcMessage(int objectId, ushort functionHash, object[] args)
        {
            this.objectId = objectId;
            this.functionHash = functionHash;
            this.args = args;
        }

        public void Deserialize(Reader reader)
        {
            objectId = reader.ReadInt();
            functionHash = reader.ReadUShort();
            args = reader.ReadAnonymousArray();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteInt(objectId);
            writer.WriteUShort(functionHash);
            writer.WriteAnonymousArray(args);
        }
    }
}