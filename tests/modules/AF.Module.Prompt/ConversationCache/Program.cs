using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Game/configuration boundaries are fixtures. Production store, adapter, text
// readers, message reader and final protocol serializer are linked without edits.
internal static class AnimusForgeModulePaths
{
    internal static string GetModuleDataFilePath(string name) => System.IO.Path.Combine(AppContext.BaseDirectory,
        name == "ConversationRuntimePrompts.json" && Environment.GetEnvironmentVariable("AF_TEST_EDITION") == "EN"
            ? "ConversationRuntimePrompts.en.json" : name);
}
internal sealed class DuelSettings
{
    internal static readonly DuelSettings Settings = new DuelSettings();
    internal static DuelSettings GetSettings() => Settings;
    internal bool ConversationPromptCacheEnabled = true;
    internal bool ConversationContextEnabled = true;
    internal int ConversationContextRecentTurns = 12, ConversationContextRecentChars = 20000, ConversationContextInputChars = 100000, ConversationContextEstimatedTokens = 128000;
    internal int GetAuxiliaryApiMaxTokens() => 12000;
    internal int GetMainApiMaxTokens() => 12000;
    internal int ConversationPromptCacheMaxChars = 150000, ConversationPromptCacheTtlMinutes = 10;
    internal string ApiUrl = "https://relay.test/v1/chat/completions", Model = "gemini-test";
    internal string AuxiliaryApiUrl = "https://auxiliary.test/v1/chat/completions";
    internal string GetEffectiveAuxiliaryModelName() => "auxiliary-model";
    internal string GetEffectiveMainModelName() => Model;
}
internal static class ShoutBehavior { internal static string AppendPlayerCustomPromptRuleToSystemPrompt(string s) => "GLOBAL"; }
internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static bool IsCurrentGeneration(long g) => g == Generation; }
internal static class Logger { internal static void Log(string c, string t) { } }
internal static class IntradayConversationMemoryRuntime { internal static void Clear() { } }
internal static class ConversationContextSettingsCapture
{
    internal static ConversationContextOptions Capture() => new ConversationContextOptions {
        Enabled = DuelSettings.Settings.ConversationContextEnabled, InputChars = DuelSettings.Settings.ConversationContextInputChars };
}

