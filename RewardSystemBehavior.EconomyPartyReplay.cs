using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    /// <summary>
    /// Creates the production PartyBase economy port. The source party and
    /// character must be captured by the channel owner at the interaction
    /// boundary; the port revalidates the stable subject and active party on
    /// the main thread before delegating to existing transfer methods.
    /// </summary>
    public static LegacyEconomyRewardDebtMainThreadPort CreatePartyEconomyRewardDebtMainThreadPortForExternal(
        PartyBase giverParty,
        BasicCharacterObject giverCharacter,
        string expectedSubjectId,
        string giverName = null)
    {
        RewardSystemBehavior owner = ResolveEconomyOwner();
        if (owner == null || giverParty?.MobileParty == null || Hero.MainHero == null)
        {
            return null;
        }

        string stableSubjectId = (expectedSubjectId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stableSubjectId))
        {
            return null;
        }
        string displayName = string.IsNullOrWhiteSpace(giverName)
            ? giverCharacter?.Name?.ToString() ?? giverParty.MobileParty.Name?.ToString() ?? "对方部队"
            : giverName.Trim();
        return new LegacyEconomyRewardDebtMainThreadPort(
            () => TWParallel.IsMainThread(),
            snapshot => owner.IsCurrentPartyEconomyReplayTarget(snapshot, giverParty, stableSubjectId),
            (plan, snapshot) => owner.ReplayPartyEconomyPlanOnMainThread(
                plan, snapshot, giverParty, giverCharacter, displayName));
    }

    private static RewardSystemBehavior ResolveEconomyOwner()
    {
        RewardSystemBehavior owner = Instance;
        if (owner != null)
        {
            return owner;
        }
        try
        {
            return Campaign.Current?.GetCampaignBehavior<RewardSystemBehavior>();
        }
        catch
        {
            return null;
        }
    }

    private bool IsCurrentPartyEconomyReplayTarget(
        GameInteractionSnapshot snapshot,
        PartyBase giverParty,
        string expectedSubjectId)
    {
        if (Hero.MainHero == null || giverParty?.MobileParty == null || !giverParty.MobileParty.IsActive)
        {
            return false;
        }
        return snapshot?.Identity != null
            && string.Equals(
                snapshot.Identity.SubjectId?.Trim(),
                (expectedSubjectId ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);
    }

    private EconomyRewardDebtReplayResult ReplayPartyEconomyPlanOnMainThread(
        EconomyRewardDebtReplayPlan plan,
        GameInteractionSnapshot snapshot,
        PartyBase giverParty,
        BasicCharacterObject giverCharacter,
        string giverName)
    {
        Hero receiver = Hero.MainHero;
        if (giverParty?.MobileParty == null || receiver == null || !giverParty.MobileParty.IsActive)
        {
            return ReplayPartyFailure("economy.party_missing_or_inactive");
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
                applied = TryReplayPartyAction(
                    action,
                    giverParty,
                    giverCharacter,
                    receiver,
                    giverName,
                    mutationObservation,
                    out factText);
            }
            catch (Exception exception)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe("[RefactorPartyEconomy] action failed kind=" + action.Kind + " error=" + exception.Message);
                break;
            }
            if (mutationObservation.UnknownAfterStart)
            {
                failedCount++;
                unknownAfterStart = true;
                LogEconomyReplayFailureSafe(
                    "[RefactorPartyEconomy] action outcome unknown kind=" + action.Kind
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
                        "economy.party_reward",
                        snapshot?.Identity?.SubjectId ?? "party",
                        factText));
                }
                catch (Exception exception)
                {
                    LogEconomyReplayFailureSafe("[RefactorPartyEconomy] confirmed fact failed kind=" + action.Kind + " error=" + exception.Message);
                }
            }
        }

        if (unknownAfterStart)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.UnknownAfterStart,
                appliedCount,
                facts,
                "economy.party_unknown_after_start");
        }
        if (appliedCount <= 0)
        {
            return new EconomyRewardDebtReplayResult(
                EconomyRewardDebtReplayStatus.Failed,
                0,
                facts,
                failedCount > 0 ? "economy.party_no_action_applied" : "economy.party_no_actions");
        }
        return new EconomyRewardDebtReplayResult(
            failedCount > 0
                ? EconomyRewardDebtReplayStatus.PartiallyApplied
                : EconomyRewardDebtReplayStatus.Applied,
            appliedCount,
            facts,
            failedCount > 0 ? "economy.party_partial_replay" : string.Empty);
    }

    private bool TryReplayPartyAction(
        EconomyRewardDebtAction action,
        PartyBase giverParty,
        BasicCharacterObject giverCharacter,
        Hero receiver,
        string giverName,
        EconomyMutationObservation mutationObservation,
        out string factText)
    {
        factText = string.Empty;
        switch (action.Kind)
        {
            case EconomyRewardDebtActionKind.GiveGold:
                return TryReplayPartyGold(action, giverParty, giverCharacter, receiver, giverName, out factText);
            case EconomyRewardDebtActionKind.GiveAsset:
                return TryReplayPartyAsset(
                    action,
                    giverParty,
                    giverCharacter,
                    receiver,
                    giverName,
                    mutationObservation,
                    out factText);
            case EconomyRewardDebtActionKind.DebtCreate:
            case EconomyRewardDebtActionKind.DebtResolve:
            case EconomyRewardDebtActionKind.SettlementTransfer:
                // Party reward authority does not own debt or fixed-asset
                // transfer semantics; do not reinterpret those tags here.
                return false;
            default:
                return false;
        }
    }

    private bool TryReplayPartyGold(
        EconomyRewardDebtAction action,
        PartyBase giverParty,
        BasicCharacterObject giverCharacter,
        Hero receiver,
        string giverName,
        out string factText)
    {
        factText = string.Empty;
        if (!int.TryParse(action.AmountToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
        {
            return false;
        }
        int actual = TransferGoldFromParty(
            giverParty,
            receiver,
            amount,
            giverName,
            giverCharacter,
            forceComplete: receiver == Hero.MainHero);
        if (actual <= 0)
        {
            return false;
        }
        factText = "部队已实际交付 " + actual.ToString(CultureInfo.InvariantCulture) + " 第纳尔。";
        return true;
    }

    private bool TryReplayPartyAsset(
        EconomyRewardDebtAction action,
        PartyBase giverParty,
        BasicCharacterObject giverCharacter,
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
            return TryReplayPartyGold(
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
                giverParty,
                giverCharacter,
                receiver,
                giverName,
                out factText);
        }
        if (string.IsNullOrWhiteSpace(assetToken)
            || TransferQuantitySpec.IsAllValue(assetToken)
            || !TransferQuantitySpec.TryParse(quantityToken, out TransferQuantitySpec quantity))
        {
            return false;
        }

        bool authorized = TryResolveAuthorizedPartyRewardItem(
            giverParty,
            giverCharacter,
            assetToken,
            out List<RewardItemInfo> authorizedItems,
            out string transferKey);
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
                assetToken,
                requestedAmount,
                giverName,
                giverCharacter,
                out itemName,
                out _,
                "refactor_party_economy_replay",
                mutationObservation: mutationObservation);
        }
        else
        {
            actual = TransferItemFromPartyForEconomyReplay(
                giverParty,
                receiver,
                lookup,
                requestedAmount,
                giverName,
                out itemName,
                giverCharacter,
                forceComplete: !quantity.IsAll && receiver == Hero.MainHero,
                mutationObservation: mutationObservation);
        }
        if (actual <= 0)
        {
            return false;
        }
        factText = "部队已实际交付物品 " + (itemName ?? lookup)
            + " ×" + actual.ToString(CultureInfo.InvariantCulture) + "。";
        return true;
    }

    private static EconomyRewardDebtReplayResult ReplayPartyFailure(string errorCode)
    {
        return new EconomyRewardDebtReplayResult(
            EconomyRewardDebtReplayStatus.RejectedByMainThreadValidation,
            0,
            Array.Empty<FactRecord>(),
            errorCode);
    }
}
