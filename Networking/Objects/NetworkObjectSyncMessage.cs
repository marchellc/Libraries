using Networking.Data;

using System;
using System.Collections.Generic;

namespace Networking.Objects
{
    public struct NetworkObjectSyncMessage : ISerialize, IDeserialize
    {
        public Dictionary<short, Type> syncTypes;

        public NetworkObjectSyncMessage(Dictionary<short, Type> syncTypes)
        {
            this.syncTypes = syncTypes;
        }

        public void Deserialize(Reader reader)
        {
            syncTypes = reader.ReadDictionary<short, Type>();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteDictionary(syncTypes);
        }
    }
}