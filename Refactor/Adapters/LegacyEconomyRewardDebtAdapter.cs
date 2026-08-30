using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Projects the current legacy action protocol into the Economy/Reward/Debt
/// capability boundary. This adapter validates only detached syntax and
/// capability membership. It never resolves or mutates a game object.
/// </summary>
public sealed class LegacyEconomyRewardDebtAdapter : IEconomyRewardDebtReplayPlanner
{
    public static bool IsEconomyAction(ActionRequest request)
    {
        return request != null && IsEconomyActionTag(request.Tag);
    }

    public static bool IsEconomyActionTag(string tag)
    {
        switch ((tag ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "ACTION:GIVE_ASSET":
            case "ACTION:GIVE_GOLD":
            case "ACTION:GIVE_ITEM":
            case "ACTION:SETTLEMENT_TRANSFER":
            case "AD":
            case "ADP":
                return true;
            default:
                return false;
        }
    }

    public static CapabilitySet CreateAllCapabilities()
    {
        return new CapabilitySet(new[]
        {
            EconomyRewardDebtCapabilityIds.GiveAsset,
            EconomyRewardDebtCapabilityIds.GiveGold,
            EconomyRewardDebtCapabilityIds.DebtCreate,
            EconomyRewardDebtCapabilityIds.DebtResolve,
            EconomyRewardDebtCapabilityIds.SettlementTransfer
        });
    }

    public EconomyRewardDebtReplayPlan Plan(ActionPlan actionPlan, CapabilitySet capabilities)
    {
        List<EconomyRewardDebtAction> actions = new List<EconomyRewardDebtAction>();
        List<string> exclusions = new List<string>();
        if (actionPlan == null)
        {
            exclusions.Add("economy.action_plan_missing");
            return new EconomyRewardDebtReplayPlan(actions, exclusions);
        }

        HashSet<string> granted = new HashSet<string>(
            (capabilities?.CapabilityIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.OrdinalIgnoreCase);
        foreach (ActionRequest request in actionPlan.Actions ?? Array.Empty<ActionRequest>())
        {
            if (!TryProject(request, out EconomyRewardDebtAction action, out string reason))
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    exclusions.Add(reason);
                }
                continue;
            }
            if (!granted.Contains(action.CapabilityId))
            {
                exclusions.Add("economy.capability_missing:" + action.CapabilityId);
                continue;
            }
            actions.Add(action);
        }
        return new EconomyRewardDebtReplayPlan(actions, exclusions.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static bool TryProject(ActionRequest request, out EconomyRewardDebtAction action, out string reason)
    {
        action = null;
        reason = string.Empty;
        if (request == null)
        {
            reason = "economy.action_missing";
            return false;
        }

        string tag = (request.Tag ?? string.Empty).Trim().ToUpperInvariant();
        switch (tag)
        {
            case "ACTION:GIVE_ASSET":
            {
                string quantity = Get(request, "quantity");
                if (string.IsNullOrWhiteSpace(request.TargetId) || !IsPositiveQuantity(quantity))
                {
                    reason = "economy.give_asset.invalid_asset_or_quantity";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveAsset, tag, request.TargetId,
                    request.TargetId, quantity, string.Empty, string.Empty, string.Empty,
                    string.Empty, EconomyRewardDebtCapabilityIds.GiveAsset);
                return true;
            }
            case "ACTION:GIVE_GOLD":
            {
                string amount = GetFirst(request, "amount", "arg0", "arg1");
                if (string.IsNullOrWhiteSpace(amount))
                {
                    // Legacy prompt files also use [ACTION:GIVE_GOLD:amount],
                    // where the parser stores the single amount in TargetId.
                    amount = request.TargetId;
                }
                if (!IsPositiveInteger(amount))
                {
                    reason = "economy.give_gold.invalid_amount";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveGold, tag, request.TargetId,
                    "GOLD", amount, amount, string.Empty, string.Empty,
                    string.Empty, EconomyRewardDebtCapabilityIds.GiveGold);
                return true;
            }
            case "ACTION:GIVE_ITEM":
            {
                string quantity = GetFirst(request, "quantity", "arg0", "arg1");
                if (string.IsNullOrWhiteSpace(request.TargetId) || !IsPositiveQuantity(quantity))
                {
                    reason = "economy.give_item.invalid_item_or_quantity";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.GiveAsset, tag, request.TargetId,
                    request.TargetId, quantity, string.Empty, string.Empty, string.Empty,
                    string.Empty, EconomyRewardDebtCapabilityIds.GiveAsset);
                return true;
            }
            case "AD":
            {
                string amount = request.TargetId;
                string days = Get(request, "arg0");
                string debtorKind = Get(request, "arg1");
                if (!IsPositiveInteger(amount) || !IsPositiveInteger(days)
                    || !(string.Equals(debtorKind, "N", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(debtorKind, "P", StringComparison.OrdinalIgnoreCase)))
                {
                    reason = "economy.debt_create.invalid_amount_days_or_kind";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.DebtCreate, tag, request.TargetId,
                    string.Empty, string.Empty, amount, string.Empty, string.Empty,
                    debtorKind, EconomyRewardDebtCapabilityIds.DebtCreate,
                    days, Get(request, "arg2"));
                return true;
            }
            case "ADP":
                if (string.IsNullOrWhiteSpace(request.TargetId))
                {
                    reason = "economy.debt_resolve.missing_debt_id";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.DebtResolve, tag, request.TargetId,
                    string.Empty, string.Empty, string.Empty, request.TargetId, string.Empty,
                    string.Empty, EconomyRewardDebtCapabilityIds.DebtResolve);
                return true;
            case "ACTION:SETTLEMENT_TRANSFER":
            {
                string direction = Get(request, "direction");
                if (string.IsNullOrWhiteSpace(direction))
                {
                    direction = request.TargetId;
                }
                string settlement = GetFirst(request, "settlement", "arg1", "arg2");
                if (string.IsNullOrWhiteSpace(settlement))
                {
                    settlement = string.Empty;
                }
                if (string.IsNullOrWhiteSpace(settlement))
                {
                    reason = "economy.settlement_transfer.missing_settlement_token";
                    return false;
                }
                action = new EconomyRewardDebtAction(
                    EconomyRewardDebtActionKind.SettlementTransfer, tag, request.TargetId,
                    string.Empty, string.Empty, string.Empty, string.Empty, settlement,
                    direction, EconomyRewardDebtCapabilityIds.SettlementTransfer);
                return true;
            }
            default:
                // Non-economy actions are not errors. The caller can pass the
                // exclusion reason to diagnostics and let another domain plan
                // the same ActionPlan.
                reason = "economy.action_not_applicable:" + tag;
                return false;
        }
    }

    private static string Get(ActionRequest request, string key)
    {
        return request.Parameters.TryGetValue(key, out string value) ? (value ?? string.Empty).Trim() : string.Empty;
    }

    private static string GetFirst(ActionRequest request, params string[] keys)
    {
        foreach (string key in keys ?? Array.Empty<string>())
        {
            string value = Get(request, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return string.Empty;
    }

    private static bool IsPositiveInteger(string value)
    {
        return long.TryParse((value ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out long parsed)
            && parsed > 0;
    }

    private static bool IsPositiveQuantity(string value)
    {
        return string.Equals((value ?? string.Empty).Trim(), "ALL", StringComparison.OrdinalIgnoreCase)
            || IsPositiveInteger(value);
    }
}
