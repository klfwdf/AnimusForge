using System;
using System.Collections.Generic;
using System.Globalization;
using Helpers;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    /// <summary>
    /// Creates the production Economy/Reward/Debt main-thread port. The port
    /// validates the detached boundary first, then this owner resolves the
    /// current Hero and delegates to the existing transfer/debt methods.
    /// Non-Hero and merchant/party callers remain fail-closed until their
    /// channel-specific owner supplies a separate adapter.
    /// </summary>
    public static LegacyEconomyRewardDebtMainThreadPort CreateEconomyRewardDebtMainThreadPortForExternal()
    {
        RewardSystemBehavior owner = Instance;
        if (owner == null)
        {
            try
            {
                owner = Campaign.Current?.GetCampaignBehavior<RewardSystemBehavior>();
            }
            catch
            {
                owner = null;
            }
        }
        if (owner == null)
        {
            return null;
        }

        return new LegacyEconomyRewardDebtMainThreadPort(
            () => TWParallel.IsMainThread(),
            owner.IsCurrentEconomyReplayTarget,
            owner.ReplayEconomyRewardDebtPlanOnMainThread);
    }

    private bool IsCurrentEconomyReplayTarget(GameInteractionSnapshot snapshot)
    {
        string subjectId = snapshot?.Identity?.SubjectId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(subjectId) || Hero.MainHero == null)
        {
            return false;
        }
        Hero subject = Hero.Find(subjectId);
        return subject != null
            && subject != Hero.MainHero
            && string.Equals(subject.StringId ?? string.Empty, subjectId, StringComparison.OrdinalIgnoreCase);
    }

    private EconomyRewardDebtReplayResult ReplayEconomyRewardDebtPlanOnMainThread(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot snapshot)
    {
        Hero giver = Hero.Find(snapshot?.Identity?.SubjectId ?? string.Empty);
        Hero receiver = Hero.MainHero;
        if (giver == null || receiver == null || giver == receiver)
        {
            return ReplayFailure("economy.giver_or_receiver_missing");
        }

        List<FactRecord> confirmedFacts = new List<FactRecord>();
        int appliedCount = 0;
        int failedCount = 0;
        bool unknownAfterStart = false;
        foreach (EconomyRewardDebtAction action in plan.Actions ?? Array.Empty<EconomyRewardDebtAction>())
        {
            if (action == null)
            {
                failedCount++;
                continue;
            }

            bool applied;
            string factText;
            EconomyMutationObservation mutationObservation = new EconomyMutationObservation();
            try
            {
                applied = TryReplayAction(action, giver, receiver, mutationObservation, out factText);
            }
            catch (Exception exception)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe("[RefactorEconomy] action failed kind=" + action.Kind + " error=" + exception.Message);
                break;
            }
            if (mutationObservation.UnknownAfterStart)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe(
                    "[RefactorEconomy] action outcome unknown kind=" + action.Kind
                    + " error=" + mutationObservation.ErrorCode);
                break;
            }
            if (!applied)
            {
                failedCount++;
                continue;
            }
            appliedCount++;
            if (!string.IsNullOrWhiteSpace(factText))
            {
                try
                {
                    confirmedFacts.Add(new FactRecord(
                        "economy.reward_debt",
                        giver.StringId ?? snapshot.Identity.SubjectId,
                        factText));
                }
                catch (Exception exception)
                {
                    LogEconomyReplayFailureSafe("[RefactorEconomy] confirmed fact failed kind=" + action.Kind + " error=" + exception.Message);
                }
            }
        }

        if (unknownAfterStart)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.UnknownAfterStart,
                appliedCount,
                confirmedFacts,
                "economy.unknown_after_start");
        }
        if (appliedCount <= 0)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Failed,
                0,
                confirmedFacts,
                failedCount > 0 ? "economy.no_action_applied" : "economy.no_actions");
        }

        return new EconomyRewardDebtReplayResult(
            failedCount > 0
                ? EconomyRewardDebtReplayStatus.PartiallyApplied
                : EconomyRewardDebtReplayStatus.Applied,
            appliedCount,
            confirmedFacts,
            failedCount > 0 ? "economy.partial_replay" : string.Empty);
    }

    private bool TryReplayAction(
        EconomyRewardDebtAction action,
        Hero giver,
        Hero receiver,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        switch (action.Kind)
        {
            case EconomyRewardDebtActionKind.GiveGold:
                return TryReplayGiveGold(action, giver, receiver, out factText);
            case EconomyRewardDebtActionKind.GiveAsset:
                return TryReplayGiveAsset(action, giver, receiver, mutationObservation, out factText);
            case EconomyRewardDebtActionKind.DebtCreate:
                return TryReplayDebtCreate(action, giver, out factText);
            case EconomyRewardDebtActionKind.DebtResolve:
                return TryReplayDebtResolve(action, giver, out factText);
            case EconomyRewardDebtActionKind.SettlementTransfer:
                return TryReplaySettlementTransfer(action, giver, receiver, mutationObservation, out factText);
            default:
                return false;
        }
    }

    private bool TryReplayGiveGold(
        EconomyRewardDebtAction action,
        Hero giver,
        Hero receiver,
        out string factText)
    {
        factText = string.Empty;
        if (!int.TryParse(action.AmountToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
        {
            return false;
        }
        Settlement market = ResolveNotableMarketSettlement(giver);
        bool forceComplete = receiver == Hero.MainHero && giver != Hero.MainHero;
        int actual = IsNotableMarketHero(giver, market)
            ? TransferGoldFromSettlement(market, receiver, amount, giver.Name?.ToString() ?? "NPC",
                giver.CharacterObject, forceComplete: forceComplete)
            : TransferGold(giver, receiver, amount, forceComplete: forceComplete);
        if (actual <= 0)
        {
            return false;
        }
        factText = "已实际转移 " + actual.ToString(CultureInfo.InvariantCulture) + " 第纳尔。";
        return true;
    }

    private bool TryReplayGiveAsset(
        EconomyRewardDebtAction action,
        Hero giver,
        Hero receiver,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        string assetToken = (action.AssetToken ?? string.Empty).Trim();
        string quantityToken = (action.QuantityToken ?? string.Empty).Trim();
        if (IsGoldAssetTokenForExternal(assetToken))
        {
            return TryReplayGiveGold(
                new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveGold,
                    action.SourceTag,
                    action.TargetId,
                    "GOLD",
                    quantityToken,
                    quantityToken,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    EconomyRewardDebtCapabilityIds.GiveGold),
                giver,
                receiver,
                out factText);
        }
        if (string.IsNullOrWhiteSpace(assetToken)
            || TransferQuantitySpec.IsAllValue(assetToken)
            || !TransferQuantitySpec.TryParse(quantityToken, out TransferQuantitySpec quantity))
        {
            return false;
        }

        bool authorized = TryResolveAuthorizedHeroRewardItem(
            giver, assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey);
        bool generated = !authorized
            && !quantity.IsAll
            && receiver == Hero.MainHero
            && giver != Hero.MainHero
            && IsValidGeneratedRpAssetNameForExternal(assetToken);
        if (!authorized && !generated)
        {
            return false;
        }
        string lookup = authorized ? transferKey : assetToken;
        // Count the authorized key before stripping a market owner prefix for transfer.
        int requestedAmount = quantity.IsAll
            ? ResolveAllRewardItemAmount(lookup, authorizedItems)
            : quantity.Amount;
        if (requestedAmount <= 0)
        {
            return false;
        }

        int actual;
        string itemName;
        if (generated)
        {
            actual = GenerateRpAssetToPlayer(
                assetToken, requestedAmount, giver.Name?.ToString() ?? "NPC", giver.CharacterObject,
                out itemName, out _, "refactor_economy_replay",
                mutationObservation: mutationObservation);
        }
        else
        {
            Settlement market = ResolveNotableMarketSettlement(giver);
            bool usesMarket = IsNotableMarketHero(giver, market);
            string marketLookup = string.Empty;
            bool marketItem = usesMarket && TryParseNotableMarketPromptStringId(lookup, out marketLookup);
            if (usesMarket && !marketItem
                && TryResolveRewardItemByNameOrId(lookup, BuildSettlementRewardItemResolutionContext(market),
                    out RewardItemResolution marketResolution, "notable_market_give_item"))
            {
                marketLookup = BuildRewardItemTransferLookup(marketResolution);
                marketItem = !string.IsNullOrWhiteSpace(marketLookup);
            }
            actual = marketItem
                ? TransferItemFromSettlementForEconomyReplay(
                    market, receiver, marketLookup, requestedAmount, giver.Name?.ToString() ?? "NPC",
                    out itemName, giver.CharacterObject,
                    forceComplete: !quantity.IsAll && receiver == Hero.MainHero && giver != Hero.MainHero,
                    mutationObservation: mutationObservation)
                : TransferItemByIdForEconomyReplay(
                    giver, receiver, lookup, requestedAmount, out itemName,
                    forceComplete: !quantity.IsAll && receiver == Hero.MainHero && giver != Hero.MainHero,
                    mutationObservation: mutationObservation);
        }
        if (actual <= 0)
        {
            return false;
        }
        factText = (generated ? "已生成并实际转移 RP 物品 " : "已实际转移物品 ")
            + (itemName ?? lookup) + " ×" + actual.ToString(CultureInfo.InvariantCulture) + "。";
        return true;
    }

    private bool TryReplayDebtCreate(
        EconomyRewardDebtAction action,
        Hero giver,
        out string factText)
    {
        factText = string.Empty;
        if (!string.Equals(action.DirectionToken, "P", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(action.AmountToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount)
            || !int.TryParse(action.DueDaysToken, NumberStyles.None, CultureInfo.InvariantCulture, out int dueDays)
            || amount <= 0 || dueDays <= 0)
        {
            return false;
        }
        DebtRecord.DebtLine line = SetDebtForNpc(giver, amount, dueDays, action.NoteToken);
        if (line == null || string.IsNullOrWhiteSpace(line.DebtId))
        {
            return false;
        }
        factText = "已记录玩家对 " + (giver.Name?.ToString() ?? giver.StringId ?? "NPC")
            + " 的债务承诺：" + amount.ToString(CultureInfo.InvariantCulture)
            + " 第纳尔，债务ID=" + line.DebtId + "。";
        return true;
    }

    private bool TryReplayDebtResolve(
        EconomyRewardDebtAction action,
        Hero giver,
        out string factText)
    {
        factText = string.Empty;
        if (string.IsNullOrWhiteSpace(action.DebtId)
            || !TryFindDebtLineById(giver, action.DebtId, out _, out _, out _))
        {
            return false;
        }
        if (!ResolveDebtByIdByAgreement(giver, action.DebtId, out string statusText)
            && string.IsNullOrWhiteSpace(statusText))
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(statusText)
            || statusText.IndexOf("已按协商解除", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }
        factText = statusText;
        return true;
    }

    private bool TryReplaySettlementTransfer(
        EconomyRewardDebtAction action,
        Hero giver,
        Hero receiver,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        string direction = (action.DirectionToken ?? string.Empty).Trim().ToUpperInvariant();
        if (direction != "TO_PLAYER" && direction != "TO_NPC")
        {
            return false;
        }
        bool applied = TryApplySettlementTransferAction(
            giver,
            receiver,
            direction,
            action.SettlementToken,
            new Dictionary<string, FixedAssetTokenResolution>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            out _,
            out string statusText,
            mutationObservation);
        if (!applied)
        {
            return false;
        }
        factText = string.IsNullOrWhiteSpace(statusText)
            ? "已完成固定资产转移：" + action.SettlementToken + "。"
            : statusText;
        return true;
    }

    private static EconomyRewardDebtReplayResult ReplayFailure(string errorCode)
    {
        return new EconomyRewardDebtReplayResult(
            EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation,
            0,
            Array.Empty<FactRecord>(),
            errorCode);
    }

    private static void LogEconomyReplayFailureSafe(string message)
    {
        try
        {
            Logger.Log("RewardSystem", message ?? string.Empty);
        }
        catch
        {
            // Diagnostics must not change a main-thread owner outcome.
        }
    }

    private sealed class EconomyMutationObservation
    {
        internal bool UnknownAfterStart { get; private set; }
        internal string ErrorCode { get; private set; } = string.Empty;

        internal void MarkUnknown(string errorCode)
        {
            UnknownAfterStart = true;
            if (string.IsNullOrWhiteSpace(ErrorCode))
            {
                ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                    ? "economy.mutation_unknown"
                    : errorCode;
            }
        }
    }
}
