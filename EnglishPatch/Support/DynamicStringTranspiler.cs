using EnglishPatch.Contracts;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EnglishPatch.Support;

public class DynamicStringTranspiler
{
    public static DynamicStringContract[] ContractsToApply;

    public static List<string> Calls = [];

    private static MethodInfo _stringEqualityMethod =
        typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });

    private static MethodInfo _splitMethod =
       typeof(string).GetMethod(nameof(string.Split), new Type[] { typeof(char), typeof(StringSplitOptions)});

    private static MethodInfo _newStringEqualityMethod =
        typeof(DynamicStringTranspiler).GetMethod(nameof(NewEqualityOperator), BindingFlags.Public | BindingFlags.Static);

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

    // This method will be used to handle string comparisons in switch statements
    public static bool NewEqualityOperator(string leftComparison, string rightComparison)
    {
        DynamicStringPatcherPlugin.Logger.LogFatal($"Testing Equality: [[ {leftComparison} ]] == [[ {rightComparison} ]]");

        // Direct match
        return string.Equals(leftComparison, rightComparison);
    }

    public static IEnumerable<CodeInstruction> ReplaceWithTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var rawToTranslated = new Dictionary<string, string>();

        // Build translation dictionaries
        foreach (var replacement in ContractsToApply)
        {
            PrepareDynamicString(replacement.Raw, out string preparedRaw, out string preparedRaw2);
            PrepareDynamicString(replacement.Translation, out string preparedTrans, out string preparedTrans2);

            // If we're replacing same string in method
            if (rawToTranslated.ContainsKey(preparedRaw))
                continue;

            rawToTranslated[preparedRaw] = preparedTrans;
            rawToTranslated[preparedRaw2] = preparedTrans2;
        }

        var codes = new List<CodeInstruction>(instructions);
        var textReplaced = new List<string>();

        // Step 1: Replace string constants
        for (int i = 0; i < codes.Count; i++)
        {
            //DynamicStringPatcherPlugin.Logger.LogFatal($"Old Instruction: {codes[i].opcode} {codes[i].operand}");

            if (codes[i].opcode == OpCodes.Ldstr &&
                codes[i].operand is string operandStr)
            {
                // Find corresponding translation
                string translatedStr = null;

                if (rawToTranslated.TryGetValue(operandStr, out translatedStr))
                {
                    var nextOp = i + 1;
                    var nextOp2 = i + 2;
                    var nextOp3 = i + 2;
                    var nextOp4 = i + 3;

                    if (ContractsToApply[0].Type == "MainView" && ContractsToApply[0].Method == "RefreshDateTimeText" && ContractsToApply[0].ILOffset == 24)
                    {
                        DynamicStringPatcherPlugin.Logger.LogFatal($"1: {codes[nextOp]}");
                        DynamicStringPatcherPlugin.Logger.LogFatal($"2: {codes[nextOp2]}");
                        DynamicStringPatcherPlugin.Logger.LogFatal($"3: {codes[nextOp3]}");
                        DynamicStringPatcherPlugin.Logger.LogFatal($"4: {codes[nextOp4]}");

                        continue;
                    }

                    //if (i + 1 < codes.Count && codes[i + 1].Calls(_splitMethod))
                    //{
                    //    DynamicStringPatcherPlugin.Logger.LogFatal($"Skip split instruction: {ContractsToApply[0].Type}.{ContractsToApply[0].Method}");
                    //    continue;
                    //}

                    // Create a new instruction with the translated string but preserve all metadata
                    var newInstruction = new CodeInstruction(OpCodes.Ldstr, translatedStr);
                    CopyLabelsAndBlocks(codes[i], newInstruction);
                    codes[i] = newInstruction;
                    textReplaced.Add(operandStr);
                }
            }
        }

        // Step 2: Replace string equality operations to handle switch statements
        //for (int i = 0; i < codes.Count; i++)
        //{
        //    //if (codes[i].Calls(_stringEqualityMethod))
        //    //{
        //    //    DynamicStringPatcherPlugin.Logger.LogFatal($"Replacing string equality operation: {ContractsToApply[0].Type}.{ContractsToApply[0].Method}");

        //    //    // Replace the string.op_Equality method with our enhanced version
        //    //    var newInstruction = new CodeInstruction(OpCodes.Call, _newStringEqualityMethod);
        //    //    CopyLabelsAndBlocks(codes[i], newInstruction);
        //    //    codes[i] = newInstruction;
        //    //}

            
        //}

        // Add logging to verify the final instructions
        //foreach (var code in codes)
        //    DynamicStringPatcherPlugin.Logger.LogFatal($"New Instruction: {code.opcode} {code.operand}");

        return codes;
    }

    private static void CopyLabelsAndBlocks(CodeInstruction rawInstruction, CodeInstruction newInstruction)
    {
        // Copy labels from original instruction
        if (rawInstruction.labels != null && rawInstruction.labels.Count > 0)
        {
            foreach (var label in rawInstruction.labels)
                newInstruction.labels.Add(label);
        }

        // Copy blocks from original instruction
        if (rawInstruction.blocks != null && rawInstruction.blocks.Count > 0)
        {
            foreach (var block in rawInstruction.blocks)
                newInstruction.blocks.Add(block);
        }
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