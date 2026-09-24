using System;

namespace AnimusForge;

internal static class WeeklyReportSchedulePolicy
{
	public static int ResolveOldestMissingWeek(int lastGeneratedWeek, int currentGameDayIndex)
	{
		if (currentGameDayIndex <= 0)
		{
			return 0;
		}
		int latestCompletedWeek = currentGameDayIndex / 7;
		if (latestCompletedWeek <= 0 || lastGeneratedWeek >= latestCompletedWeek)
		{
			return 0;
		}
		// A save that has never generated an automatic report keeps the legacy cursor at -1.
		// Start it at the latest completed week instead of replaying the entire campaign history.
		return (lastGeneratedWeek < 1) ? latestCompletedWeek : (lastGeneratedWeek + 1);
	}

	public static int GetStartDay(int weekIndex)
	{
		return (weekIndex <= 0) ? 0 : ((weekIndex - 1) * 7);
	}

	public static int GetEndDay(int weekIndex)
	{
		return (weekIndex <= 0) ? 0 : (weekIndex * 7 - 1);
	}
}
