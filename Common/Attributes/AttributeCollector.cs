using Common.IO.Collections;
using Common.Logging;
using Common.Extensions;
using Common.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Attributes
{
    public static class AttributeCollector
    {
        private static readonly string[] blacklistedNamespaces = new string[]
        {
            "System.Security",
            "System.Runtime",
            "System.CompilerServices",
            "System.InteropServices",
        };

        private static readonly Type[] blacklistedTypes = new Type[]
        {
            typeof(SerializableAttribute),
            typeof(FlagsAttribute),
            typeof(ObsoleteAttribute),
            typeof(ThreadStaticAttribute),
        };

        private static LogOutput log;
        private static LockedList<AttributeCache> cachedAttributes = new LockedList<AttributeCache>();
        private static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        public static event Action<AttributeCache> OnAdded;
        public static event Action<AttributeCache> OnRemoved;

        internal static void Init()
        {
            log = new LogOutput("Attribute Manager").Setup();

            AppDomain.CurrentDomain.AssemblyLoad += OnAssembly;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                Collect(assembly);

            log.Info($"Initialized, cached {cachedAttributes.Count} attributes.");
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
            if (cachedAttributes.Any(a => a.Member == member && TypeInstanceComparer.IsEqualTo(typeInstance, a.Instance)))
                return;

            if (member.DeclaringType is null || member.DeclaringType.Namespace is null)
                return;

            if (member.DeclaringType.Namespace.StartsWith("System") || member.DeclaringType.Assembly.FullName.StartsWith("System"))
                return;

            if (member.DeclaringType.Namespace.StartsWith("Microsoft") || member.DeclaringType.Assembly.FullName.StartsWith("Microsoft"))
                return;

            if (member.DeclaringType.Assembly.FullName.Contains("mscorlib"))
                return;

            if (TypeInstanceValidator.IsValidInstance(member.DeclaringType, typeInstance) != TypeInstanceValidator.ValidatorResult.Ok)
                return;

            var attributes = member.GetCustomAttributes<Attribute>();

            foreach (var attribute in attributes)
            {
                var type = attribute.GetType();

                if (blacklistedTypes.Contains(type) || blacklistedNamespaces.Any(type.FullName.StartsWith))
                    continue;

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

        public static void Remove()
            => Remove(Assembly.GetCallingAssembly());

        public static void Remove(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                Remove(type);
        }

        public static void Remove(Type type, object typeInstance = null)
        {
            if (TypeInstanceValidator.IsValidInstance(type, typeInstance) != TypeInstanceValidator.ValidatorResult.Ok)
                return;

            foreach (var member in type.GetMembers(flags))
                Remove(member, typeInstance);
        }

        public static void Remove(MemberInfo member, object typeInstance = null)
        {
            var removed = cachedAttributes.RemoveRange(a => a.Member == member && TypeInstanceComparer.IsEqualTo(typeInstance, a.Instance));

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