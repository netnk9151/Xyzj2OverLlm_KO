using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Translate.Tests;
public class PromptTuningTests
{
    const string workingDirectory = "../../../../Files";    

    [Fact(DisplayName = "1. Test Current Prompts")]
    public async Task TestPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        config.RetryCount = 1;
        var batchSize = config.BatchSize ?? 50;

        var testLines = new List<TranslatedRaw> {
            new("淮陵-酒楼"),
            new("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)"),
            new("男：忙忙碌碌把财求，何时云开见日头。\\n难得祖基家可立，中年衣食渐无忧。\\n女：早年行运在忙头，奔走四方事多忧。\\n费心劳神把家立，老年安生不发愁。"),
            new("押镖问询·二"),
            new("九环刀"),
            new("桌凳"),
            new("{0&0}%几率对我方随机角色添加持续一轮次的随即论点。"),
            new("听闻枰栌附近的河域，有不少的{I}，这或许是个不错的财路。特颁此令以征三条{I}。"),
            new("在<color=&&00ff00ff>武馆教头（枰栌）</color>处有概率获得"),
            new("在对抗有优势火力的敌人时，兵家的常用战术是："),
            new("阴、阳、刚、柔、毒各+{0:G}"),
            new("{0&0}%几率对我方随机角色添加持续一轮次的随即论点。"),
            new("达成机杼墨师身份，在龙蛇寨附近完成机杼传承"),
            new("前往(882,-117)请教兵狂"),
            new("击败目标点{GetZhiYingTargetPos}的{E} \\n {NeedKillNpcItemsCount}/{N})"),
            new("各家学说，各抒己见，两两之间，总有克制。\\n强克制：对目标伤害提升0.5倍。被强克制：对目标伤害降低0.5倍。\\n强克制关系：道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制：对目标伤害提升0.25倍。被弱克制：对目标伤害降低0.25倍。\\n弱克制关系：道学→儒学→墨学；佛学→魔学→农学。"),
            new("为人心性最聪明，经营求财命不穷。善于经商，同时因为过多钻研商道导致心眼狭小，商道+50，豪爽-50。"),
            new("到达目标点{GetZhiYingTargetPos}({IsCanFinish:0:1}/3)"),
        };

        config.SkipLineValidation = true;

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
                var tokenReplacer = new StringTokenReplacer();
                var preparedRaw = LineValidation.PrepareRaw(line.Raw, tokenReplacer);
                var result = await TranslationService.TranslateSplitAsync(config, preparedRaw, client, string.Empty);
                line.Trans = LineValidation.CleanupLineBeforeSaving(result, preparedRaw, string.Empty, tokenReplacer);

                recordsProcessed++;
            }));

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Line: {i + batchRange} of {totalLines} ({elapsed} ms ~ {speed}/line)");
        }

        foreach (var line in testLines)
        {
            var valid = LineValidation.CheckTransalationSuccessful(config, line.Raw, line.Trans, string.Empty);
            results.Add($"From: {line.Raw}\nTo: {line.Trans}\nValid={valid.Valid}\n{valid.CorrectionPrompt}\n");
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/0.PromptTest.txt", results);
    }


    [Fact(DisplayName = "2. Optimise Provided Prompt")]
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
        var result = await QuickTranslateWithCustomMessages(config, messages);

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
        var result = await QuickTranslate(config, input, string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain how and if the glossary was used.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainGlossaryPrompt{index}.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt2()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "豆花嫂希望你能为她丈夫带来虎鞭，至于用途应该不难猜？";
        var result = await QuickTranslate(config, input, string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ? was removed. Also explain how to adjust the system prompt to correct it to make sure the '?' was not removed and context is retained.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt2.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt3()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "若果有此意，叫八戒伐几棵树来，沙僧寻些草来，我做木匠，就在这里搭个窝铺，你与她圆房成事，我们大家散了，却不是件事业？何必又跋涉，取什经去！";
        var result = await QuickTranslate(config, input, string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ! was removed. Also explain how to adjust the system prompt to correct it to make sure the '!' was not removed and context is retained. Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt3.txt", result);
    }

    [Fact]
    public async Task ExplainAlternativesPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        var input = "完成菩提";
        var result = await QuickTranslate(config, input, string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an alternative." +
            "Also explain how to adjust the system prompt to correct it to make sure you do not provide this alternative." +
            "Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainAltPrompt.txt", result);
    }

    [Fact]
    public async Task ExplainExplanationPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        var input = "幽影-剑意纵横";
        var result = await QuickTranslate(config, input, string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an explanation." +
            "Also explain how to adjust the system prompt to correct it to make sure you do not provide this explanation." +
            "Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainExplanationPrompt.txt", result);
    }

    [Fact]
    public async Task OptimiseCorrectTagPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

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

        var result = await QuickTranslateWithCustomMessages(config, messages);

        // Calculate output of test
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, string.Empty);
        var lines = $"Valid:{validationResult.Valid}\nRaw:{raw}\nResult:{result}";
        File.WriteAllText($"{workingDirectory}/TestResults/OptimiseCorrectTag.txt", lines);
    }

    public async Task<string> QuickTranslate(LlmConfig config, string input, string outputFile = "", string additionalPrompt = "")
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request
        List<object> messages = TranslationService.GenerateBaseMessages(config, input, outputFile, additionalPrompt);

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

        return result;
    }

    public async Task<string> QuickTranslateWithCustomMessages(LlmConfig config, List<object> messages)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

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

        return result;
    }
}
