using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;
using LoreRule = AnimusForge.KnowledgeLibraryBehavior.LoreRule;

// Contract for the J06a KnowledgeRuleIndex owner, compiled from the production file with a stub KnowledgeLibraryBehavior
// (only the LoreRule type) and deterministic fake embedding/reranker ports.
internal static class Program
{
    private static int _checks;
    private static void Check(bool c, string m) { _checks++; if (!c) throw new Exception("FAIL: " + m); }

    private sealed class FakePorts : IKnowledgeIndexPorts
    {
        public bool EmbeddingAvailable { get; set; } = true;
        public bool RerankerAvailable { get; set; }
        public int SemanticTopK { get; set; } = 4;
        public float SemanticMinScore { get; set; } = 0.21f;
        public Func<string, float[]> Embed = text => null;
        public Func<string, IReadOnlyList<string>, List<float>> Score = (q, docs) => null;
        public List<string> Logs = new List<string>();
        public int EmbedCalls;
        public bool TryGetEmbedding(string text, out float[] vector) { EmbedCalls++; vector = Embed(text); return vector != null; }
        public bool TryScoreBatch(string query, IReadOnlyList<string> documents, out List<float> scores) { scores = Score(query, documents); return scores != null; }
        public void Log(string channel, string message) => Logs.Add(channel + ":" + message);
    }

    private static LoreRule Rule(string id, params string[] keywords) => new LoreRule { Id = id, Keywords = keywords.ToList(), RagShortTexts = new List<string>() };

    private static void Main()
    {
        Tokens();
        Seeds();
        SparseIndex();
        OnnxIndexAndFallback();
        CollapseAndSelect();
        RerankAndCache();
        Budgets();
        Retriever();
        ImportSupport();
        Console.WriteLine("PASS knowledge-index+lore-retriever checks=" + _checks);
    }

    private static void ImportSupport()
    {
        var rule = Rule("one", "  long\n  sword ", "LONG SWORD", "shield");
        Check(KnowledgeImportSupport.GetKnowledgeKeywordsForCompare(rule).SequenceEqual(new[] { "long sword", "shield" }), "import keywords normalize and dedupe without mutating source");
        Check(rule.Keywords[0].Contains("\n"), "keyword source remains unchanged");
        Check(KnowledgeImportSupport.NormalizeWhenStringListForImport(new List<string> { " B ", "a", "b", " " }).SequenceEqual(new[] { "a", "B" }), "condition strings trim, case-dedupe and sort");
        Check(KnowledgeImportSupport.NormalizeWhenSkillMinForImport(new Dictionary<string, int> { ["Riding"] = 2, ["bad"] = -1, ["athletics"] = 3 }).Select(x => x.Key).SequenceEqual(new[] { "athletics", "Riding" }), "skill requirements ignore negatives and sort");
        var variants = Rule("variant");
        variants.Variants.Add(new KnowledgeLibraryBehavior.LoreVariant { When = null });
        variants.Variants.Add(new KnowledgeLibraryBehavior.LoreVariant { When = new KnowledgeLibraryBehavior.LoreWhen() });
        Check(KnowledgeImportSupport.TryFindDuplicateKnowledgeVariantCondition(variants, out int first, out int second) && first == 0 && second == 1, "generic condition duplicate retains original indices");
        variants.Variants[1].When.HeroIds = new List<string> { "hero_1" };
        Check(!KnowledgeImportSupport.TryFindDuplicateKnowledgeVariantCondition(variants, out _, out _), "different condition is not duplicate");

        string dir = Path.Combine(AppContext.BaseDirectory, "import-fixture-" + Guid.NewGuid().ToString("N"));
        string rules = Path.Combine(dir, "knowledge", "rules");
        Directory.CreateDirectory(rules);
        File.WriteAllText(Path.Combine(rules, "one.json"), "{\"Id\":\"one\",\"Keywords\":[\"first\"]}");
        File.WriteAllText(Path.Combine(rules, "bad.json"), "{bad json");
        File.WriteAllText(Path.Combine(dir, "knowledge", "KnowledgeRules.json"), "{\"Rules\":[{\"Id\":\"one\",\"Keywords\":[\"later\"]},{\"Id\":\"two\"}]}");
        Check(Newtonsoft.Json.JsonConvert.DeserializeObject<LoreRule>(File.ReadAllText(Path.Combine(rules, "one.json")))?.Id == "one", "fixture Newtonsoft deserialization available");
        var loaded = KnowledgeImportSupport.LoadKnowledgeRulesFromImportDir(dir);
        Check(loaded.Select(x => x.Id).SequenceEqual(new[] { "one", "two" }) && loaded[0].Keywords.Single() == "first", "single-rule files precede aggregate and duplicate ID keeps first: " + string.Join(",", loaded.Select(x => x.Id + "/" + string.Join("|", x.Keywords))));
        Check(KnowledgeImportSupport.FindKnowledgeRuleJsonById(rules, "one") == Path.Combine(rules, "one.json"), "direct rule filename resolution");
        Check(KnowledgeImportSupport.FindKnowledgeRuleJsonById(rules, "absent") == null, "missing rule lookup is null");
        Check(KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(Path.Combine(dir, "missing")) == null, "missing import folder fails closed");
    }

