using EnglishPatch.Contracts;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EnglishPatch.Support;

public class DynamicStringTranspiler
{
    public static DynamicStringContract[] ContractsToApply;

    // Undo dump changes
    public static void PrepareDynamicString(string input, out string prepared, out string preparedAlt)
    {
        prepared = input
            .Replace("\\r", "\r")
            .Replace("\\n", "\n");

        // Inconsistent comma use
        preparedAlt = prepared
            .Replace("，", ",");
    }

    public static string StripCommas(string input)
    {
        return input
            .Replace(",", "")
            .Replace("，", "");
    }

    public static IEnumerable<CodeInstruction> ReplaceWithTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var textReplaced = new List<string>();

        foreach (var replacement in ContractsToApply)
        {
            // Undo dump changes
            PrepareDynamicString(replacement.Raw, out string preparedRaw, out string preparedRaw2);
            PrepareDynamicString(replacement.Translation, out string preparedTrans, out string preparedTrans2);

            if (textReplaced.Contains(preparedRaw))
                continue;

            var foundReplacement = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand is string operandStr)
                {
                    // Copy all labels from the original instruction to ensure we don't lose any
                    bool matchesRaw = operandStr == preparedRaw;
                    bool matchesAlt = operandStr == preparedRaw2 || StripCommas(operandStr) == StripCommas(preparedRaw);

                    if (matchesRaw || matchesAlt)
                    {
                        // Create a new instruction with the translated string but preserve all metadata
                        var newInstruction = new CodeInstruction(OpCodes.Ldstr, matchesRaw ? preparedTrans : preparedTrans2);

                        // Copy labels from original instruction
                        if (codes[i].labels != null && codes[i].labels.Count > 0)
                        {
                            foreach (var label in codes[i].labels)
                            {
                                newInstruction.labels.Add(label);
                            }
                        }

                        // Copy blocks from original instruction
                        if (codes[i].blocks != null && codes[i].blocks.Count > 0)
                        {
                            foreach (var block in codes[i].blocks)
                            {
                                newInstruction.blocks.Add(block);
                            }
                        }

                        codes[i] = newInstruction;
                        foundReplacement = true;
                        textReplaced.Add(preparedRaw);
                    }
                }
            }

            if (!foundReplacement)
                DynamicStringPatcherPlugin.Logger.LogWarning($"No match found for: {replacement.Type}.{replacement.Method}\n{preparedRaw}\n{preparedTrans}");
        }

        return codes;
    }

    public static HarmonyMethod CreateTranspilerMethod(DynamicStringContract[] contractsToApply)
    {
        // Store the patch data in a static field so our transpiler can access it
        ContractsToApply = contractsToApply;

        var methodInfo = typeof(DynamicStringTranspiler).GetMethod("ReplaceWithTranspiler",
            BindingFlags.Public | BindingFlags.Static);

        return new HarmonyMethod(methodInfo);
    }
}