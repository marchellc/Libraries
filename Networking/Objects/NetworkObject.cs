using Common.Extensions;
using Common.IO.Collections;

using Networking.Features;

using System;
using System.Threading;

namespace Networking.Objects
{
    public class NetworkObject
    {
        private LockedDictionary<ushort, NetworkVariable> netFields;
        private Timer netTimer;
        private Type thisType;

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
            this.thisType = GetType();
        }

        public virtual void OnStart() { }
        public virtual void OnStop() { }

        public void Destroy()
            => manager.Destroy(this);

        public void SendRpc(ushort functionHash, params object[] args)
            => net.Send(new NetworkRpcMessage(id, functionHash, args));

        public void SendCmd(ushort functionHash, params object[] args)
            => net.Send(new NetworkCmdMessage(id, functionHash, args));

        internal void StartInternal()
        {
            foreach (var fieldPair in manager.netFields)
            {
                if (fieldPair.Value.DeclaringType != thisType)
                    continue;

                var fieldNetVar = fieldPair.Value.FieldType.Construct() as NetworkVariable;

                fieldNetVar.parent = this;

                fieldPair.Value.SetValueFast(this, fieldNetVar);

                netFields[fieldPair.Key] = fieldNetVar;
            }

            netTimer = new Timer(_ => UpdateNetFields(), null, 100, 100);
        }

        internal void StopInternal()
        {
            netTimer?.Dispose();
            netTimer = null;

            netFields?.Clear();
            netFields = null;
        }

        internal void ProcessVarSync(NetworkVariableSyncMessage syncMsg)
        {
            if (!manager.netFields.TryGetValue(syncMsg.hash, out var field) 
                || field.DeclaringType != thisType
                || !netFields.TryGetValue(syncMsg.hash, out var netVar))
                return;

            netVar.Process(syncMsg.msg);
        }

        private void UpdateNetFields()
        {
            foreach (var netField in netFields)
            {
                while (netField.Value.pending.Count > 0)
                {
                    var msg = netField.Value.pending[0];

                    netField.Value.pending.RemoveAt(0);

                    net.Send(new NetworkVariableSyncMessage(id, netField.Key, msg));
                }
            }
        }
    }
}