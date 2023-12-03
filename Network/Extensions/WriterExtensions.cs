using Network.Interfaces;

using Newtonsoft.Json;

using System;
using System.IO;

namespace Network.Extensions
{
    public static class WriterExtensions
    {
        public static void WriteObject(this BinaryWriter writer, object obj, ITransport transport, bool isJson = false)
        {
            if (transport is null)
                throw new ArgumentNullException(nameof(transport));

            if (obj is null) 
                throw new ArgumentNullException(nameof(obj));

            var typeId = transport.GetTypeId(obj.GetType());

            if (typeId < 0)
                throw new InvalidOperationException($"Invalid type ID: {typeId}");

            writer.Write(typeId);

            if (isJson)
            {
                writer.Write(true);
                writer.Write(JsonConvert.SerializeObject(obj));
            }
            else
                writer.Write(false);

            if (obj is IMessage message)
                message.Write(writer);
        }
    }
}