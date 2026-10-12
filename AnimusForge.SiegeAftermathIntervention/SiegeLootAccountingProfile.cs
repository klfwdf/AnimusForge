namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for GCCZ loot accounting.
/// AF adapters still own Bannerlord gold/item mutation, target selection, and display side effects.
/// </summary>
public static class SiegeLootAccountingProfile
{
    public const uint LootMessageColor = 0xFFFFC46Bu;

    public const uint DirectDevastateSettlementMessageColor = 0xFFFF7777u;

    public const uint DirectPlunderSettlementMessageColor = 0xFFFFC46Bu;

    public const string MassacreActionName = "Massacre";
    public const string MassacreActionNameEn = "massacre";

    public const string CulturalRepopulationActionName = "Colonization";
    public const string CulturalRepopulationActionNameEn = "colonization";

    public const string PlunderSettlementLootReason = "Plunder settlement";
    public const string PlunderSettlementLootReasonEn = "pillage settlement";

    public const string MassacreSettlementLootReason = "Massacre settlement";
    public const string MassacreSettlementLootReasonEn = "massacre settlement";

    public const string CivilianHeroFallbackGoldSource = "civilian_hero_fallback";

    public const string CivilianFlatGoldSource = "civilian_flat";

    public const string MarketGoldSource = "market_gold";

    public const int NonHeroPlunderMinGold = 5;

    public const int NonHeroPlunderMaxGold = 9;

    public const int NonHeroMassacreGold = 10;

    public const int HeroMassacreFallbackGold = 3000;

    public const float PlunderMarketInventoryMinRatio = 0.20f;

    public const float PlunderMarketInventoryMaxRatio = 0.50f;

    public const float MassacreMarketInventoryMinRatio = 0.90f;

    public const float MassacreMarketInventoryMaxRatio = 1.00f;

    public static string BuildDirectDevastateSettlementMessage(string actionName)
        => BuildDirectDevastateSettlementMessage(actionName, false);

