using Common.IO.Data;

namespace Networking.Components.Messages
{
    public struct SpawnIdentityMessage : IData
    {
        public byte SpawnId;

        public SpawnIdentityMessage(byte spawnId)
            => SpawnId = spawnId;

        public void Deserialize(DataReader reader)
        {
            SpawnId = reader.ReadByte();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteByte(SpawnId);
        }
    }
}