using System;
using System.Collections.Generic;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// The camp owns its borrowed spectators and cleared residents throughout a ceremony.
/// A session snapshots the registered camp lists once; the 0.5 s upkeep checks at
/// most 20 walking actors and 20 clearance entries. No mission-wide tick scan or
/// teleport fallback is used. Vengeance creates nothing until the owner reports ready.
/// </summary>
public sealed partial class FieldCampMissionLogic
{
	private const int MaxCeremonyWatchers = 16;
	private const int WatchersPerRow = 8;
	private const int CeremonyTickBudget = 20;
	private const float CeremonyClearRadius = 9f;
	private const float CeremonyPreparationTimeoutSeconds = 55f;
	private const float CeremonyReturnGiveUpSeconds = 60f;
	private const float CeremonyArrivalSquared = 0.81f;

	private sealed class CeremonyMove
	{
		internal Agent Agent;
		internal CampAgent Camp;
		internal bool Prisoner;
		internal bool Spectator;
		internal bool ParkedOutside;
		internal bool Arrived;
		internal bool ReturningHome;
		internal bool CountedWaiting = true;
		internal bool CountedUncleared;
		internal Vec3 Home;
		internal Vec2 HomeFacing;
		internal Vec3 Goal;
		internal Vec2 Facing;
		internal float RetryAt;
		internal Vec3[] Route = Array.Empty<Vec3>();
		internal int RouteIndex;
	}

	// Also read by camp-life upkeep: these entries must not receive work/wander orders.
	private readonly List<CampAgent> _ceremonyMoved = new List<CampAgent>();
	private readonly List<CeremonyMove> _ceremonyWalkers = new List<CeremonyMove>();
	private readonly List<Agent> _ceremonyClearance = new List<Agent>();
	private readonly Dictionary<Agent, CampAgent> _ceremonyResidents = new Dictionary<Agent, CampAgent>();
	private readonly HashSet<Agent> _ceremonyPrisoners = new HashSet<Agent>();
	private readonly Dictionary<Agent, CeremonyMove> _ceremonyMoves = new Dictionary<Agent, CeremonyMove>();
	private readonly Dictionary<Agent, float> _ceremonyRouteRetryAt = new Dictionary<Agent, float>();
	private readonly List<CeremonyMove> _ceremonyWatchers = new List<CeremonyMove>(MaxCeremonyWatchers);
	private readonly Dictionary<long, Vec2> _ceremonyMarks = new Dictionary<long, Vec2>();
	private Agent[] _ceremonyCrowd = Array.Empty<Agent>();
	private int _ceremonyWaiting;
	private int _ceremonyUnclearedMoves;
	private bool _ceremonyClearanceVerified;
	private Guid _ceremonySession;
	private FieldCampCeremonyReadiness _ceremonyReadiness;
	private bool _ceremonyActive;
	private bool _ceremonyStaged;
	private float _ceremonyDeadline;
	private float _ceremonyReturnDeadline = -1f;
	private Vec3 _ceremonyOrigin;
	private Vec2 _ceremonyForward;
	private float _ceremonyRadius;
	private int _ceremonyWalkCursor;
	private int _ceremonyClearCursor;
	private bool _ceremonyClearPassBlocked;
	private float _nextCeremonyProgressLog;

	internal static bool IsExecutionCeremonyAvailable =>
		VengeanceIntegration.IsEnabled && VengeanceIntegration.IsEmbeddedHostActive && RichExecutionApi.IsInitialized;

	internal static void RegisterVengeanceHooks()
	{
		VengeanceIntegration.FieldCampMissionProbe = IsCampMission;
		VengeanceIntegration.FieldCampCeremonyAdmissionProbe = CanStartCeremony;
		VengeanceIntegration.FieldCampCeremonyPreparation = PrepareCeremonyHook;
		VengeanceIntegration.FieldCampCeremonyBorrowedCrowd = BorrowCeremonyCrowdHook;
		VengeanceIntegration.FieldCampCeremonyPreparationReleased = ReleaseCeremonyHook;
		VengeanceIntegration.FieldCampCeremonyStaged = OnCeremonyStagedHook;
		VengeanceIntegration.FieldCampCeremonyReleased = OnCeremonyReleasedHook;
	}

