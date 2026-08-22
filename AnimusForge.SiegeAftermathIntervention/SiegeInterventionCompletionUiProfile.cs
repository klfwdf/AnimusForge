namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI wording for final GCCZ completion and encounter-exit notifications.
/// AF adapters still own live aftermath labels, loot totals, and Bannerlord display side effects.
/// </summary>
public static class SiegeInterventionCompletionUiProfile
{
    public const string DoneMenuId = "AnimusForge_siege_intervention_done";

    public const string DoneContinueOptionId = "AnimusForge_siege_intervention_done_continue";

    public const uint CompletionMessageColor = 0xFFB6F7A8u;

    public const uint MassacreVictoryMessageColor = 0xFFFF7777u;

    public static string BuildCompletedEncounterMessage(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return BuildCompletedEncounterMessage(aftermathKind, culturalRepopulationApplied, TownActionPresentationTextCatalog.CreateEnglishFallback());
    }

    public static string BuildCompletedEncounterMessage(
        SiegeAftermathResolutionKind aftermathKind,
        bool culturalRepopulationApplied,
        TownActionPresentationTextCatalog text)
    {
        return TownActionPresentationTextCatalog.Resolve(text).BuildCompletedEncounterMessage(aftermathKind, culturalRepopulationApplied);
    }

    public static string GetCompletedEncounterLabel(SiegeAftermathResolutionKind aftermathKind, bool culturalRepopulationApplied)
    {
        return GetCompletedEncounterLabel(aftermathKind, culturalRepopulationApplied, TownActionPresentationTextCatalog.CreateEnglishFallback());
    }

    public static string GetCompletedEncounterLabel(
        SiegeAftermathResolutionKind aftermathKind,
        bool culturalRepopulationApplied,
        TownActionPresentationTextCatalog text)
    {
        return TownActionPresentationTextCatalog.Resolve(text).GetCompletedLabel(aftermathKind, culturalRepopulationApplied);
    }

    public static string BuildLootSettlementSummaryMessage(int marketItemTotal, int marketStackKinds, int marketGold, int civilianGold)
    {
        return BuildLootSettlementSummaryMessage(
            marketItemTotal,
            marketStackKinds,
            marketGold,
            civilianGold,
            TownActionPresentationTextCatalog.CreateEnglishFallback());
    }

    public static string BuildLootSettlementSummaryMessage(
        int marketItemTotal,
        int marketStackKinds,
        int marketGold,
        int civilianGold,
        TownActionPresentationTextCatalog text)
    {
        return TownActionPresentationTextCatalog.Resolve(text).BuildLootSettlementSummary(
            marketItemTotal,
            marketStackKinds,
            marketGold,
            civilianGold);
    }
}
