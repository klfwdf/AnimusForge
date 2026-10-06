using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class WalkingBehavior : AgentBehavior
{
	private readonly MissionAgentHandler _missionAgentHandler;

	private readonly bool _isIndoor;

	private UsableMachine _wanderTarget;

	private UsableMachine _lastTarget;

	private Timer _waitTimer;

	private bool _indoorWanderingIsActive;

	private bool _outdoorWanderingIsActive;

	private bool _wasSimulation;

	private bool _isWaitingNearOccupiedTarget;

	private bool CanWander
	{
		get
		{
			if (!_isIndoor || !_indoorWanderingIsActive)
			{
				if (!_isIndoor)
				{
					return _outdoorWanderingIsActive;
				}
				return false;
			}
			return true;
		}
	}

	public WalkingBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		_wanderTarget = null;
		_isIndoor = CampaignMission.Current.Location.IsIndoor;
		_indoorWanderingIsActive = true;
		_outdoorWanderingIsActive = true;
		_wasSimulation = false;
	}

	public void SetIndoorWandering(bool isActive)
	{
		_indoorWanderingIsActive = isActive;
	}

	public void SetOutdoorWandering(bool isActive)
	{
		_outdoorWanderingIsActive = isActive;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (Mission.Current.CurrentState == Mission.State.EndingNextFrame)
		{
			return;
		}
		if (_wanderTarget == null || base.Navigator.TargetUsableMachine == null || _wanderTarget.IsDisabled || !_wanderTarget.IsStandingPointAvailableForAgent(base.OwnerAgent))
		{
			_wanderTarget = FindTarget();
			_lastTarget = _wanderTarget;
			if (_wanderTarget == null)
			{
				string specialTargetTag = base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag;
				if (specialTargetTag != null && _missionAgentHandler.HasUsablePointWithTag(specialTargetTag))
				{
					if (!_isWaitingNearOccupiedTarget)
					{
						List<UsableMachine> allUsablePointsWithTag = _missionAgentHandler.GetAllUsablePointsWithTag(specialTargetTag);
						if (allUsablePointsWithTag != null && allUsablePointsWithTag.Count > 0)
						{
							UsableMachine usableMachine = allUsablePointsWithTag[0];
							Vec3 globalPosition = usableMachine.GameEntity.GlobalPosition;
							Vec3 vec = base.OwnerAgent.Position - globalPosition;
							vec.z = 0f;
							if (vec.Length < 0.01f)
							{
								vec = new Vec3(1f);
							}
							vec.Normalize();
							Vec3 position = globalPosition + vec * 2f;
							WorldPosition position2 = new WorldPosition(usableMachine.GameEntity.Scene, position);
							base.Navigator.SetTargetFrame(position2, 0f, 1f, -10f, Agent.AIScriptedFrameFlags.DoNotRun);
							_isWaitingNearOccupiedTarget = true;
						}
					}
					else if (_waitTimer == null)
					{
						_waitTimer = new Timer(base.Mission.CurrentTime, 3f);
					}
					else if (_waitTimer.Check(base.Mission.CurrentTime))
					{
						_waitTimer = null;
						_isWaitingNearOccupiedTarget = false;
					}
					return;
				}
			}
			else
			{
				_isWaitingNearOccupiedTarget = false;
			}
		}
		else if (base.Navigator.GetDistanceToTarget(_wanderTarget) < 5f)
		{
			bool flag = _wasSimulation && !isSimulation && _wanderTarget != null && _waitTimer != null && MBRandom.RandomFloat < (_isIndoor ? 0f : (Settlement.CurrentSettlement.IsVillage ? 0.6f : 0.1f));
			if (_waitTimer == null)
			{
				if (!_wanderTarget.GameEntity.HasTag("npc_idle"))
				{
					SetTimerForTheAgent(isSimulation);
				}
			}
			else if (_waitTimer.Check(base.Mission.CurrentTime) || flag)
			{
				if (CanWander)
				{
					_waitTimer = null;
					UsableMachine usableMachine2 = FindTarget();
					if (usableMachine2 == null || IsChildrenOfSameParent(usableMachine2, _wanderTarget))
					{
						SetTimerForTheAgent(isSimulation);
					}
					else
					{
						_lastTarget = _wanderTarget;
						_wanderTarget = usableMachine2;
					}
				}
				else
				{
					_waitTimer.Reset(100f);
				}
			}
		}
		else if (_wanderTarget != null)
		{
			foreach (StandingPoint standingPoint in _wanderTarget.StandingPoints)
			{
				if (standingPoint.HasUser)
				{
					_wanderTarget = null;
					break;
				}
			}
		}
		if (base.OwnerAgent.CurrentlyUsedGameObject != null && base.Navigator.GetDistanceToTarget(_lastTarget) > 1f)
		{
			base.Navigator.SetTarget(_lastTarget, _lastTarget == _wanderTarget);
		}
		base.Navigator.SetTarget(_wanderTarget);
		_wasSimulation = isSimulation;
	}

	private void SetTimerForTheAgent(bool isSimulation)
	{
		float num = ((base.OwnerAgent.CurrentlyUsedGameObject is AnimationPoint animationPoint) ? animationPoint.GetRandomWaitInSeconds() : 10f);
		if (isSimulation && MBRandom.RandomFloat < 0.33f)
		{
			num /= 10f + MBRandom.RandomFloat * 10f;
		}
		_waitTimer = new Timer(base.Mission.CurrentTime, (num < 0f) ? 2.1474836E+09f : num);
	}

	private bool IsChildrenOfSameParent(UsableMachine machine, UsableMachine otherMachine)
	{
		WeakGameEntity weakGameEntity = machine.GameEntity;
		while (weakGameEntity.Parent.IsValid)
		{
			weakGameEntity = weakGameEntity.Parent;
		}
		WeakGameEntity weakGameEntity2 = otherMachine.GameEntity;
		while (weakGameEntity2.Parent.IsValid)
		{
			weakGameEntity2 = weakGameEntity2.Parent;
		}
		return weakGameEntity == weakGameEntity2;
	}

	public override void ConversationTick()
	{
		if (_waitTimer != null)
		{
			_waitTimer.Reset(base.Mission.CurrentTime);
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (_isWaitingNearOccupiedTarget)
		{
			return 1f;
		}
		if (FindTarget() == null)
		{
			return 0f;
		}
		return 1f;
	}

	public override void SetCustomWanderTarget(UsableMachine customUsableMachine)
	{
		_wanderTarget = customUsableMachine;
		if (_waitTimer != null)
		{
			_waitTimer = null;
		}
	}

	private UsableMachine FindRandomWalkingTarget(bool forWaiting)
	{
		if (forWaiting && (_wanderTarget ?? base.Navigator.TargetUsableMachine) != null)
		{
			return null;
		}
		string text = base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag;
		if (text == null)
		{
			text = "npc_common";
		}
		else if (!_missionAgentHandler.HasUsablePointWithTag(text))
		{
			text = "npc_common_limited";
		}
		return _missionAgentHandler.FindUnusedPointWithTagForAgent(base.OwnerAgent, text);
	}

	private UsableMachine FindTarget()
	{
		return FindRandomWalkingTarget(_isIndoor && !_indoorWanderingIsActive);
	}

	private float GetTargetScore(UsableMachine usableMachine)
	{
		if (base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag != null && !usableMachine.GameEntity.HasTag(base.OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag))
		{
			return 0f;
		}
		StandingPoint vacantStandingPointForAI = usableMachine.GetVacantStandingPointForAI(base.OwnerAgent);
		if (vacantStandingPointForAI == null || vacantStandingPointForAI.IsDisabledForAgent(base.OwnerAgent))
		{
			return 0f;
		}
		float num = 1f;
		Vec3 vec = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin.GetGroundVec3() - base.OwnerAgent.Position;
		if (vec.Length < 2f)
		{
			num *= vec.Length / 2f;
		}
		return num * (0.8f + MBRandom.RandomFloat * 0.2f);
	}

	public override void OnSpecialTargetChanged()
	{
		if (_wanderTarget != null)
		{
			if (!base.Navigator.SpecialTargetTag.IsEmpty() && !_wanderTarget.GameEntity.HasTag(base.Navigator.SpecialTargetTag))
			{
				_wanderTarget = null;
				base.Navigator.SetTarget(_wanderTarget);
			}
			else if (base.Navigator.SpecialTargetTag.IsEmpty() && !_wanderTarget.GameEntity.HasTag("npc_common"))
			{
				_wanderTarget = null;
				base.Navigator.SetTarget(_wanderTarget);
			}
		}
	}

	public override string GetDebugInfo()
	{
		string text = "Walk ";
		if (_waitTimer != null)
		{
			text = text + "(Wait " + (int)_waitTimer.ElapsedTime() + "/" + _waitTimer.Duration + ")";
		}
		else if (_wanderTarget == null)
		{
			text += "(search for target!)";
		}
		return text;
	}

	protected override void OnDeactivate()
	{
		base.Navigator.ClearTarget();
		_wanderTarget = null;
		_waitTimer = null;
		_isWaitingNearOccupiedTarget = false;
	}
}
