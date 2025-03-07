using System.Text.RegularExpressions;

namespace Translate.Utility;

public static class HtmlTagValidator
{
    public static bool ValidateTags(string raw, string translated, bool allowMissingColors)
    {
        HashSet<string> rawTags = ExtractTagsWithAttributes(raw);
        HashSet<string> translatedTags = ExtractTagsWithAttributes(translated);

        var response = rawTags.SetEquals(translatedTags);

        if (allowMissingColors)
        {
            rawTags.RemoveWhere(tag => tag.Contains("color"));
            translatedTags.RemoveWhere(tag => tag.Contains("color"));
            response = rawTags.SetEquals(translatedTags);
        }

        return response;
    }

    private static HashSet<string> ExtractTagsWithAttributes(string input)
    {
        var tags = new HashSet<string>();
        var regex = new Regex("<(/?\\w+[^>]*)>");
        foreach (Match match in regex.Matches(input))
        {
            tags.Add(match.Groups[1].Value);
        }
        return tags;
    }
}

