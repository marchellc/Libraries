using Network.Interfaces;

using Newtonsoft.Json;

using System;
using System.IO;

namespace Network.Extensions
{
    public static class ReaderExtensions
    {
        public static object ReadObject(this BinaryReader reader, ITransport transport)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            if (transport is null)
                throw new ArgumentNullException(nameof(transport));

            var typeId = reader.ReadInt16();
            var type = transport.GetType(typeId);

            if (type is null)
                throw new InvalidOperationException($"Invalid type ID: {typeId}");

            var isJson = reader.ReadBoolean();

            if (isJson)
            {
                var jsonStr = reader.ReadString();
                var jsonValue = JsonConvert.DeserializeObject(jsonStr, type);

                if (jsonValue != null && jsonValue is IMessage message)
                    message.Read(reader);

                return jsonValue;
            }
            else
            {
                var value = Activator.CreateInstance(type);

                if (value != null && value is IMessage message)
                    message.Read(reader);

                return value;
            }
        }
    }
}