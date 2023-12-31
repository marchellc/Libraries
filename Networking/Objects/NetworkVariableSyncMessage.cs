using Networking.Data;

namespace Networking.Objects
{
    public struct NetworkVariableSyncMessage : IMessage
    {
        public int variableId;
        public byte[] data;

        public NetworkVariableSyncMessage(int varId, byte[] data)
        {
            this.variableId = varId;
            this.data = data;
        }

        public void Deserialize(Reader reader)
        {
            this.variableId = reader.ReadInt();
            this.data = reader.ReadBytes();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteInt(variableId);
            writer.WriteBytes(data);
        }
    }
}