	internal static void UnregisterVengeanceHooks()
	{
		Current?.OnCeremonyReleased();
		VengeanceIntegration.FieldCampMissionProbe = null;
		VengeanceIntegration.FieldCampCeremonyAdmissionProbe = null;
		VengeanceIntegration.FieldCampCeremonyPreparation = null;
		VengeanceIntegration.FieldCampCeremonyBorrowedCrowd = null;
		VengeanceIntegration.FieldCampCeremonyPreparationReleased = null;
		VengeanceIntegration.FieldCampCeremonyStaged = null;
		VengeanceIntegration.FieldCampCeremonyReleased = null;
	}

	private static bool IsCampMission(Mission mission) =>
		mission != null && mission.GetMissionBehavior<FieldCampMissionLogic>() != null;

	private static bool CanStartCeremony(Mission mission)
	{
		var camp = mission?.GetMissionBehavior<FieldCampMissionLogic>();
		return camp != null && camp._spawned && camp._campInitialized && !mission.IsMissionEnding &&
			!camp._ceremonyActive && camp._ceremonyWalkers.Count == 0 && !camp.SlaughterBusy && camp._duelists.Count == 0;
	}

	private static FieldCampCeremonyReadiness PrepareCeremonyHook(Guid session, Hero victim, Vec3 origin, Vec2 forward, float radius) =>
		Current?.PrepareCeremony(session, victim, origin, forward, radius) ?? FieldCampCeremonyReadiness.Failed;

	private static IReadOnlyList<Agent> BorrowCeremonyCrowdHook(Guid session) =>
		Current is FieldCampMissionLogic camp && camp._ceremonySession == session && camp._ceremonyActive &&
		camp._ceremonyReadiness == FieldCampCeremonyReadiness.Ready ? camp._ceremonyCrowd : Array.Empty<Agent>();

	private static void ReleaseCeremonyHook(Guid session)
	{
		var camp = Current;
		if (camp != null && camp._ceremonySession == session) camp.OnCeremonyReleased();
	}

	private static void OnCeremonyStagedHook(Hero victim, Vec3 origin, Vec2 forward) => Current?.OnCeremonyStaged(victim, origin, forward);
	private static void OnCeremonyReleasedHook() => Current?.OnCeremonyReleased();

