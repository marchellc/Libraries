using Common.Extensions;

using System.Reflection;

namespace Networking.Objects
{
    public static class NetworkObjectExtensions
    {
        public static ushort GetPropertyHash(this PropertyInfo property)
            => (ushort)((property.DeclaringType.Name + "+" + property.Name).GetStableHashCode() & 0xFFFF);

        public static ushort GetFieldHash(this FieldInfo field)
            => (ushort)((field.DeclaringType.Name + "+" + field.Name).GetStableHashCode() & 0xFFFF);

        public static ushort GetMethodHash(this MethodInfo method)
            => (ushort)((method.DeclaringType.Name + "+" + method.Name).GetStableHashCode() & 0xFFFF);
    }
}