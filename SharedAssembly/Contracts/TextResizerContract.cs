using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SharedAssembly.Contracts;

public class TextResizerContract
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string SampleText { get; set; } = string.Empty;
    
    public float IdealFontSize { get; set; }

    public float MinFontSize { get; set; }

    public float MaxFontSize { get; set; }

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Alignment { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Overflow { get; set; } = string.Empty;

    public bool AllowWordWrap { get; set; } = true;

    public bool AllowAutoSizing { get; set; } = true;

    public bool AllowPartialPath { get; set; } = true;

    // To deal with some of the wierdness in the game
    public bool AllowLeftTrimText { get; set; } = false;

    public float AdjustX { get; set; } = 0;

    public float AdjustY { get; set; } = 0;

    public float adjustWidth { get; set; } = 0;

    public float adjustHeight { get; set; } = 0;

    public TextResizerContract ShallowClone()
    {
        return (TextResizerContract)this.MemberwiseClone();
    }
}
