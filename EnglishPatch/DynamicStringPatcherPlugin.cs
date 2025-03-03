using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Contracts;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EnglishPatch;

/// <summary>
/// Used to replace hardcoded strings in IL
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringPatcher", "DynamicStringPatcher", MyPluginInfo.PLUGIN_VERSION)]
public class DynamicStringPatcherPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringPatcher");

        Logger.LogInfo("Dynamic String Patcher loading...");

        // Load translations from CSV
        var resourcePath = Path.Combine(Paths.BepInExRootPath, "resources");
        var filePath = Path.Combine(resourcePath, "dynamicStringsV2.txt");

        if (File.Exists(filePath))
            LoadTranslationsAndApplyPatches(filePath);
        else
            Logger.LogWarning($"Translation file not found at: {resourcePath}");
    } 

    private void LoadTranslationsAndApplyPatches(string filePath)
    {
        Logger.LogInfo($"Loading translations from: {filePath}");
        
        var deserializer = Yaml.CreateDeserializer();
        var lines = File.ReadAllText(filePath);
        var contracts = deserializer.Deserialize<List<DynamicStringContract>>(lines);

        var groupedContracts = contracts
            .Where(c => c.Translation != c.Raw)
            .GroupBy(c => c.Type)
            .Select(typeGroup => new
            {
                Type = typeGroup.Key,
                Methods = typeGroup
                    .GroupBy(c => c.Method)
                    .Select(methodGroup => new
                    {
                        Method = methodGroup.Key,
                        Contracts = methodGroup.ToList()
                    })
                    .ToList()
            })
            .ToList();

        try
        {
            Logger.LogInfo("Applying string patches...");

            foreach (var typeContract in groupedContracts)
            {
                           

                // Get the type
                Type targetType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    targetType = assembly.GetType(typeContract.Type);
                    if (targetType != null)
                        break;
                }

                if (targetType == null)
                {
                    Logger.LogError($"Could not find type: {typeContract.Type}");
                    continue;
                }

                foreach (var methodContract in typeContract.Methods)
                {
                    // Create a dynamic transpiler method
                    var transpiler = CreateTranspilerMethod(methodContract.Contracts);

                    try
                    {
                        // Find the method using AccessTools
                        MethodInfo targetMethod = null;

                        // Get all methods with this name
                        var methodsWithName = AccessTools.GetDeclaredMethods(targetType)
                            .Where(m => m.Name == methodContract.Method)
                            .ToList();

                        if (methodsWithName.Count == 1)
                        {
                            // Only one method with this name, use it
                            targetMethod = methodsWithName[0];
                        }
                        else
                        {
                            // Multiple methods with this name, find the one with the right IL
                            foreach (var method in methodsWithName)
                            {
                                // Check if this method contains our target string at the right offset
                                // You'll need to implement this using Mono.Cecil to analyze the IL
                                if (HasStringAtOffset(method, methodContract.Contracts[0].ILOffset, methodContract.Contracts[0].Raw))
                                {
                                    targetMethod = method;
                                    break;
                                }
                            }
                        }

                        if (targetMethod == null)
                        {
                            Logger.LogError($"Could not find method: {typeContract.Type}.{methodContract.Method}");
                            continue;
                        }

                        // Apply the patch
                        _harmony.Patch(targetMethod, transpiler: new HarmonyMethod(transpiler));

                        Logger.LogDebug($"Successfully patched: {typeContract.Type}.{methodContract.Method}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error patching {typeContract.Type}.{methodContract.Method}: {ex.Message}");
                    }
                }               
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading translations: {ex.Message}");
        }

        Logger.LogInfo("All patches applied");
    }

    private MethodInfo CreateTranspilerMethod(List<DynamicStringContract> contractsToApply)
    {
        // This is where the dynamic transpiler would be created
        // For simplicity, we'll create a static method that can handle multiple string replacements

        // In a real implementation, you would use a technique like DynamicMethod or reflection
        // to create a method at runtime. For this example, we'll use a static method.

        // Store the patch data in a static field so our transpiler can access it
        StringPatcherTranspiler.ContractsToApply = contractsToApply;

        return typeof(StringPatcherTranspiler).GetMethod("Transpiler",
            BindingFlags.Public | BindingFlags.Static);
    }

    private bool HasStringAtOffset(MethodInfo methodInfo, long offset, string expectedString)
    {
        try
        {
            // Get the assembly where the method is defined
            string assemblyPath = methodInfo.Module.Assembly.Location;
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            // Find the method definition in Cecil
            var typeDef = assembly.MainModule.GetType(methodInfo.DeclaringType.FullName);
            var methodDef = typeDef.Methods.FirstOrDefault(m => m.Name == methodInfo.Name);

            if (methodDef == null || !methodDef.HasBody)
                return false;

            // Check if there's a string at the right offset
            return methodDef.Body.Instructions.Any(i =>
                i.Offset == offset &&
                i.OpCode == Mono.Cecil.Cil.OpCodes.Ldstr &&
                i.Operand as string == expectedString);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error checking IL: {ex.Message}");
            return false;
        }
    }
}

// Static class to hold the transpiler method
public static class StringPatcherTranspiler
{
    // Static field to hold the current patch data
    public static List<DynamicStringContract> ContractsToApply { get; set; }

    // Transpiler method that will be used for the Harmony patch
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // If no patch data, return original instructions
        if (ContractsToApply == null || ContractsToApply == null)
            return codes;

        // Process all string replacements
        foreach (var replacement in ContractsToApply)
        {
            // Look for the string literal at the specified IL offset or by value
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand.ToString() == replacement.Raw)
                {
                    // Replace with the translated string
                    codes[i].operand = replacement.Translation;
                    break;
                }
            }
        }

        return codes;
    }
}