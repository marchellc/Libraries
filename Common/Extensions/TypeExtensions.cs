using Common.Extensions;
using Common.Logging;

using Fasterflect;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class TypeExtensions
    {
        public static readonly LogOutput Log = new LogOutput("Type Extensions").Setup();

        public static Type ToGeneric(this Type type, Type genericType)
            => type.MakeGenericType(genericType);

        public static Type ToGeneric<T>(this Type type)
            => type.MakeGenericType(typeof(T));

        public static MethodInfo Method(this Type type, string name)
            => Fasterflect.MethodExtensions.Method(type, name, Flags.AllMembers);

        public static MethodInfo Method(this Type type, string name, params Type[] typeArguments)
            => Fasterflect.MethodExtensions.Method(type, name, typeArguments, Flags.AllMembers);

        public static MethodInfo[] MethodsWithAttribute<T>(this Type type) where T : Attribute
            => MethodExtensions.GetAllMethods(type).Where(m => m.IsDefined(typeof(T), false)).ToArray();

        public static ConstructorInfo[] GetAllConstructors(this Type type)
            => type.Constructors(Flags.AllMembers).ToArray();

        public static ConstructorInfo GetEmptyConstructor(this Type type)
            => type.Constructors(Flags.AllMembers).FirstOrDefault(c => MethodExtensions.Parameters(c).Length <= 0);

        public static ConstructorInfo GetConstructor(this Type type, params Type[] types)
            => type.Constructor(Flags.AllMembers, types);

        public static object Construct(this Type type, params object[] parameters)
            => type.CreateInstance(parameters);

        public static T Construct<T>(this Type type, params object[] parameters)
            => (T)type.CreateInstance(parameters);

        public static T Construct<T>(params object[] parameters)
        {
            var value = typeof(T).CreateInstance(parameters);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static T ConstructSafe<T>(params object[] parameters)
        {
            try
            {
                var value = typeof(T).CreateInstance(parameters);

                if (value is null || value is not T t)
                    return default;

                return t;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to construct type '{typeof(T).ToName()}' due to an exception:\n{ex}");
                return default;
            }
        }

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
            => Fasterflect.TypeExtensions.InheritsOrImplements(type, typeof(TType));

        public static bool InheritsType(this Type type, Type inherit)
            => Fasterflect.TypeExtensions.InheritsOrImplements(type, inherit);

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
    }
}