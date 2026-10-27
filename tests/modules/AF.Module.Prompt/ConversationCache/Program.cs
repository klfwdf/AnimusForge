using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class Program
{
    private static int checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc);
    private static readonly PromptCacheText Text = new PromptCacheText { Policy = "edition-policy" };
    private static PromptCacheMessage M(string role, string text, int group = 0, string revision = "") => new PromptCacheMessage(role, text, group, revision);
    private static List<PromptCacheMessage> Input(string actor = "Alice", string state = "sitting", string question = "Q") => new List<PromptCacheMessage> {
        M("system", "GLOBAL\nPersona: " + actor + "\nCurrent action: " + state),
        M("user", "Memory: " + actor), M("user", "[State]\nGOLD=1000\n[Knowledge]\nKNOWLEDGE_KEEP"),
        M("user", "Buy prisoner for 2000.", 1), M("assistant", "Agreed. Pay 2000.", 1),
        M("user", "[AFEF PLAYER_ACTION] Paid 2000."), M("user", "RELAY_CONTEXT"), M("user", question) };
    private static ConversationPromptCacheStore.Lease Begin(ConversationPromptCacheStore store, List<PromptCacheMessage> source,
        string actor = "Alice", string key = "scene:1", bool scene = true, string global = "GLOBAL", int budget = 100000,
        long generation = 1, DateTime? now = null) => store.Begin(generation, key, actor, source, global, Text, budget, 10, now ?? Now, sceneTranscript: scene);
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
    private static string All(ConversationPromptCacheStore.Lease lease) => string.Join("\n", lease.Messages.Select(x => x.Content));
    private static int Occurrences(string value, string part) => value.Split(new[] { part }, StringSplitOptions.None).Length - 1;
    private static void ExactSource(ConversationPromptCacheStore.Lease lease, List<PromptCacheMessage> source, string label)
    {
        Check(lease.Messages.Count == source.Count && lease.Messages[0].Content.StartsWith(source[0].Content + "\n\n[AF_PROMPT_CACHE_V1]\n", StringComparison.Ordinal)
            && lease.Messages.Skip(1).SequenceEqual(source.Skip(1)), label + " preserves every selected message, role and order");
    }
    private static JObject Payload(ConversationPromptCacheStore.Lease lease, string model) => new JObject {
        ["model"] = model, ["max_tokens"] = 12000, ["stream"] = true,
        ["messages"] = new JArray(lease.Messages.Select(x => new JObject { ["role"] = x.Role, ["content"] = x.Content })) };
    private static void Main()
    {
        var store = new ConversationPromptCacheStore();
        var original = Input();
        using var first = Begin(store, original);
        ExactSource(first, original, "initial snapshot");
        Check(first.Messages.Count(x => x.Role == "system") == 1 && Occurrences(All(first), "[AF_PROMPT_CACHE_V1]") == 1, "single system and cache policy marker");
        Check(first.Commit("ACCEPTED_REPLY", true, Now), "accepted response commits exact request lease");
        var changed = Input(state: "walking", question: "Q2");
        changed[2] = M("user", "[State]\nGOLD=3000\n[Knowledge]\nKNOWLEDGE_KEEP");
        // The actual history owner, not the transport cache, supplies accepted dialogue.
        changed.Insert(changed.Count - 1, M("user", "Q", 2));
        changed.Insert(changed.Count - 1, M("assistant", "ACCEPTED_REPLY", 2));
        using var latest = Begin(store, changed);
        ExactSource(latest, changed, "replacement snapshot");
        Check(latest.Warm && !latest.PrefixAppend && latest.Reason == "snapshot_replace", "state replacement reports changed prefix instead of append-only claim");
        Check(All(latest).Contains("GOLD=3000") && !All(latest).Contains("GOLD=1000") && !All(latest).Contains("sitting"), "old gold and old system action physically absent");
        Check(Occurrences(All(latest), "KNOWLEDGE_KEEP") == 1 && All(latest).Contains("ACCEPTED_REPLY"), "unchanged knowledge once and selected accepted dialogue retained");
        Check(!All(latest).Contains(PromptCacheCoreTexts.Get("Removed").Trim()), "no generated invalidation manifest");
        latest.Commit("SECOND_REPLY", true, Now);
        var failedSource = changed.ToList(); failedSource[2] = M("user", "FAILED_STATE"); failedSource[failedSource.Count - 1] = M("user", "FAILED_INPUT");
        using (var failed = Begin(store, failedSource)) { }
        using var retry = Begin(store, changed);
        Check(retry.Warm && !All(retry).Contains("FAILED_INPUT") && !All(retry).Contains("FAILED_STATE"), "failed build neither publishes nor evicts committed snapshot");
        using var newer = Begin(store, changed);
        Check(!retry.Commit("LATE_REPLY", true, Now), "superseded ticket rejected");
        Check(!newer.Commit("STALE_REPLY", false, Now), "stale source reply rejected");
        using var bob = Begin(store, Input("Bob"), actor: "Bob");
        Check(bob.Warm && !All(bob).Contains("Memory: Alice") && !All(bob).Contains("SECOND_REPLY") && !All(bob).Contains("Persona: Alice"), "scene speaker switch contains only current viewer context");
        using var expired = Begin(store, changed, now: Now.AddMinutes(11));
        Check(!expired.Warm, "TTL resets prefix diagnostics");
        ExactSource(expired, changed, "TTL reset");
        expired.Commit("TTL_REPLY", true, Now.AddMinutes(11));
        using var generation = Begin(store, original, generation: 2);
        Check(!generation.Warm, "generation change resets pool");
        int count = store.Count;
        using var stale = Begin(store, original, generation: 1);
        Check(stale.Reason == "stale_generation" && store.Count == count, "old generation cannot clear new cache");
        using var otherScene = Begin(store, original, key: "scene:2", generation: 2);
        using var provider = Begin(store, original, key: "scene:1:provider2", generation: 2);
        Check(!otherScene.Warm && !provider.Warm, "scene and provider scopes isolated");
        var revisionStore = new ConversationPromptCacheStore();
        using var revisionFirst = Begin(revisionStore, original); revisionFirst.Commit("OLD", true, Now);
        var compressed = Input(); compressed[0] = M("system", compressed[0].Content, revision: "compressed-v2");
        compressed.RemoveRange(3, 2);
        using var revisionNext = Begin(revisionStore, compressed);
        Check(revisionNext.Reason == "history_rebuilt" && !All(revisionNext).Contains("Agreed. Pay 2000."), "compression or edit cannot resurrect removed history from transport cache");
        ExactSource(revisionNext, compressed, "compressed owner selection");
        var capacity = new ConversationPromptCacheStore();
        using var small = Begin(capacity, original, budget: 5000);
        Check(small.Commit(new string('R', 10000), true, Now), "unretained reply length does not evict current snapshot");
        var largeInput = Input(question: new string('L', 10000));
        using var large = Begin(capacity, largeInput, budget: 5000);
        Check(large.Reason == "snapshot_over_budget" && large.Messages.SequenceEqual(largeInput) && !large.Commit("reply", true, Now), "retention cap falls back losslessly without dropping selected messages");
        for (int i = 0; i < 40; i++) { using var item = Begin(store, original, key: "scope:" + i, generation: 2); item.Commit("ok", true, Now.AddSeconds(i)); }
        Check(store.Count <= 32, "request-frequency pool remains bounded");

        foreach (string model in new[] { "gemini-test", "deepseek-test", "claude-test" })
        {
            var wire = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://relay.test/v1/chat/completions", Payload(latest, model)));
            string serialized = wire.ToString();
            Check(!serialized.Contains("GOLD=1000") && serialized.Contains("GOLD=3000") && Occurrences(serialized, "KNOWLEDGE_KEEP") == 1, "final wire latest state exactly once for " + model);
            Check(serialized.Contains("Agreed. Pay 2000.") && serialized.Contains("Paid 2000.") && serialized.Contains("ACCEPTED_REPLY"), "final wire speech and AFEF retained for " + model);
            Check(wire["stream_options"]?["include_usage"]?.Value<bool>() == true, "stream usage requested for " + model);
            if (model.StartsWith("gemini")) Check(!serialized.Contains("cache_control"), "Gemini gets no foreign cache-control directives");
        }

        {
            // Claude transcript breakpoint must sit on the latest assistant turn, not the request-varying tail.
            var claude = JObject.Parse(LlmApiCompat.PrepareChatRequestJson("https://relay.test/v1/chat/completions", Payload(latest, "claude-test")));
            var wireMessages = (JArray)claude["messages"];
            int lastAssistant = -1;
            for (int i = 0; i < wireMessages.Count; i++) if (wireMessages[i]["role"]?.ToString() == "assistant") lastAssistant = i;
            Check(lastAssistant > 0 && wireMessages[lastAssistant]["cache_control"] != null, "Claude transcript breakpoint on latest assistant turn");
            Check(wireMessages[wireMessages.Count - 1]["cache_control"] == null || lastAssistant == wireMessages.Count - 1, "Claude does not mark the per-request tail");
            Check(wireMessages.Count(x => x["cache_control"] != null) <= 1, "Claude adds at most one transcript breakpoint");
        }
        Check(LlmApiCompat.FormatPromptCacheUsage(JObject.Parse("{\"prompt_tokens\":1000,\"prompt_cache_hit_tokens\":800,\"prompt_cache_miss_tokens\":200}"))
            == "prompt_tokens=1000 cached_tokens=800 cache_write_tokens=n/a hit_rate=80.0%", "DeepSeek usage cache accounting");
        Check(LlmApiCompat.FormatPromptCacheUsage(JObject.Parse("{\"prompt_tokens\":2000,\"prompt_tokens_details\":{\"cached_tokens\":1500}}"))
            .StartsWith("prompt_tokens=2000 cached_tokens=1500"), "OpenAI-compatible usage cache accounting");
        Check(LlmApiCompat.FormatPromptCacheUsage(JObject.Parse("{\"input_tokens\":100,\"cache_read_input_tokens\":900,\"cache_creation_input_tokens\":0}"))
            == "prompt_tokens=1000 cached_tokens=900 cache_write_tokens=0 hit_rate=90.0%", "Anthropic input_tokens excludes cache reads");
        Check(LlmApiCompat.FormatPromptCacheUsage(LlmApiCompat.ExtractUsageObject(JObject.Parse("{\"usageMetadata\":{\"promptTokenCount\":400,\"cachedContentTokenCount\":100}}")))
            == "prompt_tokens=400 cached_tokens=100 cache_write_tokens=n/a hit_rate=25.0%", "Gemini native usage cache accounting");
        Check(LlmApiCompat.FormatPromptCacheUsage(JObject.Parse("{\"prompt_tokens\":500}"))== "prompt_tokens=500 cached_tokens=n/a cache_write_tokens=n/a hit_rate=n/a", "missing cache fields are not estimated");
        Check(LlmApiCompat.FormatPromptCacheUsage(JObject.Parse("{\"completion_tokens\":5}")) == "", "output-only usage chunk yields no prompt figures");
        Check(LlmApiCompat.ExtractUsageObject(JObject.Parse("{\"message\":\"ok\"}")) == null
            && LlmApiCompat.ExtractUsageObject(JObject.Parse("{\"message\":null,\"usage\":null}")) == null,
            "relay string/null message and null usage do not throw");
        Check(LlmApiCompat.FormatPromptCacheUsage(LlmApiCompat.ExtractUsageObject(JObject.Parse(
            "{\"type\":\"message_start\",\"message\":{\"usage\":{\"input_tokens\":50,\"cache_read_input_tokens\":950,\"cache_creation_input_tokens\":0}}}")))
            == "prompt_tokens=1000 cached_tokens=950 cache_write_tokens=0 hit_rate=95.0%", "Anthropic message_start nested usage extracted");
        // Stream capture: relay string/null "message" keeps text; output-only delta keeps prompt figures.
        string sse = "data: {\"message\":\"ok\",\"usage\":null,\"choices\":[{\"delta\":{\"content\":\"A\"}}]}\n\n"
            + "data: {\"message\":null,\"choices\":[{\"delta\":{\"content\":\"B\"}}]}\n\n"
            + "data: {\"choices\":[],\"usage\":{\"prompt_tokens\":1000,\"prompt_tokens_details\":{\"cached_tokens\":750}}}\n\n"
            + "data: {\"type\":\"message_delta\",\"usage\":{\"output_tokens\":7}}\n\ndata: [DONE]\n\n";
        var streamed = LlmStreamingTransport.SendAsync("http://127.0.0.1/v1/chat/completions", "k", "{}",
            (req, token) => System.Threading.Tasks.Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                { Content = new System.Net.Http.StringContent(sse) }), System.Threading.CancellationToken.None).GetAwaiter().GetResult();
        Check(streamed.Content == "AB" && !streamed.ParseFailure, "usage parsing never drops stream text or flags parse failure");
        Check(streamed.PromptCacheUsage == "prompt_tokens=1000 cached_tokens=750 cache_write_tokens=n/a hit_rate=75.0%",
            "stream usage formatted once from the prompt-bearing chunk: " + streamed.PromptCacheUsage);

        foreach (string channel in new[] { "native", "scene", "courier", "scene_auxiliary" })
        {
            ConversationPromptCache.Clear();
            DuelSettings.Settings.ConversationContextInputChars = 20000;
            DuelSettings.Settings.MainApiMaxTokens = 64000;
            string longBackground = "BACKGROUND_BEGIN_" + new string('B', 5400) + "_END";
            var selected = new List<object> {
                new ConversationPromptMessage("system", "GLOBAL\n" + new string('规', 50000)),
                new ConversationPromptMessage("user", "[State]\nGOLD=1000\nSTALE_SECTION\n[Background]\n" + longBackground),
                new ConversationPromptMessage("user", "I said GOLD=1000, [No longer valid this turn], 【本轮失效】 and [State] literally.", 1),
                new ConversationPromptMessage("assistant", "Agreed. Pay 2000 and take the prisoner.", 1),
                new ConversationPromptMessage("user", "[AFEF PLAYER_ACTION] Paid 2000."),
                new ConversationPromptMessage("user", "Here you are.") };
            string system = ((ConversationPromptMessage)selected[0]).content;
            using var initial = ConversationPromptCache.Begin(selected, channel, 1, 10, "Alice");
            initial.Accept("PAYMENT_REPLY");
            string stableSystem = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(initial.Messages))[0]["content"].Value<string>();
            long lastChars = 0;
            for (int turn = 2; turn <= 8; turn++)
            {
                selected[1] = new ConversationPromptMessage("user", "[State]\nGOLD=" + (turn * 1000) + "\n[Background]\n" + longBackground);
                selected[selected.Count - 1] = new ConversationPromptMessage("user", "Continue " + turn);
                using var request = ConversationPromptCache.Begin(selected, channel, 1, 10, "Alice");
                var wire = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(request.Messages));
                string currentState = wire[1]["content"].Value<string>();
                Check(!currentState.Contains("GOLD=1000") && !currentState.Contains("STALE_SECTION")
                    && currentState.Contains("GOLD=" + (turn * 1000)) && Occurrences(wire.ToString(), longBackground) == 1,
                    channel + " turn " + turn + " removes obsolete state and carries one complete background");
                Check(wire.Count == selected.Count && wire.Skip(1).Select((x, i) => x["role"].Value<string>() == ((ConversationPromptMessage)selected[i+1]).role
                    && x["content"].Value<string>() == ((ConversationPromptMessage)selected[i+1]).content).All(x => x),
                    channel + " turn " + turn + " preserves selected history, quoted markers, AFEF and input byte for byte");
                Check(wire[0]["content"].Value<string>() == stableSystem && wire.Count(x => x["role"].Value<string>() == "system") == 1,
                    channel + " turn " + turn + " retains a single identical system above final budget target");
                long chars = request.Messages.Sum(x => ((string)x.GetType().GetProperty("content").GetValue(x)).Length);
                Check(lastChars == 0 || chars == lastChars, channel + " turn " + turn + " input length stays flat instead of accumulating snapshots");
                lastChars = chars;
                request.Accept("reply " + turn);
            }
            selected[1] = new ConversationPromptMessage("user", "");
            using var empty = ConversationPromptCache.Begin(selected, channel, 1, 10, "Alice");
            Check(!Newtonsoft.Json.JsonConvert.SerializeObject(empty.Messages).Contains(longBackground), channel + " empty authoritative state removes old background without invalidation text");
        }
        DuelSettings.Settings.MainApiMaxTokens = 12000;
        ConversationPromptCache.Clear();
        List<object> ActorRequest(string actor) => new List<object> {
            new ConversationPromptMessage("system", "GLOBAL FIXED_SCENE_SYSTEM"),
            new ConversationPromptMessage("user", "PROFILE_" + actor),
            new ConversationPromptMessage("user", "PRIVATE_" + actor + " CURRENT_INPUT") };
        ConversationPromptCacheStore.Lease Lease(ConversationPromptCache.Request request) =>
            (ConversationPromptCacheStore.Lease)typeof(ConversationPromptCache.Request).GetField("_lease", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(request);
        using var actorAlice = ConversationPromptCache.Begin(ActorRequest("Alice"), "scene", 1, 90, "Alice");
        actorAlice.Accept("Alice reply");
        using var actorBob = ConversationPromptCache.Begin(ActorRequest("Bob"), "scene", 1, 90, "Bob");
        Check(!Lease(actorBob).Warm, "scene adapter isolates each actor lease instead of overwriting another actor snapshot");
        actorBob.Accept("Bob reply");
        using var aliceAgain = ConversationPromptCache.Begin(ActorRequest("Alice"), "scene", 1, 90, "Alice");
        Check(Lease(aliceAgain).Warm && Lease(aliceAgain).PrefixAppend, "scene adapter retains actor prefix across intervening speakers");
        Check(!Newtonsoft.Json.JsonConvert.SerializeObject(aliceAgain.Messages).Contains("Bob"), "scene adapter never shares another actor private prompt");
        Console.WriteLine("Checks=" + checks);
    }
}
