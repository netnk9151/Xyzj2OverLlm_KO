using EnglishPatch.Contracts;
using HarmonyLib;
using System;
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
        var skippedOps = new List<CodeInstruction>();
        var codes = instructions.ToList();

        //var contracts = _contractsToApply;
        var contracts = ContractsToApply;

        if (!contracts.Any())
            return codes;

        foreach (var replacement in contracts)
        {
            for (int i = 0; i < codes.Count; i++)
            {
                try
                {
                    if (codes[i].opcode == OpCodes.Ldstr &&
                        codes[i].operand is string operandStr)
                    {
                        // Replace with the translated string
                        if (operandStr == replacement.Raw)
                            codes[i] = new CodeInstruction(OpCodes.Ldstr, replacement.Translation);
                        else
                            skippedOps.Add(codes[i]);
                    }
                }
                catch
                {
                    // Skip this instruction if there's a problem
                    skippedOps.Add(codes[i]);
                }
            }
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