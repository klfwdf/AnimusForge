using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

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
        var options = new[]
        {
            new PromptCandidateDescriptor(0, new[] { "apple" }),
            new PromptCandidateDescriptor(1, new[] { "sword" }),
            new PromptCandidateDescriptor(2, new[] { "shield" })
        };
        var sword = PromptCandidateSelection.SelectIndices(options, new[] { "sword" }, 2, true);
        Check(sword.SequenceEqual(new[] { 1, 0 }), "match must precede original-order fallback");
        Check(PromptCandidateSelection.SelectIndices(options, Array.Empty<string>(), 2, true).SequenceEqual(new[] { 0, 1 }), "empty mention fallback");
        Check(PromptCandidateSelection.SelectIndices(options, new[] { "unknown" }, 2, false).Count == 0, "no-match without fallback");
        Check(PromptCandidateSelection.SelectIndices(options, new[] { "unknown" }, 2, true).SequenceEqual(new[] { 0, 1 }), "no-match original-order fallback");
        var tied = new[]
        {
            new PromptCandidateDescriptor(0, new[] { "gold" }),
            new PromptCandidateDescriptor(1, new[] { "silver" }),
            new PromptCandidateDescriptor(2, new[] { "gold" })
        };
        Check(PromptCandidateSelection.SelectIndices(tied, new[] { "silver", "gold" }, 3, false).SequenceEqual(new[] { 1, 0, 2 }), "mention priority then original index");
        Check(PromptCandidateSelection.SelectIndices(options, new[] { "sword" }, 1, true).SequenceEqual(new[] { 1 }), "cap must apply after ranking");
        var repeated = new[]
        {
            new PromptCandidateDescriptor(0, new[] { "apple" }, 0),
            new PromptCandidateDescriptor(1, new[] { "apple" }, 0),
            new PromptCandidateDescriptor(2, new[] { "sword" }, 2)
        };
        Check(PromptCandidateSelection.SelectIndices(repeated, new[] { "unknown" }, 2, true).SequenceEqual(new[] { 0, 1 }), "no-match fallback retains duplicate source entries");
        Check(PromptCandidateSelection.SelectIndices(repeated, new[] { "sword" }, 3, true).SequenceEqual(new[] { 2, 0 }), "matched fallback deduplicates equal candidates");
        var chinese = new[]
        {
            new PromptCandidateDescriptor(0, new[] { "长剑" }),
            new PromptCandidateDescriptor(1, new[] { "圆盾" })
        };
        Check(PromptCandidateSelection.SelectIndices(chinese, new[] { "圆盾" }, 1, false).SequenceEqual(new[] { 1 }), "CJK exact match");
        Check(IntentQueryOptimizer.OptimizeSplitIntents(new[] { "你好", "告诉我长剑的来历", "长剑的来历", "城堡在哪里" }, 2)
            .SequenceEqual(new[] { "告诉我长剑的来历", "城堡在哪里" }), "shared intent normalization and per-speaker cap");
        Check(IntentQueryOptimizer.MaxCombinedIntentCount == 4, "combined intent cap");
        var now = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        var index = new PromptCandidateSnapshotIndex(80, TimeSpan.FromMinutes(10));
        for (int i = 0; i < 80; i++) index.Publish("key" + i, now);
        Check(index.Count == 80, "80 keys allowed");
        var evicted = index.Publish("key80", now.AddMinutes(1));
        Check(evicted.SequenceEqual(new[] { "key0" }), "81st key evicts oldest key only");
        Check(index.IsFresh("key80", now.AddMinutes(10)), "exact TTL boundary remains fresh");
        Check(!index.IsFresh("key80", now.AddMinutes(11).AddTicks(1)), "expired key rejected");
        Check(!index.IsFresh("key0", now.AddMinutes(1)), "evicted key rejected");
        Console.WriteLine("PromptCandidateSelection checks=" + _checks);
    }
}
