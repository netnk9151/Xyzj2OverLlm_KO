using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

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