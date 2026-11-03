using System;

namespace AnimusForge;

/// <summary>
/// NPC-hosted gathering pacing. Kingdoms first seen on the same day used to share one due date and then stay in
/// lock-step (29 gatherings and ~100 travel orders in one daily tick after a world-book import). Due dates are now
/// spread per kingdom, intervals jitter, and the daily tick opens at most <see cref="MaxNewPerDay"/> NPC gatherings
/// while at most <see cref="MaxConcurrentNpc"/> NPC gatherings run. Player-hosted gatherings are never counted.
/// Pure arithmetic so the contract test can run without the game.
/// </summary>
internal static class NpcGatheringSchedulePolicy
{
	internal const int MaxNewPerDay = 6;
	internal const int MaxConcurrentNpc = 6;
	// Next interval = base * (1 ± JitterFraction).
	internal const double JitterFraction = 0.25;

	/// <summary>First due day for a kingdom seen for the first time: now + [0, interval), stable per kingdom id.</summary>
	internal static double FirstDueDay(string kingdomId, double now, int intervalDays)
	{
		int interval = Math.Max(1, intervalDays);
		return now + StableUnit(kingdomId) * interval;
	}

	/// <summary>Due day after a gathering is created; unit01 is a uniform draw in [0,1).</summary>
	internal static double NextDueDay(double now, int intervalDays, double unit01)
	{
		double interval = Math.Max(1, intervalDays);
		double factor = 1.0 - JitterFraction + 2.0 * JitterFraction * Clamp01(unit01);
		return now + interval * factor;
	}

	/// <summary>How many NPC gatherings this daily tick may still open.</summary>
	internal static int RemainingSlots(int activeNpcGatherings, int createdToday)
	{
		int byConcurrent = MaxConcurrentNpc - Math.Max(0, activeNpcGatherings);
		int byDay = MaxNewPerDay - Math.Max(0, createdToday);
		return Math.Max(0, Math.Min(byConcurrent, byDay));
	}

	/// <summary>Deterministic [0,1) from a string (FNV-1a), independent of process and runtime hash seeds.</summary>
	internal static double StableUnit(string key)
	{
		unchecked
		{
			uint hash = 2166136261;
			foreach (char c in key ?? "")
			{
				hash = (hash ^ c) * 16777619;
			}
			// murmur3 finalizer: ids differing only in a trailing digit ("kingdom_1", "kingdom_2") must not cluster.
			hash ^= hash >> 16;
			hash *= 0x85ebca6b;
			hash ^= hash >> 13;
			hash *= 0xc2b2ae35;
			hash ^= hash >> 16;
			return (hash >> 8) / (double)0x01000000;
		}
	}

	private static double Clamp01(double value) => value < 0 ? 0 : value >= 1 ? 0.999999 : value;
}
