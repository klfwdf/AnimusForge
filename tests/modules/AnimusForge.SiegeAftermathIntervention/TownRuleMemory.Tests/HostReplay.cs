using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

// The production host adapter/event listeners run against value-only Bannerlord fixtures.
// Full API compatibility is validated separately by both real game-reference builds.
static class HostReplay
{
    internal static void Run()
    {
        GcczTownRuleMemoryRuntimeBridge.ClearForNewGame();
        CampaignTime.Day = 100;
        var oldOwner = new Hero { StringId = "old", Name = "Old owner" };
        var newOwner = new Hero { StringId = "new", Name = "New owner" };
        var oldClan = new Clan { Leader = oldOwner }; oldOwner.Clan = oldClan;
        var newClan = new Clan { Leader = newOwner }; newOwner.Clan = newClan;
        var town = new Settlement { StringId = "host-town", Name = "Host Town", OwnerClan = oldClan,
            IsTown = true, Culture = new CultureObject { StringId = "culture", Name = "Culture" } };
        Settlement.All[town.StringId] = town;
        GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        var host = new SiegeAiInterventionBehavior(); host.RegisterTestEvents();
        CampaignTime.Day = 110; town.OwnerClan = newClan; newClan.Settlements.Add(town);
        var recipient = new Hero { StringId = "clan-member", Name = "Grant recipient", Clan = newClan };
        CampaignEvents.OnSettlementOwnerChangedEvent.Raise(town, false, recipient, oldOwner, newOwner,
            TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.Default);
        CampaignTime.Day = 130;
        var record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.RuleStartDay == 110, "actual owner listener records event day before future reads");
        Program.Check(record.RulerId == newOwner.StringId, "grant recipient resolves to actual owning clan leader");
        Program.Check(record.CurrentRule.Evolution.Facts.Single().Text.Contains("Old owner"), "owner transition is an attributed fact");
        var heir = new Hero { StringId = "heir", Name = "Heir", Clan = newClan }; newClan.Leader = heir;
        CampaignEvents.OnClanLeaderChangedEvent.Raise(newOwner, heir);
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.RulerId == "heir" && record.RuleStartDay == 130, "clan inheritance observes only owned fiefs");
        var party = new TaleWorlds.CampaignSystem.Party.MobileParty { LeaderHero = heir };
        CampaignEvents.OnSiegeAftermathAppliedEvent.Raise(party, town,
            TaleWorlds.CampaignSystem.Actions.SiegeAftermathAction.SiegeAftermath.Pillage, oldClan, new());
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.CurrentRule.Evolution.Facts.Last().Text.Contains("劫掠"), "native aftermath callback publishes committed outcome");
        int factCount = record.CurrentRule.Evolution.Facts.Count;
        SiegeAiInterventionBehavior.Pending(town, true);
        CampaignEvents.OnSiegeAftermathAppliedEvent.Raise(party, town,
            TaleWorlds.CampaignSystem.Actions.SiegeAftermathAction.SiegeAftermath.ShowMercy, oldClan, new());
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.CurrentRule.Evolution.Facts.Count == factCount, "custom flow suppresses internal native mercy fact");
        SiegeAiInterventionBehavior.Complete(town, party);
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.CurrentRule.Evolution.Facts.Last().Text.Contains("平民死亡7"), "completed custom callback supplies real counters");
        town.Culture = new CultureObject { StringId = "new-culture", Name = "New culture" };
        GcczTownRuleMemoryRuntimeBridge.RefreshAfterRuntimeTransition(town, null, true, "test");
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.CurrentRule.Evolution.Facts.Last().Text.Contains("New culture"), "confirmed culture change supplies a fact");
        var captured = record;
        town.Culture = new CultureObject { StringId = "newer", Name = "Newer culture" };
        var accept = typeof(GcczTownRuleMemoryRuntimeBridge).GetMethod("TryStoreGeneratedNarrative", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Program.Check(!(bool)accept.Invoke(null, new object[] { captured, 130, Program.Narrative }), "host completion rechecks live culture before accepting snapshot");
        GcczTownRuleMemoryRuntimeBridge.TrySetManualNarrative(town, "heir", 130, "Protected manual");
        var saved = new FixtureDataStore { IsSaving = true };
        GcczTownRuleMemoryRuntimeBridge.SyncData(saved);
        var payload = (Dictionary<string,string>)saved.Values["_gcczTownRuleMemoryRecordsBySettlement_v1"];
        Program.Check(payload[town.StringId].StartsWith("v3|"), "same save key writes v3 primitive payload");
        GcczTownRuleMemoryRuntimeBridge.ClearForNewGame();
        saved.IsSaving = false; saved.IsLoading = true;
        GcczTownRuleMemoryRuntimeBridge.SyncData(saved);
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.CurrentRule.Narrative == "Protected manual" && record.CurrentRule.NarrativeIsManual
            && record.RuleStartDay == 130 && record.CurrentRule.Evolution.Facts.Count > 0, "host save/load preserves manual text tenure and facts");
        for (int tenure = 0; tenure < 3; tenure++)
        {
            Hero before = newClan.Leader;
            var next = new Hero { StringId = "large-" + tenure, Name = "Next", Clan = newClan };
            newClan.Leader = next;
            CampaignEvents.OnClanLeaderChangedEvent.Raise(before, next);
            for (int i = 0; i < 12; i++) GcczTownRuleMemoryRuntimeBridge.RecordConfirmedEvent(town,
                "large-" + tenure + "-" + i, new string('镇', 480));
        }
        var largeSave = new FixtureDataStore { IsSaving = true };
        GcczTownRuleMemoryRuntimeBridge.SyncData(largeSave);
        var chunks = (Dictionary<string,string>)largeSave.Values["_gcczTownRuleMemoryRecordsBySettlement_v1"];
        Program.Check(chunks.Keys.Any(k => k.StartsWith("__af_chunkcount__:"))
            && chunks.Values.All(v => System.Text.Encoding.UTF8.GetByteCount(v) <= 12000), "full Chinese event histories use real bounded save chunks");
        GcczTownRuleMemoryRuntimeBridge.ClearForNewGame();
        largeSave.IsSaving = false; largeSave.IsLoading = true;
        GcczTownRuleMemoryRuntimeBridge.SyncData(largeSave);
        record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(town);
        Program.Check(record.RulerMemories.Count == 3 && record.RulerMemories.All(r => r.Evolution.Facts.Count == 12), "full multi-tenure chunked data restores without loss");
        GcczTownRuleMemoryRuntimeBridge.RecordConfirmedEvent(new Settlement { IsTown = false, StringId = "village" }, "x", "fact");
        Program.Check(GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(new Settlement { IsTown = false }) == null, "village/castle remain outside town memory");
        GcczTownRuleMemoryRuntimeBridge.ClearForNewGame();
    }
}

