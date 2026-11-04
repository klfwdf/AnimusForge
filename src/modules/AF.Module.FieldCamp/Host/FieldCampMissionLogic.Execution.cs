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
	private const int CeremonyTickBudget = 20;
	private const float CeremonyClearRadius = 2f;
	private const float CeremonyPreparationTimeoutSeconds = 55f;
	private const float CeremonyReturnGiveUpSeconds = 60f;
	private const float CeremonyArrivalSquared = 0.81f;

	private sealed class CeremonyMove
	{
		internal Agent Agent;
		internal CampAgent Camp;
		internal bool Prisoner;
		internal bool Arrived;
        internal bool ClearanceHeld;
        internal bool Spectator;
        internal bool SpectatorRoutePending;
        internal int SpectatorRouteAttempts;
        internal float SpectatorDeadline;
        internal Vec3[] Route = Array.Empty<Vec3>();
        internal int RouteIndex;
		internal bool ReturningHome;
		internal Vec3 Home;
		internal Vec2 HomeFacing;
		internal Vec3 Goal;
		internal Vec2 Facing;
		internal float RetryAt;
        internal Vec3 LastPosition;
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
	private Guid _ceremonySession;
	private FieldCampCeremonyReadiness _ceremonyReadiness;
	private bool _ceremonyActive;
	private bool _ceremonyStaged;
	private float _ceremonyDeadline;
    private float _nextCeremonyProgressLog;
	private float _ceremonyReturnDeadline = -1f;
	private Vec3 _ceremonyOrigin;
	private Vec2 _ceremonyForward;
	private float _ceremonyRadius;
	private int _ceremonyWalkCursor;
	private int _ceremonyClearCursor;
	private bool _ceremonyClearPassBlocked;

	internal static bool IsExecutionCeremonyAvailable =>
		VengeanceIntegration.IsEnabled && VengeanceIntegration.IsEmbeddedHostActive && RichExecutionApi.IsInitialized;

	internal static void RegisterVengeanceHooks()
	{
		VengeanceIntegration.FieldCampMissionProbe = IsCampMission;
		VengeanceIntegration.FieldCampCeremonyAdmissionProbe = CanStartCeremony;
		VengeanceIntegration.FieldCampCeremonyPreparation = PrepareCeremonyHook;
		VengeanceIntegration.FieldCampCeremonyBorrowedCrowd = BorrowCeremonyCrowdHook;
		VengeanceIntegration.FieldCampCeremonyPreparationReleased = ReleaseCeremonyHook;
        VengeanceIntegration.FieldCampCeremonyGeometryChanged = OnCeremonyGeometryChangedHook;
        VengeanceIntegration.FieldCampAcquireActor = AcquireCampCeremonyActorHook;
        VengeanceIntegration.FieldCampOwnsVictimOrigin = OwnsCampVictimOriginHook;
        VengeanceIntegration.FieldCampWalkActor = WalkCampActorHook;
        VengeanceIntegration.FieldCampActorReadiness = CampActorReadinessHook;
        VengeanceIntegration.FieldCampReleaseActors = ReleaseCampActorsHook;
        VengeanceIntegration.FieldCampPrisonerPresent = CampPrisonerPresentHook;
        VengeanceIntegration.FieldCampArrangeSpectators = ArrangeCampSpectatorsHook;
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
        ReleaseCampActorsHook();
        VengeanceIntegration.FieldCampCeremonyGeometryChanged = null;
        VengeanceIntegration.FieldCampAcquireActor = null;
        VengeanceIntegration.FieldCampOwnsVictimOrigin = null;
        VengeanceIntegration.FieldCampWalkActor = null;
        VengeanceIntegration.FieldCampActorReadiness = null;
        VengeanceIntegration.FieldCampReleaseActors = null;
        VengeanceIntegration.FieldCampPrisonerPresent = null;
        VengeanceIntegration.FieldCampArrangeSpectators = null;
		VengeanceIntegration.FieldCampCeremonyStaged = null;
		VengeanceIntegration.FieldCampCeremonyReleased = null;
	}

	private static bool IsCampMission(Mission mission) =>
		mission != null && mission.GetMissionBehavior<FieldCampMissionLogic>() != null;

	private static bool CanStartCeremony(Mission mission)
	{
		var camp = mission?.GetMissionBehavior<FieldCampMissionLogic>();
		return camp != null && camp._campInitialized && !mission.IsMissionEnding &&
			!camp._ceremonyActive && camp._ceremonyWalkers.Count == 0 && !camp.SlaughterBusy && camp._duelists.Count == 0;
	}

	private static FieldCampCeremonyReadiness PrepareCeremonyHook(Guid session, Hero victim, Vec3 origin, Vec2 forward, float radius, IReadOnlyList<FieldCampCeremonyArea> areas) =>
		Current?.PrepareCeremony(session, victim, origin, forward, radius, areas) ?? FieldCampCeremonyReadiness.Failed;

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

    private IReadOnlyList<FieldCampCeremonyArea> _ceremonyAreas = Array.Empty<FieldCampCeremonyArea>();
    private bool _ceremonyGeometryPresent;

    private bool IsCeremonyClear(Vec3 position)
    {
        for (int i = 0; i < _ceremonyAreas.Count; i++)
            if (_ceremonyAreas[i].Contains(position)) return false;
        return true;
    }

    private bool IsReservedCeremonyPosition(Vec3 position) =>
        (_ceremonyActive || _ceremonyGeometryPresent) && !IsCeremonyClear(position);

    private FieldCampCeremonyReadiness PrepareCeremony(Guid session, Hero victim, Vec3 origin, Vec2 forward,
        float radius, IReadOnlyList<FieldCampCeremonyArea> areas)
    {
        if (_ceremonySession == session && session != Guid.Empty)
        {
            // The host can consume Ready on its next half-second poll. Read
            // the footprint again in that same call, immediately before build.
            if (_ceremonyActive && _ceremonyReadiness == FieldCampCeremonyReadiness.Ready &&
                !_ceremonyStaged && !_ceremonyGeometryPresent) CompleteCeremonyPreparation();
            return _ceremonyActive ? _ceremonyReadiness : FieldCampCeremonyReadiness.Failed;
        }
        if (session == Guid.Empty || !CanStartCeremony(Mission) || !origin.IsValid || !forward.IsNonZero() ||
            float.IsNaN(radius) || float.IsInfinity(radius) || areas == null || areas.Count == 0)
            return FieldCampCeremonyReadiness.Failed;
        _ceremonySession = session;
        _ceremonyActive = true;
        _ceremonyStaged = false;
        _ceremonyReadiness = FieldCampCeremonyReadiness.Pending;
        _ceremonyOrigin = origin;
        _ceremonyForward = forward.Normalized();
        _ceremonyRadius = Math.Max(CeremonyClearRadius, radius);
        _ceremonyAreas = areas;
        _ceremonyDeadline = _elapsed + CeremonyPreparationTimeoutSeconds;
        _nextCeremonyProgressLog = _elapsed + 5f;
        _ceremonyReturnDeadline = -1f;
        _ceremonyWalkCursor = _ceremonyClearCursor = 0;
        _ceremonyClearPassBlocked = false;
        _ceremonyCrowd = Array.Empty<Agent>();
        _ceremonyClearance.Clear();
        _ceremonyResidents.Clear();
        _ceremonyPrisoners.Clear();
        _ceremonyMoves.Clear();
        _ceremonyWalkers.Clear();
        _ceremonyRouteRetryAt.Clear();
        _ceremonyMoved.Clear();
        _ceremonyWatchers.Clear();
        _ceremonyMarks.Clear();
        try
        {
            if (_envoy != null) { Requeue(_envoyIssue); DropEnvoy(sendHome: true); }
            for (int i = 0; i < _agents.Count; i++)
            {
                CampAgent entry = _agents[i];
                Agent agent = entry.Agent;
                if (agent == null || !agent.IsActive() || agent == Agent.Main || _campCeremonyActors.ContainsKey(agent)) continue;
                _ceremonyResidents[agent] = entry;
                if (!IsCeremonyClear(agent.Position)) _ceremonyClearance.Add(agent);
            }
            for (int i = 0; i < _prisoners.Count; i++)
            {
                Agent agent = _prisoners[i];
                if (agent == null || !agent.IsActive() || _campCeremonyActors.ContainsKey(agent)) continue;
                if (!IsCeremonyClear(agent.Position)) _ceremonyClearance.Add(agent);
                _ceremonyPrisoners.Add(agent);
            }
            // Snapshot only people actually occupying the oriented prop footprints
            // at confirmation. Unrelated camp residents are not a moving global gate.
            for (int i = 0; i < _ceremonyClearance.Count; i++)
                Logger.Log(LogSource, "execution footprint occupant session=" + session +
                    " agent=" + _ceremonyClearance[i].Index + " position=" + _ceremonyClearance[i].Position);
            if (_ceremonyGeometryPresent || VerifyCeremonyClearanceNow()) CompleteCeremonyPreparation();
            else { AnimusForgeQuickInfo.Show(FieldCampText.DeploymentClearing()); TickCeremonyPreparation(); }
            Logger.Log(LogSource, "execution footprint preparation session=" + session + " occupants=" + _ceremonyClearance.Count);
        }
        catch (Exception ex) { FailCeremonyPreparation(ex.Message); }
        return _ceremonyReadiness;
    }

    internal bool IsCeremonyAgentHeld(Agent agent) => agent != null &&
        (_campCeremonyActors.ContainsKey(agent) || (_ceremonyActive &&
        _ceremonyMoves.TryGetValue(agent, out CeremonyMove move) && (move.Spectator || !move.ClearanceHeld)));

    private bool CanMoveForCeremony(Agent agent) => agent != null && agent.IsActive() && agent != Agent.Main &&
        agent.Controller == AgentControllerType.AI && !_campCeremonyActors.ContainsKey(agent) && !IsDuelist(agent) && !IsSceneConversationHeld(agent);

    private bool TryStartCeremonyWalk(Agent agent, Vec2 flat, bool watcher)
    {
        if (!CanMoveForCeremony(agent) || _ceremonyMoves.ContainsKey(agent)) return false;
        Vec3 goal = new Vec3(flat.x, flat.y, _ceremonyOrigin.z);
        goal.z = GroundHeight(Mission.Scene, goal);
        if (!IsCeremonyClear(goal) || !TryCampRoute(agent.Position, goal, out Vec3[] route)) return false;
        for (int i = 0; i < _ceremonyWalkers.Count; i++)
            if (_ceremonyWalkers[i].Goal.AsVec2.DistanceSquared(flat) < 1f) return false;
        _ceremonyResidents.TryGetValue(agent, out CampAgent entry);
        var move = new CeremonyMove
        {
            Agent = agent, Camp = entry, Prisoner = _ceremonyPrisoners.Contains(agent), Spectator = watcher,
            Route = route, Home = entry != null ? entry.Home : agent.Position,
            HomeFacing = entry != null ? entry.HomeFacing : agent.LookDirection.AsVec2,
            Goal = goal, Facing = _ceremonyForward
        };
        _ceremonyWalkers.Add(move);
        _ceremonyMoves.Add(agent, move);
        if (entry != null) { _ceremonyMoved.Add(entry); entry.Returning = false; entry.Seated = false; }
        IssueCeremonyWalk(move, goal, move.Facing);
        return true;
    }

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

    private void IssueCeremonyWalk(CeremonyMove move, Vec3 goal, Vec2 facing)
    {
        if (move.RouteIndex < move.Route.Length) goal = move.Route[move.RouteIndex];
        StopCampNavigation(move.Agent);
        move.Agent.Controller = AgentControllerType.AI;
        move.Agent.SetIsAIPaused(false);
        move.Agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
        if (move.Camp != null) ReleasePose(move.Camp);
        move.Agent.SetActionChannel(0, in ActionIndexCache.act_none, true);
        var world = new WorldPosition(Mission.Scene, UIntPtr.Zero, goal, false);
        var flags = Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun
            | Agent.AIScriptedFrameFlags.NeverSlowDown;
        move.Agent.SetScriptedPosition(ref world, false, flags);
        move.RetryAt = _elapsed + 5f;
        move.LastPosition = move.Agent.Position;
    }

    private void HoldCeremonyActor(CeremonyMove move)
    {
        StopCampNavigation(move.Agent);
        move.Agent.SetIsAIPaused(true);
        Vec2 facing = move.ReturningHome ? move.HomeFacing : move.Facing;
        move.Agent.LookDirection = new Vec3(facing.x, facing.y, 0f);
        if (move.Prisoner) ApplyPrisonerPose(move.Agent);
        else PlayOnce(move.Agent, StandActions, move.Agent.Index);
    }

    private void TickCeremonyPreparation()
    {
        if (!_ceremonyActive || _ceremonyReadiness != FieldCampCeremonyReadiness.Pending) return;
        try
        {
            if (Mission.IsMissionEnding || !VengeanceIntegration.IsEnabled || SlaughterBusy || _duelists.Count > 0 || _elapsed >= _ceremonyDeadline)
            { FailCeremonyPreparation("interrupted or preparation timeout"); return; }
            int walking = Math.Min(CeremonyTickBudget, _ceremonyWalkers.Count);
            for (int i = 0; i < walking; i++)
            {
                if (_ceremonyWalkCursor >= _ceremonyWalkers.Count) _ceremonyWalkCursor = 0;
                CeremonyMove move = _ceremonyWalkers[_ceremonyWalkCursor++];
                if (!move.Agent.IsActive() || IsSceneConversationHeld(move.Agent)) continue;
                if (IsCeremonyClear(move.Agent.Position))
                {
                    if (!move.ClearanceHeld) HoldCeremonyActor(move);
                    move.ClearanceHeld = true;
                    continue;
                }
                move.ClearanceHeld = false;
                if (CanAdvanceCampCorner(move.Agent, move.Route, move.RouteIndex))
                { move.RouteIndex++; IssueCeremonyWalk(move, move.Goal, move.Facing); }
                else if (_elapsed >= move.RetryAt)
                {
                    if (move.Agent.Position.AsVec2.DistanceSquared(move.LastPosition.AsVec2) < 0.04f)
                    {
                        HoldCeremonyActor(move);
                        _ceremonyMoves.Remove(move.Agent);
                        if (move.Camp != null) _ceremonyMoved.Remove(move.Camp);
                        _ceremonyWalkers.RemoveAt(--_ceremonyWalkCursor);
                        walking--; i--;
                        _ceremonyRouteRetryAt[move.Agent] = _elapsed + 1f;
                    }
                    else IssueCeremonyWalk(move, move.Goal, move.Facing);
                }
            }
            int routeBudget = 2;
            int budget = Math.Min(CeremonyTickBudget, _ceremonyClearance.Count);
            for (int i = 0; i < budget; i++)
            {
                Agent agent = _ceremonyClearance[_ceremonyClearCursor++];
                if (agent.IsActive() && !IsCeremonyClear(agent.Position))
                {
                    _ceremonyClearPassBlocked = true;
                    if (routeBudget > 0 && !_ceremonyMoves.ContainsKey(agent) && CanMoveForCeremony(agent) &&
                        (!_ceremonyRouteRetryAt.TryGetValue(agent, out float retryAt) || _elapsed >= retryAt))
                    {
                        routeBudget--;
                        _ceremonyRouteRetryAt[agent] = _elapsed + 2f;
                        Vec2 away = agent.Position.AsVec2 - _ceremonyOrigin.AsVec2;
                        away = away.IsNonZero() ? away.Normalized() : -_ceremonyForward;
                        bool moved = false;
                        int pathAttempts = 0;
                        float directionOffset = MBRandom.RandomInt(8) * MathF.PI * 0.25f;
                        // Find the closest exit, not a distant point beyond a 9 m camp-wide circle.
                        for (int ring = 1; ring <= 8 && !moved && pathAttempts < 4; ring++)
                            for (int attempt = 0; attempt < 8; attempt++)
                            {
                                float angle = attempt * MathF.PI * 0.25f + directionOffset;
                                var direction = new Vec2(away.x * MathF.Cos(angle) - away.y * MathF.Sin(angle),
                                    away.x * MathF.Sin(angle) + away.y * MathF.Cos(angle));
                                Vec2 flat = agent.Position.AsVec2 + direction * (ring * 1.5f);
                                if (!IsCeremonyClear(new Vec3(flat.x, flat.y, agent.Position.z))) continue;
                                if (pathAttempts++ >= 4) break;
                                if (TryStartCeremonyWalk(agent, flat, watcher: false)) { moved = true; break; }
                            }
                    }
                }
                if (_ceremonyClearCursor == _ceremonyClearance.Count)
                {
                    _ceremonyClearCursor = 0;
                    bool clear = !_ceremonyClearPassBlocked;
                    _ceremonyClearPassBlocked = false;
                    if (clear && VerifyCeremonyClearanceNow()) CompleteCeremonyPreparation();
                    break;
                }
            }
            if (_elapsed >= _nextCeremonyProgressLog)
            {
                _nextCeremonyProgressLog = _elapsed + 5f;
                Logger.Log(LogSource, "execution footprint clearing session=" + _ceremonySession + " movers=" + _ceremonyWalkers.Count);
            }
        }
        catch (Exception ex) { FailCeremonyPreparation(ex.Message); }
    }

    private bool VerifyCeremonyClearanceNow()
    {
        for (int i = 0; i < _ceremonyClearance.Count; i++)
            if (_ceremonyClearance[i].IsActive() && !IsCeremonyClear(_ceremonyClearance[i].Position)) return false;
        return true;
    }

    private void CompleteCeremonyPreparation()
    {
        // Recheck actual occupancy only at the build-commit boundary. A person
        // may have crossed in while the original occupants were stepping out;
        // only that person joins clearance, never the unrelated camp roster.
        if (!_ceremonyGeometryPresent && CaptureLateCeremonyOccupants())
        {
            _ceremonyReadiness = FieldCampCeremonyReadiness.Pending;
            _ceremonyClearCursor = 0;
            _ceremonyClearPassBlocked = false;
            return;
        }
        for (int i = 0; i < _ceremonyWalkers.Count; i++)
        {
            CeremonyMove move = _ceremonyWalkers[i];
            if (!move.Agent.IsActive() || IsSceneConversationHeld(move.Agent)) continue;
            HoldCeremonyActor(move);
            move.ClearanceHeld = true;
        }
        _ceremonyReadiness = FieldCampCeremonyReadiness.Ready;
        Logger.Log(LogSource, "execution preparation ready session=" + _ceremonySession + "; footprint clear, no spectator gate");
    }

    private bool CaptureLateCeremonyOccupants()
    {
        bool occupied = false;
        foreach (Agent agent in _ceremonyResidents.Keys)
            occupied |= CaptureLateCeremonyOccupant(agent);
        foreach (Agent agent in _ceremonyPrisoners)
            occupied |= CaptureLateCeremonyOccupant(agent);
        return occupied;
    }

    private bool CaptureLateCeremonyOccupant(Agent agent)
    {
        if (agent == null || !agent.IsActive() || agent == Agent.Main ||
            _campCeremonyActors.ContainsKey(agent) || IsCeremonyClear(agent.Position)) return false;
        if (!_ceremonyClearance.Contains(agent))
        {
            _ceremonyClearance.Add(agent);
            Logger.Log(LogSource, "execution late footprint occupant session=" + _ceremonySession +
                " agent=" + agent.Index + " position=" + agent.Position);
        }
        return true;
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
        // The existing pen Agent is borrowed and walks in; never fade/spawn a second copy.
    }

    private static void OnCeremonyGeometryChangedHook(Guid session, IReadOnlyList<FieldCampCeremonyArea> areas, bool created)
    {
        var camp = Current;
        if (camp == null || camp.Mission.IsMissionEnding || camp._ceremonySession != session) return;
        // A failed actor acquisition never published geometry to the ground cache.
        // Do not raycast the whole site again for a build that never existed.
        if (created || camp._ceremonyGeometryPresent)
            FieldCampSite.RefreshGroundObstacles(camp.Mission.Scene, areas);
        camp._ceremonyGeometryPresent = created;
        if (!created) { camp.ReleaseCampCeremonyActors(); return; }
        if (!camp._ceremonyActive) return;
        // Existing daily paths were calculated before the props existed. Stop them once;
        // normal upkeep chooses new paths against the updated obstacle cache.
        for (int i = 0; i < camp._agents.Count; i++)
        {
            CampAgent entry = camp._agents[i];
            if (!entry.Agent.IsActive() || entry.Seated || camp.IsSceneConversationHeld(entry.Agent) || camp._ceremonyMoved.Contains(entry)) continue;
            camp.SettleCampAgent(entry, blocked: true);
        }
    }

    private static void ArrangeCampSpectatorsHook(Guid session)
    {
        var camp = Current;
        if (camp == null || camp._ceremonySession != session || !camp._ceremonyActive) return;
        float front = 0f;
        for (int i = 0; i < camp._ceremonyAreas.Count; i++)
        {
            FieldCampCeremonyArea area = camp._ceremonyAreas[i];
            for (int corner = 0; corner < 4; corner++)
            {
                Vec2 delta = new Vec2((corner & 1) == 0 ? area.Min.x : area.Max.x,
                    (corner & 2) == 0 ? area.Min.y : area.Max.y) - camp._ceremonyOrigin.AsVec2;
                front = Math.Max(front, delta.x * camp._ceremonyForward.x + delta.y * camp._ceremonyForward.y);
            }
        }
        Vec2 side = new Vec2(camp._ceremonyForward.y, -camp._ceremonyForward.x);
        int watchers = 0;
        int spectatorSlot = 0;
        for (int i = 0; i < camp._agents.Count && watchers < MaxCeremonyWatchers; i++)
        {
            CampAgent entry = camp._agents[i];
            Agent agent = entry.Agent;
            if (!camp.CanMoveForCeremony(agent) || camp._ceremonyMoves.ContainsKey(agent) || agent.Character?.IsHero == true ||
                entry.Activity == Activity.Wounded || entry.Activity == Activity.Sentry || entry.Activity == Activity.Post) continue;
            Vec2 flat = camp._ceremonyOrigin.AsVec2 + camp._ceremonyForward * (front + 2.5f + (spectatorSlot / 8) * 1.7f)
                + side * ((spectatorSlot % 8 - 3.5f) * 1.5f);
            spectatorSlot++;
            Vec3 desired = new Vec3(flat.x, flat.y, camp._ceremonyOrigin.z);
            if (!FieldCampSite.TryResolveGroundedPosition(camp.Mission.Scene, desired, 0.35f, out Vec3 goal)
                || !camp.IsInsideCamp(goal) || !camp.IsCeremonyClear(goal)
                || !camp.IsCampPositionAvailable(agent, goal, 1.2f)) continue;
            var move = new CeremonyMove { Agent = agent, Camp = entry, Spectator = true, SpectatorRoutePending = true,
                SpectatorDeadline = camp._elapsed + 60f, Home = entry.Home, HomeFacing = entry.HomeFacing,
                Goal = goal, Facing = -camp._ceremonyForward };
            camp._ceremonyWalkers.Add(move); camp._ceremonyMoves.Add(agent, move); camp._ceremonyMoved.Add(entry);
            camp._ceremonyWatchers.Add(move);
            entry.Returning = false; entry.Seated = false;
            StopCampNavigation(agent); agent.SetIsAIPaused(true);
            watchers++;
        }
        Logger.Log(LogSource, "execution spectator walks queued session=" + session + " watchers=" + watchers);
    }

    private void TickCeremonySpectators()
    {
        if (!_ceremonyActive || !_ceremonyStaged) return;
        // At most sixteen existing residents; only two route attempts per upkeep.
        // Audience arrival never participates in preparation or actor readiness.
        int routeBudget = 2;
        for (int i = _ceremonyWatchers.Count - 1; i >= 0; i--)
        {
            CeremonyMove move = _ceremonyWatchers[i];
            Agent agent = move.Agent;
            if (!agent.IsActive() || IsSceneConversationHeld(agent))
            {
                RemoveCeremonyMove(move); _ceremonyWatchers.RemoveAt(i);
                continue;
            }
            if (move.Arrived) continue;
            if (move.SpectatorRoutePending)
            {
                if (routeBudget <= 0) continue;
                routeBudget--;
                Vec3 desired = move.Goal + new Vec3(_ceremonyForward.x, _ceremonyForward.y, 0f) * (move.SpectatorRouteAttempts * 1.5f);
                move.SpectatorRouteAttempts++;
                if (FieldCampSite.TryResolveGroundedPosition(Mission.Scene, desired, 0.35f, out Vec3 goal) &&
                    IsInsideCamp(goal) && IsCeremonyClear(goal) && IsCampPositionAvailable(agent, goal, 1.2f)
                    && TryCampRoute(agent.Position, goal, out Vec3[] route))
                {
                    move.Goal = goal; move.Route = route; move.RouteIndex = 0; move.SpectatorRoutePending = false;
                    Vec2 facing = _ceremonyOrigin.AsVec2 - goal.AsVec2;
                    move.Facing = facing.IsNonZero() ? facing.Normalized() : -_ceremonyForward;
                    IssueCeremonyWalk(move, move.Goal, move.Facing);
                }
                else if (move.SpectatorRouteAttempts >= 3)
                {
                    RemoveCeremonyMove(move); _ceremonyWatchers.RemoveAt(i);
                    StopCampNavigation(agent); agent.SetIsAIPaused(false);
                    if (move.Camp != null) SettleCampAgent(move.Camp, blocked: true);
                }
                continue;
            }
            bool arrived = agent.Position.AsVec2.DistanceSquared(move.Goal.AsVec2) <= CeremonyArrivalSquared &&
                Math.Abs(agent.Position.z - move.Goal.z) <= 0.65f;
            bool stopped = _elapsed >= move.SpectatorDeadline || (_elapsed >= move.RetryAt &&
                agent.Position.AsVec2.DistanceSquared(move.LastPosition.AsVec2) < 0.04f);
            if (arrived || stopped)
            {
                move.Arrived = true;
                Vec2 facing = _ceremonyOrigin.AsVec2 - agent.Position.AsVec2;
                move.Facing = facing.IsNonZero() ? facing.Normalized() : -_ceremonyForward;
                HoldCeremonyActor(move);
                agent.LookDirection = new Vec3(move.Facing.x, move.Facing.y, 0f);
                PlayOnce(agent, SentryActions, agent.Index);
                continue;
            }
            if (CanAdvanceCampCorner(agent, move.Route, move.RouteIndex))
            { move.RouteIndex++; IssueCeremonyWalk(move, move.Goal, move.Facing); }
            else if (_elapsed >= move.RetryAt) IssueCeremonyWalk(move, move.Goal, move.Facing);
        }
    }

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

    private void RemoveCeremonyMove(CeremonyMove move)
    {
        _ceremonyWalkers.Remove(move);
        _ceremonyMoves.Remove(move.Agent);
        if (move.Camp != null) _ceremonyMoved.Remove(move.Camp);
    }

    private void ReleaseCeremonyReturnForConversation(Agent agent)
    {
        if (!_ceremonyActive && _ceremonyMoves.TryGetValue(agent, out CeremonyMove move))
        {
            StopCampNavigation(agent);
            RemoveCeremonyMove(move);
        }
    }

    private bool FinishCeremonyReturn(CeremonyMove move)
    {
        StopCampNavigation(move.Agent);
        move.Agent.SetIsAIPaused(true);
        bool returned = move.Agent.Position.AsVec2.DistanceSquared(move.Home.AsVec2) <= CeremonyArrivalSquared;
        if (!returned && _elapsed < move.RetryAt) return false;
        if (!returned)
        {
            // Return ownership at the current grounded position, then let camp life
            // replan through the same obstacle grid. No collision-triggered teleport.
            if (move.Camp != null)
            {
                RecoverBlockedCampWalk(move.Camp);
                move.Camp.Target = move.Home;
                move.Camp.RouteQueued = true;
                return true;
            }
            move.RetryAt = _elapsed + 2f;
            move.ReturningHome = false;
            return false;
        }
        if (move.Prisoner) ApplyPrisonerPose(move.Agent);
        if (move.Camp != null) SettleCampAgent(move.Camp, blocked: false);
        return true;
    }

    private void TickCeremonyReturn()
    {
        if (_ceremonyReturnDeadline < 0f || _ceremonyActive) return;
        int routeBudget = 2;
        int budget = Math.Min(CeremonyTickBudget, _ceremonyWalkers.Count);
        for (int i = 0; i < budget && _ceremonyWalkers.Count > 0; i++)
        {
            if (_ceremonyWalkCursor >= _ceremonyWalkers.Count) _ceremonyWalkCursor = 0;
            CeremonyMove move = _ceremonyWalkers[_ceremonyWalkCursor];
            Agent agent = move.Agent;
            bool remove = agent == null || !agent.IsActive() || (move.Prisoner && !_prisoners.Contains(agent));
            try
            {
                if (!remove && IsSceneConversationHeld(agent)) { RemoveCeremonyMove(move); continue; }
                if (!remove && (_elapsed >= _ceremonyReturnDeadline ||
                    agent.Position.AsVec2.DistanceSquared(move.Home.AsVec2) <= CeremonyArrivalSquared))
                {
                    if (!FinishCeremonyReturn(move))
                    {
                        _ceremonyWalkCursor++;
                        continue;
                    }
                    remove = true;
                }
                if (!remove && !move.ReturningHome)
                {
                    if (routeBudget <= 0) { _ceremonyWalkCursor++; continue; }
                    routeBudget--;
                    if (IsReservedCeremonyPosition(move.Home) || !TryCampRoute(agent.Position, move.Home, out Vec3[] route))
                    {
                        if (!FinishCeremonyReturn(move))
                        {
                            _ceremonyWalkCursor++;
                            continue;
                        }
                        remove = true;
                    }
                    else
                    {
                        move.Route = route; move.RouteIndex = 0; move.ReturningHome = true;
                        IssueCeremonyWalk(move, move.Home, move.HomeFacing);
                    }
                }
                if (!remove && CanAdvanceCampCorner(agent, move.Route, move.RouteIndex))
                { move.RouteIndex++; IssueCeremonyWalk(move, move.Home, move.HomeFacing); }
                else if (!remove && _elapsed >= move.RetryAt)
                {
                    // Do not repeatedly drive an actor into an obstacle after a failed return.
                    if (agent.Position.AsVec2.DistanceSquared(move.LastPosition.AsVec2) < 0.04f)
                    {
                        if (!FinishCeremonyReturn(move))
                        {
                            _ceremonyWalkCursor++;
                            continue;
                        }
                        remove = true;
                    }
                    else IssueCeremonyWalk(move, move.Home, move.HomeFacing);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogSource, "[WARN] ceremony return failed agent=" + (agent?.Index ?? -1) + " error=" + ex.Message);
                if (agent?.IsActive() == true) StopCampNavigation(agent);
                remove = true;
            }
            if (remove) RemoveCeremonyMove(move);
            else _ceremonyWalkCursor++;
        }
        if (_ceremonyWalkers.Count == 0)
        {
            _ceremonyReturnDeadline = -1f;
            _ceremonyClearance.Clear(); _ceremonyResidents.Clear(); _ceremonyPrisoners.Clear();
            _ceremonyRouteRetryAt.Clear(); _ceremonyWatchers.Clear(); _ceremonyMarks.Clear();
        }
    }
}
