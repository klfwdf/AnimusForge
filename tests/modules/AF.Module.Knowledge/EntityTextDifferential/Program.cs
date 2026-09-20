using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge
{
    public sealed class TextObject { public string Value; public override string ToString() => Value; }
    public interface IFaction { TextObject Name { get; } string StringId { get; } bool IsAtWarWith(IFaction other); }
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
        public string StringId { get; set; }
        public Hero Leader;
        public bool IsEliminated, ModCreated;
        public float CurrentTotalStrength;
        public CultureObject Culture;
        public List<Clan> Clans = new List<Clan>();
        public TextObject EncyclopediaText;
        public bool IsAllyWith(Kingdom other) => false;
        public TextObject EncyclopediaRulerTitle;
        public bool IsAtWarWith(IFaction other) => false;
    }
    public sealed class Clan : IFaction
    {
        public static List<Clan> All = new List<Clan>();
        public static Clan PlayerClan;
        public TextObject Name { get; set; }
        public TextObject InformalName;
        public string StringId { get; set; }
        public Hero Leader;
        public Kingdom Kingdom;
        public bool IsEliminated;
        public float Influence;
        public int Gold, Tier;
        public CultureObject Culture;
        public List<Hero> Heroes = new List<Hero>();
        public List<Town> Fiefs = new List<Town>();
        public bool IsAtWarWith(IFaction other) => false;
    }
    public sealed class CultureObject { public TextObject Name; public string StringId; }
    public sealed class Settlement
    {
        public static List<Settlement> All = new List<Settlement>();
        public TextObject Name; public string StringId;
        public Clan OwnerClan; public IFaction MapFaction; public CultureObject Culture;
        public bool IsVillage, IsTown, IsCastle, IsHideout, IsFortification, IsUnderSiege;
        public float Militia; public PartyBase Party; public Town Town; public Village Village;
        public List<Village> BoundVillages = new List<Village>();
    }
    public sealed class Town
    {
        public Settlement Settlement; public MobileParty GarrisonParty;
        public float Prosperity, Loyalty, Security;
    }
    public sealed class Village
    {
        public enum VillageStates { Normal, BeingRaided }
        public Settlement Settlement; public float Hearth; public VillageStates VillageState;
    }
    public sealed class Roster { public int TotalManCount; }
    public sealed class MapEvent { public bool IsFinalized; }
    public sealed class Army { public TextObject Name; }
    public sealed class MobileParty
    {
        public static MobileParty MainParty;
        public static List<MobileParty> All = new List<MobileParty>();
        public bool IsActive = true, IsVisible, IsMainParty, IsGarrison, IsMilitia;
        public MapEvent MapEvent; public PartyBase Party; public Roster MemberRoster;
        public Hero LeaderHero, Owner; public IFaction MapFaction; public Clan ActualClan;
        public Settlement HomeSettlement; public float SeeingRange; public CampaignVec2 Position;
        public string ShipInfo;
        public TextObject Name;
        public string StringId;
        public Settlement CurrentSettlement, TargetSettlement;
        public Army Army;
        public string DefaultBehavior;
    }
    public sealed class PartyBase
    {
        public int NumberOfAllMembers;
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
    public static class MyBehavior { public static string BuildPlayerPublicDisplayNameForExternal() => "Player"; public static bool IsModCreatedRebelKingdomForExternal(Kingdom kingdom) => kingdom.ModCreated; }
    public sealed class Campaign { public static Campaign Current = new Campaign(); public T GetCampaignBehavior<T>() where T : class => null; }
    public interface ITradeAgreementsCampaignBehavior { }
    public static class BannerlordApiCompat { public static bool HasTradeAgreement(ITradeAgreementsCampaignBehavior behavior, Kingdom first, Kingdom second) => false; }
    // External ship-layout provider remains a fixture; production selection/formatting is extracted.
    public static class MapSeaContextGuard { public static string BuildMobilePartyShipPromptText(MobileParty party) => party.ShipInfo; }
    public readonly struct CampaignVec2
    {
        public readonly float X, Y; private readonly bool valid;
        public CampaignVec2(float x, float y) { X = x; Y = y; valid = true; }
        public static CampaignVec2 Invalid => new CampaignVec2();
        public bool IsValid() => valid;
        public float Distance(CampaignVec2 other) => (float)Math.Sqrt((X-other.X)*(X-other.X)+(Y-other.Y)*(Y-other.Y));
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
        private const int MainPromptClanMemberCap = 8, MainPromptClanFiefCap = 8, MainPromptKingdomClanCap = 6, MainPromptKingdomEncyclopediaTextCap = 600;
        private const int MaxVisiblePartyCandidates = 10;
        private const float VisiblePartyMinRange = 18f, VisiblePartyRangeMultiplier = 1.5f;
        private const float MatchThreshold = 0.72f;
        private const float NearTopDelta = 0.07f;
        private const int MaxCandidatesPerMention = EntityInjectionAllocator.MaxInjectedEntitiesHardCap;
        private const int EntityRetrievalProgressLogInterval = 500;
        private const int EntityRetrievalBudgetCheckInterval = 64;
        private const int EntityRetrievalSoftBudgetMs = 1500;
        private const int EntityRetrievalHardBudgetMs = 3000;
        private static int GetMaxInjectedEntitiesFromSettings() => 3;
        private static bool TryResolveHeroCampaignPosition(Hero hero, out CampaignVec2 position) { position = CampaignVec2.Invalid; return false; }
        private static bool CanContinueWorldEntityMatch(string category, WorldEntityRetrievalBudget budget) => !budget.IsHardExceeded;
        // FindRawRulerTitleMatches and its helpers are production methods (raw input carries "Alda the King").
        // Game-only navigation helpers not reached by these controlled stationary fixtures.
        private static string FormatMobilePartyMapLocation(MobileParty party) => throw new Exception("unexpected party");
        private static string FormatPrisonerHolder(Hero hero) => throw new Exception("unexpected prisoner");
        private static string FormatNearestSettlementForParty(MobileParty party) => throw new Exception("unexpected party");
        private static string FormatMobilePartyMapTerrainSuffix(MobileParty party) => throw new Exception("unexpected party");
        public static (string Main, string Post, string Meta) Render(bool title)
        {
            const string input = "Tell me about Praven, Alda the King; can we barter this item?";
            ResetWorld();
            Kingdom.All.Clear();
            var hero = new Hero { Name = new TextObject { Value = "Alda" }, StringId = "npc_1", Age = 31, IsLord = true };
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
            var capture = CaptureEntityCandidates(mentioned, input, null);
            if (capture.Candidates.Heroes.Count != 1 || capture.Candidates.Rulers.Count != (title ? 1 : 0)) throw new Exception("production candidate capture mismatch");
            var selected = MatchDetachedCandidates(capture.Candidates, mentioned, input, capture.MaxInjectedEntities);
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
            var context = BuildPromptContext(mentionInput, "Player", null, false, null, input, false, capture, selected);
            var fallback = BuildPromptContext(mentionInput, "Player", null, false, null, input, false, null, null);
            if (fallback.MainPromptBlock != context.MainPromptBlock || fallback.PostprocessPromptBlock != context.PostprocessPromptBlock
                || fallback.MatchCount != context.MatchCount || !fallback.ExplicitMentionedKingdomIds.SequenceEqual(context.ExplicitMentionedKingdomIds))
                throw new Exception("production detached/fallback entity text differs, title=" + title);
#else
            var context = BuildPromptContext(mentionInput, "Player", null, false, null, input, false);
#endif
            if (context.MatchCount != 1 || context.MainPromptBlock != expectedMain || context.PostprocessPromptBlock != expectedPost)
                throw new Exception("production BuildPromptContext differs, title=" + title + " count=" + context.MatchCount + " main=" + context.MainPromptBlock.Length + "/" + expectedMain.Length + " post=" + context.PostprocessPromptBlock.Length + "/" + expectedPost.Length);
            return (context.MainPromptBlock, context.PostprocessPromptBlock, context.MatchCount + "|" + string.Join(",", context.ExplicitMentionedKingdomIds));
        }

        // Raw-title scenarios keep the original neutral Hero fixtures independent of world cases.
        public static (string Main, string Post, string Meta) RenderRaw(string scenario)
        {
            ResetWorld();
            var alda = new Hero { Name = new TextObject { Value = "Alda" }, StringId = "npc_1", IsLord = true };
            var borin = new Hero { Name = new TextObject { Value = "Borin" }, StringId = "npc_2", IsLord = true };
            var cara = new Hero { Name = new TextObject { Value = "Cara" }, StringId = "npc_3", IsLord = true };
            Hero.AllAliveHeroes = new List<Hero> { alda, borin, cara };
            Hero.DeadOrDisabledHeroes = new List<Hero>();
            Kingdom.All = new List<Kingdom>
            {
                new Kingdom { Name = new TextObject { Value = "Vlandia" }, StringId = "vlandia", Leader = alda, EncyclopediaRulerTitle = new TextObject { Value = "King" } },
                new Kingdom { Name = new TextObject { Value = "Sturgia" }, StringId = "sturgia", Leader = borin, EncyclopediaRulerTitle = new TextObject { Value = "King" } },
                new Kingdom { Name = new TextObject { Value = "Empire" }, StringId = "empire", Leader = cara, EncyclopediaRulerTitle = new TextObject { Value = "High King" } }
            };
            var mentions = new MentionedWorldEntities();
            string input;
            string[] expectedIds;
            switch (scenario)
            {
                case "raw_only":
                    Kingdom.All.RemoveRange(1, 2);
                    input = "Tell me about the King.";
                    expectedIds = new[] { "npc_1" };
                    break;
                case "raw_qualified":
                    input = "Tell me about the King of Vlandia.";
                    expectedIds = new[] { "npc_1" };
                    break;
                case "raw_ambiguous":
                    input = "Tell me about the King.";
                    expectedIds = new[] { "npc_1", "npc_2" };
                    break;
                case "raw_long_title":
                    input = "Tell me about the High King.";
                    expectedIds = new[] { "npc_3" };
                    break;
                case "raw_distinct_titles":
                    input = "Tell me about the High King and the King.";
                    expectedIds = new[] { "npc_1", "npc_2", "npc_3" };
                    break;
                case "raw_overrides_mentions":
                    mentions.Entities.Add("King");
                    input = "Tell me about the King of Vlandia.";
                    expectedIds = new[] { "npc_1" };
                    break;
                default:
                    throw new ArgumentException("Unknown raw-title scenario: " + scenario);
            }
#if CURRENT
            var capture = CaptureEntityCandidates(mentions, input, null);
            var selected = MatchDetachedCandidates(capture.Candidates, mentions, input, capture.MaxInjectedEntities);
            var context = BuildPromptContext(mentions, "Player", null, false, null, input, false, capture, selected);
            var fallback = BuildPromptContext(mentions, "Player", null, false, null, input, false, null, null);
#else
            var context = BuildPromptContext(mentions, "Player", null, false, null, input, false);
#endif
            string[] actualIds = Hero.AllAliveHeroes.Where(hero => context.PostprocessPromptBlock.Contains(hero.StringId))
                .Select(hero => hero.StringId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (context.MatchCount != expectedIds.Length || !actualIds.SequenceEqual(expectedIds)
                || string.IsNullOrWhiteSpace(context.MainPromptBlock))
                throw new Exception("raw-title coverage failed scenario=" + scenario + " expected=" + string.Join(",", expectedIds)
                    + " actual=" + string.Join(",", actualIds) + " count=" + context.MatchCount);
#if CURRENT
            if (fallback.MainPromptBlock != context.MainPromptBlock || fallback.PostprocessPromptBlock != context.PostprocessPromptBlock
                || fallback.MatchCount != context.MatchCount || !fallback.ExplicitMentionedKingdomIds.SequenceEqual(context.ExplicitMentionedKingdomIds))
                throw new Exception("raw-title detached/fallback differs scenario=" + scenario);
#endif
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
            if (!main.Contains("Alda") || !main.Contains("【人物】") || !post.Contains("npc_1")) throw new Exception("entity facts missing, title=" + title);
            string prefix = title ? "RESULT_title_" : "RESULT_direct_";
            Console.WriteLine(prefix + "main=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(main)));
            Console.WriteLine(prefix + "post=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(post)));
            Console.WriteLine(prefix + "meta=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta)));
        }
        foreach (string scenario in new[] { "raw_only", "raw_qualified", "raw_ambiguous", "raw_long_title", "raw_distinct_titles", "raw_overrides_mentions" })
        {
            var (main, post, meta) = AnimusForge.WorldEntityRetrievalService.RenderRaw(scenario);
            Console.WriteLine("RESULT_" + scenario + "_main=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(main)));
            Console.WriteLine("RESULT_" + scenario + "_post=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(post)));
            Console.WriteLine("RESULT_" + scenario + "_meta=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta)));
        }
        foreach (string scenario in AnimusForge.WorldEntityRetrievalService.WorldCases)
        {
            var (main, post, meta) = AnimusForge.WorldEntityRetrievalService.RenderWorld(scenario);
            Console.WriteLine("RESULT_" + scenario + "_main=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(main)));
            Console.WriteLine("RESULT_" + scenario + "_post=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(post)));
            Console.WriteLine("RESULT_" + scenario + "_meta=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(meta)));
        }
    }
}
