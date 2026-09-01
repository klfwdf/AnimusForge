using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// Creates the production settlement-merchant economy port. The character
    /// and settlement are captured by the channel owner; all live validation
    /// and mutation remains in this RewardSystemBehavior owner.
    /// </summary>
    public static LegacyEconomyRewardDebtMainThreadPort CreateMerchantEconomyRewardDebtMainThreadPortForExternal(
        CharacterObject giverCharacter,
        Settlement settlement,
        string expectedSubjectId,
        string giverName = null)
    {
        RewardSystemBehavior owner = ResolveEconomyOwner();
        if (owner == null || giverCharacter == null || settlement == null || Hero.MainHero == null)
        {
            return null;
        }
        if (!owner.TryGetSettlementMerchantKind(giverCharacter, out SettlementMerchantKind _))
        {
            return null;
        }
        string stableSubjectId = (expectedSubjectId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stableSubjectId))
        {
            return null;
        }
        string displayName = string.IsNullOrWhiteSpace(giverName)
            ? giverCharacter.Name?.ToString() ?? "商人"
            : giverName.Trim();
        return new LegacyEconomyRewardDebtMainThreadPort(
            () => TWParallel.IsMainThread(),
            snapshot => owner.IsCurrentMerchantEconomyReplayTarget(snapshot, giverCharacter, settlement, stableSubjectId),
            (plan, snapshot) => owner.ReplayMerchantEconomyPlanOnMainThread(
                plan, snapshot, giverCharacter, settlement, displayName));
    }

    private bool IsCurrentMerchantEconomyReplayTarget(
        GameInteractionSnapshot snapshot,
        CharacterObject giverCharacter,
        Settlement settlement,
        string expectedSubjectId)
    {
        if (Hero.MainHero == null || giverCharacter == null || settlement == null)
        {
            return false;
        }
        if (!TryGetSettlementMerchantKind(giverCharacter, out SettlementMerchantKind _))
        {
            return false;
        }
        Settlement current = Settlement.CurrentSettlement;
        bool sameSettlement = current != null
            && (ReferenceEquals(current, settlement)
                || string.Equals(current.StringId ?? string.Empty, settlement.StringId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        return sameSettlement
            && snapshot?.Identity != null
            && string.Equals(snapshot.Identity.SubjectId?.Trim(), (expectedSubjectId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private EconomyRewardDebtReplayResult ReplayMerchantEconomyPlanOnMainThread(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot snapshot,
        CharacterObject giverCharacter,
        Settlement settlement,
        string giverName)
    {
        Hero receiver = Hero.MainHero;
        if (giverCharacter == null || settlement == null || receiver == null
            || !TryGetSettlementMerchantKind(giverCharacter, out SettlementMerchantKind kind))
        {
            return ReplayMerchantFailure("economy.merchant_missing_or_invalid");
        }

        int appliedCount = 0;
        int failedCount = 0;
        bool unknownAfterStart = false;
        List<FactRecord> facts = new List<FactRecord>();
        foreach (EconomyRewardDebtAction action in plan?.Actions ?? Array.Empty<EconomyRewardDebtAction>())
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
                applied = TryReplayMerchantAction(
                    action,
                    giverCharacter,
                    settlement,
                    kind,
                    receiver,
                    giverName,
                    mutationObservation,
                    out factText);
            }
            catch (Exception exception)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe("[RefactorMerchantEconomy] action failed kind=" + action.Kind + " error=" + exception.Message);
                break;
            }
            if (mutationObservation.UnknownAfterStart)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe(
                    "[RefactorMerchantEconomy] action outcome unknown kind=" + action.Kind
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
                    facts.Add(new FactRecord(
                        "economy.merchant_reward",
                        snapshot?.Identity?.SubjectId ?? giverCharacter.StringId ?? "merchant",
                        factText));
                }
                catch (Exception exception)
                {
                    LogEconomyReplayFailureSafe("[RefactorMerchantEconomy] confirmed fact failed kind=" + action.Kind + " error=" + exception.Message);
                }
            }
        }

        if (unknownAfterStart)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.UnknownAfterStart,
                appliedCount,
                facts,
                "economy.merchant_unknown_after_start");
        }
        if (appliedCount <= 0)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Failed,
                0,
                facts,
                failedCount > 0 ? "economy.merchant_no_action_applied" : "economy.merchant_no_actions");
        }
        return new EconomyRewardDebtReplayResult(
            failedCount > 0
                ? EconomyRewardDebtReplayStatus.PartiallyApplied
                : EconomyRewardDebtReplayStatus.Applied,
            appliedCount,
            facts,
            failedCount > 0 ? "economy.merchant_partial_replay" : string.Empty);
    }

    private bool TryReplayMerchantAction(
        EconomyRewardDebtAction action,
        CharacterObject giverCharacter,
        Settlement settlement,
        SettlementMerchantKind kind,
        Hero receiver,
        string giverName,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        switch (action.Kind)
        {
            case EconomyRewardDebtActionKind.GiveGold:
                return TryReplayMerchantGold(action, giverCharacter, settlement, receiver, giverName, out factText);
            case EconomyRewardDebtActionKind.GiveAsset:
                return TryReplayMerchantAsset(
                    action,
                    giverCharacter,
                    settlement,
                    receiver,
                    giverName,
                    mutationObservation,
                    out factText);
            case EconomyRewardDebtActionKind.DebtCreate:
                return TryReplayMerchantDebtCreate(action, giverCharacter, settlement, kind, out factText);
            case EconomyRewardDebtActionKind.DebtResolve:
                return TryReplayMerchantDebtResolve(action, settlement, kind, out factText);
            case EconomyRewardDebtActionKind.SettlementTransfer:
                return false;
            default:
                return false;
        }
    }

    private bool TryReplayMerchantGold(
        EconomyRewardDebtAction action,
        CharacterObject giverCharacter,
        Settlement settlement,
        Hero receiver,
        string giverName,
        out string factText)
    {
        factText = string.Empty;
        if (!int.TryParse(action.AmountToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
        {
            return false;
        }
        int actual = TransferGoldFromSettlement(
            settlement, receiver, amount, giverName, giverCharacter, forceComplete: receiver == Hero.MainHero);
        if (actual <= 0)
        {
            return false;
        }
        factText = "商人已实际交付 " + actual.ToString(CultureInfo.InvariantCulture) + " 第纳尔。";
        return true;
    }

    private bool TryReplayMerchantAsset(
        EconomyRewardDebtAction action,
        CharacterObject giverCharacter,
        Settlement settlement,
        Hero receiver,
        string giverName,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        string assetToken = (action.AssetToken ?? string.Empty).Trim();
        string quantityToken = (action.QuantityToken ?? string.Empty).Trim();
        if (IsGoldAssetTokenForExternal(assetToken))
        {
            return TryReplayMerchantGold(
                new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveGold, action.SourceTag, action.TargetId,
                    "GOLD", quantityToken, quantityToken, string.Empty, string.Empty, string.Empty,
                    EconomyRewardDebtCapabilityIds.GiveGold),
                giverCharacter, settlement, receiver, giverName, out factText);
        }
        if (string.IsNullOrWhiteSpace(assetToken)
            || TransferQuantitySpec.IsAllValue(assetToken)
            || !TransferQuantitySpec.TryParse(quantityToken, out TransferQuantitySpec quantity))
        {
            return false;
        }

        bool authorized = TryResolveAuthorizedMerchantRewardItem(
            giverCharacter, assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey);
        bool generated = !authorized
            && !quantity.IsAll
            && receiver == Hero.MainHero
            && IsValidGeneratedRpAssetNameForExternal(assetToken);
        if (!authorized && !generated)
        {
            return false;
        }
        string lookup = authorized ? transferKey : assetToken;
        int requestedAmount = quantity.IsAll
            ? ResolveAllRewardItemAmount(lookup, authorizedItems)
            : quantity.Amount;
        if (requestedAmount <= 0 || (generated && quantity.IsAll))
        {
            return false;
        }

        int actual;
        string itemName;
        if (generated)
        {
            actual = GenerateRpAssetToPlayer(
                assetToken, requestedAmount, giverName, giverCharacter,
                out itemName, out _, "refactor_merchant_economy_replay",
                mutationObservation: mutationObservation);
        }
        else
        {
            actual = TransferItemFromSettlementForEconomyReplay(
                settlement, receiver, lookup, requestedAmount, giverName, out itemName,
                giverCharacter,
                forceComplete: !quantity.IsAll && receiver == Hero.MainHero,
                mutationObservation: mutationObservation);
        }
        if (actual <= 0)
        {
            return false;
        }
        factText = "商人已实际交付物品 " + (itemName ?? lookup)
            + " ×" + actual.ToString(CultureInfo.InvariantCulture) + "。";
        return true;
    }

    private bool TryReplayMerchantDebtCreate(
        EconomyRewardDebtAction action,
        CharacterObject giverCharacter,
        Settlement settlement,
        SettlementMerchantKind kind,
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
        DebtRecord.DebtLine line = SetDebtForSettlementMerchant(settlement, kind, amount, dueDays, action.NoteToken);
        if (line == null || string.IsNullOrWhiteSpace(line.DebtId))
        {
            return false;
        }
        factText = "已记录市场债务承诺：" + amount.ToString(CultureInfo.InvariantCulture)
            + " 第纳尔，债务ID=" + line.DebtId + "。";
        return true;
    }

    private bool TryReplayMerchantDebtResolve(
        EconomyRewardDebtAction action,
        Settlement settlement,
        SettlementMerchantKind kind,
        out string factText)
    {
        factText = string.Empty;
        if (string.IsNullOrWhiteSpace(action.DebtId)
            || !TryFindSettlementMerchantDebtLineById(settlement, kind, action.DebtId, out _, out _, out _))
        {
            return false;
        }
        bool cleared = ResolveSettlementMerchantDebtByIdByAgreement(settlement, kind, action.DebtId, out string statusText);
        if (!cleared && string.IsNullOrWhiteSpace(statusText))
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

    private static EconomyRewardDebtReplayResult ReplayMerchantFailure(string errorCode)
    {
        return new EconomyRewardDebtReplayResult(
            EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation,
            0,
            Array.Empty<FactRecord>(),
            errorCode);
    }
}
