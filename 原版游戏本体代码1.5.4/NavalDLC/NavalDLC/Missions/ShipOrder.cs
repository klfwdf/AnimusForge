using System;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions;

public class ShipOrder
{
	public enum ShipMovementOrderEnum
	{
		Stop = 0,
		Move = 1,
		Retreat = 2,
		StaticOrderCount = 3,
		Follow = 3,
		Engage = 4,
		Skirmish = 5
	}

	private enum ShipIndependenceState
	{
		Independent,
		Connected,
		EnemyOnShip
	}

	private enum ShipDetachmentPriority
	{
		PlacementDetachment = 1,
		Oar,
		ControllerMachine,
		SiegeWeapon,
		ConnectionMachine
	}

	private const float BoardingDistance = 12f;

	private const float SkirmishDistance = 60f;

	private const float TimerDuration = 1f;

	private const float TargetCorrectionCheckDistance = 2f;

	private readonly QueryData<bool> _isEnemyOnShip;

	private readonly QueryData<MissionShip> _closestEnemyShip;

	private readonly MissionShip _ownerShip;

	private Vec2 _orderGlobalPosition = Vec2.Invalid;

	private Vec2 _orderGlobalDirection = Vec2.Forward;

	private bool _inSkirmishPosition;

	private MissionShip _targetShip;

	private MissionShip _engageGivenTargetOrder;

	private float _offsetDirection;

	private bool _autoSelectTargetShip;

	private Vec2 _offsetPosition = Vec2.Zero;

	private Formation _ownerFormation;

	private NavalShipsLogic _navalShipsLogic;

	private bool _cutLooseOrderActive;

	private MissionShip _boardingTargetShip;

	private ShipIndependenceState _shipIndependenceState;

	private RandomTimer _detachmentTickTimer;

	private bool _oarLevelOverridden;

	private int _originalOarsmenLevel = 2;

	private bool _isChargeOrderOverridden;

	private MBList<IFormationUnit> _availableUnitList;

	private Vec2 _lastCheckedOrderPosition = Vec2.Invalid;

	private int _enforceSailUsage;

	private MissionTimer _orderTimer;

	private MissionTimer _placementDetachmentTimer;

	public MissionShip TargetShip
	{
		get
		{
			return _targetShip;
		}
		private set
		{
			if (_targetShip == value)
			{
				return;
			}
			if (value == null)
			{
				_targetShip = null;
				SetBoardingTargetShip(null);
				return;
			}
			_targetShip = value;
			if (MovementOrderEnum == ShipMovementOrderEnum.Engage)
			{
				SetBoardingTargetShip(_targetShip);
			}
			else
			{
				SetBoardingTargetShip(null);
			}
		}
	}

	public bool HasAIController => _ownerShip.IsAIControlled;

	public bool IsAIControllableWithoutTroops { get; private set; }

	public bool IsAIControllable
	{
		get
		{
			if (_ownerShip.IsAIControlled)
			{
				if (!_ownerShip.AnyActiveFormationTroopOnShip)
				{
					return IsAIControllableWithoutTroops;
				}
				return true;
			}
			return false;
		}
	}

	public bool HasStaticOrder => MovementOrderEnum < ShipMovementOrderEnum.StaticOrderCount;

	public bool IsAutoSelectingTargetShip => _autoSelectTargetShip;

	public int OarsmenLevel { get; private set; } = 2;

	public bool TickDetachmentsNeeded { get; private set; }

	public bool BoardAtWill { get; private set; }

	public bool IsBoardingAvailable { get; set; } = true;

	public ShipMovementOrderEnum MovementOrderEnum { get; private set; }

	public MissionShip ClosestEnemyShip => _closestEnemyShip.Value;

	public bool IsEnemyOnShip => _isEnemyOnShip.Value;

	public int EnforceSailUsage
	{
		get
		{
			if (_ownerFormation != null && _ownerShip.IsAIControlled)
			{
				return _enforceSailUsage;
			}
			return 0;
		}
	}

	public Vec2 TargetPosition => _orderGlobalPosition;

	public Vec2 TargetDirection => _orderGlobalDirection;

	public ShipOrder(MissionShip missionShip, Formation ownerFormation)
	{
		_ownerShip = missionShip;
		FormationJoinShip(ownerFormation);
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipControllerChanged += OnShipControllerChanged;
		_navalShipsLogic.ShipRemovedEvent += OnShipRemoved;
		_isEnemyOnShip = new QueryData<bool>(delegate
		{
			if (_ownerShip.Team == null)
			{
				return false;
			}
			foreach (Team team in Mission.Current.Teams)
			{
				if (team.IsEnemyOf(_ownerShip.Team))
				{
					foreach (Agent activeAgent in team.ActiveAgents)
					{
						if (_ownerShip.GetIsAgentOnShip(activeAgent))
						{
							return true;
						}
					}
				}
			}
			return false;
		}, 3f);
		_closestEnemyShip = new QueryData<MissionShip>(delegate
		{
			float num = float.MaxValue;
			MissionShip result = null;
			Vec3 origin = _ownerShip.GlobalFrame.origin;
			foreach (Team team2 in Mission.Current.Teams)
			{
				if (_ownerFormation.Team.Side.IsOpponentOf(team2.Side))
				{
					foreach (Formation item in team2.FormationsIncludingEmpty)
					{
						if (item.CountOfUnits > 0)
						{
							_navalShipsLogic.GetShip(team2.TeamSide, item.FormationIndex, out var ship);
							float num2 = ship.GlobalFrame.origin.DistanceSquared(origin);
							if (num2 < num)
							{
								num = num2;
								result = ship;
							}
						}
					}
				}
			}
			return result;
		}, 5f);
		MovementOrderEnum = ShipMovementOrderEnum.Stop;
		_autoSelectTargetShip = false;
		_shipIndependenceState = ShipIndependenceState.Independent;
		_detachmentTickTimer = new RandomTimer(Mission.Current.CurrentTime, 0.9f, 1.1f);
		TickDetachmentsNeeded = true;
		_availableUnitList = new MBList<IFormationUnit>();
		_orderTimer = new MissionTimer(1f);
		_orderTimer.Set(MBRandom.RandomFloat * 1f);
		_placementDetachmentTimer = new MissionTimer(5f);
		_placementDetachmentTimer.Set(MBRandom.RandomFloat * 5f);
		SetTargetPosition(_ownerShip.GlobalFrame.origin.AsVec2, isForced: true);
	}

	public void MakeEnemyOnShipExpire()
	{
		_isEnemyOnShip.Expire();
	}

	public void SetEnforcedSailUsage(int enforce)
	{
		_enforceSailUsage = enforce;
	}

	public void SetFormation(Formation formation)
	{
		_ownerFormation = formation;
	}

	public void OnShipCaptured(MissionShip ship1, MissionShip ship2)
	{
		_closestEnemyShip.Expire();
		_isEnemyOnShip.Expire();
		if (TargetShip?.Team?.TeamSide != _ownerShip.Team?.TeamSide)
		{
			TargetShip = null;
			if (!HasStaticOrder)
			{
				_orderTimer.Reset();
				UpdateDynamicMovementOrder();
			}
		}
		if (_ownerShip == ship1 || _ownerShip == ship2)
		{
			_ownerShip.ShipSiegeWeapon?.OnShipCaptured((_ownerShip == ship1) ? ship1.BattleSide : ship2.BattleSide);
		}
	}

	public void SetAIControllableWithoutTroops(bool value)
	{
		IsAIControllableWithoutTroops = value;
	}

	public void FormationJoinShip(Formation formation)
	{
		if (formation != null && formation != _ownerFormation)
		{
			_ownerFormation = formation;
			StartUsingMachines();
		}
	}

