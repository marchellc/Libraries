using Common.Logging;
using Network.Interfaces.Synchronization;

using System.IO;
using System.Reflection;

namespace Network.Synchronization
{
    public class SynchronizedValue<TValue> : ISynchronizedValue
    {
        private ISynchronizedRoot root;
        private ISynchronizationManager manager;

        private short id;

        private bool isSynced;

        private PropertyInfo property;

        private TValue value;

        public ISynchronizedRoot Root => root;
        public ISynchronizationManager Manager => manager;

        public PropertyInfo Property => property;

        public short Id => id;

        public bool IsSynchronized => isSynced;

        public TValue Value
        {
            get => value;
            set
            {
                if (this.value is null && value is null)
                    return;

                if (this.value != null && value != null && this.value.Equals(value))
                    return;

                this.value = value;

                Manager.Update(this);
            }
        }

        public void Initialize(ISynchronizedRoot root, PropertyInfo property, short id)
        {
            this.root = root;
            this.manager = root.Manager;
            this.id = id;
            this.property = property;
        }

        public virtual void Update(BinaryReader reader)
        {

        }

        public virtual void Update(BinaryWriter writer)
        {

        }

        public void SetValueNoUpdate(TValue value)
        {
            this.value = value;
        }
    }
}