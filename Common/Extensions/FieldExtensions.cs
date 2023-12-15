using Fasterflect;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class FieldUtilities
    {
        public static FieldInfo[] GetAllFields(this Type type)
            => type.Fields(Flags.AllMembers).ToArray();

        public static FieldInfo Field(this Type type, string name)
            => FieldExtensions.Field(type, name, Flags.AllMembers);

        public static T GetValueFast<T>(this FieldInfo field)
        {
            var value = FieldInfoExtensions.Get(field);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static T GetValueFast<T>(this FieldInfo field, object target)
        {
            var value = FieldInfoExtensions.Get(field, target);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static void SetValueFast(this FieldInfo field, object value)
            => FieldInfoExtensions.Set(field, value);

        public static void SetValueFast(this FieldInfo field, object value, object target)
            => FieldInfoExtensions.Set(field, target, value);
    }
}
