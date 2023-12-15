using Network.Interfaces.Transporting;

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
                    message.Read(reader, transport);

                return jsonValue;
            }
            else
            {
                var value = Activator.CreateInstance(type);

                if (value != null && value is IMessage message)
                    message.Read(reader, transport);

                return value;
            }
        }

        public static T ReadObject<T>(this BinaryReader reader, ITransport transport)
        {
            var obj = reader.ReadObject(transport);

            if (obj is null)
                return default;

            if (obj is not T t)
                throw new InvalidDataException($"Bad data order");

            return t;
        }
    }
}