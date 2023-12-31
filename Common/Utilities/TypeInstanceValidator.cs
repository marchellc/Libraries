using System;

namespace Common.Utilities
{
    public static class TypeInstanceValidator
    {
        public static bool IsValid(Type type, object instance, bool onlyNotSupplied = false)
        {
            var result = IsValid(type, instance);

            if (result is ValidatorResult.Ok)
                return true;

            if (result is ValidatorResult.NotSuppliedForInstance)
                return false;

            return onlyNotSupplied;
        }

        public static ValidatorResult IsValid(Type type, object instance)
        {
            if (type.IsSealed && type.IsAbstract && instance != null)
            {
                return ValidatorResult.SuppliedForStatic;
            }

            if (!(type.IsSealed && type.IsAbstract) && instance is null)
            {
                return ValidatorResult.NotSuppliedForInstance;
            }

            if (instance != null && instance.GetType() != type)
            {
                return ValidatorResult.MismatchedType;
            }

            return ValidatorResult.Ok;
        }

        public enum ValidatorResult
        {
            SuppliedForStatic,
            NotSuppliedForInstance,
            MismatchedType,
            Ok
        }
    }
}