using Fasterflect;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class MemberExtensions
    {
        private static readonly Dictionary<MemberInfo, string> PreviouslyGeneratedNames = new Dictionary<MemberInfo, string>();

        public static int ToHash(this MemberInfo member)
        {
            if (member.DeclaringType != null)
                return $"{member.DeclaringType.AssemblyQualifiedName}^{member.Name}".GetStableHashCode();

            return member.Name.GetStableHashCode();
        }

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
                var str = "";

                if (method.IsPublic)
                    str += "public ";
                else if (method.IsPrivate)
                    str += "private ";

                if (method.IsInstance())
                    str += "instance ";
                else
                    str += "static ";

                str += $"method {method.ReturnType.FullName} {(method.DeclaringType != null ? $"{method.DeclaringType.FullName}." : "")}{method.Name}";

                var methodParams = method.Parameters();

                if (methodParams.Length <= 0)
                    str += "()";
                else
                {
                    str += "(";

                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        if (methodParams[i].ParameterType.IsByRef)
                            str += "ref ";

                        if (methodParams[i].IsOut)
                            str += "out ";

                        if (methodParams[i].IsDefined(typeof(ParamArrayAttribute)))
                            str += "params ";

                        str += $"{methodParams[i].ParameterType.FullName} {methodParams[i].Name}, ";
                    }

                    str = str.TrimEnd(' ', ',');
                    str += ")";
                }

                return PreviouslyGeneratedNames[method] = str;
            }

            if (member is Type type)
                return PreviouslyGeneratedNames[type] = type.Name();

            if (member is FieldInfo field)
                return PreviouslyGeneratedNames[field] = field.IsInitOnly ? $"read-only {(field.IsStatic ? "static " : "")}field {field.FieldType.FullName} {field.DeclaringType.FullName}.{field.Name}" : $"{(field.IsStatic ? "static " : "")}field {field.FieldType.FullName} {field.DeclaringType.FullName}.{field.Name}";

            if (member is PropertyInfo property)
            {
                var str = "";

                if (property.GetGetMethod(true) != null)
                    str += "get";

                if (property.GetSetMethod(true) != null)
                    str += str != "" ? "-get" : "set";

                str += $" property {property.PropertyType.FullName} {property.DeclaringType.FullName}.{property.Name}";

                return PreviouslyGeneratedNames[property] = str;
            }

            if (member is EventInfo eventInfo)
                return PreviouslyGeneratedNames[eventInfo] = $"event {eventInfo.EventHandlerType.FullName} {eventInfo.DeclaringType.FullName}.{eventInfo.Name}";

            if (member is ConstructorInfo constructor)
                return PreviouslyGeneratedNames[constructor] = $"{(constructor.IsPublic ? "public " : "private ")}constructor {constructor.DeclaringType.FullName}({string.Join(", ", constructor.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"))})";

            return null;
        }
    }
}