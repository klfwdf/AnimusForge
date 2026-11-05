using System;
using System.Collections.Generic;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>Camp life, assemble/dismiss orders and camp-local coordinates.</summary>
public sealed partial class FieldCampMissionLogic
{
	// Native scripted movement stops 0.22-0.34 m short of a goal (2026-11-04 camp log).
	// A 0.2 m corner radius left residents idle in front of every waypoint until the stall timer fired.
	private const float ArriveDistanceSquared = 0.2025f;
    private const float RouteCornerDistanceSquared = 0.2025f;
	private const float RowSpacing = 1.6f;
	private const float FileSpacing = 1.3f;
	private const int MinFilesPerRank = 10;
	private const int MaxRanks = 6;
	private const float BlockGap = 4f;
    private int _campStrollBudget;
    private float _nextCampWalkDiagnosticTime;
    private int _campCornerPassageBudget;
    private readonly MBList<Agent> _campNearbyAgents = new MBList<Agent>();


	/// <summary>
	/// Camp soldiers use the hideout action set: it is based on the warrior set (so fighting
	/// still works) and adds the sitting, eating, cooking, repairing, leaning, talking and
	/// sleeping clips the camp needs. Applied once per agent at spawn.
	/// </summary>
	internal static void ApplyCampActionSet(Agent agent)
	{
		try
		{
			Monster monster = agent?.Monster;
			if (monster == null)
			{
				return;
			}
			string code = agent.IsFemale ? "as_human_female_hideout_bandit" : "as_human_hideout_bandit";
			AnimationSystemData data = monster.FillAnimationSystemData(MBActionSet.GetActionSet(code), agent.Character?.GetStepSize() ?? 1f, false);
			agent.SetActionSet(ref data);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "camp action set failed agent=" + (agent?.Index ?? -1) + " error=" + ex.Message);
		}
	}
	private readonly Dictionary<string, ActionIndexCache> _actionCache = new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);

	// ---------- orders (called from the scene-shout postprocess tag) ----------

	/// <summary>Idempotent: several speakers may emit the same order in one shout.</summary>
	internal bool Assemble()
	{
		if (!_spawned || _assembled || SlaughterBusy || _duelists.Count > 0 || _ceremonyActive || _ceremonyWalkers.Count > 0 || _sceneConversationAgents.Count > 0 || _envoy != null)
		{
			return false;
		}
		_assembled = true;
		ComputeFormationTargets();
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			if (entry.FollowingPlayer || entry.Activity == Activity.Wounded || entry.Activity == Activity.Post)
			{
				continue;
			}
			entry.Returning = false;
			ReleasePose(entry);
			MoveTo(entry, entry.Target, entry.TargetFacing, run: true);
		}
		Logger.Log(LogSource, "assemble agents=" + _agents.Count);
		AnimusForgeQuickInfo.Show(FieldCampText.Assembled());
		return true;
	}

	internal bool Dismiss()
	{
		if (!_spawned || !_assembled || SlaughterBusy || _duelists.Count > 0 || _ceremonyActive || _ceremonyWalkers.Count > 0 || _sceneConversationAgents.Count > 0)
		{
			return false;
		}
		_assembled = false;
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
            if (entry.FollowingPlayer) continue;
            entry.Target = entry.Home;
			entry.TargetFacing = entry.HomeFacing;
			entry.Seated = false;
			entry.Returning = false;
			MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
		}
		Logger.Log(LogSource, "dismiss agents=" + _agents.Count);
		AnimusForgeQuickInfo.Show(FieldCampText.Dismissed());
		return true;
	}

	/// <summary>
	/// Blocks of up to 10 files per rank, one block per troop class, left to right
	/// in infantry / ranged / cavalry / horse-archer order, facing the player start.
	/// </summary>
	private void ComputeFormationTargets()
	{
		var byClass = new SortedDictionary<int, List<CampAgent>>();
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent entry = _agents[i];
			if (entry.FollowingPlayer || entry.Activity == Activity.Wounded || entry.Activity == Activity.Post)
			{
				continue;
			}
			int key = ClassOrder(entry.TroopClass);
			if (!byClass.TryGetValue(key, out List<CampAgent> list))
			{
				byClass[key] = list = new List<CampAgent>();
			}
			list.Add(entry);
		}
		// Ranks grow back (-y) from the parade line, normally at most MaxRanks deep. When the
		// blocks would be wider than the parade ground (between the training ground and the
		// horse lines), the gaps shrink first and then every block takes more ranks.
		float limit = Math.Max(10f, _layout.ParadeHalfWidth * 2f);
		float gap = BlockGap;
		float fileScale = 1f;
		float totalWidth = BlockWidth(byClass, gap, fileScale);
		if (totalWidth > limit)
		{
			gap = 1.5f;
			totalWidth = BlockWidth(byClass, gap, fileScale);
		}
		for (int pass = 0; pass < 6 && totalWidth > limit; pass++)
		{
			fileScale *= Math.Max(0.5f, limit / totalWidth);
			totalWidth = BlockWidth(byClass, gap, fileScale);
		}
		float x = -totalWidth * 0.5f;
		Vec2 facing = ToWorldDirection(new Vec2(0f, 1f));
		foreach (List<CampAgent> list in byClass.Values)
		{
			int files = FilesFor(list.Count, fileScale);
			for (int i = 0; i < list.Count; i++)
			{
				int file = i % files;
				int rank = i / files;
				Vec2 local = _layout.ParadeCenter + new Vec2(x + file * FileSpacing, -rank * RowSpacing);
				list[i].Target = ToWorld(local);
				list[i].TargetFacing = facing;
			}
			x += files * FileSpacing + gap;
		}
	}

	private static int FilesFor(int count, float fileScale)
	{
		int files = Math.Max(1, Math.Min(count, Math.Max(MinFilesPerRank, (count + MaxRanks - 1) / MaxRanks)));
		return Math.Max(1, (int)(files * fileScale));
	}

	private static float BlockWidth(SortedDictionary<int, List<CampAgent>> byClass, float gap, float fileScale)
	{
		float width = 0f;
		foreach (List<CampAgent> list in byClass.Values)
		{
			width += FilesFor(list.Count, fileScale) * FileSpacing + gap;
		}
		return width - gap;
	}

	private static int ClassOrder(FormationClass troopClass)
	{
		switch (troopClass)
		{
			case FormationClass.Ranged:
				return 1;
			case FormationClass.Cavalry:
				return 2;
			case FormationClass.HorseArcher:
				return 3;
			default:
				return 0;
		}
	}

	// Scene interaction owns these agents until its original owner releases them.
	internal bool IsSceneConversationHeld(Agent agent) => agent != null && _sceneConversationAgents.Contains(agent);

	internal bool IsCampAgentBusyForConversation(Agent agent) => agent == null || IsFollowingPlayer(agent) || IsSceneConversationHeld(agent) ||
		_envoy?.Agent == agent || IsDuelist(agent) || SlaughterBusy || IsCeremonyAgentHeld(agent);

	internal bool IsCampAgentSeated(Agent agent)
	{
		if (agent == null || !_campAgentsByAgent.TryGetValue(agent, out CampAgent entry) || !entry.Seated
            || entry.RestPose == CampRestPose.None || entry.PoseTransition == CampPoseTransition.Exit) return false;
		return entry.Work == FieldCampLayout.WorkKind.Sit || entry.Work == FieldCampLayout.WorkKind.Eat ||
			entry.Work == FieldCampLayout.WorkKind.Lie || entry.Activity == Activity.Fire || entry.Activity == Activity.Wounded;
	}

	internal bool IsCampPrisoner(Agent agent) => agent != null && _prisoners.Contains(agent);

    internal bool RestoreCampAttentionOwner(Agent agent)
    {
        if (agent == null || !agent.IsActive()) return false;
        if (IsCampPrisoner(agent))
        {
            if (!IsCeremonyAgentHeld(agent) && !SlaughterBusy) ApplyPrisonerPose(agent);
            return true;
        }
        if (!_campAgentsByAgent.TryGetValue(agent, out CampAgent entry)) return false;
        if (entry.FollowingPlayer || IsCampAgentBusyForConversation(agent)) return true;
        entry.Returning = false;
        MoveTo(entry, _assembled ? entry.Target : entry.Home,
            _assembled ? entry.TargetFacing : entry.HomeFacing, run: false);
        return true;
    }

	internal bool CanStartNativeConversation(Agent agent) =>
		agent != null && (_campAgentsByAgent.ContainsKey(agent) || _prisoners.Contains(agent)) &&
		!SlaughterBusy && !IsDuelist(agent) && !IsCeremonyAgentHeld(agent);

	internal bool TryAcquireSceneConversation(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !ReferenceEquals(Mission.Current, Mission) || Mission.IsMissionEnding ||
			SlaughterBusy || IsDuelist(agent) || IsCeremonyAgentHeld(agent)) return false;
		if (!_campAgentsByAgent.TryGetValue(agent, out CampAgent campAgent) && !_prisoners.Contains(agent)) return false;
		if (_sceneConversationAgents.Contains(agent)) return true;
		try
		{
			ReleaseCeremonyReturnForConversation(agent);
            if (!TryPrepareCampNavigation(agent)) return false;
			if (!agent.IsUsingGameObject && agent.CurrentlyUsedGameObject == null)
			{
                bool keepRestPose = campAgent != null && HoldCampRestForConversation(campAgent);
				if (campAgent != null && !keepRestPose)
				{
					ReleasePose(campAgent);
					campAgent.Seated = false;
				}
				StopCampNavigation(agent);
				if (keepRestPose || _prisoners.Contains(agent)) agent.SetIsAIPaused(true);
			}
			_sceneConversationAgents.Add(agent);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "conversation acquisition failed agent=" + agent.Index + " error=" + ex.Message);
			return false;
		}
	}

	internal void ReleaseSceneConversation(Agent agent)
	{
		if (agent == _nativeConversationAgent && Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return;
		if (agent == null || !_sceneConversationAgents.Remove(agent) || !agent.IsActive() ||
			!ReferenceEquals(Mission.Current, Mission) || Mission.IsMissionEnding || SlaughterBusy || IsDuelist(agent) || IsCeremonyAgentHeld(agent)) return;
			if (agent.IsUsingGameObject || agent.CurrentlyUsedGameObject != null) return;
            if (IsFollowingPlayer(agent))
            {
                TryPrepareCampNavigation(agent);
                agent.SetIsAIPaused(false);
                SceneMovementController.TryEnableVanillaSceneFollowBehavior(agent, Agent.Main);
                return;
            }
            StopCampNavigation(agent);
            if (_envoy?.Agent == agent)
		{
			_envoyNavigationActive = false;
			return;
		}
		if (_campAgentsByAgent.TryGetValue(agent, out CampAgent entry))
        {
            if (entry.FollowingPlayer) return;
			entry.Target = entry.Home;
			entry.TargetFacing = entry.HomeFacing;
			entry.Returning = false;
			entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(12f, 28f);
			MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
		}
		else if (_prisoners.Contains(agent)) ApplyPrisonerPose(agent);
	}

	private void OnCampConversationBegin()
	{
		var agent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
		if (TryAcquireSceneConversation(agent)) _nativeConversationAgent = agent;
	}

	private void OnCampConversationEnd()
	{
		Agent agent = _nativeConversationAgent;
		_nativeConversationAgent = null;
		ReleaseSceneConversation(agent);
	}

	private const float PairTalkRadius = 2.5f;
	private List<Vec3> _campFireWorld;

	// ---------- camp life ----------

    // At most 20 residents and two cached corner-corridor checks per frame. Route construction
    // stays in upkeep; reached corners advance and arrivals settle here (one distance test each).
    private void AdvanceCampWalks()
    {
        _campCornerPassageBudget = 2;
        int count = _agents.Count;
        for (int n = 0; n < Math.Min(UpkeepBatch, count); n++)
        {
            _walkCursor = (_walkCursor + 1) % count;
            CampAgent entry = _agents[_walkCursor];
            Agent agent = entry.Agent;
            if (entry.FollowingPlayer || agent == null || !agent.IsActive()
                || IsDuelist(agent) || IsEnvoy(entry) || IsSceneConversationHeld(agent)
                || _campCeremonyActors.ContainsKey(agent) || _ceremonyMoved.Contains(entry)) continue;
            try
            {
                // Pose transitions share this existing 20-agent frame budget; never scan the whole camp.
                if (AdvanceCampPoseTransition(entry) || entry.Seated || entry.MoveBlocked) continue;
                // Settle arrivals here, not only in the 20-per-0.5 s upkeep: at 400 residents that
                // left a walker idling at his stop for up to 10 s before his pose started.
                if (HasArrived(entry)) { ArriveAtCampTarget(entry); continue; }
                if (!CanAdvanceCampCorner(agent, entry.Route, entry.RouteIndex)) continue;
                entry.RouteIndex++;
                IssueCampWalk(entry);
            }
            catch (Exception ex)
            {
                entry.MoveBlocked = true;
                entry.RouteQueued = true;
                entry.Seated = true;
                entry.NextWanderTime = _elapsed + 3f;
                Logger.Log(LogSource, "resident waypoint pending agent=" + agent.Index + " error=" + ex.Message);
            }
        }
    }

	private void UpkeepBatchOfAgents()
	{
		int count = _agents.Count;
		if (count == 0)
		{
			return;
		}
		_campRouteBudget = CampRouteBudgetPerBatch();
        _campStrollBudget = _campRouteBudget;
        int batch = Math.Min(UpkeepBatch, count);
		for (int n = 0; n < batch; n++)
		{
			_upkeepCursor = (_upkeepCursor + 1) % count;
			CampAgent entry = _agents[_upkeepCursor];
			Agent agent = entry.Agent;
			if (agent == null || !agent.IsActive() || IsDuelist(agent) || IsEnvoy(entry) || IsSceneConversationHeld(agent)
					|| _campCeremonyActors.ContainsKey(agent)
                    || (_ceremonyMoved.Count > 0 && _ceremonyMoved.Contains(entry)))
			{
				continue;
			}
            try
            {
                if (entry.FollowingPlayer || entry.PoseTransition != CampPoseTransition.None) continue;
                if (entry.Training) { UpkeepCampArchery(entry); continue; }
                if (!IsInsideCamp(agent.Position) && entry.Seated && !entry.MoveBlocked)
                {
                    RecoverBlockedCampWalk(entry);
                    continue;
                }
                if (entry.MoveBlocked)
                {
                    if (_elapsed < entry.NextWanderTime) continue;
                    if (entry.RouteQueued || _assembled)
                        MoveTo(entry, entry.Target, entry.TargetFacing, run: entry.Run);
                    // A blocked worker still owning his station retries home before camp life resumes.
                    else if ((entry.CanLeavePost || entry.Returning) && agent.Position.AsVec2.DistanceSquared(entry.Home.AsVec2) > ArriveDistanceSquared)
                        MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
                    else
                    {
                        entry.MoveBlocked = false;
                        Refresh(entry);
                    }
                    continue;
                }
                if (!entry.Seated && _elapsed >= entry.MoveDeadline)
                {
                    RecoverBlockedCampWalk(entry);
                    continue;
                }
                if (!entry.Seated && CanAdvanceCampCorner(agent, entry.Route, entry.RouteIndex))
                {
                    entry.RouteIndex++;
                    IssueCampWalk(entry);
                }
                // A resident that stopped inside the arrival radius has arrived, not stalled.
                if (!entry.Seated && _elapsed >= entry.ProgressAt
                    && agent.Position.AsVec2.DistanceSquared(entry.Target.AsVec2) > ArriveDistanceSquared)
                {
                    if (agent.Position.AsVec2.DistanceSquared(entry.LastWalkPosition.AsVec2) < 0.04f)
                    { RecoverBlockedCampWalk(entry); continue; }
                    entry.LastWalkPosition = agent.Position;
                    entry.ProgressAt = _elapsed + 4f;
                }
                if (!_assembled && entry.Seated && entry.Returning && _elapsed >= entry.NextWanderTime)
                {
                    entry.Returning = false;
                    MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
                    continue;
                }
                if (!_assembled && entry.Seated && entry.Activity == Activity.Wander && _elapsed >= entry.NextWanderTime)
                {
                    Refresh(entry);
                    continue;
                }
    			if (!_assembled && entry.Seated && (_elapsed >= entry.NextWanderTime || HasFinishedCampWork(entry))
                    && entry.Activity != Activity.Wounded)
    			{
    				Refresh(entry);
    				continue;
    			}
                // A native alternative is still the requested pose, not a dropped/finished action.
                // Recover only a genuinely lost hold; one-shot work keeps its completion signal above.
                if (!_assembled && entry.Seated && !entry.MoveBlocked && entry.PoseAction != ActionIndexCache.act_none
                    && !HasFinishedCampWork(entry) && !IsCampPosePlaying(entry))
                {
                    StartCampPoseHold(entry, entry.PoseAction, 0f);
                    continue;
                }
                // Per-frame AdvanceCampWalks normally settles arrivals; this is the fallback for walkers it skipped.
    			if (!entry.Seated && HasArrived(entry)) ArriveAtCampTarget(entry);
            }
            catch (Exception ex)
            {
                entry.Seated = true;
                entry.MoveBlocked = true;
                entry.RouteQueued = true;
                entry.NextWanderTime = _elapsed + 3f;
                Logger.Log(LogSource, "resident upkeep failed agent=" + agent.Index + " error=" + ex.Message);
            }
		}
	}

    /// <summary>Same arrival test the upkeep always used: within the arrival radius of the final target.</summary>
    private static bool HasArrived(CampAgent entry) =>
        entry.Agent.Position.AsVec2.DistanceSquared(entry.Target.AsVec2) <= ArriveDistanceSquared;

    private void ArriveAtCampTarget(CampAgent entry)
    {
        Agent agent = entry.Agent;
        // Arrived: release the movement goal before applying an idle animation.
        StopCampNavigation(agent);
        agent.SetIsAIPaused(true);
        agent.LookDirection = new Vec3(entry.TargetFacing.x, entry.TargetFacing.y, 0f);
        if (!_assembled)
        {
            ApplyPose(entry);
        }
        entry.Seated = true;
        // Wanderers dwell like native town walkers (which wait 50-320 s per point); 20-45 s keeps
        // their route demand well inside the scaled per-batch budget.
        entry.NextWanderTime = _elapsed + (entry.Activity == Activity.Wander ? MBRandom.RandomFloatRanged(20f, 45f) : MBRandom.RandomFloatRanged(12f, 25f));
        if (!_assembled && entry.Activity == Activity.Wander) SettleWanderer(entry);
    }

    /// <summary>
    /// Routes per 0.5 s upkeep: 2 for up to 40 residents, then one more per 40, at most 8. A route
    /// averaged 0.29 ms (worst 4.7 ms) on a 25840-cell camp grid (2026-11-05 bench), so 8 stay near 2.3 ms.
    /// </summary>
    private int CampRouteBudgetPerBatch() => Math.Max(2, Math.Min(8, (_agents.Count + 39) / 40 + 1));

	/// <summary>
	/// Camp life (every 25-55 s per agent, in the 0.5 s upkeep batch): a seated or working
	/// man may switch pose, take a short walk to a patrol point and come back, or turn to
	/// talk with the nearest comrade. Sentries only change their look-around pose.
	/// </summary>
	private void Refresh(CampAgent entry)
	{
		entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(25f, 55f);
        bool finishedWork = HasFinishedCampWork(entry);
        if (entry.Activity == Activity.Wander || finishedWork)
        {
            entry.NextWanderTime = _elapsed + 0.5f;
            if (_campStrollBudget <= 0 || _campRouteBudget <= 0) return;
            entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(2f, 5f);
            if (TryNearbyStroll(entry, out Vec3 patrol))
            {
                entry.Returning = finishedWork;
                MoveTo(entry, patrol, entry.HomeFacing, run: false);
            }
            else
            {
                LogCampWalkPending(entry, "no reachable stroll destination");
                entry.PoseVariant++;
                ApplyPose(entry);
            }
            return;
        }
        if (RefreshCampGroundRest(entry)) return;
        float roll = MBRandom.RandomFloat;
		if (entry.Activity == Activity.Sentry || (entry.Activity == Activity.Post && !entry.CanLeavePost) || roll < 0.35f)
		{
			entry.PoseVariant++;
			ApplyPose(entry);
			return;
		}
			if (roll < 0.75f && TryNearbyStroll(entry, out Vec3 stroll))
			{
				entry.Returning = entry.Activity != Activity.Wander;
				MoveTo(entry, stroll, entry.HomeFacing, run: false);
				return;
			}
		CampAgent mate = NearestIdleMate(entry, 3.5f);
		if (mate == null)
		{
			entry.PoseVariant++;
			ApplyPose(entry);
			return;
		}
		// Turn to each other and talk for a while.
		Vec2 between = (mate.Agent.Position.AsVec2 - entry.Agent.Position.AsVec2);
		if (between.LengthSquared > 0.01f)
		{
			between = between.Normalized();
			entry.Agent.LookDirection = new Vec3(between.x, between.y, 0f);
			mate.Agent.LookDirection = new Vec3(-between.x, -between.y, 0f);
		}
		PlayLoop(entry, entry.Work == FieldCampLayout.WorkKind.Sit || entry.Work == FieldCampLayout.WorkKind.Eat || entry.Work == FieldCampLayout.WorkKind.Lie ? PoseFor(entry) : GossipActions);
		PlayLoop(mate, mate.Work == FieldCampLayout.WorkKind.Sit || mate.Work == FieldCampLayout.WorkKind.Eat || mate.Work == FieldCampLayout.WorkKind.Lie ? PoseFor(mate) : GossipActions);
		mate.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(12f, 28f);
	}

    // Camp life supplies every movement goal. Keep native daily behaviours from
    // periodically selecting a town wander target outside this temporary camp.
    private sealed class CampMovementBehaviorGroup : AgentBehaviorGroup
    {
        public CampMovementBehaviorGroup(AgentNavigator navigator, Mission mission) : base(navigator, mission) { }
        public override float GetScore(bool isSimulation) => 10f;
    }

		private static bool TryPrepareCampNavigation(Agent agent)
		{
			var component = agent.GetComponent<CampaignAgentComponent>();
			if (component == null) return false;
			var navigator = component.AgentNavigator ?? component.CreateAgentNavigator();
            navigator.RemoveBehaviorGroup<CampMovementBehaviorGroup>();
			if (navigator.GetBehaviorGroup<DailyBehaviorGroup>() == null) navigator.AddBehaviorGroup<DailyBehaviorGroup>();
			return true;
		}

		private static void StopCampNavigation(Agent agent)
		{
			var navigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
            var daily = navigator?.GetBehaviorGroup<DailyBehaviorGroup>();
            daily?.DisableScriptedBehavior();
            daily?.DisableAllBehaviors();
            if (navigator != null)
            {
                if (navigator.GetBehaviorGroup<CampMovementBehaviorGroup>() == null)
                    navigator.AddBehaviorGroup<CampMovementBehaviorGroup>();
                navigator.RefreshBehaviorGroups(false);
                navigator.ClearTarget();
            }
			agent?.DisableScriptedMovement();
			agent?.ClearTargetFrame();
		}

		private bool TryNearbyStroll(CampAgent entry, out Vec3 target)
		{
			target = entry.Home;
            if (_campStrollBudget <= 0 || _campRouteBudget <= 0) return false;
            _campStrollBudget--;
            // Dense layouts need nearby choices rather than only distant layout slots.
            for (int i = 0; i < 6; i++)
            {
                float angle = MBRandom.RandomFloat * MathF.PI * 2f;
                float distance = MBRandom.RandomFloatRanged(1.8f, 4f);
                Vec3 candidate = entry.Agent.Position + new Vec3(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance, 0f);
                if (candidate.AsVec2.DistanceSquared(entry.Home.AsVec2) <= 144f &&
                    FieldCampSite.TryResolveGroundedPosition(Mission.Scene, candidate, 0.4f, out target) &&
                    IsInsideCamp(target) && !IsReservedCeremonyPosition(target)
                    && IsCampPositionAvailable(entry.Agent, target, 0.8f))
                {
                    _campRouteBudget--;
                    if (TryCampRoute(entry.Agent.Position, target, out Vec3[] route))
                    { entry.Route = route; entry.RoutePrepared = true; return true; }
                    if (_campRouteBudget <= 0) return false;
                }
            }
			int count = _layout.WanderSlots.Count;
			int start = count > 0 ? MBRandom.RandomInt(count) : 0;
			for (int i = 0; i < Math.Min(count, 12); i++)
			{
				Vec3 candidate = ToWorld(_layout.WanderSlots[(start + i) % count].Position);
				if (candidate.AsVec2.DistanceSquared(entry.Home.AsVec2) > 144f ||
					candidate.AsVec2.DistanceSquared(entry.Agent.Position.AsVec2) < 4f ||
					!FieldCampSite.TryResolveGroundedPosition(Mission.Scene, candidate, 0.4f, out target) ||
                    (!IsInsideCamp(target) || IsReservedCeremonyPosition(target))
                    || !IsCampPositionAvailable(entry.Agent, target, 0.8f)) continue;
                _campRouteBudget--;
                if (TryCampRoute(entry.Agent.Position, target, out Vec3[] route))
                { entry.Route = route; entry.RoutePrepared = true; return true; }
                if (_campRouteBudget <= 0) return false;
			}
            return false;
        }

        /// <summary>
        /// A wanderer just arrived at a stop. Like native paired animation points: when another idle
        /// resident stands within 2.5 m, both turn to each other, this one talks and the other listens;
        /// a working mate only turns his head. Otherwise he faces the nearest fire or comrade instead of
        /// the camp's default facing. Runs once per arrival (arrivals are capped by the 2-8-routes-per-0.5 s
        /// budget): one linear pass over the residents and one over the cached fires.
        /// </summary>
        private void SettleWanderer(CampAgent entry)
        {
            Agent agent = entry.Agent;
            CampAgent mate = NearestIdleMate(entry, PairTalkRadius);
            if (mate != null)
            {
                Vec2 between = mate.Agent.Position.AsVec2 - agent.Position.AsVec2;
                if (between.LengthSquared > 0.01f)
                {
                    between = between.Normalized();
                    agent.LookDirection = new Vec3(between.x, between.y, 0f);
                    entry.TargetFacing = between;
                    // Only an idle wanderer leaves his pose; workers keep working and just look over.
                    mate.Agent.LookDirection = new Vec3(-between.x, -between.y, 0f);
                    if (mate.Activity == Activity.Wander)
                    {
                        mate.TargetFacing = -between;
                        PlayLoop(mate, ListenActions);
                        // The listener stays for the talk instead of walking off mid-sentence.
                        mate.NextWanderTime = Math.Max(mate.NextWanderTime, entry.NextWanderTime);
                    }
                }
                PlayLoop(entry, TalkActions);
                return;
            }
            if (TryNearestFire(agent.Position, 12f, out Vec3 fire))
            {
                Vec2 toFire = fire.AsVec2 - agent.Position.AsVec2;
                if (toFire.LengthSquared > 0.25f)
                {
                    toFire = toFire.Normalized();
                    agent.LookDirection = new Vec3(toFire.x, toFire.y, 0f);
                    entry.TargetFacing = toFire;
                }
            }
        }

        /// <summary>Starts a verified ambient hold without restarting its native alternatives.</summary>
        private void PlayLoop(CampAgent entry, string[] pool) => TryPlayCampPose(entry, pool);

        /// <summary>Fire positions are fixed for the mission; converted to world once, then a linear scan of a few dozen.</summary>
        private bool TryNearestFire(Vec3 from, float radius, out Vec3 fire)
        {
            fire = default(Vec3);
            if (_campFireWorld == null)
            {
                _campFireWorld = new List<Vec3>();
                foreach (FieldCampLayout.Prop prop in _layout.Props)
                    if (prop.Kind == FieldCampLayout.PropKind.Fire) _campFireWorld.Add(ToWorld(prop.Position));
            }
            float best = radius * radius;
            bool found = false;
            foreach (Vec3 candidate in _campFireWorld)
            {
                float d = candidate.AsVec2.DistanceSquared(from.AsVec2);
                if (d < best) { best = d; fire = candidate; found = true; }
            }
            return found;
        }

        private CampAgent NearestIdleMate(CampAgent entry, float radius)
	{
		CampAgent best = null;
		float bestDistance = radius * radius;
		Vec2 here = entry.Agent.Position.AsVec2;
		for (int i = 0; i < _agents.Count; i++)
		{
			CampAgent other = _agents[i];
			if (other == entry || other.Training || other.FollowingPlayer || !other.Seated || other.Agent == null || !other.Agent.IsActive()
				|| IsSceneConversationHeld(other.Agent) || IsEnvoy(other) || IsDuelist(other.Agent)
					|| _campCeremonyActors.ContainsKey(other.Agent)
                    || (_ceremonyMoved.Count > 0 && _ceremonyMoved.Contains(other))
					|| other.Activity == Activity.Sentry || other.Activity == Activity.Wounded || (other.Activity == Activity.Post && !other.CanLeavePost))
			{
				continue;
			}
			float d = other.Agent.Position.AsVec2.DistanceSquared(here);
			if (d < bestDistance)
			{
				bestDistance = d;
				best = other;
			}
		}
		return best;
	}

	private void PlayOnce(Agent agent, string[] pool, int variant)
	{
		ActionIndexCache action = ResolveAction(pool[Math.Abs(variant) % pool.Length]);
		if (action == ActionIndexCache.act_none)
		{
			return;
		}
		try
		{
			agent.SetActionChannel(0, in action, true, AnimFlags.anf_lock_movement);
		}
		catch
		{
		}
	}

	private void HoldAt(CampAgent entry)
	{
		entry.Seated = false;
		MoveTo(entry, entry.Target, entry.TargetFacing, run: false);
	}

    private void MoveTo(CampAgent entry, Vec3 target, Vec2 facing, bool run)
    {
        Agent agent = entry.Agent;
        if (agent == null || !agent.IsActive()) return;
        try
        {
            // Only a ground rest waits for a usable route before releasing. Non-resting workers
            // (especially active archers) still release tools/training immediately as before.
            bool heldGroundPose = entry.RestPose != CampRestPose.None;
            if (!heldGroundPose) ReleasePose(entry);
            StopCampNavigation(agent);
            entry.Target = target;
            entry.TargetFacing = facing;
            entry.Run = run;
            Vec3[] preparedRoute = entry.Route;
            entry.Route = Array.Empty<Vec3>();
            entry.RouteIndex = 0;
            entry.MoveBlocked = entry.RouteQueued = false;
            if (agent.Position.AsVec2.DistanceSquared(target.AsVec2) <= ArriveDistanceSquared)
            {
                SettleCampAgent(entry, blocked: false);
                return;
            }
            bool prepared = entry.RoutePrepared && preparedRoute.Length > 0
                && preparedRoute[preparedRoute.Length - 1].AsVec2.DistanceSquared(target.AsVec2) <= 0.36f;
            entry.RoutePrepared = false;
            if (!prepared && _campRouteBudget <= 0)
            {
                bool returnAfterVisit = entry.Returning;
                // A route-budget deferral is not a failed walk home; do not let it spend a station retry.
                int homeRetries = entry.HomeRetries;
                SettleCampAgent(entry, blocked: true);
                entry.Returning = returnAfterVisit;
                entry.HomeRetries = homeRetries;
                entry.RouteQueued = true;
                entry.NextWanderTime = _elapsed + 0.5f;
                return;
            }
            Vec3[] route = prepared ? preparedRoute : Array.Empty<Vec3>();
            if (!prepared) _campRouteBudget--;
            if (IsReservedCeremonyPosition(target) || (!prepared && !TryCampRoute(agent.Position, target, out route)))
            {
                SettleCampAgent(entry, blocked: true);
                return;
            }
            entry.Route = route;
            entry.Seated = false;
            entry.MoveDeadline = _elapsed + Math.Max(30f, agent.Position.AsVec2.Distance(target.AsVec2) * 3f);
            if (TryBeginCampPoseExit(entry)) return;
            if (heldGroundPose) ReleasePose(entry);
            IssueCampWalk(entry);
        }
        catch (Exception ex)
        {
            SettleCampAgent(entry, blocked: true);
            Logger.Log(LogSource, "move failed agent=" + agent.Index + " error=" + ex.Message);
        }
    }

    private bool TryCampRoute(Vec3 from, Vec3 target, out Vec3[] route)
    {
        route = Array.Empty<Vec3>();
        if (!IsInsideCamp(target)) return false;
        if (FieldCampSite.TryBuildGroundedRoute(Mission.Scene, from, target, out route, out bool covered, stayInsideCamp: true))
        {
            // Interior routes stay interior. An outside start may return through the gate.
            bool interiorStart = IsInsideCamp(from);
            for (int i = 0; interiorStart && i < route.Length; i++)
                if (!IsInsideCamp(route[i])) { route = Array.Empty<Vec3>(); return false; }
            return true;
        }
        // Unknown/uncovered floor cannot authorize a long native detour through the fence.
        return false;
    }

    private bool HasFinishedCampWork(CampAgent entry)
    {
        return !entry.Returning && entry.Work != FieldCampLayout.WorkKind.None
            && (entry.Activity == Activity.Work || (entry.Activity == Activity.Post && entry.CanLeavePost))
            && entry.PoseAction != ActionIndexCache.act_none
            && entry.PoseTransition == CampPoseTransition.None && !IsCampPosePlaying(entry);
    }

    private bool CanAdvanceCampCorner(Agent agent, Vec3[] route, int index)
    {
        if (index >= route.Length) return false;
        float distanceSquared = agent.Position.AsVec2.DistanceSquared(route[index].AsVec2);
        if (distanceSquared <= RouteCornerDistanceSquared) return true;
        // Native scripted movement can finish short of a sub-metre target. Allow
        // passage to the next leg only after checking the actual connecting corridor.
        if (distanceSquared > 0.64f || index + 1 >= route.Length || _campCornerPassageBudget <= 0) return false;
        _campCornerPassageBudget--;
        return FieldCampSite.TryValidateGroundedPassage(Mission.Scene, agent.Position, route[index + 1], 0.15f);
    }

    private void IssueCampWalk(CampAgent entry)
    {
        // Do not command a tiny correction back to the source cell before starting the route.
        while (entry.RouteIndex < entry.Route.Length
            && entry.Agent.Position.AsVec2.DistanceSquared(entry.Route[entry.RouteIndex].AsVec2) <= RouteCornerDistanceSquared)
            entry.RouteIndex++;
        Vec3 goal = entry.RouteIndex < entry.Route.Length ? entry.Route[entry.RouteIndex] : entry.Target;
        entry.Agent.Controller = AgentControllerType.AI;
        entry.Agent.SetIsAIPaused(false);
        entry.Agent.SetMaximumSpeedLimit(entry.Run ? -1f : 1.5f, isMultiplier: false);
        entry.LastWalkPosition = entry.Agent.Position;
        entry.ProgressAt = _elapsed + 4f;
        var world = new WorldPosition(Mission.Scene, UIntPtr.Zero, goal, false);
        var flags = Agent.AIScriptedFrameFlags.NoAttack;
        if (entry.RouteIndex + 1 < entry.Route.Length) flags |= Agent.AIScriptedFrameFlags.NeverSlowDown;
        if (!entry.Run) flags |= Agent.AIScriptedFrameFlags.DoNotRun;
        // Camp routes own position progression. Apply the final facing only after arrival.
        entry.Agent.SetScriptedPosition(ref world, false, flags);
    }

    private void RecoverBlockedCampWalk(CampAgent entry)
    {
        // Replan from the actual position. Never teleport on collision or stall.
        Vec3 goal = entry.Target;
        LogCampWalkPending(entry, "walk stalled or deadline reached");
        SettleCampAgent(entry, blocked: true);
        entry.Target = goal;
        entry.RouteQueued = _assembled || !IsInsideCamp(entry.Agent.Position);
        if (entry.RouteQueued && !IsInsideCamp(entry.Agent.Position)) entry.Target = entry.Home;
        entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(1f, 3f);
    }

    private void LogCampWalkPending(CampAgent entry, string reason)
    {
        if (_elapsed < _nextCampWalkDiagnosticTime) return;
        _nextCampWalkDiagnosticTime = _elapsed + 10f;
        Logger.Log(LogSource, "resident walk pending agent=" + entry.Agent.Index + " reason=" + reason
            + " activity=" + entry.Activity + " work=" + entry.Work + " seated=" + entry.Seated
            + " route=" + entry.RouteIndex + "/" + entry.Route.Length
            + " position=" + entry.Agent.Position + " target=" + entry.Target
            + " waypoint=" + (entry.RouteIndex < entry.Route.Length ? entry.Route[entry.RouteIndex] : entry.Target)
            + " nativeTarget=" + entry.Agent.GetTargetPosition() + " scripted=" + entry.Agent.GetScriptedFlags()
            + " paused=" + entry.Agent.IsPaused + " velocity=" + entry.Agent.MovementVelocity
            + " action=" + entry.Agent.GetCurrentAction(0).Index + " aiTick=" + Mission.AllowAiTicking);
    }

    private bool IsCampPositionAvailable(Agent agent, Vec3 position, float clearance = 0.8f)
    {
        float separationSquared = clearance * clearance;
        // Native spatial query reuses one list; do not scan the whole mission for every candidate.
        Mission.GetNearbyAgents(position.AsVec2, clearance, _campNearbyAgents);
        foreach (Agent other in _campNearbyAgents)
        {
            if (other != agent && other.IsActive() && other.Position.AsVec2.DistanceSquared(position.AsVec2) < separationSquared)
                return false;
        }
        // Keep work stations reserved; a wanderer's initial spawn point is not a permanent station.
        foreach (CampAgent entry in _agents)
        {
            if (entry.Agent != agent && entry.Agent?.IsActive() == true
                && ((entry.Activity != Activity.Wander && entry.Home.AsVec2.DistanceSquared(position.AsVec2) < separationSquared)
                    || ((!entry.Seated || entry.RouteQueued)
                        && entry.Target.AsVec2.DistanceSquared(position.AsVec2) < separationSquared))) return false;
        }
        foreach (CeremonyMove move in _ceremonyWalkers)
        {
            if (move.Agent != agent && move.Agent?.IsActive() == true
                && move.Goal.AsVec2.DistanceSquared(position.AsVec2) < separationSquared) return false;
        }
        return true;
    }

    private bool IsInsideCamp(Vec3 position)
    {
        Vec3 local = _campFrame.TransformToLocal(position);
        float margin = 0.8f * _campScale;
        return Math.Abs(local.x) < _layout.HalfX * _campScale - margin
            && local.y > _layout.Back * _campScale + margin
            && local.y < _layout.Front * _campScale - margin;
    }

    private void SettleCampAgent(CampAgent entry, bool blocked)
    {
        StopCampNavigation(entry.Agent);
        entry.Agent.SetIsAIPaused(true);
        entry.Route = Array.Empty<Vec3>();
        entry.RouteIndex = 0;
        entry.RoutePrepared = false;
        entry.Seated = true;
        entry.Returning = false;
        entry.MoveBlocked = blocked;
        entry.RouteQueued = false;
        entry.NextWanderTime = _elapsed + (entry.Activity == Activity.Wander ? MBRandom.RandomFloatRanged(20f, 45f) : MBRandom.RandomFloatRanged(12f, 25f));
        if (!_assembled) ApplyPose(entry);
        if (blocked) entry.NextWanderTime = _elapsed + MBRandom.RandomFloatRanged(1f, 3f);
        else if (!_assembled && entry.Activity == Activity.Wander) SettleWanderer(entry);
    }

	private void ApplyPose(CampAgent entry)
    {
        if (entry.Work == FieldCampLayout.WorkKind.Archer && !entry.Returning
            && entry.Agent.Position.AsVec2.DistanceSquared(entry.Home.AsVec2) <= ArriveDistanceSquared
            && TryStartCampArchery(entry)) return;
        string[] pool = PoseFor(entry);
        if (pool == null) return;
        bool awayFromStation = entry.Work != FieldCampLayout.WorkKind.None
            && entry.Agent.Position.AsVec2.DistanceSquared(entry.Home.AsVec2) > ArriveDistanceSquared;
        if (!awayFromStation) entry.HomeRetries = 0;

        if (pool == StandActions && awayFromStation && entry.Returning)
        {
            // On a planned visit: loiter for the visit's own timer, then walk home.
            pool = LoiterActions;
        }
        else if (pool == StandActions && awayFromStation && entry.Activity != Activity.Wounded
            && entry.Activity != Activity.Sentry && (entry.Activity != Activity.Post || entry.CanLeavePost))
        {
            // A worker stopped short of his station (blocked return, stall) keeps the station
            // and retries the walk home; converting him to a wanderer drained every work post.
            entry.Returning = true;
            entry.NextWanderTime = Math.Min(entry.NextWanderTime, _elapsed + MBRandom.RandomFloatRanged(3f, 6f));
            pool = LoiterActions;
        }
        else if (pool == StandActions && !entry.Returning && entry.Activity != Activity.Wounded
            && entry.Activity != Activity.Sentry && (entry.Activity != Activity.Post || entry.CanLeavePost))
        {
            // Releasing to act_none here paused every wanderer in a bare T-stand until the
            // stroll budget reached him. Loiter in place, then walk on after a short pause.
            entry.Work = FieldCampLayout.WorkKind.None;
            entry.Activity = Activity.Wander;
            entry.NextWanderTime = Math.Min(entry.NextWanderTime, _elapsed + MBRandom.RandomFloatRanged(2f, 5f));
            pool = LoiterActions;
        }
        if (TryPlayCampPose(entry, pool)) return;
        if (entry.Activity != Activity.Wounded && entry.Activity != Activity.Sentry
            && (entry.Activity != Activity.Post || entry.CanLeavePost))
        {
            entry.Work = FieldCampLayout.WorkKind.None;
            entry.Activity = Activity.Wander;
            entry.NextWanderTime = _elapsed + 1f;
        }
    }

	private static string[] PoseFor(CampAgent entry)
	{
        // A temporary stop after a blocked return is not the original work station.
        if (entry.Agent.Position.AsVec2.DistanceSquared(entry.Home.AsVec2) > ArriveDistanceSquared)
            return StandActions;
        switch (entry.Work)
		{
			case FieldCampLayout.WorkKind.Sit:
                return SitActions;
			case FieldCampLayout.WorkKind.Eat:
                return SitActions;
			case FieldCampLayout.WorkKind.Cook:
				return CookActions;
			case FieldCampLayout.WorkKind.Smith:
				return SmithActions;
			case FieldCampLayout.WorkKind.Repair:
				return RepairActions;
			case FieldCampLayout.WorkKind.Archer:
				return LookoutActions;
			case FieldCampLayout.WorkKind.Groom:
				return GroomActions;
			case FieldCampLayout.WorkKind.Lean:
				return LeanActions;
			case FieldCampLayout.WorkKind.Gossip:
				return GossipActions;
			case FieldCampLayout.WorkKind.Lie:
				return LieActions;
			case FieldCampLayout.WorkKind.Chop:
				return ChopActions;
			case FieldCampLayout.WorkKind.Shovel:
				return ShovelActions;
			case FieldCampLayout.WorkKind.Wash:
				return WashActions;
			case FieldCampLayout.WorkKind.Write:
				return WriteActions;
			case FieldCampLayout.WorkKind.Pickup:
				return PickupActions;
			case FieldCampLayout.WorkKind.Table:
				return TableActions;
			case FieldCampLayout.WorkKind.Lookout:
				return LookoutActions;
		}
		switch (entry.Activity)
		{
			case Activity.Fire:
			case Activity.Wounded:
				return SitActions;
			case Activity.Sentry:
				return SentryActions;
			default:
				return StandActions;
		}
	}

	/// <summary>An archer at the butts takes his bow in hand; men without one just stand and watch.</summary>
	private static bool WieldBow(Agent agent)
	{
		try
		{
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				MissionWeapon weapon = agent.Equipment[slot];
				if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
				{
					continue;
				}
				WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;
				if (weaponClass == WeaponClass.Bow || weaponClass == WeaponClass.Crossbow)
				{
					agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.Instant, false);
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void ReleasePose(CampAgent entry)
	{
        entry.PoseAction = ActionIndexCache.act_none;
        entry.RestPose = CampRestPose.None;
        entry.PoseTransition = CampPoseTransition.None;
        entry.TransitionAction = ActionIndexCache.act_none;
        entry.Training = false;
        HideHandItems(entry);
        RestoreCampPracticeEquipment(entry);
        try
        {
            entry.Agent?.DisableScriptedCombatMovement();
            entry.Agent?.SetMaximumSpeedLimit(-1f, isMultiplier: false);
            entry.Agent?.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
            entry.Agent?.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
            entry.Agent?.SetActionChannel(0, in ActionIndexCache.act_none, true, blendInPeriod: 0f, blendOutPeriodToNoAnim: 0f);
            entry.Agent?.SetActionChannel(1, in ActionIndexCache.act_none, true, blendInPeriod: 0f, blendOutPeriodToNoAnim: 0f);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Same mechanism as native AnimationPoint: weapons are sheathed and the clip's tool is
	/// attached to ItemR/ItemL through AgentNavigator.SetPrefabVisibility, which reuses one
	/// synched component per bone and prefab. Runs only when a pose starts, never per frame.
	/// </summary>
	private static void ShowHandItems(CampAgent entry, string action)
	{
		HideHandItems(entry);
		if (!HandItems.TryGetValue(action, out string[] items)) return;
		Agent agent = entry.Agent;
		try
		{
			var navigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			if (navigator == null || agent.AgentVisuals == null) return;
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			for (int hand = 0; hand < 2; hand++)
			{
				if (string.IsNullOrEmpty(items[hand])) continue;
				sbyte bone = agent.AgentVisuals.GetRealBoneIndex(hand == 0 ? HumanBone.ItemR : HumanBone.ItemL);
				navigator.SetPrefabVisibility(bone, items[hand], true);
				if (hand == 0) { entry.RightHandItem = items[hand]; entry.RightHandBone = bone; }
				else { entry.LeftHandItem = items[hand]; entry.LeftHandBone = bone; }
			}
		}
		catch (Exception ex)
		{
			HideHandItems(entry);
			Logger.Log(LogSource, "camp hand item failed agent=" + agent.Index + " action=" + action + " error=" + ex.Message);
		}
	}

	private static void HideHandItems(CampAgent entry)
	{
		if (entry.RightHandItem == null && entry.LeftHandItem == null) return;
		try
		{
			var navigator = entry.Agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			if (navigator != null)
			{
				if (entry.RightHandItem != null) navigator.SetPrefabVisibility(entry.RightHandBone, entry.RightHandItem, false);
				if (entry.LeftHandItem != null) navigator.SetPrefabVisibility(entry.LeftHandBone, entry.LeftHandItem, false);
			}
		}
		catch
		{
		}
		entry.RightHandItem = entry.LeftHandItem = null;
	}

	private ActionIndexCache ResolveAction(string name)
	{
		if (!_actionCache.TryGetValue(name, out ActionIndexCache action))
		{
			try
			{
				action = ActionIndexCache.Create(name);
			}
			catch
			{
				action = ActionIndexCache.act_none;
			}
			_actionCache[name] = action;
		}
		return action;
	}

	/// <summary>Scene line for the shout prompt while the camp mission runs (built per request, no I/O after first load).</summary>
	internal static string BuildSceneDescription(string location)
	{
		FieldCampMissionLogic camp = Current;
		return ConversationRuntimeTexts.FormatPrompt(camp != null && camp._assembled ? "AF_FC_SceneAssembled" : "AF_FC_Scene", ("LOCATION", location ?? ""));
	}

	// ---------- camp-local coordinates ----------

	private Vec3 ToWorld(Vec2 local)
	{
		Vec3 world = _campFrame.TransformToParent(new Vec3(local.x * _campScale, local.y * _campScale, 0f));
		world.z = GroundHeight(Mission.Scene, world);
		return world;
	}

	private Vec2 ToWorldDirection(Vec2 local)
	{
		Vec3 dir = _campFrame.rotation.TransformToParent(new Vec3(local.x, local.y, 0f));
		Vec2 flat = dir.AsVec2;
		return flat.LengthSquared < 0.0001f ? new Vec2(0f, 1f) : flat.Normalized();
	}

	internal static float GroundHeight(Scene scene, Vec3 position)
	{
		try
		{
				if (FieldCampSite.TryGetCachedTerrainHeight(scene, position.AsVec2, out float height)) return height;
				return scene.GetTerrainHeight(position.AsVec2);
		}
		catch
		{
			return position.z;
		}
	}
}
