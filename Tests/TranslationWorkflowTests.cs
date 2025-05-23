﻿using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Translate.Utility;

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

    [Fact(DisplayName = "2. ExportDumpedDyanmicIntoTranslated")]
    public void ExportDumpedDyanmicIntoTranslated()
    {
        TranslationService.ExportDynamicStringsToCustomFormat(workingDirectory);
    }

    [Fact(DisplayName = "2.5 MergeFilesIntoTranslated")]
    public async Task MergeFilesIntoTranslated()
    {
        await TranslationService.MergeFilesIntoTranslatedAsync(workingDirectory);
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
            while (remaining > 0 && iterations < 3)
            {
                await TranslationService.TranslateViaLlmAsync(workingDirectory, false);
                remaining = await UpdateCurrentTranslationLines(false);
                iterations++;
            }

            await PackageFinalTranslation();
        }
        else
            await TranslationService.TranslateViaLlmAsync(workingDirectory, false);
    }

    [Fact(DisplayName = "6. Package to Game Files")]
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
        foreach (var file in TranslationService.GetTextFilesToSplit().Where(t => t.TextFileType != TextFileType.RegularDb))
            File.Copy($"{workingDirectory}/Mod/Formatted/{file.Path}", $"{resourceDirectory}/{file.Path}", true);

        //await PackageRelease();
    }

    [Fact(DisplayName = "5. Copy Sprites")]
    public async Task CopySprites()
    {
        await TranslationService.PackageFinalTranslationAsync(workingDirectory);

        var sourceDirectory = $@"G:\xzyj2-sprites/completed";
        var spritesDirectory = $"{gameFolder}/BepInEx/sprites";

        if (Directory.Exists(spritesDirectory))
            Directory.Delete(spritesDirectory, true);

        TranslationService.CopyDirectory(sourceDirectory, spritesDirectory);
    }

    [Fact(DisplayName = "7. Zip Release")]
    public async Task ZipRelease()
    {
        var version = ModHelper.CalculateVersionNumber();

        string releaseFolder = $"{gameFolder}/ReleaseFolder/Files";

        File.Copy($"{workingDirectory}/Mod/db1.txt", $"{releaseFolder}/BepInEx/resources/db1.txt", true);
        File.Copy($"{workingDirectory}/Mod/Formatted/dumpedPrefabText.txt", $"{releaseFolder}/BepInEx/resources/dumpedPrefabText.txt", true);
        File.Copy($"{gameFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", $"{releaseFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", true);
        File.Copy($"{gameFolder}/BepInEx/Plugins/FanslationStudio.SharedAssembly.dll", $"{releaseFolder}/BepInEx/Plugins/FanslationStudio.SharedAssembly.dll", true);
        //File.Copy($"{gameFolder}/BepInEx/Translation/en/Text/resizer.txt", $"{releaseFolder}/BepInEx/Translation/en/Text/resizer.txt", true);

        foreach (var file in TranslationService.GetTextFilesToSplit().Where(t => t.TextFileType != TextFileType.RegularDb))
            File.Copy($"{workingDirectory}/Mod/Formatted/{file.Path}", $"{releaseFolder}/BepInEx/resources/{file.Path}", true);

        List<string> copyDirs = ["sprites", "resizers"];
        foreach (var copyDir in copyDirs)
        {
            var newDirectory = $"{releaseFolder}/BepInEx/{copyDir}";
            if (Directory.Exists(newDirectory))
                Directory.Delete(newDirectory, true);

            TranslationService.CopyDirectory($"{gameFolder}/BepInEx/{copyDir}", newDirectory);
        }

        ZipFile.CreateFromDirectory($"{releaseFolder}", $"{releaseFolder}/../EnglishPatch-{version}.zip");

        await Task.CompletedTask;
    }

    [Fact(DisplayName = "0. Check File Lines Match")]
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

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var serializer = Yaml.CreateSerializer();

        await TranslationService.IterateTranslatedFilesInParallelAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                    // Reset all the retrans flags
                    split.ResetFlags(false);

            await File.WriteAllTextAsync(outputFile, serializer.Serialize(fileLines));
        });
    }

    public static async Task<int> UpdateCurrentTranslationLines(bool resetFlag)
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var totalRecordsModded = 0;
        var logLines = new ConcurrentBag<string>();

        string[] fullFileRetrans = [
            //"horoscope.txt",
            //"randomname.txt",
            //"randomnamenew.txt"
        ];

        // Use this when we've changed a glossary value that doesnt check hallucination
        var newGlossaryStrings = new List<string>
        {
            //"羽士",
            //"[发现宝箱]",
            //"[石化]",
            //"[开心]",
            //"[不知所措]",
            //"[疑问]",
            //"[担忧]",
            //"[生气]",
            //"[哭泣]",
            //"[惊讶]",
            //"[发怒]",
            //"[抓狂]",
            //"[委屈]",
        };

        var badRegexes = new List<string>
        {
            //"·", //Figure out split before doing this
            //@"\(.*，.*\)" //Put back for big files
            //@"\|",
        };

        //await TranslationService.IterateTranslatedFilesInParallelAsync(workingDirectory, async (outputFile, textFile, fileLines) =>
        //Use non-parallel for debugging
        await TranslationService.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFile, fileLines) =>
        {
            var serializer = Yaml.CreateSerializer();

            int recordsModded = 0;

            foreach (var line in fileLines)
            {
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

                    if (UpdateSplit(logLines, newGlossaryStrings, badRegexes, split, textFile, config))
                        recordsModded++;
                }
            }

            Interlocked.Add(ref totalRecordsModded, recordsModded); // Use atomic operation for updating totalRecordsModded
            if (recordsModded > 0 || resetFlag)
            {
                Console.WriteLine($"Writing {recordsModded} records to {outputFile}");
                await File.WriteAllTextAsync(outputFile, serializer.Serialize(fileLines));
            }
        });

        Console.WriteLine($"Total Lines: {totalRecordsModded} records");
        File.WriteAllLines($"{workingDirectory}/TestResults/LineValidationLog.txt", logLines);

        return totalRecordsModded;
    }

    public static bool UpdateSplit(ConcurrentBag<string> logLines, List<string> newGlossaryStrings, List<string> badRegexes, TranslationSplit split, TextFileToSplit textFile,
        LlmConfig config)
    {
        var pattern = LineValidation.ChineseCharPattern;
        bool modified = false;
        bool cleanWithGlossary = true;

        //////// Quick Validation here

        if (!split.SafeToTranslate)
            return false;

        // If it is already translated or just special characters return it
        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);
        var cleanedRaw = LineValidation.CleanupLineBeforeSaving(split.Text, split.Text, textFile, tokenReplacer);
        var preparedResultRaw = LineValidation.CleanupLineBeforeSaving(preparedRaw, preparedRaw, textFile, tokenReplacer);
        if (!Regex.IsMatch(preparedRaw, pattern) && split.Translated != cleanedRaw && split.Translated != preparedResultRaw)
        {
            logLines.Add($"Already Translated {textFile.Path} \n{split.Translated}");
            split.Translated = preparedResultRaw;
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

        // Do not manipulate anything that is to do with the UI
        if (textFile.TextFileType == TextFileType.DynamicStrings)
        {
            if (split.Text.Contains("Sprite")
                || split.Text.Contains("UI")
                || split.Text.Contains("Prefab")
                || split.Text.StartsWith("INVALIDCHAR:"))
            {
                split.SafeToTranslate = false;
                return true;
            }
        }

        // Add Manual Translations in that are missing
        if (textFile.EnableGlossary)
        {
            foreach (var manual in config.ManualTranslations)
            {
                if (split.Text == manual.Raw)
                {
                    if (split.Translated != manual.Result)
                    {
                        logLines.Add($"Manually Translated {textFile.Path} \n{split.Text}\n{split.Translated}");
                        split.Translated = LineValidation.CleanupLineBeforeSaving(LineValidation.PrepareResult(preparedRaw, manual.Result), split.Text, textFile, new StringTokenReplacer());
                        split.ResetFlags();
                        return true;
                    }

                    return false;
                }
            }
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

        // Clean up Diacritics -- Use a new tokenizer because the translated isnt generated off the prep raw
        var cleanedUp = LineValidation.CleanupLineBeforeSaving(split.Translated, preparedRaw, textFile, new StringTokenReplacer());
        if (cleanedUp != split.Translated)
        {
            logLines.Add($"Cleaned up {textFile.Path} \n{split.Translated}\n{cleanedUp}");
            split.Translated = cleanedUp;
            modified = true;
        }

        // Remove Invalid ones -- Have to use pure raw because translated is untokenised
        var translated2 = StringTokenReplacer.CleanTranslatedForApplyRules(split.Translated);
        var result = LineValidation.CheckTransalationSuccessful(config, split.Text, translated2, textFile);
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
        HashSet<string> words =
        [
            "hiu", "tut", "thut", "oi", "avo", "porqe", "obrigado", 
            "knight", "knights", "knight-at-arms", "knights-errant",
            "nom", "esto", "tem", "mais", "com", "ver", "nos", "sobre", "vermos",
            "dar", "nam", "J'ai", "je", "veux", "pas", "ele", "una", "keqi", "shiwu",
            "fuck", "ich", "ein", "der", "ganzes", "Leben", "dort", //"de", NAmes can have de
            "knight", "thay", "tien", "div", "html", "tiantu", "ngoc", "truong", "Phong"
        ];

        string pattern = $@"\b({string.Join("|", words)})\b";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}
