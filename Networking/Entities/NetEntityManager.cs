using Common.Extensions;
using Common.IO.Collections;

using Networking.Entities.Messages;

using System;
using System.Collections.Generic;

namespace Networking.Entities
{
    public class NetEntityManager : NetComponent
    {
        private readonly LockedList<NetEntityData> entityRegistry = new LockedList<NetEntityData>();
        private readonly LockedList<NetEntity> spawnedEntities = new LockedList<NetEntity>();

        private readonly LockedDictionary<ulong, Action<object>> confirmations = new LockedDictionary<ulong, Action<object>>();

        private readonly NetEntityDataMessage.NetEntityDataMessageListener netEntityDataMessageListener = new NetEntityDataMessage.NetEntityDataMessageListener();
        private readonly NetEntitySpawnMessage.NetEntitySpawnMessageListener netEntitySpawnMessageListener = new NetEntitySpawnMessage.NetEntitySpawnMessageListener();
        private readonly NetEntityDestroyMessage.NetEntityDestroyMessageListener netEntityDestroyMessageListener = new NetEntityDestroyMessage.NetEntityDestroyMessageListener();

        private ulong entityId = 0;

        public IEnumerable<NetEntityData> EntityRegistry => entityRegistry;
        public IEnumerable<NetEntity> EntityList => spawnedEntities;

        public int KnownEntities => entityRegistry.Count;
        public int SpawnedEntities => spawnedEntities.Count;

        public override void Start()
        {
            base.Start();

            NetEntityUtils.ReloadRegistry(entityRegistry);

            Listener.Listen(netEntityDataMessageListener);
            Listener.Listen(netEntitySpawnMessageListener);
            Listener.Listen(netEntityDestroyMessageListener);
        }

        public override void Stop()
        {
            base.Stop();

            Listener.Clear(netEntityDataMessageListener);
            Listener.Clear(netEntitySpawnMessageListener);
            Listener.Clear(netEntityDestroyMessageListener);

            DestroyEntities();
        }

        public bool TryGetRegistry(ushort code, out NetEntityData netEntityData)
            => entityRegistry.TryGetFirst(val => val.LocalCode == code, out netEntityData);

        public bool TryGetRegistry(Type type, out NetEntityData netEntityData)
            => entityRegistry.TryGetFirst(val => val.LocalCode == $"{type.Namespace}.{type.Name}".GetShortCode(), out netEntityData);

        public bool TrySpawnEntity<T>(Action<T> onEntitySpawnConfirmed, out T entity) where T : NetEntity
        {
            if (!TryGetRegistry(typeof(T), out var registryData))
            {
                Log.Warn($"Tried to spawn an unregistered entity '{typeof(T).FullName}'");

                entity = default;
                return false;
            }

            try
            {
                entity = typeof(T).Construct<T>();

                entity.entityManager = this;
                entity.entityId = ++entityId;
                entity.entityData = registryData;
                entity.entityDataStorage = new NetEntityDataStorage(entity, registryData);

                spawnedEntities.Add(entity);

                entity.OnSpawned(NetEntityRequestType.LocalRequest);

                if (onEntitySpawnConfirmed != null)
                    confirmations[entity.entityId] = value => onEntitySpawnConfirmed.Call((T)value);

                Log.Verbose($"Spawned a new entity {entity.entityId} ({typeof(T).FullName})");

                Send(new NetEntitySpawnMessage(registryData.RemoteCode));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to spawn a new entity!\n{ex}");

                entity = default;
                return false;
            }
        }

        public T SpawnEntity<T>(Action<T> onSpawnConfirmed = null) where T : NetEntity
        {
            if (!TrySpawnEntity<T>(onSpawnConfirmed, out var entity))
                return default;

            return entity;
        }

        public T GetEntity<T>(ulong entityId) where T : NetEntity
        {
            if (!TryGetEntity<T>(entityId, out var entity))
                return default;

            return entity;
        }

        public bool TryGetEntity<T>(ulong entityId, out T foundEntity) where T : NetEntity
        {
            if (!spawnedEntities.TryGetFirst(e => e.entityId == entityId && e is T, out var entity))
            {
                foundEntity = default;
                return false;
            }

            foundEntity = (T)entity;
            return true;
        }

        public bool DestroyEntity(ulong entityId)
        {
            if (!TryGetEntity<NetEntity>(entityId, out var entity))
                return false;

            DestroyEntity(entity);
            return true;
        }

        public void DestroyEntity(NetEntity entity)
            => InternalDestroyEntity(entity);

        public void DestroyEntities()
        {
            foreach (var entity in spawnedEntities)
                InternalDestroyEntity(entity, false);

            spawnedEntities.Clear();
        }

        internal void ProcessEntityDestroy(NetEntity entity, bool isConfirmation)
        {
            if (!entity.IsActive && !isConfirmation)
            {
                Log.Warn($"Tried to destroy an inactive entity!");
                return;
            }

            try
            {
                if (isConfirmation)
                {
                    Log.Verbose($"Destruction of entity '{entity.entityId}' has been confirmed");

                    entity.OnDestroyConfirmed();
                    entity.ResetEntityStatus();

                    spawnedEntities.Remove(entity);
                }
                else
                {
                    Log.Verbose($"Destruction of entity '{entity.entityId}' has been requested.");
                    InternalDestroyEntity(entity, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to process entity destruction!\n{ex}");
            }
        }

        internal void ProcessEntitySpawn(ushort entityCode, ulong entityId, bool isConfirmation)
        {
            if (isConfirmation)
            {
                if (!TryGetEntity<NetEntity>(entityId, out var entity))
                {
                    Log.Warn($"Received spawn confirmation for an unknown entity: {entityId}");
                    return;
                }

                try
                {
                    entity.entityStatus = true;
                    entity.OnSpawnConfirmed();
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to process spawn confirmation!\n{ex}");
                }
            }
            else
            {
                if (!entityRegistry.TryGetFirst(val => val.LocalCode == entityCode, out var targetRegistry))
                {
                    Log.Warn($"Failed to process entity spawn: unknown entity code ({entityCode})");
                    return;
                }

                try
                {
                    var entity = targetRegistry.LocalType.Construct<NetEntity>();

                    entity.entityManager = this;
                    entity.entityData = targetRegistry;
                    entity.entityId = ++this.entityId;
                    entity.entityStatus = true;
                    entity.entityDataStorage = new NetEntityDataStorage(entity, targetRegistry);

                    entity.OnSpawned(NetEntityRequestType.RemoteRequest);
                    entity.OnSpawnConfirmed();

                    spawnedEntities.Add(entity);

                    Send(new NetEntitySpawnMessage(entity.entityId));
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to spawn a new entity '{targetRegistry.LocalType.FullName}'!\n{ex}");
                }
            }
        }

        private void InternalDestroyEntity(NetEntity entity, bool isRemote = false)
        {
            try
            {
                if (entity.IsActive)
                {
                    if (isRemote)
                    {
                        entity.OnDestroyed(NetEntityRequestType.RemoteRequest);
                        Send(new NetEntityDestroyMessage(NetEntityMessageType.Confirmation, entity.entityId));
                        entity.ResetEntityStatus();
                    }
                    else
                    {
                        entity.OnDestroyed(NetEntityRequestType.LocalRequest);
                        Send(new NetEntityDestroyMessage(NetEntityMessageType.Request, entity.entityId));
                        entity.entityStatus = false;
                    }
                }
                else
                {
                    Log.Warn($"Tried to destroy an inactive entity! ({entity.entityId})");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to destroy entity '{entity}' ({entity.entityId})!\n{ex}");
            }
        }
    }
}