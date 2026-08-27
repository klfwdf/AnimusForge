using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.ExpeditionParade.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AnimusForge.ExpeditionParade.Routing;

internal enum ParadeAnchorKind
{
	Spawn,
	Assembly,
	Gate,
	Exit
}

internal enum ParadeAnchorSource
{
	Override,
	VerifiedSceneEntity,
	NativeSpawnPoint,
	NavigationDiscovery,
	BoundaryDiscovery
}

internal enum ParadeRouteSegmentKind
{
	SpawnToAssembly,
	Street,
	GateApproach,
	GatePassage,
	OutsideRoad,
	ExitZone
}

internal sealed class ParadeRouteContext
{
	internal ParadeRouteContext(
		Scene scene,
		string sceneName,
		ParadeSettlementKind settlementKind,
		string sceneVariant,
		string gameVersion,
		string plannerVersion,
		string overrideRevision,
		bool containsMountedTroops)
	{
		Scene = scene ?? throw new ArgumentNullException(nameof(scene));
		SceneName = Require(sceneName, nameof(sceneName));
		SettlementKind = settlementKind;
		SceneVariant = sceneVariant ?? string.Empty;
		GameVersion = Require(gameVersion, nameof(gameVersion));
		PlannerVersion = Require(plannerVersion, nameof(plannerVersion));
		OverrideRevision = overrideRevision ?? string.Empty;
		ContainsMountedTroops = containsMountedTroops;
		CacheKey = string.Join("|", SceneName, SettlementKind, SceneVariant, GameVersion, PlannerVersion, OverrideRevision);
	}

	internal Scene Scene { get; }

	internal string SceneName { get; }

	internal ParadeSettlementKind SettlementKind { get; }

	internal string SceneVariant { get; }

	internal string GameVersion { get; }

	internal string PlannerVersion { get; }

	internal string OverrideRevision { get; }

	internal bool ContainsMountedTroops { get; }

	internal string CacheKey { get; }

	private static string Require(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Value is required.", parameterName);
		}
		return value;
	}
}

internal sealed class ParadeAnchorCandidate
{
	internal ParadeAnchorCandidate(string id, ParadeAnchorKind kind, ParadeAnchorSource source, Vec3 position, float confidence, bool supportsMounted)
	{
		Id = string.IsNullOrWhiteSpace(id) ? kind + "_unnamed" : id;
		Kind = kind;
		Source = source;
		Position = position;
		Confidence = float.IsNaN(confidence) || float.IsInfinity(confidence)
			? 0f
			: Math.Max(0f, Math.Min(1f, confidence));
		SupportsMounted = supportsMounted;
	}

	internal string Id { get; }

	internal ParadeAnchorKind Kind { get; }

	internal ParadeAnchorSource Source { get; }

	internal Vec3 Position { get; }

	internal float Confidence { get; }

	internal bool SupportsMounted { get; }
}

internal sealed class ParadeAnchorSet
{
	internal ParadeAnchorSet(IEnumerable<ParadeAnchorCandidate> anchors)
	{
		Anchors = (anchors ?? Enumerable.Empty<ParadeAnchorCandidate>())
			.Where(anchor => anchor != null)
			.GroupBy(anchor => anchor.Kind + ":" + anchor.Id, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(anchor => anchor.Source == ParadeAnchorSource.Override)
				.ThenByDescending(anchor => anchor.Confidence).First())
			.ToArray();
	}

	internal IReadOnlyList<ParadeAnchorCandidate> Anchors { get; }

	internal IReadOnlyList<ParadeAnchorCandidate> OfKind(ParadeAnchorKind kind)
	{
		return Anchors.Where(anchor => anchor.Kind == kind).OrderByDescending(anchor => anchor.Confidence).ToArray();
	}

	internal ParadeAnchorSet Merge(ParadeAnchorSet other)
	{
		return other == null ? this : new ParadeAnchorSet(Anchors.Concat(other.Anchors));
	}
}

internal sealed class ParadeRouteSegment
{
	internal ParadeRouteSegment(
		ParadeRouteSegmentKind kind,
		IEnumerable<Vec3> centerLine,
		int recommendedColumns,
		float speedMultiplier,
		bool allowsMounted,
		bool triggersCrowd,
		bool allowsRemoval)
	{
		Kind = kind;
		CenterLine = (centerLine ?? Enumerable.Empty<Vec3>()).ToArray();
		if (CenterLine.Count == 0)
		{
			throw new ArgumentException("A route segment requires at least one point.", nameof(centerLine));
		}
		RecommendedColumns = Math.Max(1, recommendedColumns);
		if (speedMultiplier <= 0f || float.IsNaN(speedMultiplier) || float.IsInfinity(speedMultiplier))
		{
			throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
		}
		SpeedMultiplier = speedMultiplier;
		AllowsMounted = allowsMounted;
		TriggersCrowd = triggersCrowd;
		AllowsRemoval = allowsRemoval;
	}