    private static void Tokens()
    {
        var t = KnowledgeRuleIndex.ExtractVectorTokens("Hello 世界 x!");
        Check(t.SequenceEqual(new[] { "w:hello", "c2:世界", "c1:世", "c1:界", "w1:x" }), "tokenizer words/cjk bigrams+unigrams/single: " + string.Join(",", t));
        Check(KnowledgeRuleIndex.ExtractVectorTokens("é1").SequenceEqual(new[] { "u:é", "w1:1" }) && KnowledgeRuleIndex.ExtractVectorTokens("  ").Count == 0, "non-ascii letters and blanks");
        var tf = KnowledgeRuleIndex.CountTokens(new List<string> { "a", "a", " ", "b" });
        Check(tf["a"] == 2 && tf["b"] == 1 && tf.Count == 2, "count tokens skips blanks");
        var w = KnowledgeRuleIndex.BuildVectorWeights(tf, new Dictionary<string, float> { ["a"] = 2f }, out float norm);
        float wa = (1f + (float)Math.Log(3.0)) * 2f, wb = 1f + (float)Math.Log(2.0);
        Check(Math.Abs(w["a"] - wa) < 1e-5 && Math.Abs(w["b"] - wb) < 1e-5 && Math.Abs(norm - (float)Math.Sqrt(wa * wa + wb * wb)) < 1e-4, "tf-idf weights + norm");
        Check(Math.Abs(KnowledgeRuleIndex.DotProduct(w, new Dictionary<string, float> { ["b"] = 2f }) - wb * 2f) < 1e-5 && KnowledgeRuleIndex.DotProduct(new[] { 1f, 2f, 3f }, new[] { 1f, 1f }) == 3f, "dot products (sparse + dense truncated)");
        Check(KnowledgeRuleIndex.Hash8("abc") == KnowledgeRuleIndex.Hash8("abc") && KnowledgeRuleIndex.Hash8("abc") != KnowledgeRuleIndex.Hash8("abd") && KnowledgeRuleIndex.Hash8(null).Length == 8, "fnv hash8");
    }

    private static void Seeds()
    {
        var r = new LoreRule { Id = "r", Keywords = new List<string> { " K1 ", "K1", "" }, RagShortTexts = new List<string> { "line\r\ntwo" } };
        Check(KnowledgeRuleIndex.GetRuleTopicSeeds(r).SequenceEqual(new[] { "K1", "line  two" }), "topic seeds dedupe + newline collapse");
        Check(KnowledgeRuleIndex.GetRuleEvidenceSeeds(r).SequenceEqual(new[] { "K1", "关于K1", "line  two" }), "evidence seeds add 关于 prefix");
        Check(KnowledgeRuleIndex.BuildRuleSearchText(r) == "K1 K1 line  two" && KnowledgeRuleIndex.BuildRuleRerankText(new LoreRule { Keywords = new List<string> { new string('x', 500) } }).Length == 480, "search text and rerank cap 480");
    }

    private static void SparseIndex()
    {
        var rules = new List<LoreRule> { Rule("apple", "apple pie"), Rule("berry", "berry tart"), null };
        var ports = new FakePorts { EmbeddingAvailable = false };
        var index = new KnowledgeRuleIndex(ports, () => rules);
        Check(!index.SparseReady && index.FindSparseCandidateScores("apple", 4).Count == 0, "unbuilt index returns nothing");
        index.EnsureVectorIndex();
        Check(index.SparseReady && index.SparseEntryCount == 6, "sparse entries = topic(kw) + evidence(kw, 关于kw) per rule: " + index.SparseEntryCount);
        var hits = index.FindSparseCandidateScores("apple", 4);
        Check(hits.Count == 1 && hits[0].Rule.Id == "apple" && hits[0].RawScore > 0f, "sparse recall matches only overlapping rule");
        var vec = index.FindVectorCandidateScores("berry", 4);
        Check(vec.Count == 1 && vec[0].Rule.Id == "berry" && ports.Logs.Any(l => l.Contains("semantic_source=sparse")), "onnx unavailable → sparse fallback logged");
        long v = index.Version;
        index.Touch();
        Check(index.Version == v + 1 && !index.SparseReady && index.FindSparseCandidateScores("apple", 4).Count == 0, "touch bumps version and drops index (legacy: no rebuild until asked)");
    }

