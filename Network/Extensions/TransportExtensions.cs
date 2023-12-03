using Common.Extensions;
using Common.Reflection;

using System;
using System.IO;

namespace Network.Extensions
{
    public static class TransportExtensions
    {
        public static void Send(this ITransport transport, params object[] payload)
        {
            if (transport is null)
                throw new ArgumentNullException(nameof(transport));

            if (payload.Length <= 0)
                throw new ArgumentException($"Cannot send an empty payload");

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.WriteItems(payload, obj => bw.WriteObject(obj, transport));
                transport.Send(ms.ToArray());
            }
        }

        public static void Send(this ITransport transport, byte msgId, Action<BinaryWriter> action)
        {
            if (transport is null)
                throw new ArgumentNullException(nameof(transport));

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(msgId);
                action.Call(bw);
                transport.Send(ms.ToArray());
            }
        }
    }
}