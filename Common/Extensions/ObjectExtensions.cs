using System;

namespace Common.Extensions
{
    public static class ObjectExtensions
    {
        public static T TypeCast<T>(this object value)
        {
            if (value.TryTypeCast<T>(out var cast))
                return cast;

            return default;
        }

        public static bool TryTypeCast<T>(this object value, out T castValue)
        {
            if (value is null)
            {
                castValue = default;
                return false;
            }

            if (value is T t)
            {
                castValue = t;
                return true;
            }

            castValue = default;
            return false;
        }

        public static void IfCast<T>(this object value, Action<T> action)
        {
            if (TryTypeCast<T>(value, out var castValue))
                action.Call(castValue);
        }

        public static bool IsType<T>(this object value)
            => value != null && value is T;

        public static bool IsStruct(this object value)
            => value != null && value.GetType().IsValueType;
    }
}