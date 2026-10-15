using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private Dictionary<string, int> _villageAssistance = new Dictionary<string, int>(StringComparer.Ordinal);
    private VillageAssistanceStore _assistanceStore;
    private VillageAssistanceStore Assistance => _assistanceStore ??= new VillageAssistanceStore(_villageAssistance);
    private static VillageDispositionEconomy _foreignEconomy;
    private static readonly Dictionary<string, EquipmentElement> ForeignItems = new Dictionary<string, EquipmentElement>(StringComparer.Ordinal);
    private static readonly VillageDispositionPoliticalLedger ForeignPolitics = new VillageDispositionPoliticalLedger();

    // Native save types only; these continue to own the payout after TAB retires scene state.
    private string _dispositionPayoutVillage = "";
    private string _dispositionPayoutReceipt = "";
    private ItemRoster _dispositionItems;
    private TroopRoster _dispositionRecruits;
    private TroopRoster _dispositionPrisoners;
    private int _dispositionGold;
    private int _dispositionGoldPaid;
    private int _dispositionOfferedSlaves;
    private int _dispositionSlavesBefore;
    private string _dispositionSlaveTroopId = "";
    private bool _dispositionPartyPresented;
    private bool _dispositionPartyClosed;
    private bool _dispositionItemsPresented;
    private bool _dispositionAwaitingExit;
    private int _dispositionActionSequence;
    private DateTime _nextPayoutRetryUtc;

    private static VillageAftermathBehavior EconomyOwner => Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();

    private void RegisterVillageEconomyEvents()
        => CampaignEvents.TickEvent.AddNonSerializedListener(this, OnVillageEconomyTick);

    private void SyncVillageEconomy(IDataStore store)
    {
        store.SyncData("_gcczVillageAssistance_v1", ref _villageAssistance);
        store.SyncData("_gcczVillagePayoutVillage_v1", ref _dispositionPayoutVillage);
        store.SyncData("_gcczVillagePayoutReceipt_v1", ref _dispositionPayoutReceipt);
        store.SyncData("_gcczVillagePayoutItems_v1", ref _dispositionItems);
        store.SyncData("_gcczVillagePayoutRecruits_v1", ref _dispositionRecruits);
        store.SyncData("_gcczVillagePayoutPrisoners_v1", ref _dispositionPrisoners);
        store.SyncData("_gcczVillagePayoutGold_v1", ref _dispositionGold);
        store.SyncData("_gcczVillagePayoutGoldPaid_v1", ref _dispositionGoldPaid);
        store.SyncData("_gcczVillagePayoutSlaves_v1", ref _dispositionOfferedSlaves);
        store.SyncData("_gcczVillagePayoutSlavesBefore_v1", ref _dispositionSlavesBefore);
        store.SyncData("_gcczVillagePayoutSlaveTroop_v1", ref _dispositionSlaveTroopId);
        store.SyncData("_gcczVillagePayoutPartyPresented_v1", ref _dispositionPartyPresented);
        store.SyncData("_gcczVillagePayoutPartyClosed_v1", ref _dispositionPartyClosed);
        store.SyncData("_gcczVillagePayoutItemsPresented_v1", ref _dispositionItemsPresented);
        store.SyncData("_gcczVillagePayoutAwaitingExit_v1", ref _dispositionAwaitingExit);
        store.SyncData("_gcczVillagePayoutActionSequence_v1", ref _dispositionActionSequence);
        _villageAssistance ??= new Dictionary<string, int>(StringComparer.Ordinal);
        _dispositionPayoutVillage ??= ""; _dispositionPayoutReceipt ??= "";
        _dispositionSlaveTroopId ??= "";
        if (_dispositionPayoutVillage.Length > 0)
        {
            _dispositionItems ??= new ItemRoster();
            _dispositionRecruits ??= TroopRoster.CreateDummyTroopRoster();
            _dispositionPrisoners ??= TroopRoster.CreateDummyTroopRoster();
        }
        if (store.IsLoading) _assistanceStore = null;
    }

    private void ClearVillageEconomyForNewGame()
    {
        _villageAssistance.Clear(); _assistanceStore = null;
        ClearPayout();
    }

    private static void ClearForeignEconomyScene()
    {
        _foreignEconomy = null; ForeignItems.Clear(); ForeignPolitics.Clear();
        EconomyOwner?._assistanceStore?.ResetSceneTurns();
    }

    private static bool PrepareForeignEconomy(Settlement village)
    {
        var owner = EconomyOwner;
        if (owner == null || village?.Village == null || owner._dispositionPayoutVillage.Length > 0) return false;
        var goods = new Dictionary<string, int>(StringComparer.Ordinal);
        var livestock = new Dictionary<string, int>(StringComparer.Ordinal);
#if BANNERLORD_1_4_OR_GREATER
        float multiplier = Math.Max(0f, Campaign.Current.Models.RaidModel.GetRaidLootMultiplier(PartyBase.MainParty).ResultNumber);
#else
        float multiplier = 1f; // The 1.3 RaidModel has no multiplier API; preserve that line's native base rule.
#endif
        double hitPoints = Math.Max(0d, Math.Min(1d, village.SettlementHitPoints));
        double hearthLoss = VillageDispositionEconomy.MaximumRaidHearthLoss(village.Village.Hearth, hitPoints);
        ForeignItems.Clear();
        void Add(ItemObject item, int count, EquipmentElement equipment)
        {
            if (item == null || count <= 0 || (item.IsFood && !item.IsAnimal)) return;
            string key = item.StringId + ":" + (equipment.ItemModifier?.StringId ?? "");
            var target = item.IsAnimal ? livestock : goods;
            target[key] = VillageDispositionEconomy.Count((target.TryGetValue(key, out int old) ? (double)old : 0d) + count);
            ForeignItems[key] = equipment;
        }
        foreach (ItemRosterElement stock in village.ItemRoster)
            Add(stock.EquipmentElement.Item, VillageDispositionEconomy.MaximumRaidStockLoot(stock.Amount, hitPoints, multiplier > 0f), stock.EquipmentElement);
        foreach (var production in village.Village.VillageType.Productions)
        {
            ItemObject item = production.Item1;
            if (item == null || (item.IsFood && !item.IsAnimal)) continue; // Food has the user-approved independent household budget.
            int count = VillageDispositionEconomy.MaximumRaidProductionLoot(hearthLoss, production.Item2, multiplier);
            Add(item, count, new EquipmentElement(item));
        }
        // Common raid loot is a single shared draw budget, never the full amount for EACH possible item.
        var common = Campaign.Current.Models.RaidModel.GetCommonLootItemScores()
            .Where(pair => pair.Item1 != null && pair.Item2 > 0f).ToList();
        int commonCount = multiplier > 0f ? VillageDispositionEconomy.Count(hearthLoss) : 0;
        var commonShares = VillageDispositionEconomy.AllocateByWeights(commonCount, common.Select(pair => (double)pair.Item2).ToList());
        bool animalProducer = village.Village.VillageType.Productions.Any(p => p.Item1?.IsAnimal == true);
        for (int i = 0; i < common.Count; i++)
        {
            // Non-producing villages only have their actual raid stock for animals.
            if (!common[i].Item1.IsAnimal || animalProducer) Add(common[i].Item1, commonShares[i], new EquipmentElement(common[i].Item1));
        }
        int basic = (int)Math.Ceiling(village.Village.Hearth / 30f);
        if (MobileParty.MainParty.HasPerk(DefaultPerks.Roguery.InBestLight)) basic += village.Notables.Count;
        _foreignEconomy = new VillageDispositionEconomy(village.Village.Hearth, MBRandom.RandomFloat, MBRandom.RandomFloat, basic, goods, livestock);
        owner.Assistance.Get(village.StringId, InitialVillageAssistance);
        owner.Assistance.ResetSceneTurns();
        owner._dispositionPayoutVillage = village.StringId;
        owner._dispositionPayoutReceipt = Guid.NewGuid().ToString("N");
        owner._dispositionItems = new ItemRoster();
        owner._dispositionRecruits = TroopRoster.CreateDummyTroopRoster();
        owner._dispositionPrisoners = TroopRoster.CreateDummyTroopRoster();
        owner._dispositionAwaitingExit = false;
        ForeignPolitics.Clear();
        GcczDiagnosticLog.Log("VillageBudget", "prepared village=" + village.StringId + " nativeHp=" + hitPoints + " goodsTypes=" + goods.Count + " livestockTypes=" + livestock.Count);
        return true;
    }

    private static int InitialVillageAssistance => Math.Max(0, Math.Min(5, DuelSettings.GetSettings()?.GcczVillageInitialAssistance ?? 0));
    private static int ForeignPersuasionTurn => _activeMission?.Mode == MissionMode.Conversation ? 0
        : Campaign.Current?.GetCampaignBehavior<ShoutBehavior>()?.CurrentPlayerConversationEpoch ?? 0;

    private static string BuildForeignBudgetContext()
    {
        if (_foreignEconomy == null) return VP("本次预算尚未就绪，不可承诺交付。", "No budget is ready; do not promise delivery.");
        var all = _foreignEconomy.Prepare(VillageDispositionActionKind.Plunder);
        bool support = EconomyOwner?.Assistance.CanSpendForSceneTurn(_activeVillageId, InitialVillageAssistance, ForeignPersuasionTurn) == true;
        return VP("【本村剩余预算】钱财/粮食/物资/牲畜：", "[Remaining village budget] money/food/goods/livestock: ")
            + all.Gold + "/" + all.Food + "/" + all.Goods.Values.Sum() + "/" + all.Livestock.Values.Sum()
            + (support ? VP("。已有合作意愿，仍须本人同意。", ". Cooperation exists, but actual consent is still required.")
                : VP("。目前没有可用合作意愿，不能同意自愿援助。", ". No cooperation is available; voluntary aid cannot be agreed."))
            + VP("绝不展示协助点数或公式，威胁屈从不是自愿。", "Never disclose cooperation points or formulas. Coerced submission is not voluntary.");
    }

    private static bool ApplyForeignEconomicAction(VillageDispositionActionSpec spec, int speakerAgentIndex)
    {
        var owner = EconomyOwner; Settlement village = ResolveActiveVillage();
        if (owner == null || village?.Village == null || _foreignEconomy == null) return false;
        if (spec.Kind == VillageDispositionActionKind.VillagerApproval)
        {
            bool gained = owner.Assistance.GainForSceneTurn(village.StringId, InitialVillageAssistance, ForeignPersuasionTurn);
            if (gained) RecordForeignFact(village, VP("玩家通过真诚游说获得认可。", "Sincere persuasion earned the villagers' approval."), "persuasion", false);
            return gained;
        }
        if (spec.Kind == VillageDispositionActionKind.GatherVillagers)
        {
            bool queued = TryGatherDispositionVillagers(speakerAgentIndex);
            if (queued) RecordForeignFact(village, VP("召集村民的请求已受理，尚未完成集合。", "The gathering request was accepted; assembly is not yet complete."), "gather_requested", false);
            return queued;
        }
        if (spec.Kind == VillageDispositionActionKind.StopPlunder)
        {
            StopVillageMassacre();
            InformationManager.DisplayMessage(new InformationMessage(VUi("已停止行动，不另扣关系；既有损失保持。", "Operations stopped, with no extra relation penalty. Existing losses remain."), Color.FromUint(WarningColor)));
            return false;
        }
        var plan = _foreignEconomy.Prepare(spec.Kind);
        bool destructive = VillageGovernanceRules.IsDestructive(spec.Kind);
        if (destructive && DestructiveActions.Contains(spec.Kind)) { StartVillageMassacre(); return true; }
        if (!plan.HasRewards && !destructive)
        {
            TryDisplayFailureMessage(VUi("该项剩余资源不足，本次未执行。", "Insufficient remaining resources; no action taken.")); return false;
        }
        if (spec.OutcomeLevel > 0 && !owner.Assistance.CanSpendForSceneTurn(village.StringId, InitialVillageAssistance, ForeignPersuasionTurn))
        {
            TryDisplayFailureMessage(VUi("村民还不愿自愿协助，请继续游说。", "The villagers are not willing to help voluntarily yet; keep persuading them.")); return false;
        }
        // Construct all native payloads before consuming cooperation or changing any source pool.
        var items = new ItemRoster(); var recruits = TroopRoster.CreateDummyTroopRoster(); var prisoners = TroopRoster.CreateDummyTroopRoster();
        ItemObject food = village.Village.VillageType.PrimaryProduction;
        if (food?.IsFood != true || food.IsAnimal) food = DefaultItems.Grain;
        if (plan.Food > 0 && food == null) return false;
        if (plan.Food > 0) items.AddToCounts(food, plan.Food);
        foreach (var entry in plan.Goods.Concat(plan.Livestock))
            if (entry.Value > 0) items.AddToCounts(ForeignItems[entry.Key], entry.Value);
        CharacterObject basic = village.Culture?.BasicTroop;
        CharacterObject peasant = village.Culture?.Villager;
        if (((plan.BasicRecruits > 0 || plan.ExtraRecruits > 0) && basic == null)
            || (plan.Slaves > 0 && (peasant == null || peasant.IsHero))) return false;
        if (plan.BasicRecruits > 0) recruits.AddToCounts(basic, plan.BasicRecruits);
        if (plan.ExtraRecruits > 0)
        {
            if (spec.Kind == VillageDispositionActionKind.VolunteerFollowers)
            {
                if (peasant == null) return false;
                recruits.AddToCounts(peasant, plan.ExtraRecruits);
            }
            else
            {
                var localSources = village.Notables.Where(hero => hero != null && hero.IsAlive && hero.CurrentSettlement == village)
                    .Select(hero => Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(hero)).Where(troop => troop != null).ToList();
                var weights = localSources.GroupBy(troop => troop).ToList();
                if (weights.Count == 0) recruits.AddToCounts(basic, plan.ExtraRecruits);
                else
                {
                    var shares = VillageDispositionEconomy.AllocateByWeights(plan.ExtraRecruits, weights.Select(group => (double)group.Count()).ToList());
                    for (int i = 0; i < weights.Count; i++)
                    {
                        if (shares[i] > 0) recruits.AddToCounts(weights[i].Key, shares[i]);
                    }
                }
            }
        }
        if (plan.Slaves > 0) prisoners.AddToCounts(peasant, plan.Slaves);
        if (spec.OutcomeLevel > 0 && !owner.Assistance.SpendForSceneTurn(village.StringId, InitialVillageAssistance, ForeignPersuasionTurn)) return false;
        if (plan.HasRewards && !_foreignEconomy.Commit(plan))
        {
            if (spec.OutcomeLevel > 0) owner.Assistance.Gain(village.StringId, InitialVillageAssistance);
            return false;
        }
        owner._dispositionGold += plan.Gold;
        foreach (var entry in items) owner._dispositionItems.AddToCounts(entry.EquipmentElement, entry.Amount);
        foreach (var entry in recruits.GetTroopRoster()) owner._dispositionRecruits.AddToCounts(entry.Character, entry.Number);
        foreach (var entry in prisoners.GetTroopRoster()) owner._dispositionPrisoners.AddToCounts(entry.Character, entry.Number);
        owner._dispositionOfferedSlaves += plan.Slaves;
        if (plan.Slaves > 0) owner._dispositionSlaveTroopId = peasant.StringId;
        // Actual stock is removed once; production/household allotments are not taken from military rewards.
        foreach (var entry in items)
        {
            int stock = village.ItemRoster.Where(element => element.EquipmentElement.IsEqualTo(entry.EquipmentElement)).Sum(element => element.Amount);
            if (stock > 0) village.ItemRoster.AddToCounts(entry.EquipmentElement, -Math.Min(stock, entry.Amount));
        }
        if (spec.Kind == VillageDispositionActionKind.ForcedRecruitment || spec.Kind == VillageDispositionActionKind.VolunteerFollowers)
        {
            if (plan.BasicRecruits > 0) village.Village.Hearth = Math.Max(0f, village.Village.Hearth - plan.BasicRecruits / 2f);
            if (spec.Kind == VillageDispositionActionKind.ForcedRecruitment)
                IncreaseSettlementHealthAction.Apply(village, -village.SettlementHitPoints * 0.8f);
        }
        if (destructive)
        {
            if (spec.Kind == VillageDispositionActionKind.Destroy) _foreignEconomy.DestroyRemainingResources();
            DestructiveActions.Add(spec.Kind);
            village.Village.Hearth = VillageDispositionEconomy.HearthAfterDestruction(village.Village.Hearth, _foreignEconomy.Households,
                spec.Kind == VillageDispositionActionKind.Destroy);
            StartVillageMassacre();
        }
        if (VillageGovernanceRules.IsDisposition(spec.Kind)) Departure.MarkDisposition();
        ApplyForeignRelations(village, spec);
        if (spec.OutcomeLevel < 0) QueueForeignQuestioning();
        string actionName = GcczTownPromptResourceProvider.UseEnglishPrompt ? spec.Kind.ToString() : spec.Name;
        string summary = village.Name + " / " + actionName + VP("：已收集、待玩家选择，钱财/物资/入伍者/被抓农民=", ": collected, pending player selection; money/items/recruits/captured peasants=")
            + plan.Gold + "/" + items.Sum(e => e.Amount) + "/" + recruits.TotalManCount + "/" + prisoners.TotalManCount;
        RecordForeignFact(village, summary, "collection_" + spec.Kind, spec.OutcomeLevel < 0);
        GcczTownRecentMemoryBridge.VillageAction(village, spec.Kind, owner._dispositionPayoutReceipt + ":" + owner._dispositionActionSequence);
        InformationManager.DisplayMessage(new InformationMessage(VUi("已收集处置所得，TAB离场后选择人员、俘虏和物资。", "Proceeds collected. Leave with TAB to select troops, prisoners and supplies."), Color.FromUint(SuccessColor)));
        return true;
    }

    private static void ApplyForeignRelations(Settlement village, VillageDispositionActionSpec spec)
    {
        void Apply(Hero hero, int requested)
        {
            if (hero == null || !hero.IsAlive || hero == Hero.MainHero || requested == 0) return;
            int delta = requested;
            bool merciful = hero.GetTraitLevel(DefaultTraits.Mercy) > 0;
            if (requested < 0)
            {
                delta = ForeignPolitics.AdditionalPenalty(hero.StringId, requested, merciful);
            }
            if (delta == 0) return;
            int before = (int)hero.GetRelation(Hero.MainHero);
            try
            {
                ChangeRelationAction.ApplyPlayerRelation(hero, delta, true, true);
                if (requested < 0) ForeignPolitics.CommitPenalty(hero.StringId, requested, merciful);
            }
            catch (Exception ex)
            {
                if (requested < 0 && hero.GetRelation(Hero.MainHero) <= Math.Max(-100, before + delta))
                    ForeignPolitics.CommitPenalty(hero.StringId, requested, merciful);
                Logger.Log("VillageAftermath", "Relation callback failed; resources were not awarded twice: hero=" + hero.StringId + " error=" + ex.Message);
                TryDisplayFailureMessage(VUi("部分关系结算异常；物资保留，不重复发放。", "A relationship callback failed. Collected supplies are retained, not awarded twice."));
            }
        }
        Apply(village.Village.Bound?.OwnerClan?.Leader, !_hostileDispositionActive && spec.OutcomeLevel < 0
            ? VillageGovernanceRules.OwnerPenalty(spec.Kind) : spec.OwnerPenalty);
        if (!_hostileDispositionActive && spec.OutcomeLevel < 0)
            Apply(Clan.PlayerClan?.Kingdom?.Leader, VillageGovernanceRules.RulerPenalty(spec.Kind));
        foreach (Hero notable in village.Notables.Where(hero => hero?.IsAlive == true))
            Apply(notable, spec.OutcomeLevel > 0 ? spec.NotableReward : spec.NotablePenalty);
    }

    private static void RecordForeignFact(Settlement village, string text, string action, bool major)
    {
        var owner = EconomyOwner;
        if (owner == null || village == null) return;
        string suffix = action == "payout_closed" || action == "slaves_received" ? action : (++owner._dispositionActionSequence) + "." + action;
        string key = "gccz.village." + owner._dispositionPayoutReceipt + "." + suffix;
        Hero landOwner = village.Village.Bound?.OwnerClan?.Leader;
        if (landOwner != null && landOwner != Hero.MainHero && (action.StartsWith("collection_", StringComparison.Ordinal) || action.StartsWith("aid_", StringComparison.Ordinal) || action == "mercy"))
            MyBehavior.AppendExternalPlayerFact(landOwner, Hero.MainHero.Name + " / " + village.Name + ": " + text);
        MyBehavior.RecordPlayerActionForExternal(text, key, "gccz_village_disposition", major, targetHero: village.Village.Bound?.OwnerClan?.Leader, settlement: village, locationText: village.Name.ToString());
        if (!_hostileDispositionActive && action == "collection_Destroy")
        {
            Hero ruler = Clan.PlayerClan?.Kingdom?.Leader;
            if (ruler != null && ruler != Hero.MainHero && ruler != village.Village.Bound?.OwnerClan?.Leader)
                { MyBehavior.AppendExternalPlayerFact(ruler, text);
                MyBehavior.RecordPlayerActionForExternal(text, key + ".ruler", "gccz_village_disposition", true, targetHero: ruler, settlement: village); }
        }
    }

    internal static bool HasPendingVillagePayoutForExternal(string villageId)
        => EconomyOwner?._dispositionAwaitingExit == true && EconomyOwner._dispositionPayoutVillage == villageId;

    internal static void PumpPendingPayoutForExternal(string villageId)
    {
        var owner = EconomyOwner;
        if (owner?._dispositionAwaitingExit == true && owner._dispositionPayoutVillage == villageId)
            owner.OnVillageEconomyTick(0f);
    }

    private void OnVillageEconomyTick(float dt)
    {
        if (!_dispositionAwaitingExit || Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is not MapState
            || InformationManager.IsAnyInquiryActive() || DateTime.UtcNow < _nextPayoutRetryUtc) return;
        Settlement village = Settlement.Find(_dispositionPayoutVillage);
        if (village == null || MobileParty.MainParty == null) return;
        try
        {
            CharacterObject slaveTroop = string.IsNullOrWhiteSpace(_dispositionSlaveTroopId) ? null
                : MBObjectManager.Instance.GetObject<CharacterObject>(_dispositionSlaveTroopId);
            if (_dispositionOfferedSlaves > 0 && slaveTroop == null) throw new InvalidOperationException("Saved village slave troop is unavailable.");
            if (_dispositionGoldPaid < _dispositionGold)
            {
                int before = Hero.MainHero.Gold;
                try { GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _dispositionGold - _dispositionGoldPaid, true); }
                finally { _dispositionGoldPaid += Math.Max(0, Hero.MainHero.Gold - before); }
            }
            if (!_dispositionPartyPresented && (_dispositionRecruits.TotalManCount > 0 || _dispositionPrisoners.TotalManCount > 0))
            {
                _dispositionSlavesBefore = slaveTroop != null ? MobileParty.MainParty.PrisonRoster.GetTroopCount(slaveTroop) : 0;
                try { PartyScreenHelper.OpenScreenAsLoot(_dispositionRecruits, _dispositionPrisoners, village.Name, _dispositionRecruits.TotalManCount + _dispositionPrisoners.TotalManCount); }
                finally { if (Game.Current.GameStateManager.ActiveState is PartyState) _dispositionPartyPresented = true; }
                return;
            }
            if (!_dispositionPartyClosed)
            {
                int acceptedSlaves = _dispositionPartyPresented && slaveTroop != null ? Math.Min(_dispositionOfferedSlaves,
                    Math.Max(0, MobileParty.MainParty.PrisonRoster.GetTroopCount(slaveTroop) - _dispositionSlavesBefore)) : 0;
                village.Village.Hearth = VillageDispositionEconomy.HearthAfterSlaves(village.Village.Hearth, acceptedSlaves);
                _dispositionPartyClosed = true;
                if (_dispositionOfferedSlaves > 0) RecordForeignFact(village, VP("最终接收奴隶/减少户数：", "Slaves actually accepted / hearth lost: ") + acceptedSlaves + "/" + acceptedSlaves * 0.5f, "slaves_received", true);
            }
            if (!_dispositionItemsPresented && _dispositionItems.Count > 0)
            {
                try { InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { { PartyBase.MainParty, _dispositionItems } }); }
                finally { if (Game.Current.GameStateManager.ActiveState is InventoryState) _dispositionItemsPresented = true; }
                return;
            }
            if (_dispositionGoldPaid < _dispositionGold) return;
            RecordForeignFact(village, VP("结算结束，已付第纳尔：", "Settlement complete, denars paid: ") + _dispositionGoldPaid + VP("；人员和物资选择已关闭，不把弃置内容记为所得。", "; selection screens closed. Discarded items and rejected troops are not recorded as received."), "payout_closed", false);
            ClearPayout();
        }
        catch (Exception ex)
        {
            _nextPayoutRetryUtc = DateTime.UtcNow.AddSeconds(1);
            Logger.Log("VillageAftermath", "Payout pending; will resume without repeating completed steps: " + ex.Message);
        }
    }

    private void ClearPayout()
    {
        _dispositionPayoutVillage = ""; _dispositionPayoutReceipt = "";
        _dispositionItems = null; _dispositionRecruits = null; _dispositionPrisoners = null;
        _dispositionGold = 0; _dispositionGoldPaid = 0; _dispositionOfferedSlaves = 0; _dispositionSlavesBefore = 0;
        _dispositionSlaveTroopId = "";
        _dispositionPartyPresented = false; _dispositionPartyClosed = false; _dispositionItemsPresented = false; _dispositionAwaitingExit = false;
    }
}
