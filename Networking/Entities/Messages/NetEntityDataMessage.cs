using Common.Extensions;
using Common.IO.Data;

using Networking.Enums;

using System;
using System.Reflection;

namespace Networking.Entities.Messages
{
    public struct NetEntityDataMessage : IData
    {
        public ulong Id;
        public ushort Code;
        public object[] Args; 
        public NetEntityEntryType Type;

        public NetEntityDataMessage(NetEntityEntryType entryType, ulong entityId, ushort targetCode, object[] args)
        {
            Type = entryType;
            Id = entityId;
            Code = targetCode;
            Args = args;
        }

        public void Deserialize(DataReader reader)
        {
            Id = reader.ReadCompressedULong();
            Args = reader.ReadArray<object>();
            Type = (NetEntityEntryType)reader.ReadByte();
            Code = reader.ReadUShort();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteCompressedULong(Id);
            writer.WriteEnumerable(Args);
            writer.WriteByte((byte)Type);
            writer.WriteUShort(Code);
        }

        public class NetEntityDataMessageListener : NetEntityDataListener<NetEntityDataMessage>
        {
            public override ListenerResult Process(NetEntityDataMessage message)
            {
                try
                {
                    if (!EntityManager.TryGetEntity<NetEntity>(message.Id, out var entity))
                    {
                        Log.Warn($"Received entity data for an unknown entity: {message.Id}");
                        return ListenerResult.Success;
                    }

                    if (!entity.entityData.Entries.TryGetFirst(data => data.ShortCode == message.Code && data.Type == message.Type, out var netEntityEntryData))
                    {
                        Log.Warn($"Received entity data for an unknown entity entry: {message.Id} {message.Code}");
                        return ListenerResult.Success;
                    }

                    if (netEntityEntryData.Entry is null)
                    {
                        Log.Warn($"Entry of entity {message.Id} ({message.Code}) is missing a value!");
                        return ListenerResult.Success;
                    }

                    if (netEntityEntryData.Entry is EventInfo ev)
                    {
                        ev.Raise(entity, message.Args);
                        Log.Verbose($"Raised event '{ev.Name}' on entity '{entity.entityId}'");
                    }
                    else if (netEntityEntryData.Entry is MethodInfo method)
                    {
                        if (netEntityEntryData.Type is NetEntityEntryType.ServerCode && entity.IsClient)
                        {
                            Log.Warn($"Attempted to execute server code on the client.");
                            return ListenerResult.Success;
                        }

                        if (netEntityEntryData.Type is NetEntityEntryType.ClientCode && entity.IsServer)
                        {
                            Log.Warn($"Attempted to execute client code on the server.");
                            return ListenerResult.Success;
                        }

                        method.Call(entity, message.Args);
                        Log.Verbose($"Called method '{method.Name}' on entity '{entity.entityId}'");
                        return ListenerResult.Success;
                    }
                    else if (netEntityEntryData.Entry is PropertyInfo property)
                    {
                        entity.entityDataStorage.InternalSetValue(netEntityEntryData.ShortCode, message.Args[0]);
                        Log.Verbose($"Synced value '{property.Name}' on entity '{entity.entityId}'");
                        return ListenerResult.Success;
                    }
                    else
                    {
                        Log.Warn($"Received an unknown net entity entry: {netEntityEntryData.Entry.GetType().FullName}");
                        return ListenerResult.Success;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to handle a net entity message!\n{ex}");
                    return ListenerResult.Failed;
                }

                return ListenerResult.Success;
            }
        }
    }
}