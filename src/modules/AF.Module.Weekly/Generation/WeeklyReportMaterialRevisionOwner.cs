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
	}

	private readonly Dictionary<int, long> _dayRevisions = new Dictionary<int, long>();
	private long _allRevision;
	private long _openingRevision;

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

	internal Snapshot Capture(int startDay, int endDay)
	{
		int first = Math.Max(0, startDay);
		int length = Math.Max(0, endDay - first + 1);
		long[] revisions = new long[length];
		for (int i = 0; i < revisions.Length; i++)
		{
			_dayRevisions.TryGetValue(first + i, out revisions[i]);
		}
		return new Snapshot { StartDay = first, AllRevision = _allRevision, OpeningRevision = _openingRevision, DayRevisions = revisions };
	}

	internal bool IsCurrent(Snapshot snapshot)
	{
		if (snapshot?.DayRevisions == null || snapshot.AllRevision != _allRevision || snapshot.OpeningRevision != _openingRevision)
		{
			return false;
		}
		for (int i = 0; i < snapshot.DayRevisions.Length; i++)
		{
			_dayRevisions.TryGetValue(snapshot.StartDay + i, out long current);
			if (current != snapshot.DayRevisions[i])
			{
				return false;
			}
		}
		return true;
	}
}
