﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class DelegateExtensions
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

        public static void Call<TValue1, TValue2, TValue3>(this Action<TValue1, TValue2, TValue3> action, TValue1 value1, TValue2 value2, TValue3 value3, Action callback = null, Action<Exception> errorCallback = null)
        {
            if (action is null)
                return;

            try
            {
                action(value1, value2, value3);
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

        public static TResult Call<TItem, TResult>(this Func<TItem, TResult> func, TItem item, Action<Exception> errorCallback = null)
        {
            if (func is null)
                return default;

            try
            {
                return func(item);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }

            return default;
        }

        public static TResult Call<TItem1, TItem2, TResult>(this Func<TItem1, TItem2, TResult> func, TItem1 item1, TItem2 item2, Action<Exception> errorCallback = null)
        {
            if (func is null)
                return default;

            try
            {
                return func(item1, item2);
            }
            catch (Exception ex)
            {
                errorCallback.Call(ex);
            }

            return default;
        }
    }
}
