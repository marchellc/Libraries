using Common.Extensions;
using Common.IO.Collections;

using Networking.Data;
using Networking.Interfaces;

using System;

namespace Networking.Utilities
{
    public class TypeLibrary : ITypeLibrary
    {
        private LockedDictionary<short, Type> typeLib;
        private bool everLoaded;

        public bool IsLoaded => typeLib != null;

        public event Action OnLoaded;

        public Type GetType(short typeId)
        {
            if (typeLib is null)
                throw new InvalidOperationException($"Types were never synchronized");

            if (!typeLib.TryGetValue(typeId, out var type) || type is null)
                throw new InvalidOperationException($"Unknown type ID: {typeId}");

            return type;
        }

        public short GetTypeId(Type type)
        {
            if (typeLib is null)
                return -1;

            if (!typeLib.TryGetKey(type, out var typeId))
                return -1;

            return typeId;
        }

        public bool Verify(Reader reader)
        {
            var typeLibSize = reader.ReadInt();

            typeLib = new LockedDictionary<short, Type>(typeLibSize);

            for (int i = 0; i < typeLibSize; i++)
            {
                try
                {
                    typeLib[reader.ReadShort()] = reader.ReadType();
                }
                catch
                {
                    return false;
                }
            }

            if (!everLoaded)
            {
                OnLoaded.Call();
                everLoaded = true;
            }

            return true;
        }

        public void Write(Writer writer)
        {
            if (typeLib is null)
                LoadTypes();

            writer.WriteInt(typeLib.Count);

            foreach (var pair in typeLib)
            {
                writer.WriteShort(pair.Key);
                writer.WriteType(pair.Value);
            }

            if (!everLoaded)
            {
                OnLoaded.Call();
                everLoaded = true;
            }
        }

        public void Reset()
        {
            everLoaded = false;

            typeLib.Clear();
            typeLib = null;
        }

        private void LoadTypes()
        {
            var curId = (short)0;

            typeLib = new LockedDictionary<short, Type>();

            foreach (var type in TypeLoader.additionalTypes)
                typeLib[curId++] = type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ISerialize).IsAssignableFrom(type)
                        && typeof(IDeserialize).IsAssignableFrom(type))
                        typeLib[curId++] = type;
                }
            }
        }
    }
}