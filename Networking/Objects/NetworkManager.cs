using Common.Extensions;
using Common.IO.Collections;
using Common.Utilities;

using Networking.Features;
using Networking.Utilities;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Networking.Objects
{
    public class NetworkManager : NetworkFeature
    {
        public LockedDictionary<Type, NetworkObject> objects;

        public LockedDictionary<Type, Action<NetworkObject>> creationListeners;
        public LockedDictionary<Type, Action<NetworkObject>> destroyListeners;

        public LockedDictionary<ushort, Type> netTypes;
        public LockedDictionary<ushort, PropertyInfo> netProperties;
        public LockedDictionary<ushort, FieldInfo> netFields;
        public LockedDictionary<ushort, MethodInfo> netMethods;
        public LockedDictionary<ushort, EventInfo> netEvents;

        public LockedDictionary<string, ushort> netHashes;

        public event Action OnInitialized;

        public override void Start()
        {
            try
            {
                objects = new LockedDictionary<Type, NetworkObject>();

                creationListeners = new LockedDictionary<Type, Action<NetworkObject>>();
                destroyListeners = new LockedDictionary<Type, Action<NetworkObject>>();

                netTypes = new LockedDictionary<ushort, Type>();
                netProperties = new LockedDictionary<ushort, PropertyInfo>();
                netFields = new LockedDictionary<ushort, FieldInfo>();
                netMethods = new LockedDictionary<ushort, MethodInfo>();
                netEvents = new LockedDictionary<ushort, EventInfo>();
                netHashes = new LockedDictionary<string, ushort>();

                LoadTypes();

                if (net.isServer)
                    Listen<NetworkCmdMessage>(OnCmd);
                else
                    Listen<NetworkRpcMessage>(OnRpc);

                Listen<NetworkHashSyncMessage>(OnHashSync);
                Listen<NetworkObjectAddMessage>(OnCreated);
                Listen<NetworkObjectRemoveMessage>(OnDestroyed);
                Listen<NetworkVariableSyncMessage>(OnVarSync);
                Listen<NetworkRaiseEventMessage>(OnRaise);

                log.Info($"Object networking initialized.");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public override void Stop()
        {
            objects.Clear();
            objects = null;

            creationListeners.Clear();
            creationListeners = null;

            destroyListeners.Clear();
            destroyListeners = null;

            netProperties.Clear();
            netProperties = null;

            netFields.Clear();
            netFields = null;

            netMethods.Clear();
            netMethods = null;

            netTypes.Clear();
            netTypes = null;

            netEvents.Clear();
            netEvents = null;

            netHashes.Clear();
            netHashes = null;

            log.Info($"Object networking unloaded.");
        }

        public void ListenCreate<T>(Action<T> listener) where T : NetworkObject
            => creationListeners[typeof(T)] = obj =>
            {
                if (obj is null || obj is not T tObj)
                    return;

                listener.Call(tObj);
            };

        public void ListenDestroy<T>(Action<T> listener) where T : NetworkObject
            => destroyListeners[typeof(T)] = obj =>
            {
                if (obj is null || obj is not T tObj)
                    return;

                listener.Call(tObj);
            };

        public void StopListenCreate<T>()
            => creationListeners.Remove(typeof(T));

        public void StopListenDestroy<T>()
            => destroyListeners.Remove(typeof(T));

        public T Get<T>() where T : NetworkObject
        {
            if (objects.TryGetValue(typeof(T), out var netObject))
                return (T)netObject;

            var instance = typeof(T).Construct(this) as T;

            objects[typeof(T)] = instance;

            net.Send(new NetworkObjectAddMessage(typeof(T).GetTypeHash()));

            instance.isDestroyed = false;
            instance.isReady = true;

            instance.StartInternal();
            instance.OnStart();

            if (creationListeners.TryGetValue(typeof(T), out var listener))
                listener.Call(instance);

            return instance;
        }

        public void Destroy<T>()
        {
            if (!objects.TryGetValue(typeof(T), out var netObject)
                || !netTypes.TryGetKey(typeof(T), out var typeHash))
                return;

            if (netObject.isDestroyed)
                throw new InvalidOperationException($"Object is already destroyed");

            netObject.isDestroyed = true;
            netObject.isReady = false;

            netObject.OnStop();
            netObject.StopInternal();

            objects.Remove(typeof(T));

            net.Send(new NetworkObjectRemoveMessage(typeHash));

            if (destroyListeners.TryGetValue(typeof(T), out var listener))
                listener.Call(netObject);
        }

        private void OnRaise(NetworkRaiseEventMessage raiseMsg)
        {
            if (!netEvents.TryGetValue(raiseMsg.eventHash, out var ev))
            {
                log.Warn($"Received a RAISE call for an unknown event: {raiseMsg.eventHash}");
                return;
            }

            if (!netTypes.TryGetValue(raiseMsg.typeHash, out var type))
            {
                log.Warn($"Received a RAISE call for an unknown type: {raiseMsg.typeHash}");
                return;
            }

            if (!objects.TryGetValue(type, out var obj))
            {
                log.Warn($"Received a RAISE call for type '{type.FullName}' with a missing instance.");
                return;
            }

            ev.Raise(obj, raiseMsg.args);

            log.Debug($"Raised event '{ev.ToName()}'");
        }

        private void OnCmd(NetworkCmdMessage cmdMsg)
        {
            if (net.isClient)
            {
                log.Warn($"Received a CMD call request on the client.");
                return;
            }

            if (!netMethods.TryGetValue(cmdMsg.functionHash, out var netMethod))
            {
                log.Warn($"Received a CMD call for an unknown function: {cmdMsg.functionHash}");
                return;
            }

            if (!netTypes.TryGetValue(cmdMsg.typeHash, out var type))
            {
                log.Warn($"Received a CMD message with an unknown type hash: {cmdMsg.typeHash}");
                return;
            }

            if (!objects.TryGetValue(type, out var obj))
            {
                log.Warn($"Received a CMD message for type with no active instance ({type.FullName})");
                return;
            }

            netMethod.Call(obj, cmdMsg.args);

            log.Debug($"Called by CMD: {netMethod.ToName()} (with {cmdMsg.args?.Length ?? -1} args)");
        }

        private void OnRpc(NetworkRpcMessage rpcMsg)
        {
            if (net.isServer)
            {
                log.Warn($"Received a RPC call request on the server.");
                return;
            }

            if (!netMethods.TryGetValue(rpcMsg.functionHash, out var netMethod))
            {
                log.Warn($"Received a CMD call for an unknown function: {rpcMsg.functionHash}");
                return;
            }

            if (!netTypes.TryGetValue(rpcMsg.typeHash, out var type))
            {
                log.Warn($"Received a RPC message with an unknown type hash: {rpcMsg.typeHash}");
                return;
            }

            if (!objects.TryGetValue(type, out var obj) || obj.isDestroyed || !obj.isReady)
            {
                log.Warn($"Received a RPC message for type with no active instance ({type.FullName})");
                return;
            }

            netMethod.Call(obj, rpcMsg.args);

            log.Debug($"Called by RPC: {netMethod.ToName()} (with {rpcMsg.args?.Length ?? -1} args)");
        }

        private void OnVarSync(NetworkVariableSyncMessage syncMsg)
        {
            if (!netTypes.TryGetValue(syncMsg.typeHash, out var type))
            {
                log.Warn($"Received a VAR_SYNC message with an unknown type hash: {syncMsg.typeHash}");
                return;
            }

            if (!objects.TryGetValue(type, out var obj) || obj.isDestroyed || !obj.isReady)
            {
                log.Warn($"Received a VAR_SYNC message for type with no active instance ({type.FullName})");
                return;
            }

            obj.ProcessVarSync(syncMsg);
        }

        private void OnDestroyed(NetworkObjectRemoveMessage destroyMsg)
        {
            if (netTypes.TryGetValue(destroyMsg.typeHash, out var type))
            {
                if (objects.TryGetValue(type, out var obj))
                {
                    obj.isReady = false;
                    obj.isDestroyed = true;

                    obj.OnStop();
                    obj.StopInternal();

                    if (destroyListeners.TryGetValue(type, out var listener))
                        listener.Call(obj);
                }
                else
                {
                    log.Warn($"Received a DESTROY message for type '{type.FullName}', but there isn't any instance present.");
                    return;
                }

                objects.Remove(type);
            }
            else
            {
                log.Warn($"Received a DESTROY message with an unknown type hash: {destroyMsg.typeHash}");
            }
        }

        private void OnCreated(NetworkObjectAddMessage addMsg)
        {
            if (netTypes.TryGetValue(addMsg.typeHash, out var type))
            {
                if (objects.TryGetValue(type, out _))
                {
                    log.Warn($"Received an ADD message for type '{type.FullName}', but there is already an instance present. Keeping it.");
                    return;
                }

                var instance = type.Construct(this) as NetworkObject;

                objects[type] = instance;

                instance.isDestroyed = false;
                instance.isReady = true;

                instance.StartInternal();
                instance.OnStart();

                if (creationListeners.TryGetValue(type, out var listener))
                    listener.Call(instance);
            }
            else
            {
                log.Warn($"Receive an ADD message with an unknown type hash: {addMsg.typeHash}");
            }
        }

        private void OnHashSync(NetworkHashSyncMessage syncMsg)
        {
            log.Info($"Received a hash sync message with {syncMsg.hashes.Count} hashes.");

            foreach (var hash in syncMsg.hashes)
            {
                if (netMethods.ContainsKey(hash.Value)
                    || netEvents.ContainsKey(hash.Value)
                    || netProperties.ContainsKey(hash.Value)
                    || netFields.ContainsKey(hash.Value))
                {
                    log.Verbose($"Hash '{hash.Key}'={hash.Value} is already known to this client, skipping ..");
                    continue;
                }

                log.Info($"Received an unknown hash: '{hash.Key}'={hash.Value}");

                var split = hash.Key.Split('.');

                if (split.Length != 2)
                    continue;

                log.Verbose($"Split: type={split[0]} field={split[1]}");

                foreach (var type in netTypes)
                {
                    if (type.Value.Name != split[0])
                        continue;

                    log.Verbose($"Type '{type.Value.FullName}' should contain field '{split[1]}'");

                    try
                    {
                        var field = type.Value.Field(split[1]);

                        if (field is null)
                            continue;

                        field.SetValueFast(hash.Value);

                        log.Verbose($"Field '{field.ToName()}' value set to {hash.Value}");
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }

            log.Info($"Synchronized {syncMsg.hashes.Count} hashes.");
        }

        private void LoadTypes()
        {
            try
            {
                netTypes.Clear();

                var assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
                var curAssembly = Assembly.GetExecutingAssembly();

                if (!assemblies.Contains(curAssembly))
                    assemblies.Add(curAssembly);

                var types = new List<Type>();

                foreach (var assembly in assemblies)
                    types.AddRange(assembly.GetTypes());

                foreach (var type in types)
                {
                    if (type != typeof(NetworkObject) && type.IsSubclassOf(typeof(NetworkObject))
                        && type.GetAllConstructors().Any(c => c.Parameters().Length == 1 && !type.ContainsGenericParameters))
                    {
                        netTypes[type.GetTypeHash()] = type;

                        foreach (var property in type.GetAllProperties())
                        {
                            if (property.Name.StartsWith("Network") && property.GetGetMethod(true) != null && property.GetSetMethod(true) != null
                                && !property.GetGetMethod(true).IsStatic && !property.GetSetMethod(true).IsStatic && TypeLoader.GetWriter(property.PropertyType) != null
                                && TypeLoader.GetReader(property.PropertyType) != null)
                            {
                                netProperties[property.GetPropertyHash()] = property;
                                netHashes[$"{property.DeclaringType.Name}.{property.Name}Hash"] = property.GetPropertyHash();
                            }
                        }

                        foreach (var field in type.GetAllFields())
                        {
                            if (!field.IsStatic && field.Name.StartsWith("network") && field.FieldType.InheritsType<NetworkVariable>() && !field.IsInitOnly)
                            {
                                netFields[field.GetFieldHash()] = field;
                                netHashes[$"{field.DeclaringType.Name}.{field.Name}Hash"] = field.GetFieldHash();
                            }
                        }

                        foreach (var method in type.GetAllMethods())
                        {
                            if (!method.IsStatic && (method.Name.StartsWith("Cmd") || method.Name.StartsWith("Rpc")) && method.ReturnType == typeof(void))
                            {
                                netMethods[method.GetMethodHash()] = method;
                                netHashes[$"{method.DeclaringType.Name}.{method.Name}Hash"] = method.GetMethodHash();

                                var methodFieldName = $"{method.Name}Hash";

                                foreach (var field in type.GetAllFields())
                                {
                                    if (field.Name != methodFieldName || field.FieldType != typeof(ushort) || !field.IsStatic || field.IsInitOnly)
                                        continue;

                                    field.SetValueFast(method.GetMethodHash());
                                }
                            }
                        }

                        foreach (var ev in type.GetAllEvents())
                        {
                            if (ev.Name.StartsWith("Network"))
                            {
                                netEvents[ev.GetEventHash()] = ev;
                                netHashes[$"{ev.DeclaringType.Name}.{ev.Name}Hash"] = ev.GetEventHash();

                                var eventFieldName = $"{ev.Name}Hash";

                                foreach (var field in type.GetAllFields())
                                {
                                    if (field.Name != eventFieldName || field.FieldType != typeof(ushort) || !field.IsStatic || field.IsInitOnly)
                                        continue;

                                    field.SetValueFast(ev.GetEventHash());
                                }
                            }
                        }
                    }
                }

                CodeUtils.Delay(() =>
                {
                    net.Send(new NetworkHashSyncMessage(new Dictionary<string, ushort>(netHashes)));

                    CodeUtils.Delay(() => OnInitialized.Call(), 200);
                }, 100);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to load network types, disconnecting!\n{ex}");
                net.Disconnect();
            }
        }
    }
}