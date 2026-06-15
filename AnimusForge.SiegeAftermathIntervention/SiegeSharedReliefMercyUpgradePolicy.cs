using System;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free semantic guard that upgrades a plain mercy tag to relief
/// when a civilian reply is clearly talking about citywide shared material aid.
/// AF adapters provide live target identity and shared-pool state.
/// </summary>
public static class SiegeSharedReliefMercyUpgradePolicy
{
    private static readonly string[] SharedReliefCueTerms =
    {
        "救济",
        "安抚",
        "安置",
        "发放",
        "分发",
        "补给",
        "物资",
        "粮",
        "食物",
        "饥",
        "饿",
        "钱",
        "第纳尔",
        "货",
        "原料",
        "供应",
        "商路",
        "商队",
        "市场",
        "工坊",
        "修缮",
        "修理",
        "重建",
        "药",
        "伤员",
        "活路",
        "老人",
        "孩子"
    };

    public static bool ShouldUpgradeMercyToRelief(
        string actionText,
        bool hasSharedReliefPool,
        bool targetIsAlliedSoldier,
        bool targetIsCivilian)
    {
        if (!hasSharedReliefPool
            || targetIsAlliedSoldier
            || !targetIsCivilian
            || string.IsNullOrWhiteSpace(actionText))
        {
            return false;
        }

        var kinds = SiegeActionTagCatalog.ExtractKinds(actionText);
        if (!kinds.Contains(SiegeInterventionActionKind.Mercy)
            || kinds.Contains(SiegeInterventionActionKind.Relief))
        {
            return false;
        }

        return ContainsSharedReliefCue(actionText);
    }

    public static bool ContainsSharedReliefCue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (string term in SharedReliefCueTerms)
        {
            if (!string.IsNullOrWhiteSpace(term)
                && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
