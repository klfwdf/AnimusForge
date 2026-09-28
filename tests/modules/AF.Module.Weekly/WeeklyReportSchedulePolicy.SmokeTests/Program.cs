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

var schedule = new WeeklyAutoScheduleOwner();
AssertEqual(13111, schedule.SelectWeek(-1, 91777, 13111, false, true, false), "new save selects latest complete week");
AssertEqual(13098, schedule.SelectWeek(13097, 91777, 13111, false, true, false), "existing save catches oldest gap");
AssertEqual(0, schedule.SelectWeek(13097, 91777, 13111, false, false, false), "disabled does not start");
AssertEqual(0, schedule.PendingWeek, "disabled does not defer");
AssertEqual(0, schedule.SelectWeek(13097, 91777, 13111, false, true, true), "rebellion pauses generation");
AssertEqual(13111, schedule.PendingWeek, "rebellion retains latest pending week");
schedule.Defer(13110);
AssertEqual(13111, schedule.PendingWeek, "older deferral cannot replace newer week");
AssertEqual(0, schedule.ResolvePendingWeek(13097, 91777, false, true, true), "active rebellion keeps pending");
AssertEqual(0, schedule.ResolvePendingWeek(13097, 91777, false, false, false), "disabled keeps pending without starting");
AssertEqual(0, schedule.ResolvePendingWeek(13097, 91770, false, true, false), "pending future week waits");
AssertEqual(13098, schedule.ResolvePendingWeek(13097, 91777, false, true, false), "resume catches oldest gap");
AssertEqual(13111, schedule.PendingWeek, "pending remains until generation starts");
AssertEqual(0, schedule.ResolvePendingWeek(13111, 91777, false, true, false), "fully caught up does not resume");
AssertEqual(0, schedule.PendingWeek, "fully caught up clears pending");
schedule.Defer(2);
schedule.Clear();
AssertEqual(0, schedule.PendingWeek, "load reset clears transient deferral");

bool current = true;
long staleGeneration = -1;
var completions = new WeeklyFullReportCompletionOwner(() => current, (generation, _) => generation == staleGeneration);
int applied = 0;
Task<bool> first = await Task.Factory.StartNew(() => completions.Enqueue(1, () => { applied++; return true; }));
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
