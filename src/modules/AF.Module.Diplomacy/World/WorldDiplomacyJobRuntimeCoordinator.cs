using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal enum WorldDiplomacyJobRoute
{
	Unknown = 0,
	Generate = 1,
	Analyze = 2,
	Compress = 3,
	RoundPlan = 4,
	RoundCompress = 5
}

internal sealed class WorldDiplomacyJobQueueItem
{
	internal string JobId { get; set; } = "";
	internal int Priority { get; set; }
	internal int CreatedDay { get; set; }
	internal string CacheAffinityKey { get; set; } = "";
	internal bool IsRunning { get; set; }
	internal bool AwaitingHistoryCompression { get; set; }
}

/// <summary>
/// Pure scheduling and completion-routing rules for world-diplomacy jobs.
/// The Campaign host projects persisted jobs into detached queue items and keeps
/// all TaleWorlds reads and final game mutations on the owning game thread.
/// </summary>
internal static class WorldDiplomacyJobRuntimeCoordinator
{
	internal static string SelectNextJobId(
		IEnumerable<WorldDiplomacyJobQueueItem> candidates,
		bool compressionRetryReady,
		string lastCacheAffinityKey)
	{
		List<WorldDiplomacyJobQueueItem> runnable = (candidates ?? Enumerable.Empty<WorldDiplomacyJobQueueItem>())
			.Where(item => item != null
				&& !string.IsNullOrWhiteSpace(item.JobId)
				&& !item.IsRunning
				&& (!item.AwaitingHistoryCompression || compressionRetryReady))
			.ToList();
		if (runnable.Count == 0)
		{
			return "";
		}

		int highestPriority = runnable.Max(item => item.Priority);
		return runnable
			.Where(item => item.Priority == highestPriority)
			.OrderByDescending(item => string.Equals(
				(item.CacheAffinityKey ?? "").Trim(),
				(lastCacheAffinityKey ?? "").Trim(),
				StringComparison.OrdinalIgnoreCase))
			.ThenBy(item => item.CreatedDay)
			.ThenBy(item => item.JobId, StringComparer.OrdinalIgnoreCase)
			.Select(item => item.JobId)
			.FirstOrDefault() ?? "";
	}

	internal static bool IsCurrentCompletion(
		string jobId,
		long completionRuntimeGeneration,
		long currentRuntimeGeneration,
		bool saveRuntimeIsStale)
	{
		return !string.IsNullOrWhiteSpace(jobId)
			&& completionRuntimeGeneration > 0L
			&& completionRuntimeGeneration == currentRuntimeGeneration
			&& !saveRuntimeIsStale;
	}

	internal static WorldDiplomacyJobRoute Classify(string kind)
	{
		switch ((kind ?? "").Trim().ToLowerInvariant())
		{
			case "generate": return WorldDiplomacyJobRoute.Generate;
			case "analyze": return WorldDiplomacyJobRoute.Analyze;
			case "compress": return WorldDiplomacyJobRoute.Compress;
			case "round_plan": return WorldDiplomacyJobRoute.RoundPlan;
			case "round_compress": return WorldDiplomacyJobRoute.RoundCompress;
			default: return WorldDiplomacyJobRoute.Unknown;
		}
	}
}
