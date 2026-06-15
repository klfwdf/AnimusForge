using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free shared civilian relief-pool checks and context wording.
/// </summary>
public static class SiegeSharedReliefPoolFormatter
{
    public const uint AppliedEffectMessageColor = 0xFFB6F7A8u;

    public const uint ReturnedToPlayerMessageColor = 0xFFFFD27Fu;

    public const uint CapturedTransferMessageColor = 0xFFB6F7A8u;

    public const string ReturnedToPlayerMemoryTitle = "返还";

    public const string UnavailableStatsDescription = "共享物资统计不可用";

    public const string ReturnedGoldSourcePrefix = "shared_relief_refund_";

    public const string NegativeOutcomeFallbackReason = "negative";

    public static bool HasAnyMaterial(SiegeSharedReliefPoolFacts facts)
    {
        return facts != null
            && (facts.Gold > 0
                || facts.FoodUnits > 0
                || facts.ItemTotal > 0
                || facts.ItemValue > 0);
    }

    public static string DescribeForContext(SiegeSharedReliefPoolFacts facts)
    {
        if (facts == null || (facts.Gold <= 0 && facts.FoodUnits <= 0 && facts.ItemTotal <= 0))
        {
            return "尚未通过AF给予功能交付共享物资";
        }

        var parts = new List<string>();
        if (facts.Gold > 0)
        {
            parts.Add(facts.Gold + " 第纳尔");
        }

        if (facts.FoodUnits > 0)
        {
            parts.Add(facts.FoodUnits + " 份食物");
        }

        int nonFoodItems = facts.ItemTotal - facts.FoodUnits;
        if (nonFoodItems > 0)
        {
            parts.Add(nonFoodItems + " 件非食物物资，估值 " + facts.ItemValue);
        }

        return parts.Count > 0
            ? string.Join("，", parts)
            : "尚未通过AF给予功能交付共享物资";
    }

    public static string BuildReturnedToPlayerMessage(string summary)
    {
        return "【攻城处置】已触发搜掠/血洗等负面处置，先前交给平民共享的物资已退还给你：" + NormalizeReturnSummary(summary) + "。";
    }

    public static string BuildAppliedEffectMessage(string poolDescription)
    {
        return "【攻城处置】已将AF给予的共享物资纳入本次安抚结算：" + NormalizePoolDescription(poolDescription) + "。";
    }

    public static string BuildCapturedTransferMessage(string transferSummary)
    {
        return "【攻城处置】已将给予的" + NormalizeTransferSummary(transferSummary) + "计入全城平民共享安抚物资。";
    }

    public static string BuildGoldAmountText(int goldAmount)
    {
        return goldAmount > 0 ? goldAmount + " 第纳尔" : string.Empty;
    }

    public static string BuildItemAmountText(int itemAmount, string itemNameOrId)
    {
        return itemAmount > 0 ? itemAmount + " 个 " + NormalizeItemName(itemNameOrId) : string.Empty;
    }

    public static string JoinAmountParts(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
        {
            return string.Empty;
        }

        var normalizedParts = new List<string>();
        foreach (string part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                normalizedParts.Add(part.Trim());
            }
        }

        return string.Join("、", normalizedParts);
    }

    public static string BuildReturnedToPlayerMemoryText(string summary)
    {
        return "玩家先前交付的平民共享安抚物资因负面处置被退还；返还内容：" + NormalizeReturnSummary(summary) + "。";
    }

    public static string BuildReturnedGoldSource(string reason)
    {
        return ReturnedGoldSourcePrefix + (reason ?? NegativeOutcomeFallbackReason);
    }

    private static string NormalizeReturnSummary(string summary)
    {
        return string.IsNullOrWhiteSpace(summary) ? "无明细" : summary.Trim();
    }

    private static string NormalizePoolDescription(string poolDescription)
    {
        return string.IsNullOrWhiteSpace(poolDescription) ? "尚未通过AF给予功能交付共享物资" : poolDescription.Trim();
    }

    private static string NormalizeTransferSummary(string transferSummary)
    {
        return string.IsNullOrWhiteSpace(transferSummary) ? "物资" : transferSummary.Trim();
    }

    private static string NormalizeItemName(string itemNameOrId)
    {
        return string.IsNullOrWhiteSpace(itemNameOrId) ? "物资" : itemNameOrId.Trim();
    }
}
