﻿using Common.IO.Collections;
using Common.Utilities.Dynamic;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class PropertyExtensions
    {
        private static readonly LockedDictionary<Type, PropertyInfo[]> _properties = new LockedDictionary<Type, PropertyInfo[]>();
        private static readonly LockedDictionary<PropertyInfo, DynamicProperty> _dynamic = new LockedDictionary<PropertyInfo, DynamicProperty>();

        private static readonly BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static PropertyInfo Property(this Type type, string name, bool ignoreCase = false)
        {
            var props = GetAllProperties(type);
            return props.FirstOrDefault(p => ignoreCase ? name.ToLower() == p.Name.ToLower() : name == p.Name);
        }

        public static PropertyInfo[] GetAllProperties(this Type type)
        {
            if (_properties.TryGetValue(type, out var properties))
                return properties;

            return _properties[type] = type.GetProperties(_flags);
        }

        public static object Get(this PropertyInfo prop)
            => Get(prop, null);

        public static object Get(this PropertyInfo prop, object target)
        {
            if (!_dynamic.TryGetValue(prop, out var dynamicProperty))
                _dynamic[prop] = dynamicProperty = DynamicProperty.Create(prop);

            return dynamicProperty.GetValue(target);
        }

        public static T Get<T>(this PropertyInfo prop)
        {
            var value = Get(prop);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static T Get<T>(this PropertyInfo property, object target)
        {
            var value = Get(property, target);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static void Set(this PropertyInfo property, object value)
            => Set(property, null, value);

        public static void Set(this PropertyInfo property, object target, object value)
        {
            if (!_dynamic.TryGetValue(property, out var dynamicProperty))
                _dynamic[property] = dynamicProperty = DynamicProperty.Create(property);

            dynamicProperty.SetValue(target, value);
        }
    }
}