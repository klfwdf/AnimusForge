using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WeeklyReportMaterialRevisionOwner
{
	internal sealed class Snapshot
	{
		internal int StartDay;

		internal long AllRevision;

		internal long OpeningRevision;

		internal long[] DayRevisions;
        internal long[] AppendRevisions;
        internal bool FrozenAppends;
        internal long RuntimeRevision;
	}

	private readonly Dictionary<int, long> _dayRevisions = new Dictionary<int, long>();
    private readonly Dictionary<int, long> _appendRevisions = new Dictionary<int, long>();
    internal void MarkAppend(int day) { if (day >= 0) { _appendRevisions.TryGetValue(day, out long revision); _appendRevisions[day] = revision + 1; } }
	private long _allRevision;
	private long _openingRevision;
    private long _runtimeRevision;
    // Live world changes and retention do not rewrite already detached publication facts.
    // Explicit imports/edits still use MarkAll/MarkDay/MarkOpening.
    internal void MarkRuntimeChange() => _runtimeRevision++;

	internal void MarkDay(int day)
	{
		if (day >= 0)
		{
			_dayRevisions.TryGetValue(day, out long revision);
			_dayRevisions[day] = revision + 1L;
		}
	}

	internal void MarkAll() => _allRevision++;

	internal void MarkOpening() => _openingRevision++;

    internal Snapshot Capture(int startDay, int endDay) => CaptureForCollection(startDay, endDay, false);
	internal Snapshot CaptureForCollection(int startDay, int endDay, bool frozenAppends)
	{
		int first = Math.Max(0, startDay);
		int length = Math.Max(0, endDay - first + 1);
		long[] revisions = new long[length];
        long[] appends = new long[length];
		for (int i = 0; i < revisions.Length; i++)
		{
			_dayRevisions.TryGetValue(first + i, out revisions[i]);
            _appendRevisions.TryGetValue(first + i, out appends[i]);
		}
		return new Snapshot { StartDay = first, AllRevision = _allRevision, OpeningRevision = _openingRevision, DayRevisions = revisions, AppendRevisions = appends, FrozenAppends = frozenAppends, RuntimeRevision = _runtimeRevision };
	}

	internal bool IsCurrent(Snapshot snapshot)
	{
		if (snapshot?.DayRevisions == null || snapshot.AllRevision != _allRevision || snapshot.OpeningRevision != _openingRevision)
		{
			return false;
		}
        if (!snapshot.FrozenAppends && snapshot.RuntimeRevision != _runtimeRevision) return false;
		for (int i = 0; i < snapshot.DayRevisions.Length; i++)
		{
			_dayRevisions.TryGetValue(snapshot.StartDay + i, out long current);
			if (current != snapshot.DayRevisions[i])
			{
				return false;
			}
            _appendRevisions.TryGetValue(snapshot.StartDay + i, out long appended);
            if (!snapshot.FrozenAppends && appended != (snapshot.AppendRevisions?[i] ?? 0)) return false;
		}
		return true;
	}
}