	private FieldCampCeremonyReadiness PrepareCeremony(Guid session, Hero victim, Vec3 origin, Vec2 forward, float radius)
	{
		// Polls never run admission against our own reservation or restart a failed session.
		if (_ceremonySession == session && session != Guid.Empty)
			return _ceremonyActive ? _ceremonyReadiness : FieldCampCeremonyReadiness.Failed;
		if (session == Guid.Empty || !CanStartCeremony(Mission) || !origin.IsValid || !forward.IsNonZero() ||
			float.IsNaN(radius) || float.IsInfinity(radius) || radius > 60f) return FieldCampCeremonyReadiness.Failed;

		_ceremonySession = session;
		_ceremonyActive = true;
		_ceremonyStaged = false;
		_ceremonyReadiness = FieldCampCeremonyReadiness.Pending;
		_ceremonyOrigin = origin;
		_ceremonyForward = forward.Normalized();
		_ceremonyRadius = Math.Max(CeremonyClearRadius, radius);
		_ceremonyDeadline = _elapsed + CeremonyPreparationTimeoutSeconds;
		_ceremonyReturnDeadline = -1f;
		_ceremonyWalkCursor = _ceremonyClearCursor = 0;
		_ceremonyClearPassBlocked = false;
		_ceremonyCrowd = Array.Empty<Agent>();
		_ceremonyClearance.Clear();
		_ceremonyResidents.Clear();
		_ceremonyPrisoners.Clear();
		_ceremonyMoves.Clear();
		_ceremonyRouteRetryAt.Clear();
		_ceremonyMoved.Clear();
		_ceremonyWatchers.Clear();
		_ceremonyMarks.Clear();
		_ceremonyWaiting = _ceremonyUnclearedMoves = 0;
		_ceremonyClearanceVerified = false;
		_nextCeremonyProgressLog = _elapsed + 5f;
		try
		{
			if (_envoy != null)
			{
				// DropEnvoy cancels pending speech and releases the navigator before
				// any soldier is borrowed; the issue can be raised again afterwards.
				Requeue(_envoyIssue);
				DropEnvoy(sendHome: true);
			}
			var candidates = new List<CampAgent>(MaxCeremonyWatchers);
			for (int i = 0; i < _agents.Count; i++)
			{
				CampAgent entry = _agents[i];
				Agent agent = entry.Agent;
				if (agent == null || !agent.IsActive() || agent == Agent.Main) continue;
				_ceremonyResidents[agent] = entry;
				_ceremonyClearance.Add(agent);
				if (!CanMoveForCeremony(agent) || entry.Activity == Activity.Sentry || entry.Activity == Activity.Wounded ||
					entry.Activity == Activity.Post || agent.Character?.IsHero == true) continue;
				float distance = agent.Position.AsVec2.DistanceSquared(origin.AsVec2);
				int slot = 0;
				while (slot < candidates.Count && candidates[slot].Agent.Position.AsVec2.DistanceSquared(origin.AsVec2) <= distance) slot++;
				if (slot >= MaxCeremonyWatchers) continue;
				candidates.Insert(slot, entry);
				if (candidates.Count > MaxCeremonyWatchers) candidates.RemoveAt(MaxCeremonyWatchers);
			}
			for (int i = 0; i < _prisoners.Count; i++)
			{
				Agent agent = _prisoners[i];
				// The condemned stays untouched until ready and is hidden only at commit.
				if (agent == null || !agent.IsActive() || agent.Character == victim?.CharacterObject) continue;
				_ceremonyClearance.Add(agent);
				_ceremonyPrisoners.Add(agent);
			}
			Vec2 right = new Vec2(_ceremonyForward.y, -_ceremonyForward.x);
			int watchers = 0;
			for (int i = 0; i < candidates.Count; i++)
			{
				int row = i / WatchersPerRow;
				int file = i % WatchersPerRow;
				// Leave a central entry/escape corridor instead of filling it with spectators.
				float lateral = (file < 4 ? file - 4 : file - 3) * 1.4f;
				Vec2 flat = origin.AsVec2 + _ceremonyForward * (_ceremonyRadius + 2.5f + row * 1.5f) + right * lateral;
				if (TryStartCeremonyWalk(candidates[i].Agent, flat, watcher: true)) watchers++;
			}
			if (candidates.Count > 0 && watchers == 0)
			{
				FailCeremonyPreparation("no reachable spectator mark");
				return _ceremonyReadiness;
			}
			Logger.Log(LogSource, "execution preparation started session=" + session + " watchers=" + watchers + " registered=" + _ceremonyClearance.Count);
			AnimusForgeQuickInfo.Show(FieldCampText.CeremonyPreparing());
		}
		catch (Exception ex)
		{
			FailCeremonyPreparation(ex.Message);
		}
		return _ceremonyReadiness;
	}

	/// <summary>Exact actor ownership for scene-speech release adapters; never opens global conflict gates.</summary>
	internal bool IsCeremonyAgentHeld(Agent agent) => agent != null && _ceremonyMoves.ContainsKey(agent);

	private bool CanMoveForCeremony(Agent agent) => agent != null && agent.IsActive() && agent != Agent.Main &&
		agent.Controller == AgentControllerType.AI && !IsDuelist(agent) && !IsSceneConversationHeld(agent);

