using System.Linq;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class PatrolAgentBehavior : AgentBehavior
{
	private const float DefaultPatrollingSpeed = 1.05f;

	private PatrolPoint[] _patrolPoints;

	private int _currentPatrolIndex;

	private Timer _waitTimer;

	private bool _infiniteWaitPointReached;

	private int NextPatrolIndex
	{
		get
		{
			int num = _currentPatrolIndex + 1;
			if (num >= _patrolPoints.Length)
			{
				num = 0;
			}
			return num;
		}
	}

	public PatrolAgentBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
	}

	public void SetDynamicPatrolArea(GameEntity parentPatrolPoint)
	{
		_patrolPoints = new PatrolPoint[parentPatrolPoint.ChildCount];
		PatrolPoint[] array = new PatrolPoint[parentPatrolPoint.ChildCount];
		for (int i = 0; i < parentPatrolPoint.ChildCount; i++)
		{
			array[i] = parentPatrolPoint.GetChild(i).GetChild(0).GetFirstScriptOfType<PatrolPoint>();
		}
		_patrolPoints = array.OrderBy((PatrolPoint x) => x.Index).ToArray();
	}

	protected override void OnActivate()
	{
		base.OwnerAgent.SetMaximumSpeedLimit(1.05f, isMultiplier: false);
		_infiniteWaitPointReached = false;
		PatrolPoint item = null;
		float num = float.MaxValue;
		PatrolPoint[] patrolPoints = _patrolPoints;
		foreach (PatrolPoint patrolPoint in patrolPoints)
		{
			float num2 = patrolPoint.GameEntity.GlobalPosition.DistanceSquared(base.OwnerAgent.Position);
			if (num2 < num)
			{
				num = num2;
				item = patrolPoint;
			}
		}
		_currentPatrolIndex = _patrolPoints.IndexOf(item);
		MoveAgentToThePoint(_currentPatrolIndex, correctRotation: true, isSimulation: false);
	}

	protected override void OnDeactivate()
	{
		_waitTimer = null;
		if (base.OwnerAgent.CurrentlyUsedGameObject != null)
		{
			base.OwnerAgent.StopUsingGameObjectMT();
		}
		base.Navigator.SetTarget(null);
		if (_patrolPoints[_currentPatrolIndex].GameEntity.GetFirstScriptOfType<PatrolPoint>().PatrollingSpeed != -1f || base.OwnerAgent.GetMaximumSpeedLimit().Equals(1.05f))
		{
			base.OwnerAgent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (!_infiniteWaitPointReached && base.OwnerAgent.CurrentlyUsedGameObject != null)
		{
			if (_waitTimer == null)
			{
				if (!(base.OwnerAgent.CurrentlyUsedGameObject is PatrolPoint patrolPoint))
				{
					return;
				}
				if (patrolPoint.IsInfiniteWaitPoint)
				{
					_infiniteWaitPointReached = true;
					return;
				}
				float num = (float)patrolPoint.WaitDuration + MBRandom.RandomFloatRanged(-patrolPoint.WaitDeviation, patrolPoint.WaitDeviation);
				if (num == 0f)
				{
					MoveAgentToNextPatrolPoint(isSimulation);
				}
				else
				{
					_waitTimer = new Timer(base.Mission.CurrentTime, num);
				}
			}
			else if (_waitTimer.Check(base.Mission.CurrentTime))
			{
				MoveAgentToNextPatrolPoint(isSimulation);
			}
		}
		else
		{
			if (base.Navigator.IsTargetReached())
			{
				base.Navigator.ClearTarget();
			}
			if (base.Navigator.TargetUsableMachine == null && !base.Navigator.TargetPosition.IsValid)
			{
				MoveAgentToNextPatrolPoint(isSimulation);
			}
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (!base.OwnerAgent.IsAlarmed() && !base.OwnerAgent.IsPatrollingCautious())
		{
			return 0.5f;
		}
		return 0f;
	}

	private void MoveAgentToNextPatrolPoint(bool isSimulation)
	{
		_waitTimer = null;
		PatrolPoint firstScriptOfType = _patrolPoints[_currentPatrolIndex].GameEntity.GetFirstScriptOfType<PatrolPoint>();
		base.OwnerAgent.SetMaximumSpeedLimit((firstScriptOfType.PatrollingSpeed == -1f) ? 1.05f : firstScriptOfType.PatrollingSpeed, isMultiplier: false);
		MoveAgentToThePoint(NextPatrolIndex, correctRotation: false, isSimulation);
		_currentPatrolIndex = NextPatrolIndex;
	}

	private void MoveAgentToThePoint(int pointIndex, bool correctRotation, bool isSimulation)
	{
		WeakGameEntity gameEntity = _patrolPoints[pointIndex].GameEntity;
		PatrolPoint firstScriptOfType = gameEntity.GetFirstScriptOfType<PatrolPoint>();
		if (firstScriptOfType.WaitDuration == 0 && firstScriptOfType.WaitDeviation == 0)
		{
			WorldPosition position = new WorldPosition(gameEntity.Scene, gameEntity.GlobalPosition);
			base.Navigator.SetTargetFrame(position, gameEntity.GetFrame().rotation.f.RotationX, correctRotation ? 1f : (-1f), correctRotation ? 0.8f : (-10f));
		}
		else
		{
			base.Navigator.SetTarget(gameEntity.Parent.GetFirstScriptOfType<UsablePlace>(), isSimulation, Agent.AIScriptedFrameFlags.NeverSlowDown | Agent.AIScriptedFrameFlags.DoNotRun);
		}
	}

	public override string GetDebugInfo()
	{
		return "Patrol Agent Behavior";
	}
}
