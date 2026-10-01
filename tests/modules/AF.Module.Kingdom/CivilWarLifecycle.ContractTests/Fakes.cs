// Only the TaleWorlds actions/context and AF host are fake. Owner/effects/catalog/adapter/parser are linked production files.
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem
{
    public interface IFaction { string StringId { get; } string Name { get; } bool IsEliminated { get; } bool IsAtWarWith(IFaction other); }
    public class Kingdom : IFaction
    {
        public static List<Kingdom> All = new();
        public string StringId { get; set; }
        public string Name => StringId;
        public bool IsEliminated { get; set; }
        public Clan RulingClan;
        public Hero Leader => RulingClan?.Leader;
        public List<Clan> Clans = new();
        public List<Settlement> Settlements => Clans.SelectMany(c => c.Settlements).ToList();
        public List<PolicyObject> ActivePolicies = new();
        public List<IFaction> FactionsAtWarWith = new();
        public float CurrentTotalStrength = 100;
        public bool IsAtWarWith(IFaction other) => FactionsAtWarWith.Contains(other);
        public void RemovePolicy(PolicyObject policy) => ActivePolicies.Remove(policy);
    }
    public class Clan
    {
        public static List<Clan> All = new();
        public static Clan PlayerClan;
        public string StringId, Name;
        public Kingdom Kingdom;
        public Hero Leader;
        public bool IsEliminated, IsBanditFaction, IsMinorFaction, IsUnderMercenaryService, IsClanTypeMercenary;
        public int Tier = 3;
        public float CurrentTotalStrength = 50;
        public List<Settlement> Settlements = new();
    }
    public class Hero
    {
        public static Hero MainHero;
        public string StringId => Clan?.StringId + "_hero";
        public Clan Clan;
        public bool IsAlive = true, IsPrisoner;
        public int Gold = 10000;
        public int GetRelation(Hero other) => 0;
        public int GetTraitLevel(TraitObject trait) => 0;
    }
    public class PolicyObject { public string StringId, Name; }
    public struct CampaignTime { public static CampaignTime Now => new() { ToDays = 700 }; public double ToDays; }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Settlement
    {
        public string StringId;
        public bool IsTown = true, IsCastle;
        public Clan OwnerClan;
        public static Settlement Find(string id) => Clan.All.SelectMany(c => c.Settlements).FirstOrDefault(s => s.StringId == id);
    }
}
namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
    public class TraitObject { }
    public static class DefaultTraits
    {
        public static TraitObject Mercy = new(), Authoritarian = new(), Egalitarian = new(), Valor = new(), Generosity = new(), Honor = new(), Calculating = new();
    }
}
namespace TaleWorlds.Core { public static class MBRandom { public static float RandomFloat => 0.99f; } }
namespace TaleWorlds.ObjectSystem
{
    public class MBObjectManager
    {
        public static MBObjectManager Instance = new();
        public T GetObject<T>(string id) where T : class => (typeof(T) == typeof(Clan) ? Clan.All.FirstOrDefault(c => c.StringId == id) as T
            : typeof(T) == typeof(Kingdom) ? Kingdom.All.FirstOrDefault(k => k.StringId == id) as T
            : Kingdom.All.SelectMany(k => k.ActivePolicies).FirstOrDefault(p => p.StringId == id) as T);
    }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class MakePeaceAction
    {
        public static bool Fail;
        public static void Apply(IFaction first, IFaction second)
        {
            if (Fail) throw new InvalidOperationException("peace failure");
            ((Kingdom)first).FactionsAtWarWith.Remove(second); ((Kingdom)second).FactionsAtWarWith.Remove(first);
        }
    }
    public static class ChangeKingdomAction
    {
        public static string FailClan;
        public static int Moves;
        public static void Move(Clan clan, Kingdom kingdom)
        {
            if (clan.StringId == FailClan) throw new InvalidOperationException("defection failure");
            clan.Kingdom?.Clans.Remove(clan); clan.Kingdom = kingdom; kingdom?.Clans.Add(clan); Moves++;
        }
        public static void ApplyByJoinToKingdomByDefection(Clan clan, Kingdom old, Kingdom target, CampaignTime time, bool showNotification) => Move(clan, target);
        public static void ApplyByJoinToKingdom(Clan clan, Kingdom target, CampaignTime time, bool showNotification) => Move(clan, target);
        public static void ApplyByLeaveKingdom(Clan clan, bool showNotification) => Move(clan, null);
    }
    public static class ChangeRelationAction { public static int Calls; public static void ApplyRelationChangeBetweenHeroes(Hero a, Hero b, int delta, bool show) { Calls++; } }
    public static class ChangeClanInfluenceAction { public static void Apply(Clan clan, float delta) { } }
    public static class ChangeRulingClanAction { public static void Apply(Kingdom kingdom, Clan clan) { kingdom.RulingClan = clan; } }
    public static class GiveGoldAction
    {
        public static bool ThrowAfterApply;
        public static int Calls;
        public static void ApplyBetweenCharacters(Hero a, Hero b, int gold, bool disableNotification)
        {
            a.Gold -= gold; b.Gold += gold; Calls++;
            if (ThrowAfterApply) throw new InvalidOperationException("gold event failed after transfer");
        }
    }
}
namespace TaleWorlds.CampaignSystem.Election
{
    public class DecisionOutcome { }
    public class KingdomDecision { public Kingdom Kingdom; }
    public class SettlementClaimantDecision : KingdomDecision { public class ClanAsDecisionOutcome : DecisionOutcome { } }
    public class KingdomPolicyDecision : KingdomDecision
    {
        public PolicyObject Policy;
        public class PolicyDecisionOutcome : DecisionOutcome { public bool ShouldDecisionBeEnforced; }
    }
}
namespace AnimusForge.Refactor.Modules
{
    internal static class TeamModuleServices { internal static ICivilWarModulePort CivilWar = new AnimusForge.CivilWarModuleAdapter(); }
}
namespace AnimusForge
{
    internal class PostprocessRuleEntry { public string Tag, Description; }
    internal static class Logger { public static void Log(string category, string text) { } }
    internal static class DuelSettings
    {
        internal static bool Enabled = true;
        internal static bool IsCivilWarFactionsEnabled() => Enabled;
        internal static bool IsCivilWarPlayerKingdomFactionsAllowed() => true;
        internal static CivilWarTuning BuildCivilWarTuning() => new();
    }
    internal static class PlayerKingdomRebellionImmunity { internal static bool ShouldProtectKingdom(Kingdom k) => false; }
    internal static class WorldDiplomacyBehavior { internal static void ApplyExternalPrestigeDelta(string id, int delta, string reason) { } }
    internal partial class CivilWarCampaignBehavior
    {
        internal static void RecordMaterial(Kingdom k, int week, string text) { }
        private static int Week() => 100;
        private static void RecordFiefDenied(Kingdom k, TaleWorlds.CampaignSystem.Election.SettlementClaimantDecision d, TaleWorlds.CampaignSystem.Election.SettlementClaimantDecision.ClanAsDecisionOutcome o) { }
    }
    internal class MyBehavior
    {
        internal static MyBehavior Instance = new();
        internal static int StabilityChanges;
        internal static bool CleanupAllowed = true;
        internal static bool TryAdjustKingdomStabilityForExternal(Kingdom k, int delta, string reason, out int before, out int after) { before = 50; after = before + delta; StabilityChanges += delta; return true; }
        internal static int GetKingdomStabilityValueForExternal(Kingdom k) => 50;
        internal static bool TryDiscontinueLandlessKingdomForExternal(Kingdom k, string reason)
        {
            if (!CleanupAllowed || k.Settlements.Count > 0 || k.Clans.Count > 0) return false;
            k.IsEliminated = true; return true;
        }
        internal static void QueueCivilWarRebellionForExternal(Kingdom k, Clan leader, List<Clan> followers, string id, bool now) { }
        internal static void RecordNpcActionForExternal(params object[] args) { }
        internal void CaptureWorldBulletinCivilWar(string id, string key) { }
    }
    internal static partial class AIConfigHandler
    {
        internal const string ActionPostprocessFallbackMoodTag = "[ACTION:MOOD:neutral]";
        private static Dictionary<string,string> BuildKingdomServiceRuntimeTokens(out Clan player, out Kingdom kingdom, out bool merc, out Kingdom target, out bool same, out int tier, out int mt, out int vt, out int mm, out int vm, out int trust)
        { player = Clan.PlayerClan; kingdom = target = player?.Kingdom; merc = false; same = true; tier = mt = vt = mm = vm = trust = 3; return new(); }
        private static bool IsPlayerKingdomRecruitmentModeActive(Clan c, Kingdom k) => c != null && k?.RulingClan == c;
        private static Clan ResolveConversationTargetClan() => Clan.All.FirstOrDefault(c => c != Clan.PlayerClan);
        private static Hero ResolveConversationTargetHero() => ResolveConversationTargetClan()?.Leader;
        private static string ResolvePlayerKingdomRecruitmentStateKey(params object[] args) => "ruler";
        private static string ResolveRuntimeKingdomServiceStateKeyForPostprocess(params object[] args) => "leave_only";
        private static bool CanInjectKingdomServiceLeaveCurrentPostprocessTag(params object[] args) => false;
        private static List<PostprocessRuleEntry> GetGuardrailRulePostprocessRules(string id) => new();
        private static bool IsPlayerJoinKingdomServicePostprocessTag(string tag) => true;
        private static bool ShouldIncludeKingdomServicePostprocessTag(string state, string tag, bool leave) => true;
    }
}
