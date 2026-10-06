using System;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;

namespace NavalDLC.Missions.AI.Behaviors;

internal class NavalBehaviorBoardShipSubtask
{
	public enum ShipBoardingState
	{
		ApproachFromFarAway,
		GettingClose,
		AdjustingOrientation,
		InPosition,
		Connected,
		InactiveStuck
	}

	private const float MinimumBoardingDistance = 3f;

	private const float IdealBoardingDistance = 12f;

	private const float MaximumBoardingDistance = 30f;

	private const float DriftedAwayDistance = 50f;

	private MissionShip _selfShip;

	private MissionShip _givenTargetToBoard;

	private MissionShip _effectiveTarget;

	private bool _givenSideToBoardIsRight;

	private bool _effectiveSideToBoardIsRight;

	private float _cachedEffectiveDistance = float.MaxValue;

	public ShipBoardingState State { get; private set; }

	public NavalBehaviorBoardShipSubtask(MissionShip selfShip)
	{
		_selfShip = selfShip;
	}

	public void OnBehaviorActivatedAux()
	{
		State = ShipBoardingState.ApproachFromFarAway;
	}

	public void SetOwnerShip(MissionShip selfShip)
	{
		_selfShip = selfShip;
		SetTargetShipAndSide(_givenTargetToBoard, _givenSideToBoardIsRight);
	}

	public void SetTargetShipAndSide(MissionShip targetShip, bool rightSide)
	{
		if (_givenTargetToBoard != targetShip || _effectiveTarget != targetShip || _givenSideToBoardIsRight != rightSide || _effectiveSideToBoardIsRight != rightSide || State == ShipBoardingState.InactiveStuck)
		{
			_givenTargetToBoard = targetShip;
			_effectiveTarget = targetShip;
			_givenSideToBoardIsRight = rightSide;
			_effectiveSideToBoardIsRight = rightSide;
			State = ShipBoardingState.ApproachFromFarAway;
		}
	}

	public MissionShip GetCurrentGivenTarget()
	{
		return _givenTargetToBoard;
	}

	public MissionShip GetCurrentEffectiveTargetShip()
	{
		return _effectiveTarget;
	}

	public float GetEffectiveDistanceToObjective()
	{
		if (State == ShipBoardingState.Connected)
		{
			return 0f;
		}
		if (State == ShipBoardingState.InactiveStuck)
		{
			return float.MaxValue;
		}
		return _cachedEffectiveDistance;
	}

