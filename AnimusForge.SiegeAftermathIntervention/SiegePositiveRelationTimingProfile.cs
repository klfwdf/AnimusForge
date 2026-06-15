namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for delayed positive notable/headman relation rewards.
/// AF adapters queue positive relation deltas during the scene and apply them only
/// after the final settlement aftermath still resolves through the mercy/positive track.
/// </summary>
public static class SiegePositiveRelationTimingProfile
{
    public const string FinalMercyOnlyReason = "gccz_positive_relations_final_mercy_only";

    public static bool ShouldApplyQueuedPositiveRelations(SiegeAftermathResolutionKind finalAftermath)
    {
        return finalAftermath == SiegeAftermathResolutionKind.ShowMercy;
    }
}
