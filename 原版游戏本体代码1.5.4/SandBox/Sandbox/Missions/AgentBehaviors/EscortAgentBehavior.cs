using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class EscortAgentBehavior : AgentBehavior
{
	public delegate bool OnTargetReachedDelegate(Agent agent, ref Agent escortedAgent, ref Agent targetAgent, ref UsableMachine targetMachine, ref Vec3? targetPosition);

	private enum State
	{
		NotEscorting,
		ReturnToEscortedAgent,
		Wait,
		Escorting
	}

	private const float StartWaitingDistanceSquared = 25f;

	private const float ReturnToEscortedAgentDistanceSquared = 100f;

	private const float EscortFinishedDistanceSquared = 16f;

	private const float TargetProximityThreshold = 3f;

	private const float MountedMoveProximityThreshold = 2.2f;

	private const float OnFootMoveProximityThreshold = 1.2f;

	private State _state;

	private Agent _escortedAgent;

	private Agent _targetAgent;

	private UsableMachine _targetMachine;

	private Vec3? _targetPosition;

	private bool _myLastStateWasRunning;

	private float _initialMaxSpeedLimit;

	private OnTargetReachedDelegate _onTargetReached;

	private bool _escortFinished;

	public Agent EscortedAgent => _escortedAgent;

	public Agent TargetAgent => _targetAgent;

	public EscortAgentBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_targetAgent = null;
		_escortedAgent = null;
		_myLastStateWasRunning = false;
		_initialMaxSpeedLimit = 1f;
	}

	public void Initialize(Agent escortedAgent, Agent targetAgent, OnTargetReachedDelegate onTargetReached = null)
	{
		_escortedAgent = escortedAgent;
		_targetAgent = targetAgent;
		_targetMachine = null;
		_targetPosition = null;
		_onTargetReached = onTargetReached;
		_escortFinished = false;
		_initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
		_state = State.Escorting;
	}

	public void Initialize(Agent escortedAgent, UsableMachine targetMachine, OnTargetReachedDelegate onTargetReached = null)
	{
		_escortedAgent = escortedAgent;
		_targetAgent = null;
		_targetMachine = targetMachine;
		_targetPosition = null;
		_onTargetReached = onTargetReached;
		_escortFinished = false;
		_initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
		_state = State.Escorting;
	}

	public void Initialize(Agent escortedAgent, Vec3? targetPosition, OnTargetReachedDelegate onTargetReached = null)
	{
		_escortedAgent = escortedAgent;
		_targetAgent = null;
		_targetMachine = null;
		_targetPosition = targetPosition;
		_onTargetReached = onTargetReached;
		_escortFinished = false;
		_initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
		_state = State.Escorting;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (_escortedAgent == null || !_escortedAgent.IsActive() || _targetAgent == null || !_targetAgent.IsActive())
		{
			_state = State.NotEscorting;
		}
		if (_escortedAgent != null && _state != State.NotEscorting)
		{
			ControlMovement();
		}
	}

	public bool IsEscortFinished()
	{
		return _escortFinished;
	}

	private void ControlMovement()
	{
		int nearbyEnemyAgentCount = base.Mission.GetNearbyEnemyAgentCount(_escortedAgent.Team, _escortedAgent.Position.AsVec2, 5f);
		if (_state != State.NotEscorting && nearbyEnemyAgentCount > 0)
		{
			_state = State.NotEscorting;
			base.OwnerAgent.ResetLookAgent();
			base.Navigator.ClearTarget();
			base.OwnerAgent.DisableScriptedMovement();
			base.OwnerAgent.SetMaximumSpeedLimit(_initialMaxSpeedLimit, isMultiplier: false);
			Debug.Print("[Escort agent behavior] Escorted agent got into a fight... Disable!");
			return;
		}
		float rangeThreshold = (base.OwnerAgent.HasMount ? 2.2f : 1.2f);
		float num = base.OwnerAgent.Position.DistanceSquared(_escortedAgent.Position);
		float num2;
		WorldPosition targetPosition;
		float targetRotation;
		if (_targetAgent != null)
		{
			num2 = base.OwnerAgent.Position.DistanceSquared(_targetAgent.Position);
			targetPosition = _targetAgent.GetWorldPosition();
			targetRotation = _targetAgent.Frame.rotation.f.AsVec2.RotationInRadians;
		}
		else if (_targetMachine != null)
		{
			MatrixFrame globalFrame = _targetMachine.GameEntity.GetGlobalFrame();
			num2 = base.OwnerAgent.Position.DistanceSquared(globalFrame.origin);
			targetPosition = globalFrame.origin.ToWorldPosition();
			targetRotation = globalFrame.rotation.f.AsVec2.RotationInRadians;
		}
		else if (_targetPosition.HasValue)
		{
			num2 = base.OwnerAgent.Position.DistanceSquared(_targetPosition.Value);
			targetPosition = _targetPosition.Value.ToWorldPosition();
			targetRotation = (_targetPosition.Value - base.OwnerAgent.Position).AsVec2.RotationInRadians;
		}
		else
		{
			Debug.FailedAssert("At least one target must be specified for the escort behavior.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\AgentBehaviors\\EscortAgentBehavior.cs", "ControlMovement", 160);
			num2 = 0f;
			targetPosition = base.OwnerAgent.GetWorldPosition();
			targetRotation = 0f;
		}
		if (_escortFinished)
		{
			bool flag = false;
			base.OwnerAgent.SetMaximumSpeedLimit(_initialMaxSpeedLimit, isMultiplier: false);
			if (_onTargetReached != null)
			{
				flag = _onTargetReached(base.OwnerAgent, ref _escortedAgent, ref _targetAgent, ref _targetMachine, ref _targetPosition);
			}
			if (flag && _escortedAgent != null && (_targetAgent != null || _targetMachine != null || _targetPosition.HasValue))
			{
				_state = State.Escorting;
			}
			else
			{
				_state = State.NotEscorting;
			}
		}
		switch (_state)
		{
		case State.Wait:
			if (num < 25f)
			{
				_state = State.Escorting;
				Debug.Print("[Escort agent behavior] Escorting!");
			}
			else if (num > 100f)
			{
				_state = State.ReturnToEscortedAgent;
				Debug.Print("[Escort agent behavior] Escorted agent is too far away! Return to escorted agent!");
			}
			else
			{
				SetMovePos(base.OwnerAgent.GetWorldPosition(), base.OwnerAgent.Frame.rotation.f.AsVec2.RotationInRadians, 0f);
			}
			break;
		case State.Escorting:
			if (num >= 25f)
			{
				_state = State.Wait;
				Debug.Print("[Escort agent behavior] Stop walking! Wait");
			}
			else
			{
				SetMovePos(targetPosition, targetRotation, 3f);
			}
			break;
		case State.ReturnToEscortedAgent:
			if (num < 25f)
			{
				_state = State.Wait;
			}
			else
			{
				SetMovePos(_escortedAgent.GetWorldPosition(), _escortedAgent.Frame.rotation.f.AsVec2.RotationInRadians, rangeThreshold);
			}
			break;
		}
		if (_state == State.Escorting && num2 < 16f && num < 16f && !Campaign.Current.ConversationManager.IsConversationInProgress)
		{
			_escortFinished = true;
		}
	}

	private void SetMovePos(WorldPosition targetPosition, float targetRotation, float rangeThreshold)
	{
		Agent.AIScriptedFrameFlags aIScriptedFrameFlags = Agent.AIScriptedFrameFlags.NoAttack;
		if (base.Navigator.CharacterHasVisiblePrefabs)
		{
			_myLastStateWasRunning = false;
		}
		else
		{
			float num = base.OwnerAgent.Position.AsVec2.Distance(targetPosition.AsVec2);
			float length = _escortedAgent.Velocity.AsVec2.Length;
			if (num - rangeThreshold <= 0.5f * (_myLastStateWasRunning ? 1f : 1.2f) && length <= base.OwnerAgent.Monster.WalkingSpeedLimit * (_myLastStateWasRunning ? 1f : 1.2f))
			{
				_myLastStateWasRunning = false;
			}
			else
			{
				base.OwnerAgent.SetMaximumSpeedLimit(num - rangeThreshold + length, isMultiplier: false);
				_myLastStateWasRunning = true;
			}
		}
		if (!_myLastStateWasRunning)
		{
			aIScriptedFrameFlags |= Agent.AIScriptedFrameFlags.DoNotRun;
		}
		base.Navigator.SetTargetFrame(targetPosition, targetRotation, rangeThreshold, -10f, aIScriptedFrameFlags);
	}

	public override float GetAvailability(bool isSimulation)
	{
		return (_state != State.NotEscorting) ? 1 : 0;
	}

	protected override void OnDeactivate()
	{
		_escortedAgent = null;
		_targetAgent = null;
		_targetMachine = null;
		_targetPosition = null;
		_onTargetReached = null;
		_state = State.NotEscorting;
		base.OwnerAgent.DisableScriptedMovement();
		base.OwnerAgent.ResetLookAgent();
		base.Navigator.ClearTarget();
	}

	public override string GetDebugInfo()
	{
		return "Escort " + _escortedAgent.Name + " (id:" + _escortedAgent.Index + ")" + ((_targetAgent != null) ? (" to " + _targetAgent.Name + " (id:" + _targetAgent.Index + ")") : ((_targetMachine != null) ? string.Concat(" to ", _targetMachine, "(id:", _targetMachine.Id, ")") : (_targetPosition.HasValue ? (" to position: " + _targetPosition.Value) : " to NO TARGET")));
	}

	public static void AddEscortAgentBehavior(Agent ownerAgent, Agent targetAgent, OnTargetReachedDelegate onTargetReached)
	{
		InterruptingBehaviorGroup interruptingBehaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
		if (interruptingBehaviorGroup != null)
		{
			bool num = interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() == null;
			EscortAgentBehavior escortAgentBehavior = interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() ?? interruptingBehaviorGroup.AddBehavior<EscortAgentBehavior>();
			if (num)
			{
				interruptingBehaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			}
			escortAgentBehavior.Initialize(Agent.Main, targetAgent, onTargetReached);
		}
	}

	public static void RemoveEscortBehaviorOfAgent(Agent ownerAgent)
	{
		InterruptingBehaviorGroup interruptingBehaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
		if (interruptingBehaviorGroup != null && interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() != null)
		{
			interruptingBehaviorGroup.RemoveBehavior<EscortAgentBehavior>();
		}
	}

	public static bool CheckIfAgentIsEscortedBy(Agent ownerAgent, Agent escortedAgent)
	{
		EscortAgentBehavior escortAgentBehavior = (ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>())?.GetBehavior<EscortAgentBehavior>();
		if (escortAgentBehavior != null)
		{
			return escortAgentBehavior.EscortedAgent == escortedAgent;
		}
		return false;
	}
}
