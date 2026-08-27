using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.ExpeditionParade;

internal sealed class ExpeditionParadeMissionBehavior : MissionLogic
{
	private static readonly string[] RelevantTokens =
	{
		"parade", "spawn", "gate", "door", "passage", "exit", "boundary", "lord", "keep", "village", "center"
	};

	private const string SpawnAnchorTag = "af_parade_spawn";
	private const string GateAnchorTag = "af_parade_gate";
	private const string ExitAnchorTag = "af_parade_exit";
	private const int MaxLoggedCandidates = 80;
	private const int MaxBoundarySamples = 32;

	private readonly ExpeditionParadeSession _session;
	private bool _probeCompleted;
	private bool _completionNotified;

	internal ExpeditionParadeMissionBehavior(ExpeditionParadeSession session)
	{
		_session = session ?? throw new ArgumentNullException(nameof(session));
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_probeCompleted || base.Mission?.Scene == null || base.Mission.CurrentTime < 0.5f)
		{
			return;
		}
		string liveLocationId = CampaignMission.Current?.Location?.StringId;
		if (!string.IsNullOrWhiteSpace(_session.LocationId)
			&& !string.IsNullOrWhiteSpace(liveLocationId)
			&& !string.Equals(_session.LocationId, liveLocationId, StringComparison.OrdinalIgnoreCase))
		{
			_probeCompleted = true;
			Logger.Log("ExpeditionParade", "Probe skipped because mission location changed. id=" + _session.SessionId
				+ ", expected=" + _session.LocationId + ", actual=" + liveLocationId);
			ExpeditionParadeRuntime.Complete(_session, "mission_location_mismatch");
			return;
		}

		Agent main = Agent.Main ?? base.Mission.MainAgent;
		if (main == null || !main.IsActive())
		{
			return;
		}

