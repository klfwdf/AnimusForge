namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for pending native aftermath replacement.
/// It preserves the fused runtime rule that mercy can downgrade reversible plunder,
/// while devastate-grade outcomes remain irreversible.
/// </summary>
public static class SiegeAftermathSelectionPolicy
{
    public static int GetSeverity(SiegeAftermathResolutionKind aftermath)
    {
        return aftermath switch
        {
            SiegeAftermathResolutionKind.Devastate => 3,
            SiegeAftermathResolutionKind.Pillage => 2,
            SiegeAftermathResolutionKind.ShowMercy => 1,
            _ => 0,
        };
    }

    public static bool IsDevastateOrWorse(SiegeAftermathResolutionKind aftermath)
    {
        return GetSeverity(aftermath) >= GetSeverity(SiegeAftermathResolutionKind.Devastate);
    }

    public static bool ShouldReturnSharedReliefPool(SiegeAftermathResolutionKind aftermath)
    {
        return aftermath == SiegeAftermathResolutionKind.Pillage
            || aftermath == SiegeAftermathResolutionKind.Devastate;
    }

    public static bool CanDowngradeReversiblePlunder(
        SiegeAftermathResolutionKind requestedAftermath,
        SiegeAftermathResolutionKind currentPendingAftermath,
        bool hasPendingAftermath,
        bool massacreStarted,
        bool culturalRepopulationRequested)
    {
        return requestedAftermath == SiegeAftermathResolutionKind.ShowMercy
            && !massacreStarted
            && !culturalRepopulationRequested
            && (!hasPendingAftermath || GetSeverity(currentPendingAftermath) < GetSeverity(SiegeAftermathResolutionKind.Devastate));
    }

    public static bool ShouldReplacePendingAftermath(
        SiegeAftermathResolutionKind requestedAftermath,
        SiegeAftermathResolutionKind currentPendingAftermath,
        bool hasPendingAftermath,
        bool massacreStarted,
        bool culturalRepopulationRequested)
    {
        if (!hasPendingAftermath)
        {
            return true;
        }

        return CanDowngradeReversiblePlunder(
                requestedAftermath,
                currentPendingAftermath,
                hasPendingAftermath,
                massacreStarted,
                culturalRepopulationRequested)
            || GetSeverity(requestedAftermath) >= GetSeverity(currentPendingAftermath);
    }
}
