using Common.Pooling.Pools;

using Fasterflect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class MemberExtensions
    {
        private static readonly Dictionary<MemberInfo, string> PreviouslyGeneratedNames = new Dictionary<MemberInfo, string>();

        public static bool HasAttribute<TAttribute>(this MemberInfo member, out TAttribute attribute) where TAttribute : Attribute
            => (attribute = member.GetCustomAttribute<TAttribute>()) != null;

        public static bool HasAttribute<TAttribute>(this MemberInfo member) where TAttribute : Attribute
            => member != null && member.IsDefined(typeof(TAttribute));

        public static string ToName(this MemberInfo member)
        {
            if (PreviouslyGeneratedNames.TryGetValue(member, out var name))
                return name;

            if (member is MethodInfo method)
            {
                var builder = StringBuilderPool.Shared.Next();

                if (method.IsPublic)
                    builder.Append("public ");
                else if (method.IsPrivate)
                    builder.Append("private ");

                if (method.IsInstance())
                    builder.Append("instance ");
                else
                    builder.Append("static ");

                builder.Append("method ");
                builder.Append(method.ReturnType.FullName);
                builder.Append($" {method.DeclaringType.FullName}.{method.Name}");

                var methodParams = method.Parameters();

                if (methodParams.Length <= 0)
                    builder.Append("()");
                else
                {
                    builder.Append("(");

                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        if (methodParams[i].ParameterType.IsByRef)
                            builder.Append("ref ");

                        if (methodParams[i].IsOut)
                            builder.Append("out ");

                        if (methodParams[i].IsDefined(typeof(ParamArrayAttribute)))
                            builder.Append("params ");

                        builder.Append(methodParams[i].ParameterType.FullName);
                        builder.Append($" {methodParams[i].Name}");

                        if (i + 1 < methodParams.Length)
                            builder.Append(", ");
                    }

                    var str = builder.ToString();

                    builder.Clear();

                    builder.Append(str.TrimEnd(' ', ','));
                    builder.Append(")");
                }

                PreviouslyGeneratedNames[method] = StringBuilderPool.Shared.StringReturn(builder);
            }

            if (member is Type type)
                return PreviouslyGeneratedNames[type] = type.Name();

            if (member is FieldInfo field)
                return PreviouslyGeneratedNames[field] = field.IsInitOnly ? $"read-only {(field.IsStatic ? "static " : "")}field {field.FieldType.FullName} {field.DeclaringType.FullName}.{field.Name}" : $"{(field.IsStatic ? "static " : "")}field {field.FieldType.FullName} {field.DeclaringType.FullName}.{field.Name}";

            if (member is PropertyInfo property)
            {
                var builder = StringBuilderPool.Shared.Next();

                if (property.GetGetMethod(true) != null)
                    builder.Append("get");

                if (property.GetSetMethod(true) != null)
                    builder.Append(builder.Length >= 3 ? "-set" : "set");

                builder.Append(" property");
                builder.Append(property.PropertyType.FullName);
                builder.Append($" {property.DeclaringType.FullName}.{property.Name}");

                return PreviouslyGeneratedNames[property] = StringBuilderPool.Shared.StringReturn(builder);
            }

            if (member is EventInfo eventInfo)
                return PreviouslyGeneratedNames[eventInfo] = $"event {eventInfo.EventHandlerType.FullName} {eventInfo.DeclaringType.FullName}.{eventInfo.Name}";

            if (member is ConstructorInfo constructor)
                return PreviouslyGeneratedNames[constructor] = $"{(constructor.IsPublic ? "public " : "private ")}constructor {constructor.DeclaringType.FullName}({string.Join(", ", constructor.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"))})";

            return null;
        }
    }
}