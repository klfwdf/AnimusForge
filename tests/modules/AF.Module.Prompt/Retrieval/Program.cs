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
        Check(PromptRuleIntentSplitter.Split("先给我剑，然后谈婚事。再说城堡", 2).Count == 2,
            "production guardrail splitter respects per-speaker cap after conjunction splitting");
        int combinedSplits = PromptRuleIntentSplitter.Split("剑。马。城堡。村庄。商队", 4).Count;
        Check(combinedSplits > 0 && combinedSplits <= 4,
            "production guardrail splitter respects combined cap");
        int embeddedInputs = 0;
        var intentBatch = PromptRuleIntentInputBatch.Collect("先给我剑，然后谈婚事。再说城堡", "先给我马，然后谈封地。再说商队",
            _ => { embeddedInputs++; return new[] { 1f }; });
        Check(intentBatch.Intents.Count == 4 && intentBatch.Texts.Count == 4
            && intentBatch.Intents.All(intent => intent.Weight == 1f),
            "production input collector enforces 2+2 intent budget with detached vectors");
        Check(embeddedInputs >= 4 && PromptRuleIntentInputBatch.Collect("请求", "请求", _ => null).Intents.Count == 0,
            "missing embeddings preserve semantic fallback and duplicate secondary is skipped");
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
        var evalCache = new PromptSingleEvaluationCache<string>();
        evalCache.Publish("target-a", "old", 1, 1);
        Check(evalCache.TryGet("target-a", 1, out var cached) && cached == "old", "evaluation cache hit matches key and revision");
        Check(!evalCache.TryGet("target-b", 1, out _) && !evalCache.TryGet("target-a", 2, out _), "evaluation cache isolates targets and revisions");
        evalCache.Publish("target-a", "late", 1, 2);
        Check(evalCache.TryGet("target-a", 1, out cached) && cached == "old", "late evaluation cannot replace current entry");
        evalCache.Clear();
        Check(!evalCache.TryGet("target-a", 1, out _), "reload clears derived evaluation");
        var derived = new PromptRevisionedDerivedCache<string>();
        long liveRevision = 1;
        Check(derived.GetOrBuild(1, () => liveRevision, () => "first") == "first", "derived value built for current revision");
        Check(derived.GetOrBuild(1, () => liveRevision, () => throw new Exception("unnecessary rebuild")) == "first", "same revision reuses derived value");
        liveRevision = 2;
        Check(derived.GetOrBuild(2, () => liveRevision, () => "second") == "second", "new revision rebuilds derived value");
        Check(derived.GetOrBuild(1, () => liveRevision, () => "late-old") == "late-old", "old caller retains its local result");
        Check(derived.GetOrBuild(2, () => liveRevision, () => throw new Exception("stale result overwrote live cache")) == "second", "late old result cannot replace current cache");
        var sticky = new PromptStickyRuleStore();
        var stickyRules = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["kingdom_service"] = new GuardrailRulePromptConfig { IsEnabled = true, Instruction = "kingdom", TriggerKeywords = new List<string> { "效忠" } }
        };
        var noExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long stickyRevision = 1;
        List<GuardrailRuleHit> StickyMerge(string target, string input, List<GuardrailRuleHit> live, bool eligible, bool completed, long revision = 1)
            => sticky.Merge(revision, () => stickyRevision, target, input, live, 3, noExclusions, stickyRules,
                _ => completed, _ => new PromptStickyEvidence(true, true, false, false, 1, 0.7f), _ => eligible, out _);
        var liveSticky = new List<GuardrailRuleHit> { new GuardrailRuleHit { RuleId = "kingdom_service", Score = 0.7f, Priority = 5, Instruction = "kingdom" } };
        Check(StickyMerge("hero:a", "效忠", liveSticky, true, false).Count == 1, "sticky begins from live hit");
        Check(StickyMerge("hero:b", "继续", null, true, false).Count == 0, "sticky target isolation");
        Check(Math.Abs(StickyMerge("hero:a", "继续", null, true, false).Single().Score - 0.546f) < 0.0001f, "first carry decays to 78 percent");
        Check(StickyMerge("hero:a", "继续", null, true, false).Count == 1, "second follow-up carries");
        Check(StickyMerge("hero:a", "继续", null, true, false).Count == 1, "third follow-up carries");
        Check(StickyMerge("hero:a", "继续", null, true, false).Count == 0, "carry expires after three turns");
        StickyMerge("hero:a", "效忠", liveSticky, true, false);
        Check(StickyMerge("hero:a", "继续", null, false, false).Count == 0, "target ineligibility blocks carried output");
        stickyRevision = 2;
        Check(StickyMerge("hero:a", "继续", null, true, false, 2).Count == 1, "reload preserves cross-call sticky carry");
        Check(StickyMerge("hero:a", "效忠", liveSticky, true, false, 1).Count == 1, "old caller retains live hit only");
        Check(StickyMerge("hero:a", "继续", null, true, false, 2).Count == 1, "old caller cannot replace new revision's sticky state");
        Check(StickyMerge("hero:a", "效忠", liveSticky, true, true, 2).Count == 1 && StickyMerge("hero:a", "继续", null, true, false, 2).Count == 0,
            "completed action cannot initiate sticky rule");
        var racingSticky = new PromptStickyRuleStore();
        long racingRevision = 1;
        racingSticky.Merge(1, () => racingRevision, "hero:a", "效忠", liveSticky, 3, noExclusions, stickyRules,
            _ => false, _ => { racingRevision = 2; return new PromptStickyEvidence(true, true, false, false, 1, 0.7f); }, _ => true, out _);
        Check(racingSticky.Merge(2, () => racingRevision, "hero:a", "继续", null, 3, noExclusions, stickyRules,
            _ => false, _ => default, _ => true, out _).Count == 0, "reload during evidence prevents stale sticky publication");
        int embeddedSeeds = 0;
        var semanticRecall = PromptRuleSemanticRecall.Compute(
            new[] { new PromptRuleRecallIntent("first", new[] { 1f, 0f }, 0.5f), new PromptRuleRecallIntent("second", new[] { 0f, 1f }, 1f) },
            new[] { new PromptRuleRecallRule(new[] { "a", "b" }), new PromptRuleRecallRule(new[] { "missing" }) },
            new[] { 1f, 0f },
            seed => { embeddedSeeds++; return seed == "a" ? new[] { 1f, 0f } : seed == "b" ? new[] { 0f, 1f } : null; },
            (a, b) => a[0] * b[0] + a[1] * b[1]);
        Check(embeddedSeeds == 3, "semantic recall embeds each seed once across intents");
        Check(semanticRecall.Score(0, 0) == 0.5f && semanticRecall.Seed(0, 0) == "a", "first weighted intent recalls first seed");
        Check(semanticRecall.Score(1, 0) == 1f && semanticRecall.Seed(1, 0) == "b", "second intent recalls second seed");
        Check(semanticRecall.BestInput[0] == 1f && semanticRecall.BestIntent[0] == "second" && semanticRecall.BestContext[0] == 1f,
            "aggregate and context use the same scoring evidence");
        Check(semanticRecall.BestInput[1] == 0f && semanticRecall.Seed(0, 1) == "", "unavailable embedding preserves zero-score fallback");
        var intentRules = new[] { new PromptRuleIntentDescriptor("reward"), new PromptRuleIntentDescriptor("marriage") };
        var intentRecall = PromptRuleSemanticRecall.Compute(
            new[] { new PromptRuleRecallIntent("request", new[] { 1f, 0f }, 1f) },
            new[] { new PromptRuleRecallRule(new[] { "reward" }), new PromptRuleRecallRule(new[] { "marriage" }) },
            null, seed => seed == "reward" ? new[] { 0.8f, 0f } : new[] { 0.6f, 0f },
            (a, b) => a[0] * b[0] + a[1] * b[1]);
        var rerankedIntent = PromptRuleIntentSelector.Select(intentRecall, 0, "request", 0.5f, intentRules, 2, 2,
            index => index == 0 ? "reward text" : "marriage text",
            (_, texts) => new[] { 0.2f, 0.9f });
        Check(rerankedIntent.Reranked && rerankedIntent.Scores.Select(score => score.RuleId).SequenceEqual(new[] { "marriage", "reward" }),
            "production intent selector reranks detached evidence with stable rule identity");
        Check(Math.Abs(rerankedIntent.Scores[0].FinalScore - 0.45f) < 0.0001f,
            "rerank score applies captured intent weight");
        var fallbackIntent = PromptRuleIntentSelector.Select(intentRecall, 0, "request", 1f, intentRules, 2, 2,
            _ => "text", (_, _) => new[] { 0.9f });
        Check(!fallbackIntent.Reranked && fallbackIntent.Scores[0].RuleId == "reward" && fallbackIntent.Scores[0].FinalScore == 0.8f,
            "failed ONNX batch retains semantic ordering and raw scores");
        var unavailableIntent = PromptRuleIntentSelector.Select(intentRecall, 0, "request", 1f, intentRules, 2, 1,
            _ => throw new Exception("unavailable reranker should not build text"), null);
        Check(!unavailableIntent.Reranked && unavailableIntent.Scores.Single().RuleId == "reward",
            "unavailable reranker avoids text construction and respects cap");
        Check(PromptRuleTextEvidence.SemanticSeeds("reward", "奖励。其余说明", new List<string> { " 赏赐 ", "赏赐" })
            .SequenceEqual(new[] { "赏赐", "reward 奖励" }), "reward retains keyword and instruction seeds without duplicate");
        Check(PromptRuleTextEvidence.SemanticSeeds("ordinary", "ignored", new List<string>()).Single() == "ordinary",
            "rule ID remains fallback seed when no keyword exists");
        Check(PromptRuleTextEvidence.RerankText("reward", "group", "奖励。其余说明", new List<string> { "礼物", "礼物" })
            .Contains("用途: reward 奖励") && PromptRuleTextEvidence.RerankText("reward", "group", "", null).Contains("规则组: group"),
            "rerank document retains group, ID, instruction and deduplicated keywords");
        var evalSnapshot = PromptRuleEvaluationAssembler.Create("key", intentRules, intentRecall);
        evalSnapshot.IntentCount = 1; evalSnapshot.ReturnCap = 1;
        evalSnapshot.RerankPerIntent = 2; evalSnapshot.RecallPerIntent = 2;
        Check(evalSnapshot.OrderedRules.Count == 2 && evalSnapshot.Rules["reward"].RawInput == 0.8f,
            "production evaluator initializes ordered rules from shared recall evidence");
        var evalAggregate = new PromptRuleAggregation();
        evalAggregate.Add("marriage", 0.45f, 1, "marriage", "request");
        var evaluated = PromptRuleEvaluationAssembler.Finish(evalSnapshot, evalAggregate, 1, 2, 1, "rerank");
        Check(evaluated.CandidatePoolCount == 1 && evaluated.Ranked[0].RuleTag == "marriage" && evaluated.Ranked[0].Hit,
            "production evaluator gives selected rule priority over higher raw-only evidence");
        Check(!evalSnapshot.Rules["reward"].Hit && evalSnapshot.Rules["reward"].RejectReason == "rerank_recall_miss"
            && evalSnapshot.Rules["marriage"].MatchedIntent == "request",
            "production evaluator preserves miss reason and matched intent in shared snapshot");
        var auxSnapshot = PromptAuxiliaryRuleEvaluation.Create("aux", "  hello\nworld  ", 2,
            new[] { "reward", "marriage", "unselected" });
        var auxTopics = new[]
        {
            new GuardrailAuxiliaryTopic { Number = 1, Code = "REWARD", RuleId = "reward", Label = "Rewards" },
            new GuardrailAuxiliaryTopic { Number = 2, Code = "MARRIAGE", RuleId = "marriage", Label = "Marriage" }
        };
        var auxSelected = PromptAuxiliaryRuleEvaluation.Apply(auxSnapshot, auxTopics,
            new[] { "REWARD", "REWARD", "MARRIAGE", "UNKNOWN" });
        Check(auxSelected.SequenceEqual(new[] { "reward", "marriage" }) && auxSnapshot.Rules["reward"].Hit
            && auxSnapshot.Rules["marriage"].Rank == 2, "auxiliary path deduplicates codes and respects return cap");
        Check(Math.Abs(auxSnapshot.Rules["reward"].TopGap - 0.08f) < 0.0001f
            && auxSnapshot.Rules["marriage"].MaxOtherTag == "reward"
            && auxSnapshot.Rules["unselected"].RejectReason == "auxiliary_api_miss",
            "auxiliary path preserves score diagnostics and miss fallback");
        Check(auxSnapshot.Rules["reward"].MatchedIntent == "hello world"
            && auxSnapshot.Rules["reward"].MatchedSeed == "Rewards", "auxiliary evaluation retains detached evidence");
        var configuredTopics = new[]
        {
            new GuardrailRulePromptConfig { Id = "marriage", TopicNumber = 3, TopicLabel = " Marriage ", Code = "MARRIAGE" },
            new GuardrailRulePromptConfig { Id = "reward", TopicNumber = 1, TopicLabel = "Rewards", Code = "REWARD" },
            new GuardrailRulePromptConfig { Id = "hidden", TopicNumber = 2, TopicLabel = "Hidden", Code = "HIDDEN" }
        };
        var eligibleTopics = PromptAuxiliaryRuleEvaluation.EligibleTopics(configuredTopics,
            new[] { "reward", "marriage", "hidden" }, true, (code, _, _) => code, id => id != "hidden");
        Check(eligibleTopics.Select(topic => topic.RuleId).SequenceEqual(new[] { "reward", "marriage" })
            && eligibleTopics[1].Label == "Marriage", "auxiliary topics respect captured eligibility and topic order");
        Check(PromptAuxiliaryRuleEvaluation.EligibleTopics(configuredTopics, new[] { "hidden" }, false,
            (code, _, _) => code, _ => throw new Exception("eligibility bypass must not inspect game state")).Single().RuleId == "hidden",
            "auxiliary topics bypass runtime eligibility for explicit full scope");
        string RuleKey(long revision = 1, bool enabled = true, string eligible = "reward", string target = "hero:a")
            => PromptRuleEvaluationCacheKey.Build("input", false, "", revision, true, enabled, true, 4, 3, eligible, target);
        Check(RuleKey() == "input|rag|revision=1|autoExclude=True|options=True:True:4:3|eligible=reward|target=hero:a",
            "production evaluation key preserves existing wire layout");
        Check(RuleKey() != RuleKey(revision: 2) && RuleKey() != RuleKey(enabled: false)
            && RuleKey() != RuleKey(eligible: "marriage") && RuleKey() != RuleKey(target: "hero:b"),
            "evaluation cache isolates reload, captured MCM, eligibility and target changes");
        Check(PromptRuleEvaluationCacheKey.Build("input", true, "|exclude:duel", 1, false, true, false, 2, 4, "reward", "hero:a")
            != RuleKey(), "evaluation cache isolates auxiliary route and exclusions");
        var pipelineRules = new[]
        {
            new PromptRuleRetrievalRule("reward", "trade", "", new[] { "gift" }),
            new PromptRuleRetrievalRule("marriage", "social", "", new[] { "marriage" })
        };
        var pipelineIntents = new[] { new PromptRuleRecallIntent("request", new[] { 1f }, 1f) };
        float[] EmbedRule(string seed) => seed == "gift" ? new[] { 0.8f } : seed == "marriage" ? new[] { 0.6f } : null;
        var semanticPipeline = PromptRuleRetrievalPipeline.Run("semantic-key", pipelineIntents, pipelineRules,
            null, 1, false, EmbedRule, (a, b) => a[0] * b[0], null);
        Check(semanticPipeline.Snapshot.Rules["reward"].Hit && !semanticPipeline.Snapshot.Rules["marriage"].Hit
            && semanticPipeline.Snapshot.MatchMode == "semantic", "production pipeline selects semantic recall without ONNX");
        var rerankPipeline = PromptRuleRetrievalPipeline.Run("rerank-key", pipelineIntents, pipelineRules,
            null, 1, true, EmbedRule, (a, b) => a[0] * b[0], (_, _) => new[] { 0.2f, 0.9f });
        Check(rerankPipeline.Snapshot.Rules["marriage"].Hit && !rerankPipeline.Snapshot.Rules["reward"].Hit
            && rerankPipeline.IntentSelections.Single().Reranked, "production pipeline honors deterministic reranker evidence");
        var failedPipeline = PromptRuleRetrievalPipeline.Run("failure-key", pipelineIntents, pipelineRules,
            null, 1, true, EmbedRule, (a, b) => a[0] * b[0], (_, _) => null);
        Check(failedPipeline.Snapshot.Rules["reward"].Hit && failedPipeline.Snapshot.MatchMode == "rerank"
            && !failedPipeline.IntentSelections.Single().Reranked, "production pipeline keeps semantic failure fallback and mode");
        var mutableKeywords = new List<string> { "gift" };
        var detachedRule = new PromptRuleRetrievalRule("reward", "trade", "", mutableKeywords);
        mutableKeywords[0] = "changed";
        Check(detachedRule.TriggerKeywords.Single() == "gift", "pipeline detaches configuration keywords before provider callbacks");
        var multiPipeline = PromptRuleRetrievalPipeline.Run("multi-key", new[]
        {
            new PromptRuleRecallIntent("first", new[] { 1f, 0f }, 1f),
            new PromptRuleRecallIntent("second", new[] { 0f, 1f }, 1f)
        }, pipelineRules, null, 2, false,
            seed => seed == "gift" ? new[] { 1f, 0f } : seed == "marriage" ? new[] { 0f, 1f } : null,
            (a, b) => a[0] * b[0] + a[1] * b[1], null);
        Check(multiPipeline.Snapshot.MatchMode == "semantic_multi" && multiPipeline.Snapshot.IntentCount == 2
            && multiPipeline.Snapshot.Rules["reward"].Hit && multiPipeline.Snapshot.Rules["marriage"].Hit,
            "production pipeline aggregates two captured intents without cross-intent loss");
        var aggregate = new PromptRuleAggregation();
        aggregate.Add("marriage", 0.5f, 2, "first", "intent a");
        aggregate.Add("marriage", 0.6f, 1, "second", "intent b");
        aggregate.Add("kingdom_service", 0.58f, 1, "other", "intent b");
        var aggregates = aggregate.Select(2, 4, 2);
        Check(aggregates.Count == 2 && aggregates[0].RuleId == "marriage" && Math.Abs(aggregates[0].AmpScore - 0.63f) < 0.0001f,
            "cross-intent aggregation applies repeat bonus and stable ranking");
        Check(aggregates[0].BestScore == 0.6f && aggregates[0].BestRank == 1 && aggregates[0].MatchedSeed == "second",
            "aggregation preserves highest-scoring seed, intent and best rank");
        aggregate.Add("MARRIAGE", 0.8f, 1, "third", "intent c");
        Check(aggregate.Select(2, 4, 3)[0].HitCount == 3, "same rule ID aggregates case-insensitively");
        var finals = PromptRuleFinalRanking.Rank(new[]
        {
            new PromptRuleFinalCandidate(0, "noncandidate", false, 0f, 0.9f),
            new PromptRuleFinalCandidate(1, "marriage", true, 0.6f, 0.2f),
            new PromptRuleFinalCandidate(2, "kingdom_service", true, 0.5f, 0.1f)
        }, 1, "semantic");
        Check(finals.Select(x => x.SourceIndex).SequenceEqual(new[] { 1, 2, 0 }), "candidate-first final ranking is stable");
        Check(finals[0].Hit && finals[0].RejectReason == "semantic_return(1/1)" && !finals[1].Hit && finals[1].RejectReason == "semantic_return_overflow",
            "final hit and overflow preserve return cap");
        Check(finals[0].MaxOther == 0.9f && finals[0].MaxOtherTag == "noncandidate" && Math.Abs(finals[0].TopGap - 0.1f) < 0.0001f,
            "max other uses actual score even when noncandidate ranks last");
        Check(Math.Abs(finals[0].Mean - 2f / 3f) < 0.0001f && finals[2].RejectReason == "semantic_recall_miss",
            "final diagnostics retain mean and miss reason");
        DuelSettings.Current.PromptListCandidateMaxCount = 2;
        Check(PromptListRetrievalService.GetMaxCandidateCount() == 2, "production facade reads current MCM candidate cap");
        DuelSettings.Current.PromptListCandidateMaxCount = 1;
        Check(PromptListRetrievalService.GetMaxCandidateCount() == 1, "production facade sees MCM hot edit without reload");
        var npc = new TaleWorlds.CampaignSystem.Hero { StringId = "npc-j03" };
        var publicReward = new RewardSystemBehavior.RewardItemInfo { Name = "剑", StringId = "sword" };
        var otherReward = new RewardSystemBehavior.RewardItemInfo { Name = "马", StringId = "horse" };
        var privateA = new RewardSystemBehavior.RewardItemInfo { Name = "盔", IsPrivateEquipment = true };
        var privateB = new RewardSystemBehavior.RewardItemInfo { Name = "甲", IsPrivateEquipment = true };
        var allRewards = new[] { publicReward, otherReward, privateA, privateB };
        var selectedRewards = PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(allRewards,
            new MentionedWorldEntities("剑"), 1);
        Check(selectedRewards.Count == 3 && selectedRewards[0] == publicReward
            && selectedRewards.Contains(privateA) && selectedRewards.Contains(privateB),
            "production NPC reward facade keeps private equipment outside display cap");
        Check(PromptListRetrievalService.FilterRewardItems(allRewards, null, 1).Count == 1,
            "normal reward display still enforces cap");
        PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope,
            npc, null, -1, allRewards);
        PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
            npc, null, -1, new[] { publicReward });
        bool hasAuthorized = PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope,
            npc, null, -1, out var authorized);
        bool hasDisplayed = PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
            npc, null, -1, out var displayed);
        Check(hasAuthorized && authorized.Count == 4 && hasDisplayed && displayed.Count == 1,
            "production candidate store isolates full authorization and display scopes");
        displayed.Clear();
        Check(PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
            npc, null, -1, out var displayedAgain) && displayedAgain.Count == 1,
            "snapshot getter does not leak its mutable list container");
        for (int i = 0; i < 81; i++)
            PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
                npc, null, -1, new[] { publicReward }, "key-" + i);
        Check(!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
            npc, null, -1, out _, "key-0")
            && PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope,
                npc, null, -1, out _, "key-80"), "production candidate facade enforces global 80-key index");
        var troopA = new MyBehavior.PartyTransferPromptEntry { DisplayName = "步兵" };
        var troopB = new MyBehavior.PartyTransferPromptEntry { DisplayName = "弓手" };
        PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope,
            npc, null, 7, new[] { troopA, troopB });
        PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope,
            npc, null, 7, new[] { troopB });
        Check(PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope,
            npc, null, 7, out var allTroops) && allTroops.Count == 2
            && PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope,
                npc, null, 7, out var shownTroops) && shownTroops.Single() == troopB,
            "production party-transfer scopes keep full authorization separate from shown entries");
        Check(!PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope,
            npc, null, 8, out _), "production candidate key isolates agent index");
        var settlementA = new MyBehavior.SettlementTransferPromptEntry { DisplayName = "城镇", AssetId = "town-a" };
        var settlementB = new MyBehavior.SettlementTransferPromptEntry { DisplayName = "城堡", AssetId = "castle-b" };
        PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope,
            npc, null, -1, new[] { settlementA, settlementB });
        PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope,
            npc, null, -1, new[] { settlementB });
        Check(PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope,
            npc, null, -1, out var allSettlements) && allSettlements.Count == 2
            && PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope,
                npc, null, -1, out var shownSettlements) && shownSettlements.Single() == settlementB,
            "production settlement-transfer scopes keep full authorization separate from display");
        Check(PromptListRetrievalService.BuildMentionTerms(new MentionedWorldEntities { Entities = new List<string> { "政策", "政策", " 王国 " } })
            .SequenceEqual(new[] { "政策", "王国" }), "policy mention consumer shares production normalization");
        var missionSeed = new PromptSemanticWarmupSeedBatch(42, new[] { "seed-at-mission-start" });
        int missionThread = Environment.CurrentManagedThreadId;
        RagWarmupCoordinator.TryStartBackgroundWarmup("mission_start", missionSeed);
        Check(SpinWait.SpinUntil(() => Volatile.Read(ref AIConfigHandler.ReceivedWarmupSeeds) != null,
                TimeSpan.FromSeconds(5)), "production coordinator completes bounded background warmup");
        Check(ReferenceEquals(AIConfigHandler.ReceivedWarmupSeeds, missionSeed)
            && AIConfigHandler.ReceivedWarmupSource == "rag_warmup_complete"
            && AIConfigHandler.ReceivedWarmupThread != missionThread,
            "coordinator background callback passes caller-captured seed without recapturing game state");
        Console.WriteLine("PromptJ03 focused checks=" + _checks);
    }
}

namespace AnimusForge
{
    public sealed class GuardrailRulePromptConfig
    {
        public string Id = "";
        public string TopicLabel = "";
        public int TopicNumber;
        public string Code = "";
        public bool IsEnabled;
        public string Instruction = "";
        public List<string> TriggerKeywords = new List<string>();
    }

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
