using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed partial class WorldBulletinStateOwner
{
    private WorldBulletinSaveState _newsArchiveState;
    private WorldNewsFactArchive _newsArchive;

    private WorldNewsFactArchive EnsureNewsArchive()
    {
        var state = EnsureWorldBulletinState();
        if (ReferenceEquals(_newsArchiveState, state) && _newsArchive != null) return _newsArchive;
        state.NewsFacts ??= new List<WorldNewsFact>();
        _newsArchive = new WorldNewsFactArchive(state.NewsFacts);
        _newsArchiveState = state;
        if (!state.NewsArchiveImported)
        {
            foreach (var fact in state.Events)
                if (fact != null && WorldNewsFactArchive.Durable(fact.Kind, fact.Score, fact.InvolvesPlayer))
                    _newsArchive.Upsert(ToNewsFact(fact));
            // Import only explicitly frozen facts. No title, article, regional model summary,
            // public-history candidate or guessed entity attribution becomes a fact here.
            foreach (var entry in _port?.Records?.Invoke() ?? new List<MyBehavior.EventRecordEntry>())
            {
                foreach (var material in entry?.Materials ?? new List<MyBehavior.EventMaterialReference>())
                {
                    if (material == null || (material.MaterialType != WeeklyReportArchivePolicy.ReportedMaterialType
                        && material.MaterialType != WeeklyReportArchivePolicy.RecentMaterialType)
                        || material.SourceStableKeys?.Count != 1 || string.IsNullOrWhiteSpace(material.ActionKind)
                        || string.IsNullOrWhiteSpace(material.SnapshotText) || !material.ActionDay.HasValue || material.ActionDay.Value < 0) continue;
                    if (_newsArchive.Contains(material.SourceStableKeys[0]))
                    {
                        if (material.MaterialType == WeeklyReportArchivePolicy.ReportedMaterialType)
                            _newsArchive.MarkPublished(material.SourceStableKeys);
                        continue;
                    }
                    // Capture once; existing retained events have stronger participant/group metadata.
                    _newsArchive.Upsert(new WorldNewsFact {
                        Key = material.SourceStableKeys[0], Kind = material.ActionKind, Day = material.ActionDay.Value,
                        Date = material.Label ?? "", Sentence = material.SnapshotText, Score = 40,
                        Published = material.MaterialType == WeeklyReportArchivePolicy.ReportedMaterialType,
                        KingdomIds = string.IsNullOrWhiteSpace(material.KingdomId)
                            || material.KingdomId == WeeklyReportArchivePolicy.OtherKingdomId
                            ? new List<string>() : new List<string> { material.KingdomId },
                        ParticipantIds = (material.RelatedHeroIds ?? new List<string>())
                            .Concat(new[] { material.ActorHeroId, material.TargetHeroId, material.HeroId })
                            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList()
                    });
                }
            }
            state.NewsArchiveImported = true;
        }
        return _newsArchive;
    }

    private static WorldNewsFact ToNewsFact(WorldBulletinEvent fact) => new WorldNewsFact {
        Key = fact.Key, Kind = fact.Kind, Day = fact.Day, Date = fact.GameDate, Score = fact.Score,
        Sentence = fact.Sentence, Detail = fact.Detail, Group = fact.Group,
        KingdomIds = fact.KingdomIds,
        ParticipantIds = (fact.Participants ?? new List<WorldBulletinParticipant>())
            .Where(p => p != null && !string.IsNullOrWhiteSpace(p.HeroId)).Select(p => p.HeroId).ToList()
    };

    private void ArchiveNewsFact(WorldBulletinEvent fact)
    {
        if (fact != null && WorldNewsFactArchive.Durable(fact.Kind, fact.Score, fact.InvolvesPlayer))
            EnsureNewsArchive().Upsert(ToNewsFact(fact));
    }

    internal WorldNewsRecallRequest CaptureNewsRecall(WorldNewsObserver observer, string query, long generation)
        => EnsureNewsArchive().Capture(observer, query, generation);

    private void ArchivePublishedNews(WorldBulletinSelection selection)
    {
        var archive = EnsureNewsArchive();
        foreach (var fact in selection.MajorFacts.Concat(new[] { selection.Major })
            .Concat(selection.Minors.SelectMany(m => m.Events)).Where(f => f != null))
        {
            // Publication itself establishes a durable factual source even for a minor.
            var stored = ToNewsFact(fact);
            stored.Published = true;
            archive.Upsert(stored);
        }
    }
}
