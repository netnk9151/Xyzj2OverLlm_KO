using BepInEx.Logging;
using EnglishPatch.Contracts;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EnglishPatch.Support;

public static class RuntimeStringInterceptor
{
    private readonly static Dictionary<string, Dictionary<string, string>> _stringReplacements = [];
    private static ManualLogSource Logger => DynamicStringPatcherPlugin.Logger;
    private static bool _initialized = false;

    /// <summary>
    /// Initialize the runtime string interception system
    /// </summary>
    public static void Initialize(List<GroupedDynamicStringContracts> contracts, Harmony harmony)
    {
        if (_initialized)
            return;

        Logger.LogInfo("Setting up runtime string interception...");

        // Process contracts and setup replacements
        foreach (var contract in contracts)
            RegisterStringReplacements(contract.Type, contract.Contracts);

        // Patch string accessor methods
        PatchStringMethods(harmony);

        _initialized = true;
        Logger.LogInfo($"Runtime string interception setup complete. Monitoring {_stringReplacements.Count} types for string replacements.");
    }

    private static void RegisterStringReplacements(string typeName, DynamicStringContract[] contracts)
    {
        if (!_stringReplacements.TryGetValue(typeName, out var typeReplacements))
        {
            typeReplacements = [];
            _stringReplacements[typeName] = typeReplacements;
        }

        foreach (var stringContract in contracts)
        {
            if (!string.IsNullOrEmpty(stringContract.Raw) && !string.IsNullOrEmpty(stringContract.Translation))
            {
                DynamicStringTranspiler.PrepareDynamicString(stringContract.Raw, out string preparedRaw, out string preparedRaw2);
                DynamicStringTranspiler.PrepareDynamicString(stringContract.Translation, out string preparedTrans, out string preparedTrans2);
                var stripped = DynamicStringTranspiler.StripCommas(stringContract.Raw);
                
                typeReplacements[preparedRaw] = preparedTrans;
                typeReplacements[preparedRaw2] = preparedTrans2;
                typeReplacements[stripped] = preparedTrans2; //TODO: Not Implemented
            }
        }
    }

    private static void PatchStringMethods(Harmony harmony)
    {
        // Patch core string methods that are likely to be used for accessing strings
        // This will intercept string constants at the point they're used rather than when they're defined

        //// Patch string.Concat methods
        var concatMethods = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "Concat")
            .ToList();

        foreach (var method in concatMethods)
        {
            try
            {
                harmony.Patch(
                    method,
                    prefix: new HarmonyMethod(typeof(RuntimeStringInterceptor), nameof(StringConcatPrefix))
                );
                Logger.LogDebug($"Patched string.Concat method with {method.GetParameters().Length} parameters");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to patch string.Concat method: {ex.Message}");
            }
        }

        // Patch string constructor methods
        var stringCtors = typeof(string).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        foreach (var ctor in stringCtors)
        {
            try
            {
                harmony.Patch(
                    ctor,
                    prefix: new HarmonyMethod(typeof(RuntimeStringInterceptor), nameof(StringConstructorPrefix))
                );
                Logger.LogDebug($"Patched string constructor with {ctor.GetParameters().Length} parameters");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to patch string constructor: {ex.Message}");
            }
        }

        // Optionally, you could also patch other string methods like Format, Join, etc.
    }


    // Prefix for string.Concat methods
    public static bool StringConcatPrefix(MethodBase __originalMethod, ref object __result, object[] __args)
    {
        try
        {
            // Get the stack trace to find the calling method
            var stackFrame = new System.Diagnostics.StackFrame(2, false);
            var callingMethod = stackFrame.GetMethod();
            if (callingMethod != null && callingMethod.DeclaringType != null)
            {
                string callingTypeName = callingMethod.DeclaringType.FullName;

                // Check if we have replacements for this type
                if (_stringReplacements.TryGetValue(callingTypeName, out var replacements))
                {
                    bool anyReplaced = false;

                    // Process each argument that is a string
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is string str && replacements.TryGetValue(str, out string replacement))
                        {
                            __args[i] = replacement;
                            anyReplaced = true;
                            Logger.LogInfo($"Replaced string in {callingMethod.Name}: '{str}' -> '{replacement}'");
                        }
                    }

                    // If we replaced any strings, we need to call the original method with the new arguments
                    if (anyReplaced)
                    {
                        __result = __originalMethod.Invoke(null, __args);
                        return false; // Skip the original method
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in string.Concat prefix: {ex.Message}");
        }

        return true; // Continue with the original method
    }

    // Prefix for string constructors - MethodBase __originalMethod
    public static bool StringConstructorPrefix(object[] __args)
    {
        try
        {
            // Get the stack trace to find the calling method
            var stackFrame = new System.Diagnostics.StackFrame(2, false);
            var callingMethod = stackFrame.GetMethod();
            if (callingMethod != null && callingMethod.DeclaringType != null)
            {
                string callingTypeName = callingMethod.DeclaringType.FullName;

                // Check if we have replacements for this type
                if (_stringReplacements.TryGetValue(callingTypeName, out var replacements))
                {
                    bool anyReplaced = false;

                    // Process each argument that is a string
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is string str && replacements.TryGetValue(str, out string replacement))
                        {
                            __args[i] = replacement;
                            anyReplaced = true;
                            Logger.LogInfo($"Replaced string in constructor call from {callingMethod.Name}: '{str}' -> '{replacement}'");
                        }
                    }

                    // For constructors, we still need to let the original run with the modified args
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in string constructor prefix: {ex.Message}");
        }

        return true; // Continue with the original method
    }


    // Helper method to intercept string literals - can be called from other parts of your code
    public static string InterceptString(string original, Type type)
    {
        if (string.IsNullOrEmpty(original) || type == null)
            return original;

        if (_stringReplacements.TryGetValue(type.FullName, out var replacements) &&
            replacements.TryGetValue(original, out string replacement))
        {
            return replacement;
        }

        return original;
    }
}