using Common.Extensions;
using Common.Utilities;

using Common.IO.Collections;

using Networking.Features;
using Networking.Components.Messages;
using Networking.Components.Calls;

using System;

namespace Networking.Components
{
    public class NetworkParent : NetworkFeature
    {
        public const NetworkPermissions DefaultServerPermissions = NetworkPermissions.DestroyIdentity | NetworkPermissions.DestroyObjects | NetworkPermissions.DestroyVars |
                                                                   NetworkPermissions.SpawnIdentity | NetworkPermissions.SpawnObjects | NetworkPermissions.SpawnVars |
                                                                   NetworkPermissions.SyncVars | NetworkPermissions.UpdatePermissions;

        public const NetworkPermissions DefaultClientPermisssions = NetworkPermissions.SyncVars;

        private LockedList<NetworkIdentity> networkIdentities;

        private LockedDictionary<byte, Tuple<NetworkObject, Action<NetworkObject>>> awaitingObjectSpawns;
        private LockedDictionary<byte, Tuple<NetworkIdentity, Action<NetworkIdentity>>> awaitingIdentitySpawns;

        private NetworkPermissions networkPerms;
        private NetworkIdentity defaultIdentity;

        private bool isReady;

        public event Action<NetworkIdentity, NetworkRequestType> OnIdentitySpawned;
        public event Action<NetworkIdentity, NetworkObject, NetworkRequestType> OnObjectSpawned;

        public NetworkPermissions NetworkPermissions
        {
            get => networkPerms;
            set
            {
                if (!ValidatePermissions(NetworkRequestType.Current, NetworkPermissions.UpdatePermissions))
                    throw new InvalidOperationException($"No permissions for updating permissions.");

                networkPerms = value;
            }
        }

        public NetworkIdentity Identity
        {
            get => defaultIdentity;
        }

        public bool IsReady
        {
            get => isReady;
        }

        public override void Start()
        {
            networkIdentities ??= new LockedList<NetworkIdentity>();

            awaitingObjectSpawns ??= new LockedDictionary<byte, Tuple<NetworkObject, Action<NetworkObject>>>(byte.MaxValue);
            awaitingIdentitySpawns ??= new LockedDictionary<byte, Tuple<NetworkIdentity, Action<NetworkIdentity>>>(byte.MaxValue);

            if (Network.IsClient)
                networkPerms = DefaultClientPermisssions;
            else
                networkPerms = DefaultServerPermissions;

            Listen<SpawnObjectConfirmationMessage>(OnObjectSpawnConfirmation);
            Listen<SpawnObjectMessage>(OnSpawnObject);

            Listen<SpawnIdentityConfirmationMessage>(OnSpawnIdentityConfirmation);
            Listen<SpawnIdentityMessage>(OnIdentitySpawn);

            Listen<DestroyObjectMessage>(OnDestroyObject);

            Listen<RemoteCallMessage>(OnRemoteCall);

            Listen<SynchronizeVariableMessage>(OnSynchronizeVariable);

            SpawnIdentity(NetworkRequestType.Current, identity =>
            {
                defaultIdentity = identity;
                isReady = true;
            });

            Log.Info($"Initialized networking.");
        }

        public override void Stop()
        {
            foreach (var networkIdentity in networkIdentities)
                networkIdentity.StopActive();

            networkIdentities.Clear();

            awaitingObjectSpawns.Clear();
            awaitingIdentitySpawns.Clear();

            Log.Info($"Stopped networking.");
        }

        public NetworkIdentity FindIdentity(uint networkIdentityId)
        {
            foreach (var networkIdentity in networkIdentities)
            {
                if (networkIdentity.IsActive && networkIdentity.Id == networkIdentityId)
                    return networkIdentity;
            }

            throw new Exception($"Network Identity of ID {networkIdentityId} does not exist.");
        }

        public void SpawnIdentity(NetworkRequestType requestType, Action<NetworkIdentity> spawnCallback)
        {
            if (!ValidatePermissions(requestType, NetworkPermissions.SpawnIdentity))
                throw new InvalidOperationException($"No permissions for spawning an identity!");

            var nextIdentityId = (ushort)(networkIdentities.Count + 1);
            var networkIdentity = new NetworkIdentity();

            networkIdentity.SetActive(nextIdentityId, this);

            networkIdentities.Add(networkIdentity);

            if (requestType != NetworkRequestType.Remote && isReady)
            {
                var networkIdentitySpawnId = Generator.Instance.GetByte();

                while (awaitingObjectSpawns.ContainsKey(networkIdentitySpawnId))
                    networkIdentitySpawnId = Generator.Instance.GetByte();

                AwaitIdentitySpawn(networkIdentitySpawnId, networkIdentity, spawnCallback);
                Network.Send(new SpawnIdentityMessage(networkIdentitySpawnId));
            }
            else
            {
                spawnCallback.Call(networkIdentity, null, Log.Error);
                OnIdentitySpawned.Call(networkIdentity, requestType, null, Log.Error);
            }
        }

