namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for ordering inspected soldiers to kill the regular
/// prisoners brought into an AF troop-inspection mission.
/// AF owns semantic postprocessing and Bannerlord owns the live mission fight.
/// </summary>
public static class TroopInspectionPrisonerSlaughterProfile
{
    public const string ActionTag = "[ACTION:TROOP_INSPECTION_SLAUGHTER_PRISONERS]";

    public const string StartSource = "troop_inspection_ai_prisoner_slaughter";

    public const uint StartMessageColor = 0xFFFF7777u;

    public const uint CompletionMessageColor = 0xFFB6F7A8u;

    public const uint WarningMessageColor = 0xFFFFD27Fu;

    public static bool ShouldOfferRule(
        bool inspectionActive,
        bool externalCastleRuntime,
        bool slaughterActive,
        bool speakerIsInspectedRegularSoldier,
        int regularPrisonerCount,
        int attackerCount)
    {
        return inspectionActive
            && !externalCastleRuntime
            && !slaughterActive
            && speakerIsInspectedRegularSoldier
            && regularPrisonerCount > 0
            && attackerCount > 0;
    }

    public static string BuildRuntimeInstruction(int regularPrisonerCount, int attackerCount)
        => BuildRuntimeInstruction(regularPrisonerCount, attackerCount, false);

    public static string BuildRuntimeInstruction(int regularPrisonerCount, int attackerCount, bool isEnglish)
        => BuildRuntimeInstruction(regularPrisonerCount, attackerCount, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildRuntimeInstruction(int regularPrisonerCount, int attackerCount, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.InspectionSlaughterPromptTemplate, ("prisoners", ClampCount(regularPrisonerCount).ToString()), ("soldiers", ClampCount(attackerCount).ToString()));
    }

    public static string BuildPostprocessRuleDescription(int regularPrisonerCount, int attackerCount)
        => BuildPostprocessRuleDescription(regularPrisonerCount, attackerCount, false);

    public static string BuildPostprocessRuleDescription(int regularPrisonerCount, int attackerCount, bool isEnglish)
        => BuildPostprocessRuleDescription(regularPrisonerCount, attackerCount, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildPostprocessRuleDescription(int regularPrisonerCount, int attackerCount, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        return TownPromptTextCatalog.FormatTemplate(text.InspectionSlaughterRuleDescription, ("prisoners", ClampCount(regularPrisonerCount).ToString()), ("soldiers", ClampCount(attackerCount).ToString()));
    }

    public static string BuildStartedMessage(int attackerCount, int targetCount)
        => BuildStartedMessage(attackerCount, targetCount, false);

    public static string BuildStartedMessage(int attackerCount, int targetCount, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        return text.GetRuntimeText("BuildStartedMessage_001")
            + ClampCount(attackerCount)
            + text.GetRuntimeText("BuildStartedMessage_002")
            + ClampCount(targetCount)
            + text.GetRuntimeText("BuildStartedMessage_003");

    }

    public static string BuildCompletedMessage(int killedCount)
        => BuildCompletedMessage(killedCount, false);

    public static string BuildCompletedMessage(int killedCount, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        return text.GetRuntimeText("BuildCompletedMessage_001")
            + ClampCount(killedCount)
            + text.GetRuntimeText("BuildCompletedMessage_002");

    }

    public static string BuildUnavailableMessage(string reason)
        => BuildUnavailableMessage(reason, false);

    public static string BuildUnavailableMessage(string reason, bool isEnglish)
    {
        TownPromptTextCatalog text = TownPromptCatalogResources.Get(isEnglish);
        string norm = (reason ?? string.Empty).Trim();
        return norm switch
        {
            "not_normal_inspection" => text.GetRuntimeText("BuildUnavailableMessage_002"),
            "speaker_not_inspected_soldier" => text.GetRuntimeText("BuildUnavailableMessage_004"),
            "no_regular_prisoners" => text.GetRuntimeText("BuildUnavailableMessage_006"),
            "slaughter_already_active" => text.GetRuntimeText("BuildUnavailableMessage_008"),
            "combat_setup_unavailable" => text.GetRuntimeText("BuildUnavailableMessage_010"),
            _ => text.GetRuntimeText("BuildUnavailableMessage_011")
        };

    }

    private static int ClampCount(int value)
    {
        return value < 0 ? 0 : value;
    }
}
