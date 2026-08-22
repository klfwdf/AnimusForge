using AnimusForge;

static void AssertEqual(int expected, int actual, string scenario)
{
	if (expected != actual)
	{
		throw new InvalidOperationException($"{scenario}: expected {expected}, actual {actual}");
	}
}

AssertEqual(0, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(-1, 6), "no completed week yet");
AssertEqual(1, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(-1, 7), "first weekly boundary");
AssertEqual(13111, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(-1, 91777), "new installation starts from latest week");
AssertEqual(13098, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(13097, 91777), "late save catches oldest gap");
AssertEqual(13099, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(13098, 91777), "catch-up advances one week at a time");
AssertEqual(0, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(13111, 91777), "fully caught up");
AssertEqual(0, WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(13112, 91777), "future cursor does not schedule");
AssertEqual(91679, WeeklyReportSchedulePolicy.GetStartDay(13098), "historical week start");
AssertEqual(91685, WeeklyReportSchedulePolicy.GetEndDay(13098), "historical week end");

Console.WriteLine("Weekly report schedule policy smoke tests passed.");