    private static float[] Vec(string text)
    {
        // deterministic 3-d embedding: apple→x, berry→y, other→z
        string t = (text ?? "").ToLowerInvariant();
        return new[] { t.Contains("apple") ? 1f : 0f, t.Contains("berry") ? 1f : 0f, t.Contains("apple") || t.Contains("berry") ? 0f : 1f };
    }

    private static void OnnxIndexAndFallback()
    {
        var rules = new List<LoreRule> { Rule("apple", "apple"), Rule("berry", "berry") };
        var ports = new FakePorts { Embed = Vec };
        var index = new KnowledgeRuleIndex(ports, () => rules);
        index.EnsureOnnxIndex();
        Check(index.OnnxReady && index.OnnxEntryCount == 6 && ports.EmbedCalls == 6, "onnx index embeds every seed once (3 per rule)");
        int calls = ports.EmbedCalls;
        index.EnsureOnnxIndex();
        Check(ports.EmbedCalls == calls, "ensure is idempotent at same version");
        var hits = index.FindOnnxCandidateScores("I like apple", 4);
        Check(hits.Count >= 1 && hits[0].Rule.Id == "apple" && hits[0].RawScore == 1f, "onnx query ranks apple first: " + string.Join(",", hits.Select(h => h.Rule.Id + "=" + h.RawScore)));
        Check(index.FindVectorCandidateScores("apple", 4).Count > 0 && ports.Logs.Any(l => l.Contains("semantic_source=onnx")), "onnx source wins when it has results");

        var failing = new FakePorts { Embed = _ => null };
        var idx2 = new KnowledgeRuleIndex(failing, () => rules);
        idx2.EnsureOnnxIndex();
        Check(!idx2.OnnxReady, "all embeddings failing → index not committed");
        var idx3 = new KnowledgeRuleIndex(new FakePorts { Embed = _ => null }, () => new List<LoreRule>());
        idx3.EnsureOnnxIndex();
        Check(idx3.OnnxReady && idx3.OnnxEntryCount == 0, "no rules → empty index committed");
        var idx4 = new KnowledgeRuleIndex(new FakePorts { EmbeddingAvailable = false, Embed = Vec }, () => rules);
        idx4.EnsureOnnxIndex();
        Check(!idx4.OnnxReady, "engine unavailable → not committed");
    }

    private static void CollapseAndSelect()
    {
        var a = Rule("a"); var b = Rule("b");
        var scored = new List<KnowledgeRuleScore>
        {
            new KnowledgeRuleScore { Rule = a, RawScore = 0.3f, EvidenceScore = float.NaN },
            new KnowledgeRuleScore { Rule = a, RawScore = float.NaN, EvidenceScore = 0.9f },
            new KnowledgeRuleScore { Rule = b, RawScore = float.NaN, EvidenceScore = 0.5f },
            null,
        };
        var collapsed = KnowledgeRuleIndex.CollapseRuleScoresByMax(scored).OrderBy(x => x.Rule.Id).ToList();
        Check(collapsed.Count == 2 && collapsed[0].RawScore == 0.3f && collapsed[0].EvidenceScore == 0.9f && collapsed[1].RawScore == 0.5f && collapsed[1].EvidenceScore == 0.5f, "collapse keeps max per id; evidence-only raw falls back to evidence");
        var ports = new FakePorts { SemanticMinScore = 0.4f };
        var index = new KnowledgeRuleIndex(ports, () => null);
        var sel = index.SelectSemanticCandidateScores(new List<KnowledgeRuleScore>
        {
            new KnowledgeRuleScore { Rule = Rule("lo"), RawScore = 0.1f },
            new KnowledgeRuleScore { Rule = Rule("hi"), RawScore = 0.8f },
            new KnowledgeRuleScore { Rule = Rule("hi"), RawScore = 0.7f },
            new KnowledgeRuleScore { Rule = Rule("mid"), RawScore = 0.5f },
        }, "t", "q", 3);
        Check(sel.Select(x => x.Rule.Id).SequenceEqual(new[] { "hi", "mid", "lo" }), "strict pass (>=min) then fill below threshold, unique ids: " + string.Join(",", sel.Select(x => x.Rule.Id)));
        Check(ports.Logs.Last().Contains("strictSelected=2") && ports.Logs.Last().Contains("topN=3"), "accept log reports strict count");
        var many = Enumerable.Range(0, 30).Select(i => new KnowledgeRuleScore { Rule = Rule("r" + i), RawScore = 1f }).ToList();
        Check(index.SelectSemanticCandidateScores(many, "t", "q", 0).Count == 2 && index.SelectSemanticCandidateScores(many, "t", "q", 99).Count == KnowledgeRuleIndex.SemanticResultHardCapMax, "topK<=0 → 2; hard cap 20");
    }

