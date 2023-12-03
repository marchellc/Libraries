using Common.IO.Collections;
using Common.Logging;
using Common.Logging.Console;
using Common.Logging.File;
using Common.Reflection;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Attributes
{
    public static class AttributeCollector
    {
        private static LogOutput log = new LogOutput("Attribute Manager").AddConsoleIfPresent().AddFileFromOutput(LogOutput.Common);
        private static LockedList<AttributeCache> cachedAttributes = new LockedList<AttributeCache>();
        private static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        public static event Action<AttributeCache> OnAdded;
        public static event Action<AttributeCache> OnRemoved;

        static AttributeCollector()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssembly;
        }

        public static IEnumerable<AttributeCache> Get<T>() where T : Attribute
            => cachedAttributes.Where(a => a.Attribute is T);

        public static IEnumerable<AttributeCache> Get<T>(MemberTypes types) where T : Attribute
            => cachedAttributes.Where(a => a.Attribute is T && types.HasFlag(a.Member.MemberType));

        public static void ForEach<T>(Action<AttributeCache, T> action) where T : Attribute
        {
            foreach (var attr in cachedAttributes)
            {
                if (attr.Attribute is not T t)
                    continue;

                action.Call(attr, t);
            }
        }

        public static void ForEach<T>(Action<AttributeCache, T> action, MemberTypes types) where T : Attribute
        {
            foreach (var attr in cachedAttributes)
            {
                if (attr.Attribute is not T t)
                    continue;

                if (!types.HasFlag(attr.Member.MemberType))
                    continue;

                action.Call(attr, t);
            }
        }

        public static void Collect()
            => Collect(Assembly.GetCallingAssembly());

        public static void Collect(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                Collect(type);
        }

        public static void Collect(Type type, object typeInstance = null)
        {
            foreach (var member in type.GetMembers(flags))
                Collect(member, typeInstance);
        }

        public static void Collect(MemberInfo member, object typeInstance = null)
        {
            if (cachedAttributes.Any(a => a.Member == member && TypeInstanceComparer.IsEqual(typeInstance, a.Instance)))
                return;

            var attributes = member.GetCustomAttributes(false);

            if (attributes.Length > 0)
            {
                if (member.DeclaringType is null || member.DeclaringType.Namespace is null)
                    return;

                if (member.DeclaringType.Namespace.StartsWith("System"))
                    return;

                if (TypeInstanceValidator.IsValid(member.DeclaringType, typeInstance) != TypeInstanceValidator.ValidatorResult.Ok)
                    return;

                for (int i = 0; i < attributes.Length; i++)
                {
                    var attributeObj = attributes[i];

                    if (attributeObj is null || attributeObj is not Attribute attribute)
                        continue;

                    var type = attribute.GetType();
                    var usage = type.GetCustomAttribute<AttributeUsageAttribute>();

                    if (usage is null)
                        continue;

                    var cached = new AttributeCache
                    {
                        Assembly = member.DeclaringType.Assembly,
                        Attribute = attribute,
                        Instance = typeInstance,
                        Member = member,
                        Target = usage.ValidOn,
                        Type = member.DeclaringType,
                        Usage = usage
                    };

                    cachedAttributes.Add(cached);

                    OnAdded.Call(cached);

                    if (attribute is AttributeResolver resolver)
                    {
                        resolver.Cache = cached;
                        resolver.OnResolved();
                    }
                }
            }
        }

        public static void Remove()
            => Remove(Assembly.GetCallingAssembly());

        public static void Remove(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                Remove(type);
        }

        public static void Remove(Type type, object typeInstance = null)
        {
            if (TypeInstanceValidator.IsValid(type, typeInstance) != TypeInstanceValidator.ValidatorResult.Ok)
                return;

            foreach (var member in type.GetMembers(flags))
                Remove(member, typeInstance);
        }

        public static void Remove(MemberInfo member, object typeInstance = null)
        {
            var removed = cachedAttributes.RemoveRange(a => a.Member == member && TypeInstanceComparer.IsEqual(typeInstance, a.Instance));

            if (removed.Count <= 0)
                return;

            foreach (var attr in removed)
            {
                OnRemoved.Call(attr);

                if (attr.Attribute is AttributeResolver resolver)
                {
                    resolver.OnRemoved();
                    resolver.Cache = default;
                }

                log.Debug($"Removed custom attribute: '{attr.Attribute.GetType().FullName}' on {attr.Member.MemberType} '{attr.Member.DeclaringType.FullName}.{attr.Member.Name}'");
            }
        }

        public static void Clear()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssembly;

            cachedAttributes.Clear();
            cachedAttributes = null;

            log = null;
        }

        private static void OnAssembly(object _, AssemblyLoadEventArgs ev)
        {
            if (ev.LoadedAssembly != null)
                Collect(ev.LoadedAssembly);
        }
    }
}