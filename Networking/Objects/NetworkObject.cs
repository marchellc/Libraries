using Networking.Features;

using System;

namespace Networking.Objects
{
    public class NetworkObject
    {
        public bool isDestroyed;
        public bool isReady;

        public readonly int id;

        public readonly NetworkManager manager;
        public readonly NetworkFunctions net;

        public NetworkObject(int id, NetworkManager manager)
        {
            this.id = id;
            this.manager = manager;
            this.net = manager.net;
        }

        public virtual void OnStart() { }
        public virtual void OnStop() { }

        public void Destroy()
            => manager.Destroy(this);

        public void CallRpc(int rpcId, params object[] args)
        {
            if (net.isClient)
                throw new InvalidOperationException($"Cannot call client RPCs from the client.");

            net.Send(new NetworkCallRpcMessage(id, rpcId, args));
        }

        public void CallCmd(int cmdId, params object[] args)
        {
            if (net.isServer)
                throw new InvalidOperationException($"Cannot call server CMDs from the server.");


        }
    }
}