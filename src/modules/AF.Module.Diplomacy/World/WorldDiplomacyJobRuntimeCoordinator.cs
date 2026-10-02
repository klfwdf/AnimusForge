using System;

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

/// <summary>
/// Completion identity and routing for world-diplomacy jobs.
/// Selection is owned by WorldDiplomacyRoundLifecycleRules; the Campaign host keeps
/// all TaleWorlds reads and final game mutations on the owning game thread.
/// </summary>
internal static class WorldDiplomacyJobRuntimeCoordinator
{
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
