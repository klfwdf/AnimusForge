using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge
{
    public sealed class TextObject { public string Value; public override string ToString() => Value; }
    public interface IFaction { TextObject Name { get; } bool IsAtWarWith(IFaction other); }
    public sealed class TraitObject { }
    public static class DefaultTraits
    {
        public static TraitObject Mercy = new TraitObject(), Valor = new TraitObject(), Honor = new TraitObject(), Generosity = new TraitObject(), Calculating = new TraitObject();
    }
    public class Kingdom : IFaction
    {
        public static List<Kingdom> All = new List<Kingdom>();
        public TextObject Name { get; set; }
        public TextObject InformalName;
        public string StringId;
        public Hero Leader;
        public bool IsEliminated;
        public TextObject EncyclopediaRulerTitle;
        public bool IsAtWarWith(IFaction other) => false;
    }
    public sealed class Clan
    {
        public TextObject Name;
        public TextObject InformalName;
        public string StringId;
        public Hero Leader;
        public Kingdom Kingdom;
        public bool IsEliminated;
    }
    public sealed class Settlement { public TextObject Name; public string StringId; }
    public sealed class Army { public TextObject Name; }
    public sealed class MobileParty
    {
        public TextObject Name;
        public string StringId;
        public Settlement CurrentSettlement, TargetSettlement;
        public Army Army;
        public string DefaultBehavior;
    }
    public sealed class PartyBase
    {
        public bool IsSettlement, IsMobile;
        public Settlement Settlement;
        public MobileParty MobileParty;
    }
    public sealed class Hero
    {
        public static Hero MainHero;
        public static List<Hero> AllAliveHeroes = new List<Hero>(), DeadOrDisabledHeroes = new List<Hero>();
        public TextObject Name;
        public string StringId;
        public CharacterObject CharacterObject;
        public Clan Clan;
        public IFaction MapFaction;
        public Hero Father, Mother, Spouse;
        public List<Hero> Children = new List<Hero>(), Siblings = new List<Hero>();
        public Settlement CurrentSettlement, HomeSettlement;
        public MobileParty PartyBelongedTo;
        public PartyBase PartyBelongedToAsPrisoner;
        public bool IsAlive = true, IsPrisoner, IsFemale, IsKingdomLeader, IsLord, IsWanderer, IsNotable;
        public float Age = 33;
        public string Occupation = "Lord";
        public int GetRelation(Hero other) => 0;
        public int GetTraitLevel(TraitObject trait) => 0;
    }
    public sealed class CharacterObject { public TextObject Name; public string StringId; }
    public static class RomanceSystemBehavior { public static bool TryGetPrivateLoveAsPlayerRelation(Hero hero, out int relation) { relation = 0; return false; } }
    public static class MyBehavior { public static string BuildPlayerPublicDisplayNameForExternal() => "Player"; }
    public sealed class Campaign { public static Campaign Current = new Campaign(); }
    public readonly struct CampaignVec2
    {
        public static CampaignVec2 Invalid => new CampaignVec2();
        public bool IsValid() => false;
        public float Distance(CampaignVec2 other) => 0f;
    }
    public static class Logger { public static void Log(string category, string message) { } }
    public static class FreezeWatchdog
    {
        public readonly struct ScopeToken : IDisposable { public void Dispose() { } }
        public static ScopeToken Scope(string name) => new ScopeToken();
        public static void Mark(string name, string detail, bool immediate = false) { }
    }
    public static partial class WorldEntityRetrievalService
    {
        private const float MatchThreshold = 0.72f;
        private const float NearTopDelta = 0.07f;
        private const int MaxCandidatesPerMention = EntityInjectionAllocator.MaxInjectedEntitiesHardCap;
        private const int EntityRetrievalProgressLogInterval = 500;
        private const int EntityRetrievalBudgetCheckInterval = 64;
        private const int EntityRetrievalSoftBudgetMs = 1500;
        private const int EntityRetrievalHardBudgetMs = 3000;
        private static int GetMaxInjectedEntitiesFromSettings() => 3;
        private static List<VisiblePartyCandidate> BuildVisiblePartyCandidates(Hero hero) => new List<VisiblePartyCandidate>();
        private static bool TryResolveHeroCampaignPosition(Hero hero, out CampaignVec2 position) { position = CampaignVec2.Invalid; return false; }
        private static IEnumerable<Settlement> GetSettlementCandidates() => Array.Empty<Settlement>();
        private static IEnumerable<Clan> GetClanCandidates() => Array.Empty<Clan>();
        private static IEnumerable<Kingdom> GetKingdomCandidates() => Kingdom.All;
        private static bool CanContinueWorldEntityMatch(string category, WorldEntityRetrievalBudget budget) => !budget.IsHardExceeded;
#if CURRENT
        private static RawRulerTitleMatchResult FindRawRulerTitleMatches(string input, List<RulerTitleCandidate> candidates, WorldEntityRetrievalBudget budget) => throw new Exception("raw-title route not requested");
#else
        private static RawRulerTitleMatchResult FindRawRulerTitleMatches(string input, IEnumerable<Kingdom> kingdoms, WorldEntityRetrievalBudget budget) => throw new Exception("raw-title route not requested");
#endif
        private static void AddResidentEntityMatches(Hero contextHero, bool includeResidentKingdoms, bool includeResidentPlayerEntities, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
        { if (contextHero != null || includeResidentKingdoms || includeResidentPlayerEntities) throw new Exception("resident fixture not neutral"); }
        private static void AddPostprocessResidentEntityMatches(Hero contextHero, bool includeResidentPlayerEntities, ref List<EntityMatch<Hero>> heroes, ref List<EntityMatch<Settlement>> settlements, ref List<EntityMatch<Clan>> clans, ref List<EntityMatch<Kingdom>> kingdoms)
        { if (contextHero != null || includeResidentPlayerEntities) throw new Exception("resident fixture not neutral"); }
        // Unused categories are game-port seams in this Hero-only fixture; the Hero formatters are production methods.
        private static void AppendSettlementMainFacts(System.Text.StringBuilder sb, List<EntityMatch<Settlement>> matches) { if (matches?.Count > 0) throw new Exception("unexpected settlement"); }
        private static void AppendClanMainFacts(System.Text.StringBuilder sb, List<EntityMatch<Clan>> matches) { if (matches?.Count > 0) throw new Exception("unexpected clan"); }
        private static void AppendKingdomMainFacts(System.Text.StringBuilder sb, List<EntityMatch<Kingdom>> matches) { if (matches?.Count > 0) throw new Exception("unexpected kingdom"); }
        private static void AppendVisiblePartyFacts(System.Text.StringBuilder sb, List<VisiblePartyCandidate> parties) { if (parties?.Count > 0) throw new Exception("unexpected party"); }
        private static string BuildVisiblePartyPromptLine(int index, VisiblePartyCandidate party) => throw new Exception("unexpected party");
        private static string FormatMobilePartyMapLocation(MobileParty party) => throw new Exception("unexpected party");
        private static string FormatPrisonerHolder(Hero hero) => throw new Exception("unexpected prisoner");
        private static string FormatNearestSettlementForParty(MobileParty party) => throw new Exception("unexpected party");
        private static string FormatMobilePartyMapTerrainSuffix(MobileParty party) => throw new Exception("unexpected party");
        private static string FormatSettlementNameWithType(Settlement settlement, float distance = -1f) => throw new Exception("unexpected settlement");
        public static (string Main, string Post, string Meta) Render(bool title)
        {
            Hero.MainHero = null;
            Kingdom.All.Clear();
            var hero = new Hero { Name = new TextObject { Value = "Alda" }, StringId = "hero_alda", Age = 31, IsLord = true };
            Hero.AllAliveHeroes = new List<Hero> { hero };
            Hero.DeadOrDisabledHeroes = new List<Hero>();
            string[] mentions = title ? new[] { "King" } : new[] { "Alda" };
            if (title)
            {
                Kingdom.All.Add(new Kingdom { Name = new TextObject { Value = "Vlandia" }, StringId = "vlandia", Leader = hero, EncyclopediaRulerTitle = new TextObject { Value = "King" } });
            }
            List<EntityMatch<Hero>> heroes;
#if CURRENT
            var mentioned = new MentionedWorldEntities { Entities = mentions.ToList() };
            var capture = CaptureEntityCandidates(mentioned, "", null);
            if (capture.Candidates.Heroes.Count != 1 || capture.Candidates.Rulers.Count != (title ? 1 : 0)) throw new Exception("production candidate capture mismatch");
            var selected = MatchDetachedCandidates(capture.Candidates, mentioned, "", capture.MaxInjectedEntities);
            heroes = RestoreMatches(selected.Heroes, capture.Heroes);
#else
            if (title)
            {
                heroes = FindRulerTitleMatches(mentions, EntityMentionList.BuildPriority(mentions.ToList()), Kingdom.All, "preprocess", new WorldEntityRetrievalBudget(System.Diagnostics.Stopwatch.StartNew()));
            }
            else
            {
                heroes = FindMatches("hero", mentions, EntityMentionList.BuildPriority(mentions.ToList()), GetHeroCandidates(), GetHeroAliases,
                    x => "hero:" + SafeStringId(x.StringId), x => SafeName(x.Name, x.StringId), 3, new WorldEntityRetrievalBudget(System.Diagnostics.Stopwatch.StartNew()));
            }
#endif
            if (heroes.Count != 1 || heroes[0].Value != hero) throw new Exception("production Hero matcher did not select fake Hero, title=" + title);
            var settlements = new List<EntityMatch<Settlement>>();
            var clans = new List<EntityMatch<Clan>>();
            var kingdoms = new List<EntityMatch<Kingdom>>();
            var visible = new List<VisiblePartyCandidate>();
            string expectedMain = BuildMainPromptBlock("Player", null, heroes, settlements, clans, kingdoms, visible);
            string expectedPost = BuildPostprocessPromptBlock(heroes, settlements, clans, kingdoms, visible);
            var mentionInput = new MentionedWorldEntities { Entities = mentions.ToList() };
#if CURRENT
            var context = BuildPromptContext(mentionInput, "Player", null, false, null, "", false, capture, selected);
            var fallback = BuildPromptContext(mentionInput, "Player", null, false, null, "", false, null, null);
            if (fallback.MainPromptBlock != context.MainPromptBlock || fallback.PostprocessPromptBlock != context.PostprocessPromptBlock
                || fallback.MatchCount != context.MatchCount || !fallback.ExplicitMentionedKingdomIds.SequenceEqual(context.ExplicitMentionedKingdomIds))
                throw new Exception("production detached/fallback entity text differs, title=" + title);
#else
            var context = BuildPromptContext(mentionInput, "Player", null, false, null, "", false);
#endif
            if (context.MatchCount != 1 || context.MainPromptBlock != expectedMain || context.PostprocessPromptBlock != expectedPost)
                throw new Exception("production BuildPromptContext differs, title=" + title + " count=" + context.MatchCount + " main=" + context.MainPromptBlock.Length + "/" + expectedMain.Length + " post=" + context.PostprocessPromptBlock.Length + "/" + expectedPost.Length);
            return (context.MainPromptBlock, context.PostprocessPromptBlock, context.MatchCount + "|" + string.Join(",", context.ExplicitMentionedKingdomIds));
        }
    }
}

internal static class Program
{
    private static void Main()
    {
        foreach (bool title in new[] { false, true })
        {
            var (main, post, meta) = AnimusForge.WorldEntityRetrievalService.Render(title);
            if (!main.Contains("Alda") || !main.Contains("【人物】") || !post.Contains("hero_alda")) throw new Exception("entity facts missing, title=" + title);
            string prefix = title ? "RESULT_title_" : "RESULT_direct_";
            Console.WriteLine(prefix + "main=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(main)));
            Console.WriteLine(prefix + "post=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(post)));
            Console.WriteLine(prefix + "meta=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta)));
        }
    }
}
