using System;
using System.Threading;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    private static long GeneratedRewardMarketRulesVersion = 1;
    private GeneratedRewardMarketCleanupOwner _marketCleanup;
    private GeneratedRewardMarketCleanupOwner MarketCleanup => _marketCleanup ??= CreateMarketCleanup(this);
    private static void InvalidateGeneratedRewardMarketRules() => Interlocked.Increment(ref GeneratedRewardMarketRulesVersion);
    private static GeneratedRewardMarketCleanupOwner CreateMarketCleanup(RewardSystemBehavior owner) => new GeneratedRewardMarketCleanupOwner(
        IsGeneratedRewardMarketExcludedItem, IsGeneratedRewardMarketTransferBlockedItem,
        SaveRuntimeGuard.CaptureGeneration, () => Interlocked.Read(ref GeneratedRewardMarketRulesVersion),
        () => TWParallel.IsMainThread() && (owner == null || ReferenceEquals(Instance, owner))
            && !(Campaign.Current?.SaveHandler?.IsSaving ?? false),
        (label, reason, count, slots) => Logger.Log("Logic", "[RewardItemMarketGuard] removed_market_generated_items reason=" + reason
            + " owner=" + label + " count=" + count + " slots=" + slots),
        (label, reason, error) => Logger.Log("Logic", "[RewardItemMarketGuard] cleanup_failed reason=" + reason
            + " owner=" + label + " error=" + error.GetType().Name + ":" + error.Message));

    private static void CleanGeneratedRewardTradeMarkets(ItemRoster leftRoster, Settlement settlement, string reason)
    {
        ItemRoster market = settlement?.ItemRoster;
        string label = BuildGeneratedRewardMarketRosterLabel(settlement, "trade_roster");
        RemoveGeneratedRewardItemsFromRoster(market, label, reason);
        if (!ReferenceEquals(leftRoster, market)) RemoveGeneratedRewardItemsFromRoster(leftRoster, label, reason);
    }
    private void QueueGeneratedRewardWorldMarketCleanup(string reason) => MarketCleanup.BeginWorldScan(
        () => Campaign.Current?.Settlements, reason);
    private void QueueGeneratedRewardSettlementMarketCleanup(Settlement settlement, string reason) => MarketCleanup.Enqueue(settlement, reason);
    private void ProcessGeneratedRewardMarketCleanup()
    {
        if (_marketCleanup?.HasWork != true || !TWParallel.IsMainThread()
            || !ReferenceEquals(Instance, this) || Campaign.Current == null
            || (Campaign.Current.SaveHandler?.IsSaving ?? false)) return;
        using (FreezeWatchdog.Scope("RewardSystem.MarketCleanup.Slice")) _marketCleanup.Tick();
    }
}
