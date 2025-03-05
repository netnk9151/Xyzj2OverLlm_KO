using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Contracts;
using EnglishPatch.Support;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EnglishPatch;

/// <summary>
/// Used to replace hardcoded strings in IL
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringPatcher", "DynamicStringPatcher", MyPluginInfo.PLUGIN_VERSION)]
public class DynamicStringPatcherPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Harmony _harmony;
    private Dictionary<string, AssemblyDefinition> _cachedAssemblies = [];
    private Dictionary<string, Type> _cachedTypes = [];


    private void Awake()
    {
        Logger = base.Logger;
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringPatcher");

        Logger.LogInfo("Dynamic String Patcher loading...");

        // Load translations from CSV
        var resourcePath = Path.Combine(Paths.BepInExRootPath, "resources");
        var filePath = Path.Combine(resourcePath, "dynamicStringsV3.txt");

        if (File.Exists(filePath))
            LoadTranslationsAndApplyPatches(filePath);
        else
            Logger.LogWarning($"Translation file not found at: {resourcePath}");
    }


    public static List<GroupedDynamicStringContracts> GroupedDynamicStringContracts(List<DynamicStringContract> contracts)
    {
        if (contracts == null || contracts.Count == 0)
        {
            return new List<GroupedDynamicStringContracts>(); // Return an empty list if no contracts are given
        }

        return contracts
            .GroupBy(c => (c.Type, c.Method, GetParametersKey(c.Parameters)))
            .OrderBy(g => g.Key.Type)
            .ThenBy(g => g.Key.Method)
            .ThenBy(g => g.Key.Item3) // GetParametersKey result
            .Select(group => new GroupedDynamicStringContracts
            {
                Type = group.Key.Type,
                Method = group.Key.Method,
                Parameters = group.First().Parameters,
                Contracts = group.ToArray()
            })
            .ToList();
    }

    public static string GetParametersKey(string[] parameters)
    {
        return string.Join(",", parameters);
    }

    public void LoadTranslationsAndApplyPatches(string filePath)
    {
        Logger.LogInfo($"Loading translations from: {filePath}");

        try
        {
            var deserializer = Yaml.CreateDeserializer();
            var lines = File.ReadAllText(filePath);
            var contracts = deserializer.Deserialize<List<DynamicStringContract>>(lines);

            // This is bad because on overloaded functions the addresses will be different
            // we need to match the addresses and the method before grouping
            var groupedContracts = GroupedDynamicStringContracts(contracts);

            Logger.LogInfo("Applying string patches...");

            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var contract in groupedContracts)
            {
                // Get the type
                var targetType = GetTargetType(contract);

                if (targetType == null)
                {
                    Logger.LogError($"Could not find type: {contract.Type}");
                    skipCount += contract.Contracts.Length;
                    continue;
                }

                try
                {                    
                    // Find the method using different approaches based on the method type
                    MethodBase targetMethod = null;

                    // Special case for static constructors
                    if (contract.Method == ".cctor")
                    {
                        targetMethod = AccessTools.GetDeclaredConstructors(targetType)
                            .FirstOrDefault(m => m.IsStatic);

                        if (targetMethod == null)
                        {
                            Logger.LogWarning($"Could not find static constructor for: {contract.Type}");
                            skipCount++;
                            continue;
                        }
                    }
                    else if (contract.Method == ".ctor")
                    {
                        // For instance constructors, try to find the one with matching strings
                        var constructors = AccessTools.GetDeclaredConstructors(targetType)
                            .Where(m => !m.IsStatic)
                            .ToList();

                        foreach (var method in constructors)
                        {
                            if (HasMatchingParameters(contract.Parameters, method))
                            {
                                targetMethod = method;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Regular methods
                        var methods = AccessTools.GetDeclaredMethods(targetType)
                            .Where(m => m.Name == contract.Method)
                            .ToList();

                        foreach (var method in methods)
                        {
                            if (HasMatchingParameters(contract.Parameters, method))
                            {
                                targetMethod = method;
                                break;
                            }
                        }
                    }

                    if (targetMethod == null)
                    {
                        Logger.LogError($"Could not find method: {contract.Type}.{contract.Method}");
                        skipCount++;
                        continue;
                    }

                    // Apply the patch
                    _harmony.Patch(targetMethod, transpiler: StringPatcherTranspiler.CreateTranspilerMethod(contract.Contracts));
                    

                    successCount++;

                    Logger.LogInfo($"Successfully patched: {contract.Type}.{contract.Method}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Logger.LogError($"Error patching {contract.Type}.{contract.Method}: {ex.Message}");

                    // More detailed logging for debugging
                    Logger.LogDebug($"Exception details: {ex}");
                }
            }

            Logger.LogWarning($"Patching summary: {successCount} successful, {skipCount} skipped, {errorCount} errors");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading translations: {ex.Message}");
            Logger.LogDebug($"Exception details: {ex}");
        }
    }

    private Type GetTargetType(GroupedDynamicStringContracts typeContract)
    {
        if (!_cachedTypes.TryGetValue(typeContract.Type, out var targetType))
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                targetType = assembly.GetType(typeContract.Type);
                if (targetType != null)
                    break;
            }
        }

        return targetType;
    }

    private AssemblyDefinition GetAssemblyDefinition(string assemblyLocation)
    {
        if (!_cachedAssemblies.TryGetValue(assemblyLocation, out var definition))
        {
            definition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            _cachedAssemblies[assemblyLocation] = definition;
        }
        return definition;
    }

    private bool HasMatchingParameters(string[] originalParameters, MethodBase methodBase)
    {
        bool match = true;
        var methodParameters = methodBase.GetParameters().Select(p => p.ParameterType).ToArray();
        var methodParameters2 = methodBase.GetParameters().Select(p => p.ParameterType.ToString()).ToArray();

        // Remove empty parameters from deserialization
        originalParameters = originalParameters.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();

        if (originalParameters.Length == methodParameters.Length)
        {
            for (int i = 0; i < methodParameters.Length; i++)
            {
                // Check if the original parameter matches the method parameter
                if (!IsParameterMatch(originalParameters[i], methodParameters[i]))
                {
                    match = false;
                    break;
                }
            }
        }
        else
            match = false;

        //Logger.LogWarning($"Attempting to match method: {methodBase.Name}");
        //Logger.LogWarning($"Original Parameter Length: {originalParameters.Length} Method: {methodParameters.Length}");
        //Logger.LogWarning($"Original Parameters: {string.Join(", ", originalParameters)}");
        //Logger.LogWarning($"Method Parameters: {string.Join(", ", methodParameters2)}");
        //Logger.LogWarning(match);
        return match;
    }

    private bool IsParameterMatch(string originalParamType, Type methodParamType)
    {
        // Direct full name match
        if (originalParamType == methodParamType.FullName)
            return true;

        // Match simple name
        if (originalParamType == methodParamType.Name)
            return true;

        // Handle generic types
        if (methodParamType.IsGenericType)
        {
            // Compare base generic type name
            var genericTypeName = methodParamType.GetGenericTypeDefinition().Name;
            if (originalParamType.Contains(genericTypeName))
                return true;

            // Check generic type arguments
            var genericArgs = methodParamType.GetGenericArguments();
            var originalGenericParts = originalParamType.Split('`');

            if (originalGenericParts.Length > 1)
            {
                // Check if base type matches and number of generic arguments match
                if (originalGenericParts[0] == genericTypeName &&
                    genericArgs.Length == int.Parse(originalGenericParts[1]))
                    return true;
            }
        }

        // Additional fallback for partial matches
        return originalParamType.EndsWith(methodParamType.Name);
    }

    private MethodDefinition GetMethodDefinition(MethodBase methodBase)
    {
        var assembly = GetAssemblyDefinition(methodBase.Module.Assembly.Location);
        // Find the method definition in Cecil
        var typeDef = assembly.MainModule.GetType(methodBase.DeclaringType.FullName);
        if (typeDef == null)
        {
            Logger.LogWarning($"Could not find type definition: {methodBase.DeclaringType.FullName}");
            return null;
        }

        // Find the method by name and parameters
        MethodDefinition methodDef = null;
        var candidateMethods = typeDef.Methods.Where(m => m.Name == methodBase.Name).ToList();

        if (candidateMethods.Count == 1)
        {
            methodDef = candidateMethods[0];
        }
        else if (candidateMethods.Count > 1)
        {
            // Match by parameter count and types
            var paramTypes = methodBase.GetParameters().Select(p => p.ParameterType.FullName).ToArray();
            foreach (var method in candidateMethods)
            {
                if (method.Parameters.Count == paramTypes.Length)
                {
                    bool match = true;
                    for (int i = 0; i < paramTypes.Length; i++)
                    {
                        if (method.Parameters[i].ParameterType.FullName != paramTypes[i])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        methodDef = method;
                        break;
                    }
                }
            }
        }

        return methodDef;
    }    
}