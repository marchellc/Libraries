using Common.Pooling.Pools;

using Network.Interfaces.Transporting;

using System;
using System.IO;
using System.Text;

namespace Network
{
    public static class MessageUtils
    {
        public static string ToString(params IMessage[] messages)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(messages.Length);

                for (int i = 0; i < messages.Length; i++)
                {
                    bw.Write(messages[i].GetType().AssemblyQualifiedName);
                    messages[i].Write(bw, null);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static IMessage[] FromString(string msg)
        {
            var list = ListPool<IMessage>.Shared.Next();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(msg)))
            using (var br = new BinaryReader(ms))
            {
                var size = br.ReadInt32();
                var array = new IMessage[size];

                for (int i = 0; i < size; i++)
                {
                    var message = Activator.CreateInstance(Type.GetType(br.ReadString())) as IMessage;
                    message.Read(br, null);
                    array[i] = message;
                }
            }

            return ListPool<IMessage>.Shared.ToArrayReturn(list);
        }

        public static TMessage FromString<TMessage>(string msg)
        {
            var messages = FromString(msg);

            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i] is TMessage message)
                    return message;
            }

            return default;
        }
    }
}