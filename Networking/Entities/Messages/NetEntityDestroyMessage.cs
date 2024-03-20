using Common.IO.Data;

using Networking.Enums;

namespace Networking.Entities.Messages
{
    public struct NetEntityDestroyMessage : IData
    {
        public NetEntityMessageType Type;
        public ulong Target;

        public NetEntityDestroyMessage(NetEntityMessageType type, ulong target)
        {
            Type = type;
            Target = target;
        }

        public void Deserialize(DataReader reader)
        {
            Type = (NetEntityMessageType)reader.ReadByte();
            Target = reader.ReadCompressedULong();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteCompressedULong(Target);
        }

        public class NetEntityDestroyMessageListener : NetEntityDataListener<NetEntityDestroyMessage>
        {
            public override ListenerResult Process(NetEntityDestroyMessage message)
            {
                if (!EntityManager.TryGetEntity<NetEntity>(message.Target, out var entity))
                {
                    Log.Warn($"Received a NetEntityDestroyMessage for an unknown entity: {message.Target}");
                    return ListenerResult.Success;
                }

                EntityManager.ProcessEntityDestroy(entity, message.Type is NetEntityMessageType.Confirmation);
                return ListenerResult.Success;
            }
        }
    }
}