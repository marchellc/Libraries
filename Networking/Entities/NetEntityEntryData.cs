namespace Networking.Entities
{
    public struct NetEntityEntryData
    {
        public readonly NetEntityEntryType Type;

        public readonly ushort ShortCode;

        public readonly object Entry;

        public readonly string Name;

        public NetEntityEntryData(NetEntityEntryType type, ushort code, object entry, string name)
        {
            Type = type;
            Name = name;
            ShortCode = code;
            Entry = entry;
        }
    }
}