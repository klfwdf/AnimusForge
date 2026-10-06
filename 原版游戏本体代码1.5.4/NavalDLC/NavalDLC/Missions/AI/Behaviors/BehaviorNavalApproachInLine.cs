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

public sealed class BehaviorNavalApproachInLine : NavalBehaviorComponent
{
	private enum ShipDefenseState
	{
		StandInLine,
		BeingBoarded,
		GoingToHelp,
		HelpingFriend,
		HelpingFinishedStuckBoarded
	}

	private const float DistanceToKeepWithAllyShip = 30f;

	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _formationMainShip;

	private MBReadOnlyList<ShipAttachmentMachine> _formationShipAttachmentMachines;

	private MBReadOnlyList<ShipAttachmentPointMachine> _formationShipAttachmentPointMachines;

	private TeamAINavalComponent _navalTeamAI;

	private ShipDefenseState _currentState;

	private MissionShip _leftAllyShip;

	private MissionShip _rightAllyShip;

	private MissionShip _helpedAllyShip;

	private int _navalLineOrder;

	private bool _actualRightSide;

	private MissionShip _allyShip;

	private bool _tacticallyOnRightSide;

	private bool _isAnchor;

	private bool _hasPulledAhead;

	private NavalBehaviorBoardShipSubtask _boardShipSubtask;

	public BehaviorNavalApproachInLine(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0.8f;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		List<WeakGameEntity> source = new List<WeakGameEntity>();
		_formationShipAttachmentMachines = (from ce in source
			where ce.HasScriptOfType<ShipAttachmentMachine>()
			select ce.GetFirstScriptOfType<ShipAttachmentMachine>()).ToMBList();
		_formationShipAttachmentPointMachines = (from ce in source
			where ce.HasScriptOfType<ShipAttachmentPointMachine>()
			select ce.GetFirstScriptOfType<ShipAttachmentPointMachine>()).ToMBList();
		_navalTeamAI = base.Formation.Team.TeamAI as TeamAINavalComponent;
		CalculateCurrentOrder();
		_boardShipSubtask = new NavalBehaviorBoardShipSubtask(_formationMainShip);
	}

	public override void RefreshShipReferences()
	{
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		_leftAllyShip = null;
		_rightAllyShip = null;
		if (_navalLineOrder >= _navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count || _navalLineOrder < 0)
		{
			_navalLineOrder = 0;
		}
		SetTargetShipSideAndOrder(_tacticallyOnRightSide, _navalLineOrder, _isAnchor);
		if (_helpedAllyShip != null)
		{
			_helpedAllyShip = (_tacticallyOnRightSide ? _leftAllyShip : _rightAllyShip);
		}
	}

