using Common.Reflection;

using System;
using System.IO;
using System.Reflection;

namespace Network.Synchronization
{
    public class SynchronizedProperty
    {
        private SynchronizationParent parent;
        private PropertyInfo property;
        private DateTime lastUpdate = DateTime.Now;

        public SynchronizationParent Parent => parent;
        public SynchronizationManager Manager => parent?.Manager;

        public PropertyInfo Property => property;

        public NetworkPeer Peer => Manager?.Peer;

        public DateTime LastUpdate => lastUpdate;

        public event Action OnUpdateReceived;
        public event Action OnUpdateSent;

        public virtual void Update(BinaryWriter writer)
        {
            lastUpdate = DateTime.Now;
            OnUpdateReceived.Call();
        }

        public virtual void Update(BinaryReader reader)
        {
            OnUpdateSent.Call();
        }

        public virtual void OnCreated()
        {

        }

        public virtual void OnDestroyed()
        {

        }

        public void Update()
            => Manager?.Send(parent.Id, parent.GetIndex(this));

        internal void Destroy()
        {
            OnDestroyed();
            this.parent = null;
        }

        internal void Create(SynchronizationParent parent, PropertyInfo property)
        {
            this.parent = parent;
            this.property = property;

            OnCreated();
        }
    }
}