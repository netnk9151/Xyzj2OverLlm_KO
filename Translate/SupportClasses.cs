using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public class TranslatedRaw(string raw)
{
    public string Raw { get; set; } = raw;
    public string Trans { get; set; } = string.Empty;
}

public class TextFileToSplit
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string? Path { get; set; }

    public int[]? SplitIndexes { get; set; }
}

public class TranslationSplit
{
    public int Split { get; set; } = 0;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Text { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string? Translated { get; set; }

    public bool FlaggedForRetranslation { get; set; } = false;

    //public bool FlaggedForGlossaryExtraction { get; set; } = true;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedGlossaryIn { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedGlossaryOut { get; set; } = string.Empty;

    //public DateTimeOffset LastTranslated = DateTimeOffset.Now;
    public DateTime LastTranslatedOn = DateTime.Now;

    public TranslationSplit() { }

    public TranslationSplit(int split, string text)
    {
        Split = split;
        Text = text;
    }

    public void ResetFlags()
    {
        FlaggedForRetranslation = false;
        FlaggedGlossaryIn = string.Empty;
        FlaggedGlossaryOut = string.Empty;
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

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string? Translated { get; set; }

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
    public string CorrectionPrompt = string.Empty;
}