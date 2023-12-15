using Common.Extensions;
using Common.Logging;
using Common.Utilities;

using Fasterflect;

using MonoMod.Utils;

using System;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class MethodExtensions
    {
        public static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        public static readonly LogOutput Log = new LogOutput("Method Extensions").Setup();

        public static MethodInfo ToGeneric(this MethodInfo method, Type genericType)
            => method.MakeGenericMethod(genericType);

        public static MethodInfo ToGeneric<T>(this MethodInfo method)
            => method.ToGeneric(typeof(T));

        public static MethodInfo[] GetAllMethods(this Type type)
            => type.Methods(Flags.AllMembers).ToArray();

        public static ParameterInfo[] Parameters(this MethodBase method)
            => MethodInfoExtensions.Parameters(method).ToArray();

        public static bool TryCreateDelegate<TDelegate>(this MethodBase method, object target, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !TypeInstanceValidator.IsValid(method.DeclaringType, target, false))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = method.CreateDelegate(typeof(TDelegate), target) as TDelegate;

                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{typeof(TDelegate).FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodBase method, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = method.CreateDelegate(delegateType);

                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{delegateType.FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodBase method, object target, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !TypeInstanceValidator.IsValid(method.DeclaringType, target, false))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = method.CreateDelegate(delegateType, target);

                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{delegateType.FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate<TDelegate>(this MethodBase method, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = method.CreateDelegate(typeof(TDelegate)) as TDelegate;
                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{typeof(TDelegate).FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static object Call(this MethodBase method, object instance, params object[] args)
            => Call(method, instance, LogOutput.Common.Error, args);

        public static object Call(this MethodBase method, object instance, Action<Exception> errorCallback, params object[] args)
        {
            try
            {
                return method.Invoke(instance, args);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
                return null;
            }
        }
    }
}