	private bool TryStartCeremonyWalk(Agent agent, Vec2 flat, bool watcher)
	{
		if (!CanMoveForCeremony(agent) || _ceremonyMoves.ContainsKey(agent)) return false;
		Vec3 goal = new Vec3(flat.x, flat.y, _ceremonyOrigin.z);
		goal.z = GroundHeight(Mission.Scene, goal);
		bool routed = FieldCampSite.TryBuildGroundedRoute(Mission.Scene, agent.Position, goal, out Vec3[] route, out bool covered);
		if (!routed && (covered || !HasCeremonyWalkPath(agent.Position, goal))) return false;
		if (routed && route.Length > 0) { goal = route[route.Length - 1]; flat = goal.AsVec2; }
		int cellX = (int)Math.Floor(flat.x / 1.3f);
		int cellY = (int)Math.Floor(flat.y / 1.3f);
		if (_ceremonyMarks.ContainsKey(CeremonyMarkKey(cellX, cellY))) return false;
		for (int x = cellX - 1; x <= cellX + 1; x++)
			for (int y = cellY - 1; y <= cellY + 1; y++)
				if (_ceremonyMarks.TryGetValue(CeremonyMarkKey(x, y), out Vec2 existing) && existing.DistanceSquared(flat) < 1.5f) return false;
		_ceremonyMarks[CeremonyMarkKey(cellX, cellY)] = flat;
		_ceremonyResidents.TryGetValue(agent, out CampAgent entry);
		Vec2 facing = _ceremonyOrigin.AsVec2 - flat;
		var move = new CeremonyMove
		{
			Agent = agent, Camp = entry, Prisoner = _ceremonyPrisoners.Contains(agent), Spectator = watcher,
			Home = entry != null ? entry.Home : agent.Position,
			HomeFacing = entry != null ? entry.HomeFacing : agent.LookDirection.AsVec2,
			Goal = goal, Facing = facing.IsNonZero() ? facing.Normalized() : -_ceremonyForward,
			Route = routed ? route : Array.Empty<Vec3>()
		};
		// Record ownership before touching AI so a partial failure is still returned.
		_ceremonyWalkers.Add(move);
		_ceremonyMoves.Add(agent, move);
		_ceremonyWaiting++;
		move.CountedUncleared = agent.Position.AsVec2.DistanceSquared(_ceremonyOrigin.AsVec2) < _ceremonyRadius * _ceremonyRadius;
		if (move.CountedUncleared) _ceremonyUnclearedMoves++;
		if (watcher) _ceremonyWatchers.Add(move);
		if (entry != null)
		{
			_ceremonyMoved.Add(entry);
			entry.Returning = false;
			entry.Seated = false;
			entry.Target = goal;
			entry.TargetFacing = move.Facing;
		}
		IssueCeremonyWalk(move, goal, move.Facing);
		Logger.Log(LogSource, "ceremony walk assigned agent=" + agent.Index + " cachedRoute=" + routed + " waypoints=" + move.Route.Length);
		return true;
	}

	private static long CeremonyMarkKey(int x, int y) => ((long)x << 32) | (uint)y;

	private bool HasCeremonyWalkPath(Vec3 from, Vec3 target)
	{
		if (!target.IsValid || !Mission.IsPositionInsideBoundaries(target.AsVec2) ||
			Mission.Scene.GetNavigationMeshForPosition(in target) == UIntPtr.Zero ||
			Mission.IsPositionOnAnyBlockerNavMeshFace(target)) return false;
		// Runtime props need not update the baked navmesh. Reject a mark inside
		// a tent/cart/body even when the navmesh still claims it is reachable.
		Vec3 low = target + new Vec3(0f, 0f, 0.2f);
		Vec3 high = target + new Vec3(0f, 0f, 1.9f);
		if (Mission.Scene.RayCastForClosestEntityOrTerrain(low, high, out _, out _, out _, 0.35f,
			BodyFlags.CommonCollisionExcludeFlagsForAgent)) return false;
		var start = new WorldPosition(Mission.Scene, from);
		var end = new WorldPosition(Mission.Scene, target);
		float direct = Math.Max(1f, from.AsVec2.Distance(target.AsVec2));
		return Mission.Scene.GetPathDistanceBetweenPositions(ref start, ref end, 0.4f, out float path) &&
			!float.IsNaN(path) && !float.IsInfinity(path) && path >= 0f && path <= direct * 3f + 8f;
	}

	private void IssueCeremonyWalk(CeremonyMove move, Vec3 goal, Vec2 facing, bool followRoute = true)
	{
		Agent agent = move.Agent;
		if (followRoute && _ceremonyActive && move.Route.Length > 0)
		{
			while (move.RouteIndex + 1 < move.Route.Length && agent.Position.AsVec2.DistanceSquared(move.Route[move.RouteIndex].AsVec2) <= 0.09f)
				move.RouteIndex++;
			goal = move.Route[move.RouteIndex];
		}
		StopCampNavigation(agent);
		agent.Controller = AgentControllerType.AI;
		agent.SetIsAIPaused(false);
		// A previous conversation may have frozen speed before ceremony ownership.
		agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		agent.SetActionChannel(0, in ActionIndexCache.act_none, true);
		var world = new WorldPosition(Mission.Scene, UIntPtr.Zero, goal, false);
		agent.SetScriptedPositionAndDirection(ref world, facing.RotationInRadians, false,
			Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun);
		move.RetryAt = _elapsed + 5f;
	}

