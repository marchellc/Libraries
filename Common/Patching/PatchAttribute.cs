using Common.Extensions;

using Fasterflect;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Patching
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PatchAttribute : Attribute
    {
        public MethodBase Target { get; }
        public PatchType Type { get; }

        public bool IsValid { get; private set; }

        public PatchAttribute(Type declaringType, string name, PatchType type, params Type[] typeArgs)
        {
            if (declaringType is null)
                throw new ArgumentNullException(nameof(declaringType));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                var propertyName = name.Remove(0, 4);
                var property = declaringType.Property(propertyName);

                if (property is null)
                    throw new Exception($"Failed to find property '{propertyName}' in type '{declaringType.FullName}' ({name})");

                var propertyMethod = name.StartsWith("get_") ? property.GetGetMethod(true) : property.GetSetMethod(true);

                if (propertyMethod is null)
                    throw new Exception($"Property '{property.ToName()}' does not have a {(name.StartsWith("get_") ? "getter" : "setter")}!");

                Type = type;
                Target = propertyMethod;
            }
            else if (name is ".cctor")
            {
                foreach (var constructor in declaringType.GetAllConstructors())
                {
                    if ((typeArgs.Length == 0 && Extensions.MethodExtensions.Parameters(constructor).Length == 0)
                        || (Extensions.MethodExtensions.Parameters(constructor).Select(p => p.ParameterType).SequenceEqual(typeArgs)))
                    {
                        Target = constructor;
                        break;
                    }
                }

                if (Target is null)
                    throw new Exception($"Failed to find a constructor of '{declaringType.FullName}' matching your type arguments");

                Type = type;
            }
            else if (name == $"~{declaringType.Name}")
            {
                var finalizer = declaringType.GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (finalizer is null)
                    throw new Exception($"Failed to find the finalizer of '{declaringType.FullName}'");

                Target = finalizer;
                Type = type;
            }
            else
            {
                foreach (var method in declaringType.GetAllMethods())
                {
                    if (method.Name == name && (typeArgs.Length == 0 && Extensions.MethodExtensions.Parameters(method).Length == 0)
                        || (Extensions.MethodExtensions.Parameters(method).Select(p => p.ParameterType).SequenceEqual(typeArgs)))
                    {
                        Target = method;
                        break;
                    }
                }

                if (Target is null)
                    throw new Exception($"Failed to find method '{name}' in '{declaringType.FullName}' matching your type arguments");

                Type = type;
            }

            IsValid = Target != null && Type != PatchType.All && Type != PatchType.Reverse;
        }
    }
}