	internal ParadeRouteSegmentKind Kind { get; }

	internal IReadOnlyList<Vec3> CenterLine { get; }

	internal int RecommendedColumns { get; }

	internal float SpeedMultiplier { get; }

	internal bool AllowsMounted { get; }

	internal bool TriggersCrowd { get; }

	internal bool AllowsRemoval { get; }
}

internal sealed class ParadeExitZone
{
	internal ParadeExitZone(Vec3 center, float radius, bool isBoundaryInnerBand)
	{
		if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
		{
			throw new ArgumentOutOfRangeException(nameof(radius));
		}
		Center = center;
		Radius = radius;
		IsBoundaryInnerBand = isBoundaryInnerBand;
	}

	internal Vec3 Center { get; }

	internal float Radius { get; }

	internal bool IsBoundaryInnerBand { get; }
}

internal sealed class ParadeRoutePlan
{
	internal ParadeRoutePlan(
		string candidateId,
		ParadeAnchorCandidate spawn,
		ParadeAnchorCandidate gate,
		ParadeAnchorCandidate exit,
		IEnumerable<ParadeRouteSegment> segments,
		ParadeExitZone exitZone,
		float candidateScore,
		float minimumPassageWidth,
		bool supportsMounted)
	{
		CandidateId = string.IsNullOrWhiteSpace(candidateId) ? "route_candidate" : candidateId;
		Spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
		Gate = gate ?? throw new ArgumentNullException(nameof(gate));
		Exit = exit ?? throw new ArgumentNullException(nameof(exit));
		Segments = (segments ?? Enumerable.Empty<ParadeRouteSegment>()).ToArray();
		if (Segments.Count == 0)
		{
			throw new ArgumentException("A route plan requires segments.", nameof(segments));
		}
		ExitZone = exitZone ?? throw new ArgumentNullException(nameof(exitZone));
		CandidateScore = float.IsNaN(candidateScore) || float.IsInfinity(candidateScore) ? float.MinValue : candidateScore;
		MinimumPassageWidth = float.IsNaN(minimumPassageWidth) || float.IsInfinity(minimumPassageWidth)
			? 0f
			: Math.Max(0f, minimumPassageWidth);
		SupportsMounted = supportsMounted;
	}

	internal string CandidateId { get; }

	internal ParadeAnchorCandidate Spawn { get; }

	internal ParadeAnchorCandidate Gate { get; }

	internal ParadeAnchorCandidate Exit { get; }

	internal IReadOnlyList<ParadeRouteSegment> Segments { get; }

	internal ParadeExitZone ExitZone { get; }

	internal float CandidateScore { get; }

	internal float MinimumPassageWidth { get; }

	internal bool SupportsMounted { get; }
}

internal sealed class ParadeRouteValidation
{
	internal ParadeRouteValidation(bool accepted, ParadeRoutePlan plan, string code, string detail, bool hiddenAgentWalkTestPassed)
	{
		Accepted = accepted;
		Plan = plan;
		Code = code ?? string.Empty;
		Detail = detail ?? string.Empty;
		HiddenAgentWalkTestPassed = hiddenAgentWalkTestPassed;
	}

	internal bool Accepted { get; }

	internal ParadeRoutePlan Plan { get; }

	internal string Code { get; }

	internal string Detail { get; }

	internal bool HiddenAgentWalkTestPassed { get; }
}

internal sealed class ParadeRouteResolution
{
	private ParadeRouteResolution(bool succeeded, ParadeRoutePlan plan, string source, IReadOnlyList<string> diagnostics)
	{
		Succeeded = succeeded;
		Plan = plan;
		Source = source ?? string.Empty;
		Diagnostics = diagnostics ?? Array.Empty<string>();
	}

	internal bool Succeeded { get; }

	internal ParadeRoutePlan Plan { get; }

	internal string Source { get; }

	internal IReadOnlyList<string> Diagnostics { get; }

	internal static ParadeRouteResolution Success(ParadeRoutePlan plan, string source, IReadOnlyList<string> diagnostics)
	{
		return new ParadeRouteResolution(true, plan ?? throw new ArgumentNullException(nameof(plan)), source, diagnostics);
	}

	internal static ParadeRouteResolution Failure(IReadOnlyList<string> diagnostics)
	{
		return new ParadeRouteResolution(false, null, "none", diagnostics);
	}
}