	private void CheckAndSwitchState()
	{
		if (_givenTargetToBoard == null || _effectiveTarget == null)
		{
			return;
		}
		if (State != ShipBoardingState.Connected && State != ShipBoardingState.InactiveStuck && _selfShip.GetIsConnected())
		{
			if (_selfShip.SearchShipConnection(_givenTargetToBoard, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))
			{
				State = ShipBoardingState.Connected;
			}
			else
			{
				State = ShipBoardingState.InactiveStuck;
			}
			return;
		}
		MatrixFrame globalFrame = _effectiveTarget.GlobalFrame;
		MatrixFrame globalFrame2 = _selfShip.GlobalFrame;
		Vec2 vec = (_effectiveSideToBoardIsRight ? globalFrame.rotation.f.AsVec2.LeftVec().Normalized() : globalFrame.rotation.f.AsVec2.RightVec().Normalized());
		if (State == ShipBoardingState.ApproachFromFarAway && ((globalFrame.origin.AsVec2 - globalFrame2.origin.AsVec2).LengthSquared < 900f || (globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared < 2500f))
		{
			State = ShipBoardingState.GettingClose;
		}
		if (State == ShipBoardingState.GettingClose && ((globalFrame.origin.AsVec2 - globalFrame2.origin.AsVec2).LengthSquared < 900f || (globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared < 900f))
		{
			State = ShipBoardingState.AdjustingOrientation;
		}
		if (State == ShipBoardingState.AdjustingOrientation)
		{
			if ((globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared > 2500f)
			{
				State = ShipBoardingState.GettingClose;
			}
			else if (Math.Abs(globalFrame2.rotation.f.AsVec2.Normalized().DotProduct(globalFrame.rotation.f.AsVec2.Normalized())) > 0.8f)
			{
				State = ShipBoardingState.InPosition;
			}
		}
		if (State == ShipBoardingState.InPosition)
		{
			if (_selfShip.GetIsConnected())
			{
				State = ShipBoardingState.Connected;
			}
			else if (Math.Abs(globalFrame2.rotation.f.AsVec2.Normalized().DotProduct(globalFrame.rotation.f.AsVec2.Normalized())) < 0.6f)
			{
				State = ShipBoardingState.AdjustingOrientation;
			}
			else if ((globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared > 2500f)
			{
				State = ShipBoardingState.GettingClose;
			}
		}
		if (State == ShipBoardingState.Connected)
		{
			if (!_selfShip.GetIsConnected())
			{
				State = ShipBoardingState.GettingClose;
			}
			else if (!_selfShip.SearchShipConnection(_givenTargetToBoard, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))
			{
				State = ShipBoardingState.InactiveStuck;
			}
		}
		if (State == ShipBoardingState.InactiveStuck && !_selfShip.GetIsConnected())
		{
			State = ShipBoardingState.ApproachFromFarAway;
		}
	}

	private bool IsEffectivelyRightSide()
	{
		if (State == ShipBoardingState.ApproachFromFarAway)
		{
			return _givenSideToBoardIsRight;
		}
		return (_selfShip.GameEntity.GlobalPosition.AsVec2 - _givenTargetToBoard.GameEntity.GlobalPosition.AsVec2).DotProduct(_givenTargetToBoard.GlobalFrame.rotation.f.AsVec2.LeftVec()) >= 0f;
	}

	private bool IsRelevantSideOfEnemyShipRight(MissionShip testedShip)
	{
		if (State == ShipBoardingState.ApproachFromFarAway)
		{
			if (!((_selfShip.GameEntity.GlobalPosition.AsVec2 - testedShip.GameEntity.GlobalPosition.AsVec2).DotProduct(testedShip.GlobalFrame.rotation.f.AsVec2) >= 0f))
			{
				return _givenSideToBoardIsRight;
			}
			return !_givenSideToBoardIsRight;
		}
		return (_selfShip.GameEntity.GlobalPosition.AsVec2 - testedShip.GameEntity.GlobalPosition.AsVec2).DotProduct(testedShip.GlobalFrame.rotation.f.AsVec2.RightVec()) >= 0f;
	}

	private void DetermineEffectiveTargetShip()
	{
		_effectiveSideToBoardIsRight = IsRelevantSideOfEnemyShipRight(_givenTargetToBoard);
		_effectiveTarget = _givenTargetToBoard.GetOutermostConnectedShipFromSide(_effectiveSideToBoardIsRight, out _effectiveSideToBoardIsRight, 0uL);
	}

	private void ApproachFromDistance(MissionShip enemyShip, out Vec2 desiredPosition)
	{
		Vec2 vec = (enemyShip.GameEntity.GlobalPosition.AsVec2 - _selfShip.GameEntity.GlobalPosition.AsVec2).Normalized();
		desiredPosition = enemyShip.GlobalFrame.origin.AsVec2 + (_effectiveSideToBoardIsRight ? vec.RightVec().Normalized() : vec.LeftVec().Normalized()) * 12f;
	}

	private void GettingCloseCase(MissionShip enemyShip, out Vec2 desiredPosition, out Vec2 desiredDirection)
	{
		Vec2 vec = _selfShip.GameEntity.GlobalPosition.AsVec2 - enemyShip.GameEntity.GlobalPosition.AsVec2;
		if (enemyShip == _givenTargetToBoard)
		{
			MatrixFrame globalFrame = enemyShip.GlobalFrame;
			desiredPosition = globalFrame.origin.AsVec2 + ((vec.DotProduct(globalFrame.rotation.f.AsVec2.LeftVec()) >= 0f) ? globalFrame.rotation.f.AsVec2.LeftVec().Normalized() : globalFrame.rotation.f.AsVec2.RightVec().Normalized()) * 12f;
		}
		else
		{
			ApproachFromDistance(enemyShip, out desiredPosition);
		}
		_ = enemyShip.GlobalFrame.origin - _selfShip.GlobalFrame.origin;
		if (enemyShip.GlobalFrame.rotation.f.AsVec2.DotProduct(_selfShip.GlobalFrame.rotation.f.AsVec2) >= 0f)
		{
			desiredDirection = enemyShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		}
		else
		{
			desiredDirection = -enemyShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		}
	}

	public void CalculateShipOrders(out Vec2 desiredPosition, out Vec2 desiredDirection, out MissionShip boardingTargetShip)
	{
		CheckAndSwitchState();
		MatrixFrame globalFrame = _selfShip.GlobalFrame;
		desiredPosition = globalFrame.origin.AsVec2;
		desiredDirection = _selfShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		boardingTargetShip = null;
		if (_givenTargetToBoard != null && _effectiveTarget != null)
		{
			DetermineEffectiveTargetShip();
			switch (State)
			{
			case ShipBoardingState.ApproachFromFarAway:
				ApproachFromDistance(_effectiveTarget, out desiredPosition);
				boardingTargetShip = null;
				break;
			case ShipBoardingState.GettingClose:
				GettingCloseCase(_effectiveTarget, out desiredPosition, out desiredDirection);
				boardingTargetShip = null;
				break;
			case ShipBoardingState.AdjustingOrientation:
			case ShipBoardingState.InPosition:
				GettingCloseCase(_effectiveTarget, out desiredPosition, out desiredDirection);
				boardingTargetShip = _effectiveTarget;
				break;
			case ShipBoardingState.Connected:
				boardingTargetShip = _givenTargetToBoard;
				break;
			case ShipBoardingState.InactiveStuck:
				boardingTargetShip = null;
				break;
			}
			_cachedEffectiveDistance = desiredPosition.Distance(globalFrame.origin.AsVec2);
		}
	}
}
