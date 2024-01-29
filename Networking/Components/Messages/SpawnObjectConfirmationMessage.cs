﻿using Common.IO.Data;

namespace Networking.Components.Messages
{
    public struct SpawnObjectConfirmationMessage : IData
    {
        public byte SpawnId;

        public SpawnObjectConfirmationMessage(byte spawnId)
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