	/// <summary>Called unconditionally by the owner's 0.5 s upkeep, including while active.</summary>
	private void TickCeremonyPreparation()
	{
		if (!_ceremonyActive || _ceremonyReadiness != FieldCampCeremonyReadiness.Pending) return;
		try
		{
			if (Mission.IsMissionEnding || !VengeanceIntegration.IsEnabled || SlaughterBusy || _duelists.Count > 0 || _elapsed >= _ceremonyDeadline)
			{
				FailCeremonyPreparation("interrupted or preparation timeout");
				return;
			}
			int walking = Math.Min(CeremonyTickBudget, _ceremonyWalkers.Count);
			for (int i = 0; i < walking; i++)
			{
				if (_ceremonyWalkCursor >= _ceremonyWalkers.Count) _ceremonyWalkCursor = 0;
				CeremonyMove move = _ceremonyWalkers[_ceremonyWalkCursor++];
				bool active = move.Agent.IsActive();
				bool uncleared = active && move.Agent.Position.AsVec2.DistanceSquared(_ceremonyOrigin.AsVec2) < _ceremonyRadius * _ceremonyRadius;
				if (move.CountedUncleared != uncleared)
				{
					_ceremonyUnclearedMoves += uncleared ? 1 : -1;
					move.CountedUncleared = uncleared;
				}
				if (!active)
				{
					if (move.CountedWaiting) { _ceremonyWaiting--; move.CountedWaiting = false; }
					continue;
				}
				if (IsSceneConversationHeld(move.Agent))
				{
					FailCeremonyPreparation("ceremony actor acquired by conversation");
					return;
				}
				if (move.ParkedOutside && !uncleared) continue;
				if (move.ParkedOutside)
				{
					// A physically re-entering actor becomes a blocker again; never waive actual clearance.
					move.ParkedOutside = false;
					if (!move.CountedWaiting) { _ceremonyWaiting++; move.CountedWaiting = true; }
				}
				if (FieldCampPreparationReadiness.CanParkOutside(move.Spectator, move.Arrived,
					_elapsed - (_ceremonyDeadline - CeremonyPreparationTimeoutSeconds),
					move.Agent.Position.AsVec2.DistanceSquared(_ceremonyOrigin.AsVec2), _ceremonyRadius))
				{
					// Keep the real actor safely outside. A late optional watcher is NOT counted as arrived/borrowed.
					IssueCeremonyWalk(move, move.Agent.Position, move.Facing, followRoute: false);
					move.ParkedOutside = true;
					if (move.CountedWaiting) { _ceremonyWaiting--; move.CountedWaiting = false; }
					Logger.Log(LogSource, "ceremony actor held outside cleared site agent=" + move.Agent.Index + " optionalSpectator=" + move.Spectator);
					continue;
				}
				if (move.Route.Length > 0 && move.RouteIndex + 1 < move.Route.Length
					&& move.Agent.Position.AsVec2.DistanceSquared(move.Route[move.RouteIndex].AsVec2) <= 0.09f)
					IssueCeremonyWalk(move, move.Goal, move.Facing);
				if ((move.Route.Length == 0 || move.RouteIndex + 1 == move.Route.Length)
					&& move.Agent.Position.AsVec2.DistanceSquared(move.Goal.AsVec2) <= CeremonyArrivalSquared)
				{
					if (!move.Arrived)
					{
						// Soft hold at the actual arrival position; never snap the root to its mark.
						IssueCeremonyWalk(move, move.Agent.Position, move.Facing, followRoute: false);
						move.Arrived = true;
						if (move.CountedWaiting) { _ceremonyWaiting--; move.CountedWaiting = false; }
						if (move.Camp != null)
						{
							move.Camp.Seated = true;
							PlayOnce(move.Agent, StandActions, move.Agent.Index);
						}
						if (move.Prisoner) ApplyPrisonerPose(move.Agent);
					}
				}
				else
				{
					if (!move.CountedWaiting) { _ceremonyWaiting++; move.CountedWaiting = true; }
					move.Arrived = false;
					if (_elapsed >= move.RetryAt) IssueCeremonyWalk(move, move.Goal, move.Facing);
				}
			}
			// Round-robin the immutable registry, not Mission.Agents. A blocked pass
			// cannot grant ready even if its last blocker was just told to walk away.
			int budget = Math.Min(CeremonyTickBudget, _ceremonyClearance.Count);
			int newRoutes = 2; // Bound collision-grid searches separately from cheap actor checks.
			for (int i = 0; i < budget; i++)
			{
				Agent agent = _ceremonyClearance[_ceremonyClearCursor++];
				if (agent.IsActive() && agent.Position.AsVec2.DistanceSquared(_ceremonyOrigin.AsVec2) < _ceremonyRadius * _ceremonyRadius)
				{
					_ceremonyClearanceVerified = false;
					// Tracked movers have an O(1) outstanding counter; this pass
					// only waits for residents not yet safely assigned a route.
					if (!_ceremonyMoves.ContainsKey(agent)) _ceremonyClearPassBlocked = true;
					if (newRoutes > 0 && !_ceremonyMoves.ContainsKey(agent) && CanMoveForCeremony(agent) &&
						(!_ceremonyRouteRetryAt.TryGetValue(agent, out float retryAt) || _elapsed >= retryAt))
					{
						newRoutes--;
						_ceremonyRouteRetryAt[agent] = _elapsed + 5f;
						Vec2 away = agent.Position.AsVec2 - _ceremonyOrigin.AsVec2;
						away = away.IsNonZero() ? away.Normalized() : -_ceremonyForward;
						for (int attempt = 0; attempt < 8; attempt++)
						{
							float angle = attempt * MathF.PI * 0.25f;
							float cosine = MathF.Cos(angle), sine = MathF.Sin(angle);
							Vec2 direction = new Vec2(away.x * cosine - away.y * sine, away.x * sine + away.y * cosine);
							Vec2 flat = _ceremonyOrigin.AsVec2 + direction * (_ceremonyRadius + 2.5f);
							if (TryStartCeremonyWalk(agent, flat, watcher: false)) break;
						}
					}
				}
				if (_ceremonyClearCursor == _ceremonyClearance.Count)
				{
					_ceremonyClearCursor = 0;
					bool clear = !_ceremonyClearPassBlocked;
					_ceremonyClearPassBlocked = false;
					_ceremonyClearanceVerified = clear;
					break;
				}
			}
			if (_elapsed >= _nextCeremonyProgressLog)
			{
				_nextCeremonyProgressLog = _elapsed + 5f;
				Logger.Log(LogSource, "execution preparation pending session=" + _ceremonySession + " waiting=" + _ceremonyWaiting
					+ " uncleared=" + _ceremonyUnclearedMoves + " clearanceVerified=" + _ceremonyClearanceVerified);
			}
			if ((_ceremonyClearanceVerified || _ceremonyClearance.Count == 0) &&
				_ceremonyWaiting == 0 && _ceremonyUnclearedMoves == 0) CompleteCeremonyPreparation();
		}
		catch (Exception ex) { FailCeremonyPreparation(ex.Message); }
	}