        public void SpawnObject<TObject>(NetworkIdentity parentIdentity, NetworkRequestType requestType, Action<TObject> spawnCallback) where TObject : NetworkObject
        {
            if (parentIdentity is null)
                throw new ArgumentNullException(nameof(parentIdentity));

            if (!ValidatePermissions(requestType, NetworkPermissions.SpawnObjects))
                throw new InvalidOperationException($"No permissions for spawning objects.");

            var networkObject = typeof(TObject).Construct<TObject>();

            parentIdentity.AddObject(networkObject);

            if (requestType != NetworkRequestType.Remote)
            {
                var networkObjectSpawnId = Generator.Instance.GetByte();

                while (awaitingObjectSpawns.ContainsKey(networkObjectSpawnId))
                    networkObjectSpawnId = Generator.Instance.GetByte();

                AwaitObjectSpawn(networkObjectSpawnId, networkObject, spawnCallback);

                Network.Send(new SpawnObjectMessage(typeof(TObject), parentIdentity.Id, networkObjectSpawnId));
            }
            else
            {
                spawnCallback.Call(networkObject, null, Log.Error);
                OnObjectSpawned.Call(parentIdentity, networkObject, requestType, null, Log.Error);
            }
        }

        public void DestroyObject(NetworkIdentity parentIdentity, NetworkObject networkObject, NetworkRequestType requestType)
        {
            if (parentIdentity is null)
                throw new ArgumentNullException(nameof(parentIdentity));

            if (networkObject is null)
                throw new ArgumentNullException(nameof(networkObject));

            if (!ValidatePermissions(requestType, NetworkPermissions.DestroyObjects))
                throw new InvalidOperationException($"No permissions for destroying objects.");

            if (requestType != NetworkRequestType.Current)
                return;

            Network.Send(new DestroyObjectMessage(parentIdentity.Id, networkObject.Index));
        }

        public bool HasPermission(NetworkPermissions networkPermissions)
            => (NetworkPermissions & networkPermissions) != 0;

        public bool ValidatePermissions(NetworkRequestType requestType, NetworkPermissions networkPermissions)
            => !isReady || requestType is NetworkRequestType.Remote || HasPermission(networkPermissions);

        private void AwaitObjectSpawn<TObject>(byte spawnId, TObject networkObject, Action<TObject> spawnCallback) where TObject : NetworkObject
            => awaitingObjectSpawns[spawnId] = new Tuple<NetworkObject, Action<NetworkObject>>(networkObject, obj => spawnCallback((TObject)obj));

        private void AwaitIdentitySpawn(byte spawnId, NetworkIdentity networkIdentity, Action<NetworkIdentity> spawnCallback)
            => awaitingIdentitySpawns[spawnId] = new Tuple<NetworkIdentity, Action<NetworkIdentity>>(networkIdentity, spawnCallback);

        private void OnObjectSpawnConfirmation(SpawnObjectConfirmationMessage spawnObjectConfirmationMessage)
        {
            if (!awaitingObjectSpawns.TryGetValue(spawnObjectConfirmationMessage.SpawnId, out var awaiter))
            {
                Log.Warn($"Received object spawn confirmation for an unknown spawn ID {spawnObjectConfirmationMessage.SpawnId}");
                return;
            }

            awaitingObjectSpawns.Remove(spawnObjectConfirmationMessage.SpawnId);

            awaiter.Item2(awaiter.Item1);

            OnObjectSpawned.Call(awaiter.Item1.Identity, awaiter.Item1, NetworkRequestType.Current, null, Log.Error);
        }

        private void OnSpawnIdentityConfirmation(SpawnIdentityConfirmationMessage spawnIdentityConfirmationMessage)
        {
            if (!awaitingIdentitySpawns.TryGetValue(spawnIdentityConfirmationMessage.SpawnId, out var awaiter))
            {
                Log.Warn($"Received identity spawn confirmation for an unknown spawn ID {spawnIdentityConfirmationMessage.SpawnId}");
                return;
            }

            awaitingIdentitySpawns.Remove(spawnIdentityConfirmationMessage.SpawnId);

            awaiter.Item2(awaiter.Item1);

            OnIdentitySpawned.Call(awaiter.Item1, NetworkRequestType.Remote, null, Log.Error);
        }