internal static class Program
{
    private static int _checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly PromptCacheText Text = new PromptCacheText
    {
        Policy = "[AF_PROMPT_CACHE_V1]\nPOLICY", Snapshot = "ACTIVE:{ACTOR}:{SECTIONS}",
        Section = "DEFINE:{ACTOR}:{SECTIONS}", Input = "INPUT:{ACTOR}"
    };
    private static PromptCacheMessage M(string r, string t) => new PromptCacheMessage(r, t);
    private static List<PromptCacheMessage> Input(string speaker = "Alice", string rule = "RULE", string current = "QUESTION")
        => new List<PromptCacheMessage> { M("system", "GLOBAL\nPERSONA:" + speaker), M("user", "MEMORY"),
            M("user", rule), M("assistant", "OLD_NPC"), M("user", "[AFEF] EXECUTED_FACT"),
            M("user", "RELAY_CONTEXT"), M("user", current) };
    private static ConversationPromptCacheStore.Lease Begin(ConversationPromptCacheStore s, List<PromptCacheMessage> input,
        string actor = "agent:1", int budget = 50000, string key = "scene:1", long generation = 1, DateTime? now = null)
        => s.Begin(generation, key, actor, input, "GLOBAL", Text, budget, 10, now ?? Now);
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL " + name); _checks++; Console.WriteLine("PASS " + name); }
    private static string All(ConversationPromptCacheStore.Lease request) => string.Join("\n", request.Messages.Select(m => m.Content));
    private static JObject Payload(ConversationPromptCacheStore.Lease request, string model, bool stream = false)
        => new JObject { ["model"] = model, ["max_tokens"] = 1000, ["stream"] = stream,
            ["messages"] = new JArray(request.Messages.Select(m => new JObject { ["role"] = m.Role, ["content"] = m.Content })) };
    private static void Main()
    {
        var store = new ConversationPromptCacheStore();
        using var first = Begin(store, Input());
        foreach (string content in new[] { "PERSONA:Alice", "MEMORY", "RULE", "OLD_NPC", "[AFEF] EXECUTED_FACT", "RELAY_CONTEXT", "QUESTION" })
            Check(All(first).Contains(content), "first preserves " + content);
        Check(first.Messages.Any(m => m.Role == "assistant" && m.Content == "OLD_NPC"), "assistant role retained");
        Check(first.Commit("REAL_REPLY", true, Now), "accepted reply commits");
        Check(!first.Commit("DUPLICATE", true, Now), "double completion rejected");
        using var second = Begin(store, Input(current: "SECOND"));
        Check(second.Warm && first.Messages.SequenceEqual(second.Messages.Take(first.Messages.Count)), "exact prior request prefix");
        Check(second.Messages.Skip(first.Messages.Count).Count(m => m.Content == "MEMORY") == 0, "whole unchanged sections reused");
        Check(second.Messages.Last().Content == "SECOND", "current input always appended");
        Check(second.Commit("SECOND_REPLY", true, Now), "second reply commits");
        using var changed = Begin(store, Input(rule: "NEW_RULE", current: "THIRD"));
        string manifest = changed.Messages.Last(m => m.Content.StartsWith("ACTIVE:")).Content;
        Check(!manifest.Split(':').Last().Contains("3:user"), "removed old rule absent from active manifest");
        Check(changed.Messages.Any(m => m.Content == "NEW_RULE"), "replacement rule fully present");
        using var failed = Begin(store, Input(current: "FAILED")); failed.Dispose();
        using var afterFailure = Begin(store, Input(current: "AFTER_FAILURE"));
        Check(!afterFailure.Messages.Any(m => m.Content == "FAILED"), "failed request absent from transcript");
        Check(!changed.Commit("LATE", true, Now), "superseded completion rejected");
        Check(!afterFailure.Commit("STALE", false, Now), "stale source rejected");
        using var expired = Begin(store, Input(), now: Now.AddMinutes(11));
        Check(!expired.Warm && !All(expired).Contains("REAL_REPLY"), "TTL reset has no stale turns");
        Check(expired.Commit("TTL_REPLY", true, Now.AddMinutes(11)), "fresh TTL reply accepted");
        using var generation = Begin(store, Input(), generation: 2);
        Check(!generation.Warm, "save generation isolation");
        Check(!expired.Commit("OLD_GENERATION", true, Now), "old generation cannot commit");
        int scopesBeforeStale = store.Count;
        using var staleBegin = Begin(store, Input(), generation: 1);
        Check(staleBegin.Reason == "stale_generation" && store.Count == scopesBeforeStale, "late begin cannot clear newer generation");
        using var scene2 = Begin(store, Input(), key: "scene:2", generation: 2);
        Check(!scene2.Warm, "scene identity isolation despite identical system");
        using var provider2 = Begin(store, Input(), key: "scene:1:provider2", generation: 2);
        Check(!provider2.Warm, "provider identity isolation");
        var resetStore = new ConversationPromptCacheStore();
        using var seed = Begin(resetStore, Input(), budget: 2000);
        Check(seed.Commit(new string('x', 1200), true, Now), "seed near budget commits");
        using var reset = Begin(resetStore, Input(current: new string('q', 650)), budget: 2000);
        Check(reset.Reason == "budget_reset", "current input counted before budget decision");
        Check(reset.Messages.Any(m => m.Content.Contains("PERSONA:Alice")) && reset.Messages.Any(m => m.Content == "MEMORY"), "reset same turn restores full persona and memory");
        using var oversized = Begin(resetStore, Input(current: new string('z', 5000)), budget: 2000);
        Check(oversized.Reason == "snapshot_over_budget" && oversized.Messages.Last().Content.Length == 5000, "oversized request preserves complete original");
        Check(!oversized.Commit("NO_CACHE", true, Now), "oversized request not retained");
        var noGlobal = new ConversationPromptCacheStore();
        using var noRule = noGlobal.Begin(1, "npc", "id", Input(), "NOT_PRESENT", Text, 50000, 10, Now);
        Check(noRule.Messages[0].Content.StartsWith("GLOBAL\nPERSONA:Alice"), "missing global rule keeps original system authority");
        using var duplicate = Begin(new ConversationPromptCacheStore(), new List<PromptCacheMessage>
            { M("system", "GLOBAL"), M("user", "SAME"), M("assistant", "SAME"), M("user", "SAME"), M("user", "NOW") });
        Check(duplicate.Messages.Any(m => m.Role == "assistant" && m.Content == "SAME"), "identical texts retain distinct roles");
        Check(duplicate.Messages.First(m => m.Content.StartsWith("ACTIVE:")).Content.EndsWith("1:user,2:assistant,1:user"), "duplicate occurrences and order preserved");
        for (int i = 0; i < 80; i++) { using var pooled = Begin(store, Input(), key: "scope:" + i, generation: 2); pooled.Commit("ok", true, Now.AddSeconds(i)); }
        Check(store.Count <= 32, "bounded session pool");

        JObject claudeInput = Payload(first, "claude-test", true);
        string originalJson = claudeInput.ToString();
        JObject claude = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://api.anthropic.com/v1/messages", claudeInput));
        Check(claude["system"] is JArray && claude["system"][0]["cache_control"] != null, "native Anthropic keeps system breakpoints");
        Check(claude["messages"].Last["content"][0]["cache_control"] != null, "native Anthropic keeps transcript breakpoint");
        Check(claude["stream_options"] == null, "native Anthropic has no unsupported stream_options");
        Check(claudeInput.ToString() == originalJson, "serialization does not mutate source payload");
        JObject claudeChat = LlmApiCompat.PrepareChatRequestPayload("https://relay.test/v1/chat/completions", Payload(first, "claude-test"));
        Check(claudeChat["messages"][0]["content"][0]["cache_control"] != null, "Claude chat proxy keeps breakpoints");
        JObject gemini = LlmApiCompat.PrepareChatRequestPayload("https://relay.test/v1/chat/completions", Payload(first, "gemini-test", true));
        Check(!gemini.ToString().Contains("cache_control"), "Gemini receives no Anthropic cache control");
        Check(gemini["stream_options"]?["include_usage"]?.Value<bool>() == true, "chat stream usage requested");
        JArray geminiMessages = (JArray)gemini["messages"];
        Check(!geminiMessages.Zip(geminiMessages.Skip(1), (a, b) => a["role"].ToString() == b["role"].ToString()).Any(x => x), "Gemini final wire roles alternate");
        JObject gemini2 = LlmApiCompat.PrepareChatRequestPayload("https://relay.test/v1/chat/completions", Payload(second, "gemini-test", true));
        Check(geminiMessages.Select((m, i) => JToken.DeepEquals(m, gemini2["messages"][i])).All(x => x), "Gemini final wire preserves complete prior prefix");
        JObject ordinary = JObject.Parse("{\"model\":\"other\",\"messages\":[{\"role\":\"system\",\"content\":\"ordinary\"},{\"role\":\"user\",\"content\":\"hi\"}]}");
        Check(JToken.DeepEquals(ordinary, LlmApiCompat.PrepareChatRequestPayload("https://relay.test/v1/chat/completions", ordinary)), "unrelated courier and auxiliary payload unchanged");

        ConversationPromptCache.Clear();
        var sourceMessages = Input().Select(m => (object)new { role = m.Role, content = m.Content }).ToList();
        string before = Newtonsoft.Json.JsonConvert.SerializeObject(sourceMessages);
        using var adapter1 = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:alice");
        adapter1.Accept("ADAPTER_REPLY");
        using var adapter2 = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:alice");
        Check(Newtonsoft.Json.JsonConvert.SerializeObject(sourceMessages) == before, "real adapter leaves authoritative original list unchanged");
        Check(Newtonsoft.Json.JsonConvert.SerializeObject(adapter2.Messages).Contains("ADAPTER_REPLY"), "real native adapter receives only its accepted reply");
        using var otherNative = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:bob");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(otherNative.Messages).Contains("ADAPTER_REPLY"), "native targets cannot share accepted replies");
        using var sceneAdapter = ConversationPromptCache.Begin(sourceMessages, "scene", 1, 9, "agent:1");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(sceneAdapter.Messages).Contains("ADAPTER_REPLY"), "native and scene channels isolated");
        sceneAdapter.Accept("SCENE_REPLY");
        using var nextSpeaker = ConversationPromptCache.Begin(Input("Bob").Select(m => (object)new { role=m.Role, content=m.Content }).ToList(), "scene", 1, 9, "agent:2");
        Check(Newtonsoft.Json.JsonConvert.SerializeObject(nextSpeaker.Messages).Contains("PERSONA:Bob"), "new scene speaker receives complete own context");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(nextSpeaker.Messages).Contains("SCENE_REPLY"), "scene speaker switches cannot expose private transcript");
        Check(PrimaryChatMessagePolicy.TryReadMessage(sceneAdapter.Messages[0], out _, out string publicPrefix)
            && PrimaryChatMessagePolicy.TryReadMessage(nextSpeaker.Messages[0], out _, out string nextPrefix)
            && publicPrefix == nextPrefix, "scene speakers retain identical shared public prefix");
        using var auxAdapter = ConversationPromptCache.Begin(sourceMessages, "scene_auxiliary", 1, 9, "agent:1");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(auxAdapter.Messages).Contains("SCENE_REPLY"), "scene auxiliary provider cannot reuse main-provider transcript");
        using var staleAdapter = ConversationPromptCache.Begin(sourceMessages, "native", 0, 9, "hero:alice");
        using var afterStaleAdapter = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:alice");
        Check(Newtonsoft.Json.JsonConvert.SerializeObject(afterStaleAdapter.Messages).Contains("ADAPTER_REPLY"), "stale adapter request does not clear current sessions");
        var multimodal = new List<object> { new { role = "system", content = "GLOBAL" }, new { role = "user", content = new object[] { new { type = "image_url", image_url = "fixture" } } } };
        using var unsupported = ConversationPromptCache.Begin(multimodal, "native", 1, 9, "hero:alice");
        Check(ReferenceEquals(unsupported.Messages, multimodal), "unsupported multimodal content remains byte-shape intact");
        using var cancelledAdapter = ConversationPromptCache.Begin(sourceMessages, "native", 1, 11, "hero:alice");
        cancelledAdapter.Accept("CANCELLED_SAME_GENERATION", false);
        using var nextAfterCancel = ConversationPromptCache.Begin(sourceMessages, "native", 1, 11, "hero:alice");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(nextAfterCancel.Messages).Contains("CANCELLED_SAME_GENERATION"), "same-generation cancelled owner cannot publish cache reply");
        DuelSettings.Settings.Model = "different-model";
        using var modelChange = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:alice");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(modelChange.Messages).Contains("ADAPTER_REPLY"), "real adapter rebases on model change");
        DuelSettings.Settings.ConversationPromptCacheEnabled = false;
        using var disabled = ConversationPromptCache.Begin(sourceMessages, "native", 1, 9, "hero:alice");
        Check(ReferenceEquals(disabled.Messages, sourceMessages), "disable restores original messages without rewriting");
        DuelSettings.Settings.ConversationContextInputChars = 20000;
        var oversizedHistory = new List<object> { new ConversationPromptMessage("system", "GLOBAL"),
            new ConversationPromptMessage("user", new string('X', 21000), 1),
            new ConversationPromptMessage("assistant", "OLD_REPLY", 1), new ConversationPromptMessage("user", "CURRENT_INPUT") };
        using var boundedWithoutCache = ConversationPromptCache.Begin(oversizedHistory, "native", 1, 9, "hero:alice");
        Check(boundedWithoutCache.Messages.Count == 2 && oversizedHistory.Count == 4,
            "disabling cache preserves independent context budget and original source");
        DuelSettings.Settings.ConversationPromptCacheEnabled = true;
        var mandatoryLarge = new List<object> { new ConversationPromptMessage("system", new string('S', 30000)),
            new ConversationPromptMessage("user", "OLD_DROP", 1), new ConversationPromptMessage("assistant", "OLD_REPLY_DROP", 1),
            new ConversationPromptMessage("user", "CURRENT_KEEP") };
        using var continuing = ConversationPromptCache.Begin(mandatoryLarge, "native", 1, 12, "hero:alice");
        Check(continuing.Messages.Count == 2 && mandatoryLarge.Count == 4
            && Newtonsoft.Json.JsonConvert.SerializeObject(continuing.Messages).Contains("CURRENT_KEEP"),
            "required floor continues beyond target without retaining or replaying oversized history");
        DuelSettings.Settings.ConversationContextInputChars = 100000;
        Check(ConversationRuntimeTexts.Prompt("AF_CONV_Cache_Policy").Length > 100, "real package-language reader supplies cache policy");
        Console.WriteLine("Checks=" + _checks);
    }
}
