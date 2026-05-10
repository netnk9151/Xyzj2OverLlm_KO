using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Translate.Support;
using Translate.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Translate;

public class LlmConfig
{
    public string? ApiKey { get; set; }
    public bool ApiKeyRequired { get; set; }
    public string? Url { get; set; }
    public string? Model { get; set; }
    public int? RetryCount { get; set; }
    public int? BatchSize { get; set; }
    public bool SkipLineValidation { get; set; }
    public bool CorrectionPromptsEnabled { get; set; }
    public bool TranslateFlagged { get; set; }
    public Dictionary<string, object>? ModelParams { get; set; }

    // Not serialised in Yaml
    [YamlIgnore]
    public Dictionary<string, string> Prompts { get; set; } = [];

    [YamlIgnore]
    public string? WorkingDirectory { get; set; }

    [YamlIgnore]
    public List<GlossaryLine> GlossaryLines { get; set; } = [];

    [YamlIgnore]
    public List<GlossaryLine> ManualTranslations { get; set; } = [];

    [YamlIgnore]
    public Dictionary<string, string> TranslationCache { get; set; } = [];
}

public static class Configuration
{
    public static LlmConfig GetConfiguration(string workingDirectory)
    {
        var deserializer = Yaml.CreateDeserializer();

        // 1. Config.yaml 로드 (프로그램 실행을 위한 필수 설정)
        var configText = File.ReadAllText($"{workingDirectory}/Config.yaml", Encoding.UTF8);
        var response = deserializer.Deserialize<LlmConfig>(configText);

        if (response == null)
        {
            throw new Exception("Config.yaml 파일을 로드할 수 없습니다. 파일 내용을 확인해주세요.");
        }

        response.WorkingDirectory = workingDirectory;
        response.Prompts = CachePrompts(workingDirectory);

        // 2. Glossary.yaml 로드 
        // 파일 내용을 읽은 후, Deserialize 결과가 null이면 빈 리스트(new List<GlossaryLine>())를 할당함
        var glossaryText = File.ReadAllText($"{workingDirectory}/Glossary.yaml", Encoding.UTF8);
        response.GlossaryLines = deserializer.Deserialize<List<GlossaryLine>>(glossaryText) ?? new List<GlossaryLine>();

        // 3. ManualTranslations.yaml 로드
        // 마찬가지로 파일이 비어있어 null이 반환될 경우를 대비해 빈 리스트를 할당함
        var manualText = File.ReadAllText($"{workingDirectory}/ManualTranslations.yaml", Encoding.UTF8);
        response.ManualTranslations = deserializer.Deserialize<List<GlossaryLine>>(manualText) ?? new List<GlossaryLine>();

        // 4. GlossaryLines 데이터 가공 (내용이 있을 때만 실행됨)
        // response.GlossaryLines가 위에서 null 방어를 했기 때문에 foreach에서 에러가 나지 않음
        foreach (var line in response.GlossaryLines)
        {
            // line 자체가 null이 아니고, line.Result 값이 null이 아닐 때만 Replace 수행
            if (line?.Result != null)
            {
                line.Result = line.Result.Replace("-", "\u2011"); // Change Hyphens to non-breaking hyphens
            }
        }

        return response;
    }

    public static Dictionary<string, string> CachePrompts(string workingDirectory)
    {
        var prompts = new Dictionary<string, string>();
        var path = $"{workingDirectory}/Prompts";

        foreach (var file in Directory.EnumerateFiles(path))
            prompts.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));

        return prompts;
    }   

    //public static void AddToDictionaryGlossary(Dictionary<string, string> globalGlossary, List<DataLine> entries)
    //{
    //    foreach (var line in entries)
    //        globalGlossary.Add(line.Raw, line.Result);
    //}  
}
