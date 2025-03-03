using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Translate.Tests;

public class TranslationWorkflowTests
{
    const string workingDirectory = "../../../../Files";
    const string gameFolder = "G:\\SteamLibrary\\steamapps\\common\\下一站江湖Ⅱ\\下一站江湖Ⅱ\\";

    [Fact(DisplayName = "1. SplitDbAssets")]
    public void SplitDbAssets()
    {
        TranslationService.SplitDbAssets(workingDirectory);
    }

    [Fact(DisplayName = "2. ExportAssetsIntoTranslated")]
    public void ExportAssetsIntoTranslated()
    {
        TranslationService.ExportTextAssetsToCustomFormat(workingDirectory);
    }

    [Fact(DisplayName = "2. ExportDumpedIntoTranslated")]
    public void ExportDumpedIntoTranslated()
    {
        TranslationService.ExportDumpedPrefabToCustomFormat(workingDirectory);
    }

    [Fact(DisplayName = "3. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await UpdateCurrentTranslationLines(true);
    }

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await PerformTranslateLines(false);
    }

    [Fact(DisplayName = "0. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await PerformTranslateLines(true);
    }

    private async Task PerformTranslateLines(bool keepCleaning)
    {
        if (keepCleaning)
        {
            int remaining = await UpdateCurrentTranslationLines(false);
            int iterations = 0;
            while (remaining > 0 && iterations < 10)
            {
                await TranslationService.TranslateViaLlmAsync(workingDirectory, false);
                remaining = await UpdateCurrentTranslationLines(false);
                iterations++;
            }
        }
        else
            await TranslationService.TranslateViaLlmAsync(workingDirectory, false);


        await PackageFinalTranslation();
    }

    [Fact(DisplayName = "6. PackageFinalTranslation")]
    public async Task PackageFinalTranslation()
    {
        await TranslationService.PackageFinalTranslationAsync(workingDirectory);

        var sourceDirectory = $"{workingDirectory}/Mod/{ModHelper.ContentFolder}";
        var modDirectory = $"{gameFolder}/下一站江湖Ⅱ_Data/StreamingAssets/Mod/{ModHelper.ContentFolder}";
        var resourceDirectory = $"{gameFolder}/BepInEx/resources";

        if (Directory.Exists(modDirectory))
            Directory.Delete(modDirectory, true);

        TranslationService.CopyDirectory(sourceDirectory, modDirectory);

        File.Copy($"{workingDirectory}/Mod/db1.txt", $"{resourceDirectory}/db1.txt", true);
        File.Copy($"{workingDirectory}/Mod/Formatted/dumpedPrefabText.txt", $"{resourceDirectory}/dumpedPrefabText.txt", true);

        await PackageRelease();
    }

    [Fact(DisplayName = "6. Copy Sprites")]
    public async Task CopySprites()
    {
        await TranslationService.PackageFinalTranslationAsync(workingDirectory);

        var sourceDirectory = $@"H:\xzyj2-sprites/completed";
        var spritesDirectory = $"{gameFolder}/BepInEx/sprites";

        if (Directory.Exists(spritesDirectory))
            Directory.Delete(spritesDirectory, true);

        TranslationService.CopyDirectory(sourceDirectory, spritesDirectory);
    }

    [Fact(DisplayName = "7. Pack Release")]
    public async Task PackageRelease()
    {
        var version = ModHelper.CalculateVersionNumber();

        string releaseFolder = $"{gameFolder}/ReleaseFolder";

        File.Copy($"{workingDirectory}/Mod/db1.txt", $"{releaseFolder}/BepInEx/resources/db1.txt", true);
        File.Copy($"{workingDirectory}/Mod/Formatted/dumpedPrefabText.txt", $"{releaseFolder}/BepInEx/resources/dumpedPrefabText.txt", true);
        File.Copy($"{gameFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", $"{releaseFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", true);
        File.Copy($"{gameFolder}/BepInEx/Translation/en/Text/resizer.txt", $"{releaseFolder}/BepInEx/Translation/en/Text/resizer.txt", true);

        ZipFile.CreateFromDirectory($"{releaseFolder}/BepInEx", $"{releaseFolder}/EnglishPatch-{version}.zip");

        await Task.CompletedTask;
    }

    public static Dictionary<string, string> GetManualCorrections()
    {
        return new Dictionary<string, string>()
        {
            // Manual
            //{  "奖励：", "Reward:" },
        };
    }

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                    // Reset all the retrans flags
                    split.ResetFlags(false);

            var serializer = Yaml.CreateSerializer();
            File.WriteAllText(outputFile, serializer.Serialize(fileLines));

            await Task.CompletedTask;
        });
    }

    public static async Task<int> UpdateCurrentTranslationLines(bool resetFlag)
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var totalRecordsModded = 0;
        var manual = GetManualCorrections();
        var logLines = new List<string>();


        string[] fullFileRetrans = [
            //"horoscope.txt",
            //"randomname.txt",
            //"randomnamenew.txt"
            ];

        //Use this when we've changed a glossary value that doesnt check hallucination
        var newGlossaryStrings = new List<string>
        {
            //"狂",
            //"邪",
            //"正",
            //"阴",
            //"阳",
        };

        var badRegexes = new List<string>
        {
            //"·", //Figure out split before doing this
            //@"\(.*，.*\)" //Put back for big files
        };

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFile, fileLines) =>
        {
            int recordsModded = 0;

            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                {
                    // Reset all the retrans flags
                    if (resetFlag)
                        split.ResetFlags(false);

                    if (fullFileRetrans.Contains(textFile.Path))
                    {
                        split.FlaggedForRetranslation = true;
                        recordsModded++;
                        continue;
                    }

                    if (UpdateSplit(logLines, newGlossaryStrings, badRegexes, manual, split, textFile, config))
                        recordsModded++;
                }

            totalRecordsModded += recordsModded;
            var serializer = Yaml.CreateSerializer();
            if (recordsModded > 0 || resetFlag)
            {
                Console.WriteLine($"Writing {recordsModded} records to {outputFile}");
                File.WriteAllText(outputFile, serializer.Serialize(fileLines));
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Total Lines: {totalRecordsModded} records");
        File.WriteAllLines($"{workingDirectory}/TestResults/LineValidationLog.txt", logLines);

        return totalRecordsModded;
    }

    public static bool UpdateSplit(List<string> logLines, List<string> newGlossaryStrings, List<string> badRegexes, Dictionary<string, string> manual, TranslationSplit split, TextFileToSplit textFile,
        LlmConfig config)
    {
        var pattern = LineValidation.ChineseCharPattern;
        bool modified = false;
        bool cleanWithGlossary = true;

        //////// Quick Validation here

        // If it is already translated or just special characters return it
        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);
        var cleanedRaw = LineValidation.CleanupLineBeforeSaving(split.Text, split.Text, textFile, tokenReplacer);
        if (!Regex.IsMatch(preparedRaw, pattern) && split.Translated != cleanedRaw)
        {
            logLines.Add($"Already Translated {textFile.Path} \n{split.Translated}");
            split.Translated = cleanedRaw;
            split.ResetFlags();
            return true;
        }

        foreach (var glossary in newGlossaryStrings)
        {
            if (preparedRaw.Contains(glossary))
            {
                logLines.Add($"New Glossary {textFile.Path} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        foreach (var badRegex in badRegexes)
        {
            if (Regex.IsMatch(split.Text, badRegex))
            {
                logLines.Add($"Bad Regex {textFile.Path} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        // Add Manual Translations in that are missing        
        if (manual.TryGetValue(preparedRaw, out string? value))
        {
            if (split.Translated != value)
            {
                logLines.Add($"Manually Translated {textFile.Path} \n{split.Text}\n{split.Translated}");
                split.Translated = LineValidation.CleanupLineBeforeSaving(LineValidation.PrepareResult(value), split.Text, textFile, tokenReplacer);
                split.ResetFlags();
                return true;
            }

            return false;
        }

        // Skip Empty but flag so we can find them easily
        if (string.IsNullOrEmpty(split.Translated) && !string.IsNullOrEmpty(preparedRaw))
        {
            split.FlaggedForRetranslation = true;
            split.FlaggedMistranslation = "Failed"; //Easy search
            return true;
        }

        // Temp force retrans of splits because of changes in calcs
        //foreach (var splitCharacters in TranslationService.SplitCharactersList)
        //    if (preparedRaw.Contains(splitCharacters))
        //    {
        //        split.FlaggedForRetranslation = true;
        //        return true;
        //    }

        if (MatchesBadWords(split.Translated))
        {
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        //////// Manipulate split from here
        if (cleanWithGlossary)
        {
            // Glossary Clean up - this won't check our manual jobs
            modified = CheckMistranslationGlossary(config, split, modified, textFile);
            modified = CheckHallucinationGlossary(config, split, modified, textFile);
        }

        // Characters
        //if (preparedRaw.Contains("?")
        //    && !split.Translated.Contains("?"))
        //{
        //    Console.WriteLine($"Missing ? {outputFile} Replaces: \n{split.Translated}");
        //    split.FlaggedForRetranslation = true;
        //    modified = true;
        //}

        //if (preparedRaw.Contains("!")
        //    && !split.Translated.Contains("!"))
        //{
        //    Console.WriteLine($"Missing ! {outputFile} Replaces: \n{split.Translated}");
        //    split.FlaggedForRetranslation = true;
        //    modified = true;
        //}

        if (preparedRaw.EndsWith("...")
            && preparedRaw.Length < 15
            && !split.Translated.EndsWith("...")
            && !split.Translated.EndsWith("...?")
            && !split.Translated.EndsWith("...!")
            && !split.Translated.EndsWith("...!!")
            && !split.Translated.EndsWith("...?!"))
        {
            logLines.Add($"Missing ... {textFile.Path} Replaces: \n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        if (preparedRaw.StartsWith("...") && !split.Translated.StartsWith("..."))
        {
            logLines.Add($"Missing ... {textFile.Path} Replaces: \n{split.Translated}");
            split.Translated = $"...{split.Translated}";
            modified = true;
        }


        // Trim line
        if (split.Translated.Trim().Length != split.Translated.Length)
        {
            logLines.Add($"Needed Trimming:{textFile.Path} \n{split.Translated}");
            split.Translated = split.Translated.Trim();
            modified = true;
        }

        // Add . into Dialogue
        //if (outputFile.EndsWith("stringlang.txt") && char.IsLetter(split.Translated[^1]) && preparedRaw != split.Translated)
        //{
        //    logLines.Add($"Needed full stop:{textFile.Path} \n{split.Translated}");
        //    split.Translated += '.';
        //    modified = true;
        //}

        // Clean up Diacritics
        var cleanedUp = LineValidation.CleanupLineBeforeSaving(split.Translated, preparedRaw, textFile, tokenReplacer);
        if (cleanedUp != split.Translated)
        {
            logLines.Add($"Cleaned up {textFile.Path} \n{split.Translated}\n{cleanedUp}");
            split.Translated = cleanedUp;
            modified = true;
        }

        // Remove Invalid ones -- Have to use final raw because translated is untokenised
        var result = LineValidation.CheckTransalationSuccessful(config, split.Text, split.Translated ?? string.Empty, textFile);
        if (!result.Valid)
        {
            logLines.Add($"Invalid {textFile.Path} Failures:{result.CorrectionPrompt}\n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        return modified;
    }

    private static bool CheckMistranslationGlossary(LlmConfig config, TranslationSplit split, bool modified, TextFileToSplit textFile)
    {
        if (!textFile.EnableGlossary)
            return modified;

        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            if (!item.CheckForBadTranslation)
                continue;

            //Exclusions and Targetted Glossary
            if (item.OnlyOutputFiles.Count > 0 && !item.OnlyOutputFiles.Contains(textFile.Path))
                continue;
            else if (item.ExcludeOutputFiles.Count > 0 && item.ExcludeOutputFiles.Contains(textFile.Path))
                continue;

            if (preparedRaw.Contains(item.Raw) && !split.Translated.Contains(item.Result, StringComparison.OrdinalIgnoreCase))
            {
                var found = false;
                foreach (var alternative in item.AllowedAlternatives)
                {
                    found = split.Translated.Contains(alternative, StringComparison.OrdinalIgnoreCase);
                    if (found)
                        break;
                }

                if (!found)
                {
                    split.FlaggedForRetranslation = true;
                    split.FlaggedMistranslation += $"{item.Result},{item.Raw},";
                    modified = true;
                }
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    private static bool CheckHallucinationGlossary(LlmConfig config, TranslationSplit split, bool modified, TextFileToSplit textFile)
    {
        if (!textFile.EnableGlossary)
            return modified;

        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            var wordPattern = $"\\b{item.Result}\\b";

            if (!preparedRaw.Contains(item.Raw) && split.Translated.Contains(item.Result))
            {
                if (!item.CheckForMisusedTranslation)
                    continue;

                //Exclusions and Targetted Glossary
                if (item.OnlyOutputFiles.Count > 0 && !item.OnlyOutputFiles.Contains(textFile.Path))
                    continue;
                else if (item.ExcludeOutputFiles.Count > 0 && item.ExcludeOutputFiles.Contains(textFile.Path))
                    continue;

                // Regex matches on terms with ... match incorrectly
                if (!Regex.IsMatch(split.Translated, wordPattern, RegexOptions.IgnoreCase))
                    continue;

                // Check for Alternatives
                var dupes = config.GlossaryLines.Where(s => s.Result == item.Result && s.Raw != item.Raw);
                bool found = false;

                foreach (var dupe in dupes)
                {
                    found = preparedRaw.Contains(dupe.Raw);
                    if (found)
                        break;
                }

                if (!found)
                {
                    split.FlaggedForRetranslation = true;
                    split.FlaggedHallucination += $"{item.Result},{item.Raw},";
                    modified = true;
                }
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    public static bool MatchesBadWords(string input)
    {
        HashSet<string> words = new HashSet<string>
        {
            "hiu", "tut", "thut", "oi", "avo", "porqe", "obrigado",
            "nom", "esto", "tem", "mais", "com", "ver", "nos", "sobre", "vermos",
            "dar", "nam", "J'ai", "je", "veux", "pas", "ele", "una", "keqi", "shiwu",
            "fuck", "ich", "ein", "der", "ganzes", "Leben", "dort",
            "knight", "thay", "tien", "div", "html",
        };

        string pattern = $@"\b({string.Join("|", words)})\b";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}
