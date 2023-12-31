﻿using Common.Extensions;

using System;
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

        public static ushort GetEventHash(this EventInfo ev)
            => (ushort)((ev.DeclaringType.Name + "+" + ev.Name).GetStableHashCode() & 0xFFFF);

        public static ushort GetTypeHash(this Type type)
            => (ushort)(type.FullName.GetStableHashCode() & 0xFFFF);
    }
}