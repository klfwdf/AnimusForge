using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class Program
{
    private static int checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly PromptCacheText Text = new PromptCacheText { Policy = "edition-policy" };
    private static PromptCacheMessage M(string role, string text, int group = 0, string revision = "") => new PromptCacheMessage(role, text, group, revision);
    private static List<PromptCacheMessage> Input(string actor = "Alice", string state = "sitting", string question = "Q") => new List<PromptCacheMessage> {
        M("system", "GLOBAL\nPersona: " + actor + "\nCurrent action: " + state),
        M("user", "Memory: " + actor), M("user", "The current time is noon\nYou are at the tavern\nStanding before you: Player\nDistance from you: 3m"),
        M("user", "[Additional rules]\nRULE_KEEP\n[Knowledge recall]\nKNOWLEDGE_KEEP"),
        M("assistant", "OLD_SPEECH"), M("user", "[AFEF] EXECUTED_FACT"), M("user", "RELAY_CONTEXT"), M("user", question) };
    private static ConversationPromptCacheStore.Lease Begin(ConversationPromptCacheStore store, List<PromptCacheMessage> source,
        string actor = "Alice", string key = "scene:1", bool scene = true, string global = "GLOBAL", int budget = 100000,
        long generation = 1, DateTime? now = null) => store.Begin(generation, key, actor, source, global, Text, budget, 10, now ?? Now, sceneTranscript: scene);
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
    private static string All(ConversationPromptCacheStore.Lease lease) => string.Join("\n", lease.Messages.Select(x => x.Content));
    private static JObject Payload(ConversationPromptCacheStore.Lease lease, string model) => new JObject {
        ["model"] = model, ["max_tokens"] = 12000, ["stream"] = true,
        ["messages"] = new JArray(lease.Messages.Select(x => new JObject { ["role"] = x.Role, ["content"] = x.Content })) };
    private static void Main()
    {
        var store = new ConversationPromptCacheStore();
        using var first = Begin(store, Input());
        foreach (string content in new[] { "Persona: Alice", "Memory: Alice", "RULE_KEEP", "KNOWLEDGE_KEEP", "OLD_SPEECH", "[AFEF] EXECUTED_FACT", "RELAY_CONTEXT", "Distance from you: 3m", "Q" })
            Check(All(first).Contains(content), "scene preserves " + content);
        Check(first.Messages.Count == 2 && first.Messages[0].Content.StartsWith("GLOBAL"), "original scene algorithm uses fixed global system and one cue");
        Check(first.Commit("ALICE_ACTUAL_REPLY", true, Now), "accepted scene response commits");
        using var bob = Begin(store, Input("Bob", "standing", "BOB_Q"), actor: "Bob");
        Check(bob.Warm && bob.PrefixAppend && first.Messages.SequenceEqual(bob.Messages.Take(first.Messages.Count)), "switching NPC extends the same scene transcript byte for byte");
        Check(bob.Messages.Any(x => x.Role == "assistant" && x.Content == "ALICE_ACTUAL_REPLY"), "actual prior speaker reply retains assistant role");
        Check(bob.Messages.Last().Content.Contains("Bob") && bob.Messages.Last().Content.Contains("BOB_Q"), "last cue selects only the current speaker");
        Check(bob.Commit("BOB_ACTUAL_REPLY", true, Now.AddSeconds(1)), "second speaker commits");
        using var alice = Begin(store, Input("Alice", "walking", "ALICE_Q2"));
        Check(alice.Warm && alice.PrefixAppend && alice.Messages[0].Equals(first.Messages[0]), "speaker action change never rewrites scene system");
        Check(alice.Messages.Last().Content.Contains("walking") && All(alice).Contains("BOB_ACTUAL_REPLY"), "latest state update and interleaved speaker retained");
        alice.Dispose();
        using (var failed = Begin(store, Input("Alice", "FAILED_STATE", "FAILED_INPUT"))) { }
        using var afterFailure = Begin(store, Input("Alice", "FAILED_STATE", "RETRY_INPUT"));
        Check(!All(afterFailure).Contains("FAILED_INPUT") && afterFailure.Messages.Last().Content.Contains("FAILED_STATE"), "failed build publishes neither cue nor dedup state");
        using var newer = Begin(store, Input("Bob", "new", "NEWER"), actor: "Bob");
        Check(!afterFailure.Commit("LATE_REPLY", true, Now), "superseded reply rejected by exact ticket");
        Check(!newer.Commit("STALE_REPLY", false, Now), "stale scene reply rejected");
        using var expired = Begin(store, Input(), now: Now.AddMinutes(11));
        Check(!expired.Warm && !All(expired).Contains("BOB_ACTUAL_REPLY"), "TTL creates a complete clean snapshot");
        expired.Commit("EXPIRED_REPLY", true, Now.AddMinutes(11));
        using var generation = Begin(store, Input(), generation: 2);
        Check(!generation.Warm, "save generation changes clear transcript");
        int count = store.Count;
        using var stale = Begin(store, Input(), generation: 1);
        Check(stale.Reason == "stale_generation" && store.Count == count, "late old generation cannot clear new cache");
        using var otherScene = Begin(store, Input(), key: "scene:2", generation: 2);
        Check(!otherScene.Warm, "different scene is isolated");
        using var provider = Begin(store, Input(), key: "scene:1:provider2", generation: 2);
        Check(!provider.Warm, "different provider is isolated");

        var native = new ConversationPromptCacheStore();
        using var n1 = Begin(native, Input(), scene: false, global: "", key: "native:Alice");
        Check(n1.Messages.Any(x => x.Role == "assistant" && x.Content == "OLD_SPEECH"), "first native request preserves source speech roles");
        n1.Commit("NATIVE_REPLY", true, Now);
        var nextNative = Input(state: "walking", question: "NATIVE_Q2");
        nextNative[nextNative.Count - 2] = M("user", "NEW_RELAY_CONTEXT");
        using var n2 = Begin(native, nextNative, scene: false, global: "", key: "native:Alice");
        Check(n2.Warm && n2.PrefixAppend && n1.Messages.SequenceEqual(n2.Messages.Take(n1.Messages.Count)), "original native frozen prefix survives action changes without custom rule");
        Check(n2.Messages.Last().Content.Contains("walking") && n2.Messages.Last().Content.Contains("NEW_RELAY_CONTEXT"), "native changed system and suffix facts are appended");
        var noRule = new ConversationPromptCacheStore();
        using var fallbackA = Begin(noRule, Input(), global: ""); fallbackA.Commit("A_PRIVATE", true, Now);
        using var fallbackB = Begin(noRule, Input("Bob"), actor: "Bob", global: "");
        Check(!fallbackB.Warm && !All(fallbackB).Contains("A_PRIVATE"), "original no-global-rule fallback keeps NPC systems separate");

        var resetStore = new ConversationPromptCacheStore();
        using var seed = Begin(resetStore, Input(), budget: 5000); seed.Commit(new string('R', 3000), true, Now);
        using var reset = Begin(resetStore, Input(question: new string('Q', 2500)), budget: 5000);
        Check(reset.Reason == "budget_reset" && All(reset).Contains("Persona: Alice") && All(reset).Contains("RULE_KEEP"), "budget rebuild regenerates full snapshot before deduplication");
        using var oversized = Begin(resetStore, Input(question: new string('L', 10000)), budget: 5000);
        Check(oversized.Reason == "snapshot_over_budget" && oversized.Messages.Last().Content.Length == 10000, "oversized required request continues unchanged");
        Check(!oversized.Commit("NO_CACHE", true, Now), "oversized snapshot is not retained");
        for (int i = 0; i < 40; i++) { using var item = Begin(store, Input(), key: "scope:" + i, generation: 2); item.Commit("ok", true, Now.AddSeconds(i)); }
        Check(store.Count <= 32, "pool is bounded at request time");

        foreach (string model in new[] { "gemini-test", "deepseek-test", "claude-test" })
        {
            var p1 = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://relay.test/v1/chat/completions", Payload(first, model)));
            var p2 = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://relay.test/v1/chat/completions", Payload(bob, model)));
            Check(p1["messages"].Children().Select((m, i) => JToken.DeepEquals(m["role"], p2["messages"][i]["role"])
                && JToken.DeepEquals(m["content"], p2["messages"][i]["content"])).All(x => x), "final wire prefix retained for " + model);
            Check(p2["stream_options"]?["include_usage"]?.Value<bool>() == true, "stream usage requested for " + model);
            if (model.StartsWith("gemini")) Check(!p2.ToString().Contains("cache_control") && p2["messages"].Count() == 4, "Gemini original alternating transcript has no foreign cache controls");
        }
        var dedup = new ConversationSessionCache("Alice", 1);
        string baseState = "[State]\nALPHA\nBETA\nGAMMA\nDELTA";
        DynamicBlockDeduplicator.ApplySections(dedup, baseState, "native");
        Check(DynamicBlockDeduplicator.ApplySections(dedup, "[State]\nDELTA\nGAMMA\nBETA\nALPHA", "native") == DynamicBlockDeduplicator.NoChangeMarker, "original line-set dedup ignores reranking order in EN headings");
        string delta = DynamicBlockDeduplicator.ApplySections(dedup, "[State]\nALPHA\nBETA\nGAMMA\nEPSILON", "native");
        Check(delta.Contains("DELTA") && delta.Contains("EPSILON"), "state delta explicitly invalidates removed lines");
        string multiBase = "[A]\nONE\n[B]\nTWO\n[C]\nTHREE";
        DynamicBlockDeduplicator.ApplySections(dedup, multiBase, "scene");
        Check(DynamicBlockDeduplicator.ApplySections(dedup, "[B]\nTWO", "scene") != DynamicBlockDeduplicator.NoChangeMarker
            && DynamicBlockDeduplicator.ApplySections(dedup, "", "scene") != DynamicBlockDeduplicator.NoChangeMarker,
            "vanished multi-section and empty block emit removals and clear state");
        Check(DynamicBlockDeduplicator.ApplySections(dedup, multiBase, "scene").Contains("THREE"),
            "cleared state resends full sections after expiry");

        ConversationPromptCache.Clear();
        var source = Input().Select(x => (object)new ConversationPromptMessage(x.Role, x.Content)).ToList();
        using var adapterA = ConversationPromptCache.Begin(source, "scene", 1, 5, "Alice"); adapterA.Accept("ADAPTER_REPLY");
        var sourceB = Input("Bob").Select(x => (object)new ConversationPromptMessage(x.Role, x.Content)).ToList();
        using var adapterB = ConversationPromptCache.Begin(sourceB, "scene", 1, 5, "Bob");
        Check(Newtonsoft.Json.JsonConvert.SerializeObject(adapterB.Messages).Contains("ADAPTER_REPLY"), "production adapter shares scene transcript across actors");
        using var aux = ConversationPromptCache.Begin(sourceB, "scene_auxiliary", 1, 5, "Bob");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(aux.Messages).Contains("ADAPTER_REPLY"), "auxiliary remains separate from main transcript");
        using var nativeAdapter = ConversationPromptCache.Begin(sourceB, "native", 1, 5, "Bob");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(nativeAdapter.Messages).Contains("ADAPTER_REPLY"), "native remains separate from scene transcript");
        DuelSettings.Settings.ConversationPromptCacheEnabled = false;
        using var disabled = ConversationPromptCache.Begin(source, "scene", 1, 5, "Alice");
        Check(ReferenceEquals(disabled.Messages, source), "disabled cache preserves original request");
        DuelSettings.Settings.ConversationContextInputChars = 20000;
        var huge = new List<object> { new ConversationPromptMessage("system", "GLOBAL"), new ConversationPromptMessage("user", new string('X', 21000), 1), new ConversationPromptMessage("assistant", "DROP", 1), new ConversationPromptMessage("user", "KEEP") };
        using var trimmed = ConversationPromptCache.Begin(huge, "native", 1, 9, "Alice");
        Check(trimmed.Messages.Count == 2 && huge.Count == 4, "context budget still removes complete optional groups while caching disabled");
        DuelSettings.Settings.ConversationPromptCacheEnabled = true;
        var required = new List<object> { new ConversationPromptMessage("system", new string('S', 30000)), new ConversationPromptMessage("user", "KEEP") };
        using var continuing = ConversationPromptCache.Begin(required, "native", 1, 9, "Alice");
        Check(continuing.Messages.Count == 2, "mandatory floor continues over local target without an error");
        Console.WriteLine("Checks=" + checks);
    }
}
