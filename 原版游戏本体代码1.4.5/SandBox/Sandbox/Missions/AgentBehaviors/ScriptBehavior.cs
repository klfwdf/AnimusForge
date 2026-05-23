using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class ScriptBehavior : AgentBehavior
{
	public delegate bool SelectTargetDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold);

	public delegate bool OnTargetReachedDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame);

	public delegate void OnTargetReachedWaitDelegate(Agent agent, ref float waitTimeInSeconds);

	private enum State
	{
		NoTarget,
		GoToUsableMachine,
		GoToAgent,
		GoToTargetFrame,
		NearAgent,
		NearStationaryTarget
	}

	private UsableMachine _targetUsableMachine;

	private Agent _targetAgent;

	private WorldFrame _targetFrame;

	private State _state;

	private bool _sentToTarget;

	private float _waitTimeInSeconds;

	private bool _isWaiting;

	private MissionTimer _waitTimer;

	private float _customTargetReachedRangeThreshold = 1f;

	private float _customTargetReachedRotationThreshold = 1f;

	private float _initialWaitInSeconds;

	private bool _isInitiallyWaiting;

	private SelectTargetDelegate _selectTargetDelegate;

	private OnTargetReachedDelegate _onTargetReachedDelegate;

	private OnTargetReachedWaitDelegate _onTargetReachWaitDelegate;

	public ScriptBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
	}

	public static void AddUsableMachineTarget(Agent ownerAgent, UsableMachine targetUsableMachine)
	{
		DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
		bool num = behaviorGroup.ScriptedBehavior != scriptBehavior;
		scriptBehavior._targetUsableMachine = targetUsableMachine;
		scriptBehavior._state = State.GoToUsableMachine;
		scriptBehavior._sentToTarget = false;
		if (num)
		{
			behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
		}
	}

	public static void AddAgentTarget(Agent ownerAgent, Agent targetAgent)
	{
		DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
		bool num = behaviorGroup.ScriptedBehavior != scriptBehavior;
		scriptBehavior._targetAgent = targetAgent;
		scriptBehavior._state = State.GoToAgent;
		scriptBehavior._sentToTarget = false;
		if (num)
		{
			behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
		}
	}

	public static void AddWorldFrameTarget(Agent ownerAgent, WorldFrame targetWorldFrame)
	{
		DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
		bool num = behaviorGroup.ScriptedBehavior != scriptBehavior;
		scriptBehavior._targetFrame = targetWorldFrame;
		scriptBehavior._state = State.GoToTargetFrame;
		scriptBehavior._sentToTarget = false;
		if (num)
		{
			behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
		}
	}

	public static void AddTargetWithDelegate(Agent ownerAgent, SelectTargetDelegate selectTargetDelegate, OnTargetReachedWaitDelegate onTargetReachWaitDelegate, OnTargetReachedDelegate onTargetReachedDelegate, float initialWaitInSeconds = 0f)
	{
		DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
		bool num = behaviorGroup.ScriptedBehavior != scriptBehavior;
		scriptBehavior._selectTargetDelegate = selectTargetDelegate;
		scriptBehavior._onTargetReachedDelegate = onTargetReachedDelegate;
		scriptBehavior._onTargetReachWaitDelegate = onTargetReachWaitDelegate;
		scriptBehavior._initialWaitInSeconds = initialWaitInSeconds;
		scriptBehavior._isInitiallyWaiting = initialWaitInSeconds > 0f;
		scriptBehavior._state = State.NoTarget;
		scriptBehavior._sentToTarget = false;
		if (num)
		{
			behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
		}
	}

	public bool IsNearTarget(Agent targetAgent)
	{
		if (_targetAgent == targetAgent)
		{
			if (_state != State.NearAgent)
			{
				return _state == State.NearStationaryTarget;
			}
			return true;
		}
		return false;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (_isInitiallyWaiting)
		{
			if (_waitTimer == null)
			{
				_waitTimer = new MissionTimer(_initialWaitInSeconds);
			}
			else if (_waitTimer.Check())
			{
				_isInitiallyWaiting = false;
				_waitTimer = null;
			}
			return;
		}
		if (_state == State.NoTarget)
		{
			if (_selectTargetDelegate == null)
			{
				if (BehaviorGroup.ScriptedBehavior == this)
				{
					BehaviorGroup.DisableScriptedBehavior();
				}
				return;
			}
			SearchForNewTarget();
		}
		switch (_state)
		{
		case State.GoToUsableMachine:
			if (!_sentToTarget)
			{
				base.Navigator.SetTarget(_targetUsableMachine);
				_sentToTarget = true;
			}
			else if (base.OwnerAgent.IsUsingGameObject && base.OwnerAgent.Position.DistanceSquared(_targetUsableMachine.GameEntity.GetGlobalFrame().origin) < 1f)
			{
				if (CheckForSearchNewTarget(State.NearStationaryTarget))
				{
					base.OwnerAgent.StopUsingGameObject(isSuccessful: false);
				}
				else
				{
					RemoveTargets();
				}
			}
			break;
		case State.GoToAgent:
			if (_targetAgent.IsActive())
			{
				float interactionDistanceToUsable = base.OwnerAgent.GetInteractionDistanceToUsable(_targetAgent);
				if (base.OwnerAgent.Position.DistanceSquared(_targetAgent.Position) < interactionDistanceToUsable * interactionDistanceToUsable)
				{
					if (!CheckForSearchNewTarget(State.NearAgent))
					{
						base.Navigator.SetTargetFrame(base.OwnerAgent.GetWorldPosition(), base.OwnerAgent.Frame.rotation.f.AsVec2.RotationInRadians, _customTargetReachedRangeThreshold, _customTargetReachedRotationThreshold);
						RemoveTargets();
					}
				}
				else
				{
					base.Navigator.SetTargetFrame(_targetAgent.GetWorldPosition(), _targetAgent.Frame.rotation.f.AsVec2.RotationInRadians, _customTargetReachedRangeThreshold, _customTargetReachedRotationThreshold);
				}
			}
			else if (!CheckForSearchNewTarget(State.NearAgent))
			{
				base.Navigator.SetTargetFrame(base.OwnerAgent.GetWorldPosition(), base.OwnerAgent.Frame.rotation.f.AsVec2.RotationInRadians, _customTargetReachedRangeThreshold, _customTargetReachedRotationThreshold);
				RemoveTargets();
			}
			break;
		case State.GoToTargetFrame:
			if (!_sentToTarget)
			{
				base.Navigator.SetTargetFrame(_targetFrame.Origin, _targetFrame.Rotation.f.AsVec2.RotationInRadians, _customTargetReachedRangeThreshold, _customTargetReachedRotationThreshold, Agent.AIScriptedFrameFlags.DoNotRun);
				_sentToTarget = true;
			}
			else if (base.Navigator.IsTargetReached() && !CheckForSearchNewTarget(State.NearStationaryTarget) && _waitTimer == null)
			{
				RemoveTargets();
			}
			break;
		case State.NearAgent:
			if (base.OwnerAgent.Position.DistanceSquared(_targetAgent.Position) >= 1f)
			{
				_state = State.GoToAgent;
				break;
			}
			base.Navigator.SetTargetFrame(base.OwnerAgent.GetWorldPosition(), base.OwnerAgent.Frame.rotation.f.AsVec2.RotationInRadians, _customTargetReachedRangeThreshold, _customTargetReachedRotationThreshold);
			RemoveTargets();
			break;
		}
	}

	private bool CheckForSearchNewTarget(State endState)
	{
		bool flag = false;
		bool flag2 = false;
		if (_onTargetReachWaitDelegate != null && !_isWaiting)
		{
			_onTargetReachWaitDelegate(base.OwnerAgent, ref _waitTimeInSeconds);
			_isWaiting = _waitTimeInSeconds > 0f;
		}
		if (_isWaiting)
		{
			if (_waitTimer == null)
			{
				_waitTimer = new MissionTimer(_waitTimeInSeconds);
			}
			else if (_waitTimer.Check())
			{
				_isWaiting = false;
				_waitTimer = null;
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			if (_onTargetReachedDelegate != null)
			{
				flag2 = _onTargetReachedDelegate(base.OwnerAgent, ref _targetAgent, ref _targetUsableMachine, ref _targetFrame);
			}
			if (flag2)
			{
				SearchForNewTarget();
			}
			else
			{
				_state = endState;
			}
			return flag2;
		}
		return false;
	}

	private void SearchForNewTarget()
	{
		Agent targetAgent = null;
		UsableMachine targetUsableMachine = null;
		WorldFrame targetFrame = WorldFrame.Invalid;
		float customTargetReachedRangeThreshold = _customTargetReachedRangeThreshold;
		float customTargetReachedRotationThreshold = _customTargetReachedRotationThreshold;
		if (_selectTargetDelegate(base.OwnerAgent, ref targetAgent, ref targetUsableMachine, ref targetFrame, ref customTargetReachedRangeThreshold, ref customTargetReachedRotationThreshold))
		{
			if (targetAgent != null)
			{
				_targetAgent = targetAgent;
				_state = State.GoToAgent;
				_sentToTarget = false;
			}
			else if (targetUsableMachine != null)
			{
				_targetUsableMachine = targetUsableMachine;
				_state = State.GoToUsableMachine;
				_sentToTarget = false;
			}
			else
			{
				_targetFrame = targetFrame;
				_state = State.GoToTargetFrame;
				_sentToTarget = false;
			}
			_customTargetReachedRangeThreshold = customTargetReachedRangeThreshold;
			_customTargetReachedRotationThreshold = customTargetReachedRotationThreshold;
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		return (_state != State.NoTarget) ? 1 : 0;
	}

	protected override void OnDeactivate()
	{
		base.Navigator.ClearTarget();
		RemoveTargets();
	}

	private void RemoveTargets()
	{
		_targetUsableMachine = null;
		_targetAgent = null;
		_targetFrame = WorldFrame.Invalid;
		_state = State.NoTarget;
		_selectTargetDelegate = null;
		_onTargetReachedDelegate = null;
		_sentToTarget = false;
	}

	public override string GetDebugInfo()
	{
		return "Scripted";
	}
}
