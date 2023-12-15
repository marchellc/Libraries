using System;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class EnumExtensions
    {
        private static readonly Dictionary<Type, Enum[]> enumValuesCache = new Dictionary<Type, Enum[]>();

        public static TEnum[] GetValues<TEnum>() where TEnum : Enum
        {
            if (enumValuesCache.TryGetValue(typeof(TEnum), out var values))
                return values.CastArray<TEnum>();
            else
            {
                values = Enum.GetValues(typeof(TEnum)).CastArray<Enum>();
                enumValuesCache[typeof(TEnum)] = values;
                return values.CastArray<TEnum>();
            }
        }

        public static Enum[] GetValues(Type type)
            => enumValuesCache.TryGetValue(type, out var values) ? values : enumValuesCache[type] = Enum.GetValues(type).CastArray<Enum>();

        public static bool Any<TEnum>(this TEnum target, TEnum value) where TEnum : struct, Enum
        {
            var typeCode = target.GetTypeCode();

            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Object:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.DBNull:
                case TypeCode.Empty:
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has an invalid type code.");

                case TypeCode.Byte: return HasByte(target, value);

                case TypeCode.Int16: return HasInt16(target, value);
                case TypeCode.Int32: return HasInt32(target, value);
                case TypeCode.Int64: return HasInt64(target, value);

                case TypeCode.UInt16: return HasUInt16(target, value);
                case TypeCode.UInt32: return HasUInt32(target, value);
                case TypeCode.UInt64: return HasUInt64(target, value);

                default:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has unsupported type code: '{typeCode}'");
            }
        }

        public static TEnum Remove<TEnum>(this TEnum target, TEnum flag) where TEnum : struct, Enum
        {
            var typeCode = target.GetTypeCode();

            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Object:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.DBNull:
                case TypeCode.Empty:
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has an invalid type code.");

                case TypeCode.Byte: return (TEnum)OperateBytes(target, flag, EnumOperation.Remove);

                case TypeCode.Int16: return (TEnum)OperateInt16(target, flag, EnumOperation.Remove);
                case TypeCode.Int32: return (TEnum)OperateInt32(target, flag, EnumOperation.Remove);
                case TypeCode.Int64: return (TEnum)OperateInt64(target, flag, EnumOperation.Remove);

                case TypeCode.UInt16: return (TEnum)OperateUInt16(target, flag, EnumOperation.Remove);
                case TypeCode.UInt32: return (TEnum)OperateUInt32(target, flag, EnumOperation.Remove);
                case TypeCode.UInt64: return (TEnum)OperateUInt64(target, flag, EnumOperation.Remove);

                default:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has unsupported type code: '{typeCode}'");
            }
        }

        public static TEnum Combine<TEnum>(this TEnum target, TEnum flag) where TEnum : struct, Enum
        {
            var typeCode = target.GetTypeCode();

            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Object:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.DBNull:
                case TypeCode.Empty:
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has an invalid type code.");

                case TypeCode.Byte: return (TEnum)OperateBytes(target, flag);

                case TypeCode.Int16: return (TEnum)OperateInt16(target, flag);
                case TypeCode.Int32: return (TEnum)OperateInt32(target, flag);
                case TypeCode.Int64: return (TEnum)OperateInt64(target, flag);

                case TypeCode.UInt16: return (TEnum)OperateUInt16(target, flag);
                case TypeCode.UInt32: return (TEnum)OperateUInt32(target, flag);
                case TypeCode.UInt64: return (TEnum)OperateUInt64(target, flag);

                default:
                    throw new ArgumentException($"Enum '{typeof(TEnum).FullName}' has unsupported type code: '{typeCode}'");
            }
        }

        public static bool HasInt16(Enum target, Enum flag)
        {
            var tValue = (short)(object)target;
            var fValue = (short)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasUInt16(Enum target, Enum flag)
        {
            var tValue = (ushort)(object)target;
            var fValue = (ushort)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasInt32(Enum target, Enum flag)
        {
            var tValue = (int)(object)target;
            var fValue = (int)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasUInt32(Enum target, Enum flag)
        {
            var tValue = (uint)(object)target;
            var fValue = (uint)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasInt64(Enum target, Enum flag)
        {
            var tValue = (long)(object)target;
            var fValue = (long)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasUInt64(Enum target, Enum flag)
        {
            var tValue = (ulong)(object)target;
            var fValue = (ulong)(object)flag;

            return (tValue & fValue) == fValue;
        }

        public static bool HasByte(Enum target, Enum flag)
        {
            var tByte = (byte)(object)target;
            var fByte = (byte)(object)flag;

            return (tByte & fByte) == fByte;
        }

        public static object OperateInt16(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (short)(object)target;
            var fValue = (short)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateInt32(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (int)(object)target;
            var fValue = (int)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateInt64(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (long)(object)target;
            var fValue = (long)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateUInt16(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (ushort)(object)target;
            var fValue = (ushort)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateUInt32(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (uint)(object)target;
            var fValue = (uint)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateUInt64(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tValue = (ulong)(object)target;
            var fValue = (ulong)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tValue | fValue) : (byte)(tValue & ~fValue);
        }

        public static object OperateBytes(Enum target, Enum flag, EnumOperation operation = EnumOperation.Combine)
        {
            var tByte = (byte)(object)target;
            var fByte = (byte)(object)flag;

            return operation is EnumOperation.Combine ? (byte)(tByte | fByte) : (byte)(tByte & ~fByte);
        }

        public enum EnumOperation
        {
            Combine,
            Remove
        }
    }
}
