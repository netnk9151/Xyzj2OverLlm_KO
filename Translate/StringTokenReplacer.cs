using System.Text.RegularExpressions;

namespace Translate;

/// <summary>
/// Replace things we know cause issues with the LLM with straight tokens which it seems to handle ok. 
/// TODO: This might be useful for markup like <color>
/// </summary>
public class StringTokenReplacer
{
    private const string PlaceholderMatchPattern = @"(\{[^{}]+\})";
    private const string CoordinateMatchPattern = @"\(-?\d+,-?\d+\)";
    private const string NumericValue = @"(?<!\{)[+-]?\d+(\.\d+)?";
    private Dictionary<int, string> placeholderMap = new();

    public string[] otherTokens = ["{}"];

    public string Replace(string input)
    {
        int index = 0;
        placeholderMap.Clear();

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

        result = Regex.Replace(result, NumericValue, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        foreach (var token in otherTokens)
        {
            result = result.Replace(token, $"{{{index}}}");
            placeholderMap.Add(index++, token);
        }

        return result;
    }

    public string Restore(string input)
    {
        return Regex.Replace(input, PlaceholderMatchPattern, match =>
        {
            if (int.TryParse(match.Value.Trim('{', '}'), out int index)
                && placeholderMap.TryGetValue(index, out string? original))
            {
                return original;
            }
            return match.Value;
        });
    }
}