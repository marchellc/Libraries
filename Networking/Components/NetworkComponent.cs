using System;
using System.Reflection;

using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using Networking.Components.Calls;
using Networking.Components.Messages;

namespace Networking.Components
{
    public class NetworkComponent : NetworkObject
    {
        private static LockedDictionary<string, PropertyInfo> syncVars;
        private static LockedDictionary<string, EventInfo> syncEvents;
        private static LockedDictionary<string, MethodInfo> syncVarCallbacks;

        private LockedDictionary<string, object> syncValues;

        public static LogOutput Log { get; } = new LogOutput("Network Component").Setup();

        public virtual void OnStart()
        {
            syncVars ??= new LockedDictionary<string, PropertyInfo>();
            syncEvents ??= new LockedDictionary<string, EventInfo>();
            syncVarCallbacks ??= new LockedDictionary<string, MethodInfo>();

            syncValues = new LockedDictionary<string, object>();

            if (syncEvents.Count == 0)
            {
                foreach (var ev in GetType().GetAllEvents())
                {
                    if (ev.IsMulticast && (ev.Name.StartsWith("Network") || ev.Name.StartsWith("OnNetwork")))
                    {
                        syncEvents[ev.Name] = ev;
                    }
                }
            }

            if (syncVars.Count == 0 || syncVars.Count != syncValues.Count)
            {
                foreach (var property in GetType().GetAllProperties())
                {
                    var setMethod = property.GetSetMethod(true);
                    var getMethod = property.GetGetMethod(true);

                    if (setMethod is null || setMethod.IsStatic)
                        continue;

                    if (getMethod is null || getMethod.IsStatic)
                        continue;

                    if (!property.Name.StartsWith("Network"))
                        continue;

                    syncVars[property.Name] = property;
                    syncValues[property.Name] = null;

                    var callbackMethod = GetType().Method($"On{property.Name}Updated");

                    if (callbackMethod != null && !callbackMethod.IsStatic)
                    {
                        var methodParams = callbackMethod.Parameters();

                        if (methodParams.Length == 2 && methodParams[0].ParameterType == property.PropertyType
                            && methodParams[1].ParameterType == property.PropertyType)
                        {
                            syncVarCallbacks[property.Name] = callbackMethod;
                        }
                    }
                }
            }

            foreach (var key in syncVars.Keys)
            {
                var field = GetType().Field($"{syncVars[key].Name}InitValue");

                if (field != null)
                {
                    syncValues[key] = field.IsStatic ? field.GetValueFast<object>() : field.GetValueFast<object>(this);
                    continue;
                }

                syncValues[key] = null;
            }
        }

        public virtual void OnUpdate() { }

        public void InvokeRpc(string rpcName, params object[] args)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Cannot invoke RPCs from an inactive component");

            if (Identity.IsClient)
                throw new InvalidOperationException($"Cannot invoke RPCs from the client");

            Client.Send(new RemoteCallMessage(Identity.Id, Index, rpcName, args));
        }

        public void InvokeCmd(string cmdName, params object[] args)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Cannot invoke CMDs from an inactive component");

            if (Identity.IsServer)
                throw new InvalidOperationException($"Cannot invoke CMDs from the server");

            Client.Send(new RemoteCallMessage(Identity.Id, Index, cmdName, args));
        }

        public void InvokeEvent(string eventName, params object[] args)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Cannot invoke events from an inactive component");

            if (!syncEvents.TryGetValue(eventName, out var syncEvent))
                throw new InvalidOperationException($"Unknown sync event: {eventName}");

            syncEvent.Raise(this, args);

            Client.Send(new RemoteCallMessage(Identity.Id, Index, eventName, args));
        }

        public void SetSyncVar(string syncVar, object value)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Cannot sync variables from an inactive component");

            if (!syncValues.ContainsKey(syncVar))
                throw new InvalidOperationException($"Unknown sync variable: {syncVar}");

            if (!Parent.ValidatePermissions(NetworkRequestType.Current, NetworkPermissions.SyncVars))
                throw new AccessViolationException($"No permissions for updating sync variables.");

            var curValue = syncValues[syncVar];
    
            syncValues[syncVar] = value;

            if (syncVarCallbacks.TryGetValue(syncVar, out var callback))
                callback.Call(this, Log.Error, curValue, value);

            Client.Send(new SynchronizeVariableMessage(Identity.Id, Index, syncVar, value));
        }

        public T GetSyncVar<T>(string syncVar)
        {
            if (!IsActive)
                throw new InvalidOperationException($"Cannot sync variables from an inactive component");

            if (!syncValues.TryGetValue(syncVar, out var value))
                throw new InvalidOperationException($"Unknown sync variable: {syncVar}");

            if (value is null)
                return default;

            return (T)value;
        }

        internal void InternalDestroy()
        {
            syncValues.Clear();
            syncValues = null;
        }

        internal bool InternalSetSyncVar(string syncVar, object value)
        {
            if (!syncValues.ContainsKey(syncVar))
                return false;

            var curValue = syncValues[syncVar];

            syncValues[syncVar] = value;

            if (syncVarCallbacks.TryGetValue(syncVar, out var callback))
                callback.Call(this, Log.Error, curValue, value);

            return true;
        }

        internal EventInfo GetEvent(string eventName)
            => syncEvents.TryGetValue(eventName, out var ev) ? ev : null;
    }
}