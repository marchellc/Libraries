using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkCallRpcMessage : IMessage
    {
        public int objectId;
        public int rpcId;

        public object[] args;

        public NetworkCallRpcMessage(int objectId, int rpcId, object[] args)
        {
            this.objectId = objectId;
            this.rpcId = rpcId;
            this.args = args;
        }

        public void Deserialize(Reader reader)
        {
            objectId = reader.ReadInt();
            rpcId = reader.ReadInt();

            args = reader.ReadObjects();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteInt(objectId);
            writer.WriteInt(rpcId);

            writer.WriteObjects(args);
        }
    }
}