using Common.Utilities.Threading;

using System;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class ThreadingExtensions
    {
        public static void Run(this IThreadManager threadManager, Action method, Action callback, bool callIfFailed = false)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, null, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess || callIfFailed)
                    callback();
            });
        }

        public static void Run<T>(this IThreadManager threadManager, Action<T> method, T value, Action callback, bool callIfFailed = false)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess || callIfFailed)
                    callback();
            });
        }

        public static void Run<T1, T2>(this IThreadManager threadManager, Action<T1, T2> method, T1 value1, T2 value2, Action callback, bool callIfFailed = false)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess || callIfFailed)
                    callback();
            });
        }

        public static void Run<T1, T2, T3>(this IThreadManager threadManager, Action<T1, T2, T3> method, T1 value1, T2 value2, T3 value3, Action callback)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3 }, threadManager, false);

            threadManager.Run(action, callback);
        }

        public static void Run<T1, T2, T3, T4>(this IThreadManager threadManager, Action<T1, T2, T3, T4> method, T1 value1, T2 value2, T3 value3, T4 value4, Action callback, bool callIfFailed = false)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3, value4 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess || callIfFailed)
                    callback();
            });
        }

        public static void Run<T>(this IThreadManager threadManager, Func<T> method, Action<T> callback, bool callWithDefaultIfFailed = true)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, null, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static void Run<T1, T>(this IThreadManager threadManager, Func<T1, T> method, T1 value1, Action<T> callback, bool callWithDefaultIfFailed = true)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static void Run<T1, T2, T>(this IThreadManager threadManager, Func<T1, T2, T> method, T1 value1, T2 value2, Action<T> callback, bool callWithDefaultIfFailed = true)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static void Run<T1, T2, T3, T>(this IThreadManager threadManager, Func<T1, T2, T3, T> method, T1 value1, T2 value2, T3 value3, Action<T> callback, bool callWithDefaultIfFailed = true)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static void Run<T1, T2, T3, T4, T>(this IThreadManager threadManager, Func<T1, T2, T3, T4, T> method, T1 value1, T2 value2, T3 value3, T4 value4, Action<T> callback, bool callWithDefaultIfFailed = true)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3, value4 }, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static void RunArgs<TDelegate>(this IThreadManager threadManager, TDelegate method, Action callback, params object[] args) where TDelegate : Delegate
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, args, threadManager, false);

            threadManager.Run(action, callback);
        }

        public static void RunArgs<TDelegate, T>(this IThreadManager threadManager, TDelegate method, Action<T> callback, bool callWithDefaultIfFailed = true, params object[] args) where TDelegate : Delegate
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, args, threadManager, false);

            threadManager.Run(action, () =>
            {
                if (action.IsSuccess)
                    callback.Call(action.GetResult<T>());
                else if (callWithDefaultIfFailed)
                    callback.Call(default);
            });
        }

        public static async Task RunAsync(this IThreadManager threadManager, Action method, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, null, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }
        }

        public static async Task RunAsync<T>(this IThreadManager threadManager, Action<T> method, T value, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;
        }

        public static async Task RunAsync<T1, T2>(this IThreadManager threadManager, Action<T1, T2> method, T1 value1, T2 value2, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;
        }

        public static async Task RunAsync<T1, T2, T3>(this IThreadManager threadManager, Action<T1, T2, T3> method, T1 value1, T2 value2, T3 value3, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;
        }

        public static async Task RunAsync<T1, T2, T3, T4>(this IThreadManager threadManager, Action<T1, T2, T3, T4> method, T1 value1, T2 value2, T3 value3, T4 value4, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3, value4 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;
        }

        public static async Task<T> RunAsync<T>(this IThreadManager threadManager, Func<T> method, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, null, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static async Task<T> RunAsync<T1, T>(this IThreadManager threadManager, Func<T1, T> method, T1 value1, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static async Task<T> RunAsync<T1, T2, T>(this IThreadManager threadManager, Func<T1, T2, T> method, T1 value1, T2 value2, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static async Task<T> RunAsync<T1, T2, T3, T>(this IThreadManager threadManager, Func<T1, T2, T3, T> method, T1 value1, T2 value2, T3 value3, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static async Task<T> RunAsync<T1, T2, T3, T4, T>(this IThreadManager threadManager, Func<T1, T2, T3, T4, T> method, T1 value1, T2 value2, T3 value3, T4 value4, int timeout = -1)
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, new object[] { value1, value2, value3, value4 }, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        throw new TimeoutException($"Method '{method.Method.ToName()}' has failed to finish executing in '{timeout}' miliseconds.");
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static async Task RunArgsAsync<TDelegate>(this IThreadManager threadManager, TDelegate method, int timeout, params object[] args) where TDelegate : Delegate
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, args, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        break;
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;
        }

        public static async Task<T> RunArgsAsync<TDelegate, T>(this IThreadManager threadManager, TDelegate method, int timeout, params object[] args) where TDelegate : Delegate
        {
            if (threadManager is null)
                throw new ArgumentNullException(nameof(threadManager));

            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!threadManager.IsRunning)
                throw new InvalidOperationException($"Thread Manager '{threadManager.GetType().FullName}' is not active!");

            var action = new ThreadAction(method.Method, method.Target, args, threadManager, false);
            var time = 0;

            threadManager.Run(action, null);

            while (!action.IsFinished)
            {
                if (timeout != -1)
                {
                    time += 10;

                    if (time >= timeout)
                        break;
                }

                await Task.Delay(10);
            }

            if (!action.IsSuccess)
                throw action.Exception;

            return action.GetResult<T>();
        }

        public static void RunTask(this IThreadManager threadManager, Task task, Action callback, bool callIfFailed = false)
            => threadManager.Run(() =>
            {
                try
                {
                    task.Start();
                }
                catch { }

                while (!task.IsFinished())
                    continue;

                if (task.Exception != null)
                    throw task.Exception;
            }, callback, callIfFailed);

        public static void RunTask<T>(this IThreadManager threadManager, Task<T> task, Action<T> callback, bool callIfFailed = false)
            => threadManager.Run(() =>
            {
                try
                {
                    task.Start();
                }
                catch { }

                while (!task.IsFinished())
                    continue;

                if (task.Exception != null)
                    throw task.Exception;

                return task.Result;
            }, callback, callIfFailed);

        public static void RunValueTask(this IThreadManager threadManager, ValueTask task, Action callback, bool callIfFailed = false)
            => RunTask(threadManager, task.AsTask(), callback, callIfFailed);

        public static void RunValueTask<T>(this IThreadManager threadManager, ValueTask<T> task, Action<T> callback, bool callIfFailed = false)
            => RunTask<T>(threadManager, task.AsTask(), callback, callIfFailed);
    }
}