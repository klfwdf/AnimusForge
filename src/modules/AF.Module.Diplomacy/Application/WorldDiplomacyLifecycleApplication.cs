namespace AnimusForge;

internal interface IWorldDiplomacyLifecycleSource
{
    bool StartAtPeace { get; }
    void ResetTransientRuntime(string reason);
}

internal static class WorldDiplomacyLifecycleApplication
{
    internal static void Run<TSource>(WorldDiplomacyLifecycleEvent lifecycle, IWorldDiplomacyOrchestration orchestration,
        ref TSource source) where TSource : struct, IWorldDiplomacyLifecycleSource
    {
        if (orchestration == null) return;
        if (lifecycle == WorldDiplomacyLifecycleEvent.NewGame)
        {
            orchestration.ResetStorageForNewGame(source.StartAtPeace);
            orchestration.EnsureScheduleInitialized();
            source.ResetTransientRuntime("new-game");
            return;
        }
        // Normalization restores runtime indexes, including player reply deadlines.
        // Clear the previous session before rebuilding them, never after.
        source.ResetTransientRuntime(lifecycle == WorldDiplomacyLifecycleEvent.Loaded ? "game-loaded" : "session-launched");
        orchestration.NormalizeStorage(allowWorldValidation: true);
        orchestration.RecoverPlayerCourtReceiptsFromKnowledge();
        orchestration.EnsureScheduleInitialized();
        orchestration.ReconcileActiveDiplomacyAfterLoad();
    }
}
