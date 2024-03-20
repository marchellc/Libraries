using Common.Extensions;
using Common.IO.Collections;

using System;

namespace Common.Utilities
{
    public static class TypeSearch
    {
        private static readonly LockedDictionary<ushort, Type> shortDiscovery = new LockedDictionary<ushort, Type>();
        private static readonly LockedDictionary<int, Type> longDiscovery = new LockedDictionary<int, Type>();

        public static event Action<Type> OnTypeDiscovered;

        public static bool TryFind(int typeId, out Type type)
        {
            if (longDiscovery.TryGetValue(typeId, out type))
                return true;

            var types = ModuleInitializer.SafeQueryTypes();

            foreach (var searchType in types)
            {
                if (searchType.FullName.StartsWith("Microsoft") || searchType.InheritsType(typeof(Attribute)))
                    continue;

                if (searchType.GetLongCode() == typeId)
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

            var types = ModuleInitializer.SafeQueryTypes();

            foreach (var searchType in types)
            {
                if (searchType.FullName.StartsWith("Microsoft") || searchType.InheritsType(typeof(Attribute)))
                    continue;

                if (searchType.GetShortCode() == typeId)
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