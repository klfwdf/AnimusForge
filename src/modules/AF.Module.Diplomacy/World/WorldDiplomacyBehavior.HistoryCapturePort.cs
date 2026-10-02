using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private readonly struct HistoryCapturePort : IWorldDiplomacyHistoryCapturePort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal HistoryCapturePort(WorldDiplomacyBehavior owner) { _owner = owner; }
        public int CurrentHour() => WorldDiplomacyBehavior.CurrentHour();
        public long WeeklyRevision() => MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts()
        {
            foreach (MyBehavior.WorldWeeklyReportHistoryEntry report in MyBehavior.GetPublishedWorldWeeklyReportHistoryForExternal())
                yield return report == null ? null : new WorldDiplomacyWeeklyArtifact(report.SourceId,
                    report.PublishedTitle, report.PublishedReportText, report.CreatedDay, report.CreatedDate);
        }
    }
}
