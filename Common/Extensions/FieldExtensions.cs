using Common.IO.Collections;
using Common.Utilities.Dynamic;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class FieldExtensions
    {
        private static readonly LockedDictionary<Type, FieldInfo[]> _fields = new LockedDictionary<Type, FieldInfo[]>();
        private static readonly LockedDictionary<FieldInfo, DynamicField> _dynamic = new LockedDictionary<FieldInfo, DynamicField>();

        private static readonly BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static FieldInfo[] GetAllFields(this Type type)
        {
            if (_fields.TryGetValue(type, out var fields))
                return fields;

            return _fields[type] = type.GetFields(_flags);
        }

        public static FieldInfo Field(this Type type, string name, bool ignoreCase = false)
            => GetAllFields(type).FirstOrDefault(f => ignoreCase ? f.Name.ToLower() == name.ToLower() : f.Name == name);

        public static object Get(this FieldInfo field)
            => Get(field, null);

        public static object Get(this FieldInfo field, object target)
        {
            if (!_dynamic.TryGetValue(field, out var dynamicField))
                _dynamic[field] = dynamicField = DynamicField.Create(field);

            return dynamicField.GetValue(target);
        }

        public static T Get<T>(this FieldInfo field)
        {
            var value = Get(field);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static T Get<T>(this FieldInfo field, object target)
        {
            var value = Get(field, target);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static void Set(this FieldInfo field, object value)
            => Set(field, null, value);

        public static void Set(this FieldInfo field, object target, object value)
        {
            if (!_dynamic.TryGetValue(field, out var dynamicField))
                _dynamic[field] = dynamicField = DynamicField.Create(field);

            dynamicField.SetValue(target, value);
        }
    }
}
