using Common.Extensions;
using Common.IO.Collections;

using Networking.Features;

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Networking.Objects
{
    public class NetworkManager : NetworkFeature
    {
        public LockedDictionary<int, NetworkObject> objects;
        public LockedDictionary<int, NetworkVariable> vars;

        public LockedDictionary<int, LockedList<NetworkMethod>> methods;
        public LockedDictionary<short, LockedDictionary<int, Tuple<MethodInfo, MethodInfo>>> methodBinding;

        public LockedDictionary<short, Type> objectTypes;

        public int objectId;
        public int varId;

        public short objectTypeId;

        public override void Start()
        {
            objects = new LockedDictionary<int, NetworkObject>();
            vars = new LockedDictionary<int, NetworkVariable>();
            methods = new LockedDictionary<int, LockedList<NetworkMethod>>();

            objectTypes = new LockedDictionary<short, Type>();
            methodBinding = new LockedDictionary<short, LockedDictionary<int, Tuple<MethodInfo, MethodInfo>>>();

            varId = 0;
            objectId = 0;
            objectTypeId = 0;

            if (net.isServer)
                LoadAndSendTypes();
            else
            {
                Listen<NetworkObjectSyncMessage>(OnSync);
                Listen<NetworkCallRpcMessage>(OnCallRpc);
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
            Remove<NetworkObjectSyncMessage>();
            Remove<NetworkCallRpcMessage>();

            objects.Clear();
            objects = null;

            objectTypes.Clear();
            objectTypes = null;

            vars.Clear();
            vars = null;

            methods.Clear();
            methods = null;

            methodBinding.Clear();
            methodBinding = null;

            varId = 0;
            objectId = 0;
            objectTypeId = 0;

            log.Info($"Object networking unloaded.");
        }

        public int GetMethodId(NetworkObject networkObject)
        {
            if (networkObject is null)
                throw new ArgumentNullException(nameof(networkObject));

            var type = networkObject.GetType();
            var trace = new StackTrace();
            var frame = trace.GetFrame(0);
            var callMethod = frame.GetMethod();

            if (callMethod is null)
                throw new InvalidOperationException($"Cannot retrieve calling method");

            if (callMethod.DeclaringType != type)
                throw new InvalidOperationException($"This method has to be called from the network object");

            if (!objectTypes.TryGetKey(type, out var typeId))
                throw new InvalidOperationException($"Type is not a network object");

            if (!methodBinding.TryGetValue(typeId, out var methods))
                return -1;

            foreach (var method in methods)
            {
                if (method.Value.Item1 == callMethod
                    || method.Value.Item2 == callMethod)
                    return method.Key;
            }

            return -1;
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

            var netObj = type.Construct(new object[] { objectId++, this });

            if (netObj is null || netObj is not NetworkObject netObjInstance)
                throw new Exception($"Failed to instantiate type '{type.FullName}'");

            RegisterVariables(netObjInstance);

            netObjInstance.OnStart();

            objects[netObjInstance.id] = netObjInstance;

            net.Send(new NetworkObjectAddMessage(typeId));

            netObjInstance.isReady = true;
            netObjInstance.isDestroyed = false;

            if (methodBinding.TryGetValue(typeId, out var methods))
            {
                foreach (var method in methods)
                    this.methods[method.Key].Add(new NetworkMethod(netObjInstance.id, method.Key, method.Value.Item1, method.Value.Item2, netObjInstance));
            }

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

            objects.Remove(netObject.id);

            net.Send(new NetworkObjectRemoveMessage(netObject.id));

            netObject.isDestroyed = true;
            netObject.isReady = false;

            UnregisterVariables(netObject);

            log.Trace($"Destroyed network type {netObject.GetType().FullName} at {netObject.id}");
        }

        public void Synchronize(NetworkVariable netVar)
        {
            if (netVar is null)
                throw new ArgumentNullException(nameof(netVar));

            if (!vars.ContainsKey(netVar.id))
                throw new InvalidOperationException($"Unknown variable ID");

            var writer = net.GetWriter();

            netVar.GetValue(writer);

            net.Send(new NetworkVariableSyncMessage(netVar.id, writer.Buffer));

            writer.Return();

            log.Trace($"Sent a sync message for VAR {netVar.id}");
        }

        private void RegisterVariables(NetworkObject netObj)
        {
            var type = netObj.GetType();
            
            foreach (var field in type.GetAllFields())
            {
                if (field.IsStatic || field.IsInitOnly)
                    continue;

                if (!field.IsDefined(typeof(NetworkVariable), false))
                    continue;

                var netVar = new NetworkVariable(field.FieldType, field, netObj, varId++, netObj.id, this);

                vars[netVar.id] = netVar;

                log.Trace($"Registered network variable field {field.FieldType.FullName} {field.DeclaringType.FullName}.{field.Name} ({netVar.id}-{netVar.parentId})");
            }

            foreach (var prop in type.GetAllProperties())
            {
                if (!prop.CanWrite || !prop.CanRead || prop.SetMethod is null || prop.SetMethod.IsStatic || prop.GetMethod is null || prop.GetMethod.IsStatic)
                    continue;

                if (!prop.IsDefined(typeof(NetworkVariable), false))
                    continue;

                var netVar = new NetworkVariable(prop.PropertyType, prop, netObj, varId++, netObj.id, this);

                vars[netVar.id] = netVar;

                log.Trace($"Registered network variable field {prop.PropertyType.FullName} {prop.DeclaringType.FullName}.{prop.Name} ({netVar.id}-{netVar.parentId})");
            }
        }

        private void UnregisterVariables(NetworkObject netObj)
        {
            var type = netObj.GetType();

            foreach (var field in type.GetAllFields())
            {
                if (field.IsStatic || field.IsInitOnly)
                    continue;

                if (!field.IsDefined(typeof(NetworkVariable), false))
                    continue;

                var netVar = vars.FirstOrDefault(nVar => nVar.Value.parentId == netObj.id && nVar.Value.member is FieldInfo netField && netField == field);

                if (netVar.Value is null)
                    continue;

                vars.Remove(netVar.Key);

                netVar.Value.Dispose();
            }

            foreach (var prop in type.GetAllProperties())
            {
                if (!prop.CanWrite || !prop.CanRead || prop.SetMethod is null || prop.SetMethod.IsStatic || prop.GetMethod is null || prop.GetMethod.IsStatic)
                    continue;

                if (!prop.IsDefined(typeof(NetworkVariable), false))
                    continue;

                var netVar = vars.FirstOrDefault(nVar => nVar.Value.parentId == netObj.id && nVar.Value.member is PropertyInfo netProp && netProp == prop);

                if (netVar.Value is null)
                    continue;

                vars.Remove(netVar.Key);

                netVar.Value.Dispose();
            }
        }

        private void OnCallRpc(NetworkCallRpcMessage rpcMessage)
        {
            if (!objects.TryGetValue(rpcMessage.objectId, out var netObj))
                return;

            if (!methods.TryGetValue(rpcMessage.rpcId, out var netMethods))
                return;

            var netMethod = netMethods.FirstOrDefault(netMethod => netMethod.parentId == netObj.id);

            if (netMethod is null)
                return;

            netMethod.targetRpc.Call(netMethod.reference, rpcMessage.args);
        }

        private void OnVarSync(NetworkVariableSyncMessage syncMsg)
        {
            if (!vars.TryGetValue(syncMsg.variableId, out var netVar))
                throw new InvalidOperationException($"Unknown network variable ID: {syncMsg.variableId}");

            var reader = net.GetReader(syncMsg.data);

            netVar.SetValue(reader);

            reader.Return();

            log.Trace($"Received VAR_SYNC: {syncMsg.variableId}");
        }

        private void OnDestroyed(NetworkObjectRemoveMessage destroyMsg)
        {
            if (!objects.TryGetValue(destroyMsg.objectId, out var netObj))
                return;

            netObj.OnStop();

            objects.Remove(destroyMsg.objectId);

            netObj.isDestroyed = true;
            netObj.isReady = false;

            UnregisterVariables(netObj);

            if (objectTypes.TryGetKey(netObj.GetType(), out var typeId)
                && methodBinding.TryGetValue(typeId, out var methods))
            {
                foreach (var method in methods)
                {
                    if (this.methods.ContainsKey(method.Key))
                        this.methods[method.Key].RemoveRange(netMethod => netMethod.parentId == destroyMsg.objectId);
                }
            }

            log.Trace($"Received DESTROY: {destroyMsg.objectId}");
        }

        private void OnCreated(NetworkObjectAddMessage addMsg)
        {
            if (!objectTypes.TryGetValue(addMsg.typeId, out var type))
                return;

            var netObj = type.Construct(new object[] { objectId++, this });

            if (netObj is null || netObj is not NetworkObject netObjInstance)
                throw new Exception($"Failed to instantiate type '{type.FullName}'");

            netObjInstance.OnStart();

            objects[netObjInstance.id] = netObjInstance;

            netObjInstance.isReady = true;
            netObjInstance.isDestroyed = false;

            RegisterVariables(netObjInstance);

            objectId++;

            if (methodBinding.TryGetValue(addMsg.typeId, out var methods))
            {
                foreach (var method in methods)
                    this.methods[method.Key].Add(new NetworkMethod(netObjInstance.id, method.Key, method.Value.Item1, method.Value.Item2, netObjInstance));
            }

            log.Trace($"Received ADD: {addMsg.typeId} ({type.FullName})");
        }

        private void OnSync(NetworkObjectSyncMessage syncMsg)
        {
            foreach (var type in syncMsg.syncTypes)
                objectTypes[objectTypeId++] = type;

            Remove<NetworkObjectSyncMessage>();

            log.Trace($"Received SYNC: {syncMsg.syncTypes.Count}");
        }

        private void LoadAndSendTypes()
        {
            if (objectTypes.Count > 0)
                throw new InvalidOperationException($"Types have already been loaded");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.InheritsType<NetworkObject>())
                    {
                        var nextId = objectTypeId++;
                        var methodId = 0;

                        objectTypes[nextId] = type;
                        methodBinding[nextId] = new LockedDictionary<int, Tuple<MethodInfo, MethodInfo>>();

                        foreach (var method in type.GetAllMethods())
                        {
                            if (method.IsStatic || method.ReturnType != typeof(void))
                                continue;

                            if (method.Name.StartsWith("Rpc"))
                            {
                                var clearMethodName = method.Name.Replace("Rpc", "");
                                var callMethod = type.Method($"CallRpc{clearMethodName}");

                                if (callMethod is null)
                                    continue;

                                if (!callMethod.Parameters().Select(p => p.ParameterType).SequenceEqual(method.Parameters().Select(p => p.ParameterType)))
                                    continue;

                                methodBinding[nextId][methodId++] = new Tuple<MethodInfo, MethodInfo>(method, callMethod);
                            }
                        }

                        log.Trace($"Loaded type {type.FullName} ({objectTypeId})");
                    }
                }
            }

            net.Send(new NetworkObjectSyncMessage(objectTypes.Values.ToList()));

            log.Trace($"Sent SYNC");
        }
    }
}