using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AnimusForge
{
    public sealed class Hero { public string StringId; public string Name; public CharacterObject CharacterObject; }
    public sealed class CharacterObject { public string StringId; public Hero HeroObject; }
    public sealed class MentionedWorldEntities
    {
        public List<string> Entities = new List<string>();
        public bool IsEmpty => Entities.Count == 0;
        public MentionedWorldEntities Clone() => new MentionedWorldEntities { Entities = Entities.ToList() };
        public void Merge(MentionedWorldEntities other) { if (other != null) Entities.AddRange(other.Entities); }
    }
    public static class WorldEntityRetrievalService
    {
        internal sealed class EntityCapture { }
        internal sealed class DetachedEntityCandidates { }
        internal sealed class DetachedEntityMatches { }
#if CURRENT
        internal static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string player, Hero context, bool residentKingdom, HashSet<string> rules, string input, bool residentPlayer, EntityCapture capture, DetachedEntityMatches matches) { TextPorts.SawCapturedEntity = capture != null; return TextPorts.Entity(); }
#else
        internal static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string player, Hero context, bool residentKingdom, HashSet<string> rules, string input, bool residentPlayer) => TextPorts.Entity();
#endif
    }
    internal sealed class LoreCandidateRules { }
    public sealed class WorldEntityPromptContext
    {
        public string MainPromptBlock, PostprocessPromptBlock;
        public List<string> ExplicitMentionedKingdomIds = new List<string>();
        public bool HasContent;
        public int MatchCount;
    }
    internal static class TextPorts
    {
        internal static string Lore, Rule, EntityMain, EntityPost, EntityMeta;
        internal static int LoreCalls, RuleCalls, EntityCalls;
        internal static bool SawPreselected, SawCapturedEntity, SawFallbackHits;
        internal static WorldEntityPromptContext Entity()
        {
            EntityCalls++;
            return new WorldEntityPromptContext { MainPromptBlock = EntityMain, PostprocessPromptBlock = EntityPost,
                HasContent = !string.IsNullOrWhiteSpace(EntityMain), MatchCount = int.Parse(EntityMeta.Split('|')[0]),
                ExplicitMentionedKingdomIds = EntityMeta.Split('|')[1].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() };
        }
    }
    internal static class Logger { internal static void Log(string category, string value) { } }
    internal static class FreezeWatchdog
    {
        internal readonly struct ScopeToken : IDisposable { public void Dispose() { } }
        internal static ScopeToken Scope(string name) => new ScopeToken();
        internal static void Mark(string name, string detail) { }
    }
    internal sealed class RewardSystemBehavior
    {
        internal static RewardSystemBehavior Instance;
        internal bool HasUnpaidDebtForInteraction(Hero hero, CharacterObject character) => false;
        internal string BuildDueDateReferenceForAI() => "";
        internal string BuildDebtHintForAI(Hero hero) => "";
        internal string BuildTrustPromptForAI(Hero hero) => "";
        internal string BuildSettlementMerchantDebtHintForAI(CharacterObject character) => "";
    }
    internal static class DuelBehavior { internal static bool TryConsumeLastDuelResult(Hero hero, out bool playerWon) { playerWon = false; return false; } }
    internal static class TeamModuleServices
    {
        internal static GatheringPort Gathering = new GatheringPort();
        internal static PolicyPort Policy = new PolicyPort();
        internal sealed class GatheringPort { internal string BuildFeastAttendanceContext(Hero hero) => ""; }
        internal sealed class PolicyPort { internal string BuildActivePolicyDialogueContextForExternal(Hero hero, CharacterObject character, string kingdom) => ""; }
    }
    internal readonly struct CampaignVec2 { internal bool IsValid() => false; }
    internal static class LordEncounterBehavior
    {
        internal static bool IsEncounterMeetingMissionActive => false;
        internal static bool TryGetSavedMainPartyPosition(out CampaignVec2 value) { value = default; return false; }
    }
    internal sealed class MobileParty { internal static MobileParty MainParty = new MobileParty(); internal CampaignVec2 Position; }
    internal static class ShoutUtils { internal static string BuildNearbySettlementsDetailForPrompt(CampaignVec2 value, Hero hero) => ""; }
    internal static class VoteDealBehavior { internal static WorldEntityPromptContext BuildUnifiedAgendaPromptContextForExternal(Hero hero, MentionedWorldEntities mentions) => null; }
    internal static class RomanceSystemBehavior { internal static void SetMarriagePostprocessContextEnabled(Hero hero, bool enabled) { } }
    internal static class AfGcczShoutBridge
    {
        internal static bool IsActive() => false;
        internal static void AppendRuntimePromptToShoutContext(MyBehavior.ShoutPromptContext context, Hero hero, CharacterObject character, int agent, string culture) { }
        internal static bool HasInjectedRuleBlock(string extras) => false;
    }
    internal static class AIConfigHandler
    {
        internal static bool LoanEnabled => false;
        internal static bool RewardEnabled => false;
        internal static string BuildGuardrailClarificationHint(string input, bool duel, float duelScore, bool reward, float rewardScore, bool loan, float loanScore, bool surrounding, float surroundingScore) => "";
#if CURRENT
        internal static string GetLoreContextWithCandidates(string input, Hero hero, string secondary, MentionedWorldEntities mentions, LoreCandidateRules candidates, long version) { if (candidates == null || version == 0) throw new Exception("missing Lore candidate input"); TextPorts.LoreCalls++; return TextPorts.Lore; }
        internal static string GetLoreContextWithCandidates(string input, CharacterObject character, string kingdom, string secondary, MentionedWorldEntities mentions, LoreCandidateRules candidates, long version) { if (candidates == null || version == 0) throw new Exception("missing Lore candidate input"); TextPorts.LoreCalls++; return TextPorts.Lore; }
#else
        internal static string GetLoreContext(string input, Hero hero, string secondary, MentionedWorldEntities mentions) { TextPorts.LoreCalls++; return TextPorts.Lore; }
        internal static string GetLoreContext(string input, CharacterObject character, string kingdom, string secondary, MentionedWorldEntities mentions) { TextPorts.LoreCalls++; return TextPorts.Lore; }
#endif
    }
    public partial class MyBehavior
    {
        public sealed class ShoutPromptContext
        {
            public string Extras, EntityPostprocessContext, PreprocessExcludedRuleBlock;
            public MentionedWorldEntities MentionedEntities = new MentionedWorldEntities();
            public List<string> ExplicitMentionedKingdomIds = new List<string>();
            public List<string> PreprocessRuleIds = new List<string>();
            public List<string> PreprocessExcludedRuleIds = new List<string>();
            public bool UseDuelContext, UseRewardContext, IsLoanContext, IsQualified;
        }
        public sealed class WeeklyPromptSnapshot { }
        private readonly HashSet<string> _recentlyDefeatedByPlayer = new HashSet<string>();
        private readonly HashSet<string> _recentlyReleasedPrisoners = new HashSet<string>();
        private static ShoutPromptContext CreateEmptyShoutPromptContext() => new ShoutPromptContext();
        private static void LogShoutPromptContextStage(string stage, Stopwatch total, Stopwatch segment, Hero hero, CharacterObject character, int agent, string detail = "", bool immediate = false) { }
        private static bool IsPartyTransferRuleEligible(Hero hero, CharacterObject character, int agent) => false;
        private static bool HasDuelRuntimeTarget(Hero hero, CharacterObject character, int agent) => false;
        private static string BuildPlayerPublicDisplayNameForPrompt(Hero hero, CharacterObject character = null, int agent = -1) => "Player";
        private static string BuildHeroPrisonerStatusPromptLineForExternal(Hero hero) => "";
        private static string BuildHeroArmyRuntimeFactForPrompt(Hero hero) => "";
        private static string BuildPlayerArmyRuntimeFactForPrompt(Hero hero, CharacterObject character, int agent) => "";
        private static string BuildResidentRecentActionsPrompt(Hero hero, CharacterObject character, int agent) => "";
        private static bool ShouldExcludeNpcShortReportFromWeeklyShortLayer(string rules, Hero hero, CharacterObject character, string kingdom, WeeklyPromptSnapshot weekly) => false;
        private static string BuildWeeklyShortReportsPromptBlock(Hero hero, CharacterObject character, string kingdom, bool exclude, WeeklyPromptSnapshot weekly) => "";
        private static string BuildTriggeredWeeklyFullReportsPromptBlock(string rules, Hero hero, CharacterObject character, string kingdom, WeeklyPromptSnapshot weekly) => "";
        private static bool DoesPlayerNotorietyObserverKnowPlayer(Hero hero, CharacterObject character, int agent) => false;
        private static string AppendPlayerPartySharedResourcePrompt(string extras, Hero hero, CharacterObject character) => extras;
#if CURRENT
        private string BuildTriggeredRuleInstructions(string input, Hero hero, bool duel, bool qualified, int tier, bool reward, bool loan, bool surroundings, bool hasAnyHero, CharacterObject character, string kingdom, int agent, string secondary, bool includeDuelStake, bool playerWon, bool worldMap, IEnumerable<string> excluded, IEnumerable<string> preselected, bool suppressMeeting, List<GuardrailRuleHit> fallbackHits) { TextPorts.RuleCalls++; TextPorts.SawPreselected = preselected != null; TextPorts.SawFallbackHits = fallbackHits != null; return TextPorts.Rule; }
#else
        private string BuildTriggeredRuleInstructions(string input, Hero hero, bool duel, bool qualified, int tier, bool reward, bool loan, bool surroundings, bool hasAnyHero, CharacterObject character, string kingdom, int agent, string secondary, bool includeDuelStake, bool playerWon, bool worldMap, IEnumerable<string> excluded, IEnumerable<string> preselected, bool suppressMeeting) { TextPorts.RuleCalls++; TextPorts.SawPreselected = preselected != null; return TextPorts.Rule; }
#endif
        internal ShoutPromptContext Replay(bool preselected, bool captureFailed)
        {
            var hero = new Hero { StringId = "npc_1", Name = "Alda" };
            string input = Environment.GetEnvironmentVariable("AF_J06_COMMON_INPUT") ?? "Tell me about Praven, Alda the King; can we barter this item?";
            string mentionsJson = Environment.GetEnvironmentVariable("AF_J06_COMMON_MENTIONS");
            var mentions = new MentionedWorldEntities { Entities = mentionsJson == null ? new List<string> { "Praven", "Alda", "barter" } : JsonSerializer.Deserialize<List<string>>(mentionsJson) };
            var phases = new PromptBuildPhases
            {
                Request = new PromptBuildRequest { Input = input, TargetAgentIndex = 7,
                    HasAnyHero = true, HasTargetHero = true, PlayerClanTier = 2, MinimumClanTier = 1,
                    ExplicitExcludedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    ExcludedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    PreprocessExcludedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase), TargetDisplayName = "Alda" },
                Routing = new PromptRoutingResult { AuxiliaryRuleHitIds = preselected ? new List<string> { "trade_context" } : null },
                Retrieval = new PromptRetrievalCapture { AuxiliaryMentions = mentions,
#if CURRENT
                    LoreCandidates = new LoreCandidateRules(), LoreRuleVersion = 1,
                    EntityCapture = captureFailed ? null : new WorldEntityRetrievalService.EntityCapture(),
                    EntityMatches = captureFailed ? null : new WorldEntityRetrievalService.DetachedEntityMatches(),
                    FallbackExtraRuleHits = preselected ? null : new List<GuardrailRuleHit> { new GuardrailRuleHit { RuleId = "trade_context" } },
#endif
                }, DirectPreprocessMentions = mentions,
                TotalStopwatch = Stopwatch.StartNew(), StageStopwatch = Stopwatch.StartNew()
            };
            return CompleteSharedPromptBuild(phases, hero, null, null);
        }
    }
}

