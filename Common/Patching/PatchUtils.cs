using HarmonyLib;

using System.Reflection;

using Common.Extensions;
using Common.Pooling.Pools;

namespace Common.Patching
{
    public static class PatchUtils
    {
        public static CodeInstruction[] GetInstructions(MethodInfo method)
        {
            var instructions = method.GetInstructions();
            var harmonyInstructions = ListPool<CodeInstruction>.Shared.Rent();

            for (int i = 0; i < instructions.Length; i++)
                harmonyInstructions.Add(new CodeInstruction(instructions[i].Code, instructions[i].Operand));

            return ListPool<CodeInstruction>.Shared.ToArrayReturn(harmonyInstructions);
        }
    }
}