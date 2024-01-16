using System.Reflection;

namespace Common.Patching
{
    public class PatchInfo
    {
        public MethodInfo Replacement { get; }
        public MethodInfo Patch { get; }

        public MethodBase Target { get; }
        public MethodBase Original { get; }

        public PatchType Type { get; }

        public bool IsActive { get; set; }

        public PatchInfo(MethodBase target, MethodInfo replacement, MethodInfo patch, MethodBase original, PatchType type)
        {
            Target = target;
            Replacement = replacement;
            Patch = patch;
            Original = original;
            Type = type;
        }
    }
}