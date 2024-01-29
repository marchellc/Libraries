using System;

namespace Networking.Components
{
    [Flags]
    public enum NetworkPermissions : byte
    {
        None = 0,

        SpawnObjects = 2,
        SpawnVars = 4,
        SpawnIdentity = 8,

        DestroyObjects = 16,
        DestroyVars = 32,
        DestroyIdentity = 64,

        SyncVars = 128,

        UpdatePermissions = 255
    }
}