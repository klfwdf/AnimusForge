namespace TaleWorlds.Core
{
    public class ItemObject { public string Id; public ItemObject(string id) { Id = id; } }
    public struct EquipmentElement { public ItemObject Item; public EquipmentElement(ItemObject item) { Item = item; } }
    public class ItemRoster
    {
        private readonly Dictionary<ItemObject, int> _items = new();
        public int Count => _items.Count;
        public int TotalCount => _items.Values.Sum();
        public void AddToCounts(ItemObject item, int count) => _items[item] = _items.GetValueOrDefault(item) + count;
        public void AddToCounts(EquipmentElement item, int count) => AddToCounts(item.Item, count);
    }
    public class Game
    {
        public static Game Current = new();
        public StateManager GameStateManager = new();
    }
    public class StateManager { public object ActiveState = new TaleWorlds.CampaignSystem.GameState.MapState(); }
}
namespace TaleWorlds.CampaignSystem
{
    public class Hero { public static Hero MainHero = new(); public int Gold; }
    public class CharacterObject { public bool IsHero; }
    public class Campaign { public static Campaign Current = new(); public Models Models = new(); }
    public class Models { public RewardModel BattleRewardModel = new(); public RaidModel RaidModel = new(); }
    public class RaidModel { public int GoldRewardForEachLostHearth => 4; }
    public class RewardModel
    {
        private readonly TaleWorlds.Core.ItemObject _equipment = new("gear");
        public bool CanTroopBeTakenPrisoner(CharacterObject troop) => true;
        public float GetExpectedLootedItemValueFromCasualty(Hero hero, CharacterObject troop) => 10f;
        public TaleWorlds.Core.EquipmentElement GetLootedItemFromTroop(CharacterObject troop, float value) => new(_equipment);
    }
}
namespace TaleWorlds.CampaignSystem.GameState { public class MapState { } public class PartyState { } public class InventoryState { } }
namespace TaleWorlds.CampaignSystem.Party
{
    public class PartyBase { public static PartyBase MainParty = new(); }
    public class MobileParty { public static MobileParty MainParty = new(); }
}
namespace TaleWorlds.CampaignSystem.Roster
{
    public class TroopRoster
    {
        public int TotalManCount;
        public static TroopRoster CreateDummyTroopRoster() => new();
        public void AddToCounts(TaleWorlds.CampaignSystem.CharacterObject troop, int count) => TotalManCount += count;
    }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    public class Settlement { public string StringId = "village"; public string Name = "Village"; public Village Village = new(); public float SettlementHitPoints = 1f; }
    public class Village { public float Hearth = 400f; public VillageType VillageType = new(); }
    public class VillageType { public List<Tuple<TaleWorlds.Core.ItemObject, float>> Productions = new(); }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class GiveGoldAction
    {
        public static bool ThrowAfterPayment;
        public static void ApplyBetweenCharacters(TaleWorlds.CampaignSystem.Hero source, TaleWorlds.CampaignSystem.Hero target, int amount)
        {
            target.Gold += amount;
            if (ThrowAfterPayment) throw new InvalidOperationException("notification failure");
        }
    }
    public static class IncreaseSettlementHealthAction
    {
        public static void Apply(TaleWorlds.CampaignSystem.Settlements.Settlement village, float delta) => village.SettlementHitPoints += delta;
    }
}
namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
    public static class SkillLevelingManager
    {
        public static void OnForceSupplies(TaleWorlds.CampaignSystem.Party.MobileParty party, TaleWorlds.Core.ItemRoster items, bool attacked) { }
    }
}
namespace TaleWorlds.CampaignSystem.MapEvents { public class MapEvent { public static MapEvent PlayerMapEvent; } }
namespace TaleWorlds.MountAndBlade
{
    public enum AgentState { Active, Killed, Unconscious }
    public class Agent { public int Index; public object Character; public AgentState State; }
    public class Mission { public static Mission Current; }
}
namespace TaleWorlds.Library { public static class InformationManager { public static bool IsAnyInquiryActive() => false; } }
namespace AnimusForge { public static class SettlementEntryTroopSelectionLog { public static void Log(string text) { } } }
namespace Helpers
{
    public static class ScreenProbe
    {
        public static int PartyOpens, InventoryOpens;
        public static bool FailBeforePush, FailAfterPush;
        public static void Reset() { PartyOpens = InventoryOpens = 0; FailBeforePush = FailAfterPush = false; }
        public static void Push(object state)
        {
            if (FailBeforePush) throw new InvalidOperationException("before push");
            TaleWorlds.Core.Game.Current.GameStateManager.ActiveState = state;
            if (state is TaleWorlds.CampaignSystem.GameState.PartyState) PartyOpens++; else InventoryOpens++;
            if (FailAfterPush) throw new InvalidOperationException("after push");
        }
    }
    public static class PartyScreenHelper
    {
        public static void OpenScreenAsLoot(TaleWorlds.CampaignSystem.Roster.TroopRoster members, TaleWorlds.CampaignSystem.Roster.TroopRoster prisoners, string name, int limit)
            => ScreenProbe.Push(new TaleWorlds.CampaignSystem.GameState.PartyState());
    }
    public static class InventoryScreenHelper
    {
        public static void OpenScreenAsLoot(Dictionary<TaleWorlds.CampaignSystem.Party.PartyBase, TaleWorlds.Core.ItemRoster> items)
            => ScreenProbe.Push(new TaleWorlds.CampaignSystem.GameState.InventoryState());
    }
}
