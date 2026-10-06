using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.Behaviors;

public sealed class BehaviorNavalEngageCorrespondingEnemy : NavalBehaviorComponent
{
	private enum ShipBoardingState
	{
		ApproachFromFarAway,
		GettingClose,
		AdjustingOrientation,
		InPosition,
		Connected
	}

	private const float IdealBoardingDistance = 12f;

	private const float MaximumBoardingDistance = 30f;

	private const float DriftedAwayDistance = 50f;

	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _formationMainShip;

	private MBReadOnlyList<ShipAttachmentMachine> _formationShipAttachmentMachines;

	private MBReadOnlyList<ShipAttachmentPointMachine> _formationShipAttachmentPointMachines;

	private TeamAINavalComponent _navalTeamAI;

	private ShipBoardingState _currentState;

	private bool _tacticallyOnRightSide;

	private MissionShip _targetShip;

	private int _navalLineOrder;

	private bool _perfectMatch = true;

	private bool _actualRightSide;

	private NavalBehaviorBoardShipSubtask _boardShipSubtask;

	public BehaviorNavalEngageCorrespondingEnemy(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		_formationMainShip.GameEntity.GetChildrenRecursive(ref children);
		_formationShipAttachmentMachines = (from ce in children
			where ce.HasScriptOfType<ShipAttachmentMachine>()
			select ce.GetFirstScriptOfType<ShipAttachmentMachine>()).ToMBList();
		_formationShipAttachmentPointMachines = (from ce in children
			where ce.HasScriptOfType<ShipAttachmentPointMachine>()
			select ce.GetFirstScriptOfType<ShipAttachmentPointMachine>()).ToMBList();
		_navalTeamAI = base.Formation.Team.TeamAI as TeamAINavalComponent;
		_currentState = ShipBoardingState.ApproachFromFarAway;
		CalculateCurrentOrder();
		_boardShipSubtask = new NavalBehaviorBoardShipSubtask(_formationMainShip);
	}

	public override void RefreshShipReferences()
	{
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		SetTargetShipSideAndOrder(_tacticallyOnRightSide, _navalLineOrder);
	}

	public void SetTargetShipSideAndOrder(bool rightSide, int navalLineOrder)
	{
		_tacticallyOnRightSide = rightSide;
		_actualRightSide = rightSide;
		_navalLineOrder = navalLineOrder;
		_targetShip = FindCorrespondingEnemyShip();
		_boardShipSubtask.SetTargetShipAndSide(_targetShip, _tacticallyOnRightSide);
	}

	private MissionShip FindCorrespondingEnemyShip()
	{
		if (_formationMainShip == null || _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count <= 0)
		{
			return null;
		}
		if (_formationMainShip.GetIsConnectedToEnemy(out var connectedEnemyShip))
		{
			return connectedEnemyShip;
		}
		float num = ((float)_navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count - 1f) * 0.5f;
		float num2 = ((float)_navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count - 1f) * 0.5f;
		bool flag = num > num2;
		if ((int)num == _navalLineOrder && (float)(int)num + 0.1f > num)
		{
			if (num2 >= (float)(int)num2 + 0.1f)
			{
				_actualRightSide = flag;
				num2 += (_actualRightSide ? 0.5f : (-0.5f));
			}
			if (num2 < 0f)
			{
				num2 = 0f;
				_actualRightSide = true;
			}
			else if (num2 >= (float)_navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count)
			{
				num2 = _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count - 1;
				_actualRightSide = false;
			}
			return _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder[(int)num2];
		}
		int num3;
		int num4;
		float num5;
		float num6;
		if ((float)(int)num + 0.1f > num)
		{
			num3 = (int)(num - 1f);
			num4 = (int)(num + 1f);
			num5 = num2 + 1f;
			num6 = num2 - 1f;
		}
		else
		{
			num3 = (int)(num - 0.5f);
			num4 = (int)(num + 0.5f);
			num5 = num2 + 0.5f;
			num6 = num2 - 0.5f;
		}
		while (num3 >= 0 || num4 < _navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count)
		{
			if (num3 == _navalLineOrder)
			{
				if (num5 >= (float)(int)num5 + 0.1f)
				{
					num5 += (flag ? (-0.5f) : 0.5f);
					_actualRightSide = !flag;
				}
				else
				{
					_actualRightSide = flag;
				}
				if ((int)num5 >= _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count)
				{
					_actualRightSide = false;
					num5 = _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder.Count - 1;
				}
				return _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder[(int)num5];
			}
			if (num4 == _navalLineOrder)
			{
				if (num6 >= (float)(int)num6 + 0.1f)
				{
					num6 += (flag ? 0.5f : (-0.5f));
					_actualRightSide = flag;
				}
				else
				{
					_actualRightSide = !flag;
				}
				if (num6 < 0f)
				{
					_actualRightSide = true;
					num6 = 0f;
				}
				return _navalTeamAI.TeamNavalQuerySystem.EnemyShipsWithFormationsInLeftToRightOrder[(int)num6];
			}
			num3--;
			num4++;
			num5 += 1f;
			num6 -= 1f;
		}
		return null;
	}

