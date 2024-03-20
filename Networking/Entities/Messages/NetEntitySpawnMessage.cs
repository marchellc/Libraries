using Common.IO.Data;
using Common.Logging;

using Networking.Data;
using Networking.Enums;

namespace Networking.Entities.Messages
{
    public struct NetEntitySpawnMessage : IData
    {
        public NetEntityMessageType Type;
        public ushort Code;
        public ulong Id;

        public NetEntitySpawnMessage(ushort code)
        {
            Code = code;
            Type = NetEntityMessageType.Request;
        }

        public NetEntitySpawnMessage(ulong id)
        {
            Id = id;
            Type = NetEntityMessageType.Confirmation;
        }

        public void Deserialize(DataReader reader)
        {
            Type = (NetEntityMessageType)reader.ReadByte();

            if (Type is NetEntityMessageType.Confirmation)
            {
                Id = reader.ReadCompressedULong();
                return;
            }

            Code = reader.ReadUShort();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteByte((byte)Type);

            if (Type is NetEntityMessageType.Confirmation)
            {
                writer.WriteCompressedULong(Id);
                return;
            }

            writer.WriteUShort(Code);
        }

        public class NetEntitySpawnMessageListener : NetEntityDataListener<NetEntitySpawnMessage>
        {
            public override ListenerResult Process(NetEntitySpawnMessage message)
            {
                EntityManager.ProcessEntitySpawn(message.Code, message.Id, message.Type is NetEntityMessageType.Confirmation);
                return ListenerResult.Success;
            }
        }
    }
}