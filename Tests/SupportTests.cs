using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Sdk;
using System.Security.Cryptography;
using Translate.Support;
using Translate.Utility;

namespace Translate.Tests;

public class SupportTests
{
    const string workingDirectory = "../../../../Files";

    public static TextFileToSplit DefaultTestTextFile() => new()
    {
        Path = "",
    };

    [Fact]
    public void GenerateTextToFiles()
    {
        //new() { Path = "achievement.txt", SplitIndexes = [] },
        var dir = new DirectoryInfo($"{workingDirectory}/Raw/SplitDb");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
            Console.WriteLine($"new() {{Path = \"{file.Name}\", 1}},");
    }

    [Fact]
    public void CheckGlossaryForDuplicates()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var duplicates = config.GlossaryLines
            .GroupBy(line => line.Raw)
            .Where(group => group.Count() > 1);

        Assert.Empty(duplicates);

        var empty = config.GlossaryLines
            .Where(l => string.IsNullOrEmpty(l.Result));

        Assert.Empty(empty);
    }

    [Fact]
    public async Task GetSectsAndPlaces()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var sects = new List<string>();
        var places = new List<string>();

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            if (outputFile.EndsWith("buildprototype.txt"))
            {
                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                    {
                        if (split.Text.Contains('-'))
                        {
                            var splits = split.Text.Split('-');
                            var sect = splits[0];
                            var place = splits[1];

                            if (!sects.Contains(sect))
                                sects.Add(sect);

                            if (!places.Contains(place))
                                places.Add(place);
                        }
                    }
                }
            }

            await Task.CompletedTask;
        });

        var glossary = new List<string>();
        foreach (var sect in sects)
        {
            // var trans = await QuickTranslate(config, sect);
            glossary.Add($"- raw: {sect}");
            glossary.Add($"  result: ");
            //glossary.Add($"  result: {trans}");
            glossary.Add($"  checkForHallucination: true");
            glossary.Add($"  checkForMistranslation: true");
        }

        glossary.Add("");

        foreach (var place in places)
        {
            //var trans = await QuickTranslate(config, place);
            glossary.Add($"- raw: {place}");
            glossary.Add($"  result: ");
            //glossary.Add($"  result: {trans}");
            glossary.Add($"  checkForHallucination: true");
            glossary.Add($"  checkForMistranslation: true");
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportGlossary.yaml", glossary);
    }

    [Fact]
    public void GetEmojiNames()
    {
        var emojiNames = new List<string>();

        var deserializer = Yaml.CreateDeserializer();
        var lines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText($"{workingDirectory}/Converted/emoji.txt"));

        foreach(var line in lines)
        {
            if (line.Splits.Count == 0)
                continue;

            var emojiCode = $"\"[{line.Splits[0].Text}]\",";

            if (!emojiNames.Contains(emojiCode))
                emojiNames.Add(emojiCode);
            
            //TODO: Token replace these and then remove emoji from the replacement list
            //First check theres nothing in Dynamic strings
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportEmoji.yaml", emojiNames);
    }

    [Fact]
    public async Task GetNames()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var sects = new List<string>();

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            if (outputFile.EndsWith("condition_group.txt"))
            {
                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                    {
                        if (split.Text.StartsWith("对话"))
                        {
                            var text = split.Text.Replace("对话", "");

                            if (!sects.Contains(text))
                                sects.Add(text);
                        }
                    }
                }
            }

            await Task.CompletedTask;
        });

        var client = new HttpClient();
        var glossary = new List<string>();
        foreach (var sect in sects)
        {
            var trans = await TranslationService.TranslateInputAsync(client, config, sect, DefaultTestTextFile(), "All provided text are names");
            trans = LineValidation.CleanupLineBeforeSaving(trans, sect, DefaultTestTextFile(), new StringTokenReplacer());
            glossary.Add($"- raw: {sect}");
            //glossary.Add($"  result: ");
            glossary.Add($"  result: {trans}");
            glossary.Add($"  badtrans: true");
        }
        File.WriteAllLines($"{workingDirectory}/TestResults/ExportGlossary.yaml", glossary);
    }

    [Fact]
    public async Task FindAllPlaceholders()
    {
        var placeholders = new List<string>();
        var pattern = LineValidation.PlaceholderMatchPattern;

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    var matches = Regex.Matches(split.Text, pattern);
                    foreach (Match match in matches)
                    {
                        if (!placeholders.Contains(match.Value))
                            placeholders.Add(match.Value);
                    }
                }
            }

            await Task.CompletedTask;
        });

        foreach (var p in placeholders)
            Console.WriteLine(p);
    }

    [Fact]
    public async Task FindAllFailingTranslations()
    {
        //var failures = new List<string>();
        var pattern = LineValidation.ChineseCharPattern;

        var forTheGlossary = new List<string>();

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    if (string.IsNullOrEmpty(split.Text))
                        continue;

                    // If it is already translated or just special characters return it
                    if (!Regex.IsMatch(split.Text, pattern))
                        continue;

                    if (!string.IsNullOrEmpty(split.Text) && string.IsNullOrEmpty(split.Translated))
                    {
                        //failures.Add($"Invalid {textFileToTranslate.Path}:\n{split.Text}");

                        if (split.Text.Length < 6)
                            if (!forTheGlossary.Contains(split.Text))
                                forTheGlossary.Add(split.Text);
                    }
                }
            }

            await Task.CompletedTask;
        });

        //File.WriteAllLines($"{workingDirectory}/TestResults/FailingTranslations.txt", failures);
        File.WriteAllLines($"{workingDirectory}/TestResults/ForManualTrans.txt", forTheGlossary);

        //await TranslateFailedLinesForManualTranslation();
    }

    [Fact]
    public void CheckFileLinesMatch()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var badFiles = new List<string>();

        foreach (var textFile in TranslationService.GetTextFilesToSplit())
        {
            var file = $"{workingDirectory}/Raw/Export/{textFile.Path}";
            var convertedFile = $"{workingDirectory}/Converted/{textFile.Path}";

            var deserializer = Yaml.CreateDeserializer();

            var lines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(file));
            var convertedLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(convertedFile)); ;

            if (lines.Count != convertedLines.Count)
                badFiles.Add($"Bad File: {Path.GetFileName(file)} Export: {lines.Count} Converted: {convertedLines.Count} ");

            Assert.Empty(badFiles);
        }
    }
}