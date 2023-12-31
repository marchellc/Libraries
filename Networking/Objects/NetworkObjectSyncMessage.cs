using Networking.Data;

using System;
using System.Collections.Generic;

namespace Networking.Objects
{
    public struct NetworkObjectSyncMessage : ISerialize, IDeserialize
    {
        public List<Type> syncTypes;

        public NetworkObjectSyncMessage(List<Type> syncTypes)
            => this.syncTypes = syncTypes;

        public void Deserialize(Reader reader)
        {
            syncTypes = reader.ReadList<Type>(() => reader.ReadType());
        }

        public void Serialize(Writer writer)
        {
            writer.WriteList(syncTypes, type => writer.WriteType(type));
        }
    }
}