using Network.Data;

using System;
using System.Collections.Generic;
using System.IO;

namespace Network.Extensions
{
    public static class WriterExtensions
    {
        public static void WriteMessage(this BinaryWriter writer, MessageId messageId)
        {
            writer.Write(messageId.Header);
            writer.Write(messageId.Channel);
            writer.Write(messageId.Id);
            writer.Write((byte)(messageId.IsInternal ? 1 : 0));
        }

        public static void WriteObject(this BinaryWriter writer, object obj, NetworkPeer peer) 
        {
            if (obj is null)
            {
                writer.Write(true);
                return;
            }
            else
            {
                var type = obj.GetType();
                var types = peer.GetSyncTypes();

                if (!types.Contains(type))
                    throw new Exception($"Invalid type!");

                writer.Write(false);
                writer.WriteType(type, types);
                
                if (obj is IWritable writable)
                    writable.Write(writer, peer);
            }
        }

        public static void WriteObject(this BinaryWriter writer, object obj, MessageId message, NetworkPeer peer)
        {
            if (obj is null)
            {
                writer.Write(true);
                return;
            }
            else
            {
                writer.Write(false);

                if (obj is IWritable writable)
                    writable.Write(writer, peer);
            }
        }

        public static void WriteType(this BinaryWriter writer, Type type, List<Type> types)
        {
            var index = types.IndexOf(type);

            if (index < 0)
                throw new InvalidOperationException($"The type list does not contain type '{type.FullName}'");

            writer.Write((short)index);
        }

        public static void WriteType(this BinaryWriter writer, Type type)
            => writer.Write(type.AssemblyQualifiedName);
    }
}