    private static void RerankAndCache()
    {
        var rules = new List<LoreRule> { Rule("apple", "apple"), Rule("berry", "berry"), Rule("cherry", "cherry") };
        var ports = new FakePorts { Embed = Vec, RerankerAvailable = true, SemanticMinScore = 0f, Score = (q, docs) => docs.Select(d => d.Contains("berry") ? 0.9f : 0.1f).ToList() };
        var index = new KnowledgeRuleIndex(ports, () => rules);
        index.EnsureOnnxIndex();
        var recalled = index.FindVectorCandidateScores("apple berry", 4);
        Check(recalled.Count >= 2, "recall has both");
        var ranked = index.RerankCandidateScores("q", recalled, 2, 2f);
        Check(ranked[0].Rule.Id == "berry" && Math.Abs(ranked[0].RawScore - 1.8f) < 1e-5 && ranked[0].RerankScore == ranked[0].RawScore && ranked.Count == 2, "cross-encoder score × weight reorders; topN applied: " + string.Join(",", ranked.Select(r => r.Rule.Id + "=" + r.RawScore)));
        Check(ports.Logs.Any(l => l.Contains("source=cross_encoder")), "cross encoder source logged");
        ports.RerankerAvailable = false;
        var fallback = index.RerankCandidateScores("q", recalled, 4, 1f);
        Check(fallback[0].Rule.Id == "apple" || fallback[0].Rule.Id == "berry", "recall fallback keeps raw order");
        Check(ports.Logs.Any(l => l.Contains("source=recall_fallback")), "fallback source logged");

        ports.RerankerAvailable = true;
        int embedCalls = ports.EmbedCalls;
        var first = index.FindRankedVectorCandidateScores("apple berry", 4, 2, 1f);
        var second = index.FindRankedVectorCandidateScores(" apple berry ", 4, 2, 1f);
        Check(first.Count == 2 && second.Select(x => x.Rule.Id).SequenceEqual(first.Select(x => x.Rule.Id)) && ports.EmbedCalls == embedCalls + 1 && index.RankedCacheCount == 1, "ranked cache hit on trimmed input (one embedding call total)");
        first[0].RawScore = -99f;
        Check(index.FindRankedVectorCandidateScores("apple berry", 4, 2, 1f)[0].RawScore != -99f, "cache returns clones");
        Check(index.FindRankedVectorCandidateScores("apple berry", 4, 2, 2f).Count == 2 && index.RankedCacheCount == 2, "weight is part of the cache key");
        index.Touch();
        Check(index.RankedCacheCount == 0 && index.FindRankedVectorCandidateScores("apple berry", 4, 2, 1f).Count == 0, "touch clears cache and stale index yields nothing");
        Check(index.FindRankedVectorCandidateScores("  ", 4, 2, 1f).Count == 0, "blank input → empty");
    }

