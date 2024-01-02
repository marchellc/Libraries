using Common.Extensions;
using Common.IO.Collections;

using Networking.Features;
using Networking.Utilities;
using Networking.Requests;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Networking.Objects
{
    public class NetworkManager : NetworkFeature
    {
        public LockedDictionary<Type, NetworkObject> objects;

        public LockedDictionary<ushort, Type> netTypes;
        public LockedDictionary<ushort, PropertyInfo> netProperties;
        public LockedDictionary<ushort, FieldInfo> netFields;
        public LockedDictionary<ushort, MethodInfo> netMethods;

        public override void Start()
        {
            try
            {
                objects = new LockedDictionary<Type, NetworkObject>();

                netTypes = new LockedDictionary<ushort, Type>();
                netProperties = new LockedDictionary<ushort, PropertyInfo>();
                netFields = new LockedDictionary<ushort, FieldInfo>();
                netMethods = new LockedDictionary<ushort, MethodInfo>();

                LoadTypes();

                if (net.isServer)
                    Listen<NetworkCmdMessage>(OnCmd);
                else
                    Listen<NetworkRpcMessage>(OnRpc);

                Listen<NetworkObjectAddMessage>(OnCreated);
                Listen<NetworkObjectRemoveMessage>(OnDestroyed);
                Listen<NetworkVariableSyncMessage>(OnVarSync);

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

            netProperties.Clear();
            netProperties = null;

            netFields.Clear();
            netFields = null;

            netMethods.Clear();
            netMethods = null;

            netTypes.Clear();
            netTypes = null;

            log.Info($"Object networking unloaded.");
        }

        public T Instantiate<T>() where T : NetworkObject
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
        }

        private void OnCmd(NetworkCmdMessage cmdMsg)
        {
            if (net.isClient)
            {
                log.Warn($"Received a CMD call request on the server.");
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
            }
            else
            {
                log.Warn($"Receive an ADD message with an unknown type hash: {addMsg.typeHash}");
            }
        }

        private void LoadTypes()
        {
            if (netTypes.Count > 0)
                throw new InvalidOperationException($"Types have already been loaded");

            try
            {
                log.Debug($"Loading sync types ..");

                var assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
                var curAssembly = Assembly.GetExecutingAssembly();

                if (!assemblies.Contains(curAssembly))
                    assemblies.Add(curAssembly);

                var types = new List<Type>();

                foreach (var assembly in assemblies)
                    types.AddRange(assembly.GetTypes());

                if (!types.Contains(typeof(RequestManager)))
                    types.Add(typeof(RequestManager));

                foreach (var type in types)
                {
                    if (type != typeof(NetworkObject) && type.IsSubclassOf(typeof(NetworkObject))
                        && type.GetAllConstructors().Any(c => c.Parameters().Length == 1 && !type.ContainsGenericParameters))
                    {
                        netTypes[type.GetTypeHash()] = type;

                        log.Debug($"Cached sync type: {type.FullName} ({type.GetTypeHash()})");

                        foreach (var property in type.GetAllProperties())
                        {
                            if (property.Name.StartsWith("Network") && property.GetMethod != null && property.SetMethod != null
                                && !property.GetMethod.IsStatic && !property.SetMethod.IsStatic && TypeLoader.GetWriter(property.PropertyType) != null
                                && TypeLoader.GetReader(property.PropertyType) != null)
                            {
                                netProperties[property.GetPropertyHash()] = property;
                                log.Debug($"Cached network property: {property.ToName()} ({property.GetPropertyHash()})");
                            }
                        }

                        foreach (var field in type.GetAllFields())
                        {
                            if (!field.IsStatic && field.Name.StartsWith("network") && field.FieldType.InheritsType<NetworkVariable>() && !field.IsInitOnly)
                            {
                                netFields[field.GetFieldHash()] = field;
                                log.Debug($"Cached network field: {field.ToName()} ({field.GetFieldHash()})");
                            }
                        }

                        foreach (var method in type.GetAllMethods())
                        {
                            if (!method.IsStatic && (method.Name.StartsWith("Cmd") || method.Name.StartsWith("Rpc")) && method.ReturnType == typeof(void))
                            {
                                netMethods[method.GetMethodHash()] = method;

                                log.Debug($"Cached network method: {method.ToName()} ({method.GetMethodHash()})");

                                var methodFieldName = $"{method.Name}Hash";

                                foreach (var field in type.GetAllFields())
                                {
                                    if (field.Name != methodFieldName || field.FieldType != typeof(ushort) || !field.IsStatic || field.IsInitOnly)
                                        continue;

                                    field.SetValueFast(method.GetMethodHash());
                                    log.Debug($"Set value to network method field {field.ToName()} ({method.GetMethodHash()})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}