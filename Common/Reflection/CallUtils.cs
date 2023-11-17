using System;

namespace Common.Reflection
{
    public static class CallUtils
    {
        public static void Call(this Action action, Action callback = null, Action<Exception> errorCallback = null)
        {
            if (action is null)
                return;

            try
            {
                action();
                callback.Call(null, errorCallback);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }
        }

        public static void Call<TValue>(this Action<TValue> action, TValue value, Action callback = null, Action<Exception> errorCallback = null)
        {
            if (action is null)
                return;

            try
            {
                action(value);
                callback.Call(null, errorCallback);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }
        }

        public static void Call<TValue1, TValue2>(this Action<TValue1, TValue2> action, TValue1 value1, TValue2 value2, Action callback = null, Action<Exception> errorCallback = null)
        {
            if (action is null)
                return;

            try
            {
                action(value1, value2);
                callback.Call(null, errorCallback);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }
        }

        public static TResult Call<TResult>(this Func<TResult> func, Action<Exception> errorCallback = null)
        {
            if (func is null)
                return default;

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }

            return default;
        }
    }
}