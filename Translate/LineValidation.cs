using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Translate;

public static partial class LineValidation
{
    public const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";
    public const string PlaceholderMatchPattern = @"(\{[^{}]+\})";

    public static string PrepareRaw(string raw, StringTokenReplacer? tokenReplacer)
    {
        // Clean up the Raw string before using

        //StripColorTags(raw)
        raw = raw
            //.Replace("。", ".") //Hold off on this one for now
            .Replace("…", "...")
            //.Replace("·", ":") // Need a way to do split better or it kills to many lines
            .Replace("：", ":")
            .Replace("：", ":")
            .Replace("《", "'")
            .Replace("》", "'")
            .Replace("（", "(")
            .Replace("）", ")")
            .Replace("？", "?")
            .Replace("、", ",")
            .Replace("，", ",")
            .Replace("！", "!");

        //For testing
        if (tokenReplacer != null)
            raw = tokenReplacer.Replace(raw);

        return raw;
    }

    public static string PrepareResult(string llmResult)
    {
        // Fix up anything we know the LLM has messed up but can autocorrect before validation
        return llmResult;
    }

    public static string CleanupLineBeforeSaving(string input, string raw, TextFileToSplit textFile, StringTokenReplacer tokenReplacer)
    {
        //Finalise line before saving out
        var result = input.Trim();

        if (!string.IsNullOrEmpty(result))
        {
            if (result.Contains('\"') && !raw.Contains('\"'))
                result = result.Replace("\"", "");

            if (result.Contains('[') && !raw.Contains('['))
                result = result.Replace("[", "");

            if (result.Contains(']') && !raw.Contains(']'))
                result = result.Replace("]", "");

            if (result.Contains('`') && !raw.Contains('`'))
                result = result.Replace("`", "'");

            // Take out wide quotes
            if (result.Contains('“') && !raw.Contains('“'))
                result = result.Replace("“", "");

            if (result.Contains('”') && !raw.Contains('”'))
                result = result.Replace("”", "");

            result = result
                .Replace("…", "...")
                .Replace("？", "?")
                .Replace(".:", ":")
                .Replace(". -", " -")
                .Replace("！", "!");

            //Take out wide quotes
            result = result
                .Replace("’", "'")
                .Replace("‘", "'");

            //Strip .'s
            //if (result.EndsWith('.') && !raw.EndsWith(".") && !result.EndsWith(".."))
            //    result = result[..^1];

            if (textFile.RemoveNumbers)
                result = RemoveNumbers(result);

            if (textFile.NameCleanupRoutines)
            {
                result = result.Replace(" ", "")
                    .Replace(".", "");

                result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
            }

            result = RemoveDiacritics(result);
            result = ReplaceIncorrectLowercaseWords(result);

            result = EncaseColorsForWholeLines(raw, result);
            result = EncaseSquareBracketsForWholeLines(raw, result);


            if (!textFile.RemoveExtraFullStop)
                result = RemoveFullStop(raw, result);

            if (string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"Something Bad happened somewhere: {raw}\n{result}");
                return result;
            }

            if (result.StartsWith('\'') && result.EndsWith('\''))
                if (result.Length > 3)
                    result = result[1..^1];

            if (Char.IsLower(result[0]) && raw != result)
                result = Char.ToUpper(result[0]) + result[1..];
        }

        result = tokenReplacer.Restore(result);

