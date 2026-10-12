namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for GCCZ scene-entry UI wording.
/// AF adapters still resolve Bannerlord settlements, locations, menu args, and display side effects.
/// </summary>
public static class SiegeInterventionEntryProfile
{
    public const string SettlementCenterLocationId = "center";


    public const int DefaultAutoSummonCount = 50;

    public const int MaxSummonPerAction = 50;

    public const uint MissingSceneMessageColor = 0xFFFF7777u;

    public const uint EntryInstructionMessageColor = 0xFFB6F7A8u;

    public const uint SelectionConfirmedMessageColor = 0xFFB6F7A8u;

    public const uint SelectionFallbackMessageColor = 0xFFFFD27Fu;

    public const uint BattleEquipmentAppliedMessageColor = 0xFFB6F7A8u;

    public const uint NoHealthyTroopsMessageColor = 0xFFFFD27Fu;

    public const uint SummonedTroopsMessageColor = 0xFFB6F7A8u;

    public const string EntryMenuOptionText = "Decide in person at town";
    public const string EntryMenuOptionTextEn = "Decide in person at town";
    
    public const string SelectionUnavailableMissionSource = "selection_unavailable";
    public const string TroopSelectionDoneMissionSource = "game_menu_troop_selection_done";
    public const string SceneEntryCleanupSource = "siege_intervention_enter_isolated_scene";
    public const string AutoEnterSummonSource = "auto_enter";
    public const string EnsureAlliedTroopsSummonSource = "ensure";

    public static readonly string EnabledTooltip = "Do not immediately settle aftermath affairs; enter the town in battle gear leading about " + DefaultAutoSummonCount + " healthy troops, while civilians remain in the streets, deciding reassurance, mercy, plunder, or massacre through dialogue or action.";
    public static readonly string EnabledTooltipEn = "Do not immediately settle aftermath affairs; enter the town in battle gear leading about " + DefaultAutoSummonCount + " healthy troops, while civilians remain in the streets, deciding reassurance, mercy, plunder, or massacre through dialogue or action.";

    public const string MissingSceneTooltip = "No victorious siege settlement scene is currently available to enter.";
    public const string MissingSceneTooltipEn = "No victorious siege settlement scene is currently available to enter.";
    
    public const string EntryMemoryTitle = "Entry disposition";
    public const string EntryMemoryTitleEn = "Entry disposition";
    
    public const string MissingSceneMessage = "[Siege aftermath] No captured settlement scene is currently available to enter.";
    public const string MissingSceneMessageEn = "[Siege aftermath] No captured settlement scene is currently available to enter.";
    
    public const string DecisionPolicyMessage = "[Siege aftermath] Disposition is decided by you on-site: departing directly settles as plunder; explicit mercy, reassurance, or relief protects civilians; massacre or colonization may be ordered.";
    public const string DecisionPolicyMessageEn = "[Siege aftermath] Disposition is decided by you on-site: departing directly settles as plunder; explicit mercy, reassurance, or relief protects civilians; massacre or colonization may be ordered.";

    public const string EntryFailedMessage = "[Siege aftermath] Temporarily unable to enter the captured settlement scene.";
    public const string EntryFailedMessageEn = "[Siege aftermath] Temporarily unable to enter the captured settlement scene.";
    
    public const string SelectionFallbackMessage = "[Siege aftermath] No followers selected; healthy regular troops will be brought automatically.";
    public const string SelectionFallbackMessageEn = "[Siege aftermath] No followers selected; healthy regular troops will be brought automatically.";

    public const string BattleEquipmentAppliedMessage = "[Siege aftermath] You entered the city in battle gear.";
    public const string BattleEquipmentAppliedMessageEn = "[Siege aftermath] You entered the city in battle gear.";
    
    public const string NoHealthyTroopsMessage = "[Siege aftermath] The main party has no healthy soldiers or companions eligible to enter.";
    public const string NoHealthyTroopsMessageEn = "[Siege aftermath] The main party has no healthy soldiers or companions eligible to enter.";
    
