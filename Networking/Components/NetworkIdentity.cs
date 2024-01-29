using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using System;

namespace Networking.Components
{
    public class NetworkIdentity
    {
        private LockedList<NetworkObject> networkObjects;

        public static LogOutput Log { get; } = new LogOutput("Network Identity").Setup();

        public NetworkParent Parent { get; internal set; }

        public ushort Id { get; internal set; }

        public bool IsActive { get; internal set; }

        public bool IsServer
        {
            get => Parent.Network.IsServer;
        }

        public bool IsClient
        {
            get => Parent.Network.IsClient;
        }

        public NetworkPermissions NetworkPermissions
        {
            get => Parent.NetworkPermissions;
        }

        public void AddObject(NetworkObject networkObject)
        {
            if (networkObjects.Count >= ushort.MaxValue)
                throw new InvalidOperationException($"There are too many objects in this identity.");

            networkObjects.Add(networkObject);

            networkObject.Identity = this;
            networkObject.Index = (ushort)networkObjects.IndexOf(networkObject);
            networkObject.IsActive = true;

            if (networkObject is NetworkComponent networkComponent)
            {
                networkComponent.OnStart();
                CodeUtils.WhileTrue(() => networkObject.IsActive, networkComponent.OnUpdate, 10);
            }
        }

        public void RemoveObject(NetworkObject networkObject, NetworkRequestType requestType)
        {
            if (!networkObjects.Contains(networkObject))
                throw new InvalidOperationException($"This object does not belong to this identity.");

            Log.Verbose($"Removing object {networkObject.GetType().FullName}: {requestType}");

            if (!networkObjects.Remove(networkObject))
            {
                Log.Warn($"Failed to remove network object {networkObject.GetType().FullName}!");
                return;
            }

            DestroyObject(networkObject, requestType);

            Log.Verbose($"Removed object");
        }

        public void DestroyObject(NetworkObject networkObject, NetworkRequestType requestType)
        {
            Log.Verbose($"Destroying object {networkObject.GetType().FullName}: {requestType}");

            networkObject.IsActive = false;
            networkObject.OnDestroy();

            if (networkObject is NetworkComponent networkComponent)
                networkComponent.InternalDestroy();

            networkObject.Identity = null;
            networkObject.Index = 0;

            if (requestType != NetworkRequestType.Remote)
                Parent.DestroyObject(this, networkObject, requestType);
        }

        public NetworkObject GetObject(ushort objectIndex)
        {
            if (objectIndex >= networkObjects.Count)
                throw new ArgumentOutOfRangeException(nameof(objectIndex));

            return networkObjects[objectIndex];
        }

        public bool HasPermission(NetworkPermissions networkPermissions)
            => (NetworkPermissions & networkPermissions) != 0;

        internal void SetActive(ushort id, NetworkParent parent)
        {
            if (IsActive)
                throw new InvalidOperationException($"This identity is already active!");

            IsActive = true;

            Parent = parent;
            Id = id;

            networkObjects ??= new LockedList<NetworkObject>(ushort.MaxValue);
        }

        internal void StopActive()
        {
            if (!IsActive)
                throw new InvalidOperationException($"This identity is not active!");

            foreach (var netObj in networkObjects)
                netObj.OnDestroy();

            networkObjects.Clear();

            Parent = null;

            Id = 0;

            IsActive = false;
        }
    }
}