		_probeCompleted = true;
		RunReadOnlyProbe(main);
	}

	protected override void OnEndMission()
	{
		if (!_completionNotified)
		{
			_completionNotified = true;
			ExpeditionParadeRuntime.Complete(_session, "mission_behavior_end");
		}
		base.OnEndMission();
	}

	private void RunReadOnlyProbe(Agent main)
	{
		Scene scene = base.Mission.Scene;
		try
		{
			List<GameEntity> entities = new();
			scene.GetEntities(ref entities);
			List<EntityProbe> candidates = entities
				.Where(IsRelevantEntity)
				.Select(entity => BuildEntityProbe(scene, main, entity))
				.OrderBy(probe => probe.PathReachable ? 0 : 1)
				.ThenBy(probe => probe.PathDistance)
				.ThenBy(probe => probe.Name, StringComparer.OrdinalIgnoreCase)
				.Take(MaxLoggedCandidates)
				.ToList();

			scene.GetSceneLimits(out Vec3 sceneMin, out Vec3 sceneMax);
			Logger.Log("ExpeditionParade", "Probe summary. id=" + _session.SessionId
				+ ", scene=" + Safe(scene.GetName())
				+ ", location=" + Safe(CampaignMission.Current?.Location?.StringId)
				+ ", entities=" + entities.Count
				+ ", agents=" + (base.Mission.Agents?.Count ?? 0)
				+ ", navFaces=" + SafeInt(scene.GetNavMeshFaceCount)
				+ ", hardBoundary=" + SafeInt(scene.GetHardBoundaryVertexCount)
				+ ", softBoundary=" + SafeInt(scene.GetSoftBoundaryVertexCount)
				+ ", sceneMin=" + Format(sceneMin)
				+ ", sceneMax=" + Format(sceneMax)
				+ ", main=" + Format(main.Position)
				+ ", roster=" + _session.Roster.BuildDiagnosticSummary());

			for (int index = 0; index < candidates.Count; index++)
			{
				EntityProbe candidate = candidates[index];
				Logger.Log("ExpeditionParade", "Probe candidate[" + index + "] " + candidate);
			}

			BoundaryProbe boundary = ProbeBoundaryExit(scene, main);
			Logger.Log("ExpeditionParade", "Probe boundary " + boundary);

			bool explicitRouteReady = ValidateExplicitRoute(scene, main, out string explicitRouteDetail);
			Logger.Log("ExpeditionParade", "Probe route status=" + (explicitRouteReady ? "explicit_route_valid" : "runtime_evidence_required")
				+ ", detail=" + explicitRouteDetail);

			InformationManager.DisplayMessage(new InformationMessage(explicitRouteReady
				? "出征阅兵路线探针完成：本场景的三个专用锚点已连通。当前阶段仍不会生成士兵。"
				: "出征阅兵路线探针完成：诊断信息已写入 AF 日志。需要据此确认门口、城门/村口和退出区。"));
		}
		catch (Exception ex)
		{
			Logger.Log("ExpeditionParade", "Stage-0 scene probe failed. id=" + _session.SessionId + ", error=" + ex);
			InformationManager.DisplayMessage(new InformationMessage("出征阅兵路线探针失败，详情已写入 AF 日志。"));
		}
	}

	private static bool IsRelevantEntity(GameEntity entity)
	{
		if (entity == null)
		{
			return false;
		}
		if (ContainsRelevantToken(entity.Name))
		{
			return true;
		}
		if (entity.Tags != null && entity.Tags.Any(ContainsRelevantToken))
		{
			return true;
		}
		try
		{
			return entity.GetScriptComponents().Any(script => ContainsRelevantToken(script?.GetType().Name));
		}
		catch
		{
			return false;
		}
	}

	private static bool ContainsRelevantToken(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string normalized = value.ToLowerInvariant();
		return RelevantTokens.Any(normalized.Contains);
	}

	private static EntityProbe BuildEntityProbe(Scene scene, Agent main, GameEntity entity)
	{
		Vec3 position = entity.GlobalPosition;
		bool pathReachable = TryMeasurePath(scene, main.Position, position, out float pathDistance);
		string scripts;
		try
		{
			scripts = string.Join(",", entity.GetScriptComponents().Select(script => script?.GetType().Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct());
		}
		catch
		{
			scripts = string.Empty;
		}
		return new EntityProbe(
			entity.Name,
			entity.Tags == null ? string.Empty : string.Join(",", entity.Tags),
			scripts,
			position,
			pathReachable,
			pathDistance);
	}

	private static BoundaryProbe ProbeBoundaryExit(Scene scene, Agent main)
	{
		int softCount = SafeInt(scene.GetSoftBoundaryVertexCount);
		int hardCount = SafeInt(scene.GetHardBoundaryVertexCount);
		bool useSoft = softCount > 0;
		int count = useSoft ? softCount : hardCount;
		if (count <= 0)
		{
			return BoundaryProbe.Missing("scene_has_no_boundary_vertices");
		}

		int step = Math.Max(1, (int)Math.Ceiling((double)count / MaxBoundarySamples));
		BoundaryProbe best = BoundaryProbe.Missing("no_reachable_boundary_sample");
		for (int index = 0; index < count; index += step)
		{
			Vec2 vertex;
			try
			{
				vertex = useSoft ? scene.GetSoftBoundaryVertex(index) : scene.GetHardBoundaryVertex(index);
			}
			catch
			{
				continue;
			}

			Vec2 direction = main.Position.AsVec2 - vertex;
			float length = direction.Length;
			if (length < 0.1f)
			{
				continue;
			}
			direction /= length;
			Vec2 inner = vertex + direction * 3f;
			Vec3 candidate = main.Position;
			candidate.x = inner.x;
			candidate.y = inner.y;
			if (!TryMeasurePath(scene, main.Position, candidate, out float pathDistance))
			{
				continue;
			}
			if (!best.Reachable || pathDistance > best.PathDistance)
			{
				best = new BoundaryProbe(true, useSoft ? "soft" : "hard", index, candidate, pathDistance, "reachable_inner_band");
			}
		}
		return best;
	}

	private static bool ValidateExplicitRoute(Scene scene, Agent main, out string detail)
	{
		GameEntity spawn = scene.FindEntityWithTag(SpawnAnchorTag);
		GameEntity gate = scene.FindEntityWithTag(GateAnchorTag);
		GameEntity exit = scene.FindEntityWithTag(ExitAnchorTag);
		if (spawn == null || gate == null || exit == null)
		{
			detail = "anchors spawn=" + (spawn != null) + ",gate=" + (gate != null) + ",exit=" + (exit != null)
				+ "; expected tags=" + SpawnAnchorTag + "," + GateAnchorTag + "," + ExitAnchorTag;
			return false;
		}

		bool spawnToGate = TryMeasurePath(scene, spawn.GlobalPosition, gate.GlobalPosition, out float firstDistance);
		bool gateToExit = TryMeasurePath(scene, gate.GlobalPosition, exit.GlobalPosition, out float secondDistance);
		bool mainToSpawn = TryMeasurePath(scene, main.Position, spawn.GlobalPosition, out float entryDistance);
		detail = "main_spawn=" + FormatPath(mainToSpawn, entryDistance)
			+ ",spawn_gate=" + FormatPath(spawnToGate, firstDistance)
			+ ",gate_exit=" + FormatPath(gateToExit, secondDistance);
		return mainToSpawn && spawnToGate && gateToExit;
	}

	private static bool TryMeasurePath(Scene scene, Vec3 source, Vec3 destination, out float pathDistance)
	{
		pathDistance = float.MaxValue;
		try
		{
			WorldPosition sourceWorld = new(scene, source);
			WorldPosition destinationWorld = new(scene, destination);
			if (sourceWorld.GetNavMesh() == UIntPtr.Zero || destinationWorld.GetNavMesh() == UIntPtr.Zero)
			{
				return false;
			}
			return scene.GetPathDistanceBetweenPositions(ref sourceWorld, ref destinationWorld, 0.45f, out pathDistance);
		}
		catch
		{
			return false;
		}
	}

	private static int SafeInt(Func<int> getter)
	{
		try
		{
			return getter();
		}
		catch
		{
			return -1;
		}
	}

	private static string Safe(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Replace('\r', ' ').Replace('\n', ' ');
	}

	private static string Format(Vec3 value)
	{
		return value.x.ToString("0.###") + "," + value.y.ToString("0.###") + "," + value.z.ToString("0.###");
	}

	private static string FormatPath(bool reachable, float distance)
	{
		return reachable ? "ok:" + distance.ToString("0.##") : "blocked";
	}

	private sealed class EntityProbe
	{
		internal EntityProbe(string name, string tags, string scripts, Vec3 position, bool pathReachable, float pathDistance)
		{
			Name = Safe(name);
			Tags = Safe(tags);
			Scripts = Safe(scripts);
			Position = position;
			PathReachable = pathReachable;
			PathDistance = pathDistance;
		}

		internal string Name { get; }
		internal string Tags { get; }
		internal string Scripts { get; }
		internal Vec3 Position { get; }
		internal bool PathReachable { get; }
		internal float PathDistance { get; }

		public override string ToString()
		{
			return "name=" + Name + ",tags=" + Tags + ",scripts=" + Scripts + ",pos=" + Format(Position)
				+ ",path=" + FormatPath(PathReachable, PathDistance);
		}
	}

	private sealed class BoundaryProbe
	{
		internal BoundaryProbe(bool reachable, string kind, int index, Vec3 position, float pathDistance, string reason)
		{
			Reachable = reachable;
			Kind = kind;
			Index = index;
			Position = position;
			PathDistance = pathDistance;
			Reason = reason;
		}

		internal bool Reachable { get; }
		internal string Kind { get; }
		internal int Index { get; }
		internal Vec3 Position { get; }
		internal float PathDistance { get; }
		internal string Reason { get; }

		internal static BoundaryProbe Missing(string reason)
		{
			return new BoundaryProbe(false, "none", -1, Vec3.Invalid, float.MaxValue, reason);
		}

		public override string ToString()
		{
			return "reachable=" + Reachable + ",kind=" + Kind + ",index=" + Index
				+ ",pos=" + (Reachable ? Format(Position) : "N/A")
				+ ",path=" + FormatPath(Reachable, PathDistance) + ",reason=" + Reason;
		}
	}
}