        private void OnSpawnObject(SpawnObjectMessage spawnObjectMessage)
        {
            if (!networkIdentities.TryGetFirst(identity => identity.Id == spawnObjectMessage.IdentityId, out var networkIdentity))
            {
                Log.Warn($"Received spawn object message with an unknown identity ID {spawnObjectMessage.IdentityId}");
                return;
            }

            var networkObject = spawnObjectMessage.ObjectType.Construct<NetworkObject>();

            networkIdentity.AddObject(networkObject);

            Network.Send(new SpawnObjectConfirmationMessage(spawnObjectMessage.SpawnId));

            OnObjectSpawned.Call(networkIdentity, networkObject, NetworkRequestType.Remote, null, Log.Error);
        }

        private void OnIdentitySpawn(SpawnIdentityMessage spawnIdentityMessage)
        {
            SpawnIdentity(NetworkRequestType.Remote, identity =>
            {
                Network.Send(new SpawnIdentityConfirmationMessage(spawnIdentityMessage.SpawnId));

                if (defaultIdentity is null)
                {
                    defaultIdentity = identity;
                    isReady = true;
                }
            });
        }

        private void OnDestroyObject(DestroyObjectMessage destroyObjectMessage)
        {
            if (!networkIdentities.TryGetFirst(identity => identity.Id == destroyObjectMessage.ParentIdentityId, out var networkIdentity))
            {
                Log.Warn($"Received object destroy message with an unknown identity ID {destroyObjectMessage.ParentIdentityId}");
                return;
            }

            var networkObject = networkIdentity.GetObject(destroyObjectMessage.TargetObject);

            if (networkObject is null)
            {
                Log.Warn($"Received object destroy message with an invalid object ID {destroyObjectMessage.TargetObject}");
                return;
            }

            networkIdentity.RemoveObject(networkObject, NetworkRequestType.Remote);
        }

        private void OnRemoteCall(RemoteCallMessage remoteCallMessage)
        {
            if (!networkIdentities.TryGetFirst(identity => identity.Id == remoteCallMessage.TargetIdentity, out var networkIdentity))
            {
                Log.Warn($"Received remote call message with an unknown identity ID {remoteCallMessage.TargetIdentity}");
                return;
            }

            var networkObject = networkIdentity.GetObject(remoteCallMessage.TargetObject);

            if (networkObject is null)
            {
                Log.Warn($"Received remote call message with an invalid object ID {remoteCallMessage.TargetObject}");
                return;
            }

            if (networkObject is NetworkComponent networkComponent)
            {
                var syncEvent = networkComponent.GetEvent(remoteCallMessage.TargetMethod);

                if (syncEvent != null)
                {
                    syncEvent.Raise(networkObject, remoteCallMessage.TargetArgs);
                    return;
                }
            }

            var remoteCall = NetworkObject.GetRemoteCall(remoteCallMessage.TargetMethod);

            if (remoteCall is null)
            {
                Log.Warn($"Received remote call message with an unknown method ID {remoteCallMessage.TargetMethod}");
                return;
            }

            if (!remoteCall.ValidateCall(Network.IsClient))
            {
                Log.Warn($"Received an invalid remote call message for this client type ({remoteCall.Type}).");
                return;
            }

            if (!remoteCall.ValidateArguments(remoteCallMessage.TargetArgs, out var expectedType, out var receivedType, out var argumentPos))
            {
                Log.Warn($"Received remote call message with invalid method arguments. (Expected: {expectedType?.FullName ?? "null"}; Received: {receivedType?.FullName ?? "null"}; Position: {argumentPos})");
                return;
            }

            remoteCall.Execute(networkObject, remoteCallMessage.TargetArgs);
        }

        private void OnSynchronizeVariable(SynchronizeVariableMessage synchronizeVariableMessage)
        {
            if (!networkIdentities.TryGetFirst(identity => identity.Id == synchronizeVariableMessage.TargetIdentity, out var networkIdentity))
            {
                Log.Warn($"Received synchronize variable message with an unknown identity ID {synchronizeVariableMessage.TargetIdentity}");
                return;
            }

            var networkObject = networkIdentity.GetObject(synchronizeVariableMessage.TargetObject);

            if (networkObject is null)
            {
                Log.Warn($"Received synchronize variable message with an invalid object ID {synchronizeVariableMessage.TargetObject}");
                return;
            }

            if (networkObject is not NetworkComponent networkComponent)
            {
                Log.Warn($"Received synchronize variable message for a non-component object");
                return;
            }

            if (!networkComponent.InternalSetSyncVar(synchronizeVariableMessage.TargetVariable, synchronizeVariableMessage.TargetValue))
                Log.Warn($"Received synchronize variable message for an unknown variable ID {synchronizeVariableMessage.TargetVariable}");
        }
    }
}