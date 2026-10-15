using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    // Called only by an admitted, direct friendly-village reply. No Tick inventory scans.
    private static bool OpenVillageGift(FriendlyVillageActionInvocation invocation)
    {
        var owner = EconomyOwner; var village = ResolveActiveVillage(); var mission = _activeMission;
        var party = MobileParty.MainParty; var hero = Hero.MainHero;
        if (_giftInquiryActive || owner == null || !IsActive() || !CanEnterFriendlyVillage(village)
            || party?.ItemRoster == null || village.ItemRoster == null || hero == null) return false;
        int kind = invocation.Action.Gift, minimum = VillageAidRules.Minimum(kind);
        int amount = invocation.Amount ?? minimum;
        if (amount < minimum)
        { TryDisplayFailureMessage(VUi("本次援助低于最低数量：", "This delivery is below the minimum: ") + minimum); return false; }
        var roster = party.ItemRoster; var market = village.ItemRoster;
        var transfer = new List<KeyValuePair<EquipmentElement, int>>();
        if (kind == 0)
        {
            if (hero.Gold < amount) { TryDisplayFailureMessage(VUi("金钱不足，未完成援助。", "Not enough money; no aid delivered.")); return false; }
        }
        else
        {
            var stock = roster.Where(e => e.Amount > 0 && IsVillageAidItem(e.EquipmentElement.Item, kind))
                .Select(e => new VillageAidStock<EquipmentElement> {
                    Item = e.EquipmentElement, Id = e.EquipmentElement.Item.StringId, Name = e.EquipmentElement.Item.Name.ToString(),
                    Count = e.Amount, Price = e.EquipmentElement.ItemValue,
                    HorseOrPackAnimal = e.EquipmentElement.Item.HasHorseComponent && !e.EquipmentElement.Item.HorseComponent.IsLiveStock,
                    RandomOrder = kind == 3 ? MBRandom.RandomFloat : 0f }).ToList();
            var failure = VillageAidRules.Plan(kind, amount, invocation.Item, stock, out transfer);
            if (failure != VillageAidPlanFailure.None)
            {
                TryDisplayFailureMessage(failure == VillageAidPlanFailure.UnknownOrAmbiguousItem
                    ? VUi("背包里找不到唯一匹配的指定物品，请说明准确名称；未扣物品。", "No unique matching item in your inventory. Specify its exact name; nothing was taken.")
                    : VUi("符合要求的物品不足，未扣物品，也不会用其他物品替代。", "Insufficient matching supplies; nothing was taken or substituted."));
                return false;
            }
        }
        string itemList = string.Join("\n", transfer.Select(p => p.Key.GetModifiedItemName() + " × " + p.Value));
        string summary = kind == 0 ? amount + VUi(" 第纳尔", " denars") : itemList;
        long sequence = ++_giftInquirySequence;
        _giftInquiryActive = true;
        bool OwnsInquiry() => _giftInquiryActive && sequence == _giftInquirySequence;
        bool Valid() => OwnsInquiry() && ReferenceEquals(owner, EconomyOwner) && ReferenceEquals(mission, _activeMission)
            && IsActive() && ResolveActiveVillage() == village && CanEnterFriendlyVillage(village)
            && ReferenceEquals(party, MobileParty.MainParty) && ReferenceEquals(hero, Hero.MainHero)
            && ReferenceEquals(roster, party.ItemRoster) && ReferenceEquals(market, village.ItemRoster);
        void Complete()
        {
            if (!Valid()) { if (OwnsInquiry()) _giftInquiryActive = false; return; }
            _giftInquiryActive = false; // Consume before any native events can re-enter this callback.
            if (kind == 0)
            {
                if (hero.Gold < amount) { TryDisplayFailureMessage(VUi("金钱不足，未完成援助。", "Not enough money; no aid delivered.")); return; }
                GiveGoldAction.ApplyBetweenCharacters(hero, null, amount, true);
            }
            else if (!TransferVillageAid(roster, market, transfer))
            {
                TryDisplayFailureMessage(VUi("交付未完成，请检查背包；没有获得援助奖励。", "Delivery did not complete. Check your inventory; no aid rewards were granted."));
                return;
            }
            owner.RecordVillageGift(village, kind, amount, itemList);
            InformationManager.DisplayMessage(new InformationMessage(VUi("援助已交付，要人关系增加；可以继续援助。", "Aid delivered and notable relations improved. You may donate again.")));
        }
        try
        {
            InformationManager.ShowInquiry(new InquiryData(VUi("确认援助", "Confirm village aid"),
                VUi("向村庄交付以下财物？\n", "Deliver the following to the village?\n") + summary
                    + (kind == 0 ? "" : VUi("\n物品进入村庄市场，库存尚在时可按市场价格买回。", "\nItems enter the village market and can be bought back at market prices while stocked.")),
                true, true, VUi("交付", "Deliver"), VUi("取消", "Cancel"), Complete,
                () => { if (OwnsInquiry()) _giftInquiryActive = false; }), true);
        }
        catch { if (OwnsInquiry()) _giftInquiryActive = false; throw; }
        return true;
    }

    private static bool IsVillageAidItem(ItemObject item, int kind)
        => item != null && (kind == 1 ? item.IsFood && !item.HasHorseComponent
            : kind == 3 ? item.HasHorseComponent : kind == 2 && item.IsTradeGood && !item.IsFood && !item.HasHorseComponent);

    private sealed class AidElementComparer : IEqualityComparer<EquipmentElement>
    {
        internal static readonly AidElementComparer Instance = new AidElementComparer();
        public bool Equals(EquipmentElement a, EquipmentElement b) => a.IsEqualTo(b);
        public int GetHashCode(EquipmentElement e) => (e.Item?.GetHashCode() ?? 0) ^ (e.ItemModifier?.GetHashCode() ?? 0);
    }

    private static int AidItemCount(ItemRoster roster, EquipmentElement item)
        => roster.Where(e => e.EquipmentElement.IsEqualTo(item)).Sum(e => Math.Max(0, e.Amount));

    private static bool TransferVillageAid(ItemRoster source, ItemRoster market, List<KeyValuePair<EquipmentElement, int>> transfer)
    {
        // Snapshot each roster once; native EquipmentElement identity includes item modifiers.
        var sourceCounts = source.ToDictionary(e => e.EquipmentElement, e => Math.Max(0, e.Amount), AidElementComparer.Instance);
        var marketCounts = market.ToDictionary(e => e.EquipmentElement, e => Math.Max(0, e.Amount), AidElementComparer.Instance);
        var sourceBefore = transfer.Select(p => sourceCounts.TryGetValue(p.Key, out int n) ? n : 0).ToArray();
        var marketBefore = transfer.Select(p => marketCounts.TryGetValue(p.Key, out int n) ? n : 0).ToArray();
        for (int i = 0; i < transfer.Count; i++)
            if (sourceBefore[i] < transfer[i].Value || (long)marketBefore[i] + transfer[i].Value > int.MaxValue) return false;
        try
        {
            foreach (var pair in transfer) { source.AddToCounts(pair.Key, -pair.Value); market.AddToCounts(pair.Key, pair.Value); }
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("VillageAftermath", "Aid transfer failed; restoring roster snapshots: " + ex.Message);
            for (int i = 0; i < transfer.Count; i++)
            {
                try
                {
                    source.AddToCounts(transfer[i].Key, sourceBefore[i] - AidItemCount(source, transfer[i].Key));
                    market.AddToCounts(transfer[i].Key, marketBefore[i] - AidItemCount(market, transfer[i].Key));
                }
                catch (Exception rollback) { Logger.Log("VillageAftermath", "Aid roster restore failed: " + rollback.Message); }
            }
            return false;
        }
    }

    private void RecordVillageGift(Settlement village, int kind, int amount, string itemList)
    {
        _aidLastDay[village.StringId + ":" + kind] = CurrentDay;
        if (VillageAidRules.RecordDelivery(_aidDeliveryRemainder, village.StringId)) Assistance.Gain(village.StringId, InitialVillageAssistance);
        if (!_aidRoundDay.TryGetValue(village.StringId, out int round) || CurrentDay - round >= VillageGovernanceRules.AidCooldownDays)
        { round = CurrentDay; _aidRoundDay[village.StringId] = round; _aidRoundMask[village.StringId] = 0; }
        int before = _aidRoundMask.TryGetValue(village.StringId, out int mask) ? mask : 0;
        int after = before | (1 << kind); _aidRoundMask[village.StringId] = after;
        int percent = VillageGovernanceRules.AidPercent(after);
        if (percent > 0)
        {
            string key = village.StringId + ":" + round;
            _aidBonuses[key] = percent;
            if (!_aidBonusExpires.ContainsKey(key)) _aidBonusExpires[key] = CurrentDay + VillageGovernanceRules.AidDurationDays;
        }
        RefreshAidMultipliers();
        Departure.MarkDisposition();
        var landOwner = village.Village.Bound?.OwnerClan?.Leader;
        void Relation(Hero target, int delta)
        {
            if (target == null || target == Hero.MainHero || !target.IsAlive) return;
            try { ChangeRelationAction.ApplyPlayerRelation(target, delta, true, true); }
            catch (Exception ex) { Logger.Log("VillageAftermath", "Aid relation failed: " + ex.Message); }
        }
        Relation(landOwner, 5);
        foreach (var notable in village.Notables) Relation(notable, 10);
        try
        {
            string summary = kind == 0 ? amount + VP(" 第纳尔", " denars") : itemList;
            RecordForeignFact(village, VP("玩家向村庄实际交付了：", "The player actually delivered aid: ") + summary, "aid_" + kind, false);
        }
        catch (Exception ex) { Logger.Log("VillageAftermath", "Aid memory failed after delivery: " + ex.Message); }
    }
    private void RefreshAidMultipliers()
    {
        _aidMultiplier.Clear();
        foreach (var entry in _aidBonuses.ToList())
        {
            int split = entry.Key.LastIndexOf(':');
            if (split < 1 || !int.TryParse(entry.Key.Substring(split + 1), out int day) || (_aidBonusExpires.TryGetValue(entry.Key, out int expiry) ? CurrentDay >= expiry : CurrentDay - day >= VillageGovernanceRules.AidDurationDays))
            { _aidBonuses.Remove(entry.Key); _aidBonusExpires.Remove(entry.Key); continue; }
            string id = entry.Key.Substring(0, split);
            _aidMultiplier[id] = (_aidMultiplier.TryGetValue(id, out float old) ? old : 1f) + entry.Value / 100f;
        }
    }

}
