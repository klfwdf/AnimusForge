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

bool current = true;
long staleGeneration = -1;
var completions = new WeeklyFullReportCompletionOwner(() => current, (generation, _) => generation == staleGeneration);
int applied = 0;
Task<bool> first = await Task.Run(() => completions.Enqueue(1, () => { applied++; return true; }));
Task<bool> second = completions.Enqueue(1, () => { applied++; return true; });
Task<bool> third = completions.Enqueue(1, () => { applied++; return true; });
if (first.IsCompleted || second.IsCompleted || third.IsCompleted) throw new InvalidOperationException("enqueue must await main-thread processing");
completions.Process();
if (!await first || !await second || third.IsCompleted) throw new InvalidOperationException("per-tick completion budget changed");
AssertEqual(2, applied, "two completions applied");
completions.Cancel();
if (await third || applied != 2) throw new InvalidOperationException("cancel must settle without applying pending work");

Task<bool> stale = completions.Enqueue(2, () => { applied++; return true; });
staleGeneration = 2;
completions.Process();
if (await stale || applied != 2) throw new InvalidOperationException("stale generation must not apply");
if (await completions.Enqueue(2, () => true)) throw new InvalidOperationException("stale enqueue must reject");

staleGeneration = -1;
Task<bool> oldOwner = completions.Enqueue(3, () => { applied++; return true; });
current = false;
completions.Process();
if (await oldOwner || applied != 2) throw new InvalidOperationException("replaced owner must not apply");
if (await completions.Enqueue(3, () => true)) throw new InvalidOperationException("replaced owner must reject enqueue");

current = true;
Task<bool> failed = completions.Enqueue(4, () => throw new InvalidOperationException("apply failed"));
completions.Process();
try
{
	await failed;
	throw new InvalidOperationException("apply exception must reach awaiting request");
}
catch (InvalidOperationException ex) when (ex.Message == "apply failed") { }

Console.WriteLine("Weekly report schedule policy smoke tests passed.");