sealed class FixtureDataStore : IDataStore
{
    public bool IsSaving { get; set; }
    public bool IsLoading { get; set; }
    public readonly Dictionary<string,object> Values = new();
    public bool SyncData<T>(string key, ref T value)
    { if (IsSaving) Values[key] = value; else if (Values.TryGetValue(key, out var saved)) value = (T)saved; return true; }
}

namespace AnimusForge
{
    internal static class MyBehavior
    { internal static void GetNpcPersonaForExternal(Hero hero, out string persona, out string background) { persona = "kind"; background = ""; } }
    internal static class GcczTownRuleMemorySpeakerResolver
    {
        internal static Settlement ResolveCurrentTownScene() => null;
        internal static TaleWorlds.Core.CharacterObject ResolveTargetCharacter(TaleWorlds.Core.CharacterObject character, int index) => character;
        internal static bool IsEligible(Settlement settlement, Hero hero, TaleWorlds.Core.CharacterObject character) => false;
    }
    public partial class SiegeAiInterventionBehavior
    {
        private static bool _hasPendingAftermath;
        private static Settlement _activeSettlement;
        private static int _lastLootValue = 10, _lastMarketGoldLoot = 1, _lastCivilianGoldLoot = 2,
            _lastKilledCivilianUnits = 7, _lastKilledNotables = 3, _appliedSharedCivilianReliefGold = 0, _appliedSharedCivilianReliefFoodUnits = 0;
        internal void RegisterTestEvents() => RegisterTownRuleMemoryEvents();
        internal static void Pending(Settlement town, bool pending) { _activeSettlement = town; _hasPendingAftermath = pending; }
        internal static void Complete(Settlement town, TaleWorlds.CampaignSystem.Party.MobileParty party)
        { _hasPendingAftermath = false; RecordCompletedTownMemory(town, party, TaleWorlds.CampaignSystem.Actions.SiegeAftermathAction.SiegeAftermath.ShowMercy); }
    }
}
namespace TaleWorlds.Core { public class CharacterObject { public Hero HeroObject { get; set; } } }
namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { bool IsSaving { get; } bool IsLoading { get; } bool SyncData<T>(string key, ref T value); }
    public class Hero { public static Hero MainHero; public string StringId, Name; public Clan Clan; public int GetTraitLevel(int trait) => 0; }
    public class Clan { public Hero Leader; public List<Settlement> Settlements = new(); }
    public class CultureObject { public string StringId, Name; }
    public struct CampaignTime { public static int Day; public static CampaignTime Now => new(); public double ToDays => Day; public double ToSeconds => Day * 86400; }
    public static class CampaignEvents
    {
        public static readonly OwnerEvent OnSettlementOwnerChangedEvent = new();
        public static readonly LeaderEvent OnClanLeaderChangedEvent = new();
        public static readonly AftermathEvent OnSiegeAftermathAppliedEvent = new();
    }
    public class OwnerEvent
    {
        private Action<Settlement,bool,Hero,Hero,Hero,Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail> _listener;
        public void AddNonSerializedListener(object owner, Action<Settlement,bool,Hero,Hero,Hero,Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail> callback) => _listener += callback;
        public void Raise(Settlement town, bool claim, Hero next, Hero old, Hero capturer, Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail) => _listener?.Invoke(town,claim,next,old,capturer,detail);
    }
    public class LeaderEvent
    {
        private Action<Hero,Hero> _listener;
        public void AddNonSerializedListener(object owner, Action<Hero,Hero> callback) => _listener += callback;
        public void Raise(Hero old, Hero next) => _listener?.Invoke(old,next);
    }
    public class AftermathEvent
    {
        private Action<Party.MobileParty,Settlement,Actions.SiegeAftermathAction.SiegeAftermath,Clan,Dictionary<Party.MobileParty,float>> _listener;
        public void AddNonSerializedListener(object owner, Action<Party.MobileParty,Settlement,Actions.SiegeAftermathAction.SiegeAftermath,Clan,Dictionary<Party.MobileParty,float>> callback) => _listener += callback;
        public void Raise(Party.MobileParty party, Settlement town, Actions.SiegeAftermathAction.SiegeAftermath outcome, Clan previous, Dictionary<Party.MobileParty,float> contributions) => _listener?.Invoke(party,town,outcome,previous,contributions);
    }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Settlement
    {
        public static readonly Dictionary<string,Settlement> All = new();
        public static Settlement Find(string id) => All.TryGetValue(id,out var town) ? town : null;
        public string StringId, Name; public bool IsTown; public Clan OwnerClan; public CultureObject Culture;
    }
}
namespace TaleWorlds.CampaignSystem.Party { public class MobileParty { public Hero LeaderHero; } }
namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{ public static class DefaultTraits { public static int Mercy=0,Valor=1,Honor=2,Generosity=3,Calculating=4; } }
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class ChangeOwnerOfSettlementAction { public enum ChangeOwnerOfSettlementDetail { Default } }
    public static class SiegeAftermathAction { public enum SiegeAftermath { ShowMercy, Pillage, Devastate } }
}
