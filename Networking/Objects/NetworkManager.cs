using Common.Extensions;
using Common.IO.Collections;

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
        public LockedDictionary<int, NetworkObject> objects;

        public LockedDictionary<short, Type> objectTypes;

        public LockedDictionary<ushort, PropertyInfo> netProperties;
        public LockedDictionary<ushort, FieldInfo> netFields;
        public LockedDictionary<ushort, MethodInfo> netMethods;

        public int objectId;
        public short objectTypeId;

        public override void Start()
        {
            objects = new LockedDictionary<int, NetworkObject>();
            objectTypes = new LockedDictionary<short, Type>();

            netProperties = new LockedDictionary<ushort, PropertyInfo>();
            netFields = new LockedDictionary<ushort, FieldInfo>();

            objectId = 0;
            objectTypeId = 0;

            if (net.isServer)
            {
                LoadAndSendTypes();
                Listen<NetworkCmdMessage>(OnCmd);
            }
            else
            {
                Listen<NetworkObjectSyncMessage>(OnSync);
                Listen<NetworkRpcMessage>(OnRpc);
            }

            Listen<NetworkObjectAddMessage>(OnCreated);
            Listen<NetworkObjectRemoveMessage>(OnDestroyed);
            Listen<NetworkVariableSyncMessage>(OnVarSync);

            log.Info($"Object networking initialized.");
        }

        public override void Stop()
        {
            Remove<NetworkObjectSyncMessage>();
            Remove<NetworkObjectAddMessage>();
            Remove<NetworkObjectRemoveMessage>();
            Remove<NetworkVariableSyncMessage>();
            Remove<NetworkRpcMessage>();
            Remove<NetworkCmdMessage>();

            objects.Clear();
            objects = null;

            objectTypes.Clear();
            objectTypes = null;

            objectId = 0;
            objectTypeId = 0;

            log.Info($"Object networking unloaded.");
        }

        public T Instantiate<T>() where T : NetworkObject
        {
            var netObjInstance = Instantiate(typeof(T));

            if (netObjInstance is null)
                throw new Exception($"Failed to instantiate type '{typeof(T).FullName}'");

            if (netObjInstance is not T t)
                throw new Exception($"Instantiated type cannot be cast to '{typeof(T).FullName}'");

            return t;
        }

        public T Get<T>(int objectId)
        {
            if (!objects.TryGetValue(objectId, out var netObj))
                throw new InvalidOperationException($"No network object with ID {objectId}");

            if (netObj is not T t)
                throw new InvalidOperationException($"Cannot cast object {netObj.GetType().FullName} to {typeof(T).FullName}");

            return t;
        }

        public NetworkObject Get(int objectId)
        {
            if (!objects.TryGetValue(objectId, out var netObj))
                throw new InvalidOperationException($"No network object with ID {objectId}");

            return netObj;
        }

        public NetworkObject Instantiate(Type type)
        {
            if (objectTypes.Count <= 0)
                throw new InvalidOperationException($"Types were not synchronized.");

            if (!objectTypes.TryGetKey(type, out var typeId))
                throw new InvalidOperationException($"Cannot instantiate a non network object type");

            var netObj = type.Construct([objectId++, this]);

            if (netObj is null || netObj is not NetworkObject netObjInstance)
                throw new Exception($"Failed to instantiate type '{type.FullName}'");

            netObjInstance.StartInternal();
            netObjInstance.OnStart();

            objects[netObjInstance.id] = netObjInstance;

            net.Send(new NetworkObjectAddMessage(typeId));

            netObjInstance.isReady = true;
            netObjInstance.isDestroyed = false;

            log.Trace($"Instantiated network type {type.FullName} at {netObjInstance.id}");

            return netObjInstance;
        }

        public void Destroy(NetworkObject netObject)
        {
            if (netObject is null)
                throw new ArgumentNullException(nameof(netObject));

            if (netObject.id <= 0 || !objects.ContainsKey(netObject.id))
                throw new InvalidOperationException($"This network object was not spawned by this manager.");

            if (netObject.isDestroyed)
                throw new InvalidOperationException($"Object is already destroyed");

            netObject.OnStop();
            netObject.StopInternal();

            objects.Remove(netObject.id);

            net.Send(new NetworkObjectRemoveMessage(netObject.id));

            netObject.isDestroyed = true;
            netObject.isReady = false;

            log.Trace($"Destroyed network type {netObject.GetType().FullName} at {netObject.id}");
        }

        private void OnCmd(NetworkCmdMessage cmdMsg)
        {
            if (net.isClient)
                return;

            if (!objects.TryGetValue(cmdMsg.objectId, out var netObj))
                return;

            if (!netMethods.TryGetValue(cmdMsg.functionHash, out var netMethod))
                return;

            netMethod.Call(netObj, cmdMsg.args);
        }

        private void OnRpc(NetworkRpcMessage rpcMsg)
        {
            if (net.isServer)
                return;

            if (!objects.TryGetValue(rpcMsg.objectId, out var netObj))
                return;

            if (!netMethods.TryGetValue(rpcMsg.functionHash, out var netMethod))
                return;

            netMethod.Call(netObj, rpcMsg.args);
        }

        private void OnVarSync(NetworkVariableSyncMessage syncMsg)
        {
            if (!objects.TryGetValue(syncMsg.objectId, out var netObj))
                return;

            netObj.ProcessVarSync(syncMsg);
        }

        private void OnDestroyed(NetworkObjectRemoveMessage destroyMsg)
        {
            if (!objects.TryGetValue(destroyMsg.objectId, out var netObj))
                return;

            netObj.OnStop();
            netObj.StopInternal();

            objects.Remove(destroyMsg.objectId);

            netObj.isDestroyed = true;
            netObj.isReady = false;

            log.Trace($"Received DESTROY: {destroyMsg.objectId}");
        }

        private void OnCreated(NetworkObjectAddMessage addMsg)
        {
            if (!objectTypes.TryGetValue(addMsg.typeId, out var type))
                return;

            var netObj = type.Construct([objectId++, this]);

            if (netObj is null || netObj is not NetworkObject netObjInstance)
                throw new Exception($"Failed to instantiate type '{type.FullName}'");

            netObjInstance.StartInternal();
            netObjInstance.OnStart();

            objects[netObjInstance.id] = netObjInstance;

            netObjInstance.isReady = true;
            netObjInstance.isDestroyed = false;

            log.Trace($"Received ADD: {addMsg.typeId} ({type.FullName})");
        }

        private void OnSync(NetworkObjectSyncMessage syncMsg)
        {
            if (net.isServer)
                return;

            if (syncMsg.syncTypes is null || syncMsg.syncTypes.Count <= 0)
                return;

            foreach (var syncType in syncMsg.syncTypes)
            {
                objectTypes[syncType.Key] = syncType.Value;

                foreach (var property in syncType.Value.GetAllProperties())
                {
                    if (property.Name.StartsWith("Network") && property.GetMethod != null && property.SetMethod != null
                        && !property.GetMethod.IsStatic && !property.SetMethod.IsStatic && TypeLoader.GetWriter(property.PropertyType) != null
                        && TypeLoader.GetReader(property.PropertyType) != null)
                        netProperties[property.GetPropertyHash()] = property;
                }

                foreach (var field in syncType.Value.GetAllFields())
                {
                    if (!field.IsStatic && field.Name.StartsWith("network") && field.FieldType.InheritsType<NetworkVariable>() && !field.IsInitOnly)
                        netFields[field.GetFieldHash()] = field;
                }

                foreach (var method in syncType.Value.GetAllMethods())
                {
                    if (!method.IsStatic && (method.Name.StartsWith("Cmd") || method.Name.StartsWith("Rpc")) && method.ReturnType == typeof(void))
                    {
                        netMethods[method.GetMethodHash()] = method;

                        var methodField = method.DeclaringType.Field($"{method.Name}Hash");

                        if (methodField != null && !methodField.IsInitOnly && methodField.IsStatic && methodField.FieldType == typeof(ushort))
                            methodField.SetValueFast(method.GetMethodHash());
                    }
                }
            }
        }

        private void LoadAndSendTypes()
        {
            if (net.isClient)
                throw new InvalidOperationException($"The client cannot send their types.");

            if (objectTypes.Count > 0)
                throw new InvalidOperationException($"Types have already been loaded");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.InheritsType<NetworkObject>() && type.GetAllConstructors().Any(c => c.Parameters().Length == 0 && !type.ContainsGenericParameters))
                    {
                        objectTypes[objectTypeId++] = type;

                        foreach (var property in type.GetAllProperties())
                        {
                            if (property.Name.StartsWith("Network") && property.GetMethod != null && property.SetMethod != null
                                && !property.GetMethod.IsStatic && !property.SetMethod.IsStatic && TypeLoader.GetWriter(property.PropertyType) != null
                                && TypeLoader.GetReader(property.PropertyType) != null)
                                netProperties[property.GetPropertyHash()] = property;
                        }

                        foreach (var field in type.GetAllFields())
                        {
                            if (!field.IsStatic && field.Name.StartsWith("network") && field.FieldType.InheritsType<NetworkVariable>() && !field.IsInitOnly)
                                netFields[field.GetFieldHash()] = field;
                        }

                        foreach (var method in type.GetAllMethods())
                        {
                            if (!method.IsStatic && (method.Name.StartsWith("Cmd") || method.Name.StartsWith("Rpc")) && method.ReturnType == typeof(void))
                            {
                                netMethods[method.GetMethodHash()] = method;

                                var methodField = method.DeclaringType.Field($"{method.Name}Hash");

                                if (methodField != null && !methodField.IsInitOnly && methodField.IsStatic && methodField.FieldType == typeof(ushort))
                                    methodField.SetValueFast(method.GetMethodHash());
                            }
                        }
                    }
                }
            }

            net.Send(new NetworkObjectSyncMessage(new Dictionary<short, Type>(objectTypes)));
        }
    }
}