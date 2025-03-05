using EnglishPatch.Contracts;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EnglishPatch.Support;

public class StringPatcherTranspiler
{
    public static DynamicStringContract[] ContractsToApply;

    public static IEnumerable<CodeInstruction> ReplaceWithTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        foreach (var replacement in ContractsToApply)
        {
            bool foundReplacement = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand is string operandStr)
                {
                    // Replace with the translated string
                    if (operandStr == replacement.Raw)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldstr, replacement.Translation);
                        foundReplacement = true;

                        //if (codes[i].operand is string operandStr2)
                        //    DynamicStringPatcherPlugin.Logger.LogInfo($"Changed: {replacement.Type}.{replacement.Method}  [{operandStr}] -> [{operandStr2}]");
                    }
                }
            }

            if (!foundReplacement)
                DynamicStringPatcherPlugin.Logger.LogError($"Skipped Contract: {replacement.Type}.{replacement.Method}.{replacement.Raw}.{replacement.Translation}");
        }

        return codes;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceWithTranspiler(instructions);
    }

    public static HarmonyMethod CreateTranspilerMethod(DynamicStringContract[] contractsToApply)
    {
        // Store the patch data in a static field so our transpiler can access it
        ContractsToApply = contractsToApply;

        var methodInfo = typeof(StringPatcherTranspiler).GetMethod("Transpiler",
            BindingFlags.Public | BindingFlags.Static);

        return new HarmonyMethod(methodInfo);
    }
}