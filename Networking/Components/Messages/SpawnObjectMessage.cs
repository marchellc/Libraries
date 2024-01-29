using Common.IO.Data;
using System;

namespace Networking.Components.Messages
{
    public struct SpawnObjectMessage : IData
    {
        public Type ObjectType;
        public ushort IdentityId;
        public byte SpawnId;

        public SpawnObjectMessage(Type objectType, ushort parentIdentityId, byte spawnId)
        {
            ObjectType = objectType;
            IdentityId = parentIdentityId;
            SpawnId = spawnId;
        }

        public void Deserialize(DataReader reader)
        {
            ObjectType = reader.ReadType();
            IdentityId = reader.ReadUShort();
            SpawnId = reader.ReadByte();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteType(ObjectType);
            writer.WriteUShort(IdentityId);
            writer.WriteByte(SpawnId);
        }
    }
}