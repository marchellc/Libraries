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

        private LockedList<ISynchronizedRoot> roots;
        private LockedList<Type> rootTypes;
        private LockedDictionary<Type, LockedList<Delegate>> creationHandlers = new LockedDictionary<Type, LockedList<Delegate>>();

        private short rootId;

        public IEnumerable<ISynchronizedRoot> Roots => roots;

        public event Action OnReady;

        public override void OnStarted()
        {
            base.OnStarted();

            roots = new LockedList<ISynchronizedRoot>();
            rootTypes = new LockedList<Type>();
            creationHandlers = new LockedDictionary<Type, LockedList<Delegate>>();

            rootId = 0;

            Transport.CreateHandler(UPDATE_ROOT_TYPES_REQ, HandleRootTypesSyncRequest);
            Transport.CreateHandler(UPDATE_ROOT_TYPES_RES, HandleRootTypesSyncResponse);
            Transport.CreateHandler(UPDATE_VALUE_REQ, HandleValueUpdate);
            Transport.CreateHandler(CREATE_ROOT_REQ, HandleRootCreate);
            Transport.CreateHandler(DESTROY_ROOT_REQ, HandleRootDestroy);

            if (Controller is IServer)
                Transport.Send(UPDATE_ROOT_TYPES_REQ, null);
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

            Transport.RemoveHandler(UPDATE_ROOT_TYPES_REQ, HandleRootTypesSyncRequest);
            Transport.RemoveHandler(UPDATE_ROOT_TYPES_RES, HandleRootTypesSyncResponse);
            Transport.RemoveHandler(UPDATE_VALUE_REQ, HandleValueUpdate);
            Transport.RemoveHandler(CREATE_ROOT_REQ, HandleRootCreate);
            Transport.RemoveHandler(DESTROY_ROOT_REQ, HandleRootDestroy);

            base.OnStopped();
        }

        public TRoot Create<TRoot>() where TRoot : ISynchronizedRoot, new()
        {
            if (!rootTypes.Contains(typeof(TRoot)))
                throw new InvalidOperationException($"The client is missing root type '{typeof(TRoot).FullName}'");

            var root = new TRoot();

            if (root is not SynchronizedRoot syncRoot)
                throw new InvalidOperationException($"This manager requires a SynchronizedRoot.");

            syncRoot.manager = this;
            syncRoot.id = GetNextId();
            syncRoot.OnCreated();

            roots.Add(syncRoot);

            Log.Debug($"Created a new synchronization root: {typeof(TRoot).FullName}");

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

            Log.Debug($"Destroyed root: {root.Id}");

            root.OnDestroyed();
            roots.Remove(root);
        }

        public void Update(ISynchronizedValue value)
        {
            if (value.Root is null)
                return;

            Log.Debug($"Updating value: {value.Id} ({value.Root.Id})");

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
            Log.Debug($"Received a root creation request");

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
            Log.Debug($"Received a root destruction request");

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

            Log.Debug($"Sent a root type sync response");
        }

        private void HandleRootTypesSyncResponse(BinaryReader reader)
        {
            rootTypes.Clear();
            rootTypes.AddRange(reader.ReadArray(true, reader.ReadType));

            Log.Debug($"Received a root type sync response ({rootTypes.Count})");

            OnReady?.Invoke();
        }

        private void HandleValueUpdate(BinaryReader reader)
        {
            Log.Debug($"Received a value update request");

            var rootId = reader.ReadInt16();
            var valueId = reader.ReadInt16();

            var root = roots.FirstOrDefault(r => r.Id == rootId);

            if (root is null)
                return;

            var value = root.ValueOfId(valueId);

            if (value is null)
                return;

            value.Update(reader);

            Log.Debug($"Updated value: {value.Id} ({value.Root.Id})");
        }

        private short GetNextId()
        {
            if (rootId >= short.MaxValue)
                rootId = 0;

            return rootId++;
        }
    }
}