	private void CompleteCeremonyPreparation()
	{
		var crowd = new List<Agent>(MaxCeremonyWatchers);
		for (int i = 0; i < _ceremonyWatchers.Count; i++)
		{
			CeremonyMove move = _ceremonyWatchers[i];
			if (move.Arrived && !move.ParkedOutside && move.Agent.IsActive()) crowd.Add(move.Agent);
		}
		_ceremonyCrowd = crowd.ToArray();
		_ceremonyReadiness = FieldCampCeremonyReadiness.Ready;
		Logger.Log(LogSource, "execution preparation ready session=" + _ceremonySession + " borrowedCrowd=" + crowd.Count);
	}

	private void FailCeremonyPreparation(string reason)
	{
		_ceremonyReadiness = FieldCampCeremonyReadiness.Failed;
		Logger.Log(LogSource, "[WARN] execution preparation failed session=" + _ceremonySession + " reason=" + reason);
		OnCeremonyReleased();
	}

	private void OnCeremonyStaged(Hero victim, Vec3 origin, Vec2 forward)
	{
		if (!_ceremonyActive || _ceremonyStaged || _ceremonyReadiness != FieldCampCeremonyReadiness.Ready) return;
		_ceremonyStaged = true;
		HideCampCopyOfCondemned(victim);
	}

	/// <summary>Only after ready: remove the pen copy once, never re-add a hidden victim.</summary>
	private void HideCampCopyOfCondemned(Hero victim)
	{
		CharacterObject character = victim?.CharacterObject;
		if (character == null) return;
		for (int i = _prisoners.Count - 1; i >= 0; i--)
		{
			Agent agent = _prisoners[i];
			if (agent == null || agent.Character != character) continue;
			// Removing before FadeOut prevents a visual dismissal being recorded as a casualty.
			ForgetCampPrisonerForCeremony(agent);
			if (agent.IsActive()) agent.FadeOut(true, false);
		}
	}

