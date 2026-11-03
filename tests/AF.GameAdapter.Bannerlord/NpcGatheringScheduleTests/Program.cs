using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("FAIL: " + message); Console.WriteLine("PASS " + message); }

// 1. First due days spread over [now, now+interval) instead of all landing on now+interval.
var ids = Enumerable.Range(0, 60).Select(i => "kingdom_" + i).ToList();
var first = ids.Select(id => NpcGatheringSchedulePolicy.FirstDueDay(id, 1000, 21)).ToList();
Check(first.All(d => d >= 1000 && d < 1021), "first due day lies in [now, now + interval)");
Check(first.Select(d => (int)Math.Floor(d)).Distinct().Count() >= 15, "60 kingdoms first seen together spread over many days");
Check(first.GroupBy(d => (int)Math.Floor(d)).Max(g => g.Count()) <= 8, "no single day collects most kingdoms");
Check(NpcGatheringSchedulePolicy.FirstDueDay("wre_kingdom", 1000, 21) == NpcGatheringSchedulePolicy.FirstDueDay("wre_kingdom", 1000, 21),
    "first due day is stable for the same kingdom");

// 2. Next due day jitters around the interval.
Check(NpcGatheringSchedulePolicy.NextDueDay(0, 21, 0.0) == 21 * 0.75 && Math.Abs(NpcGatheringSchedulePolicy.NextDueDay(0, 21, 0.999999) - 21 * 1.25) < 0.001,
    "next interval stays within ±25%");
Check(NpcGatheringSchedulePolicy.NextDueDay(0, 21, -5) >= 21 * 0.75 && NpcGatheringSchedulePolicy.NextDueDay(0, 21, 5) <= 21 * 1.25, "out-of-range draws are clamped");

// 3. Caps: 6 new per daily tick and 6 concurrent NPC gatherings.
Check(NpcGatheringSchedulePolicy.MaxNewPerDay == 6 && NpcGatheringSchedulePolicy.MaxConcurrentNpc == 6, "caps are 6 per day and 6 concurrent");
Check(NpcGatheringSchedulePolicy.RemainingSlots(0, 0) == 6, "empty world allows 6");
Check(NpcGatheringSchedulePolicy.RemainingSlots(0, 6) == 0, "daily cap stops the 7th creation");
Check(NpcGatheringSchedulePolicy.RemainingSlots(4, 0) == 2, "concurrent cap leaves room for 2 when 4 run");
Check(NpcGatheringSchedulePolicy.RemainingSlots(6, 0) == 0 && NpcGatheringSchedulePolicy.RemainingSlots(9, 0) == 0, "no slot at or above the concurrent cap");

// 4. Simulated lock-step world (the 29-at-once case): 29 kingdoms all due today.
int active = 0, created = 0;
var deferred = new List<int>();
for (int k = 0; k < 29; k++)
{
    if (NpcGatheringSchedulePolicy.RemainingSlots(active, created) <= 0) { deferred.Add(k); continue; }
    created++; active++;
}
Check(created == 6 && deferred.Count == 23, "a 29-kingdom burst opens 6 and defers the rest to later days");

Console.WriteLine("PASS " + checks + " npc gathering schedule assertions");