	public void SetTargetShipSideAndOrder(bool rightSide, int navalLineOrder, bool isAnchor)
	{
		if (_navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count > 0)
		{
			_isAnchor = isAnchor;
			_tacticallyOnRightSide = rightSide;
			_actualRightSide = rightSide;
			_navalLineOrder = navalLineOrder;
			Formation formation = ((navalLineOrder > 0) ? _navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.ElementAt(_navalLineOrder - 1) : null);
			Formation formation2 = ((navalLineOrder < _navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Count - 1) ? _navalTeamAI.TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.ElementAt(_navalLineOrder + 1) : null);
			if (formation != null)
			{
				_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, formation.FormationIndex, out _leftAllyShip);
			}
			if (formation2 != null)
			{
				_navalShipsLogic.GetShip(base.Formation.Team.TeamSide, formation2.FormationIndex, out _rightAllyShip);
			}
			if (_tacticallyOnRightSide)
			{
				_allyShip = _leftAllyShip;
			}
			else
			{
				_allyShip = _rightAllyShip;
			}
		}
	}

	protected override void CalculateCurrentOrder()
	{
		if (_navalShipsLogic == null || base.Formation.CachedClosestEnemyFormation == null || _allyShip == null)
		{
			base.CurrentOrder = MovementOrder.MovementOrderStop;
		}
		else if (_formationMainShip != null && _formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: false))
		{
			if (_currentState == ShipDefenseState.BeingBoarded)
			{
				base.CurrentOrder = _formationMainShip.GetMovementOrderToRallyPoint();
				CurrentFacingOrder = _formationMainShip.GetFacingOrderToRallyPoint();
			}
			else
			{
				base.CurrentOrder = MovementOrder.MovementOrderCharge;
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			}
		}
		else
		{
			base.CurrentOrder = MovementOrder.MovementOrderStop;
		}
	}

	private void CalculateAndSetShipOrders()
	{
		Vec2 position = _formationMainShip.GlobalFrame.origin.AsVec2;
		Vec2 direction = _formationMainShip.GlobalFrame.rotation.f.AsVec2;
		MissionShip boardingTargetShip = null;
		switch (_currentState)
		{
		case ShipDefenseState.StandInLine:
		{
			Vec2 vec = (_navalTeamAI.TeamNavalQuerySystem.AverageEnemyShipPosition - _navalTeamAI.TeamNavalQuerySystem.AverageShipPosition).Normalized();
			if (_isAnchor)
			{
				Vec2 vec2 = _navalTeamAI.TeamNavalQuerySystem.AverageShipPosition * _navalTeamAI.TeamNavalQuerySystem.TeamShipsWithFormationsInLeftToRightOrder.Count;
				vec2 -= _formationMainShip.GameEntity.GlobalPosition.AsVec2;
				vec2 /= (float)(_navalTeamAI.TeamNavalQuerySystem.TeamShipsWithFormationsInLeftToRightOrder.Count - 1);
				Vec2 v = _formationMainShip.GameEntity.GlobalPosition.AsVec2 - vec2;
				float num = vec.DotProduct(v);
				bool flag = false;
				if (_navalTeamAI.UseSpawnPathApproachPosition)
				{
					_navalTeamAI.GetRiverApproachPosition(out position, out direction);
					flag = _formationMainShip.GlobalFrame.origin.AsVec2.DistanceSquared(position) > 900f && _formationMainShip.GlobalFrame.origin.AsVec2.Distance(_navalTeamAI.TeamNavalQuerySystem.AverageEnemyShipPosition) - position.Distance(_navalTeamAI.TeamNavalQuerySystem.AverageEnemyShipPosition) >= 50f;
				}
				if (flag)
				{
					break;
				}
				if (_hasPulledAhead)
				{
					if (num <= 10f)
					{
						_hasPulledAhead = false;
					}
				}
				else if (num >= 20f)
				{
					_hasPulledAhead = true;
				}
				direction = vec;
				position = ((!_hasPulledAhead) ? (_formationMainShip.GlobalFrame.origin.AsVec2 + direction * 450f) : (_formationMainShip.GlobalFrame.origin.AsVec2 + direction * 15f));
			}
			else
			{
				(_formationMainShip.GlobalFrame.origin - _allyShip.GlobalFrame.origin).Normalize();
				Vec2 vec3 = (_actualRightSide ? (_navalTeamAI.UseSpawnPathApproachPosition ? _allyShip.GlobalFrame.rotation.f.AsVec2.RightVec() : vec.RightVec()) : (_navalTeamAI.UseSpawnPathApproachPosition ? _allyShip.GlobalFrame.rotation.f.AsVec2.LeftVec() : vec.LeftVec()));
				position = _allyShip.GlobalFrame.origin.AsVec2 + vec * 30f + vec3 * 30f;
				float num2 = (position - _formationMainShip.GlobalFrame.origin.AsVec2).DotProduct(vec);
				if (num2 < 0f)
				{
					position += num2 * vec;
				}
				direction = vec;
			}
			break;
		}
		case ShipDefenseState.GoingToHelp:
		case ShipDefenseState.HelpingFriend:
			_boardShipSubtask.CalculateShipOrders(out position, out direction, out boardingTargetShip);
			break;
		}
		if (_formationMainShip.IsFormationAndShipAIControlled)
		{
			_formationMainShip.ShipOrder.SetShipMovementOrder(position, in direction);
			_formationMainShip.ShipOrder.SetBoardingTargetShip(boardingTargetShip);
		}
	}

	private void CheckAndSwitchState()
	{
		if (base.Formation.CachedClosestEnemyFormation == null)
		{
			return;
		}
		switch (_currentState)
		{
		case ShipDefenseState.StandInLine:
			if (_formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.BeingBoarded;
			}
			else if (_leftAllyShip != null && _leftAllyShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.GoingToHelp;
				_helpedAllyShip = _leftAllyShip;
				_boardShipSubtask.SetTargetShipAndSide(_helpedAllyShip, _tacticallyOnRightSide);
			}
			else if (_rightAllyShip != null && _rightAllyShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.GoingToHelp;
				_helpedAllyShip = _rightAllyShip;
				_boardShipSubtask.SetTargetShipAndSide(_helpedAllyShip, _tacticallyOnRightSide);
			}
			else if (_formationMainShip.GetIsConnected())
			{
				_currentState = ShipDefenseState.HelpingFinishedStuckBoarded;
			}
			break;
		case ShipDefenseState.BeingBoarded:
			if (!_formationMainShip.GetIsConnected())
			{
				_currentState = ShipDefenseState.StandInLine;
			}
			else if (!_formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.HelpingFinishedStuckBoarded;
			}
			break;
		case ShipDefenseState.GoingToHelp:
			if (_formationMainShip.SearchShipConnection(_helpedAllyShip, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.HelpingFriend;
			}
			else if (_helpedAllyShip == null || !_helpedAllyShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.StandInLine;
				_helpedAllyShip = null;
			}
			else if (_formationMainShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.BeingBoarded;
				_helpedAllyShip = null;
			}
			else if (_formationMainShip.GetIsConnected())
			{
				_currentState = ShipDefenseState.HelpingFinishedStuckBoarded;
				_helpedAllyShip = null;
			}
			break;
		case ShipDefenseState.HelpingFriend:
			if (!_formationMainShip.SearchShipConnection(_helpedAllyShip, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))
			{
				_currentState = ShipDefenseState.GoingToHelp;
				_boardShipSubtask.SetTargetShipAndSide(_helpedAllyShip, _tacticallyOnRightSide);
			}
			break;
		}
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		_currentState = ShipDefenseState.StandInLine;
	}

	protected override void OnBehaviorActivatedAux()
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_formationMainShip = _navalShipsLogic.GetShipAssignment(base.Formation.Team.TeamSide, base.Formation.FormationIndex).MissionShip;
		_boardShipSubtask.SetOwnerShip(_formationMainShip);
		_boardShipSubtask.SetTargetShipAndSide(null, _tacticallyOnRightSide);
		_currentState = ShipDefenseState.StandInLine;
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
		if (_formationMainShip.Formation != base.Formation && _navalShipsLogic.GetShip(base.Formation.Team.TeamSide, base.Formation.FormationIndex, out var ship))
		{
			_formationMainShip = ship;
		}
		CheckAndSwitchState();
		CalculateAndSetShipOrders();
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
	}

	protected override float GetAiWeight()
	{
		float num = 1f;
		if (base.Formation.CachedClosestEnemyFormation != null)
		{
			if (base.Formation.QuerySystem.FormationMeleeFightingPower > 0f)
			{
				float num2 = base.Formation.CachedClosestEnemyFormation.FormationMeleeFightingPower / base.Formation.QuerySystem.FormationMeleeFightingPower;
				num *= ((num2 >= 1f) ? num2 : 1f);
			}
			else
			{
				num = 2f;
			}
		}
		float num3 = 1f / base.Formation.Team.QuerySystem.TotalPowerRatio;
		num *= ((num3 >= 1f) ? num3 : 1f);
		return ((_currentState == ShipDefenseState.HelpingFinishedStuckBoarded) ? 0f : 1f) * num * 2f * ((_currentState != ShipDefenseState.HelpingFinishedStuckBoarded && _currentState != ShipDefenseState.StandInLine) ? 5f : 1f);
	}
}
