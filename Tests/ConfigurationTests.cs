using SharedAssembly.Contracts;
using Translate.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Translate.Tests;

public class ConfigurationTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public async Task CachePromptsTest()
    {
        var prompts = Configuration.CachePrompts(workingDirectory);

        var serializer = Yaml.CreateSerializer();       

        await File.WriteAllTextAsync($"{workingDirectory}/TestResults/AllPrompts.yaml", serializer.Serialize(prompts));
    }

    [Fact]
    public void DeserializeResizerTest()
    {
        var deserializer = Yaml.CreateDeserializer();
        var file = $"{workingDirectory}/Mod/AddedResizers.yaml";
        var newResizers = deserializer.Deserialize<List<TextResizerContract>>(File.ReadAllText(file));
    }

    [Fact]
    public void ReserializeResizerTest()
    {
        var deserializer = Yaml.CreateDeserializer();
        var file = $@"G:\SteamLibrary\steamapps\common\下一站江湖Ⅱ\下一站江湖Ⅱ\BepInEx\resizers/AddedResizers.yaml";

        File.Copy(file, $"{workingDirectory}/Resizers/AddedResizers.yaml", true);
        var newResizers = deserializer.Deserialize<List<TextResizerContract>>(File.ReadAllText(file));

        var serializer = Yaml.CreateSerializer();
        var content = serializer.Serialize(newResizers);

        File.WriteAllText(file, content);
    }

    [Fact]
    public void BackupResizersTest()
    {
        var folder = $@"G:\SteamLibrary\steamapps\common\下一站江湖Ⅱ\下一站江湖Ⅱ\BepInEx\resizers/";
        var outputFolder = $"{workingDirectory}/Resizers";
        if (Directory.Exists(outputFolder))
            Directory.Delete(outputFolder, true);

        Directory.CreateDirectory(outputFolder);

        foreach (var file in Directory.EnumerateFiles(folder))
            File.Copy(file, $"{outputFolder}/{Path.GetFileName(file)}", true);
    }
}
