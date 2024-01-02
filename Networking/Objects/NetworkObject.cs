using Common.Extensions;
using Common.IO.Collections;
using Common.Utilities;

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

        public readonly NetworkManager manager;
        public readonly NetworkFunctions net;
        public readonly ushort typeHash;

        public NetworkObject(NetworkManager manager)
        {
            this.manager = manager;
            this.net = manager.net;
            this.thisType = GetType();
            this.typeHash = this.thisType.GetTypeHash();
            this.netFields = new LockedDictionary<ushort, NetworkVariable>();
        }

        public virtual void OnStart() { }
        public virtual void OnStop() { }

        public void SendRpc(ushort functionHash, params object[] args)
            => net.Send(new NetworkRpcMessage(typeHash, functionHash, args));

        public void SendCmd(ushort functionHash, params object[] args)
            => net.Send(new NetworkCmdMessage(typeHash, functionHash, args));

        public void SendEvent(ushort eventHash, bool shouldExecute, params object[] args)
        {
            if (shouldExecute && manager.netEvents.TryGetValue(eventHash, out var ev))
                ev.Raise(this, args);

            net.Send(new NetworkRaiseEventMessage(typeHash, eventHash, args));
        }

        internal void StartInternal()
        {
            foreach (var fieldPair in manager.netFields)
            {
                if (fieldPair.Value.DeclaringType != thisType)
                    continue;

                var fieldNetVar = fieldPair.Value.FieldType.Construct() as NetworkVariable;

                fieldNetVar.parent = this;
                fieldPair.Value.SetValueFast(fieldNetVar, this);

                netFields[fieldPair.Key] = fieldNetVar;
            }

            CodeUtils.WhileTrue(() => !isDestroyed && isReady, UpdateNetFields, 10);
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
            if (!netFields.TryGetValue(syncMsg.hash, out var netVar))
            {
                manager.log.Warn($"Received a sync message for an unknown network variable: {syncMsg.hash}");
                return;
            }

            netVar.Process(syncMsg.msg);
        }

        private void UpdateNetFields()
        {
            foreach (var netField in netFields)
            {
                while (netField.Value.pending.TryDequeue(out var syncMsg))
                    net.Send(new NetworkVariableSyncMessage(typeHash, netField.Key, syncMsg));
            }
        }
    }
}