	private void OnCeremonyReleased()
	{
		if (!_ceremonyActive) return;
		_ceremonyActive = false;
		if (_ceremonyReadiness == FieldCampCeremonyReadiness.Pending) _ceremonyReadiness = FieldCampCeremonyReadiness.Failed;
		_ceremonyCrowd = Array.Empty<Agent>();
		try { if (_duelists.Count == 0) CalmTeams(); }
		catch (Exception ex) { Logger.Log(LogSource, "[WARN] ceremony team release failed: " + ex.Message); }
		finally
		{
			_ceremonyReturnDeadline = _ceremonyWalkers.Count > 0 ? _elapsed + CeremonyReturnGiveUpSeconds : -1f;
			Logger.Log(LogSource, "execution ceremony released session=" + _ceremonySession + " moved=" + _ceremonyWalkers.Count);
		}
	}

	/// <summary>Existing 0.5 s return check; bounded actors walk home, never teleport.</summary>
	private void TickCeremonyReturn()
	{
		if (_ceremonyReturnDeadline < 0f || _ceremonyActive) return;
		bool giveUp = _elapsed >= _ceremonyReturnDeadline;
		int budget = Math.Min(CeremonyTickBudget, _ceremonyWalkers.Count);
		for (int i = 0; i < budget && _ceremonyWalkers.Count > 0; i++)
		{
			if (_ceremonyWalkCursor >= _ceremonyWalkers.Count) _ceremonyWalkCursor = 0;
			int index = _ceremonyWalkCursor;
			CeremonyMove move = _ceremonyWalkers[index];
			Agent agent = move.Agent;
			bool remove = agent == null || !agent.IsActive() || (move.Prisoner && !_prisoners.Contains(agent));
			try
			{
				if (!remove && IsSceneConversationHeld(agent)) { _ceremonyWalkCursor++; continue; }
				if (!remove && !move.ReturningHome)
				{
					if (agent.Controller != AgentControllerType.AI && !giveUp) { _ceremonyWalkCursor++; continue; }
					IssueCeremonyWalk(move, move.Home, move.HomeFacing, followRoute: false);
					move.ReturningHome = true;
					if (move.Camp != null)
					{
						move.Camp.Target = move.Home;
						move.Camp.TargetFacing = move.HomeFacing;
						move.Camp.Seated = false;
						move.Camp.Returning = false;
					}
				}
				if (!remove && agent.Position.AsVec2.DistanceSquared(move.Home.AsVec2) <= CeremonyArrivalSquared)
				{
					IssueCeremonyWalk(move, agent.Position, move.HomeFacing);
					if (move.Prisoner) ApplyPrisonerPose(agent);
					if (move.Camp != null)
					{
						ApplyPose(move.Camp);
						move.Camp.Seated = true;
						move.Camp.NextWanderTime = _elapsed + 30f;
					}
					remove = true;
				}
				else if (!remove && giveUp)
				{
					// Return ownership at the current position on a failed route. The
					// camp can retry ordinary duties; prisoners remain bound and mortal.
					if (move.Prisoner) ApplyPrisonerPose(agent);
					if (move.Camp != null) MoveTo(move.Camp, move.Home, move.HomeFacing, run: false);
					remove = true;
				}
				else if (!remove && _elapsed >= move.RetryAt)
				{
					IssueCeremonyWalk(move, move.Home, move.HomeFacing, followRoute: false);
				}
			}
			catch (Exception ex)
			{
				Logger.Log(LogSource, "[WARN] ceremony return failed agent=" + (agent?.Index ?? -1) + " error=" + ex.Message);
				remove = giveUp;
			}
			if (remove)
			{
				_ceremonyWalkers.RemoveAt(index);
				if (agent != null) _ceremonyMoves.Remove(agent);
				if (move.Camp != null) _ceremonyMoved.Remove(move.Camp);
			}
			else _ceremonyWalkCursor++;
		}
		if (_ceremonyWalkers.Count == 0)
		{
			_ceremonyReturnDeadline = -1f;
			_ceremonyClearance.Clear();
			_ceremonyResidents.Clear();
			_ceremonyPrisoners.Clear();
			_ceremonyRouteRetryAt.Clear();
			_ceremonyWatchers.Clear();
			_ceremonyMarks.Clear();
		}
	}
}
