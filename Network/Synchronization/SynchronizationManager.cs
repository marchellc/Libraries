using Common;
using Common.Extensions;

using Network.Data;
using Network.Extensions;
using Network.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Network.Synchronization
{
    public class SynchronizationManager : NetworkFeature
    {
        public readonly MessageId SyncMessage = new MessageId(0x50, 15, 0, true);
        public readonly MessageId SyncTypesMessage = new MessageId(0x15, 15, 3, true);
        public readonly MessageId CreateParentMessage = new MessageId(0x25, 15, 1, true);
        public readonly MessageId RemoveParentMessage = new MessageId(0x20, 15, 2, true);

        private List<SynchronizationParent> parents = new List<SynchronizationParent>();
        private List<Type> syncParents = new List<Type>();

        private ByteId idGenerator = new ByteId();

        public override void Start()
        {
            base.Start();

            Peer.Handle(SyncMessage, HandleSyncMessage);
            Peer.Handle(SyncTypesMessage, HandleSyncTypeMessage);
            Peer.Handle(CreateParentMessage, HandleCreateMessage);
            Peer.Handle(RemoveParentMessage, HandleDeleteMessage);

            if (Peer.Manager.IsClient)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(SynchronizationParent)))
                            syncParents.Add(type);
                    }
                }

                Peer.Send(SyncTypesMessage, bw => bw.WriteItems(syncParents, t => bw.WriteType(t)));
            }
        }

        public override void Stop()
        {
            Peer.RemoveHandle(HandleSyncMessage);
            Peer.RemoveHandle(HandleSyncTypeMessage);
            Peer.RemoveHandle(HandleCreateMessage);
            Peer.RemoveHandle(HandleDeleteMessage);

            idGenerator = null;

            syncParents.Clear();
            syncParents = null;

            parents.Clear();
            parents = null;

            base.Stop();
        }

        public TParent Create<TParent>(params object[] args) where TParent : SynchronizationParent, new()
        {
            if (!idGenerator.TryTake(out var id))
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Cannot create new synchronization parent; the ID pool is empty");
                return default;
            }

            var parent = new TParent();

            parent.Init(this, id);
            parent.OnCreated(args);

            parents.Add(parent);

            Peer.Send(CreateParentMessage, parent.WriteProps);

            return parent;
        }

        public void Delete(SynchronizationParent parent)
        {
            if (parents.Remove(parent))
            {
                Peer.Send(RemoveParentMessage, bw => bw.Write(parent.Id));
                idGenerator.Return(parent.Id);
                parent.Dispose();
            }
        }

        public void Send(byte parentId, byte propertyId)
        {
            if (Peer is null || !Peer.IsConnected)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Attempted to synchronize with an invalid peer");
                return;
            }

            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i].Id == parentId)
                {
                    var property = parents[i].GetProperty(propertyId);

                    if (property is null)
                    {
                        NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Failed to send update (parent={parentId};property={propertyId}): missing property");
                        return;
                    }

                    Peer.Send(SyncMessage, property.Update);
                }
            }
        }

        private void HandleDeleteMessage(BinaryReader reader)
        {
            var parentId = reader.ReadByte();
            var parent = parents.FirstOrDefault(p => p.Id == parentId);

            if (parent != null && parents.Remove(parent))
            {
                idGenerator.Return(parentId);
                parent.Dispose();
            }
        }

        private void HandleCreateMessage(BinaryReader reader)
        {
            var typeIndex = reader.ReadByte();

            if (typeIndex >= syncParents.Count)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Invalid sync parent type received");
                return;
            }

            var type = syncParents[typeIndex];
            var parent = Activator.CreateInstance(type) as SynchronizationParent;

            if (parent is null)
            {
                NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Failed to create sync parent");
                return;
            }

            var parentId = reader.ReadByte();

            parent.Init(this, parentId);
            parent.ReadProps(reader);

            parents.Add(parent);

            idGenerator.Remove(parentId);
        }

        private void HandleSyncTypeMessage(BinaryReader reader)
        {
            syncParents.Clear();
            syncParents.AddRange(reader.ReadList(reader.ReadType));
        }

        private void HandleSyncMessage(BinaryReader reader)
        {
            var parentId = reader.ReadByte();

            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i].Id == parentId)
                {
                    var propertyIndex = reader.ReadByte();
                    var property = parents[i].GetProperty(propertyIndex);

                    if (property is null)
                    {
                        NetworkLog.Add(NetworkLogLevel.Error, "SYNCHRONIZATION", $"Failed to receive update (parent={parentId};property={propertyIndex}): missing property");
                        return;
                    }

                    property.Update(reader);
                    return;
                }
            }
        }
    }
}