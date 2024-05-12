using Common.IO.Collections;
using Common.Logging;
using Common.Pooling.Pools;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class TypeExtensions
    {
        private static readonly LockedDictionary<Type, ConstructorInfo[]> _constructors = new LockedDictionary<Type, ConstructorInfo[]>();
        private static readonly LockedDictionary<Type, Type[]> _implements = new LockedDictionary<Type, Type[]>();

        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static readonly LogOutput Log = new LogOutput("Type Extensions").Setup();

        public static Type ToGeneric(this Type type, params Type[] args)
            => type.MakeGenericType(args);

        public static Type ToGeneric<T>(this Type type)
            => type.MakeGenericType(typeof(T));

        public static Type GetFirstGenericType(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var genericArguments = type.GetGenericArguments();

            if (genericArguments is null || genericArguments.Length <= 0)
                throw new InvalidOperationException($"Attempted to get generic arguments of a type that does not have any.");

            return genericArguments[0];
        }

        public static bool IsStatic(this Type type)
            => type.IsSealed && type.IsAbstract;

        public static bool InheritsType<TType>(this Type type)
            => InheritsType(type, typeof(TType));

        public static bool InheritsType(this Type type, Type inherit)
        {
            if (_implements.TryGetValue(type, out var implements))
                return implements.Contains(inherit);

            var baseType = type.BaseType;
            var cache = ListPool<Type>.Shared.Rent();

            while (baseType != null)
            {
                cache.Add(baseType);
                baseType = baseType.BaseType;
            }

            var interfaces = type.GetInterfaces();

            cache.AddRange(interfaces);

            foreach (var interfaceType in interfaces)
            {
                baseType = interfaceType.BaseType;

                while (baseType != null && baseType.IsInterface)
                {
                    cache.Add(baseType);
                    baseType = baseType.BaseType;
                }
            }

            return (_implements[type] = ListPool<Type>.Shared.ToArrayReturn(cache)).Contains(inherit);
        }

        public static Type ToType(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Byte: return typeof(byte);
                case TypeCode.SByte: return typeof(sbyte);
                case TypeCode.Int16: return typeof(short);
                case TypeCode.UInt16: return typeof(ushort);
                case TypeCode.Int32: return typeof(int);
                case TypeCode.UInt32: return typeof(uint);
                case TypeCode.Int64: return typeof(long);
                case TypeCode.UInt64: return typeof(ulong);
                case TypeCode.Single: return typeof(float);
                case TypeCode.Double: return typeof(double);
                case TypeCode.Decimal: return typeof(decimal);
                case TypeCode.Char: return typeof(char);
                case TypeCode.String: return typeof(string);
                case TypeCode.Boolean: return typeof(bool);

                case TypeCode.DateTime: return typeof(DateTime);

                default: return null;
            }
        }

        public static int GetLongCode(this Type type)
            => type.FullName.GetIntegerCode();

        public static ushort GetShortCode(this Type type)
            => type.FullName.GetShortCode();
    }
}