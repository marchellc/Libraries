using Common.IO.Collections;
using Common.Extensions;

using Network.Extensions;
using Network.Features;
using Network.Interfaces.Synchronization;
using Network.Interfaces.Controllers;

using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace Network.Synchronization
{
    public class SynchronizationManager : Feature, ISynchronizationManager
    {
        public const byte CREATE_ROOT_REQ = 25;

        public const byte DESTROY_ROOT_REQ = 28;

        public const byte UPDATE_VALUE_REQ = 26;
        public const byte UPDATE_ROOT_TYPES_REQ = 27;
        public const byte UPDATE_ROOT_TYPES_RES = 29;

        public const byte UPDATE_ROOT_TYPES_RES_CONF = 30;

        private LockedList<ISynchronizedRoot> roots;
        private LockedList<Type> rootTypes;

        private LockedDictionary<Type, LockedList<Delegate>> creationHandlers = new LockedDictionary<Type, LockedList<Delegate>>();

        private short rootId;
        private bool isSynced;

        public IEnumerable<ISynchronizedRoot> Roots => roots;

        public event Action OnReady;

        public override void OnStarted()
        {
            base.OnStarted();

            OnReady += () => Log.Info("Ready!");

            roots = new LockedList<ISynchronizedRoot>();
            rootTypes = new LockedList<Type>();
            creationHandlers = new LockedDictionary<Type, LockedList<Delegate>>();

            rootId = 0;

            Transport.CreateHandler(UPDATE_ROOT_TYPES_REQ, HandleRootTypesSyncRequest);
            Transport.CreateHandler(UPDATE_ROOT_TYPES_RES, HandleRootTypesSyncResponse);
            Transport.CreateHandler(UPDATE_VALUE_REQ, HandleValueUpdate);
            Transport.CreateHandler(UPDATE_ROOT_TYPES_RES_CONF, HandleRootTypesSyncResponseConfirmation);

            Transport.CreateHandler(CREATE_ROOT_REQ, HandleRootCreate);

            Transport.CreateHandler(DESTROY_ROOT_REQ, HandleRootDestroy);

            if (Controller is IServer)
            {
                Transport.Send(UPDATE_ROOT_TYPES_REQ, null);
            }
            else
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < assemblies.Length; i++)
                {
                    foreach (var type in assemblies[i].GetTypes())
                    {
                        if (type == typeof(ISynchronizedRoot) || type == typeof(SynchronizedRoot))
                            continue;

                        if (typeof(ISynchronizedRoot).IsAssignableFrom(type))
                        {
                            rootTypes.Add(type);
                        }
                    }
                }
            }

            Log.Info("Started!");
        }

        public override void OnStopped()
        {
            for (int i = 0; i < roots.Count; i++)
                roots[i].OnDestroyed();

            roots.Clear();
            roots = null;

            rootTypes.Clear();
            rootTypes = null;

            creationHandlers.Clear();
            creationHandlers = null;

            rootId = 0;

            isSynced = false;

            Transport.RemoveHandler(UPDATE_ROOT_TYPES_REQ, HandleRootTypesSyncRequest);
            Transport.RemoveHandler(UPDATE_ROOT_TYPES_RES, HandleRootTypesSyncResponse);
            Transport.RemoveHandler(UPDATE_VALUE_REQ, HandleValueUpdate);
            Transport.RemoveHandler(UPDATE_ROOT_TYPES_RES_CONF, HandleRootTypesSyncResponseConfirmation);

            Transport.RemoveHandler(CREATE_ROOT_REQ, HandleRootCreate);

            Transport.RemoveHandler(DESTROY_ROOT_REQ, HandleRootDestroy);

            base.OnStopped();
        }

        public TRoot Create<TRoot>() where TRoot : ISynchronizedRoot, new()
        {
            if (!rootTypes.Contains(typeof(TRoot)))
            {
                Log.Error($"Unrecognized root type!");
                return default;
            }

            var root = new TRoot();

            if (root is not SynchronizedRoot syncRoot)
            {
                Log.Error($"Requested root type is invalid");
                return default;
            }

            try
            {
                syncRoot.manager = this;
                syncRoot.id = GetNextId();
                syncRoot.OnCreated();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to update root!\n{ex}");
                return default;
            }

            roots.Add(syncRoot);

            Transport.Send(CREATE_ROOT_REQ, bw =>
            {
                bw.Write((short)rootTypes.IndexOf(typeof(TRoot)));
                bw.Write(syncRoot.id);
            });

            return root;
        }

        public void Destroy(ISynchronizedRoot root)
        {
            if (!roots.Contains(root))
                return;

            root.OnDestroyed();

            roots.Remove(root);
        }

        public void Update(ISynchronizedValue value)
        {
            if (value.Root is null)
                return;

            Transport.Send(UPDATE_VALUE_REQ, bw =>
            {
                bw.Write(value.Root.Id);
                bw.Write(value.Id);

                value.Update(bw);
            });
        }

        public void CreateHandler<TRoot>(Action<TRoot> handler) where TRoot : ISynchronizedRoot
        {
            if (!creationHandlers.TryGetValue(typeof(TRoot), out var list))
                list = creationHandlers[typeof(TRoot)] = new LockedList<Delegate>();

            if (list.Contains(handler))
                return;

            list.Add(handler);
        }

        public void RemoveHandler<TRoot>(Action<TRoot> handler) where TRoot : ISynchronizedRoot
        {
            if (!creationHandlers.TryGetValue(typeof(TRoot), out var list))
                return;

            list.Remove(handler);
        }

        private void HandleRootCreate(BinaryReader reader)
        {
            var rootType = rootTypes[reader.ReadInt16()];
            var root = Activator.CreateInstance(rootType) as ISynchronizedRoot;

            if (root is null)
                return;

            if (root is not SynchronizedRoot syncRoot)
                return;

            syncRoot.manager = this;
            syncRoot.id = reader.ReadInt16();

            roots.Add(syncRoot);

            syncRoot.OnCreated();
        }

        private void HandleRootDestroy(BinaryReader reader)
        {
            var rootId = reader.ReadInt16();
            var root = roots.FirstOrDefault(r => r.Id == rootId);

            if (root != null)
            {
                root.OnDestroyed();
                roots.Remove(root);
            }       
        }

        private void HandleRootTypesSyncRequest(BinaryReader reader)
        {
            Transport.Send(UPDATE_ROOT_TYPES_RES, bw => bw.WriteItems(rootTypes, t => bw.WriteType(t)));
        }

        private void HandleRootTypesSyncResponse(BinaryReader reader)
        {
            if (isSynced)
                return;

            isSynced = true;

            rootTypes.Clear();
            rootTypes.AddRange(reader.ReadArray(true, reader.ReadType));

            OnReady?.Invoke();

            Transport.Send(UPDATE_ROOT_TYPES_RES_CONF, null);
        }

        private void HandleRootTypesSyncResponseConfirmation(BinaryReader reader)
        {
            if (isSynced)
                return;

            isSynced = true;

            OnReady?.Invoke();
        }

        private void HandleValueUpdate(BinaryReader reader)
        {
            var rootId = reader.ReadInt16();
            var valueId = reader.ReadInt16();

            var root = roots.FirstOrDefault(r => r.Id == rootId);

            if (root is null)
                return;

            var value = root.ValueOfId(valueId);

            if (value is null)
                return;

            value.Update(reader);
        }

        private short GetNextId()
        {
            if (rootId >= short.MaxValue)
                rootId = 0;

            return rootId++;
        }
    }
}