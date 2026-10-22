using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;
internal static class Program
{
    private static int checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
    private static void Check(bool result, string name) { if (!result) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static ConversationMessage M(string role, string text) => new ConversationMessage { Role = role, Content = text, SpeakerName = role == "user" ? "Player" : "Alice", GameDayIndex = 17 };
    private static PromptCacheMessage P(string role, string text, int group = 0, string revision = "") => new PromptCacheMessage(role, text, group, revision);
    private static string All(IEnumerable<PromptCacheMessage> messages) => string.Join("\n", messages.Select(x => x.Content));
    private static void Main()
    {
        var options = new ConversationContextOptions();
        var memory = new IntradayConversationMemory();
        var cache = new ConversationPromptCacheStore();
        var text = new PromptCacheText { Policy = "policy", Snapshot = "active:{SECTIONS}", Section = "section:{SECTIONS}", Input = "input" };
        var history = new List<ConversationMessage>();
        int calls = 0, peakMessages = 0, peakChars = 0;
        IntradayConversationMemory.Result last = null;
        for (int turn = 1; turn <= 300; turn++)
        {
            history.Add(M("user", turn == 3 ? "我们的琥珀口令是北门第三盏灯，不是南门。" : "Question " + turn));
            history.Add(M("assistant", turn == 3 ? "我记得琥珀口令，北门第三盏灯；见面前不要付款。" : "Answer " + turn + " " + new string('文', 700)));
            int before = calls;
            last = memory.Build("generation:1|Alice|day17|provider1|CN", history, "普通闲聊", options,
                source => { calls++; return "Summary of earlier speech; claims are not executed actions."; }, () => true, utcNow: Now.AddSeconds(turn));
            if (calls - before > 1) throw new Exception("more than one summary per request");
            var window = ConversationContextWindow.Select(history, "普通闲聊", options);
            if (window.Count > 24) throw new Exception("window grew past 12 ordinary turns");
            int group = 0;
            var request = new List<PromptCacheMessage> { P("system", new string('固', 30000), revision: last.Revision), P("user", last.Context) };
            foreach (var speech in window)
            {
                if (speech.Role == "user") group++;
                request.Add(P(speech.Role, speech.Content, group < 12 ? group : 0));
            }
            request.Add(P("user", "Current question " + turn));
            using var lease = cache.Begin(1, "scene:Alice", "Alice", request, "", text, PromptCacheSettings.MaxTranscriptCharacters, 10, Now.AddSeconds(turn));
            if (!ConversationRequestBudget.Fits(lease.Messages, options.InputChars, options.ContextTokens, 12000)) throw new Exception("final budget failed");
            peakMessages = Math.Max(peakMessages, lease.Messages.Count); peakChars = Math.Max(peakChars, lease.Chars);
            lease.Commit("Accepted reply " + turn, true, Now.AddSeconds(turn));
        }
        Check(history.Count == 600 && history[4].Content.Contains("北门第三盏灯"), "300 same-day turns retain all original source");
        Check(calls > 0 && calls < 60, "summaries run only for closed segments, at most one per request");
        Check(last.Context.Length > 0 && last.Context.Length <= 4800 + 100, "intraday summaries bounded without waiting for another day");
        Check(peakMessages < 100 && peakChars < 100000, "300-turn final cache requests stay bounded");
        var recall = memory.Build("generation:1|Alice|day17|provider1|CN", history, "琥珀口令是什么？", options, _ => "summary", () => true, utcNow: Now.AddHours(1));
        Check(recall.Recall.Any(x => x.Content.Contains("北门第三盏灯")) && recall.Recall.Any(x => x.Role == "assistant"), "early precise detail recalled with original user/assistant roles");
        Check(recall.Recall.Sum(x => x.Content.Length) <= 6000, "original quote recall budget");
        var other = memory.Build("generation:1|Bob|day17|provider1|CN", new List<ConversationMessage>(), "琥珀口令", options, _ => throw new Exception(), () => true);
        Check(other.Context.Length == 0 && other.Recall.Count == 0, "different NPC cannot receive another owner's memory");
        var shortWindow = ConversationContextWindow.Select(history, "琥珀口令", options);
        Check(shortWindow.Any(x => x.Content.Contains("北门第三盏灯")) && shortWindow.Count <= 30, "recent window plus bounded relevant earlier turns");
        var facts = new[] { M("system", "EXECUTED_FACT_ONLY") }.Concat(history).ToList();
        Check(ConversationContextWindow.Select(facts, "", options).Any(x => x.Role == "system" && x.Content == "EXECUTED_FACT_ONLY"),
            "confirmed facts stay separate from the ordinary speech window");
        bool factLeaked = false;
        new IntradayConversationMemory().Build("fact-check", facts, "", options, source => {
            factLeaked = source.Contains("EXECUTED_FACT_ONLY"); return "only speech summary";
        }, () => true);
        Check(!factLeaked, "authoritative facts are not passed to the dialogue summarizer");
        var ordered = new List<ConversationMessage>(history);
        ordered[10] = M("user", "EVENT_EARLIER amberpass"); ordered[11] = M("assistant", "amberpass received");
        ordered[100] = M("user", "EVENT_LATER cobaltpass"); ordered[101] = M("assistant", "cobaltpass received");
        var chronological = new IntradayConversationMemory().Build("chronology", ordered, "amberpass cobaltpass", options, null, () => true);
        int earlierIndex = chronological.Recall.FindIndex(x => x.Content.Contains("EVENT_EARLIER"));
        Check(earlierIndex >= 0 && earlierIndex < chronological.Recall.FindIndex(x => x.Content.Contains("EVENT_LATER")),
            "ranked original quotes return in source chronology");
        var failed = new IntradayConversationMemory(); int failures = 0;
        var failure = failed.Build("failure", history, "琥珀口令", options, _ => { failures++; throw new Exception("provider unavailable"); }, () => true, utcNow: Now);
        failed.Build("failure", history, "琥珀口令", options, _ => { failures++; return null; }, () => true, utcNow: Now.AddSeconds(1));
        Check(failure.Context.Length == 0 && failure.Recall.Count > 0 && failures == 1, "failed summary retains raw recall and cools down failed segment");
        bool current = true;
        var stale = new IntradayConversationMemory();
        var staleResult = stale.Build("stale", history, "琥珀口令", options, _ => { current = false; return "LATE_RESULT"; }, () => current);
        Check(staleResult.Context.Length == 0, "late cancelled summary never published");
        var oversized = new List<PromptCacheMessage> { P("system", new string('S', 30000)), P("user", "OLD_USER", 1), P("assistant", new string('A', 30000), 1), P("user", "CURRENT") };
        using var overTarget = cache.Begin(1, "over-target", "Alice", oversized, "", text, 35000, 10, Now);
        Check(overTarget.Messages.SequenceEqual(oversized) && oversized.Count == 4,
            "over-target cache fallback preserves selected user/assistant turns and current input");
        Check(!ConversationRequestBudget.Fits(oversized, 10000, 128000, 12000), "complete input can be identified as above management target");
        Check(!overTarget.Commit("NOT_RETAINED", true, Now), "oversized complete request does not grow retained cache");
        Check(!ConversationRequestBudget.Fits(oversized, 100000, 10000, 9000), "output allowance included in estimated context diagnostics");
        var resetting = new ConversationPromptCacheStore();
        var seed = new List<PromptCacheMessage> { P("system", "SYS", revision:"summary1"), P("user", "OLD_HISTORY", 1), P("user", "Q") };
        using (var first = resetting.Begin(1, "actor", "Alice", seed, "", text, 100000, 10, Now)) first.Commit("A", true, Now);
        using var changed = resetting.Begin(1, "actor", "Alice", new List<PromptCacheMessage> { P("system", "SYS", revision:"summary2"), P("user", "NEW_HISTORY", 1), P("user", "Q2") }, "", text, 100000, 10, Now.AddSeconds(1));
        Check(!changed.Warm && !All(changed.Messages).Contains("OLD_HISTORY"), "summary revision and outgoing window rebuild cache physically");
        var shrinking = new ConversationPromptCacheStore();
        using (var first = shrinking.Begin(1, "actor", "Alice", seed, "", text, 100000, 10, Now)) first.Commit("A", true, Now);
        using var shrunk = shrinking.Begin(1, "actor", "Alice", new List<PromptCacheMessage> { P("system", "SYS", revision:"summary1"), P("user", "NEW_HISTORY", 1), P("user", "Q2") }, "", text, 100000, 10, Now.AddSeconds(1));
        Check(!shrunk.Warm && shrunk.Reason == "history_rebuilt" && !All(shrunk.Messages).Contains("OLD_HISTORY"),
            "window eviction alone removes stale transcript even without a new summary");
        var big = M("user", new string('大', 25000));
        Check(ConversationContextWindow.Select(new[] { big }, "", options).Single().Content.Length == 25000, "oversized current input is preserved for explicit budget handling");
        Console.WriteLine($"Checks={checks}; summaryCalls={calls}; peakMessages={peakMessages}; peakChars={peakChars}; no HTTP/game/save writes");
    }
}
