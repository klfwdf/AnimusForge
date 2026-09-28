using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private readonly struct HistoryCapturePort : IWorldDiplomacyHistoryCapturePort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal HistoryCapturePort(WorldDiplomacyBehavior owner) { _owner = owner; }
        public void EnsureInitialized() => _owner.EnsureCanonicalHistoryInitialized();
        public int CurrentHour() => WorldDiplomacyBehavior.CurrentHour();
        public long WeeklyRevision() => MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts()
        {
            foreach (MyBehavior.WorldWeeklyReportHistoryEntry report in MyBehavior.GetPublishedWorldWeeklyReportHistoryForExternal())
                yield return report == null ? null : new WorldDiplomacyWeeklyArtifact(report.SourceId,
                    report.PublishedTitle, report.PublishedReportText, report.CreatedDay, report.CreatedDate);
        }
        public void AppendWeekly(WorldDiplomacyWeeklyArtifact artifact)
        {
            if (artifact == null) return;
            WorldDiplomacyCanonicalHistoryRules.AppendPublishedWorldWeeklyArtifact(
                _owner._storage, _owner._canonicalHistorySourceKeys, GetHistoryCompressionTriggerTokens(),
                _owner.EnsureCanonicalHistoryInitialized, NewId, FormatCampaignDate, Logger.EstimateTokens,
                _owner.InvalidateCanonicalHistoryRenderCache, artifact.SourceId, artifact.Title, artifact.Text, artifact.Day, artifact.Date);
        }
        public void SyncPolicyArtifacts(int count) => _owner.SyncPublishedPolicyArtifacts(count);
        public void RetryDeferredEntries() => _owner.RetryDeferredCanonicalHistoryEntries();
        public void SyncSources(bool force) => _owner.SyncCanonicalHistorySources(force);
        public string Render(long throughSequence) => _owner.BuildCanonicalHistoryBlock(throughSequence);
    }
}
