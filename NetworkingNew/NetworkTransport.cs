using Common.Extensions;

using Networking.Enums;

using System;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public static class NetworkTransport
    {
        public class NetworkClientTransportEvents
        {
            public Action OnStarted;
            public Action<byte[]> OnData;
            public Action<TransportError, object> OnStopped;

            public Func<IPEndPoint> GetRemote;
            public Action<IPEndPoint> SetRemote;

            public UdpClient Client;
        }

        internal static void ClientReceive(UdpClient client, NetworkClientTransportEvents transportEvents, bool callStart)
        {
            client.BeginReceive(result => InternalClientReceive(result, transportEvents), null);

            if (callStart)
                transportEvents.OnStarted.Call();
        }

        private static void InternalClientReceive(IAsyncResult result, NetworkClientTransportEvents events)
        {
            try
            {
                var sender = events.GetRemote.Call() ?? new IPEndPoint(0, 0);
                var data = events.Client.EndReceive(result, ref sender);

                events.SetRemote.Call(sender);
                events.OnData.Call(data);
            }
            catch (Exception ex)
            {
                events.OnStopped.Call(TransportError.Exception, ex);
                return;
            }

            if (events.Client is null || events.Client.Client is null || !events.Client.Client.Connected)
            {
                events.OnStopped.Call(TransportError.Disconnected, null);
                return;
            }

            ClientReceive(events.Client, events, false);
        }
    }
}