	private void StartUsingMachines()
	{
		if (!_ownerShip.BeingAbandoned)
		{
			_ownerFormation.JoinDetachment(_ownerShip.ClimbingMachineDetachment);
			foreach (ShipOarMachine leftSideShipOarMachine in _ownerShip.LeftSideShipOarMachines)
			{
				if (!leftSideShipOarMachine.IsDisabled)
				{
					_ownerFormation.StartUsingMachine(leftSideShipOarMachine, isPlayerOrder: true);
				}
			}
			foreach (ShipOarMachine rightSideShipOarMachine in _ownerShip.RightSideShipOarMachines)
			{
				if (!rightSideShipOarMachine.IsDisabled)
				{
					_ownerFormation.StartUsingMachine(rightSideShipOarMachine, isPlayerOrder: true);
				}
			}
			if (_ownerShip.ShipSiegeWeapon != null && !_ownerShip.ShipSiegeWeapon.IsDisabled)
			{
				_ownerFormation.StartUsingMachine(_ownerShip.ShipSiegeWeapon, isPlayerOrder: true);
			}
			if (!_ownerShip.ShipControllerMachine.IsDisabled)
			{
				_ownerFormation.StartUsingMachine(_ownerShip.ShipControllerMachine, isPlayerOrder: true);
			}
			foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
			{
				if (!attachmentMachine.IsDisabled)
				{
					_ownerFormation.StartUsingMachine(attachmentMachine, isPlayerOrder: true);
					attachmentMachine.SetIsDisabledForAI(isDisabledForAI: true);
				}
			}
			_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
		{
			if (!attachmentPointMachine.IsDisabled)
			{
				_ownerFormation.StartUsingMachine(attachmentPointMachine, isPlayerOrder: true);
				attachmentPointMachine.SetIsDisabledForAI(isDisabledForAI: true);
			}
		}
	}

	public void StopUsingMachines(bool formationLeaving)
	{
		if (_ownerFormation == null)
		{
			return;
		}
		if (_ownerFormation.Detachments.IndexOf(_ownerShip.ClimbingMachineDetachment) >= 0)
		{
			_ownerFormation.LeaveDetachment(_ownerShip.ClimbingMachineDetachment);
		}
		foreach (ShipOarMachine leftSideShipOarMachine in _ownerShip.LeftSideShipOarMachines)
		{
			if (_ownerFormation.Detachments.IndexOf(leftSideShipOarMachine) >= 0)
			{
				_ownerFormation.StopUsingMachine(leftSideShipOarMachine, isPlayerOrder: true);
			}
		}
		foreach (ShipOarMachine rightSideShipOarMachine in _ownerShip.RightSideShipOarMachines)
		{
			if (_ownerFormation.Detachments.IndexOf(rightSideShipOarMachine) >= 0)
			{
				_ownerFormation.StopUsingMachine(rightSideShipOarMachine, isPlayerOrder: true);
			}
		}
		if (_ownerShip.ShipSiegeWeapon != null && _ownerFormation.Detachments.IndexOf(_ownerShip.ShipSiegeWeapon) >= 0)
		{
			_ownerFormation.StopUsingMachine(_ownerShip.ShipSiegeWeapon, isPlayerOrder: true);
		}
		if (_ownerFormation.Detachments.IndexOf(_ownerShip.ShipControllerMachine) >= 0)
		{
			_ownerFormation.StopUsingMachine(_ownerShip.ShipControllerMachine, isPlayerOrder: true);
		}
		foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
		{
			if (_ownerFormation.Detachments.IndexOf(attachmentMachine) >= 0)
			{
				_ownerFormation.StopUsingMachine(attachmentMachine, isPlayerOrder: true);
				attachmentMachine.SetIsDisabledForAI(isDisabledForAI: true);
			}
		}
		if (formationLeaving || _ownerShip.IsShipNavmeshDisabled || !_ownerShip.BeingAbandoned)
		{
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
			{
				if (_ownerFormation.Detachments.IndexOf(attachmentPointMachine) >= 0)
				{
					_ownerFormation.StopUsingMachine(attachmentPointMachine, isPlayerOrder: true);
					attachmentPointMachine.SetIsDisabledForAI(isDisabledForAI: true);
				}
			}
		}
		if (_ownerFormation.Detachments.IndexOf(_ownerShip.ShipPlacementDetachment) >= 0)
		{
			_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
		}
	}

	public void FormationLeaveShip()
	{
		if (_ownerFormation != null)
		{
			StopUsingMachines(formationLeaving: true);
			_ownerFormation = null;
		}
	}

	public bool GetIsChargeOrderOverridden()
	{
		return _isChargeOrderOverridden;
	}

	public bool IsOarsmenLevelLocked()
	{
		return _oarLevelOverridden;
	}

	public void SetOrderOarsmenLevel(int newOarsmenLevel)
	{
		_originalOarsmenLevel = newOarsmenLevel;
		if (!_oarLevelOverridden)
		{
			SetOarsmenLevel(_originalOarsmenLevel);
		}
	}

	private void SetOarsmenLevel(int newOarsmenLevel)
	{
		if (OarsmenLevel == newOarsmenLevel)
		{
			return;
		}
		if (newOarsmenLevel > OarsmenLevel)
		{
			TickDetachmentsNeeded = true;
			int num = 0;
			int num2 = int.MaxValue;
			if (OarsmenLevel == 1)
			{
				num2 = (_ownerFormation.Arrangement.UnitCount + _ownerShip.ShipPlacementDetachment.CountOfAgents) / 2;
			}
			for (int i = 0; i < _ownerShip.LeftSideShipOarMachines.Count && i < _ownerShip.RightSideShipOarMachines.Count; i++)
			{
				_ownerShip.LeftSideShipOarMachines[i].SetIsDisabledForAI(isDisabledForAI: false);
				_ownerShip.RightSideShipOarMachines[i].SetIsDisabledForAI(isDisabledForAI: false);
				if (newOarsmenLevel == 1)
				{
					num += 2;
					if (num >= num2)
					{
						break;
					}
				}
			}
		}
		else
		{
			int num3 = _ownerShip.LeftSideShipOarMachines.Count + _ownerShip.RightSideShipOarMachines.Count;
			int num4;
			switch (newOarsmenLevel)
			{
			case 0:
				num4 = 0;
				break;
			case 1:
			{
				int num5 = _ownerFormation.CountOfUnits;
				if (_ownerFormation.HasPlayerControlledTroop)
				{
					num5--;
				}
				num4 = Math.Min(num3, num5) / 2;
				break;
			}
			default:
				num4 = num3;
				break;
			}
			LowerOarsmenLevelForOarMachines(_ownerShip.LeftSideShipOarMachines, num4 / 2);
			LowerOarsmenLevelForOarMachines(_ownerShip.RightSideShipOarMachines, num4 - num4 / 2);
		}
		OarsmenLevel = newOarsmenLevel;
	}

	private void LowerOarsmenLevelForOarMachines(MBReadOnlyList<ShipOarMachine> oars, int numberOfOarsNeedToBeActive)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < oars.Count; i++)
		{
			ShipOarMachine shipOarMachine = oars[i];
			if (shipOarMachine.PilotStandingPoint.HasUser || shipOarMachine.PilotStandingPoint.HasAIMovingTo)
			{
				num++;
			}
			if (shipOarMachine.DestructionComponent.HitPoint <= 0f)
			{
				num2++;
			}
		}
		int num3 = oars.Count - numberOfOarsNeedToBeActive;
		int num4 = num - numberOfOarsNeedToBeActive;
		num3 -= num2;
		int num5 = 0;
		for (int j = 0; j < oars.Count; j++)
		{
			if (num3 <= 0)
			{
				break;
			}
			int index = ((j < (oars.Count + 1) / 2) ? (j * 2) : ((j - (oars.Count + 1) / 2) * 2 + 1));
			ShipOarMachine shipOarMachine2 = oars[index];
			if (num5 == numberOfOarsNeedToBeActive)
			{
				shipOarMachine2.SetIsDisabledForAI(isDisabledForAI: true);
				Agent pilotAgent = shipOarMachine2.PilotAgent;
				if (pilotAgent != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent.StopUsingGameObject();
				}
				num3--;
			}
			else if (num4 <= 0)
			{
				if (!shipOarMachine2.PilotStandingPoint.HasUser && !shipOarMachine2.PilotStandingPoint.HasAIMovingTo)
				{
					shipOarMachine2.SetIsDisabledForAI(isDisabledForAI: true);
					Agent pilotAgent2 = shipOarMachine2.PilotAgent;
					if (pilotAgent2 != null && _navalShipsLogic.IsDeploymentMode)
					{
						pilotAgent2.StopUsingGameObject();
					}
					if (shipOarMachine2.DestructionComponent.HitPoint > 0f)
					{
						num3--;
					}
				}
				else
				{
					num5++;
				}
			}
			else
			{
				if (shipOarMachine2.PilotStandingPoint.HasUser || shipOarMachine2.PilotStandingPoint.HasAIMovingTo)
				{
					num4--;
				}
				shipOarMachine2.SetIsDisabledForAI(isDisabledForAI: true);
				Agent pilotAgent3 = shipOarMachine2.PilotAgent;
				if (pilotAgent3 != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent3.StopUsingGameObject();
				}
				num3--;
			}
		}
	}

	public bool GetIsCuttingLoose()
	{
		if (_cutLooseOrderActive)
		{
			return _ownerShip.GetIsAnyBridgeActive();
		}
		return false;
	}

	public void ToggleCutLoose()
	{
		SetCutLoose(!_cutLooseOrderActive);
	}

	public void SetCutLoose(bool enable)
	{
		if (_cutLooseOrderActive == enable)
		{
			return;
		}
		if (enable)
		{
			SetBoardingTargetShip(null);
			foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
			{
				if (!attachmentMachine.IsShipAttachmentMachineBridged())
				{
					if (attachmentMachine.PilotAgent != null)
					{
						attachmentMachine.PilotAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
					else if (attachmentMachine.PilotStandingPoint.MovingAgent != null)
					{
						attachmentMachine.PilotStandingPoint.MovingAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
				}
			}
			TickDetachmentsNeeded = true;
		}
		_cutLooseOrderActive = enable;
		foreach (ShipAttachmentMachine attachmentMachine2 in _ownerShip.AttachmentMachines)
		{
			if (_cutLooseOrderActive)
			{
				if (!attachmentMachine2.IsShipAttachmentMachineBridged())
				{
					if (attachmentMachine2.PilotAgent != null)
					{
						attachmentMachine2.PilotAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
					else
					{
						attachmentMachine2.PilotStandingPoint.MovingAgent?.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
				}
				attachmentMachine2.SetIsDisabledForAI(isDisabledForAI: false);
			}
			attachmentMachine2.SetIsDisabledForAI(!enable);
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
		{
			attachmentPointMachine.SetIsDisabledForAI(!enable);
		}
		_navalShipsLogic.OnCutLooseOrder(_ownerShip);
	}

	public bool GetIsAttemptingBoarding()
	{
		if (_boardingTargetShip != null)
		{
			return !_ownerShip.SearchShipConnection(_boardingTargetShip, isDirect: true, findEnemy: true, enforceActive: false, acceptNotBridgedConnections: false);
		}
		return false;
	}

	public MissionShip GetBoardingTargetShip()
	{
		return _boardingTargetShip;
	}

	public void SetBoardingTargetShip(MissionShip missionShip)
	{
		if (_boardingTargetShip == missionShip || !IsBoardingAvailable)
		{
			return;
		}
		if (missionShip != null)
		{
			_cutLooseOrderActive = false;
			foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
			{
				if (attachmentMachine.IsShipAttachmentMachineBridged() || !attachmentMachine.CalculateCanConnectToTargetShip(missionShip))
				{
					if (attachmentMachine.PilotAgent != null)
					{
						attachmentMachine.PilotAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
					else if (attachmentMachine.PilotStandingPoint.MovingAgent != null)
					{
						attachmentMachine.PilotStandingPoint.MovingAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine.IsShipAttachmentPointBridged())
				{
					if (attachmentPointMachine.PilotAgent != null)
					{
						attachmentPointMachine.PilotAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
					else if (attachmentPointMachine.PilotStandingPoint.MovingAgent != null)
					{
						attachmentPointMachine.PilotStandingPoint.MovingAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					}
				}
			}
			TickDetachmentsNeeded = true;
			_navalShipsLogic.OnBoardingOrder(_ownerShip, missionShip);
		}
		foreach (ShipAttachmentMachine attachmentMachine2 in _ownerShip.AttachmentMachines)
		{
			if (missionShip != null)
			{
				attachmentMachine2.SetPreferredTargetShip(missionShip);
				attachmentMachine2.SetIsDisabledForAI(isDisabledForAI: false);
			}
			else
			{
				attachmentMachine2.SetPreferredTargetShip(null);
				attachmentMachine2.SetIsDisabledForAI(isDisabledForAI: true);
			}
		}
		_boardingTargetShip = missionShip;
	}

	public void SetShipStopOrder()
	{
		MovementOrderEnum = ShipMovementOrderEnum.Stop;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		SetStopShipAux();
	}

	public void SetShipMovementOrder(in Vec2 targetPosition)
	{
		MovementOrderEnum = ShipMovementOrderEnum.Move;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		SetTargetPosition(in targetPosition);
	}

	public void SetShipRetreatOrder()
	{
		MovementOrderEnum = ShipMovementOrderEnum.Retreat;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		SetTargetPosition(Mission.Current.GetClosestFleePositionForFormation(_ownerFormation).AsVec2);
	}

	public void Tick()
	{
		if (HasAIController && _ownerShip.AIController.HasTarget && !IsAIControllable)
		{
			_ownerShip.AIController.ClearTarget();
		}
		if (_ownerFormation == null || _ownerFormation.CountOfUnits <= 0)
		{
			return;
		}
		if (Mission.Current.IsDeploymentFinished)
		{
			if (_ownerShip.GetIsConnected())
			{
				switch (MovementOrderEnum)
				{
				case ShipMovementOrderEnum.Engage:
					if (!_autoSelectTargetShip && (TargetShip?.Formation == null || TargetShip.Formation.CountOfUnits <= 0 || (!TargetShip.Formation.Team.Side.IsOpponentOf(_ownerFormation.Team.Side) && !TargetShip.GetIsConnectedToEnemy())))
					{
						_autoSelectTargetShip = true;
					}
					if ((!_autoSelectTargetShip && (TargetShip == null || !_ownerShip.SearchShipConnection(TargetShip, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true)) && (_boardingTargetShip == null || !_ownerShip.SearchShipConnection(_boardingTargetShip, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))) || (_autoSelectTargetShip && !_ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true)))
					{
						SetCutLoose(enable: true);
					}
					break;
				case ShipMovementOrderEnum.Move:
				case ShipMovementOrderEnum.Retreat:
				case ShipMovementOrderEnum.StaticOrderCount:
				case ShipMovementOrderEnum.Skirmish:
					if (_boardingTargetShip == null || !_ownerShip.SearchShipConnection(_boardingTargetShip, isDirect: true, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: true))
					{
						SetCutLoose(enable: true);
					}
					break;
				}
			}
			else if (!HasStaticOrder && _orderTimer.Check(reset: true))
			{
				UpdateDynamicMovementOrder();
			}
		}
		CheckAndChangeIndependenceState();
		if (HasAIController)
		{
			DecideOarsmenLevel();
		}
		TickClimbingMachines();
		if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
		{
			if (_ownerShip.GetIsConnectedToEnemyWithSide(out var direction))
			{
				_ownerShip.ShipPlacementDetachment.SetBoarding(isBoarding: true, direction);
			}
			else
			{
				_ownerShip.ShipPlacementDetachment.SetBoarding(isBoarding: false, direction);
				_ownerShip.ShipPlacementDetachment.SetUnderMissileFire(_ownerFormation.QuerySystem.IsUnderRangedAttack);
			}
			if (_ownerShip.ShipPlacementDetachment.IsTickRequired)
			{
				_ownerShip.ShipPlacementDetachment.Tick();
			}
		}
		if (!_ownerShip.IsSinking && (TickDetachmentsNeeded || _detachmentTickTimer.Check(Mission.Current.CurrentTime)))
		{
			ManageShipDetachments();
			_detachmentTickTimer.Reset(Mission.Current.CurrentTime);
		}
		if ((_ownerFormation.IsAIControlled && _ownerFormation.IsAIOwned) || !_ownerFormation.IsPlayerTroopInFormation)
		{
			return;
		}
		switch (_ownerFormation.GetReadonlyMovementOrderReference().OrderEnum)
		{
		case MovementOrder.MovementOrderEnum.Charge:
			if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
			{
				if (_ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
				{
					_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
				}
			}
			else if (!_ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
				{
					_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
				}
				_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: true, playersOrder: false);
				_isChargeOrderOverridden = true;
			}
			break;
		case MovementOrder.MovementOrderEnum.Follow:
			if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
			{
				_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
			}
			break;
		default:
			if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
			{
				_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
			}
			else if (_isChargeOrderOverridden && _ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
			{
				SetChargeOrder(applyToPlayerFormation: true);
				_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
				_isChargeOrderOverridden = false;
			}
			break;
		}
	}

	public void SetShipSkirmishOrder(bool autoTargetClosest = true)
	{
		MissionShip closestEnemyShip = ClosestEnemyShip;
		if (closestEnemyShip != null)
		{
			MovementOrderEnum = ShipMovementOrderEnum.Skirmish;
			_autoSelectTargetShip = autoTargetClosest;
			_orderTimer.Reset();
			UpdateSkirmishOrder(closestEnemyShip);
		}
		else
		{
			SetShipStopOrder();
		}
	}

	public void SetShipFollowOrder(MissionShip shipToFollow, float offsetDistance)
	{
		MovementOrderEnum = ShipMovementOrderEnum.StaticOrderCount;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		UpdateFollowOrder(shipToFollow, new Vec2(offsetDistance, -15f));
	}

	public void SetShipMovementOrder(Vec2 targetPosition, in Vec2 targetDirection)
	{
		MovementOrderEnum = ShipMovementOrderEnum.Move;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		SetTargetState(in targetPosition, in targetDirection);
	}

	public void SetShipEngageOrder(bool autoTargetClosest = true)
	{
		MissionShip closestEnemyShip = ClosestEnemyShip;
		if (closestEnemyShip != null)
		{
			MovementOrderEnum = ShipMovementOrderEnum.Engage;
			_autoSelectTargetShip = autoTargetClosest;
			_orderTimer.Reset();
			_engageGivenTargetOrder = closestEnemyShip;
			UpdateEngageOrder(closestEnemyShip);
		}
		else
		{
			SetShipStopOrder();
		}
	}

	public void SetShipEngageOrder(MissionShip shipToEngage)
	{
		MovementOrderEnum = ShipMovementOrderEnum.Engage;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		_engageGivenTargetOrder = shipToEngage;
		UpdateEngageOrder(shipToEngage);
	}

	public void SetShipSkirmishOrder(MissionShip shipToSkirmish)
	{
		MovementOrderEnum = ShipMovementOrderEnum.Skirmish;
		_autoSelectTargetShip = false;
		_orderTimer.Reset();
		_inSkirmishPosition = false;
		UpdateSkirmishOrder(shipToSkirmish);
	}

	private void ProjectOrderPositionToBoundaries(ref Vec2 orderPosition)
	{
		Mission current = Mission.Current;
		bool flag = false;
		NavalMissionDeploymentPlanningLogic deploymentPlan;
		if (current.IsDeploymentFinished)
		{
			flag = true;
		}
		else if (_ownerShip.Team != null && current.GetDeploymentPlan<NavalMissionDeploymentPlanningLogic>(out deploymentPlan))
		{
			if (!deploymentPlan.IsPositionInsideDeploymentBoundaries(_ownerShip.Team, in orderPosition))
			{
				Vec2 closestDeploymentBoundaryPosition = deploymentPlan.GetClosestDeploymentBoundaryPosition(_ownerShip.Team, in orderPosition);
				orderPosition = closestDeploymentBoundaryPosition;
			}
		}
		else
		{
			flag = true;
		}
		if (flag && !Mission.Current.IsPositionInsideBoundaries(orderPosition))
		{
			Vec2 closestBoundaryPosition = Mission.Current.GetClosestBoundaryPosition(orderPosition);
			orderPosition = closestBoundaryPosition;
		}
	}

	private Agent GetNextAgent(ref int currentIndex)
	{
		while (currentIndex >= 0)
		{
			if (_availableUnitList[currentIndex--] is Agent { IsAIControlled: not false, IsDetachableFromFormation: not false } agent && agent.CanBeAssignedForScriptedMovement() && (agent != Agent.Main || !_ownerShip.HasPlayerStandingPointEntity))
			{
				return agent;
			}
		}
		return null;
	}

	private void UpdateStaticMovementOrder()
	{
		if (MovementOrderEnum == ShipMovementOrderEnum.Stop)
		{
			SetShipStopOrder();
		}
		else if (MovementOrderEnum == ShipMovementOrderEnum.Move)
		{
			SetShipMovementOrder(_orderGlobalPosition, in _orderGlobalDirection);
		}
		else if (MovementOrderEnum == ShipMovementOrderEnum.Retreat)
		{
			SetShipRetreatOrder();
		}
	}

	public void TickClimbingMachines()
	{
		_ownerShip.ClimbingMachineDetachment.TickClimbingMachines();
	}

	private void UpdateDynamicMovementOrder()
	{
		switch (MovementOrderEnum)
		{
		case ShipMovementOrderEnum.StaticOrderCount:
			if (TargetShip == null)
			{
				SetShipStopOrder();
			}
			else
			{
				UpdateFollowOrder(TargetShip, in _offsetPosition);
			}
			break;
		case ShipMovementOrderEnum.Engage:
			if (_autoSelectTargetShip && TargetShip == _engageGivenTargetOrder)
			{
				TrySelectBetterTargetShip();
			}
			else if (TargetShip?.Formation == null || TargetShip.Formation.CountOfUnits <= 0 || (!TargetShip.Formation.Team.Side.IsOpponentOf(_ownerFormation.Team.Side) && !TargetShip.GetIsConnectedToEnemy()))
			{
				_autoSelectTargetShip = true;
				TargetShip = ClosestEnemyShip;
			}
			if (TargetShip == null)
			{
				SetShipStopOrder();
			}
			else
			{
				UpdateEngageOrder(_engageGivenTargetOrder);
			}
			break;
		case ShipMovementOrderEnum.Skirmish:
			if (_autoSelectTargetShip)
			{
				TrySelectBetterTargetShip();
			}
			if (TargetShip == null)
			{
				SetShipStopOrder();
			}
			else
			{
				UpdateSkirmishOrder(TargetShip);
			}
			break;
		}
	}

	private void TrySelectBetterTargetShip(float decisionDistance = 4f)
	{
		if (TargetShip?.Formation == null || TargetShip.Formation.CountOfUnits <= 0 || (!TargetShip.Formation.Team.Side.IsOpponentOf(_ownerFormation.Team.Side) && !TargetShip.GetIsConnectedToEnemy()))
		{
			_engageGivenTargetOrder = ClosestEnemyShip;
			TargetShip = ClosestEnemyShip;
			return;
		}
		MissionShip closestEnemyShip = ClosestEnemyShip;
		if (closestEnemyShip != null)
		{
			Vec2 asVec = _ownerShip.GlobalFrame.origin.AsVec2;
			float num = asVec.Distance(TargetShip.GlobalFrame.origin.AsVec2);
			if (asVec.Distance(closestEnemyShip.GlobalFrame.origin.AsVec2) + decisionDistance < num)
			{
				_engageGivenTargetOrder = closestEnemyShip;
				TargetShip = closestEnemyShip;
			}
		}
	}

	private void DecideOarsmenLevel()
	{
		switch (MovementOrderEnum)
		{
		case ShipMovementOrderEnum.Engage:
			SetOrderOarsmenLevel(2);
			break;
		case ShipMovementOrderEnum.Skirmish:
			if (TargetShip == null)
			{
				break;
			}
			if (_originalOarsmenLevel != 0)
			{
				float num = TargetShip.GameEntity.GlobalPosition.DistanceSquared(_ownerShip.GameEntity.GlobalPosition);
				float num2 = 4356f;
				float num3 = 3240f;
				if (num <= num2 && num >= num3)
				{
					SetOrderOarsmenLevel(0);
				}
			}
			else
			{
				float num4 = TargetShip.GameEntity.GlobalPosition.DistanceSquared(_ownerShip.GameEntity.GlobalPosition);
				float num5 = 5184f;
				float num6 = 2304f;
				if (num4 > num5 || num4 < num6)
				{
					SetOrderOarsmenLevel(2);
				}
			}
			break;
		case ShipMovementOrderEnum.Stop:
			SetOrderOarsmenLevel(2);
			break;
		}
	}

	private void UpdateFollowOrder(MissionShip shipToFollow, in Vec2 offsetPosition)
	{
		SetMovementTargetShip(shipToFollow, in offsetPosition);
	}

	private void UpdateSkirmishOrder(MissionShip shipToSkirmish)
	{
		TargetShip = shipToSkirmish;
		if (!_ownerShip.IsAIControlled)
		{
			return;
		}
		Vec3 vec = _ownerShip.GlobalFrame.origin - shipToSkirmish.GlobalFrame.origin;
		Vec3 vec2 = vec.NormalizedCopy();
		Vec3 vec3 = shipToSkirmish.GlobalFrame.origin + vec2 * 60f;
		Vec3 vec4 = vec3 - _ownerShip.GlobalFrame.origin;
		if (vec4.Length < 3f * (_inSkirmishPosition ? 2f : 1f))
		{
			_inSkirmishPosition = true;
			_ownerShip.AIController.SetTargetState((vec2.AsVec2.LeftVec().DotProduct(_ownerShip.GlobalFrame.rotation.f.AsVec2) > 0f) ? new NavalState(_ownerShip.GlobalFrame.origin.AsVec2, vec2.AsVec2.LeftVec().Normalized()) : new NavalState(_ownerShip.GlobalFrame.origin.AsVec2, vec2.AsVec2.RightVec().Normalized()));
			return;
		}
		_inSkirmishPosition = false;
		Vec2 vec5 = -vec.AsVec2.Normalized();
		Vec2 direction = vec4.AsVec2.Normalized();
		float num = vec5.DotProduct(direction);
		Vec2 direction2 = _ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		if (num >= 0f)
		{
			if (vec4.AsVec2.Length >= 60f || vec5.DotProduct(direction2) < 0.5f)
			{
				if (vec.Length >= 120f)
				{
					_ownerShip.AIController.SetTargetState(new NavalState(vec3.AsVec2, in direction, _ownerShip.Physics.LinearVelocity.Length));
					return;
				}
				if (direction2.DotProduct(direction) < 0.6f)
				{
					_ownerShip.AIController.SetTargetState(new NavalState(vec3.AsVec2, in direction, _ownerShip.Physics.LinearVelocity.Length));
					return;
				}
				Vec2 vec6 = _ownerShip.GlobalFrame.rotation.f.AsVec2.RightVec().Normalized();
				float num2 = direction.DotProduct(vec6);
				if (TaleWorlds.Library.MathF.Abs(num2) <= 0.1f)
				{
					_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 50f + 10f * ((num2 >= 0f) ? _ownerShip.GlobalFrame.rotation.f.AsVec2.LeftVec().Normalized() : vec6), in direction2, _ownerShip.Physics.LinearVelocity.Length));
				}
				else
				{
					_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 50f, in direction2, _ownerShip.Physics.LinearVelocity.Length));
				}
			}
			else
			{
				float num3 = vec5.DotProduct(direction2);
				if (TaleWorlds.Library.MathF.Abs(num3) <= 0.8f)
				{
					_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 20f, in direction2, _ownerShip.Physics.LinearVelocity.Length));
					return;
				}
				Vec2 vec7 = _ownerShip.GlobalFrame.rotation.f.AsVec2.LeftVec().Normalized();
				_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 20f + 6.66f * (((vec5.DotProduct(vec7) > 0f) ^ (num3 > 0f)) ? vec7 : (-vec7)), in direction2, _ownerShip.Physics.LinearVelocity.Length));
			}
		}
		else
		{
			float num4 = vec5.DotProduct(direction2);
			if (TaleWorlds.Library.MathF.Abs(num4) <= 0.5f)
			{
				_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 20f, in direction2, _ownerShip.Physics.LinearVelocity.Length));
				return;
			}
			Vec2 vec8 = _ownerShip.GlobalFrame.rotation.f.AsVec2.LeftVec().Normalized();
			_ownerShip.AIController.SetTargetState(new NavalState(_ownerShip.GlobalFrame.origin.AsVec2 + direction2 * 20f + 20f * (((vec5.DotProduct(vec8) > 0f) ^ (num4 > 0f)) ? vec8 : (-vec8)), in direction2, _ownerShip.Physics.LinearVelocity.Length));
		}
	}

	private void UpdateEngageOrder(MissionShip shipToEngage)
	{
		MatrixFrame globalFrame = shipToEngage.GlobalFrame;
		bool effectiveSideOfOutermostShip = (_ownerShip.GlobalFrame.origin - globalFrame.origin).AsVec2.DotProduct(globalFrame.rotation.f.AsVec2.RightVec()) > 0f;
		if (!_ownerShip.GetIsConnected())
		{
			shipToEngage = shipToEngage.GetOutermostConnectedShipFromSide(effectiveSideOfOutermostShip, out effectiveSideOfOutermostShip, 0uL);
		}
		Vec2 positionOffset = new Vec2(effectiveSideOfOutermostShip ? 12f : (-12f), 0f);
		float directionOffset = ((Vec2.DotProduct(_ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized(), globalFrame.rotation.f.AsVec2.Normalized()) >= 0f) ? 0f : System.MathF.PI);
		SetMovementTargetShip(shipToEngage, in positionOffset, directionOffset);
	}

	private void SetStopShipAux()
	{
		SetTargetPosition(_ownerShip.GlobalFrame.origin.AsVec2);
	}

	private void SetTargetPosition(in Vec2 targetPosition, bool isForced = false)
	{
		TargetShip = null;
		_offsetPosition = Vec2.Zero;
		_offsetDirection = 0f;
		Vec2 orderGlobalDirection = _ownerShip.GlobalFrame.rotation.f.AsVec2.Normalized();
		_orderGlobalPosition = targetPosition;
		_orderGlobalDirection = orderGlobalDirection;
		if (!_lastCheckedOrderPosition.IsValid || _orderGlobalPosition.DistanceSquared(_lastCheckedOrderPosition) >= 4f)
		{
			ProjectOrderPositionToBoundaries(ref _orderGlobalPosition);
			_lastCheckedOrderPosition = _orderGlobalPosition;
		}
		if (_navalShipsLogic.IsTeleportingShips)
		{
			TryTeleportShipAux(in _orderGlobalPosition, in _orderGlobalDirection);
		}
		if (IsAIControllable || (HasAIController && isForced))
		{
			_ownerShip.AIController.SetTargetState(new NavalState(in _orderGlobalPosition, in _orderGlobalDirection), stopOnArrival: true);
		}
	}

	private void SetTargetState(in Vec2 targetPosition, in Vec2 targetDirection)
	{
		TargetShip = null;
		_offsetPosition = Vec2.Zero;
		_offsetDirection = 0f;
		_orderGlobalPosition = targetPosition;
		_orderGlobalDirection = targetDirection;
		if (!_lastCheckedOrderPosition.IsValid || _orderGlobalPosition.DistanceSquared(_lastCheckedOrderPosition) >= 4f)
		{
			ProjectOrderPositionToBoundaries(ref _orderGlobalPosition);
			_lastCheckedOrderPosition = _orderGlobalPosition;
		}
		if (_navalShipsLogic.IsTeleportingShips)
		{
			TryTeleportShipAux(in _orderGlobalPosition, in _orderGlobalDirection);
		}
		if (IsAIControllable)
		{
			_ownerShip.AIController.SetTargetState(in _orderGlobalPosition, in _orderGlobalDirection);
		}
	}

	private void SetMovementTargetShip(MissionShip targetShip, in Vec2 positionOffset, float directionOffset = 0f)
	{
		TargetShip = targetShip;
		_offsetPosition = positionOffset;
		directionOffset = MBMath.WrapAngle(directionOffset);
		_offsetDirection = directionOffset;
		MatrixFrame globalFrame = TargetShip.GlobalFrame;
		Vec2 vec = globalFrame.rotation.s.AsVec2.Normalized();
		Vec2 vec2 = globalFrame.rotation.f.AsVec2.Normalized();
		_orderGlobalPosition = globalFrame.origin.AsVec2 + _offsetPosition.X * vec + _offsetPosition.Y * vec2;
		_orderGlobalDirection = globalFrame.rotation.f.AsVec2;
		_orderGlobalDirection.RotateCCW(_offsetDirection);
		_orderGlobalDirection.Normalize();
		if (_navalShipsLogic.IsTeleportingShips)
		{
			Vec2 orderPosition = _orderGlobalPosition;
			Vec2 direction = (Mission.Current.IsDeploymentFinished ? _orderGlobalDirection : (_orderGlobalPosition - _ownerShip.GlobalFrame.origin.AsVec2).Normalized());
			if (!Mission.Current.IsDeploymentFinished)
			{
				ProjectOrderPositionToBoundaries(ref orderPosition);
			}
			TryTeleportShipAux(in orderPosition, in direction);
		}
		if (IsAIControllable)
		{
			_ownerShip.AIController.SetTargetShipWithOffset(TargetShip, new NavalVec(in _offsetPosition, _offsetDirection));
		}
	}

	public void ManageShipDetachments()
	{
		if (_ownerShip.IsShipNavmeshDisabled && _ownerFormation.Detachments.Count > 0)
		{
			StopUsingMachines(formationLeaving: false);
			return;
		}
		if (!_ownerShip.IsShipNavmeshDisabled && _ownerFormation.Detachments.Count == 0)
		{
			StartUsingMachines();
		}
		bool hasPlayerStandingPointEntity = _ownerShip.HasPlayerStandingPointEntity;
		Agent main = Agent.Main;
		if (hasPlayerStandingPointEntity && main != null && _ownerShip.IsPlayerShip)
		{
			if (main.IsUsingGameObject)
			{
				Agent.StopUsingGameObjectFlags stopUsingGameObjectFlags = Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject;
				if (main.IsAIControlled)
				{
					stopUsingGameObjectFlags |= Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject;
				}
				main.StopUsingGameObject(isSuccessful: true, stopUsingGameObjectFlags);
			}
			else if (main.IsAIControlled)
			{
				main.TryAttachToFormation();
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
		{
			if (_boardingTargetShip != null && MissionShip.AreShipsConnected(_ownerShip, _boardingTargetShip) && attachmentMachine.GetBestEnemyAttachment() == null)
			{
				if (attachmentMachine.PilotAgent != null && attachmentMachine.PilotAgent.IsAIControlled)
				{
					attachmentMachine.PilotAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
				}
				else
				{
					attachmentMachine.PilotStandingPoint.MovingAgent?.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
				}
			}
		}
		Agent captain = _ownerFormation.Captain;
		ShipControllerMachine shipControllerMachine = _ownerShip.ShipControllerMachine;
		if ((shipControllerMachine.PilotAgent == null || shipControllerMachine.PilotAgent.IsAIControlled || _navalShipsLogic.IsDeploymentMode) && captain != null && captain.IsAIControlled && !(captain == Agent.Main && hasPlayerStandingPointEntity) && (_navalShipsLogic.IsDeploymentMode || ((captain.MovementMode & AgentMovementMode.WaterDiving) == AgentMovementMode.Land && (!captain.IsDetachedFromFormation || !(captain.Detachment is ClimbingMachineDetachment)))))
		{
			if (captain.IsDetachedFromFormation && captain.CurrentlyUsedGameObject != shipControllerMachine.PilotStandingPoint && captain.HumanAIComponent.GetCurrentlyMovingGameObject() != shipControllerMachine.PilotStandingPoint)
			{
				if (captain.IsUsingGameObject)
				{
					captain.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
				}
				else
				{
					captain.TryAttachToFormation();
				}
			}
			if (shipControllerMachine.PilotAgent != null && shipControllerMachine.PilotAgent != captain)
			{
				shipControllerMachine.PilotAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
			}
			if (captain.Detachment == null && !captain.IsUsingGameObject && !shipControllerMachine.IsDisabledForAI)
			{
				shipControllerMachine.AddAgentAtSlotIndex(captain, shipControllerMachine.PilotStandingPointSlotIndex);
			}
		}
		if (_ownerFormation.CountOfDetachableNonPlayerUnits > 0)
		{
			_ownerFormation.Arrangement.GetAllUnits(in _availableUnitList);
			int currentIndex = _availableUnitList.Count - 1;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			if (_boardingTargetShip != null)
			{
				while (currentIndex >= 0 && num < _ownerShip.AttachmentMachines.Count)
				{
					ShipAttachmentMachine shipAttachmentMachine = _ownerShip.AttachmentMachines[num++];
					if (shipAttachmentMachine.PilotAgent == null && !shipAttachmentMachine.PilotStandingPoint.HasAIMovingTo && shipAttachmentMachine.CurrentAttachment == null && !shipAttachmentMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && shipAttachmentMachine.CalculateCanConnectToTargetShip(_boardingTargetShip) && (!MissionShip.AreShipsConnected(_ownerShip, _boardingTargetShip) || shipAttachmentMachine.GetBestEnemyAttachment() != null))
					{
						Agent nextAgent = GetNextAgent(ref currentIndex);
						if (nextAgent == null)
						{
							break;
						}
						shipAttachmentMachine.AddAgentAtSlotIndex(nextAgent, shipAttachmentMachine.PilotStandingPointSlotIndex);
					}
				}
			}
			else if (_cutLooseOrderActive)
			{
				while (currentIndex >= 0 && num < _ownerShip.AttachmentMachines.Count)
				{
					ShipAttachmentMachine shipAttachmentMachine2 = _ownerShip.AttachmentMachines[num++];
					if (shipAttachmentMachine2.IsShipAttachmentMachineBridged() && !shipAttachmentMachine2.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && shipAttachmentMachine2.PilotAgent == null && !shipAttachmentMachine2.PilotStandingPoint.HasAIMovingTo)
					{
						Agent nextAgent = GetNextAgent(ref currentIndex);
						if (nextAgent == null)
						{
							break;
						}
						shipAttachmentMachine2.AddAgentAtSlotIndex(nextAgent, shipAttachmentMachine2.PilotStandingPointSlotIndex);
					}
				}
				while (currentIndex >= 0 && num2 < _ownerShip.AttachmentPointMachines.Count)
				{
					ShipAttachmentPointMachine shipAttachmentPointMachine = _ownerShip.AttachmentPointMachines[num2++];
					if (shipAttachmentPointMachine.IsShipAttachmentPointBridged() && !shipAttachmentPointMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && shipAttachmentPointMachine.PilotAgent == null && !shipAttachmentPointMachine.PilotStandingPoint.HasAIMovingTo)
					{
						Agent nextAgent = GetNextAgent(ref currentIndex);
						if (nextAgent == null)
						{
							break;
						}
						shipAttachmentPointMachine.AddAgentAtSlotIndex(nextAgent, shipAttachmentPointMachine.PilotStandingPointSlotIndex);
					}
				}
			}
			if (_ownerShip.ShipSiegeWeapon != null)
			{
				RangedSiegeWeapon shipSiegeWeapon = _ownerShip.ShipSiegeWeapon;
				if (shipSiegeWeapon.PilotAgent == null && !shipSiegeWeapon.PilotStandingPoint.HasAIMovingTo && !shipSiegeWeapon.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
				{
					Agent nextAgent = GetNextAgent(ref currentIndex);
					if (nextAgent != null)
					{
						shipSiegeWeapon.AddAgentAtSlotIndex(nextAgent, shipSiegeWeapon.PilotStandingPointSlotIndex);
					}
				}
			}
			if (_ownerShip.ShipControllerMachine.PilotAgent == null && !_ownerShip.ShipControllerMachine.PilotStandingPoint.HasAIMovingTo && !_ownerShip.ShipControllerMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && (!_ownerShip.IsPlayerShip || Mission.Current.MainAgent == null || (_ownerFormation != null && _ownerFormation.IsAIControlled)))
			{
				Agent nextAgent = GetNextAgent(ref currentIndex);
				if (nextAgent != null)
				{
					_ownerShip.ShipControllerMachine.AddAgentAtSlotIndex(nextAgent, _ownerShip.ShipControllerMachine.PilotStandingPointSlotIndex);
				}
			}
			while (currentIndex >= 0 && (num3 < _ownerShip.LeftSideShipOarMachines.Count || num3 < _ownerShip.RightSideShipOarMachines.Count))
			{
				if (num3 < _ownerShip.LeftSideShipOarMachines.Count)
				{
					ShipOarMachine shipOarMachine = _ownerShip.LeftSideShipOarMachines[num3];
					if (shipOarMachine.PilotAgent == null && !shipOarMachine.PilotStandingPoint.HasAIMovingTo && !shipOarMachine.PilotStandingPoint.IsDeactivated && !shipOarMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
					{
						Agent nextAgent = GetNextAgent(ref currentIndex);
						if (nextAgent == null)
						{
							break;
						}
						shipOarMachine.AddAgentAtSlotIndex(nextAgent, shipOarMachine.PilotStandingPointSlotIndex);
					}
				}
				if (num3 < _ownerShip.RightSideShipOarMachines.Count)
				{
					ShipOarMachine shipOarMachine2 = _ownerShip.RightSideShipOarMachines[num3];
					if (shipOarMachine2.PilotAgent == null && !shipOarMachine2.PilotStandingPoint.HasAIMovingTo && !shipOarMachine2.PilotStandingPoint.IsDeactivated && !shipOarMachine2.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
					{
						Agent nextAgent = GetNextAgent(ref currentIndex);
						if (nextAgent == null)
						{
							break;
						}
						shipOarMachine2.AddAgentAtSlotIndex(nextAgent, shipOarMachine2.PilotStandingPointSlotIndex);
					}
				}
				num3++;
			}
			if (_ownerShip.ShipPlacementDetachment == null || !_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
			{
				return;
			}
			while (currentIndex >= 0 && _ownerShip.ShipPlacementDetachment.HasAvailableSlots)
			{
				Agent nextAgent = GetNextAgent(ref currentIndex);
				if (nextAgent != null)
				{
					if (_navalShipsLogic.IsDeploymentMode || (nextAgent.MovementMode & AgentMovementMode.WaterDiving) == AgentMovementMode.Land)
					{
						_ownerShip.ShipPlacementDetachment.AddAgent(nextAgent);
					}
					continue;
				}
				break;
			}
			return;
		}
		ShipDetachmentPriority shipDetachmentPriority = ShipDetachmentPriority.ConnectionMachine;
		IDetachment detachment = null;
		bool flag = false;
		if (_cutLooseOrderActive)
		{
			foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine.IsShipAttachmentPointBridged() && !attachmentPointMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && attachmentPointMachine.PilotAgent == null && !attachmentPointMachine.PilotStandingPoint.HasAIMovingTo)
				{
					detachment = attachmentPointMachine;
					break;
				}
			}
			if (detachment != null)
			{
				goto IL_0aea;
			}
		}
		if (_cutLooseOrderActive || _boardingTargetShip != null)
		{
			foreach (ShipAttachmentMachine attachmentMachine2 in _ownerShip.AttachmentMachines)
			{
				if (((_cutLooseOrderActive && attachmentMachine2.IsShipAttachmentMachineBridged()) || (_boardingTargetShip != null && attachmentMachine2.CurrentAttachment == null && attachmentMachine2.CalculateCanConnectToTargetShip(_boardingTargetShip) && (!MissionShip.AreShipsConnected(_ownerShip, _boardingTargetShip) || attachmentMachine2.GetBestEnemyAttachment() != null))) && !attachmentMachine2.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && attachmentMachine2.PilotAgent == null && !attachmentMachine2.PilotStandingPoint.HasAIMovingTo)
				{
					detachment = attachmentMachine2;
					break;
				}
			}
			if (detachment != null)
			{
				goto IL_0aea;
			}
		}
		shipDetachmentPriority--;
		if (_ownerShip.ShipSiegeWeapon != null)
		{
			RangedSiegeWeapon shipSiegeWeapon2 = _ownerShip.ShipSiegeWeapon;
			if (shipSiegeWeapon2.PilotAgent == null && !shipSiegeWeapon2.PilotStandingPoint.HasAIMovingTo && !shipSiegeWeapon2.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
			{
				detachment = shipSiegeWeapon2;
				goto IL_0aea;
			}
		}
		if (detachment == null)
		{
			shipDetachmentPriority--;
			if (_ownerShip.ShipControllerMachine.PilotAgent == null && !_ownerShip.ShipControllerMachine.PilotStandingPoint.HasAIMovingTo && !_ownerShip.ShipControllerMachine.IsDisabledForBattleSideAI(_ownerFormation.Team.Side) && (!_ownerShip.IsPlayerShip || Mission.Current.MainAgent == null || (_ownerFormation != null && _ownerFormation.IsAIControlled)))
			{
				detachment = _ownerShip.ShipControllerMachine;
			}
			else
			{
				shipDetachmentPriority--;
				for (int i = 0; i < _ownerShip.LeftSideShipOarMachines.Count || i < _ownerShip.RightSideShipOarMachines.Count; i++)
				{
					if (i < _ownerShip.LeftSideShipOarMachines.Count)
					{
						ShipOarMachine shipOarMachine3 = _ownerShip.LeftSideShipOarMachines[i];
						if (shipOarMachine3.PilotAgent == null && !shipOarMachine3.PilotStandingPoint.HasAIMovingTo && !shipOarMachine3.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
						{
							detachment = shipOarMachine3;
							break;
						}
					}
					if (i < _ownerShip.RightSideShipOarMachines.Count)
					{
						ShipOarMachine shipOarMachine4 = _ownerShip.RightSideShipOarMachines[i];
						if (shipOarMachine4.PilotAgent == null && !shipOarMachine4.PilotStandingPoint.HasAIMovingTo && !shipOarMachine4.IsDisabledForBattleSideAI(_ownerFormation.Team.Side))
						{
							detachment = shipOarMachine4;
							break;
						}
					}
				}
				if (detachment == null)
				{
					shipDetachmentPriority--;
				}
			}
		}
		goto IL_0aea;
		IL_0aea:
		if (shipDetachmentPriority > ShipDetachmentPriority.PlacementDetachment)
		{
			int slotIndex = ((detachment is UsableMachine usableMachine) ? usableMachine.PilotStandingPointSlotIndex : 0);
			if (_ownerShip.ShipPlacementDetachment.HasAgent)
			{
				Agent agent = _ownerShip.ShipPlacementDetachment.PickLastAgent();
				detachment.AddAgentAtSlotIndex(agent, slotIndex);
				return;
			}
			if (shipDetachmentPriority > ShipDetachmentPriority.Oar)
			{
				for (int j = 0; j < _ownerShip.LeftSideShipOarMachines.Count || j < _ownerShip.RightSideShipOarMachines.Count; j++)
				{
					if (j < _ownerShip.LeftSideShipOarMachines.Count)
					{
						ShipOarMachine shipOarMachine5 = _ownerShip.LeftSideShipOarMachines[j];
						if (shipOarMachine5.PilotAgent != null && shipOarMachine5.PilotAgent.IsAIControlled)
						{
							Agent pilotAgent = shipOarMachine5.PilotAgent;
							pilotAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
							detachment.AddAgentAtSlotIndex(pilotAgent, slotIndex);
							flag = true;
							break;
						}
					}
					if (j < _ownerShip.RightSideShipOarMachines.Count)
					{
						ShipOarMachine shipOarMachine6 = _ownerShip.RightSideShipOarMachines[j];
						if (shipOarMachine6.PilotAgent != null && shipOarMachine6.PilotAgent.IsAIControlled)
						{
							Agent pilotAgent2 = shipOarMachine6.PilotAgent;
							pilotAgent2.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
							detachment.AddAgentAtSlotIndex(pilotAgent2, slotIndex);
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					return;
				}
				if (shipDetachmentPriority > ShipDetachmentPriority.ControllerMachine && _ownerShip.ShipControllerMachine.PilotAgent != null && _ownerShip.ShipControllerMachine.PilotAgent.IsAIControlled)
				{
					Agent pilotAgent3 = _ownerShip.ShipControllerMachine.PilotAgent;
					pilotAgent3.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
					detachment.AddAgentAtSlotIndex(pilotAgent3, slotIndex);
					return;
				}
				if (shipDetachmentPriority > ShipDetachmentPriority.SiegeWeapon)
				{
					RangedSiegeWeapon shipSiegeWeapon3 = _ownerShip.ShipSiegeWeapon;
					if (shipSiegeWeapon3?.PilotAgent != null && shipSiegeWeapon3.PilotAgent.IsAIControlled)
					{
						Agent pilotAgent4 = shipSiegeWeapon3.PilotAgent;
						pilotAgent4.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
						detachment.AddAgentAtSlotIndex(pilotAgent4, slotIndex);
						return;
					}
				}
			}
			TickDetachmentsNeeded = false;
			_detachmentTickTimer.Reset(Mission.Current.CurrentTime);
		}
		else
		{
			TickDetachmentsNeeded = false;
			_detachmentTickTimer.Reset(Mission.Current.CurrentTime);
		}
	}

	private void TryTeleportShipAux(in Vec2 position, in Vec2 direction)
	{
		MatrixFrame globalFrame = _ownerShip.GlobalFrame;
		if (position.DistanceSquared(globalFrame.origin.AsVec2) >= 0.01f || direction.AngleBetween(globalFrame.rotation.f.AsVec2.Normalized()) >= 0.1f)
		{
			Vec2 vec = position;
			Vec2 vec2 = (direction.IsValid ? direction : _ownerShip.GameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized());
			Vec3 origin = vec.ToVec3();
			Vec3 f = vec2.ToVec3().NormalizedCopy();
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation.f = f;
			identity.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			identity.origin = origin;
			bool anchorShip = _ownerShip.Physics != null && _ownerShip.Physics.IsAnchored;
			_navalShipsLogic.TeleportShip(_ownerShip, identity, checkFreeArea: true, anchorShip);
		}
	}

	private void SetChargeOrder(bool applyToPlayerFormation)
	{
		if (applyToPlayerFormation || _ownerFormation.PlayerOwner != Mission.Current.MainAgent || !_ownerFormation.HasPlayerControlledTroop)
		{
			_ownerFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
		}
	}

	public void JoinPlayerFormationToPlacementDetachment(bool isPlayersOrder)
	{
		if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
		{
			_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
		}
		if (isPlayersOrder)
		{
			_isChargeOrderOverridden = false;
		}
	}

	internal void RefreshOrders()
	{
		if (!HasAIController)
		{
			SetShipStopOrder();
			return;
		}
		if (HasStaticOrder)
		{
			UpdateStaticMovementOrder();
			return;
		}
		_orderTimer.Reset();
		UpdateDynamicMovementOrder();
	}

	internal void OnOwnerShipRemoved()
	{
		_navalShipsLogic.ShipControllerChanged -= OnShipControllerChanged;
		_navalShipsLogic.ShipRemovedEvent -= OnShipRemoved;
	}

	private void CheckAndChangeIndependenceState()
	{
		MissionShip boardingTargetShip = _boardingTargetShip;
		bool flag = boardingTargetShip != null && boardingTargetShip.AnyActiveFormationTroopOnShip && MissionShip.AreShipsConnected(_ownerShip, _boardingTargetShip);
		bool flag2 = flag || _isEnemyOnShip.Value;
		if (!flag2)
		{
			foreach (ShipAttachmentMachine attachmentMachine in _ownerShip.AttachmentMachines)
			{
				if (attachmentMachine.IsShipAttachmentMachineBridged())
				{
					flag2 = true;
					flag = true;
					break;
				}
				if (!flag2 && ShipAttachmentMachine.DoesShipAttachmentMachineSatisfyOarsmenGetUpCondition(attachmentMachine.CurrentAttachment))
				{
					flag2 = true;
				}
				if (attachmentMachine.IsShipAttachmentMachineConnectedToEnemy())
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (ShipAttachmentPointMachine attachmentPointMachine in _ownerShip.AttachmentPointMachines)
				{
					if (attachmentPointMachine.IsShipAttachmentPointBridged())
					{
						flag2 = true;
						flag = true;
						break;
					}
					if (!flag2 && ShipAttachmentMachine.DoesShipAttachmentMachineSatisfyOarsmenGetUpCondition(attachmentPointMachine.CurrentAttachment))
					{
						flag2 = true;
					}
					if (attachmentPointMachine.IsShipAttachmentPointConnectedToEnemy())
					{
						flag = true;
						break;
					}
				}
			}
		}
		switch (_shipIndependenceState)
		{
		case ShipIndependenceState.Independent:
			if (flag || _isEnemyOnShip.Value)
			{
				if (flag2)
				{
					_oarLevelOverridden = true;
					_originalOarsmenLevel = OarsmenLevel;
					SetOarsmenLevel(0);
				}
				_ownerShip.ShipControllerMachine.SetIsDisabledForAI(isDisabledForAI: true);
				Agent pilotAgent2 = _ownerShip.ShipControllerMachine.PilotAgent;
				if (pilotAgent2 != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent2.StopUsingGameObject();
				}
				_shipIndependenceState = ShipIndependenceState.Connected;
			}
			if (!_isEnemyOnShip.Value)
			{
				break;
			}
			foreach (ShipAttachmentMachine attachmentMachine2 in _ownerShip.AttachmentMachines)
			{
				attachmentMachine2.SetIsDisabledForAI(isDisabledForAI: true);
				Agent pilotAgent3 = attachmentMachine2.PilotAgent;
				if (pilotAgent3 != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent3.StopUsingGameObject();
				}
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine2 in _ownerShip.AttachmentPointMachines)
			{
				attachmentPointMachine2.SetIsDisabledForAI(isDisabledForAI: true);
				Agent pilotAgent4 = attachmentPointMachine2.PilotAgent;
				if (pilotAgent4 != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent4.StopUsingGameObject();
				}
			}
			_shipIndependenceState = ShipIndependenceState.EnemyOnShip;
			break;
		case ShipIndependenceState.Connected:
			if (_isEnemyOnShip.Value)
			{
				Agent pilotAgent;
				foreach (ShipAttachmentMachine attachmentMachine3 in _ownerShip.AttachmentMachines)
				{
					attachmentMachine3.SetIsDisabledForAI(isDisabledForAI: true);
					pilotAgent = attachmentMachine3.PilotAgent;
					if (pilotAgent != null && _navalShipsLogic.IsDeploymentMode)
					{
						pilotAgent.StopUsingGameObject();
					}
				}
				foreach (ShipAttachmentPointMachine attachmentPointMachine3 in _ownerShip.AttachmentPointMachines)
				{
					attachmentPointMachine3.SetIsDisabledForAI(isDisabledForAI: true);
					pilotAgent = attachmentPointMachine3.PilotAgent;
					if (pilotAgent != null && _navalShipsLogic.IsDeploymentMode)
					{
						pilotAgent.StopUsingGameObject();
					}
				}
				_ownerShip.ShipControllerMachine.SetIsDisabledForAI(isDisabledForAI: true);
				pilotAgent = _ownerShip.ShipControllerMachine.PilotAgent;
				if (pilotAgent != null && _navalShipsLogic.IsDeploymentMode)
				{
					pilotAgent.StopUsingGameObject();
				}
				_shipIndependenceState = ShipIndependenceState.EnemyOnShip;
				SetChargeOrder(applyToPlayerFormation: false);
			}
			else if (!flag)
			{
				_shipIndependenceState = ShipIndependenceState.Independent;
				SetOarsmenLevel(_originalOarsmenLevel);
				_oarLevelOverridden = false;
				_ownerShip.ShipControllerMachine.SetIsDisabledForAI(isDisabledForAI: false);
			}
			else if (!_oarLevelOverridden && flag2)
			{
				_oarLevelOverridden = true;
				_originalOarsmenLevel = OarsmenLevel;
				SetOarsmenLevel(0);
			}
			break;
		case ShipIndependenceState.EnemyOnShip:
			if (_isEnemyOnShip.Value)
			{
				break;
			}
			if (_cutLooseOrderActive)
			{
				foreach (ShipAttachmentPointMachine attachmentPointMachine4 in _ownerShip.AttachmentPointMachines)
				{
					attachmentPointMachine4.SetIsDisabledForAI(isDisabledForAI: false);
				}
			}
			if (_cutLooseOrderActive || _boardingTargetShip != null)
			{
				foreach (ShipAttachmentMachine attachmentMachine4 in _ownerShip.AttachmentMachines)
				{
					attachmentMachine4.SetIsDisabledForAI(isDisabledForAI: false);
				}
			}
			_shipIndependenceState = ShipIndependenceState.Connected;
			if (!flag)
			{
				_shipIndependenceState = ShipIndependenceState.Independent;
				SetOarsmenLevel(_originalOarsmenLevel);
				_oarLevelOverridden = false;
				_ownerShip.ShipControllerMachine.SetIsDisabledForAI(isDisabledForAI: false);
			}
			break;
		}
		switch (_shipIndependenceState)
		{
		case ShipIndependenceState.Independent:
			if ((_ownerFormation.IsAIControlled || _ownerFormation.IsAIOwned || !_ownerFormation.HasPlayerControlledTroop) && !_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
			{
				_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
			}
			_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: false, playersOrder: false);
			break;
		case ShipIndependenceState.Connected:
			if (_ownerFormation.IsAIControlled)
			{
				if (_boardingTargetShip != null && MissionShip.AreShipsConnected(_boardingTargetShip, _ownerShip) && _boardingTargetShip.Formation != null && _ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
				{
					if ((_ownerFormation.IsAIControlled || _ownerFormation.IsAIOwned || !_ownerFormation.HasPlayerControlledTroop) && _ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
					{
						_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
					}
					SetChargeOrder(applyToPlayerFormation: false);
				}
				else
				{
					if ((_ownerFormation.IsAIControlled || _ownerFormation.IsAIOwned || !_ownerFormation.HasPlayerControlledTroop) && !_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
					{
						_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
					}
					_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: false, playersOrder: false);
				}
			}
			else
			{
				if (_ownerFormation.HasPlayerControlledTroop)
				{
					break;
				}
				switch (MovementOrderEnum)
				{
				case ShipMovementOrderEnum.Engage:
					if (!_autoSelectTargetShip)
					{
						if (MissionShip.AreShipsConnected(_ownerShip, TargetShip) && _ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
						{
							if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
							{
								_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
							}
							SetChargeOrder(applyToPlayerFormation: false);
						}
						else
						{
							if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
							{
								_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
							}
							_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: false, playersOrder: false);
						}
					}
					else if (_ownerShip.SearchShipConnection(null, isDirect: true, findEnemy: true, enforceActive: true, acceptNotBridgedConnections: true))
					{
						if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
						{
							_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
						}
						SetChargeOrder(applyToPlayerFormation: false);
					}
					else
					{
						if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
						{
							_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
						}
						_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: false, playersOrder: false);
					}
					break;
				case ShipMovementOrderEnum.Move:
				case ShipMovementOrderEnum.Retreat:
				case ShipMovementOrderEnum.StaticOrderCount:
				case ShipMovementOrderEnum.Skirmish:
					if (_boardingTargetShip == null || !MissionShip.AreShipsConnected(_ownerShip, _boardingTargetShip) || !_boardingTargetShip.AnyActiveFormationTroopOnShip)
					{
						if (!_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation) && !_ownerShip.IsShipNavmeshDisabled)
						{
							_ownerFormation.JoinDetachment(_ownerShip.ShipPlacementDetachment);
						}
						_ownerShip.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: false, playersOrder: false);
					}
					else
					{
						if (_ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
						{
							_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
						}
						SetChargeOrder(applyToPlayerFormation: false);
					}
					break;
				}
			}
			break;
		case ShipIndependenceState.EnemyOnShip:
			if ((_ownerFormation.IsAIControlled || _ownerFormation.IsAIOwned || !_ownerFormation.HasPlayerControlledTroop) && _ownerShip.ShipPlacementDetachment.IsUsedByFormation(_ownerFormation))
			{
				_ownerFormation.LeaveDetachment(_ownerShip.ShipPlacementDetachment);
			}
			SetChargeOrder(applyToPlayerFormation: false);
			break;
		}
	}

	private void OnShipControllerChanged(MissionShip ship)
	{
		if (_ownerShip == ship)
		{
			RefreshOrders();
			if (ship.IsPlayerShip && ship.IsAIControlled)
			{
				ManageShipDetachments();
			}
		}
	}

	private void OnShipRemoved(MissionShip ship)
	{
		if (ship != _ownerShip && TargetShip == ship)
		{
			TargetShip = null;
		}
		if (_boardingTargetShip == ship)
		{
			_boardingTargetShip = null;
		}
	}
}
