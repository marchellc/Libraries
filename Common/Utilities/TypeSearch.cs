using Common.Extensions;
using Common.IO.Collections;

using System;
using System.Collections.Generic;

namespace Common.Utilities
{
    public static class TypeSearch
    {
        private static readonly LockedDictionary<ushort, Type> shortDiscovery = new LockedDictionary<ushort, Type>();
        private static readonly LockedDictionary<int, Type> longDiscovery = new LockedDictionary<int, Type>();

        public static event Action<Type> OnTypeDiscovered;

        public static IEnumerable<Type> GetTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        try
                        {
                            types.Add(type);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return types;
        }

        public static bool TryFind(int typeId, out Type type)
        {
            if (longDiscovery.TryGetValue(typeId, out type))
                return true;

            var types = GetTypes();

            foreach (var searchType in types)
            {
                if (searchType.FullName.GetShortCode() == typeId)
                {
                    longDiscovery[typeId] = searchType;
                    type = searchType;
                    OnTypeDiscovered.Call(type);
                    return true;
                }
            }

            type = null;
            return false;
        }

        public static bool TryFind(ushort typeId, out Type type)
        {
            if (shortDiscovery.TryGetValue(typeId, out type))
                return true;

            var types = GetTypes();

            foreach (var searchType in types)
            {
                if (searchType.FullName.GetShortCode() == typeId)
                {
                    shortDiscovery[typeId] = searchType;
                    type = searchType;
                    OnTypeDiscovered.Call(type);
                    return true;
                }
            }

            type = null;
            return false;
        }
    }
}