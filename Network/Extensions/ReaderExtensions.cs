using Network.Data;

using System;
using System.IO;

namespace Network.Extensions
{
    public static class ReaderExtensions
    {
        public static object ReadObject(this BinaryReader reader, NetworkPeer peer)
        {
            var types = peer.GetSyncTypes();
            var isNull = reader.ReadBoolean();

            if (isNull)
                return null;

            var typeIndex = reader.ReadInt16();

            if (typeIndex < 0 || typeIndex >= types.Count)
                throw new InvalidDataException($"Invalid type index!");

            var instance = Activator.CreateInstance(types[typeIndex]);

            if (instance is null)
                throw new Exception($"Failed to create instance of type '{types[typeIndex].FullName}'");

            if (instance is IReadable readable)
                readable.Read(reader, peer);

            return instance;
        }

        public static object ReadObject(this BinaryReader reader, MessageId message, NetworkPeer peer)
        {
            var types = peer.GetSyncTypes();
            var isNull = reader.ReadBoolean();

            if (isNull)
                return null;

            var typeIndex = message.Id;

            if (typeIndex < 0 || typeIndex >= types.Count)
                throw new InvalidDataException($"Invalid type index!");

            var instance = Activator.CreateInstance(types[typeIndex]);

            if (instance is null)
                throw new Exception($"Failed to create instance of type '{types[typeIndex].FullName}'");

            if (instance is IReadable readable)
                readable.Read(reader, peer);

            return instance;
        }

        public static MessageId ReadMessage(this BinaryReader reader)
            => new MessageId
            {
                Header = reader.ReadByte(),
                Channel = reader.ReadByte(),
                Id = reader.ReadInt16(),
                IsInternal = reader.ReadByte() == 1
            };

        public static Type ReadType(this BinaryReader reader)
        {
            var typeName = reader.ReadString();
            var type = Type.GetType(typeName);

            if (type is null)
                throw new InvalidDataException($"Unknown type: {typeName}");

            return type;
        }
    }
}