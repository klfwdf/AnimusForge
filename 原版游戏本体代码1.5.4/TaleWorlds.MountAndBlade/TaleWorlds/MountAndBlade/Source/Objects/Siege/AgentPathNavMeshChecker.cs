using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Objects.Siege;

public class AgentPathNavMeshChecker
{
	public enum Direction
	{
		ForwardOnly,
		BackwardOnly,
		BothDirections
	}

	private BattleSideEnum _teamToCollect;

	private Direction _directionToCollect;

	private MatrixFrame _pathFrameToCheck;

	private float _radiusToCheck;

	private Mission _mission;

	private int _navMeshId;

	private Timer _tickOccasionallyTimer;

	private MBList<Agent> _nearbyAgents = new MBList<Agent>();

	private bool _isBeingUsed;

	private Timer _setBeingUsedToFalseTimer;

	private float _maxDistanceCheck;

	private float _agentMoveTime;

	public AgentPathNavMeshChecker(Mission mission, MatrixFrame pathFrameToCheck, float radiusToCheck, int navMeshId, BattleSideEnum teamToCollect, Direction directionToCollect, float maxDistanceCheck, float agentMoveTime)
	{
		_mission = mission;
		_pathFrameToCheck = pathFrameToCheck;
		_radiusToCheck = radiusToCheck;
		_navMeshId = navMeshId;
		_teamToCollect = teamToCollect;
		_directionToCollect = directionToCollect;
		_maxDistanceCheck = maxDistanceCheck;
		_agentMoveTime = agentMoveTime;
	}

	public void Tick(float dt)
	{
		float currentTime = _mission.CurrentTime;
		if (_tickOccasionallyTimer == null || _tickOccasionallyTimer.Check(currentTime))
		{
			float dt2 = dt;
			if (_tickOccasionallyTimer != null)
			{
				dt2 = _tickOccasionallyTimer.ElapsedTime();
			}
			_tickOccasionallyTimer = new Timer(currentTime, 0.1f + MBRandom.RandomFloat * 0.1f);
			TickOccasionally(dt2);
		}
		bool flag = false;
		foreach (Agent nearbyAgent in _nearbyAgents)
		{
			Vec3 position = nearbyAgent.Position;
			if ((_teamToCollect != BattleSideEnum.None && (nearbyAgent.Team == null || nearbyAgent.Team.Side != _teamToCollect)) || !nearbyAgent.IsAIControlled)
			{
				continue;
			}
			if (nearbyAgent.GetCurrentNavigationFaceId() == _navMeshId)
			{
				flag = true;
				break;
			}
			if (_isBeingUsed && position.DistanceSquared(_pathFrameToCheck.origin) < _radiusToCheck * _radiusToCheck)
			{
				flag = true;
				break;
			}
			if (nearbyAgent.MovementVelocity.LengthSquared > 0.01f && nearbyAgent.HasPathThroughNavigationFaceIdFromDirection(direction: (_directionToCollect == Direction.ForwardOnly) ? _pathFrameToCheck.rotation.f.AsVec2 : ((_directionToCollect != Direction.BackwardOnly) ? Vec2.Zero : (-_pathFrameToCheck.rotation.f.AsVec2)), navigationFaceId: _navMeshId))
			{
				float num = nearbyAgent.GetPathDistanceToPoint(ref _pathFrameToCheck.origin);
				if (num >= 100000f)
				{
					num = nearbyAgent.Position.Distance(_pathFrameToCheck.origin);
				}
				if (num < _radiusToCheck * 2f || num / nearbyAgent.GetMaximumForwardUnlimitedSpeed() < _agentMoveTime)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			_isBeingUsed = true;
			_setBeingUsedToFalseTimer = null;
		}
		else if (_setBeingUsedToFalseTimer == null)
		{
			_setBeingUsedToFalseTimer = new Timer(currentTime, 1f);
		}
		if (_setBeingUsedToFalseTimer != null && _setBeingUsedToFalseTimer.Check(currentTime))
		{
			_setBeingUsedToFalseTimer = null;
			_isBeingUsed = false;
		}
	}

	public void TickOccasionally(float dt)
	{
		_nearbyAgents = _mission.GetNearbyAgents(_pathFrameToCheck.origin.AsVec2, _maxDistanceCheck, _nearbyAgents);
	}

	public bool HasAgentsUsingPath()
	{
		return _isBeingUsed;
	}
}
