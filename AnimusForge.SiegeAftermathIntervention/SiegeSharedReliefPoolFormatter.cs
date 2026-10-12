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

    public const string ReturnedToPlayerMemoryTitle = "Returned";
    public const string ReturnedToPlayerMemoryTitleEn = "Returned";

    public const string UnavailableStatsDescription = "Shared relief stats unavailable";
    public const string UnavailableStatsDescriptionEn = "Shared relief stats unavailable";

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
        => DescribeForContext(facts, false);

    public static string DescribeForContext(SiegeSharedReliefPoolFacts facts, bool isEnglish)
    {
        if (facts == null || (facts.Gold <= 0 && facts.FoodUnits <= 0 && facts.ItemTotal <= 0))
        {
            return isEnglish ? "No shared supplies delivered through AF yet" : SiegeAftermathCompatibilityTexts.Raw("pool_empty");
        }

        var parts = new List<string>();
        if (facts.Gold > 0)
        {
            parts.Add(facts.Gold + (isEnglish ? " denars" : SiegeAftermathCompatibilityTexts.Raw("pool_denars")));
        }

        if (facts.FoodUnits > 0)
        {
            parts.Add(facts.FoodUnits + (isEnglish ? " food units" : SiegeAftermathCompatibilityTexts.Raw("pool_food_units")));
        }

        int nonFoodItems = facts.ItemTotal - facts.FoodUnits;
        if (nonFoodItems > 0)
        {
            parts.Add(isEnglish
                ? nonFoodItems + " non-food items, value " + facts.ItemValue
                : nonFoodItems + SiegeAftermathCompatibilityTexts.Raw("pool_non_food") + facts.ItemValue);
        }

        return parts.Count > 0
            ? string.Join(isEnglish ? ", " : "；", parts)
            : (isEnglish ? "No shared supplies delivered through AF yet" : SiegeAftermathCompatibilityTexts.Raw("pool_empty"));
    }

    public static string BuildReturnedToPlayerMessage(string summary)
        => BuildReturnedToPlayerMessage(summary, false);

    public static string BuildReturnedToPlayerMessage(string summary, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Siege aftermath] Pillage/massacre triggered; previously handed-over shared relief supplies returned to you: "
                + NormalizeReturnSummary(summary, true) + ".";
        }
        return SiegeAftermathCompatibilityTexts.Raw("pool_returned_msg_prefix") + NormalizeReturnSummary(summary, false) + SiegeAftermathCompatibilityTexts.Raw("pool_returned_msg_suffix");
    }

    public static string BuildAppliedEffectMessage(string poolDescription)
        => BuildAppliedEffectMessage(poolDescription, false);

    public static string BuildAppliedEffectMessage(string poolDescription, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Siege aftermath] Shared supplies delivered through AF included in this reassurance settlement: "
                + NormalizePoolDescription(poolDescription, true) + ".";
        }
        return SiegeAftermathCompatibilityTexts.Raw("pool_applied_msg_prefix") + NormalizePoolDescription(poolDescription, false) + SiegeAftermathCompatibilityTexts.Raw("pool_applied_msg_suffix");
    }

    public static string BuildCapturedTransferMessage(string transferSummary)
        => BuildCapturedTransferMessage(transferSummary, false);

    public static string BuildCapturedTransferMessage(string transferSummary, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Siege aftermath] Delivered " + NormalizeTransferSummary(transferSummary, true) + " recorded into town-wide civilian relief supplies.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("pool_transfer_msg_prefix") + NormalizeTransferSummary(transferSummary, false) + SiegeAftermathCompatibilityTexts.Raw("pool_transfer_msg_suffix");
    }

    public static string BuildGoldAmountText(int goldAmount)
        => BuildGoldAmountText(goldAmount, false);

    public static string BuildGoldAmountText(int goldAmount, bool isEnglish)
    {
        return goldAmount > 0 ? goldAmount + (isEnglish ? " denars" : SiegeAftermathCompatibilityTexts.Raw("pool_denars")) : string.Empty;
    }

    public static string BuildItemAmountText(int itemAmount, string itemNameOrId)
        => BuildItemAmountText(itemAmount, itemNameOrId, false);

    public static string BuildItemAmountText(int itemAmount, string itemNameOrId, bool isEnglish)
    {
        return itemAmount > 0 ? itemAmount + (isEnglish ? " " : SiegeAftermathCompatibilityTexts.Raw("pool_item_unit")) + NormalizeItemName(itemNameOrId, isEnglish) : string.Empty;
    }

    public static string JoinAmountParts(params string[] parts)
        => JoinAmountParts(false, parts);

    public static string JoinAmountParts(bool isEnglish, params string[] parts)
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

        return string.Join(isEnglish ? ", " : "、", normalizedParts);
    }

    public static string BuildReturnedToPlayerMemoryText(string summary)
        => BuildReturnedToPlayerMemoryText(summary, false);

    public static string BuildReturnedToPlayerMemoryText(string summary, bool isEnglish)
    {
        if (isEnglish)
        {
            return "Previously delivered civilian relief supplies returned due to negative disposition; return details: "
                + NormalizeReturnSummary(summary, true) + ".";
        }
        return SiegeAftermathCompatibilityTexts.Raw("pool_memory_prefix") + NormalizeReturnSummary(summary, false) + SiegeAftermathCompatibilityTexts.Raw("pool_memory_suffix");
    }

    public static string BuildReturnedGoldSource(string reason)
    {
        return ReturnedGoldSourcePrefix + (reason ?? NegativeOutcomeFallbackReason);
    }

    private static string NormalizeReturnSummary(string summary)
        => NormalizeReturnSummary(summary, false);

    private static string NormalizeReturnSummary(string summary, bool isEnglish)
    {
        return string.IsNullOrWhiteSpace(summary) ? (isEnglish ? "no details" : SiegeAftermathCompatibilityTexts.Raw("pool_no_details")) : summary.Trim();
    }

    private static string NormalizePoolDescription(string poolDescription)
        => NormalizePoolDescription(poolDescription, false);

    private static string NormalizePoolDescription(string poolDescription, bool isEnglish)
    {
        return string.IsNullOrWhiteSpace(poolDescription) ? (isEnglish ? "No shared supplies delivered through AF yet" : SiegeAftermathCompatibilityTexts.Raw("pool_empty")) : poolDescription.Trim();
    }

    private static string NormalizeTransferSummary(string transferSummary)
        => NormalizeTransferSummary(transferSummary, false);

    private static string NormalizeTransferSummary(string transferSummary, bool isEnglish)
    {
        return string.IsNullOrWhiteSpace(transferSummary) ? (isEnglish ? "supplies" : SiegeAftermathCompatibilityTexts.Raw("pool_supplies")) : transferSummary.Trim();
    }

    private static string NormalizeItemName(string itemNameOrId)
        => NormalizeItemName(itemNameOrId, false);

    private static string NormalizeItemName(string itemNameOrId, bool isEnglish)
    {
        return string.IsNullOrWhiteSpace(itemNameOrId) ? (isEnglish ? "supplies" : SiegeAftermathCompatibilityTexts.Raw("pool_supplies")) : itemNameOrId.Trim();
    }
}
