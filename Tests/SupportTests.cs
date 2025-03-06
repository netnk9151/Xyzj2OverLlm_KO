using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Sdk;
using System.Security.Cryptography;

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
        foreach(var sect in sects)
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

    [Theory]
    [InlineData("A.Hello name, welcome to place!")]
    [InlineData("B.Hello {name}, welcome to {place}!")]
    [InlineData("C.与{E}交谈（{IsCanFinish:0:1}/1)")]
    [InlineData("D.击败目标点{GetZhiYingTargetPos}的{E}（{NeedKillNpcItemsCount}/{N})")]
    [InlineData("E.击败目标点{GetZhiYingTargetPos}的{E} \\n {NeedKillNpcItemsCount}/{N})")]
    [InlineData("F.各家学说，各抒己见，两两之间，总有克制。\\n强克制：对目标伤害提升0.5倍。被强克制：对目标伤害降低0.5倍。\\n强克制关系：道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制：对目标伤害提升0.25倍。被弱克制：对目标伤害降低0.25倍。\\n弱克制关系：道学→儒学→墨学；佛学→魔学→农学。")]
    [InlineData("G.正有事找你，前些日子{}特意送来好礼，如今也该是回礼的日子了，你拿上此物交给{}事务总管，事成之后门中会奖励一枚不夜京承渝令。")]
    [InlineData("H.天随人愿，历经千辛万苦，终于在{0}发现了{1}，可谓福气满满。")]
    [InlineData("I.覆灭穆特前线的所有穆特族（{IsCanFinish:0:1}/1）")]
    [InlineData("J.到达目标点{GetZhiYingTargetPos}({IsCanFinish:0:1}/3)")]
    [InlineData("K.在淮陵游456玩之际，<color=&&00ff00ff>遇到{0}一123位自</color>，我观其似乎武艺高强。")]
    [InlineData("L.王铁(1000，1000)")]
    
    public static void StringTokenReplacer(string original)
    {
        var replacer = new StringTokenReplacer();

        //Want string cleaned up
        original = LineValidation.PrepareRaw(original, null);
        string replaced = replacer.Replace(original);
        

        string restored = replacer.Restore(replaced);

        Console.WriteLine("Original: " + original);
        Console.WriteLine("Replaced: " + replaced);
        Console.WriteLine("Restored: " + restored);

        Assert.Equal(original, restored);
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

    [Theory]
    [InlineData("[SweetPotato.Gift/GIFT_TYPE，System.Collections.Generic.Dictionary`2<System.Int64，System.Collections.Generic.Dictionary`2<System.Int64，System.Int32>>]", 1)]
    [InlineData("[System.Collections.Generic.Dictionary`2<System.Int64>，SweetPotato.Gift/GIFT_TYPE，System.Collections.Generic.Dictionary`2<System.Int64，System.Int32>>]", 2)]
    public void TestParameterSplitRegex(string rawParameters, int index)
    {
        var serializer = Yaml.CreateSerializer();
        var parameters = TranslationService.PrepareMethodParameters(rawParameters);
        var output = serializer.Serialize(parameters);

        string outputFile = $"{workingDirectory}/TestResults/TestParameterSplitRegex{index}.yaml";
        File.WriteAllText(outputFile, output);
        File.AppendAllLines(outputFile, [rawParameters]);
    }

    [Theory]
    [InlineData("Hello.", "Hello")] // Single word, should remove full stop
    [InlineData("This is a test.", "This is a test")] // Three words, should remove full stop
    [InlineData("This is a longer test.", "This is a longer test.")] // Four words, should keep full stop
    [InlineData("No full stop here", "No full stop here")] // No full stop, should remain unchanged
    [InlineData("Multiple. Sentences here.", "Multiple. Sentences here.")] // Multiple sentences, should remain unchanged
    [InlineData("  Spaces before and after.  ", "  Spaces before and after  ")] // Leading/trailing spaces, should remove full stop
    [InlineData("Spaces before and after.  ", "Spaces before and after  ")] // trailing spaces, should remove full stop
    public void RemoveFullStop_Tests(string input, string expected)
    {
        string result = LineValidation.RemoveFullStop("", input);
        Assert.Equal(expected, result);
    }
}