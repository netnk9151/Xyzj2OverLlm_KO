using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text.RegularExpressions;
using EnglishPatch.Contracts;
using System.Runtime.CompilerServices;

namespace EnglishPatch.Dumpers;

/// <summary>
/// Used to replace hardcoded strings in IL
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringDumperPlugin", "DynamicStringDumperPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class DynamicStringDumperPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogError("Dynamic String Dumper loading...");

        //Disable Dumper for non devs
        // Load translations from CSV
        //var resourcePath = Path.Combine(Paths.BepInExRootPath, "resources");
        //var dumpFilePath = Path.Combine(resourcePath, "dynamicstrings-dump.txt");

        //// Generate a template with current strings
        //if (!File.Exists(dumpFilePath))
        //    DumpFiles(dumpFilePath);
        //else
        //    Logger.LogError("Dump already exists...");
    }

    public void DumpFiles(string outputPath)
    {
        Logger.LogError("Dumping dynamic strings...");

        try
        {
            //!!! Manually copy YamlDotNet.dll to the ManagedDlls folder
            var serializer = Yaml.CreateSerializer();
            string gamePath = Paths.ManagedPath;
            string assemblyPath = Path.Combine(gamePath, "Assembly-CSharp.dll");

            var contracts = new List<DynamicStringContract>();
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            int count = 0;

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    ProcessType(type, contracts);
                    count++;
                }
            }

            //YAML is too hard to handle when splitting
            //File.WriteAllText(outputPath, serializer.Serialize(contracts));

            var lines = new List<string>();
            foreach (var contract in contracts)
                lines.Add($"{CleanForCsv(contract.Type)},{CleanForCsv(contract.Method)},{CleanForCsv(contract.ILOffset.ToString())},{CleanForCsv(contract.Raw)},");

            File.WriteAllLines(outputPath, lines);

            Logger.LogWarning($"Dumped to: {outputPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error dumping: {ex.Message}");
        }
    }

    public string CleanForCsv(string input)
    {
        return input.Replace(",", "，")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private void ProcessType(TypeDefinition type, List<DynamicStringContract> stringReferences, int recursionLevel = 0)
    {
        // Process nested types
        foreach (var nestedType in type.NestedTypes)
            if (!stringReferences.Any(r => r.Type == nestedType.ToString()) && recursionLevel < 15) //15 should be fine
                ProcessType(nestedType, stringReferences, recursionLevel++);

        // Process methods
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                // Look for string load operations
                if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string stringValue)
                {
                    // Skip empty strings and strings with just whitespace
                    if (string.IsNullOrWhiteSpace(stringValue))
                        continue;

                    // Skip non chinese strings
                    if (!Regex.IsMatch(stringValue, MainPlugin.ChineseCharPattern))
                        continue;

                    // Add to our list
                    stringReferences.Add(new DynamicStringContract
                    {
                        Type = type.FullName,
                        Method = method.Name,
                        Raw = stringValue,
                        ILOffset = instruction.Offset
                    });
                }
            }
        }
    }
}
