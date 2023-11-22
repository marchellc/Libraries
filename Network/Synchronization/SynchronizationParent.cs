using Common.Pooling;

using System;
using System.IO;
using System.Reflection;

namespace Network.Synchronization
{
    public class SynchronizationParent
    {
        private byte generatedId;

        private SynchronizedProperty[] properties;
        private SynchronizationManager manager;

        public byte Id => generatedId;

        public SynchronizationManager Manager => manager;

        public virtual void OnCreated(object[] args)
        {

        }

        public virtual void OnDestroyed()
        {

        }

        public void UpdateAll()
        {
            for (int i = 0; i < properties.Length; i++)
                properties[i].Update();
        }

        internal void Init(SynchronizationManager manager, byte id)
        {
            this.generatedId = id;
            this.manager = manager;

            var props = PoolExtensions.GetList<SynchronizedProperty>();

            foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.GetType().IsSubclassOf(typeof(SynchronizedProperty))
                    && prop.CanWrite 
                    && prop.CanRead)
                {
                    var syncProp = Activator.CreateInstance(prop.GetType()) as SynchronizedProperty;

                    if (syncProp != null)
                    {
                        syncProp.Create(this, prop);
                        prop.SetValue(this, syncProp);
                        props.Add(syncProp);
                    }
                }
            }

            properties = props.ArrayReturn();
        }

        internal void Dispose()
        {
            OnDestroyed();

            for (int i = 0; i < properties.Length; i++)
                properties[i].Destroy();

            manager = null;
            properties = null;
            generatedId = 0;
        }

        internal void WriteProps(BinaryWriter writer)
        {
            writer.Write((byte)properties.Length);

            for (int i = 0; i < properties.Length; i++)
            {
                writer.Write(properties[i].Property.Name);
                properties[i].Update(writer);
            }
        }

        internal void ReadProps(BinaryReader reader)
        {
            var size = reader.ReadByte();
            var type = GetType();

            properties = new SynchronizedProperty[size];

            for (int i = 0; i < size; i++)
            {
                var propertyName = reader.ReadString();
                var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (property is null)
                    throw new InvalidOperationException($"Missing property: {propertyName}");

                var syncPropType = property.PropertyType;
                var syncProp = Activator.CreateInstance(syncPropType) as SynchronizedProperty;

                syncProp.Create(this, property);
                syncProp.Update(reader);

                properties[i] = syncProp;

                property.SetValue(this, syncProp);
            }

            OnCreated(Array.Empty<object>());
        }

        internal byte GetIndex(SynchronizedProperty property)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i] == property)
                {
                    return (byte)i;
                }
            }

            return 0;
        }

        internal SynchronizedProperty GetProperty(byte index)
        {
            if (index >= properties.Length)
                return null;

            return properties[index];
        }
    }
}