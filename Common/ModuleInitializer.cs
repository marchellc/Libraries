using Common.Attributes;
using Common.Attributes.Custom;
using Common.Logging;
using Common.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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

        public static List<string> Args { get; } = new List<string>();

        public static DateTime InitializedAt { get; private set; }
        public static Assembly Assembly { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;

#if DEBUG
            IsDebugBuild = true;
#elif TRACE
            IsTraceBuild = true;
#endif

            Args.Clear();

            try
            {
                Args.AddRange(Environment.GetCommandLineArgs());
            }
            catch { }

            Assembly = Assembly.GetExecutingAssembly();

            LogOutput.Init();

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

            LogOutput.Common.Info($"Library initialized!");
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
                return cachedAppName = Path.GetFileNameWithoutExtension(proc.ProcessName);
        }
    }
}
