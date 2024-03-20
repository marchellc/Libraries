using Common.Extensions;

using System;

namespace Networking.Entities
{
    public struct NetEntityData
    {
        public readonly Type LocalType;

        public readonly ushort RemoteCode;
        public readonly ushort LocalCode;

        public NetEntityEntryData[] Entries;

        public NetEntityData(Type localType, string remoteType, NetEntityEntryData[] entries)
        {
            LocalType = localType;
            LocalCode = localType.FullName.GetShortCode();
            RemoteCode = remoteType.GetShortCode();
            Entries = entries;
        }
    }
}