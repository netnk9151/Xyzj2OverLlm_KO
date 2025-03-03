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

namespace EnglishPatch;

/// <summary>
/// Used to replace hardcoded strings in IL
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringPatcher", "DynamicStringPatcher", MyPluginInfo.PLUGIN_VERSION)]
public class DynamicStringPatcherPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Dictionary<string, Dictionary<string, StringPatchData>> patchesByType;

    private void Awake()
    {
        Logger = base.Logger;
        patchesByType = new Dictionary<string, Dictionary<string, StringPatchData>>();

        Logger.LogInfo("Dynamic String Patcher loading...");

        // Load translations from CSV
        string translationFilePath = Path.Combine(Paths.PluginPath, "translations.csv");
        if (File.Exists(translationFilePath))
        {
            LoadTranslationsAndApplyPatches(translationFilePath);
        }
        else
        {
            Logger.LogWarning($"Translation file not found at: {translationFilePath}");
            // Generate a template with current strings
            ExportStringTemplate(Path.Combine(Paths.PluginPath, "translation_template.csv"));
        }
    }

    private void ExportStringTemplate(string outputPath)
    {
        Logger.LogInfo("Generating translation template...");

        try
        {
            string gamePath = Paths.GameRootPath;
            string assemblyPath = Path.Combine(gamePath, "Assembly-CSharp.dll");

            var stringReferences = new List<StringReference>();
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    ProcessType(type, stringReferences);
                }
            }

            using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine("Type,Method,IL Offset,Original String,Translation");

                // Write data
                foreach (var reference in stringReferences)
                {
                    // Escape quotes in the string value
                    string escapedValue = reference.StringValue.Replace("\"", "\"\"");

                    writer.WriteLine(
                        $"{reference.TypeName},{reference.MethodName},{reference.ILOffset},\"{escapedValue}\",\"\"");
                }
            }

            Logger.LogInfo($"Translation template generated at: {outputPath}");
            Logger.LogInfo("Fill in the Translation column and rename to 'translations.csv'");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error generating template: {ex.Message}");
        }
    }

    private void ProcessType(TypeDefinition type, List<StringReference> stringReferences)
    {
        // Process nested types
        foreach (var nestedType in type.NestedTypes)
        {
            ProcessType(nestedType, stringReferences);
        }

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

                    // Skip very short strings (optional)
                    if (stringValue.Length < 2)
                        continue;

                    // Add to our list
                    stringReferences.Add(new StringReference
                    {
                        TypeName = type.FullName,
                        MethodName = method.Name,
                        StringValue = stringValue,
                        ILOffset = instruction.Offset
                    });
                }
            }
        }
    }

    private void LoadTranslationsAndApplyPatches(string filePath)
    {
        Logger.LogInfo($"Loading translations from: {filePath}");
        int count = 0;

        try
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                // Skip header
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    var data = ParseCsvLine(line);

                    if (data.Count >= 5 && !string.IsNullOrWhiteSpace(data[4]))
                    {
                        string typeName = data[0];
                        string methodName = data[1];
                        int ilOffset = int.Parse(data[2]);
                        string originalString = data[3];
                        string translation = data[4];

                        // Skip if translation is empty or same as original
                        if (string.IsNullOrWhiteSpace(translation) || translation == originalString)
                            continue;

                        // Group patches by type and method
                        if (!patchesByType.ContainsKey(typeName))
                        {
                            patchesByType[typeName] = new Dictionary<string, StringPatchData>();
                        }

                        string methodKey = methodName;
                        if (!patchesByType[typeName].ContainsKey(methodKey))
                        {
                            patchesByType[typeName][methodKey] = new StringPatchData
                            {
                                MethodName = methodName,
                                StringReplacements = new List<StringReplacement>()
                            };
                        }

                        patchesByType[typeName][methodKey].StringReplacements.Add(new StringReplacement
                        {
                            ILOffset = ilOffset,
                            OriginalString = originalString,
                            TranslatedString = translation
                        });

                        count++;
                    }
                }
            }

            Logger.LogInfo($"Loaded {count} translations");

            // Apply patches for each type and method
            ApplyPatches();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading translations: {ex.Message}");
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    field.Append('"');
                    i++;
                }
                else
                {
                    // Toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of field
                result.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        // Add the last field
        result.Add(field.ToString());

        return result;
    }

    private void ApplyPatches()
    {
        Logger.LogInfo("Applying string patches...");

        // Iterate through each type
        foreach (var typeEntry in patchesByType)
        {
            string typeName = typeEntry.Key;
            var methodPatches = typeEntry.Value;

            // Get the type
            Type targetType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                targetType = assembly.GetType(typeName);
                if (targetType != null)
                    break;
            }

            if (targetType == null)
            {
                Logger.LogWarning($"Could not find type: {typeName}");
                continue;
            }

            // Apply patches for each method in this type
            foreach (var methodEntry in methodPatches)
            {
                string methodName = methodEntry.Key;
                var patchData = methodEntry.Value;

                // Create a dynamic transpiler method
                MethodInfo transpiler = CreateTranspilerMethod(patchData);

                try
                {
                    // Find the method to patch
                    MethodInfo targetMethod = targetType.GetMethod(patchData.MethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    if (targetMethod == null)
                    {
                        Logger.LogWarning($"Could not find method: {typeName}.{methodName}");
                        continue;
                    }

                    // Apply the patch
                    harmony.Patch(targetMethod,
                        transpiler: new HarmonyMethod(transpiler));

                    Logger.LogInfo($"Successfully patched: {typeName}.{methodName}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error patching {typeName}.{methodName}: {ex.Message}");
                }
            }
        }

        Logger.LogInfo("All patches applied");
    }

    private MethodInfo CreateTranspilerMethod(StringPatchData patchData)
    {
        // This is where the dynamic transpiler would be created
        // For simplicity, we'll create a static method that can handle multiple string replacements

        // In a real implementation, you would use a technique like DynamicMethod or reflection
        // to create a method at runtime. For this example, we'll use a static method.

        // Store the patch data in a static field so our transpiler can access it
        StringPatcherTranspiler.CurrentPatchData = patchData;

        return typeof(StringPatcherTranspiler).GetMethod("Transpiler",
            BindingFlags.Public | BindingFlags.Static);
    }

    private class StringReference
    {
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string StringValue { get; set; }
        public int ILOffset { get; set; }
    }

    private class StringPatchData
    {
        public string MethodName { get; set; }
        public List<StringReplacement> StringReplacements { get; set; }
    }

    private class StringReplacement
    {
        public int ILOffset { get; set; }
        public string OriginalString { get; set; }
        public string TranslatedString { get; set; }
    }
}

// Static class to hold the transpiler method
public static class StringPatcherTranspiler
{
    // Static field to hold the current patch data
    public static DynamicStringPatcherPlugin.StringPatchData CurrentPatchData { get; set; }

    // Transpiler method that will be used for the Harmony patch
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // If no patch data, return original instructions
        if (CurrentPatchData == null || CurrentPatchData.StringReplacements == null)
            return codes;

        // Process all string replacements
        foreach (var replacement in CurrentPatchData.StringReplacements)
        {
            // Look for the string literal at the specified IL offset or by value
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr &&
                    codes[i].operand.ToString() == replacement.OriginalString)
                {
                    // Replace with the translated string
                    codes[i].operand = replacement.TranslatedString;
                    break;
                }
            }
        }

        return codes;
    }
}