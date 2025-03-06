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
        var textReplaced = new List<string>();

        foreach (var replacement in ContractsToApply)
        {
            // Undo dump changes
            var preparedRaw = replacement.Raw
                .Replace("\\r", "\r")
                .Replace("\\n", "\n");

            // Inconsistent comma use
            var preparedRaw2 = preparedRaw
                .Replace("，", ",");

            var preparedTrans = replacement.Translation
                .Replace("\\r", "\r")
                .Replace("\\n", "\n");

            // Inconsistent comma use
            var preparedTrans2 = preparedTrans
                .Replace("，", ",");

            if (textReplaced.Contains(preparedRaw))
                continue;

            bool foundReplacement = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand is string operandStr)
                {
                    // Replace with the translated string
                    if (operandStr == preparedRaw)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldstr, preparedTrans);
                        foundReplacement = true;
                        textReplaced.Add(preparedRaw);

                        //if (codes[i].operand is string operandStr2)
                        //    DynamicStringPatcherPlugin.Logger.LogInfo($"Changed: {replacement.Type}.{replacement.Method}  [{operandStr}] -> [{operandStr2}]");
                    }
                    else if (operandStr == preparedRaw2)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldstr, preparedTrans2);
                        foundReplacement = true;
                        textReplaced.Add(preparedRaw); //Yes put it in preparedRaw not preparedRaw2
                    }
                }
            }

            if (!foundReplacement)
                DynamicStringPatcherPlugin.Logger.LogError($"Skipped Contract: {replacement.Type}.{replacement.Method}.{preparedRaw}.{preparedTrans}");
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