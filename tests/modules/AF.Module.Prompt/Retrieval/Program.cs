using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        var store = new RevisionedPromptConfigurationStore<string>("initial");
        var first = store.Capture();
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            var loading = Task.Run(() => store.Reload(() => { entered.Set(); release.Wait(); return "new"; }, _ => "default"));
            Check(entered.Wait(TimeSpan.FromSeconds(5)), "reload must begin");
            Check(ReferenceEquals(first, store.Capture()), "in-flight reload retains complete previous snapshot");
            release.Set();
            Check(loading.GetAwaiter().GetResult().Revision == first.Revision + 1, "successful reload advances revision");
        }
        Check(first.Value == "initial" && store.Capture().Value == "new", "captured old value remains stable");
        var failed = store.Reload(() => throw new InvalidOperationException("bad config"), _ => "default");
        Check(failed.Value == "default" && failed.Revision == first.Revision + 2, "fallback replacement also advances revision");
        using (store.BeginCapture())
        {
            var pinned = store.Read();
            store.Reload(() => "later", _ => "default");
            Check(ReferenceEquals(pinned, store.Read()), "one operation retains captured revision");
            using (store.BeginCapture())
                Check(ReferenceEquals(pinned, store.Read()), "nested scope inherits parent revision");
            Check(ReferenceEquals(pinned, store.Read()), "nested scope restores parent revision");
            Task.Run(async () => { await Task.Yield(); Check(ReferenceEquals(pinned, store.Read()), "capture crosses a real async yield"); }).GetAwaiter().GetResult();
        }
        Check(store.Read().Value == "later", "scope restores live configuration");
        PromptRetrievalContextOwner.Hero.Value = "parent";
        PromptRetrievalContextOwner.Semantic.Value = "parent context";
        using (PromptRetrievalContextOwner.BeginScope())
        {
            PromptRetrievalContextOwner.Hero.Value = "child";
            PromptRetrievalContextOwner.Semantic.Value = "child context";
            Check(PromptRetrievalContextOwner.Hero.Value == "child", "child target set");
            using (PromptRetrievalContextOwner.BeginScope())
            {
                PromptRetrievalContextOwner.Hero.Value = "grandchild";
                Check(PromptRetrievalContextOwner.Hero.Value == "grandchild", "nested target set");
            }
            Check(PromptRetrievalContextOwner.Hero.Value == "child", "nested target restored");
            Task.Run(async () => { await Task.Yield(); Check(PromptRetrievalContextOwner.Semantic.Value == "child context", "context crosses async yield"); }).GetAwaiter().GetResult();
        }
        Check(PromptRetrievalContextOwner.Hero.Value == "parent" && PromptRetrievalContextOwner.Semantic.Value == "parent context", "outer scope restored");
        try
        {
            using (PromptRetrievalContextOwner.BeginScope())
            {
                PromptRetrievalContextOwner.Hero.Value = "exception";
                throw new InvalidOperationException();
            }
        }
        catch (InvalidOperationException) { }
        Check(PromptRetrievalContextOwner.Hero.Value == "parent", "exception restores parent");
        var latest = PromptRetrievalContextOwner.CreateSlot<object>(context => context.LatestEntities, (context, value) => context.LatestEntities = value);
        latest.Value = "parent mention";
        using (PromptRetrievalContextOwner.BeginScope((parent, child) => parent + "," + child))
            latest.Value = "child mention";
        Check((string)latest.Value == "parent mention,child mention", "mentions explicitly continue into parent");
        using (PromptRetrievalContextOwner.BeginScope((_, _) => throw new InvalidOperationException()))
            latest.Value = "failing continuation";
        Check((string)latest.Value == "parent mention,child mention", "merge failure still restores parent");
        var mutableSeeds = new[] { "duel", "reward" };
        var warmup = new PromptSemanticWarmupSeedBatch(5, mutableSeeds);
        mutableSeeds[0] = "changed";
        Check(warmup.Seeds[0] == "duel", "warmup captures detached seed values");
        long currentWarmupRevision = 5;
        var warmedSeeds = new List<string>();
        var warmupResult = PromptSemanticWarmupExecutor.Run(warmup, () => currentWarmupRevision, (revision, seed) =>
        {
            warmedSeeds.Add(seed);
            currentWarmupRevision = 6;
            return revision == 5;
        });
        Check(warmupResult.Stale && warmedSeeds.SequenceEqual(new[] { "duel" }), "stale warmup stops before second embedding");
        Check(warmupResult.SeedCount == 2 && warmupResult.Warmed == 1, "warmup reports captured count and completed work");
        Check(PromptSemanticWarmupExecutor.Run(warmup, () => 6, (_, _) => throw new Exception("must not embed")).Stale,
            "old revision is rejected before embedding");
        var mentionsStore = new PromptAuxiliaryMentionStore(64);
        for (int i = 0; i < 64; i++) mentionsStore.Publish("key" + i, new MentionedWorldEntities("name" + i));
        Check(mentionsStore.Get("key0")?.Entities.Single() == "name0", "64 auxiliary keys remain available");
        mentionsStore.Publish("key0", new MentionedWorldEntities("second"));
        Check(mentionsStore.Get("key0")?.Entities.Count == 2, "same-key auxiliary mentions merge");
        var detached = mentionsStore.Get("key0");
        detached.Entities.Clear();
        Check(mentionsStore.Get("key0")?.Entities.Count == 2, "auxiliary get does not expose stored mutable value");
        mentionsStore.Publish("key64", new MentionedWorldEntities("latest"));
        Check(mentionsStore.Get("key0") == null && mentionsStore.Get("key1") != null, "65th auxiliary key evicts FIFO oldest");
        var vectors = new PromptSemanticVectorCache(2, 1);
        vectors.PublishPhrase("v1|a", new[] { 1f }, 1, 1);
        vectors.PublishPhrase("v1|b", new[] { 2f }, 1, 1);
        vectors.PublishPhrase("v1|stale", new[] { 9f }, 1, 2);
        Check(vectors.TryGetPhrase("v1|a", out _) && !vectors.TryGetPhrase("v1|stale", out _), "stale phrase result does not evict live cache");
        vectors.PublishPhrase("v2|c", new[] { 3f }, 2, 2);
        Check(!vectors.TryGetPhrase("v1|a", out _) && vectors.TryGetPhrase("v2|c", out _), "phrase cache clears at capacity");
        vectors.PublishInput("v2|input", new[] { 4f }, 2, 2);
        vectors.PublishInput("v1|late", new[] { 8f }, 1, 2);
        Check(vectors.TryGetInput("v2|input", out _) && !vectors.TryGetInput("v1|late", out _), "stale input result does not evict live cache");
        vectors.Clear();
        Check(!vectors.TryGetPhrase("v2|c", out _) && !vectors.TryGetInput("v2|input", out _), "reload clears both vector caches");
        PromptRetrievalContextOwner.Hero.Value = "operation-parent";
        using (var operation = new PromptRetrievalOperationScope(PromptRetrievalContextOwner.BeginScope(), store.BeginCapture()))
        {
            PromptRetrievalContextOwner.Hero.Value = "operation-child";
            store.Reload(() => "new-live", _ => "default");
            Check(store.Read().Value == "later" && PromptRetrievalContextOwner.Hero.Value == "operation-child", "operation pins configuration and target together");
        }
        Check(store.Read().Value == "new-live" && PromptRetrievalContextOwner.Hero.Value == "operation-parent", "operation restores both ambient owners");
        Check(PromptRuleRanking.RerankBudget(1) == 8 && PromptRuleRanking.RerankBudget(20) == 36, "rerank total budget clamps to 8..36");
        Check(PromptRuleRanking.PerIntentRerank(36, 4) == 9 && PromptRuleRanking.PerIntentRerank(8, 4) == 4, "per-intent rerank clamps to 4..12");
        Check(PromptRuleRanking.PerIntentRecall(4) == 10 && PromptRuleRanking.PerIntentRecall(12) == 30, "per-intent recall clamps to 10..30");
        var scores = new[]
        {
            new PromptRuleCandidate(0, "low", 0.10f, 0.10f),
            new PromptRuleCandidate(1, "duel", 0.70f, 0.80f),
            new PromptRuleCandidate(2, "DUEL", 0.60f, 0.75f),
            new PromptRuleCandidate(3, "reward", 0.60f, 0.60f)
        };
        var ranked = PromptRuleRanking.Select(scores, 3);
        Check(ranked.Indices.SequenceEqual(new[] { 1, 3, 0 }) && ranked.StrictCount == 2, "rule ranking dedups ID then fills below threshold");
        Check(ranked.BestFinal == 0.80f && ranked.SecondRaw == 0.60f, "rule ranking reports actual top evidence");
        Check(PromptRuleRanking.Select(new[] { new PromptRuleCandidate(0, "bad", 1f, float.NaN), scores[3] }, 1).Indices.Single() == 3,
            "rule ranking excludes NaN and keeps source index");
        Check(PromptRuleRanking.TryLexicalHit("", "请谈长剑", new[] { "短剑", " 长剑 " }, out var keyword) && keyword == "长剑",
            "lexical rule matcher checks secondary input and preserves keyword order");
        Console.WriteLine("PromptJ03 focused checks=" + _checks);
    }
}

namespace AnimusForge
{
    public sealed class MentionedWorldEntities
    {
        public List<string> Entities = new List<string>();
        public bool IsEmpty => Entities.Count == 0;
        public MentionedWorldEntities() { }
        public MentionedWorldEntities(string name) { Entities.Add(name); }
        public MentionedWorldEntities Clone() => new MentionedWorldEntities { Entities = new List<string>(Entities) };
        public void Merge(MentionedWorldEntities other)
        {
            foreach (string name in other.Entities)
                if (!Entities.Contains(name, StringComparer.OrdinalIgnoreCase)) Entities.Add(name);
        }
    }
}
