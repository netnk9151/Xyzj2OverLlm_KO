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
    //public static Dictionary<MethodBase, DynamicStringContract[]> ContractsToApply = [];

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

    //public static IEnumerable<CodeInstruction> ReplaceWithTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase methodBase)
    public static IEnumerable<CodeInstruction> ReplaceWithTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var textReplaced = new List<string>();

        //foreach (var replacement in ContractsToApply[methodBase])
        foreach (var replacement in ContractsToApply)
        {
            // Undo dump changes
            PrepareDynamicString(replacement.Raw, out string preparedRaw, out string preparedRaw2);
            PrepareDynamicString(replacement.Translation, out string preparedTrans, out string preparedTrans2);

            if (textReplaced.Contains(preparedRaw))
                continue;

            bool foundReplacement = false;
            bool isDangerous = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand is string operandStr)
                {
                    // Replace with the translated string
                    if (operandStr == preparedRaw)
                    {
                        // Check if the string is part of a dangerous context (e.g., method call, comparison, or jump)
                        if (IsLdstrPartOfDangerousSequence(codes, i))
                        {
                            isDangerous = true;
                            continue;  // Skip modification in dangerous contexts
                        }

                        codes[i] = new CodeInstruction(OpCodes.Ldstr, preparedTrans);
                        foundReplacement = true;
                        textReplaced.Add(preparedRaw);

                        //if (codes[i].operand is string operandStr2)
                        //    DynamicStringPatcherPlugin.Logger.LogInfo($"Changed: {replacement.Type}.{replacement.Method}  [{operandStr}] -> [{operandStr2}]");
                    }
                    else if (operandStr == preparedRaw2 
                        || StripCommas(operandStr) == StripCommas(preparedRaw))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldstr, preparedTrans2);
                        foundReplacement = true;
                        textReplaced.Add(preparedRaw); //Yes put it in preparedRaw not preparedRaw2
                    }
                }
            }

            if (!foundReplacement && !isDangerous)
                DynamicStringPatcherPlugin.Logger.LogError($"Skipped Contract: {replacement.Type}.{replacement.Method}\n{preparedRaw}\n{preparedTrans}");
            else if (!foundReplacement)
                DynamicStringPatcherPlugin.Logger.LogDebug($"Skipped Contract: {replacement.Type}.{replacement.Method}\n{preparedRaw}\n{preparedTrans}");
        }

        return codes;
    }


    // Function to check if Ldstr is part of a dangerous sequence
    private static bool IsLdstrPartOfDangerousSequence(List<CodeInstruction> codes, int index)
    {
        if (index + 1 < codes.Count)
        {
            var nextCode = codes[index + 1];

            // Check for method calls (we allow String.Format, Console.WriteLine, etc.)
            //if (nextCode.opcode == OpCodes.Call || nextCode.opcode == OpCodes.Callvirt)
            //{
            //    var method = nextCode.operand as MethodBase;
            //    if (method != null)
            //    {
            //        // List of dangerous method names to avoid
            //        var dangerousMethods = new HashSet<string>
            //        {
            //            // Add more specific dangerous methods here (e.g., any method that changes control flow)
            //        };

            //        // If the method is in the dangerous methods list, skip it
            //        if (dangerousMethods.Contains(method.ToString()))
            //        {
            //            return true;
            //        }
            //    }
            //}

            //TODO: Equality checks - Do we want them changed?
            //nextCode.opcode == OpCodes.Beq || nextCode.opcode == OpCodes.Bne_Un ||

            // Check if it's a jump instruction (e.g., conditional branch)
            if (nextCode.opcode == OpCodes.Br || nextCode.opcode == OpCodes.Switch)
            {
                return true;
            }
        }

        return false;
    }

    //public static HarmonyMethod CreateTranspilerMethod(DynamicStringContract[] contractsToApply, MethodBase methodBase)
    public static HarmonyMethod CreateTranspilerMethod(DynamicStringContract[] contractsToApply)
    {
        // Store the patch data in a static field so our transpiler can access it
        //ContractsToApply.Add(methodBase, contractsToApply);
        ContractsToApply = contractsToApply;

        var methodInfo = typeof(DynamicStringTranspiler).GetMethod("ReplaceWithTranspiler",
            BindingFlags.Public | BindingFlags.Static);

        return new HarmonyMethod(methodInfo);
    }
}