    private static void Budgets()
    {
        var index = new KnowledgeRuleIndex(new FakePorts { SemanticTopK = 99 }, () => null);
        Check(index.GetKnowledgeReturnCap() == 12 && new KnowledgeRuleIndex(new FakePorts { SemanticTopK = 0 }, () => null).GetKnowledgeReturnCap() == 1, "return cap clamps 1..12");
        Check(KnowledgeRuleIndex.GetKnowledgeRerankBudget(1) == 8 && KnowledgeRuleIndex.GetKnowledgeRerankBudget(4) == 12 && KnowledgeRuleIndex.GetKnowledgeRerankBudget(20) == 36, "rerank budget 3x clamped 8..36");
        Check(KnowledgeRuleIndex.GetKnowledgePerEntityRerank(12, 5) == 4 && KnowledgeRuleIndex.GetKnowledgePerEntityRerank(36, 1) == 12 && KnowledgeRuleIndex.GetKnowledgePerEntityRerank(10, 4) == 4, "per-entity rerank round-away clamped 4..12");
        Check(KnowledgeRuleIndex.GetKnowledgePerEntityRecall(4) == 10 && KnowledgeRuleIndex.GetKnowledgePerEntityRecall(12) == 30 && KnowledgeRuleIndex.GetKnowledgePerEntityRecall(6) == 15, "per-entity recall 2.5x clamped 10..30");
        Check(KnowledgeRuleIndex.GetLoreInjectLimit(0) == 1 && KnowledgeRuleIndex.GetLoreInjectLimit(50) == 12 && KnowledgeRuleIndex.GetSemanticResultHardCap(0) == 20 && KnowledgeRuleIndex.GetSemanticResultHardCap(5) == 5, "inject limit and hard cap");
    }

