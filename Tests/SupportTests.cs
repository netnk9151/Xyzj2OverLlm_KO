using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace Translate.Tests;

public class SupportTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public void GenerateTextToFiles()
    {
        //new() { Path = "achievement.txt", SplitIndexes = [] },
        var dir = new DirectoryInfo($"{workingDirectory}/Raw/SplitDb");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
            Console.WriteLine($"new() {{Path = \"{file.Name}\", SplitIndexes = []}},");
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
    [InlineData("Hello name, welcome to place!")]
    [InlineData("Hello {name}, welcome to {place}!")]
    [InlineData("与{E}交谈（{IsCanFinish:0:1}/1)")]
    [InlineData("击败目标点{GetZhiYingTargetPos}的{E}（{NeedKillNpcItemsCount}/{N})")]
    [InlineData("击败目标点{GetZhiYingTargetPos}的{E} \\n {NeedKillNpcItemsCount}/{N})")]
    [InlineData("各家学说，各抒己见，两两之间，总有克制。\\n强克制：对目标伤害提升0.5倍。被强克制：对目标伤害降低0.5倍。\\n强克制关系：道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制：对目标伤害提升0.25倍。被弱克制：对目标伤害降低0.25倍。\\n弱克制关系：道学→儒学→墨学；佛学→魔学→农学。")]
    [InlineData("正有事找你，前些日子{}特意送来好礼，如今也该是回礼的日子了，你拿上此物交给{}事务总管，事成之后门中会奖励一枚不夜京承渝令。")]
    [InlineData("天随人愿，历经千辛万苦，终于在{0}发现了{1}，可谓福气满满。")]
    [InlineData("覆灭穆特前线的所有穆特族（{IsCanFinish:0:1}/1）")]
    static void StringTokenReplacer(string original)
    {
        var replacer = new StringTokenReplacer();

        string replaced = replacer.Replace(original);
        Console.WriteLine("Replaced: " + replaced);

        string restored = replacer.Restore(replaced);
        Console.WriteLine("Restored: " + restored);

        Assert.Equal(original, restored);
    }
}