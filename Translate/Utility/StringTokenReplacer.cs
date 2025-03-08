using System.Text.RegularExpressions;

namespace Translate.Utility;

/// <summary>
/// Replace things we know cause issues with the LLM with straight tokens which it seems to handle ok. 
/// TODO: This might be useful for markup like <color>
/// </summary>
public class StringTokenReplacer
{
    private const string PlaceholderMatchPattern = @"(\{[^{}]+\})";
    private const string CoordinateMatchPattern = @"\(-?\d+,-?\d+\)";
    private const string NumericValueMatchPattern = @"(?<![{<]|color=|<[^>]*)(?:[+-]?(?:\d+\.\d*|\.\d+|\d+))(?![}>])";
    private const string ColorStartMatchPattern = @"<color=[^>]+>";
    private const string KeyPressMatchPattern = @"<\w+\s+>";
    private const string KeyPressNoSpaceMatchPattern = @"<\w+\s+>";

    //private const string ColorEndMatchPattern = @"</color>";
    private Dictionary<int, string> placeholderMap = new();
    private Dictionary<string, string> colorMap = new();

    public string[] otherTokens = ["{}"];

    public string Replace(string input)
    {
        int index = 0;
        int colorIndex = 0;
        placeholderMap.Clear();
        colorMap.Clear();

        string result = Regex.Replace(input, PlaceholderMatchPattern, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        result = Regex.Replace(result, CoordinateMatchPattern, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        //Pull out color tags into different dictionary
        // Do not need </color> because we use same one
        result = Regex.Replace(result, ColorStartMatchPattern, match =>
        {
            string replacement = $"<color={colorIndex++}>";
            colorMap.Add(replacement, match.Value);
            return replacement;
        });

        //Key Press
        result = Regex.Replace(result, KeyPressMatchPattern, match =>
        {
            placeholderMap.Add(index, match.Value.Replace(" ", "")); //Safe because only spaces right
            return $"{{{index++}}}";
        });

        // Picks up all digits - be careful it doesnt pick it up out of special tags or markup for game
        result = Regex.Replace(result, NumericValueMatchPattern, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });        

        foreach (var token in otherTokens)
        {
            if (result.Contains(token))
            {
                result = result.Replace(token, $"{{{index}}}");
                placeholderMap.Add(index++, token);
            }
        }

        return result;
    }

    public string Restore(string input)
    {
        var result = Regex.Replace(input, PlaceholderMatchPattern, match =>
        {
            if (int.TryParse(match.Value.Trim('{', '}'), out int index)
                && placeholderMap.TryGetValue(index, out string? original))
            {
                return original;
            }
            return match.Value;
        });

        foreach (var color in colorMap)
        {
            result = result.Replace(color.Key, color.Value);
        }

        return result;
    }
}