    private static void Retriever()
    {
        Check(LoreCandidateRetriever.NormalizeKeywordForCompare(" a \r\n  b\tc ") == "a b c" && LoreCandidateRetriever.NormalizeKeywordForCompare(null) == "", "keyword normalization collapses whitespace");
        var terms = LoreCandidateRetriever.BuildMentionTerms(new[] { " Vlandia ", "vlandia", "", null, "Battania", new string('z', 100) });
        Check(terms.SequenceEqual(new[] { "Vlandia", "Battania", new string('z', 80) }), "mention terms: dedupe ci, skip blank, cap 80: " + string.Join(",", terms));
        Check(LoreCandidateRetriever.BuildMentionTerms(Enumerable.Range(0, 50).Select(i => "e" + i)).Count == LoreCandidateRetriever.MentionTermHardCap, "hard cap 32 terms");
        Check(LoreCandidateRetriever.BuildMentionSignature(new string[0], 4) == "mentions=empty" && LoreCandidateRetriever.BuildMentionSignature(new[] { "A", "b" }, 4) == "mentions=" + KnowledgeRuleIndex.Hash8("a|b") + ":2:cap4", "mention signature");
        Check(LoreCandidateRetriever.FormatMentionCounts(null) == "entities=0" && LoreCandidateRetriever.FormatMentionCounts(new[] { "x", "" }) == "entities=2", "mention counts (raw list size)");
        var q = LoreCandidateRetriever.BuildQueryInputsFromMentions(Enumerable.Range(0, 20).Select(i => "n" + i), 5, out int termCount);
        Check(q.Count == 5 && termCount == 20 && q.All(x => x.Weight == 1f) && q[0].Text == "n0", "query inputs limited by inject limit, term count reports all");
        Check(LoreCandidateRetriever.BuildQueryInputsFromMentions(new[] { "a" }, 99, out _).Count == 1 && LoreCandidateRetriever.BuildQueryInputsFromMentions(new[] { "a", "b" }, 0, out _).Count == 1, "query limit clamps 1..12");
        Check(LoreCandidateRetriever.QuoteJson("a\"b\\c\n") == "\"a\\\"b\\\\c\\n\"" && LoreCandidateRetriever.QuoteJson("汉") == "\"汉\"", "json quoting for logs");

        // per-entity allocation over a fake index: apple/berry/cherry rules; two entities mention apple and berry.
        var rules = new List<LoreRule> { Rule("apple", "apple"), Rule("berry", "berry"), Rule("cherry", "cherry"), Rule("apple2", "apple") };
        var ports = new FakePorts { Embed = Vec, SemanticMinScore = 0f, SemanticTopK = 4 };
        var index = new KnowledgeRuleIndex(ports, () => rules);
        index.EnsureOnnxIndex();
        var logs = new List<string>();
        var retriever = new LoreCandidateRetriever(index, (c, m) => logs.Add(c + ":" + m));
        var pool = retriever.CollectCandidateRules(new[] { "apple", "berry" }, retrievalEnabled: true);
        Check(pool.MatchMode == "mentions_semantic_per_entity" && pool.InjectLimit == 4 && pool.EntityQueryCount == 2 && pool.RerankPerEntity == 6 && pool.RecallPerEntity == 15, "pool budgets from return cap 4 / 2 entities: " + pool.MatchMode + " " + pool.RerankPerEntity + " " + pool.RecallPerEntity);
        Check(pool.OrderedRules.Count >= 2 && pool.OrderedRules[0].Id.StartsWith("apple") && pool.OrderedRules[1].Id == "berry", "primary pass: best rule per entity in mention order: " + string.Join(",", pool.OrderedRules.Select(r => r.Id)));
        Check(pool.OrderedRules.Select(r => r.Id).Distinct().Count() == pool.OrderedRules.Count, "no duplicate rules across entities");
        Check(logs.Any(l => l.StartsWith("LoreMatch:knowledge_mentions terms=2")) && logs.Any(l => l.StartsWith("KnowledgeRetrieval:entity_allocation")) && logs.Any(l => l.StartsWith("LoreMatch:candidate_pool mode=mentions_semantic_per_entity")), "legacy log lines emitted");
        var disabled = retriever.CollectCandidateRules(new[] { "apple" }, retrievalEnabled: false);
        Check(disabled.MatchMode == "none" && disabled.OrderedRules.Count == 0 && disabled.InjectLimit == 2, "retrieval disabled → default pool");
        logs.Clear();
        var none = retriever.CollectCandidateRules(new string[0], true);
        Check(none.OrderedRules.Count == 0 && logs.Any(l => l.Contains("skip reason=no_mentions entities=0")), "no mentions → skip log");

        // collision: both entities' best is the same rule → second entity falls back to its rank-2 candidate.
        // (fake embedding: "berry" scores both rules 1.0; id tie-break puts only-berry first, so shared is taken by apple and berry keeps its own best)
        var rules2 = new List<LoreRule> { Rule("shared", "apple berry"), Rule("only-berry", "berry") };
        var idx2 = new KnowledgeRuleIndex(new FakePorts { Embed = Vec, SemanticMinScore = 0f, SemanticTopK = 2 }, () => rules2);
        idx2.EnsureOnnxIndex();
        var r2 = new LoreCandidateRetriever(idx2, (c, m) => logs.Add(m));
        var sel = r2.SelectVectorRulesPerEntity(new List<WeightedKnowledgeInput> { new WeightedKnowledgeInput { Text = "apple" }, new WeightedKnowledgeInput { Text = "berry" } }, 2, 10, 4, 2, out string mode);
        Check(mode == "semantic_per_entity" && sel.Select(x => x.Id).SequenceEqual(new[] { "shared", "only-berry" }) && logs.Last(l => l.StartsWith("entity_allocation")).Contains("primary=2"), "unique rule per entity in mention order: " + string.Join(",", sel.Select(x => x.Id)));
        var rules3 = new List<LoreRule> { Rule("both", "apple berry") };
        var idx3 = new KnowledgeRuleIndex(new FakePorts { Embed = Vec, SemanticMinScore = 0f, SemanticTopK = 2 }, () => rules3);
        idx3.EnsureOnnxIndex();
        var r3 = new LoreCandidateRetriever(idx3, (c, m) => logs.Add(m));
        var sel3 = r3.SelectVectorRulesPerEntity(new List<WeightedKnowledgeInput> { new WeightedKnowledgeInput { Text = "apple" }, new WeightedKnowledgeInput { Text = "berry" } }, 2, 10, 4, 4, out mode);
        Check(sel3.Count == 1 && sel3[0].Id == "both" && logs.Last(l => l.StartsWith("entity_allocation")).Contains("2:berry->(none)"), "same rule for two entities is injected once");
        var capped = r2.SelectVectorRulesPerEntity(new List<WeightedKnowledgeInput> { new WeightedKnowledgeInput { Text = "berry" } }, 1, 10, 4, 1, out mode);
        Check(capped.Count == 1, "inject limit 1 stops after first entity");
        var lower = r2.SelectVectorRulesPerEntity(new List<WeightedKnowledgeInput> { new WeightedKnowledgeInput { Text = "berry" } }, 1, 10, 4, 3, out mode);
        Check(lower.Count == 2 && logs.Last(l => l.StartsWith("entity_allocation")).Contains("allowLowerRanks=True") && logs.Last(l => l.StartsWith("entity_allocation")).Contains("secondary=1"), "limit > entity count allows rank-2 sweep");
        Check(r2.SelectVectorRulesPerEntity(new List<WeightedKnowledgeInput> { new WeightedKnowledgeInput { Text = " ", Weight = 1f }, new WeightedKnowledgeInput { Text = "x", Weight = 0f } }, 2, 10, 4, 3, out mode).Count == 0 && mode == "none", "blank/zero-weight inputs ignored");
    }
}
