using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal sealed class WorldDiplomacyWeeklyArtifact
{
    internal string SourceId { get; }
    internal string Title { get; }
    internal string Text { get; }
    internal int Day { get; }
    internal string Date { get; }
    internal WorldDiplomacyWeeklyArtifact(string sourceId, string title, string text, int day, string date)
    { SourceId = sourceId; Title = title; Text = text; Day = day; Date = date; }
}

internal interface IWorldDiplomacyHistoryCapturePort
{
    void EnsureInitialized();
    int CurrentHour();
    long WeeklyRevision();
    IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts();
    void AppendWeekly(WorldDiplomacyWeeklyArtifact artifact);
    void SyncPolicyArtifacts(int maxBatches);
    void RetryDeferredEntries();
    void SyncSources(bool force);
    string Render(long throughSequence);
}

internal static class WorldDiplomacyHistoryCaptureApplication
{
    internal static void EnsureInitialized(WorldDiplomacyStorage storage, ref bool initialized,
        HashSet<string> sourceKeys, long tokenLimit, Func<string, int> estimateTokens, Action invalidate, Action<string> log)
    {
        if (initialized && storage?.CanonicalHistory?.Snapshot != null && storage.CanonicalHistory.DeltaEntries != null) return;
        WorldDiplomacyStorageMigration.NormalizeCanonicalHistoryState(storage, sourceKeys, tokenLimit, estimateTokens, invalidate, log);
        initialized = true;
    }

    internal static void SyncSources<TPort>(ref TPort port, bool force, ref int lastSyncHour, ref long observedWeeklyRevision,
        int forcePolicyBatches) where TPort : IWorldDiplomacyHistoryCapturePort
    {
        port.EnsureInitialized();
        int hour = port.CurrentHour();
        if (!force && lastSyncHour == hour) return;
        long revision = port.WeeklyRevision();
        if (observedWeeklyRevision != revision)
        {
            foreach (WorldDiplomacyWeeklyArtifact artifact in port.WeeklyArtifacts()) port.AppendWeekly(artifact);
            observedWeeklyRevision = revision;
        }
        port.SyncPolicyArtifacts(force ? forcePolicyBatches : 1);
        lastSyncHour = hour;
    }

    internal static void Capture<TPort>(WorldDiplomacyStorage storage, WorldDiplomacyJob job, bool syncSources,
        long throughSequence, ref TPort port) where TPort : IWorldDiplomacyHistoryCapturePort
    {
        if (job == null) return;
        if (syncSources)
        {
            port.RetryDeferredEntries();
            port.SyncSources(true);
        }
        port.EnsureInitialized();
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        job.HistoryThroughSequence = WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, throughSequence);
        WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(job, history, port.Render(job.HistoryThroughSequence));
    }
}
