using Common.Extensions;

using Networking.Interfaces;
using Networking.Enums;

using System;

namespace Networking.Extensions
{
    public static class NetClientExtensions
    {
        public static bool ExecuteIf<TClient>(this IClient client, Action<TClient> action) where TClient : IClient
        {
            if (client is null || client is not TClient targetClient)
                return false;

            action.Call(targetClient);
            return false;
        }

        public static bool ExecuteIfClient(this IClient client, Action action)
        {
            if (client != null && client.Type is ClientType.Client)
            {
                action.Call();
                return true;
            }

            return false;
        }

        public static bool ExecuteIfClient(this IClient client, Action<IClient> action)
        {
            if (client != null && client.Type is ClientType.Client)
            {
                action.Call(client);
                return true;
            }

            return false;
        }

        public static bool ExecuteIfPeer(this IClient client, Action action)
        {
            if (client != null && client.Type is ClientType.Peer)
            {
                action.Call();
                return true;
            }

            return false;
        }

        public static bool ExecuteIfPeer(this IClient client, Action<IClient> action)
        {
            if (client != null && client.Type is ClientType.Peer)
            {
                action.Call(client);
                return true;
            }

            return false;
        }

        public static bool ExecuteIfServer(this IClient client, Action action)
        {
            if (client != null && client.Type is ClientType.Server)
            {
                action.Call();
                return true;
            }

            return false;
        }

        public static bool ExecuteIfServer(this IClient client, Action<IClient> action)
        {
            if (client != null && client.Type is ClientType.Server)
            {
                action.Call(client);
                return true;
            }

            return false;
        }
    }
}