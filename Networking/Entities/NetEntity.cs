using Common.IO.Data;

using Networking.Interfaces;
using Networking.Enums;
using Networking.Entities.Messages;

using Common.Extensions;

using System.Reflection;
using System;

namespace Networking.Entities
{
    public class NetEntity
    {
        internal ulong entityId;
        internal bool entityStatus;
        internal NetEntityData entityData;
        internal NetEntityManager entityManager;
        internal NetEntityDataStorage entityDataStorage;

        public ulong EntityId => entityId;

        public NetEntityData EntityData => entityData;
        public NetEntityManager EntityManager => entityManager;
        public NetEntityDataStorage EntityDataStorage => entityDataStorage;

        public IClient Client => entityManager.Client;

        public bool IsServer => Client.Type is ClientType.Peer;
        public bool IsClient => Client.Type is ClientType.Client;

        public bool IsActive => entityStatus;

        public virtual void OnSpawned(NetEntityRequestType netEntityRequestType = NetEntityRequestType.LocalRequest) { }
        public virtual void OnSpawnConfirmed() { }

        public virtual void OnDestroyed(NetEntityRequestType netEntityRequestType = NetEntityRequestType.LocalRequest) { }
        public virtual void OnDestroyConfirmed() { }

        public void Destroy()
        {
            if (!entityStatus)
                return;

            entityManager.DestroyEntity(this);
        }

        public T GetSync<T>(ushort code)
            => entityDataStorage.GetValue<T>(code);

        public void SetSync(ushort code, object value)
            => entityDataStorage.SetValue(code, value);

        public void Hook<T>(ushort code, Action<T, T> hook)
            => entityDataStorage.Hook<T>(code, hook);

        public void Unhook<T>(ushort code, Action<T, T> hook)
            => entityDataStorage.Unhook<T>(code, hook);

        public void Call(ushort code, params object[] args)
            => Send(new NetEntityDataMessage(IsClient ? NetEntityEntryType.ServerCode : NetEntityEntryType.ClientCode, entityId, code, args));

        public void Raise(ushort code, bool raiseLocal, params object[] args)
        {
            if (raiseLocal && entityData.Entries.TryGetFirst(en => en.ShortCode == code && en.Entry != null && en.Entry is EventInfo, out var evEntry))
                ((EventInfo)evEntry.Entry).Raise(this, args);

            Send(new NetEntityDataMessage(NetEntityEntryType.NetworkEvent, entityId, code, args));
        }

        public void Send<T>(T data) where T : IData
            => Client.Sender.SendSingular(data);

        internal void ResetEntityStatus()
        {
            entityManager = null;
            entityData = default;
            entityStatus = false;
            entityId = 0;
        }
    }
}