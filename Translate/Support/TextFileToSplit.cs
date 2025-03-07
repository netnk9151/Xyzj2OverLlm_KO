using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public enum TextFileType
{
    RegularDb,
    PrefabText,
    DynamicStrings,
    DynamicStringsV2
}

public class TextFileToSplit
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path { get; set; } = string.Empty;

    public bool PackageOutput { get; set; } = true;

    public TextFileType TextFileType { get; set; } = TextFileType.RegularDb;

    public bool IsMainDialogueAsset { get; set; } = false;

    public bool EnableGlossary { get; set; } = true;

    public string AdditionalPromptName { get; set; } = string.Empty;

    public bool EnableBasePrompts { get; set; } = true;

    public bool RemoveNumbers { get; set; } = false;

    public bool RemoveExtraFullStop { get; set; } = true;
    
    public bool NameCleanupRoutines { get; set; } = false;

    public bool AllowMissingColorTags { get; set; } = true;
}
