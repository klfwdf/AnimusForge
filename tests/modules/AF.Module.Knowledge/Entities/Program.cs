using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

// Contract for the J06c world-entity retrieval owners (fuzzy matcher, mention list, injection allocator).
internal static class Program
{
    private static int _checks;
    private static void Check(bool c, string m) { _checks++; if (!c) throw new Exception("FAIL: " + m); }

    private static GlobalEntityCandidate C(string type, string key, string name, string mention, int prio, float score, bool exact = false, string heroClan = null, string heroKingdom = null, string scopeClan = null, string scopeKingdom = null, float distBonus = 0f)
        => new GlobalEntityCandidate { Type = type, Key = type + ":" + key, Name = name, Mention = mention, MentionPriority = prio, TypePriority = type == "hero" ? 0 : type == "settlement" ? 1 : type == "clan" ? 2 : 3, Score = score, FinalScore = score, ExactNameMatch = exact, HeroClanId = heroClan, HeroKingdomId = heroKingdom, ScopeClanId = scopeClan, ScopeKingdomId = scopeKingdom, HeroDistanceBonus = distBonus, HeroDistance = distBonus > 0 ? 5f : float.MaxValue };

    private static void Main()
    {
        Matcher();
        Mentions();
        Allocator();
        Console.WriteLine("PASS knowledge-entities checks=" + _checks);
    }

    private static void Matcher()
    {
        Check(EntityNameMatcher.Normalize(" Der-ven's Hold! ") == "dervenshold" && EntityNameMatcher.Normalize("阿塞莱 城") == "阿塞莱城" && EntityNameMatcher.Normalize(null) == "", "normalize keeps letters/digits/cjk lower-cased");
        Check(EntityNameMatcher.IsExactNameMatch("Derven's hold", "DERVENS HOLD") && !EntityNameMatcher.IsExactNameMatch("", "x") && !EntityNameMatcher.IsExactNameMatch("a", "b"), "exact match after normalization");
        Check(EntityNameMatcher.Score("Vlandia", "vlandia") == 1f && EntityNameMatcher.Score("", "x") == 0f, "identical → 1, blank → 0");
        float contain = EntityNameMatcher.Score("Vland", "Vlandia");
        Check(Math.Abs(contain - (0.86f + 0.12f * 5f / 7f)) < 1e-5, "containment score: " + contain);
        Check(EntityNameMatcher.Score("Vlandio", "Vlandia") >= 1f - 1f / 7f - 1e-5, "levenshtein floor");
        Check(EntityNameMatcher.LevenshteinDistance("kitten", "sitting") == 3 && EntityNameMatcher.LevenshteinDistance("", "ab") == 2, "levenshtein");
        Check(EntityNameMatcher.ShortCjkNearNameScore("阿塞莱", "阿赛莱", 1) == 0.82f && EntityNameMatcher.ShortCjkNearNameScore("阿塞莱城", "阿赛莱城", 1) == 0.86f && EntityNameMatcher.ShortCjkNearNameScore("阿塞莱", "阿塞莱城", 1) == 0.80f && EntityNameMatcher.ShortCjkNearNameScore("abc", "abd", 1) == 0f && EntityNameMatcher.ShortCjkNearNameScore("阿塞莱", "阿赛莱", 2) == 0f, "short cjk near-name tiers");
        Check(EntityNameMatcher.SplitTokens("Lord Derthert of Vlandia, x").SequenceEqual(new[] { "lord", "derthert", "of", "vlandia" }), "tokens drop single chars, distinct");
        float overlap = EntityNameMatcher.TokenOverlapScore("Derthert of Vlandia", "King Derthert");
        Check(Math.Abs(overlap - (0.65f + 0.25f * 1f / 4f)) < 1e-5, "token overlap jaccard band: " + overlap);
        Check(EntityNameMatcher.BestScore("Derthert", new[] { "Nobody", "derthert", null }) == 1f && EntityNameMatcher.BestScore("x", null) == 0f, "best alias score");
        Check(EntityNameMatcher.BuildAliasProfiles(new[] { " a ", "A", "", "b" }).Count == 2, "alias profiles dedupe");
        Check(EntityNameMatcher.IsOrderedSubsequence("ac", "abc") && !EntityNameMatcher.IsOrderedSubsequence("ca", "abc") && EntityNameMatcher.IsAllCjkText("汉字") && !EntityNameMatcher.IsAllCjkText("汉a"), "subsequence / all-cjk");
    }