internal static class Program
{
    private static int Main()
    {
        try
        {
            string Decode(string key) => Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable(key) ?? throw new Exception("missing " + key)));
            AnimusForge.TextPorts.Lore = Decode("AF_J06_LORE");
            AnimusForge.TextPorts.Rule = Decode("AF_J06_RULE");
            AnimusForge.TextPorts.EntityMain = Decode("AF_J06_ENTITY_MAIN");
            AnimusForge.TextPorts.EntityPost = Decode("AF_J06_ENTITY_POST");
            AnimusForge.TextPorts.EntityMeta = Decode("AF_J06_ENTITY_META");
            if (new[] { AnimusForge.TextPorts.Lore, AnimusForge.TextPorts.Rule, AnimusForge.TextPorts.EntityMain, AnimusForge.TextPorts.EntityPost }.Any(string.IsNullOrWhiteSpace)) throw new Exception("empty retrieved text");
            bool preselected = Environment.GetEnvironmentVariable("AF_J06_PRESELECTED") == "1";
            bool captureFailed = Environment.GetEnvironmentVariable("AF_J06_CAPTURE_FAIL") == "1";
            var context = new AnimusForge.MyBehavior().Replay(preselected, captureFailed);
            if (AnimusForge.TextPorts.LoreCalls != 1 || AnimusForge.TextPorts.RuleCalls != 1 || AnimusForge.TextPorts.EntityCalls != 1 || AnimusForge.TextPorts.SawPreselected != preselected) throw new Exception("production section branch not reached");
#if CURRENT
            if (AnimusForge.TextPorts.SawCapturedEntity == captureFailed || AnimusForge.TextPorts.SawFallbackHits == preselected) throw new Exception("detached/fallback route not passed to section capture");
#endif
            if (Environment.GetEnvironmentVariable("AF_J06_ALLOW_LOSS") != "1" &&
                (!context.Extras.Contains(AnimusForge.TextPorts.Lore) || !context.Extras.Contains(AnimusForge.TextPorts.Rule) || !context.Extras.Contains(AnimusForge.TextPorts.EntityMain) || context.EntityPostprocessContext != AnimusForge.TextPorts.EntityPost)) throw new Exception("production completion lost retrieved text");
            Console.WriteLine("CONTEXT=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context, new JsonSerializerOptions { IncludeFields = true }))));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
