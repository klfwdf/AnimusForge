using System.Linq;
using SandBox.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions;

public class CoverAnimalAgentComponent : AgentComponent, IFocusable
{
	private enum NavigationState
	{
		WaitingToStart,
		NoTarget,
		GoToTarget,
		AtFinalPosition
	}

	private PatrolPoint[] _patrolPoints;

	private int _currentPatrolAreaIndex;

	private Timer _waitTimer;

	private NavigationState _agentState;

	private WorldPosition _targetPosition;

	private Vec2 _targetDirection;

	private bool _targetReached;

	private float _rangeThreshold;

	public bool IsMovementStarted => _agentState != NavigationState.WaitingToStart;

	public bool IsAtFinalPoint => _agentState == NavigationState.AtFinalPosition;

	public FocusableObjectType FocusableObjectType => FocusableObjectType.Item;

	public virtual bool IsFocusable => true;

	public CoverAnimalAgentComponent(Agent agent)
		: base(agent)
	{
		_agentState = NavigationState.WaitingToStart;
		Agent.SetMaximumSpeedLimit(1f, isMultiplier: false);
	}

	public void SetDynamicPatrolArea(GameEntity parentPatrolPoint)
	{
		_patrolPoints = new PatrolPoint[parentPatrolPoint.ChildCount];
		bool flag = false;
		PatrolPoint[] array = new PatrolPoint[parentPatrolPoint.ChildCount];
		for (int i = 0; i < parentPatrolPoint.ChildCount; i++)
		{
			array[i] = parentPatrolPoint.GetChild(i).GetChild(0).GetFirstScriptOfType<PatrolPoint>();
			if (!flag)
			{
				flag = array[i].IsInfiniteWaitPoint;
			}
		}
		_patrolPoints = array.OrderBy((PatrolPoint x) => x.Index).ToArray();
	}

	public void StartMovement()
	{
		if (!IsMovementStarted)
		{
			_agentState = NavigationState.NoTarget;
			Agent.SetMaximumSpeedLimit(1f, isMultiplier: false);
		}
	}

	public override void OnTick(float dt)
	{
		if (!Agent.Mission.AllowAiTicking || !Agent.IsAIControlled || _agentState == NavigationState.WaitingToStart)
		{
			return;
		}
		if (_waitTimer == null && IsTargetReached() && _agentState != NavigationState.NoTarget)
		{
			PatrolPoint patrolPoint = _patrolPoints[_currentPatrolAreaIndex];
			float duration = (float)patrolPoint.WaitDuration + MBRandom.RandomFloatRanged(-patrolPoint.WaitDeviation, patrolPoint.WaitDeviation);
			_waitTimer = new Timer(Mission.Current.CurrentTime, duration);
		}
		if (_agentState != NavigationState.AtFinalPosition)
		{
			if (!_targetPosition.IsValid)
			{
				MoveAnimalToNextPatrolPoint();
			}
			Timer waitTimer = _waitTimer;
			if (waitTimer != null && waitTimer.Check(Mission.Current.CurrentTime))
			{
				_waitTimer = null;
				Agent.ClearTargetFrame();
				_targetPosition = WorldPosition.Invalid;
				_agentState = NavigationState.NoTarget;
			}
		}
	}

	private void DebugTick()
	{
		int num = _currentPatrolAreaIndex;
		if (num == -1)
		{
			num = 0;
		}
		if (num + 1 >= _patrolPoints.Length)
		{
			num = -1;
		}
		for (int i = 0; i < _patrolPoints.Length; i++)
		{
		}
		_ = _waitTimer;
	}

	public bool IsTargetReached()
	{
		if (_targetDirection.IsValid && _targetPosition.IsValid)
		{
			_targetReached = (Agent.Position - _targetPosition.GetGroundVec3()).LengthSquared < _rangeThreshold * _rangeThreshold;
		}
		return _targetReached;
	}

	public void SetTargetFrame(WorldPosition position, float rotation, float rangeThreshold = 1f, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None)
	{
		if (_agentState != NavigationState.NoTarget)
		{
			Agent.ClearTargetFrame();
			_targetPosition = WorldPosition.Invalid;
			_agentState = NavigationState.NoTarget;
		}
		_targetPosition = position;
		_targetDirection = Vec2.FromRotation(rotation);
		_rangeThreshold = rangeThreshold;
		if (IsTargetReached())
		{
			_targetPosition = WorldPosition.Invalid;
			_agentState = NavigationState.NoTarget;
		}
		else
		{
			Agent.SetScriptedPosition(ref position, addHumanLikeDelay: false, flags);
			_agentState = NavigationState.GoToTarget;
		}
	}

	private void MoveAnimalToNextPatrolPoint()
	{
		_waitTimer = null;
		if (_patrolPoints[_currentPatrolAreaIndex].IsInfiniteWaitPoint)
		{
			_agentState = NavigationState.AtFinalPosition;
			return;
		}
		_currentPatrolAreaIndex++;
		if (_currentPatrolAreaIndex >= _patrolPoints.Length)
		{
			_currentPatrolAreaIndex = 0;
		}
		WeakGameEntity gameEntity = _patrolPoints[_currentPatrolAreaIndex].GameEntity;
		WorldPosition position = new WorldPosition(gameEntity.Scene, gameEntity.GlobalPosition);
		SetTargetFrame(position, gameEntity.GetFrame().rotation.f.RotationX, 1f, Agent.AIScriptedFrameFlags.DoNotRun);
		_agentState = NavigationState.GoToTarget;
	}

	public void OnFocusGain(Agent userAgent)
	{
	}

	public void OnFocusLose(Agent userAgent)
	{
	}

	public TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
	{
		return null;
	}

	public TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		TextObject textObject = GameTexts.FindText("str_key_action");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		textObject.SetTextVariable("ACTION", new TextObject("{=F7JGCr9s}Move"));
		return textObject;
	}
}
