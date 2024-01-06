using Networking.Data;

using System.Collections.Generic;

namespace Networking.Objects
{
    public struct NetworkHashSyncMessage : IMessage
    {
        public Dictionary<string, ushort> hashes;

        public NetworkHashSyncMessage(Dictionary<string, ushort> hashes)
            => this.hashes = hashes;

        public void Deserialize(Reader reader)
            => hashes = reader.ReadDictionary<string, ushort>();

        public void Serialize(Writer writer)
            => writer.WriteDictionary(hashes);
    }
}