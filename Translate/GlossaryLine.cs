using System.Text;

namespace Translate;

public class GlossaryLine
{
    public string Raw { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public List<string> AllowedAlternatives { get; set; } = [];
    public string Transliteration { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public bool CheckForHallucination { get; set; } = true;
    public bool CheckForMistranslation { get; set; } = true;

    public GlossaryLine()
    {
    }

    public GlossaryLine(string raw, string result)
    {
        Raw = raw;
        Result = result;
    }

    public static string AppendPromptsFor(string raw, List<GlossaryLine> glossaryLines)
    {
        var prompt = new StringBuilder();

        foreach (var line in glossaryLines)
        {
            if (raw.Contains(line.Raw))
            {
                prompt.Append($"- {line.Raw}: {line.Result}\n");

                if (line.AllowedAlternatives != null)
                    foreach(var alternative in line.AllowedAlternatives)
                        prompt.Append($"- {line.Raw}: {alternative}\n");
            }
        }

        if (prompt.Length > 0)
            return $"##### Glossary Items\n{prompt.ToString()}";
        else
            return string.Empty;
    }
}
