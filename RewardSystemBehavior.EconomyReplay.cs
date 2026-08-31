using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Helpers;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
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
        foreach (EconomyRewardDebtAction action in plan.Actions ?? Array.Empty<EconomyRewardDebtAction>())
        {
            if (action == null)
            {
                failedCount++;
                continue;
            }

            try
            {
                if (TryReplayAction(action, giver, receiver, out string factText))
                {
                    appliedCount++;
                    if (!string.IsNullOrWhiteSpace(factText))
                    {
                        confirmedFacts.Add(new FactRecord(
                            "economy.reward_debt",
                            giver.StringId ?? snapshot.Identity.SubjectId,
                            factText));
                    }
                }
                else
                {
                    failedCount++;
                }
            }
            catch (Exception exception)
            {
                failedCount++;
                Logger.Log("RewardSystem", "[RefactorEconomy] action failed kind=" + action.Kind + " error=" + exception.Message);
            }
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
        out string factText)
    {
        factText = string.Empty;
        switch (action.Kind)
        {
            case EconomyRewardDebtActionKind.GiveGold:
                return TryReplayGiveGold(action, giver, receiver, out factText);
            case EconomyRewardDebtActionKind.GiveAsset:
                return TryReplayGiveAsset(action, giver, receiver, out factText);
            case EconomyRewardDebtActionKind.DebtCreate:
                return TryReplayDebtCreate(action, giver, out factText);
            case EconomyRewardDebtActionKind.DebtResolve:
                return TryReplayDebtResolve(action, giver, out factText);
            case EconomyRewardDebtActionKind.SettlementTransfer:
                return TryReplaySettlementTransfer(action, giver, receiver, out factText);
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
        int actual = TransferGold(giver, receiver, amount, forceComplete: receiver == Hero.MainHero && giver != Hero.MainHero);
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
        if (string.IsNullOrWhiteSpace(assetToken) || string.IsNullOrWhiteSpace(quantityToken))
        {
            return false;
        }

        if (TransferQuantitySpec.IsAllValue(quantityToken))
        {
            List<RewardItemInfo> items = BuildHeroRewardItemResolutionContext(giver)
                ?.Where(item => item?.Item != null && item.Count > 0)
                ?.ToList() ?? new List<RewardItemInfo>();
            int total = 0;
            HashSet<string> transferredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RewardItemInfo item in items)
            {
                string key = GetRewardItemTransferKey(item);
                if (string.IsNullOrWhiteSpace(key) || !transferredKeys.Add(key))
                {
                    continue;
                }
                int actual = TransferItemById(giver, receiver, key, Math.Max(1, ResolveAllRewardItemAmount(key, items)), out string itemName, forceComplete: false);
                total += Math.Max(0, actual);
            }
            if (total <= 0)
            {
                return false;
            }
            factText = "已实际转移全部可用普通物品，共 " + total.ToString(CultureInfo.InvariantCulture) + " 件。";
            return true;
        }

        if (!int.TryParse(quantityToken, NumberStyles.None, CultureInfo.InvariantCulture, out int quantity) || quantity <= 0)
        {
            return false;
        }
        List<RewardItemInfo> contextItems = BuildHeroRewardItemResolutionContext(giver);
        string lookup = assetToken;
        if (TryResolveRewardItemByNameOrId(lookup, contextItems, out RewardItemResolution resolution, "refactor_economy"))
        {
            string resolvedLookup = BuildRewardItemTransferLookup(resolution);
            if (!string.IsNullOrWhiteSpace(resolvedLookup))
            {
                lookup = resolvedLookup;
            }
        }
        if (TryResolveKnownItemAssetTokenForExternal(lookup, out string itemId))
        {
            int actual = TransferItemById(giver, receiver, itemId, quantity, out string itemName, forceComplete: true);
            if (actual <= 0)
            {
                return false;
            }
            factText = "已实际转移物品 " + (itemName ?? itemId) + " ×" + actual.ToString(CultureInfo.InvariantCulture) + "。";
            return true;
        }

        if (!IsValidGeneratedRpAssetNameForExternal(assetToken))
        {
            return false;
        }
        int generated = GenerateRpAssetToPlayer(
            assetToken,
            quantity,
            giver.Name?.ToString() ?? "NPC",
            giver.CharacterObject,
            out string generatedName,
            out ItemObject generatedItem,
            "refactor_economy_replay");
        if (generated <= 0)
        {
            return false;
        }
        factText = "已生成并实际转移 RP 物品 " + (generatedName ?? assetToken) + " ×" + generated.ToString(CultureInfo.InvariantCulture) + "。";
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
            out string statusText);
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
}
