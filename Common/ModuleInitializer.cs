using Common.Pooling.Pools;
using Common.Logging.File;
using Common.Utilities.Exceptions;
using Common.Logging;
using Common.Utilities;
using Common.Extensions;
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

                if (!System.IO.Directory.Exists($"{appData}/Common Library"))
                    System.IO.Directory.CreateDirectory($"{appData}/Common Library");

                var appName = GetAppName();

                if (!System.IO.Directory.Exists($"{appData}/Common Library/{appName}"))
                    System.IO.Directory.CreateDirectory($"{appData}/Common Library/{appName}");

                Directory = new Directory($"{appData}/Common Library/{appName}");

                LogUtils.Default = IsDebugBuild ? LogUtils.General | LogUtils.Debug : LogUtils.General;
                LogOutput.Init();

                if (!System.IO.Directory.Exists($"{Directory.Path}/Logs"))
                    System.IO.Directory.CreateDirectory($"{Directory.Path}/Logs");

                FileLogger.Init($"{Directory.Path}/{DateTime.Now.Day}_{DateTime.Now.Month} {DateTime.Now.Hour}h {DateTime.Now.Minute}m.txt");

                ConsoleArgs.Parse(Environment.GetCommandLineArgs());

                ExceptionManager.Init();

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

                if (!ConsoleArgs.HasSwitch("DisableCommands"))
                    ConsoleCommands.Enable();

                MethodExtensions.EnableLogging = ConsoleArgs.HasSwitch("MethodLogger");

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
            catch { return cachedAppName = "Default App"; }
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
