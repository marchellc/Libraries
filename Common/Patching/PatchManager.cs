using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Common.Patching
{
    public static class PatchManager
    {
        private static LockedList<PatchInfo> patches = new LockedList<PatchInfo>();

        public static Harmony Patcher { get; } = new Harmony($"common.patcher.{DateTime.Now.Ticks}");
        public static LogOutput Log { get; } = new LogOutput("Patch Manager").Setup();

        public static List<PatchInfo> GetPatchesOf(MethodBase target)
            => patches.Where(p => p.Target == target).ToList();

        public static List<PatchInfo> GetPatchesBy(MethodBase method)
            => patches.Where(p => p.Replacement == method).ToList();

        public static bool Patch<TDelegate>(MethodBase target, Expression<TDelegate> expression, PatchType type) where TDelegate : Delegate
            => Patch(target, (LambdaExpression)expression, type);

        public static bool Patch(MethodBase target, LambdaExpression lambdaExpression, PatchType type)
        {
            if (lambdaExpression.Body is null || lambdaExpression.Body is not MethodCallExpression callExpression)
            {
                Log.Warn($"Cannot patch method '{target}' with delegate: patch lambda expression should consist of a method call only.\n{lambdaExpression}");
                return false;
            }

            if (callExpression.Method is null)
            {
                Log.Warn($"Cannot patch method '{target}' with delegate: cannot find method called by expression.\n{lambdaExpression}");
                return false;
            }

            Log.Verbose($"Patching '{target.ToName()}' with expression:\n{lambdaExpression}");

            return Patch(target, callExpression.Method, type);
        }

        public static bool Patch(PatchInfo patch)
        {
            try
            {
                MethodInfo patchMethod = null;

                switch (patch.Type)
                {
                    case PatchType.Prefix:
                        patchMethod = Patcher.Patch(patch.Target, new HarmonyMethod(patch.Replacement), null, null, null);
                        break;

                    case PatchType.Postfix:
                        patchMethod = Patcher.Patch(patch.Target, null, new HarmonyMethod(patch.Replacement), null, null);
                        break;

                    case PatchType.Finalizer:
                        patchMethod = Patcher.Patch(patch.Target, null, null, null, new HarmonyMethod(patch.Replacement));
                        break;

                    case PatchType.Transpiler:
                        patchMethod = Patcher.Patch(patch.Target, null, null, new HarmonyMethod(patch.Replacement), null);
                        break;

                    default:
                        Log.Warn($"Cannot patch '{patch.Target.ToName()}' with '{patch.Replacement.ToName()}': unsupported patch type '{patch.Type}'");
                        return false;
                }

                if (patchMethod is null)
                {
                    Log.Warn($"Failed to patch method '{patch.Target.ToName()}' with '{patch.Replacement.ToName()}' due to an unknown error.");
                    return false;
                }

                patch.Patch = patchMethod;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed while patching method '{patch.Target.ToName()}' by '{patch.Replacement.ToName()}':\n{ex}");
                return false;
            }
        }

        public static bool Patch(MethodBase target, MethodInfo patch, PatchType type)
        {
            try
            {
                MethodInfo patchMethod = null;

                switch (type)
                {
                    case PatchType.Prefix:
                        patchMethod = Patcher.Patch(target, new HarmonyMethod(patch), null, null, null);
                        break;

                    case PatchType.Postfix:
                        patchMethod = Patcher.Patch(target, null, new HarmonyMethod(patch), null, null);
                        break;

                    case PatchType.Finalizer:
                        patchMethod = Patcher.Patch(target, null, null, null, new HarmonyMethod(patch));
                        break;

                    case PatchType.Transpiler:
                        patchMethod = Patcher.Patch(target, null, null, new HarmonyMethod(patch), null);
                        break;

                    default:
                        Log.Warn($"Cannot patch '{target.ToName()}' with '{patch.ToName()}': unsupported patch type '{type}'");
                        return false;
                }

                if (patchMethod is null)
                {
                    Log.Warn($"Failed to patch method '{target.ToName()}' with '{patch.ToName()}' due to an unknown error.");
                    return false;
                }

                patches.Add(new PatchInfo(target, patch, patchMethod, target, type));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed while patching method '{target.ToName()}' by '{patch.ToName()}':\n{ex}");
                return false;
            }
        }

        public static int PatchAll()
            => PatchAll(Assembly.GetCallingAssembly());

        public static int PatchAll(Assembly assembly)
        {
            var counter = 0;

            foreach (var type in assembly.GetTypes())
                counter += PatchAll(type);

            return counter;
        }

        public static int PatchAll(Type type)
        {
            var counter = 0;

            foreach (var method in type.GetAllMethods())
            {
                if (!method.IsStatic || !method.HasAttribute<PatchAttribute>(out var patchAttribute))
                    continue;

                if (patchAttribute.Target is null)
                {
                    Log.Warn($"Cannot apply patch '{method.ToName()}': unknown patch target");
                    continue;
                }

                if (patchAttribute.Type is PatchType.All || patchAttribute.Type is PatchType.Reverse)
                {
                    Log.Warn($"Cannot apply patch '{method.ToName()}': unsupported patch type ({patchAttribute.Type})");
                    continue;
                }

                if (!patchAttribute.IsValid)
                    continue;

                if (Patch(patchAttribute.Target, method, patchAttribute.Type))
                    counter++;
            }

            return counter;
        }

        public static int UnpatchAll()
            => UnpatchAll(Assembly.GetCallingAssembly());

        public static int UnpatchAll(Assembly assembly)
        {
            var counter = 0;

            foreach (var type in assembly.GetTypes())
                counter += UnpatchAll(type);

            return counter;
        }

        public static int UnpatchAll(Type type)
        {
            foreach (var patch in patches)
            {
                if (patch.Replacement.DeclaringType != type)
                    continue;

                try
                {
                    Patcher.Unpatch(patch.Original, patch.Patch);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to unpatch method '{patch.Target.ToName()}' (patched by '{patch.Replacement.ToName()}'):\n{ex}");
                    continue;
                }

                patch.Patch = null;
            }

            return patches.RemoveRange(p => !p.IsActive)?.Count ?? 0;
        }

        public static bool Unpatch(PatchInfo patch)
        {
            if (!patch.IsActive)
                return false;

            try
            {
                Patcher.Unpatch(patch.Original, patch.Patch);
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to unpatch method '{patch.Target.ToName()}' (patched by '{patch.Replacement.ToName()}'):\n{ex}");
                return false;
            }

            patch.Patch = null;
            return true;
        }

        public static int RemoveAllPatchesBy(MethodInfo replacement)
        {
            foreach (var patch in patches)
            {
                if (patch.Replacement != replacement)
                    continue;

                try
                {
                    Patcher.Unpatch(patch.Original, patch.Patch);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to unpatch method '{patch.Target.ToName()}' (patched by '{patch.Replacement.ToName()}'):\n{ex}");
                    continue;
                }

                patch.Patch = null;
            }

            return patches.RemoveRange(p => !p.IsActive)?.Count ?? 0;
        }

        public static int RemoveAllPatchesOf(MethodInfo targetMethod, PatchType? onlyType = null)
        {
            foreach (var patch in patches)
            {
                if (patch.Target != targetMethod || (onlyType != null && patch.Type != onlyType.Value))
                    continue;

                try
                {
                    Patcher.Unpatch(patch.Original, patch.Patch);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to unpatch method '{patch.Target.ToName()}' (patched by '{patch.Replacement.ToName()}'):\n{ex}");
                    continue;
                }

                patch.Patch = null;
            }

            return patches.RemoveRange(p => !p.IsActive)?.Count ?? 0;
        }

        public static void RemoveAllPatches()
        {
            Patcher.UnpatchAll(Patcher.Id);
            patches.Clear();
        }

        public static void Patch(MethodInfo methodInfo, Func<bool> value, PatchType prefix)
        {
            throw new NotImplementedException();
        }
    }
}