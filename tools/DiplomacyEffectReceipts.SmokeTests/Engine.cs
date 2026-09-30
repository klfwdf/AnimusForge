using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

internal enum Fault { None, Before, NoOp, After, Unreadable, BetweenPeaceAndTerms }
internal static class Engine
{
    internal static Fault Fault;
    internal static int Calls, Registrations;
    internal static bool AtWar, Allied, Trading, MainThread = true;
    internal static Kingdom Player = null!, Npc = null!;
    internal static StanceLink Stance = new();
    internal static void Reset(Fault fault, bool war = false, bool allied = false, bool trading = false)
    {
        Fault = fault; Calls = Registrations = 0; MainThread = true;
        AtWar = war; Allied = allied; Trading = trading; Stance = new();
        Kingdom.All.Clear(); Hero.All.Clear();
        var playerClan = new Clan { StringId = "player-clan" };
        var npcClan = new Clan { StringId = "npc-clan" };
        Player = new Kingdom { StringId = "player", RulingClan = playerClan };
        Npc = new Kingdom { StringId = "npc", RulingClan = npcClan };
        playerClan.Kingdom = Player; npcClan.Kingdom = Npc;
        Hero.MainHero = new Hero { StringId = "ruler", Clan = playerClan };
        var speaker = new Hero { StringId = "speaker", Clan = npcClan };
        playerClan.Leader = Hero.MainHero; npcClan.Leader = speaker;
        Clan.PlayerClan = playerClan;
        Hero.All.AddRange(new[] { Hero.MainHero, speaker });
        Kingdom.All.AddRange(new[] { Player, Npc });
    }
    internal static void Apply(Action mutation)
    {
        Calls++;
        if (Fault == Fault.Before) throw new InvalidOperationException("before mutation");
        if (Fault == Fault.NoOp) return;
        mutation();
        if (Fault is Fault.After or Fault.Unreadable) throw new InvalidOperationException("observer after mutation");
    }
    internal static void Read()
    { if (Calls > 0 && Fault == Fault.Unreadable) throw new InvalidOperationException("readback unavailable"); }
}

