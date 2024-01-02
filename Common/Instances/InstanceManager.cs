using Common.Logging;
using Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Instances
{
    public static class InstanceManager
    {
        private static readonly Dictionary<Type, Func<object>> compiledConstructors = new Dictionary<Type, Func<object>>();
        private static readonly Dictionary<Type, List<MemberInfo>> instantiationListeners = new Dictionary<Type, List<MemberInfo>>();

        private static readonly List<InstanceDescriptor> descriptors = new List<InstanceDescriptor>();

        public static event Action<InstanceDescriptor> OnCreated;

        public static LogOutput Log = new LogOutput("Instance Manager");

        internal static void Init()
        {
            Log.Setup();
            Log.Info("Initialized.");

            AppDomain.CurrentDomain.AssemblyLoad += OnAssembly;
        }

        private static void OnAssembly(object _, AssemblyLoadEventArgs ev)
            => ScanAndInstantiate(ev.LoadedAssembly);

        public static void ScanAndInstantiate()
            => ScanAndInstantiate(Assembly.GetCallingAssembly());

        public static void ScanAndInstantiate(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                ScanAndInstantiate(type);
        }

        public static void ScanAndInstantiate(Type type)
        {
            if (!type.IsStatic() && type.HasAttribute<InstantiateAttribute>())
            {
                var constructor = InstanceUtils.FindConstructor(type);

                if (constructor != null)
                {
                    var lambda = InstanceUtils.CompileConstructor(constructor);

                    if (lambda != null)
                        compiledConstructors[type] = lambda;
                    else
                        Log.Error($"Failed to compile a constructing lambda expression for type '{type.ToName()}'");
                }
                else
                    Log.Error($"Type '{type.ToName()}' is marked with the 'Instantiate' attribute, but it doesn't have any parameter-less constructors.");
            }

            instantiationListeners[type] = new List<MemberInfo>();

            var fields = type.GetAllFields();
            var props = type.GetAllProperties();

            foreach (var field in fields)
            {
                if (!field.HasAttribute<InstanceAttribute>(out var instanceAttribute))
                    continue;

                if (instanceAttribute.Types.Length > 0 && !instanceAttribute.Types.Contains(type))
                    continue;

                if (!field.IsStatic || field.IsInitOnly)
                {
                    Log.Error($"Field '{field.ToName()}' is marked as an Instance listener, but it is not static or writable.");
                    continue;
                }

                if (field.FieldType != type && !type.InheritsType(field.FieldType))
                    continue;

                instantiationListeners[type].Add(field);
            }

            foreach (var prop in props)
            {
                if (!prop.HasAttribute<InstanceAttribute>(out var instanceAttribute))
                    continue;

                if (instanceAttribute.Types.Length > 0 && !instanceAttribute.Types.Contains(type))
                    continue;

                if (!prop.CanWrite || prop.SetMethod is null || !prop.SetMethod.IsStatic)
                {
                    Log.Error($"Property '{prop.ToName()}' is marked as an Instance listener, but it is not static or writable.");
                    continue;
                }

                if ((prop.PropertyType != type && !type.InheritsType(prop.PropertyType)))
                    continue;

                instantiationListeners[type].Add(prop);
            }

            Instantiate(type);
        }

        public static void Instantiate(Type type)
        {
            if (!compiledConstructors.TryGetValue(type, out var constructor))
                return;

            if (descriptors.Any(d => d.Type == type && d.Reference.IsAlive))
                return;

            var value = constructor.Call();

            if (value is null)
                return;

            if (instantiationListeners.TryGetValue(type, out var members))
            {
                foreach (var member in members)
                {
                    if (member is FieldInfo field)
                        field.SetValue(null, value);
                    else if (member is PropertyInfo prop)
                        prop.SetValue(null, value);
                }
            }

            var descriptor = new InstanceDescriptor(value);

            descriptors.RemoveAll(d => d.Type == type);
            descriptors.Add(descriptor);

            OnCreated.Call(descriptor);
        }

        public static object Get(Type type, bool addIfMissingOrDead = true)
        {
            for (int i = 0; i < descriptors.Count; i++)
            {
                if (descriptors[i].Type == type && descriptors[i].Reference.IsAlive)
                    return descriptors[i].Reference.Value;
            }

            if (addIfMissingOrDead)
            {
                if (!compiledConstructors.ContainsKey(type))
                    ScanAndInstantiate(type);
                else
                    Instantiate(type);

                return Get(type, false);
            }

            return null;
        }
    }
}