        return result;
    }

    public static ValidationResult CheckTransalationSuccessful(LlmConfig config, string raw, string result, TextFileToSplit textFile)
    {
        var response = true;
        var correctionPrompts = new StringBuilder();

        if (string.IsNullOrEmpty(raw))
            response = false;        

        // Didnt translate at all and default response to prompt.
        if (result.Contains("provide the text")
            || result.Contains("translates to")
            || result.Contains("'''") 
            || result.Contains("<p")
            || result.Contains("</p")
            || result.Contains("<em")
            || result.Contains("</em")
            || result.Contains("<|")
            || result.Contains("<strong")
            || result.Contains("</strong")
            //|| result.Contains("–")
            || result.Contains("\\U"))
            response = false;

        // 99% chance its gone crazy with hallucinations
        if (result.Length > 50 && raw.Length <= 4)
            response = false;

        // Small source with 'or' is ususually an alternative
        if ((result.Contains(" or") || result.Contains("(or")) && raw.Length <= 4)
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "or");
        }

        // Small source with 'and' is ususually an alternative
        //if (result.Contains(" and") && raw.Length < 3 && !result.Contains("Spear and Staff", StringComparison.OrdinalIgnoreCase))
        //{
        //    response = false;
        //    correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "and");
        //}

        // Small source with ';' is ususually an alternative
        if (result.Contains(';') && !raw.Contains(';') && raw.Length < 3)
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", ";");
        }       

        // Added literal
        if (result.Contains("(lit."))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectExplainationPrompt");
        }

        // Removed :
        if (raw.Contains(':') && !result.Contains(':'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectColonSegementPrompt");
        }

        // TODO: This aint working
        //Place holders - incase the model ditched them
        var matches = PlaceholderPatternRegex().Matches(raw);
        foreach (Match match in matches)
        {
            if (!result.Contains(match.Value))
            {
                response = false;
                correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", match.Value);
            }
        }

        if (raw.Contains("\\n") && !result.Contains("\\n"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "\\n");
        }

        if (raw.Contains('-')  && !result.Contains('-'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "-");
        }

        // This can cause bad hallucinations if not being explicit on retries
        if (raw.Contains("<br>") && !result.Contains("<br>"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "<br>");
            //correctionPrompts.AddPromptWithValues(config, "CorrectTagPrompt");
        }
        // Color tags are evil
        //else if (raw.Contains("<color") && !result.Contains("<color"))
        //{
        //    response = false;
        //    correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "<color>");
        //    correctionPrompts.AddPromptWithValues(config, "CorrectTagPrompt");
        //}                

        // Some raws dont have both because they are dynamic strings
        // Color invalidation - if it has a start tag but no end tag
        if (result.Contains("<color") && raw.Contains("</color>") && !result.Contains("</color>"))
        {
            response = false;
        }
        // Color invalidation - if it has a end tag but no start tag
        if (result.Contains("</color") && raw.Contains("<color") && !result.Contains("<color"))
        {
            response = false;
        }

        // Random additions
        if (result.Contains("<br>") && !raw.Contains("<br>"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAdditionalPrompt", "<br>");
        }

        if (result.Contains('\n') && !raw.Contains('\n'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAdditionalPrompt", "\\n");
        }

        if (Regex.IsMatch(result, ChineseCharPattern))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectChinesePrompt");
        }

        // Dialog specific
        // Added Brackets (Literation) where no brackets or widebrackets in raw
        if (result.Contains('(') && !raw.Contains('(') && !raw.Contains('（'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectExplainationPrompt");
        }

        ////Alternatives
        if (result.Contains('/') && !raw.Contains('/'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "/");
        }

        if (result.Contains('\\') && !raw.Contains('\\'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "\\");
        }

        //TODO: This is doing wierd shit
        if (result.Contains('<') && !result.Contains("<br>") 
            && !result.Contains("<color") && !result.Contains("</color"))
        {
            // Check markup
            var markup = FindMarkup(raw);
            if (markup.Count > 0)
            {
                var resultMarkup = FindMarkup(result);
                if (resultMarkup.Count != markup.Count)
                {
                    response = false;
                    correctionPrompts.AddPromptWithValues(config, "CorrectTagPrompt");
                }
            }
        }

        if (textFile.NameCleanupRoutines)
        {
            if ((raw.Length == 1 && result.Length > 6)
                || (raw.Length == 2 && result.Length > 12)
                || (raw.Length == 3 && result.Length > 17))
                response = false;
        }

        return new ValidationResult
        {
            Valid = response,
            Result = result,
            CorrectionPrompt = correctionPrompts.ToString(),
        };
    }

    public static string CalulateCorrectionPrompt(LlmConfig config, ValidationResult validationResult, string raw, string result)
    {
        return string.Format(config.Prompts["BaseCorrectionPrompt"], raw, result, validationResult.CorrectionPrompt); ;
    }

    public static List<string> FindMarkup(string input)
    {
        var markupTags = new List<string>();

        if (input == null)
            return markupTags;

        // Regular expression to match markup tags in the format <tag>
        string pattern = "<[^>]+>";
        MatchCollection matches = Regex.Matches(input, pattern);

        // Add each match to the list of markup tags
        foreach (Match match in matches)
            markupTags.Add(match.Value);

        return markupTags;
    }

    public static List<string> FindPlaceholders(string input)
    {
        var placeholders = new List<string>();

        if (input == null)
            return placeholders;

        // Regular expression to match placeholders in the format {number}
        string pattern = "\\{.+\\}";
        MatchCollection matches = Regex.Matches(input, pattern);

        // Add each match to the list of placeholders
        foreach (Match match in matches)
            placeholders.Add(match.Value);

        return placeholders;
    }

    public static string ConvertColorTagsToPlaceholderTags(string input)
    {
        // Regex to match <color> tags and capture their contents and attributes
        string pattern = @"<color=(#[0-9A-Fa-f]{6})>(.*?)<\/color>";
        string replacement = "<mark style=\"color: $1;\">$2</mark>";

        // Replace <color> tags with <font> tags
        string result = Regex.Replace(input, pattern, replacement, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        return result;
    }

    public static string EncaseColorsForWholeLines(string raw, string translated)
    {
        var pattern = @"(<[^>]+>).*(</[^>]+>)";

        if (raw.StartsWith("<color") && raw.EndsWith("</color>")
            && raw.LastIndexOf("<color") == 0 && !translated.StartsWith("<color"))
        {
            var matches = Regex.Matches(raw, pattern);
            string start = matches[0].Groups[1].Value;
            string end = matches[0].Groups[2].Value;
            translated = $"{start}{translated}{end}";
        }

        return translated;
    }

    public static string EncaseSquareBracketsForWholeLines(string raw, string translated)
    {
        if (raw.StartsWith('【')
            && raw.EndsWith('】')
            && !translated.Contains('【')
            && !translated.Contains('】'))
        {
            translated = $"【{translated}】";
        }

        return translated;
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string ReplaceIncorrectLowercaseWords(string input)
    {
        var words = new Dictionary<string, string>
        {
            { "jianghu", "Jianghu" },
            { "wulin", "Wulin"  }
        };

        foreach (var word in words)
        {
            string pattern = $"\\b{word.Key}\\b"; // \b ensures 'jianghu' is a whole word
            input = Regex.Replace(input, pattern, word.Value);
        }

        return input;
    }

    public static string RemoveNumbers(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove all digits from the string
        return Regex.Replace(input, @"\d", "");
    }

    public static string RemoveFullStop(string raw, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) 
            return input;

        if (raw.Contains(' '))
            return input;

        var fullStop = '.';

        // Check if there's only one sentence (one full stop at the end)
        if (input.Count(c => c == fullStop) == 1 && input.TrimEnd().EndsWith(fullStop))
        {
            // Count words
            var words = input.TrimEnd(fullStop).
                Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 4)
            {
                return input.Replace(fullStop.ToString(), string.Empty); // Remove full stop leaving spaces
            }
        }

        return input;
    }

    [GeneratedRegex(PlaceholderMatchPattern)]
    private static partial Regex PlaceholderPatternRegex();
}
