namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Deterministic validation summary for one GCCZ postprocess model response.
/// </summary>
public sealed class SiegePostprocessValidationResult
{
    public SiegePostprocessValidationResult(
        string normalizedTags,
        SiegeInterventionActionKind? selectedTownAction,
        int detectedTownActionCount,
        int rejectedTownActionCount,
        bool usedLegacyTownTagFormat)
    {
        NormalizedTags = normalizedTags ?? string.Empty;
        SelectedTownAction = selectedTownAction;
        DetectedTownActionCount = detectedTownActionCount < 0 ? 0 : detectedTownActionCount;
        RejectedTownActionCount = rejectedTownActionCount < 0 ? 0 : rejectedTownActionCount;
        UsedLegacyTownTagFormat = usedLegacyTownTagFormat;
    }

    public string NormalizedTags { get; }

    public SiegeInterventionActionKind? SelectedTownAction { get; }

    public int DetectedTownActionCount { get; }

    public int RejectedTownActionCount { get; }

    public bool UsedLegacyTownTagFormat { get; }

    public bool HasTownAction => SelectedTownAction.HasValue;

    public bool HadMultipleTownActions => DetectedTownActionCount > 1;
}
