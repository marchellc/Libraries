using Common.Extensions;
using Common.IO.Collections;
using Common.Pooling.Pools;

using Networking.Components.Calls;
using Networking.Features;

using System;

namespace Networking.Components
{
    public class NetworkObject
    {
        private static readonly LockedDictionary<Type, RemoteCall[]> remoteCalls = new LockedDictionary<Type, RemoteCall[]>();

        public NetworkIdentity Identity { get; internal set; }

        public NetworkParent Parent
        {
            get => Identity.Parent;
        }

        public NetworkFunctions Client
        {
            get => Identity.Parent.Network;
        }

        public uint NetId
        {
            get => Identity.Id;
        }

        public ushort Index { get; internal set; }

        public bool IsActive { get; internal set; }

        public NetworkObject()
        {
            RegisterRemoteCalls(GetType());
        }

        public virtual void OnDestroy() { }

        public void DestroyObject()
        {
            if (Identity is null)
                throw new InvalidOperationException($"This object's Network Identity has not been set!");

            Identity.DestroyObject(this, NetworkRequestType.Current);
        }

        public static RemoteCall GetRemoteCall(string methodName)
        {
            foreach (var pair in remoteCalls)
            {
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    if (pair.Value[i].Method.Name == methodName)
                        return pair.Value[i];
                }
            }

            return null;
        }

        private static void RegisterRemoteCalls(Type type)
        {
            if (remoteCalls.ContainsKey(type))
                return;

            var foundCalls = ListPool<RemoteCall>.Shared.Rent();

            foreach (var method in type.GetAllMethods())
            {
                if (!method.IsStatic && (method.Name.StartsWith("Rpc") || method.Name.StartsWith("Cmd") || method.Name.StartsWith("Client") || method.Name.StartsWith("Server")))
                {
                    var remoteCall = new RemoteCall(method, (method.Name.StartsWith("Rpc") || method.Name.StartsWith("Client")) ? RemoteCallType.Rpc : RemoteCallType.Command);
                    foundCalls.Add(remoteCall);
                }
            }

            remoteCalls[type] = ListPool<RemoteCall>.Shared.ToArrayReturn(foundCalls);
        }
    }
}