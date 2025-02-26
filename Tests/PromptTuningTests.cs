using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Translate.Tests;
public class PromptTuningTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public async Task OptimiseCorrectTagPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request
        var raw = "<color=#FF0000>炼狱</color>";
        var origResult = "Hellforge";
        var origValidationResult = LineValidation.CheckTransalationSuccessful(config, raw, origResult, string.Empty);
        List<object> messages = TranslationService.GenerateBaseMessages(config, raw, string.Empty);

        // Tweak Correction Prompt here
        var correctionPrompt = LineValidation.CalulateCorrectionPrompt(config, origValidationResult, raw, origResult);
        //var correctionPrompt = "Try again. The markup rules were not followed.";

        // Add what the correction prompt would have been
        TranslationService.AddCorrectionMessages(messages, origResult, correctionPrompt);

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        //var requestData = File.ReadAllText($"{workingDirectory}/Optimise/OptimiseCorrectTag.json")
        //  .Replace("%ReplaceMe%", correctionPrompt);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        // Calculate output of test
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, string.Empty);
        var lines = $"Valid:{validationResult.Valid}\nRaw:{raw}\nResult:{result}";
        File.WriteAllText($"{workingDirectory}/TestResults/OptimiseCorrectTag.txt", lines);
    }

    [Fact]
    public async Task OptimiseProvidedPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        var basePrompt = config.Prompts["0PromptToOptimise"];
        var optimisePrompt = config.Prompts["0OptimisePrompt"];

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(optimisePrompt),
                LlmHelpers.GenerateUserPrompt(basePrompt.ToString())
            ];

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        File.WriteAllText($"{workingDirectory}/TestResults/1.MinimisePrompt.txt", result);
    }

    [Theory]
    [InlineData(1, "完成奇遇任务《轻功高手》")]
    [InlineData(2, "雄霸武林")]
    [InlineData(3, "德高望重重，才广武林称。兼备风云志，胸怀揽星辰。")]
    [InlineData(4, "于狂刀门贡献堂主处累积购买二十次")]
    public async Task ExplainGlossaryPrompt(int index, string input)
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, input,
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain how and if the glossary was used.");

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainGlossaryPrompt{index}.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt2()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, "豆花嫂希望你能为她丈夫带来虎鞭，至于用途应该不难猜？",
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ? was removed. Also explain how to adjust the system prompt to correct it to make sure the '?' was not removed and context is retained.");

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt3()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, "若果有此意，叫八戒伐几棵树来，沙僧寻些草来，我做木匠，就在这里搭个窝铺，你与她圆房成事，我们大家散了，却不是件事业？何必又跋涉，取什经去！",
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ! was removed. Also explain how to adjust the system prompt to correct it to make sure the '!' was not removed and context is retained. Show an example prompt.");

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt.txt", result);
    }

    [Fact]
    public async Task TestPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        config.RetryCount = 1;
        var batchSize = config.BatchSize ?? 10;

        var testLines = new List<TranslatedRaw> {
            new("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)"),  
            new("男：忙忙碌碌把财求，何时云开见日头。\\n难得祖基家可立，中年衣食渐无忧。\\n女：早年行运在忙头，奔走四方事多忧。\\n费心劳神把家立，老年安生不发愁。"),
            new("押镖问询·二"),
            new("九环刀"),
        };

        var results = new List<string>();
        var totalLines = testLines.Count;
        var stopWatch = Stopwatch.StartNew();

        for (int i = 0; i < totalLines; i += batchSize)
        {
            stopWatch.Restart();

            int batchRange = Math.Min(batchSize, totalLines - i);

            // Use a slice of the list directly
            var batch = testLines.GetRange(i, batchRange);

            int recordsProcessed = 0;

            // Process the batch in parallel
            await Task.WhenAll(batch.Select(async line =>
            {
                line.Trans = await TranslationService.TranslateSplitAsync(config, line.Raw, client, string.Empty);
                recordsProcessed++;
            }));

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Line: {i + batchRange} of {totalLines} ({elapsed} ms ~ {speed}/line)");
        }

        foreach (var line in testLines)
            results.Add($"From: {line.Raw}\nTo: {line.Trans}\n");

        File.WriteAllLines($"{workingDirectory}/TestResults/0.PromptTest.txt", results);
    }

    private class StructuredTranslation
    {
        public string Translated { get; set; } = string.Empty;
        public string Transliterated { get; set; } = string.Empty;
    }

    [Fact]
    public async Task StructuredPromptTests()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request
        var basePrompt = "Direct Translate and Transliterate whatever I give you. Respond in VALID Json only.";

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(basePrompt),
                LlmHelpers.GenerateUserPrompt("经验值80000点，江湖声望，玄铁")
            ];

        //Generate Schema
        JsonSerializerOptions options = JsonSerializerOptions.Default;
        JsonNode schema = options.GetJsonSchemaAsNode(typeof(StructuredTranslation));
        config.ModelParams!.Add("format", schema);

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        File.WriteAllText($"{workingDirectory}/TestResults/4.FormatedResponse.json", result);
    }
}
