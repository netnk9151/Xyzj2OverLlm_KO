using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SharedAssembly.Contracts;

public class TextResizerContract
{
    // Text Component Details

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string SampleText { get; set; } = string.Empty;
    
    public float? IdealFontSize { get; set; }

    public float? MinFontSize { get; set; }

    public float? MaxFontSize { get; set; }

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Alignment { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string OverflowMode { get; set; } = string.Empty;

    public bool? AllowWordWrap { get; set; }

    public bool? AllowAutoSizing { get; set; }

    // Custom Flags

    public bool AllowPartialPath { get; set; }

    // To deal with some of the wierdness in the game
    public bool AllowLeftTrimText { get; set; } = false;

    public bool PathIsRegex { get; set; } = false;

    public float AdjustX { get; set; }

    public float AdjustY { get; set; }

    public float AdjustWidth { get; set; }

    public float AdjustHeight { get; set; }

    public TextResizerContract ShallowClone()
    {
        return (TextResizerContract)this.MemberwiseClone();
    }
}