	private void RefreshTargetShip()
	{
		MissionShip missionShip2;
		MissionShip missionShip = (missionShip2 = _boardShipSubtask.GetCurrentEffectiveTargetShip());
		Formation formation = base.Formation.CachedClosestEnemyFormation?.Formation;
		MissionShip ship = null;
		if (formation != null)
		{
			_navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out ship);
		}
		if (ship != null)
		{
			float num = ship.GameEntity.GlobalPosition.DistanceSquared(_formationMainShip.GameEntity.GlobalPosition);
			if (num <= 3600f)
			{
				double num2 = Math.Sqrt(num);
				if (_targetShip == null || ((double)_targetShip.GameEntity.GlobalPosition.Distance(_formationMainShip.GameEntity.GlobalPosition) - num2 > 30.0 && (double)_boardShipSubtask.GetEffectiveDistanceToObjective() - num2 > 30.0))
				{
					missionShip2 = ship;
				}
			}
		}
		if (missionShip != missionShip2 && _targetShip != missionShip2)
		{
			MissionShip targetShip = _targetShip;
			if ((targetShip != null && !targetShip.AnyActiveFormationTroopOnShip) || missionShip2 == null)
			{
				_targetShip = missionShip2;
			}
			else if (_boardShipSubtask.GetEffectiveDistanceToObjective() > 60f || _boardShipSubtask.GetEffectiveDistanceToObjective() > _formationMainShip.GameEntity.GlobalPosition.Distance(missionShip2.GameEntity.GlobalPosition) * 1.2f)
			{
				_targetShip = missionShip2;
				_boardShipSubtask.SetTargetShipAndSide(_targetShip, _tacticallyOnRightSide);
			}
		}
	}

	protected override void CalculateCurrentOrder()
	{
		if (_navalShipsLogic == null || base.Formation.CachedClosestEnemyFormation == null || _targetShip == null)
		{
			base.CurrentOrder = MovementOrder.MovementOrderStop;
		}
		else if (_formationMainShip != null && (_formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: false, acceptNotBridgedConnections: false) || _currentState == ShipBoardingState.Connected))
		{
			base.CurrentOrder = MovementOrder.MovementOrderCharge;
		}
		else
		{
			base.CurrentOrder = MovementOrder.MovementOrderStop;
		}
	}

	private void CalculateAndSetShipOrders()
	{
		if (base.Formation.CachedClosestEnemyFormation != null && _targetShip != null && _formationMainShip != null && _formationMainShip.IsFormationAndShipAIControlled)
		{
			_boardShipSubtask.CalculateShipOrders(out var desiredPosition, out var desiredDirection, out var boardingTargetShip);
			_formationMainShip.ShipOrder.SetShipMovementOrder(desiredPosition, in desiredDirection);
			_formationMainShip.ShipOrder.SetBoardingTargetShip(boardingTargetShip);
		}
	}

	private void CheckAndRefreshTargetIfNecessary()
	{
		if (_targetShip == null || !_targetShip.AnyActiveFormationTroopOnShip)
		{
			_targetShip = FindCorrespondingEnemyShip();
			_boardShipSubtask.SetTargetShipAndSide(_targetShip, _tacticallyOnRightSide);
			return;
		}
		switch (_boardShipSubtask.State)
		{
		case NavalBehaviorBoardShipSubtask.ShipBoardingState.ApproachFromFarAway:
		case NavalBehaviorBoardShipSubtask.ShipBoardingState.GettingClose:
			RefreshTargetShip();
			break;
		case NavalBehaviorBoardShipSubtask.ShipBoardingState.InactiveStuck:
		{
			MissionShip missionShip = FindCorrespondingEnemyShip();
			if (missionShip != _boardShipSubtask.GetCurrentGivenTarget())
			{
				_targetShip = missionShip;
				_boardShipSubtask.SetTargetShipAndSide(_targetShip, _tacticallyOnRightSide);
			}
			break;
		}
		}
	}

	private void CheckAndSwitchState()
	{
		if (base.Formation.CachedClosestEnemyFormation != null && _targetShip != null && _targetShip.AnyActiveFormationTroopOnShip && _formationMainShip != null)
		{
			MatrixFrame globalFrame = _targetShip.GlobalFrame;
			MatrixFrame globalFrame2 = _formationMainShip.GlobalFrame;
			Vec2 vec = (_actualRightSide ? globalFrame.rotation.f.AsVec2.LeftVec().Normalized() : globalFrame.rotation.f.AsVec2.RightVec().Normalized());
			switch (_currentState)
			{
			case ShipBoardingState.ApproachFromFarAway:
				if ((globalFrame.origin.AsVec2 - globalFrame2.origin.AsVec2).LengthSquared < 900f || (globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared < 2500f)
				{
					_currentState = ShipBoardingState.GettingClose;
				}
				break;
			case ShipBoardingState.GettingClose:
				if ((globalFrame.origin.AsVec2 - globalFrame2.origin.AsVec2).LengthSquared < 900f || (globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared < 900f)
				{
					_currentState = ShipBoardingState.AdjustingOrientation;
				}
				break;
			case ShipBoardingState.AdjustingOrientation:
				if ((globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared > 2500f)
				{
					_currentState = ShipBoardingState.GettingClose;
				}
				else if (Math.Abs(globalFrame2.rotation.f.AsVec2.Normalized().DotProduct(globalFrame.rotation.f.AsVec2.Normalized())) > 0.8f)
				{
					_currentState = ShipBoardingState.InPosition;
				}
				break;
			case ShipBoardingState.InPosition:
			{
				bool flag2 = false;
				foreach (ShipAttachmentMachine formationShipAttachmentMachine in _formationShipAttachmentMachines)
				{
					if (formationShipAttachmentMachine.CurrentAttachment != null)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					foreach (ShipAttachmentPointMachine formationShipAttachmentPointMachine in _formationShipAttachmentPointMachines)
					{
						if (formationShipAttachmentPointMachine.CurrentAttachment != null)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (flag2)
				{
					_currentState = ShipBoardingState.Connected;
				}
				else if (Math.Abs(globalFrame2.rotation.f.AsVec2.Normalized().DotProduct(globalFrame.rotation.f.AsVec2.Normalized())) < 0.6f)
				{
					_currentState = ShipBoardingState.AdjustingOrientation;
				}
				else if ((globalFrame.origin.AsVec2 + vec * 12f - globalFrame2.origin.AsVec2).LengthSquared > 2500f)
				{
					_currentState = ShipBoardingState.GettingClose;
				}
				break;
			}
			case ShipBoardingState.Connected:
			{
				bool flag = false;
				foreach (ShipAttachmentMachine formationShipAttachmentMachine2 in _formationShipAttachmentMachines)
				{
					if (formationShipAttachmentMachine2.CurrentAttachment != null)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					foreach (ShipAttachmentPointMachine formationShipAttachmentPointMachine2 in _formationShipAttachmentPointMachines)
					{
						if (formationShipAttachmentPointMachine2.CurrentAttachment != null)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					_currentState = ShipBoardingState.GettingClose;
				}
				break;
			}
			}
		}
		else
		{
			RefreshTargetShip();
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		_currentState = ShipBoardingState.ApproachFromFarAway;
	}

	public override void ResetBehavior()
	{
		base.ResetBehavior();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		_currentState = ShipBoardingState.ApproachFromFarAway;
	}

	protected override void OnBehaviorActivatedAux()
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		RefreshTargetShip();
		_boardShipSubtask.SetOwnerShip(_formationMainShip);
		_targetShip = FindCorrespondingEnemyShip();
		_boardShipSubtask.SetTargetShipAndSide(_targetShip, _tacticallyOnRightSide);
		_currentState = ShipBoardingState.ApproachFromFarAway;
		_formationMainShip.ShipOrder.SetBoardingTargetShip(null);
		_formationMainShip.ShipOrder.SetCutLoose(enable: false);
		_formationMainShip.ShipOrder.SetOrderOarsmenLevel(2);
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	private void CancelPreferredTargetShipForAttachmentMachines()
	{
		foreach (ShipAttachmentMachine formationShipAttachmentMachine in _formationShipAttachmentMachines)
		{
			formationShipAttachmentMachine.SetPreferredTargetShip(null);
		}
	}

	public override void OnLostAIControl()
	{
		base.OnLostAIControl();
		CancelPreferredTargetShipForAttachmentMachines();
	}

	public override void OnBehaviorCanceled()
	{
		base.OnBehaviorCanceled();
		CancelPreferredTargetShipForAttachmentMachines();
	}

	public override void TickOccasionally()
	{
		if (_navalShipsLogic == null)
		{
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			if (_navalShipsLogic == null)
			{
				return;
			}
		}
		if (_formationMainShip.Formation != base.Formation)
		{
			_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out _formationMainShip);
		}
		CheckAndRefreshTargetIfNecessary();
		CalculateAndSetShipOrders();
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override float GetAiWeight()
	{
		float value = 0f;
		if (base.Formation.CachedClosestEnemyFormation != null)
		{
			value = ((!(base.Formation.CachedClosestEnemyFormation.FormationMeleeFightingPower > 0f)) ? 20f : (base.Formation.QuerySystem.FormationMeleeFightingPower / base.Formation.CachedClosestEnemyFormation.FormationMeleeFightingPower));
		}
		return (_perfectMatch ? 1.5f : 1.25f) * TaleWorlds.Library.MathF.Clamp(value, 0f, 20f) * base.Formation.QuerySystem.InfantryUnitRatio;
	}
}