    public static string BuildEntryMemoryText(string settlementName)
        => BuildEntryMemoryText(settlementName, false);

    public static string BuildEntryMemoryText(string settlementName, bool isEnglish)
    {
        string safeSettlementName = string.IsNullOrWhiteSpace(settlementName)
            ? (isEnglish ? "this newly captured settlement" : SiegeAftermathCompatibilityTexts.Raw("entry_settlement_default"))
            : settlementName.Trim();
        if (isEnglish)
        {
            return "The player captured " + safeSettlementName + " and entered in person to oversee post-siege order; defeated garrison and lords fell, and civilians, merchants, artisans, headmen, and notables should recognize they are in a victor disposition scene, while retaining their cultural and personal memories.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("entry_memory_prefix") + safeSettlementName + SiegeAftermathCompatibilityTexts.Raw("entry_memory_suffix");
    }

    public static bool IsSupportedSettlementKind(bool isTown, bool isCastle)
    {
        return isTown || isCastle;
    }

    public static string BuildEnabledTooltip(bool isCastle)
        => BuildEnabledTooltip(isCastle, false);

    public static string BuildEnabledTooltip(bool isCastle, bool isEnglish)
    {
        if (isCastle)
        {
            return isEnglish
                ? "{=!}Do not settle immediately; you will select up to 50 allied troops and 200 prisoners, then enter the castle war scene to handle aftermath in person."
                : SiegeAftermathCompatibilityTexts.Raw("entry_tooltip_castle");
        }
        return isEnglish ? EnabledTooltipEn : EnabledTooltip;
    }

    public static string BuildDecisionPolicyMessage(bool isCastle)
        => BuildDecisionPolicyMessage(isCastle, false);

    public static string BuildDecisionPolicyMessage(bool isCastle, bool isEnglish)
    {
        return isCastle
            ? SiegeCastleRosterSelectionProfile.BuildDecisionPolicyMessage(isEnglish)
            : (isEnglish ? DecisionPolicyMessageEn : DecisionPolicyMessage);
    }

    public static string[] GetPreferredLocationIds(bool isTown, bool isCastle)
    {
        return isTown || isCastle ? new[] { SettlementCenterLocationId } : System.Array.Empty<string>();
    }

    public static string BuildTroopSelectionInstructionMessage(int maxCount)
        => BuildTroopSelectionInstructionMessage(maxCount, false);

    public static string BuildTroopSelectionInstructionMessage(int maxCount, bool isEnglish)
    {
        int safeMaxCount = maxCount < 0 ? 0 : maxCount;
        if (isEnglish)
        {
            return "[Siege aftermath] Select up to " + safeMaxCount + " followers/companions to enter; unselected slots automatically bring healthy soldiers.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("entry_selection_instr_prefix") + safeMaxCount + SiegeAftermathCompatibilityTexts.Raw("entry_selection_instr_suffix");
    }

    public static string BuildSelectionConfirmedMessage(int selectedCount)
        => BuildSelectionConfirmedMessage(selectedCount, false);

    public static string BuildSelectionConfirmedMessage(int selectedCount, bool isEnglish)
    {
        int safeSelectedCount = selectedCount < 0 ? 0 : selectedCount;
        if (isEnglish)
        {
            return "[Siege aftermath] Selected " + safeSelectedCount + " followers to enter the town.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("entry_selected_summary_prefix") + safeSelectedCount + SiegeAftermathCompatibilityTexts.Raw("entry_selected_summary_suffix");
    }

    public static string BuildSummonedTroopsMessage(int spawnedCount)
        => BuildSummonedTroopsMessage(spawnedCount, false);

    public static string BuildSummonedTroopsMessage(int spawnedCount, bool isEnglish)
    {
        int safeSpawnedCount = spawnedCount < 0 ? 0 : spawnedCount;
        if (isEnglish)
        {
            return "[Siege aftermath] Brought " + safeSpawnedCount + " escort soldiers/companions, grouped and following in formation by default.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("entry_spawned_prefix") + safeSpawnedCount + SiegeAftermathCompatibilityTexts.Raw("entry_spawned_suffix");
    }
}
