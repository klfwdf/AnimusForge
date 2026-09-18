using System;
using System.Collections.Generic;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        var main = new AIConfigModel { DuelSettings = new DuelConfig { AcceptKeywords = new List<string> { "duel" } } };
        var guardrail = new GuardrailConfigModel { RulePrompts = new List<GuardrailRulePromptConfig>
            { new GuardrailRulePromptConfig { Id = "duel", TriggerKeywords = new List<string> { "sword" } } } };
        var action = new ActionPostprocessConfigModel { MoodRules = new List<PostprocessRuleEntry>
            { new PostprocessRuleEntry { Tag = "[ACTION:MOOD:CALM]" } } };
        var preprocess = new PreprocessPromptsConfigModel { StrictJson = new StrictPreprocessPromptConfig
            { MentionedEntitiesSchema = JObject.Parse("{\"entities\":[\"original\"]}") } };
        var proactive = new ProactiveNpcRequestPromptsConfigModel { Requests = new Dictionary<string, ProactiveNpcRequestPromptEntry>
            { ["need"] = new ProactiveNpcRequestPromptEntry { OpeningPrompt = "hello" } } };
        var rp = new RpItemIntroductionPromptsConfigModel { SystemPrompt = "intro" };
        string[] before = { JsonConvert.SerializeObject(main), JsonConvert.SerializeObject(guardrail),
            JsonConvert.SerializeObject(action), JsonConvert.SerializeObject(preprocess),
            JsonConvert.SerializeObject(proactive), JsonConvert.SerializeObject(rp) };
        var snapshot = new PromptConfigurationSnapshot(main, guardrail, action, preprocess, proactive, rp, "");
        string[] published = { JsonConvert.SerializeObject(snapshot.Main), JsonConvert.SerializeObject(snapshot.Guardrail),
            JsonConvert.SerializeObject(snapshot.ActionPostprocess), JsonConvert.SerializeObject(snapshot.Preprocess),
            JsonConvert.SerializeObject(snapshot.ProactiveRequest), JsonConvert.SerializeObject(snapshot.RpItemIntroduction) };
        for (int i = 0; i < before.Length; i++)
            Check(before[i] == published[i], "legal production model JSON identity changed at root " + i);
        main.DuelSettings.AcceptKeywords[0] = "source";
        guardrail.RulePrompts[0].TriggerKeywords[0] = "source";
        action.MoodRules[0].Tag = "source";
        ((JArray)preprocess.StrictJson.MentionedEntitiesSchema["entities"])[0] = "source";
        proactive.Requests["need"].OpeningPrompt = "source";
        rp.SystemPrompt = "source";
        Check(snapshot.Main.DuelSettings.AcceptKeywords[0] == "duel", "main source detached");
        Check(snapshot.Guardrail.RulePrompts[0].TriggerKeywords[0] == "sword", "guardrail source detached");
        Check(snapshot.ActionPostprocess.MoodRules[0].Tag == "[ACTION:MOOD:CALM]", "action source detached");
        Check((string)snapshot.Preprocess.StrictJson.MentionedEntitiesSchema["entities"][0] == "original", "JObject source detached");
        Check(snapshot.ProactiveRequest.Requests["need"].OpeningPrompt == "hello", "proactive source detached");
        Check(snapshot.RpItemIntroduction.SystemPrompt == "intro", "RP source detached");
        snapshot.Main.DuelSettings.AcceptKeywords[0] = "reader";
        snapshot.Guardrail.RulePrompts[0].TriggerKeywords[0] = "reader";
        snapshot.ActionPostprocess.MoodRules[0].Tag = "reader";
        ((JArray)snapshot.Preprocess.StrictJson.MentionedEntitiesSchema["entities"])[0] = "reader";
        snapshot.ProactiveRequest.Requests["need"].OpeningPrompt = "reader";
        snapshot.RpItemIntroduction.SystemPrompt = "reader";
        Check(snapshot.Main.DuelSettings.AcceptKeywords[0] == "duel", "main reader detached");
        Check(snapshot.Guardrail.RulePrompts[0].TriggerKeywords[0] == "sword", "guardrail reader detached");
        Check(snapshot.ActionPostprocess.MoodRules[0].Tag == "[ACTION:MOOD:CALM]", "action reader detached");
        Check((string)snapshot.Preprocess.StrictJson.MentionedEntitiesSchema["entities"][0] == "original", "JObject reader detached");
        Check(snapshot.ProactiveRequest.Requests["need"].OpeningPrompt == "hello", "proactive reader detached");
        Check(snapshot.RpItemIntroduction.SystemPrompt == "intro", "RP reader detached");
        Console.WriteLine("PromptJ03 production model checks=" + _checks);
    }
}