    public static string BuildDirectDevastateSettlementMessage(string actionName, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Siege aftermath] " + NormalizeActionName(actionName, "Massacre")
                + " has settled as devastation; the town suffers devastation effects. Opening loot screen.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_msg_prefix") + NormalizeActionName(actionName, SiegeAftermathCompatibilityTexts.Raw("loot_action_massacre"))
            + SiegeAftermathCompatibilityTexts.Raw("loot_devastate_suffix");
    }

    public static string BuildDirectPlunderSettlementMessage()
        => BuildDirectPlunderSettlementMessage(false);

    public static string BuildDirectPlunderSettlementMessage(bool isEnglish)
    {
        return isEnglish
            ? "[Siege aftermath] Pillage has settled as plunder; opening loot screen to collect seized supplies."
            : SiegeAftermathCompatibilityTexts.Raw("loot_plunder_msg");
    }

    public static string BuildLootCreditedSummaryMessage(int marketGold, int civilianGold, int itemTotal, int stackKinds)
        => BuildLootCreditedSummaryMessage(marketGold, civilianGold, itemTotal, stackKinds, false);

    public static string BuildLootCreditedSummaryMessage(int marketGold, int civilianGold, int itemTotal, int stackKinds, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Loot tally] Wealth received: Market vault " + ClampNonNegative(marketGold)
                + ", civilian denars " + ClampNonNegative(civilianGold)
                + "; supplies " + ClampNonNegative(itemTotal)
                + " items / " + ClampNonNegative(stackKinds) + " types.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_tally_gold_prefix") + ClampNonNegative(marketGold)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_civilian_gold") + ClampNonNegative(civilianGold)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_supplies") + ClampNonNegative(itemTotal)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_items_sep") + ClampNonNegative(stackKinds)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_kinds_suffix");
    }

    public static string BuildCivilianExitSettlementMessage(int recordedCivilianCount, int lootedCivilianCount, int gainedGold)
        => BuildCivilianExitSettlementMessage(recordedCivilianCount, lootedCivilianCount, gainedGold, false);

    public static string BuildCivilianExitSettlementMessage(int recordedCivilianCount, int lootedCivilianCount, int gainedGold, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Loot tally] Total " + ClampNonNegative(recordedCivilianCount)
                + " civilians recorded; settled remaining " + ClampNonNegative(lootedCivilianCount)
                + " at exit, gained +" + ClampNonNegative(gainedGold) + " denars.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_prefix") + ClampNonNegative(recordedCivilianCount)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_middle") + ClampNonNegative(lootedCivilianCount)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_gained") + ClampNonNegative(gainedGold) + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_suffix");
    }

    public static string BuildCivilianLootMessage(string actorName, string targetName, int amount)
        => BuildCivilianLootMessage(actorName, targetName, amount, false);

    public static string BuildCivilianLootMessage(string actorName, string targetName, int amount, bool isEnglish)
    {
        return (isEnglish ? "[Loot tally] " : SiegeAftermathCompatibilityTexts.Raw("loot_tally_tag")) + BuildCivilianLootLine(actorName, targetName, amount, isEnglish);
    }

    public static string BuildMarketGoldMessage(string reason, int amount)
        => BuildMarketGoldMessage(reason, amount, false);

    public static string BuildMarketGoldMessage(string reason, int amount, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Loot tally] " + NormalizeReason(reason) + " market vault: gained " + ClampNonNegative(amount) + " denars.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_tally_tag") + NormalizeReason(reason) + SiegeAftermathCompatibilityTexts.Raw("loot_tally_market_gold") + ClampNonNegative(amount) + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_suffix");
    }

    public static string BuildMarketInventoryMessage(string reason, int itemTotal, int stackKinds, int itemValue)
        => BuildMarketInventoryMessage(reason, itemTotal, stackKinds, itemValue, false);

    public static string BuildMarketInventoryMessage(string reason, int itemTotal, int stackKinds, int itemValue, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Loot tally] " + NormalizeReason(reason) + " market inventory: seized " + ClampNonNegative(itemTotal)
                + " items (" + ClampNonNegative(stackKinds) + " types, val " + ClampNonNegative(itemValue) + "); collect on loot screen at departure.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_tally_tag") + NormalizeReason(reason) + SiegeAftermathCompatibilityTexts.Raw("loot_tally_market_stock") + ClampNonNegative(itemTotal)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_stock_items") + ClampNonNegative(stackKinds)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_stock_val") + ClampNonNegative(itemValue)
            + SiegeAftermathCompatibilityTexts.Raw("loot_tally_stock_suffix");
    }

    public static string BuildCivilianSpoilsMessage(int gold)
        => BuildCivilianSpoilsMessage(gold, false);

    public static string BuildCivilianSpoilsMessage(int gold, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[Loot tally] Civilian spoils: obtained " + ClampNonNegative(gold) + " denars.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("loot_tally_civilian_prop") + ClampNonNegative(gold) + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_suffix");
    }

    public static string BuildCivilianLootLine(string actorName, string targetName, int amount)
        => BuildCivilianLootLine(actorName, targetName, amount, false);

    public static string BuildCivilianLootLine(string actorName, string targetName, int amount, bool isEnglish)
    {
        string safeTargetName = string.IsNullOrWhiteSpace(targetName) ? (isEnglish ? "target" : SiegeAftermathCompatibilityTexts.Raw("loot_target_fallback")) : targetName.Trim();
        int safeAmount = ClampNonNegative(amount);
        if (isEnglish)
        {
            return string.IsNullOrWhiteSpace(actorName)
                ? "Obtained " + safeAmount + " denars from " + safeTargetName + "."
                : actorName.Trim() + " questioned " + safeTargetName + " and obtained " + safeAmount + " denars.";
        }
        if (string.IsNullOrWhiteSpace(actorName))
        {
            return SiegeAftermathCompatibilityTexts.Raw("loot_from_prefix") + safeTargetName + SiegeAftermathCompatibilityTexts.Raw("loot_obtained") + safeAmount + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_suffix");
        }
        return actorName.Trim() + SiegeAftermathCompatibilityTexts.Raw("loot_interrogated") + safeTargetName + SiegeAftermathCompatibilityTexts.Raw("loot_after_obtained") + safeAmount + SiegeAftermathCompatibilityTexts.Raw("loot_tally_entry_suffix");
    }

    private static string NormalizeReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
    }

    private static string NormalizeActionName(string actionName, string fallback)
    {
        return string.IsNullOrWhiteSpace(actionName) ? fallback : actionName.Trim();
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }
}
