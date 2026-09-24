using System;

namespace AnimusForge;

// Transient scheduling state; the persisted generated-week cursor remains on MyBehavior.
internal sealed class WeeklyAutoScheduleOwner
{
	private int _pendingWeek;

	internal int PendingWeek => _pendingWeek;

	internal int SelectWeek(int lastGeneratedWeek, int currentDay, int currentWeek,
		bool generationInProgress, bool enabled, bool rebellionActive)
	{
		int missingWeek = WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(lastGeneratedWeek, currentDay);
		if (currentWeek <= 0 || missingWeek <= 0 || missingWeek > currentWeek || generationInProgress || !enabled)
		{
			return 0;
		}
		if (rebellionActive)
		{
			Defer(currentWeek);
			return 0;
		}
		return missingWeek;
	}

	internal int ResolvePendingWeek(int lastGeneratedWeek, int currentDay,
		bool generationInProgress, bool enabled, bool rebellionActive)
	{
		if (_pendingWeek <= 0 || generationInProgress || rebellionActive || !enabled
			|| currentDay <= 0 || currentDay / 7 < _pendingWeek)
		{
			return 0;
		}
		int missingWeek = WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(lastGeneratedWeek, currentDay);
		if (missingWeek <= 0)
		{
			Clear();
		}
		return missingWeek;
	}

	internal void Defer(int weekIndex)
	{
		_pendingWeek = Math.Max(_pendingWeek, weekIndex);
	}

	internal void Clear()
	{
		_pendingWeek = 0;
	}
}
