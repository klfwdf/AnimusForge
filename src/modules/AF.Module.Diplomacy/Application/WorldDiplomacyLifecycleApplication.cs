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
        orchestration.NormalizeStorage(allowWorldValidation: true);
        orchestration.RecoverUnsettledAiInternationalReputation();
        orchestration.RecoverPlayerCourtReceiptsFromKnowledge();
        orchestration.EnsureScheduleInitialized();
        source.ResetTransientRuntime(lifecycle == WorldDiplomacyLifecycleEvent.Loaded ? "game-loaded" : "session-launched");
        orchestration.ReconcileActiveDiplomacyAfterLoad();
    }
}
