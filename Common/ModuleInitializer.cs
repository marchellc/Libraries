using Common.Attributes;
using Common.Attributes.Custom;
using Common.Pooling.Pools;
using Common.Logging;
using Common.Utilities;
using Common.Extensions;
using Common.IO.Data;
using Common.IO;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Threading;

namespace Common
{
    public class ModuleInitializer
    {
        private static string cachedAppName;

        public static event Action OnInitialized;
        public static event Action OnUnloaded;

        public static bool IsDebugBuild { get; private set; }
        public static bool IsTraceBuild { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static DateTime InitializedAt { get; private set; }

        public static Assembly Assembly { get; private set; }
        public static Version Version { get; private set; }

        public static Directory Directory { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized)
                return;

            try
            {
                var initStarted = DateTime.Now;

                IsInitialized = true;

#if DEBUG
                IsDebugBuild = true;
#elif TRACE
                IsTraceBuild = true;
#endif

                Assembly = Assembly.GetExecutingAssembly();
                Version = Assembly.GetName().Version;

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (!System.IO.Directory.Exists($"{appData}/CommonLib"))
                    System.IO.Directory.CreateDirectory($"{appData}/CommonLib");

                var appName = GetAppName();

                if (!System.IO.Directory.Exists($"{appData}/CommonLib/{appName}"))
                    System.IO.Directory.CreateDirectory($"{appData}/CommonLib/{appName}");

                Directory = new Directory($"{appData}/CommonLib/{appName}");

                LogUtils.Default = LogUtils.General;
                LogOutput.Init();

                ConsoleArgs.Parse(Environment.GetCommandLineArgs());

                if (IsDebugBuild || ConsoleArgs.HasSwitch("DebugLogs"))
                {
                    LogOutput.Common.Enable(LogLevel.Debug);
                    LogUtils.Default = LogUtils.General | LogUtils.Debug;
                }

                if (ConsoleArgs.HasSwitch("InvariantCulture"))
                {
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    }
                    catch { }
                }

                LogOutput.Common.Info("Initializing Attribute Manager ..");

                AttributeCollector.Init();

                if (LogUtils.IsConsoleAvailable && !ConsoleArgs.HasSwitch("DisableCommands"))
                {
                    LogOutput.Common.Info("Initializing commands ..");
                    ConsoleCommands.Enable();
                }

                LogOutput.Common.Info($"Directory: {Directory.Path}");

                MethodExtensions.EnableLogging = ConsoleArgs.HasSwitch("MethodLogger");
                DelegateExtensions.DisableFastInvoker = ConsoleArgs.HasSwitch("DisableInvoker");

                DataWriterLoader.Initialize();
                DataReaderLoader.Initialize();

                AttributeCollector.ForEach<InitAttribute>((data, attr) =>
                {
                    if (data.Member is null || data.Member is not MethodBase method)
                        return;

                    method.Call(data.Instance, ex =>
                    {
                        LogOutput.Common.Error($"Failed to invoke init-method '{method.DeclaringType.FullName}::{method.Name}':\n{ex}");
                    });
                });

                InitializedAt = DateTime.Now;

                OnInitialized.Call();

                LogOutput.Common.Info($"Library initialized (version: {Version}, time: {DateTime.Now.ToString("G")}), took {(InitializedAt - initStarted).TotalSeconds} second(s)!");
            }
            catch (Exception ex)
            {
                LogOutput.Raw(ex, ConsoleColor.Red);
            }
        }

        public static void Unload()
        {
            LogOutput.Common.Info($"Unloading library ..");

            OnUnloaded.Call();
            InitializedAt = default;

            AttributeCollector.ForEach<UnloadAttribute>((data, attr) =>
            {
                if (data.Member is null || data.Member is not MethodBase method)
                    return;

                method.Call(data.Instance, ex =>
                {
                    LogOutput.Common.Error($"Failed to invoke unload-method '{method.DeclaringType.FullName}::{method.Name}':\n{ex}");
                });
            });

            AttributeCollector.Clear();

            Assembly = null;
            cachedAppName = null;

            LogOutput.Common.Info($"Library unloaded!");

            IsInitialized = false;
        }

        public static string GetAppName()
        {
            try
            {
                if (cachedAppName != null)
                    return cachedAppName;

                var entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly != null)
                {
                    var entryName = entryAssembly.GetName();

                    if (entryName != null && !string.IsNullOrWhiteSpace(entryName.Name))
                        return cachedAppName = entryName.Name;
                }

                using (var proc = Process.GetCurrentProcess())
                    return cachedAppName = System.IO.Path.GetFileNameWithoutExtension(proc.ProcessName);
            }
            catch { return "Default App"; } 
        }

        public static Type[] SafeQueryTypes()
        {
            var assemblies = ListPool<Assembly>.Shared.Rent();

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        assemblies.Add(asm);
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                var curAsm = Assembly.GetExecutingAssembly();

                if (!assemblies.Contains(curAsm))
                    assemblies.Add(curAsm);
            }
            catch { }

            try
            {
                var callAsm = Assembly.GetCallingAssembly();

                if (callAsm != null)
                    assemblies.Add(callAsm);
            }
            catch { }

            var types = ListPool<Type>.Shared.Rent();

            try
            {
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                types.Add(type);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            ListPool<Assembly>.Shared.Return(assemblies);
            return ListPool<Type>.Shared.ToArrayReturn(types);
        }
    }
}