namespace AnimusForge
{
    internal static class PermanentAllianceGuard
    { internal static void RunAuthorizedBreak(string source, Kingdom first, Kingdom second, Action action) => action(); }
    internal static class MeetingBattleRuntime
    { internal static void RunWithDiplomaticSideEffectsUnlocked(string source, Action action) => action(); }
    internal static class DiplomacyRecentPeaceGuard
    { internal static void RegisterPeace(object first, object second, string source) => Engine.Registrations++; }
    internal static class BannerlordApiCompat
    {
        internal static bool HasTradeAgreement(ITradeAgreementsCampaignBehavior trade, Kingdom first, Kingdom second)
            => TryGetTradeAgreementState(trade, first, second, out bool active) && active;
        internal static bool TryGetTradeAgreementState(ITradeAgreementsCampaignBehavior trade, Kingdom first, Kingdom second, out bool active)
        { active = false; try { Engine.Read(); active = Engine.Trading; return true; } catch { return false; } }
    }
    internal readonly struct AfTributePowerContext { internal int CalculatedTribute => 90; }
    internal static class DiplomacyConversationBridge
    { internal static bool TryBuildTributePowerContext(Kingdom first, Kingdom second, out AfTributePowerContext context) { context = new(); return true; } }
    internal sealed class WorldDiplomacyPeaceTerms
    {
        public string CessionSettlementId = "", CessionFromKingdomId = "", CessionToKingdomId = "";
        public string TributePayerKingdomId = "", TributeReceiverKingdomId = "";
        public int DailyTribute, DurationDays;
    }
}
namespace TaleWorlds.Library
{
    public static class TWParallel { public static bool IsMainThread() => Engine.MainThread; }
    public static class MBMath { public static int ClampInt(int value, int min, int max) => Math.Clamp(value, min, max); }
}
namespace TaleWorlds.CampaignSystem
{
    public sealed class Kingdom
    {
        public static List<Kingdom> All { get; } = new();
        public string StringId { get; set; } = "";
        public string Name => StringId;
        public bool IsEliminated { get; set; }
        public Clan RulingClan { get; set; } = null!;
        public List<Fief> Fiefs { get; } = new();
        public StanceLink GetStanceWith(Kingdom other) { Engine.Read(); return Engine.Stance; }
    }
    public sealed class Fief { public float Prosperity { get; set; } }
    public sealed class StanceLink
    {
        public int Tribute, DailyTributeInstallments;
        public int GetDailyTributeToPay(Kingdom payer) { Engine.Read(); return Tribute; }
    }
    public sealed class Clan
    {
        public static Clan PlayerClan { get; set; } = null!;
        public string StringId { get; set; } = "";
        public Kingdom Kingdom { get; set; } = null!;
        public Hero Leader { get; set; } = null!;
        public bool IsEliminated, IsUnderMercenaryService, IsBanditFaction, IsOutlaw;
    }
    public sealed class Hero
    {
        public static Hero MainHero { get; set; } = null!;
        public static List<Hero> All { get; } = new();
        public string StringId { get; set; } = "";
        public Clan Clan { get; set; } = null!;
        public Kingdom MapFaction => Clan.Kingdom;
        public bool IsDead;
        public static Hero Find(string id) => All.FirstOrDefault(h => h.StringId == id)!;
        public static Hero FindFirst(Func<Hero, bool> predicate) => All.FirstOrDefault(predicate)!;
    }
    public readonly struct CampaignTime
    {
        private readonly int _days;
        private CampaignTime(int days) => _days = days;
        public double ToDays => _days;
        public static CampaignTime Days(int days) => new(days);
    }
    public sealed class TradeModel
    { public CampaignTime GetTradeAgreementDurationInYears(Kingdom first, Kingdom second) => CampaignTime.Days(100); }
    public sealed class Models { public TradeModel TradeAgreementModel { get; } = new(); }
    public sealed class Campaign
    {
        public static Campaign Current { get; } = new();
        public Models Models { get; } = new();
        public T? GetCampaignBehavior<T>() where T : class => new Behaviors() as T;
    }
    public static class FactionManager
    {
        public static bool IsAtWarAgainstFaction(object first, object second) { Engine.Read(); return Engine.AtWar; }
        public static bool IsAtConstantWarAgainstFaction(object first, object second) => false;
    }
}
namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
    public interface IAllianceCampaignBehavior
    {
        bool IsAllyWithKingdom(Kingdom first, Kingdom second);
        void StartAlliance(Kingdom first, Kingdom second);
        void EndAlliance(Kingdom first, Kingdom second);
    }
    public interface ITradeAgreementsCampaignBehavior
    {
        void MakeTradeAgreement(Kingdom first, Kingdom second, CampaignTime duration);
        void EndTradeAgreement(Kingdom first, Kingdom second);
    }
    public sealed class Behaviors : IAllianceCampaignBehavior, ITradeAgreementsCampaignBehavior
    {
        public bool IsAllyWithKingdom(Kingdom first, Kingdom second) { Engine.Read(); return Engine.Allied; }
        public void StartAlliance(Kingdom first, Kingdom second) => Engine.Apply(() => Engine.Allied = true);
        public void EndAlliance(Kingdom first, Kingdom second) => Engine.Apply(() => Engine.Allied = false);
        public void MakeTradeAgreement(Kingdom first, Kingdom second, CampaignTime duration) => Engine.Apply(() => Engine.Trading = true);
        public void EndTradeAgreement(Kingdom first, Kingdom second) => Engine.Apply(() => Engine.Trading = false);
    }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public sealed class Settlement
    {
        public string StringId = "town";
        public string Name => StringId;
        internal Clan Owner = null!;
        public Clan OwnerClan { get { Engine.Read(); return Owner; } }
    }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class MakePeaceAction
    {
        public static void Apply(object first, object second) => Engine.Apply(() => Engine.AtWar = false);
        public static void ApplyByKingdomDecision(Kingdom first, Kingdom second, int tribute, int days) => Engine.Apply(() =>
        {
            Engine.AtWar = false;
            if (Engine.Fault == Fault.BetweenPeaceAndTerms) throw new InvalidOperationException("neutral event before tribute");
            Engine.Stance.Tribute = tribute; Engine.Stance.DailyTributeInstallments = days;
        });
    }
    public static class ChangeOwnerOfSettlementAction
    {
        public static void ApplyByBarter(Hero recipient, Settlements.Settlement settlement)
            => Engine.Apply(() => settlement.Owner = recipient.Clan);
    }
}
