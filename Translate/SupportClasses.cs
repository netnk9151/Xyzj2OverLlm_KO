using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public class TranslatedRaw(string raw)
{
    public string Raw { get; set; } = raw;
    public ValidationResult ValidationResult { get; set; } = new ValidationResult();
}

public class TextFileToSplit
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path { get; set; } = string.Empty;

    public bool PackageOutput { get; set; } = true;

    public bool ExternalAsset { get; set; } = false;

    public bool IsMainDialogueAsset { get; set; } = false;

    public bool EnableGlossary { get; set; } = true;

    public string AdditionalPromptName { get; set; } = string.Empty;

    public bool EnableBasePrompts { get; set; } = true;

    public bool RemoveNumbers { get; set; } = false;

    public bool ForceTitleCase { get; set; } = false;
}

public class TranslationSplit
{
    public int Split { get; set; } = 0;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Text { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Translated { get; set; } = string.Empty;

    public bool FlaggedForRetranslation { get; set; } = false;

    //public bool FlaggedForGlossaryExtraction { get; set; } = true;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedMistranslation { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedHallucination { get; set; } = string.Empty;

    public DateTime LastTranslatedOn = DateTime.Now;

    public TranslationSplit() { }

    public TranslationSplit(int split, string text)
    {
        Split = split;
        Text = text;
    }

    public void ResetFlags(bool translated = true)
    {
        if (translated)
            LastTranslatedOn = DateTime.Now;

        FlaggedForRetranslation = false;
        FlaggedMistranslation = string.Empty;
        FlaggedHallucination = string.Empty;
    }

    //public void ResetGlossaryFlags()
    //{
    //    FlaggedForGlossaryExtraction = true;
    //}
}

public class TranslationLine
{
    public long LineNum { get; set; } = 0;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Raw { get; set; } = string.Empty;

    [YamlIgnore]
    public string Translated { get; set; } = string.Empty;

    public List<TranslationSplit> Splits { get; set; } = [];

    public TranslationLine() { }

    public TranslationLine(int lineNum, string raw)
    {
        LineNum = lineNum;
        Raw = raw;
    }
}

public class ValidationResult
{
    public bool Valid;
    public string Result = string.Empty;
    public string CorrectionPrompt = string.Empty;

    public ValidationResult() { 
    }

    public ValidationResult(bool valid, string result)
    {
        Valid = valid;
        Result = result;
    }

    public ValidationResult(string result)
    {
        Valid = !string.IsNullOrEmpty(result);
        Result = result;
    }
}