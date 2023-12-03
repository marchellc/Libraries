using Common.IO.Collections;

using Network.Interfaces.Synchronization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Network.Synchronization
{
    public class SynchronizedRoot : ISynchronizedRoot
    {
        internal ISynchronizationManager manager;
        internal LockedList<ISynchronizedValue> values;
        internal PropertyInfo[] properties;
        internal short id;
        internal short valueId;

        public IEnumerable<ISynchronizedValue> Values => values;
        public ISynchronizationManager Manager => manager;

        public short Id => id;

        public virtual void OnCreated() 
        {
            values = new LockedList<ISynchronizedValue>();
            properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => typeof(ISynchronizedValue).IsAssignableFrom(p.PropertyType)).OrderBy(p => p.Name).ToArray(); // order by name to ensure same id on client and server

            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(this);

                if (value is null)
                    value = Activator.CreateInstance(properties[i].PropertyType);

                if (value is null || value is not ISynchronizedValue syncValue)
                    continue;

                syncValue.Initialize(this, properties[i], GetNextId());

                properties[i].SetValue(this, value);

                values.Add(syncValue);
            }
        }

        public virtual void OnDestroyed() 
        {
            properties = null;

            values.Clear();
            values = null;

            manager = null;
        }

        public ISynchronizedValue ValueOfId(short id)
            => values.FirstOrDefault(v => v.Id == id);

        private short GetNextId()
        {
            if (valueId >= short.MaxValue)
                valueId = 0;

            return valueId++;
        }
    }
}