    private static void Mentions()
    {
        var unified = EntityMentionList.BuildUnified(new[] { " 瓦兰迪亚的德塞特 ", "德塞特", "", null, "帝国之剑", "Vlandia" });
        Check(unified.SequenceEqual(new[] { "瓦兰迪亚", "德塞特", "帝国", "剑", "Vlandia" }), "的/之 segmentation, ordered dedupe: " + string.Join(",", unified));
        var prio = EntityMentionList.BuildPriority(unified);
        Check(prio["德塞特"] == 1 && prio["vlandia"] == 4 && EntityMentionList.GetPriority(prio, " 剑 ") == 3 && EntityMentionList.GetPriority(prio, "none") == int.MaxValue / 2, "priority map ci + fallback");
        Check(EntityMentionList.BuildActiveRuleIdSet(new[] { " Duel ", "duel", "" }).SetEquals(new[] { "duel" }), "active rule ids lower-cased");
        Check(EntityMentionList.CountNonBlank(new List<string> { "a", " ", null }) == 1 && EntityMentionList.FormatForLog(null) == "(none)" && EntityMentionList.FormatForLog(Enumerable.Range(0, 20).Select(i => "m" + i)).Split('|').Length == 12, "count / log format cap 12");
    }

    private static void Allocator()
    {
        Check(EntityInjectionAllocator.ClampMaxInjectedEntities(0) == 1 && EntityInjectionAllocator.ClampMaxInjectedEntities(99) == 20 && EntityInjectionAllocator.ClampMaxInjectedEntities(6) == 6, "clamp 1..20");
        Check(EntityInjectionAllocator.ComputeDistanceBonus(0f) == 0.15f && Math.Abs(EntityInjectionAllocator.ComputeDistanceBonus(30f).Value - 0.15f * (float)Math.Exp(-1)) < 1e-6 && EntityInjectionAllocator.ComputeDistanceBonus(float.NaN) == null && EntityInjectionAllocator.ComputeDistanceBonus(-1f) == null, "distance bonus exp decay / invalid → null");
        Check(EntityInjectionAllocator.PreviewLogValue("a\r\nb", 10) == "a  b" && EntityInjectionAllocator.PreviewLogValue("abcdef", 3) == "abc...", "log preview");

        // one candidate per mention, primary pass in mention order, then secondary sweep when cap > mentions.
        var cands = new List<GlobalEntityCandidate>
        {
            C("settlement", "s1", "Pravend", "Pravend", 0, 1f, true),
            C("hero", "h1", "Derthert", "Derthert", 1, 1f, true, heroClan: "clan_a"),
            C("hero", "h2", "Derthert2", "Derthert", 1, 0.95f),
            C("kingdom", "k1", "Vlandia", "Vlandia", 2, 1f, true, scopeKingdom: "k1"),
        };
        var sel = EntityInjectionAllocator.Select(cands, 2, 3, out string summary);
        Check(sel.Select(x => x.Key).SequenceEqual(new[] { "settlement:s1", "hero:h1" }) && summary.Contains("primary=2") && summary.Contains("allowSecondary=False"), "cap 2 over 3 mentions: first two mentions only: " + summary);
        sel = EntityInjectionAllocator.Select(cands, 6, 3, out summary);
        Check(sel.Select(x => x.Key).SequenceEqual(new[] { "settlement:s1", "hero:h1", "kingdom:k1", "hero:h2" }) && summary.Contains("secondary=1") && summary.Contains("allowSecondary=True"), "secondary sweep adds runner-up: " + string.Join(",", sel.Select(x => x.Key)));

        // ambiguous person name: two heroes for mention 1, one exact → scope boost from kingdom mention promotes the clan/kingdom member.
        var amb = new List<GlobalEntityCandidate>
        {
            C("hero", "h1", "Derthert", "Derthert", 0, 1f, true, heroKingdom: "empire"),
            C("hero", "h2", "Derthert", "Derthert", 0, 1f, true, heroKingdom: "vlandia"),
            C("kingdom", "k1", "Vlandia", "Vlandia", 1, 1f, true, scopeKingdom: "vlandia"),
        };
        Check(EntityInjectionAllocator.FindAmbiguousPersonNamePriorities(amb).SetEquals(new[] { 0 }), "ambiguous person-name mention detected");
        sel = EntityInjectionAllocator.Select(amb, 3, 2, out summary);
        Check(sel[0].Key == "hero:h2" && sel[0].HeroScopeScore == 1000 + 99 && sel[0].HeroScopeEvidence == "王国:Vlandia" && summary.Contains("scopeBoostedHeroes=1"), "kingdom scope boosts the matching hero: " + summary);
        var ambClan = new List<GlobalEntityCandidate>
        {
            C("hero", "h1", "A", "A", 0, 1f, true, heroClan: "c1", heroKingdom: "k9"),
            C("hero", "h2", "A", "A", 0, 1f, true, heroClan: "c2"),
            C("clan", "c1", "Clan1", "Clan1", 1, 1f, true, scopeClan: "c1"),
        };
        sel = EntityInjectionAllocator.Select(ambClan, 3, 2, out summary);
        Check(sel[0].Key == "hero:h1" && sel[0].HeroScopeScore == 10000 + 99, "clan scope outranks kingdom scope");

        // no ambiguity → no scope boost, and distance bonus only applies to ambiguous heroes.
        var single = new List<GlobalEntityCandidate> { C("hero", "h1", "A", "A", 0, 0.9f, distBonus: 0.5f), C("kingdom", "k1", "K", "K", 1, 1f, true, scopeKingdom: "k1") };
        sel = EntityInjectionAllocator.Select(single, 3, 2, out summary);
        Check(sel[0].HeroScopeScore == 0 && sel[0].HeroDistanceBonus == 0f && sel[0].FinalScore == 0.9f && summary.Contains("distanceBoostedHeroes=0"), "unambiguous hero gets no boosts");
        var amb2 = new List<GlobalEntityCandidate> { C("hero", "h1", "A", "A", 0, 0.95f, true, distBonus: 0.5f), C("hero", "h2", "A", "A", 0, 1f, true) };
        sel = EntityInjectionAllocator.Select(amb2, 3, 1, out summary);
        Check(sel[0].Key == "hero:h1" && Math.Abs(sel[0].FinalScore - 1.10f) < 1e-5 && sel[0].HeroDistanceBonus == 0.15f && summary.Contains("distanceBoostedHeroes=1"), "distance bonus capped at 0.15 lifts nearby ambiguous hero: " + sel[0].FinalScore);

        // competing exact non-hero cancels ambiguity; duplicate keys collapse; unknown priorities appended after known.
        var comp = new List<GlobalEntityCandidate> { C("hero", "h1", "Epicrotea", "Epicrotea", 0, 1f, true), C("hero", "h2", "Epicrotea", "Epicrotea", 0, 1f, true), C("settlement", "s1", "Epicrotea", "Epicrotea", 0, 1f, true) };
        Check(EntityInjectionAllocator.FindAmbiguousPersonNamePriorities(comp).Count == 0, "exact settlement name competes → not ambiguous");
        sel = EntityInjectionAllocator.Select(comp, 5, 1, out summary);
        Check(sel[0].Type == "hero" && sel.Count == 3 && summary.Contains("secondary=2"), "type priority breaks ties; secondary limited to 3 per mention");
        var many = Enumerable.Range(0, 6).Select(i => C("settlement", "s" + i, "S" + i, "S", 0, 1f - i * 0.01f)).ToList();
        sel = EntityInjectionAllocator.Select(many, 10, 1, out summary);
        Check(sel.Count == 1 + EntityInjectionAllocator.MaxSecondaryMatchesPerMention && summary.Contains("secondary=3"), "secondary pass capped at 3 per mention even with headroom: " + sel.Count);
        var late = new List<GlobalEntityCandidate> { C("clan", "c9", "Z", "Z", 7, 1f, true), C("hero", "h1", "A", "A", 0, 1f, true) };
        sel = EntityInjectionAllocator.Select(late, 5, 1, out summary);
        Check(sel.Select(x => x.Key).SequenceEqual(new[] { "hero:h1", "clan:c9" }) && summary.Contains("8:Z->clan:Z@1"), "unknown priority appended after known mention slots");
        Check(EntityInjectionAllocator.Select(null, 3, 0, out summary).Count == 0 && summary.Contains("assignments=(none)"), "null candidates");
    }
}
