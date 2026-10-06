using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class FleeBehavior : AgentBehavior
{
	private abstract class FleeGoalBase
	{
		protected readonly AgentNavigator _navigator;

		protected readonly Agent _ownerAgent;

		protected FleeGoalBase(AgentNavigator navigator, Agent ownerAgent)
		{
			_navigator = navigator;
			_ownerAgent = ownerAgent;
		}

		public abstract void TargetReached();

		public abstract void GoToTarget();

		public abstract bool IsGoalAchievable();

		public abstract bool IsGoalAchieved();
	}

	private class FleeAgentTarget : FleeGoalBase
	{
		public Agent Savior { get; private set; }

		public FleeAgentTarget(AgentNavigator navigator, Agent ownerAgent, Agent savior)
			: base(navigator, ownerAgent)
		{
			Savior = savior;
		}

		public override void GoToTarget()
		{
			_navigator.SetTargetFrame(Savior.GetWorldPosition(), Savior.Frame.rotation.f.AsVec2.RotationInRadians, 0.2f, 0.02f, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown);
		}

		public override bool IsGoalAchievable()
		{
			if (Savior.GetWorldPosition().GetNearestNavMesh() != UIntPtr.Zero && _navigator.TargetPosition.IsValid && Savior.IsActive())
			{
				return Savior.CurrentWatchState != Agent.WatchState.Alarmed;
			}
			return false;
		}

		public override bool IsGoalAchieved()
		{
			if (_navigator.TargetPosition.IsValid)
			{
				return _navigator.TargetPosition.GetGroundVec3().Distance(_ownerAgent.Position) <= _ownerAgent.GetInteractionDistanceToUsable(Savior);
			}
			return false;
		}

		public override void TargetReached()
		{
			_ownerAgent.SetActionChannel(0, in ActionIndexCache.act_cheer_1, ignorePriority: true, (AnimFlags)0uL);
			_ownerAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
			_ownerAgent.DisableScriptedMovement();
			Savior.DisableScriptedMovement();
			Savior.SetLookAgent(_ownerAgent);
			_ownerAgent.SetLookAgent(Savior);
		}
	}

	private class FleePassageTarget : FleeGoalBase
	{
		public Passage EscapePortal { get; private set; }

		public FleePassageTarget(AgentNavigator navigator, Agent ownerAgent, Passage escapePortal)
			: base(navigator, ownerAgent)
		{
			EscapePortal = escapePortal;
		}

		public override void GoToTarget()
		{
			_navigator.SetTarget(EscapePortal);
		}

		public override bool IsGoalAchievable()
		{
			if (EscapePortal.GetVacantStandingPointForAI(_ownerAgent) != null)
			{
				return !EscapePortal.IsDestroyed;
			}
			return false;
		}

		public override bool IsGoalAchieved()
		{
			return EscapePortal.GetVacantStandingPointForAI(_ownerAgent)?.IsUsableByAgent(_ownerAgent) ?? false;
		}

		public override void TargetReached()
		{
		}
	}

	private class FleePositionTarget : FleeGoalBase
	{
		public Vec3 Position { get; private set; }

		public FleePositionTarget(AgentNavigator navigator, Agent ownerAgent, Vec3 position)
			: base(navigator, ownerAgent)
		{
			Position = position;
		}

		public override void GoToTarget()
		{
		}

		public override bool IsGoalAchievable()
		{
			return _navigator.TargetPosition.IsValid;
		}

		public override bool IsGoalAchieved()
		{
			if (_navigator.TargetPosition.IsValid)
			{
				return _navigator.IsTargetReached();
			}
			return false;
		}

		public override void TargetReached()
		{
		}
	}

	private class FleeCoverTarget : FleeGoalBase
	{
		public FleeCoverTarget(AgentNavigator navigator, Agent ownerAgent)
			: base(navigator, ownerAgent)
		{
		}

		public override void GoToTarget()
		{
			_ownerAgent.DisableScriptedMovement();
		}

		public override bool IsGoalAchievable()
		{
			return true;
		}

		public override bool IsGoalAchieved()
		{
			return true;
		}

		public override void TargetReached()
		{
		}
	}

	private enum State
	{
		None,
		Afraid,
		LookForPlace,
		Flee,
		Complain
	}

	private enum FleeTargetType
	{
		Indoor,
		Guard,
		Cover
	}

	public const float ScoreThreshold = 1f;

	public const float DangerDistance = 5f;

	public const float ImmediateDangerDistance = 2f;

	public const float DangerDistanceSquared = 25f;

	public const float ImmediateDangerDistanceSquared = 4f;

	private readonly MissionAgentHandler _missionAgentHandler;

	private readonly MissionFightHandler _missionFightHandler;

	private State _state;

	private readonly BasicMissionTimer _reconsiderFleeTargetTimer;

	private const float ReconsiderImmobilizedFleeTargetTime = 0.5f;

	private const float ReconsiderDefaultFleeTargetTime = 1f;

	private FleeGoalBase _selectedGoal;

	private BasicMissionTimer _scareTimer;

	private float _scareTime;

	private BasicMissionTimer _complainToGuardTimer;

	private const float ComplainToGuardTime = 2f;

	private FleeTargetType _selectedFleeTargetType;

	private FleeTargetType SelectedFleeTargetType
	{
		get
		{
			return _selectedFleeTargetType;
		}
		set
		{
			if (value != _selectedFleeTargetType)
			{
				_selectedFleeTargetType = value;
				MBActionSet actionSet = base.OwnerAgent.ActionSet;
				ActionIndexCache actionCode = base.OwnerAgent.GetCurrentAction(1);
				if (_selectedFleeTargetType != FleeTargetType.Cover && !actionSet.AreActionsAlternatives(in actionCode, in ActionIndexCache.act_scared_idle_1) && !actionSet.AreActionsAlternatives(in actionCode, in ActionIndexCache.act_scared_reaction_1))
				{
					base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_scared_reaction_1, ignorePriority: false, (AnimFlags)0uL);
				}
				if (_selectedFleeTargetType == FleeTargetType.Cover)
				{
					BeAfraid();
				}
				_selectedGoal.GoToTarget();
			}
		}
	}

	public FleeBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		_missionFightHandler = base.Mission.GetMissionBehavior<MissionFightHandler>();
		_reconsiderFleeTargetTimer = new BasicMissionTimer();
		_state = State.None;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		switch (_state)
		{
		case State.None:
			base.OwnerAgent.DisableScriptedMovement();
			base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_scared_reaction_1, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat);
			_selectedGoal = new FleeCoverTarget(base.Navigator, base.OwnerAgent);
			SelectedFleeTargetType = FleeTargetType.Cover;
			break;
		case State.Afraid:
			if (_scareTimer.ElapsedTime > _scareTime)
			{
				_state = State.LookForPlace;
				_scareTimer = null;
			}
			break;
		case State.LookForPlace:
			LookForPlace();
			break;
		case State.Flee:
			Flee();
			break;
		case State.Complain:
			if (_complainToGuardTimer != null && _complainToGuardTimer.ElapsedTime > 2f)
			{
				_complainToGuardTimer = null;
				base.OwnerAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: false, (AnimFlags)0uL);
				base.OwnerAgent.SetLookAgent(null);
				(_selectedGoal as FleeAgentTarget).Savior.SetLookAgent(null);
				AlarmedBehaviorGroup.AlarmAgent((_selectedGoal as FleeAgentTarget).Savior);
				_state = State.LookForPlace;
			}
			break;
		}
	}

	private Vec3 GetDangerPosition()
	{
		Vec3 zero = Vec3.Zero;
		if (_missionFightHandler != null)
		{
			IEnumerable<Agent> dangerSources = _missionFightHandler.GetDangerSources(base.OwnerAgent);
			if (dangerSources.Any())
			{
				foreach (Agent item in dangerSources)
				{
					zero += item.Position;
				}
				zero /= (float)dangerSources.Count();
			}
		}
		return zero;
	}

	private bool IsThereDanger()
	{
		if (_missionFightHandler == null)
		{
			return false;
		}
		return _missionFightHandler.GetDangerSources(base.OwnerAgent).Any();
	}

	private float GetPathScore(WorldPosition startWorldPos, WorldPosition targetWorldPos)
	{
		float num = 1f;
		NavigationPath navigationPath = new NavigationPath();
		base.Mission.Scene.GetPathBetweenAIFaces(startWorldPos.GetNearestNavMesh(), targetWorldPos.GetNearestNavMesh(), startWorldPos.AsVec2, targetWorldPos.AsVec2, 0f, navigationPath, null);
		Vec2 asVec = GetDangerPosition().AsVec2;
		float toAngle = MBMath.WrapAngle((asVec - startWorldPos.AsVec2).RotationInRadians);
		float num2 = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngle((navigationPath.Size > 0) ? (navigationPath.PathPoints[0] - startWorldPos.AsVec2).RotationInRadians : (targetWorldPos.AsVec2 - startWorldPos.AsVec2).RotationInRadians), toAngle)) / System.MathF.PI * 1f;
		float num3 = startWorldPos.AsVec2.DistanceSquared(asVec);
		if (navigationPath.Size > 0)
		{
			float num4 = float.MaxValue;
			Vec2 line = startWorldPos.AsVec2;
			for (int i = 0; i < navigationPath.Size; i++)
			{
				float num5 = Vec2.DistanceToLineSegmentSquared(navigationPath.PathPoints[i], line, asVec);
				line = navigationPath.PathPoints[i];
				if (num5 < num4)
				{
					num4 = num5;
				}
			}
			num = ((num3 > num4 && num4 < 25f) ? (1f * (num4 - num3) / 225f) : ((!(num3 > 4f)) ? 1f : (1f * num4 / 225f)));
		}
		float num6 = 1f * (225f / startWorldPos.AsVec2.DistanceSquared(targetWorldPos.AsVec2));
		return (1f + num2) * (1f + num2) - 2f + num + num6;
	}

	private void LookForPlace()
	{
		FleeGoalBase selectedGoal = new FleeCoverTarget(base.Navigator, base.OwnerAgent);
		FleeTargetType selectedFleeTargetType = FleeTargetType.Cover;
		if (IsThereDanger())
		{
			List<(float, Agent)> availableGuardScores = GetAvailableGuardScores();
			List<(float, Passage)> availablePassageScores = GetAvailablePassageScores();
			float num = float.MinValue;
			foreach (var item in availablePassageScores)
			{
				var (num2, _) = item;
				if (num2 > num)
				{
					num = num2;
					selectedFleeTargetType = FleeTargetType.Indoor;
					selectedGoal = new FleePassageTarget(base.Navigator, base.OwnerAgent, item.Item2);
				}
			}
			foreach (var item2 in availableGuardScores)
			{
				var (num3, _) = item2;
				if (num3 > num)
				{
					num = num3;
					selectedFleeTargetType = FleeTargetType.Guard;
					selectedGoal = new FleeAgentTarget(base.Navigator, base.OwnerAgent, item2.Item2);
				}
			}
		}
		_selectedGoal = selectedGoal;
		SelectedFleeTargetType = selectedFleeTargetType;
		_state = State.Flee;
	}

	private bool ShouldChangeTarget()
	{
		if (_selectedFleeTargetType == FleeTargetType.Guard)
		{
			WorldPosition worldPosition = (_selectedGoal as FleeAgentTarget).Savior.GetWorldPosition();
			WorldPosition worldPosition2 = base.OwnerAgent.GetWorldPosition();
			if (GetPathScore(worldPosition2, worldPosition) <= 1f)
			{
				return IsThereASafePlaceToEscape();
			}
			return false;
		}
		if (_selectedFleeTargetType == FleeTargetType.Indoor)
		{
			StandingPoint vacantStandingPointForAI = (_selectedGoal as FleePassageTarget).EscapePortal.GetVacantStandingPointForAI(base.OwnerAgent);
			if (vacantStandingPointForAI == null)
			{
				return true;
			}
			WorldPosition worldPosition3 = base.OwnerAgent.GetWorldPosition();
			WorldPosition origin = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin;
			if (GetPathScore(worldPosition3, origin) <= 1f)
			{
				return IsThereASafePlaceToEscape();
			}
			return false;
		}
		return true;
	}

	private bool IsThereASafePlaceToEscape()
	{
		if (!GetAvailablePassageScores(1).Any(((float, Passage) d) => d.Item1 > 1f))
		{
			return GetAvailableGuardScores(1).Any(((float, Agent) d) => d.Item1 > 1f);
		}
		return true;
	}

	private List<(float, Passage)> GetAvailablePassageScores(int maxPaths = 10)
	{
		WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
		List<(float, Passage)> list = new List<(float, Passage)>();
		List<(float, Passage)> list2 = new List<(float, Passage)>();
		List<(WorldPosition, Passage)> list3 = new List<(WorldPosition, Passage)>();
		if (_missionAgentHandler.TownPassageProps != null)
		{
			foreach (UsableMachine townPassageProp in _missionAgentHandler.TownPassageProps)
			{
				StandingPoint vacantStandingPointForAI = townPassageProp.GetVacantStandingPointForAI(base.OwnerAgent);
				Passage passage = townPassageProp as Passage;
				if (vacantStandingPointForAI != null && passage != null)
				{
					WorldPosition origin = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin;
					list3.Add((origin, passage));
				}
			}
		}
		list3 = list3.OrderBy(((WorldPosition, Passage) a) => base.OwnerAgent.Position.AsVec2.DistanceSquared(a.Item1.AsVec2)).ToList();
		foreach (var item2 in list3)
		{
			var (targetWorldPos, _) = item2;
			if (targetWorldPos.IsValid && !(targetWorldPos.GetNearestNavMesh() == UIntPtr.Zero))
			{
				float pathScore = GetPathScore(worldPosition, targetWorldPos);
				(float, Passage) item = (pathScore, item2.Item2);
				list.Add(item);
				if (pathScore > 1f)
				{
					list2.Add(item);
				}
				if (list2.Count >= maxPaths)
				{
					break;
				}
			}
		}
		if (list2.Count > 0)
		{
			return list2;
		}
		return list;
	}

	private List<(float, Agent)> GetAvailableGuardScores(int maxGuards = 5)
	{
		WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
		List<(float, Agent)> list = new List<(float, Agent)>();
		List<(float, Agent)> list2 = new List<(float, Agent)>();
		List<Agent> list3 = new List<Agent>();
		foreach (Agent activeAgent in base.OwnerAgent.Team.ActiveAgents)
		{
			if (activeAgent.Character is CharacterObject characterObject && activeAgent.IsAIControlled && activeAgent.CurrentWatchState != Agent.WatchState.Alarmed && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.PrisonGuard))
			{
				list3.Add(activeAgent);
			}
		}
		list3 = list3.OrderBy((Agent a) => base.OwnerAgent.Position.DistanceSquared(a.Position)).ToList();
		foreach (Agent item2 in list3)
		{
			WorldPosition worldPosition2 = item2.GetWorldPosition();
			if (worldPosition2.IsValid)
			{
				float pathScore = GetPathScore(worldPosition, worldPosition2);
				(float, Agent) item = (pathScore, item2);
				list.Add(item);
				if (pathScore > 1f)
				{
					list2.Add(item);
				}
				if (list2.Count >= maxGuards)
				{
					break;
				}
			}
		}
		if (list2.Count > 0)
		{
			return list2;
		}
		return list;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_state = State.None;
	}

	private void Flee()
	{
		if (_selectedGoal.IsGoalAchievable())
		{
			if (_selectedGoal.IsGoalAchieved())
			{
				_selectedGoal.TargetReached();
				switch (SelectedFleeTargetType)
				{
				case FleeTargetType.Cover:
					if (_reconsiderFleeTargetTimer.ElapsedTime > 0.5f)
					{
						_state = State.LookForPlace;
						_reconsiderFleeTargetTimer.Reset();
					}
					break;
				case FleeTargetType.Guard:
					_complainToGuardTimer = new BasicMissionTimer();
					_state = State.Complain;
					break;
				}
				return;
			}
			if (SelectedFleeTargetType == FleeTargetType.Guard)
			{
				_selectedGoal.GoToTarget();
			}
			if (_reconsiderFleeTargetTimer.ElapsedTime > 1f)
			{
				_reconsiderFleeTargetTimer.Reset();
				if (ShouldChangeTarget())
				{
					_state = State.LookForPlace;
				}
			}
		}
		else
		{
			_state = State.LookForPlace;
		}
	}

	private void BeAfraid()
	{
		_scareTimer = new BasicMissionTimer();
		_scareTime = 0.5f + MBRandom.RandomFloat * 0.5f;
		_state = State.Afraid;
	}

	public override string GetDebugInfo()
	{
		return "Flee " + _state;
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (base.Mission.CurrentTime < 3f)
		{
			return 0f;
		}
		if (!MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
		{
			return 0.9f;
		}
		return 0.1f;
	}
}
