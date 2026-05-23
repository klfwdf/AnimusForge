using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class OrderController
{
	public const float FormationGapInLine = 1.5f;

	protected readonly Mission _mission;

	public readonly Team Team;

	public Agent Owner;

	protected readonly MBList<Formation> _selectedFormations;

	private Dictionary<Formation, float> actualWidths;

	private Dictionary<Formation, int> actualUnitSpacings;

	private List<Func<Formation, MovementOrder, MovementOrder>> orderOverrides;

	private List<(Formation, OrderType)> overridenOrders;

	protected bool _formationUpdateEnabledAfterSetOrder = true;

	private bool _gesturesEnabled;

	private MissionTimer _yellingAfterChargeOrderTimer;

	public SiegeWeaponController SiegeWeaponController { get; private set; }

	public MBReadOnlyList<Formation> SelectedFormations => _selectedFormations;

	public bool FormationUpdateEnabledAfterSetOrder => _formationUpdateEnabledAfterSetOrder;

	public Dictionary<Formation, Formation> simulationFormations { get; private set; }

	public event OnOrderIssuedDelegate OnOrderIssued;

	public event Action OnSelectedFormationsChanged;

	public OrderController(Mission mission, Team team, Agent owner)
	{
		_mission = mission;
		Team = team;
		Owner = owner;
		_gesturesEnabled = true;
		_selectedFormations = new MBList<Formation>();
		SiegeWeaponController = new SiegeWeaponController(mission, Team);
		simulationFormations = new Dictionary<Formation, Formation>();
		actualWidths = new Dictionary<Formation, float>();
		actualUnitSpacings = new Dictionary<Formation, int>();
		_yellingAfterChargeOrderTimer = new MissionTimer(30f);
		_yellingAfterChargeOrderTimer.Set(-30f);
		foreach (Formation item in Team.FormationsIncludingEmpty)
		{
			item.OnWidthChanged += Formation_OnWidthChanged;
			item.OnUnitSpacingChanged += Formation_OnUnitSpacingChanged;
		}
		if (Team.IsPlayerGeneral)
		{
			foreach (Formation item2 in Team.FormationsIncludingEmpty)
			{
				item2.PlayerOwner = owner;
			}
		}
		CreateDefaultOrderOverrides();
	}

	internal void AssignDelegatesToController(OrderController newController)
	{
		newController.OnOrderIssued = this.OnOrderIssued;
		newController.OnSelectedFormationsChanged = this.OnSelectedFormationsChanged;
	}

	private void Formation_OnUnitSpacingChanged(Formation formation)
	{
		actualUnitSpacings.Remove(formation);
	}

	private void Formation_OnWidthChanged(Formation formation)
	{
		actualWidths.Remove(formation);
	}

	protected void OnSelectedFormationsCollectionChanged()
	{
		this.OnSelectedFormationsChanged?.Invoke();
		foreach (Formation item in SelectedFormations.Except(simulationFormations.Keys))
		{
			simulationFormations[item] = new Formation(null, -1);
		}
	}

	protected virtual void SelectFormation(Formation formation, Agent selectorAgent)
	{
		if (!_selectedFormations.Contains(formation) && IsFormationSelectable(formation, selectorAgent))
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new SelectFormation(formation.Index));
				GameNetwork.EndModuleEventAsClient();
			}
			if (selectorAgent != null && AreGesturesEnabled())
			{
				PlayFormationSelectedGesture(formation, selectorAgent);
			}
			MBDebug.Print(string.Concat(formation?.RepresentativeClass, " added to selected formations."));
			_selectedFormations.Add(formation);
			OnSelectedFormationsCollectionChanged();
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Formation already selected or is not selectable", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SelectFormation", 194);
		}
	}

	public void SelectFormation(Formation formation)
	{
		SelectFormation(formation, Owner);
	}

	public void DeselectFormation(Formation formation)
	{
		if (_selectedFormations.Contains(formation))
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new UnselectFormation(formation.Index));
				GameNetwork.EndModuleEventAsClient();
			}
			MBDebug.Print(string.Concat(formation?.RepresentativeClass, " is removed from selected formations."));
			_selectedFormations.Remove(formation);
			OnSelectedFormationsCollectionChanged();
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Trying to deselect an unselected formation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "DeselectFormation", 220);
		}
	}

	public bool IsFormationListening(Formation formation)
	{
		return SelectedFormations.Contains(formation);
	}

	public bool IsFormationSelectable(Formation formation)
	{
		return IsFormationSelectable(formation, Owner);
	}

	public bool BackupAndDisableGesturesEnabled()
	{
		bool gesturesEnabled = _gesturesEnabled;
		_gesturesEnabled = false;
		return gesturesEnabled;
	}

	public void RestoreGesturesEnabled(bool oldValue)
	{
		_gesturesEnabled = oldValue;
	}

	protected bool IsFormationSelectable(Formation formation, Agent selectorAgent)
	{
		if (selectorAgent == null || formation.PlayerOwner == selectorAgent)
		{
			return formation.GetCountOfUnitsWithCondition((Agent agent) => agent.Health > 0f) > 0;
		}
		return false;
	}

	protected bool AreGesturesEnabled()
	{
		if (_gesturesEnabled && _mission.IsOrderGesturesEnabled())
		{
			return !GameNetwork.IsClientOrReplay;
		}
		return false;
	}

	protected virtual void SelectAllFormations(Agent selectorAgent, bool uiFeedback)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SelectAllFormations());
			GameNetwork.EndModuleEventAsClient();
		}
		if (uiFeedback && selectorAgent != null && AreGesturesEnabled())
		{
			selectorAgent.MakeVoice(SkinVoiceManager.VoiceType.Everyone, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
		}
		MBDebug.Print("Selected formations being cleared. Select all formations:");
		_selectedFormations.Clear();
		foreach (Formation item in Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && IsFormationSelectable(f, selectorAgent)))
		{
			MBDebug.Print(string.Concat(item.RepresentativeClass, " added to selected formations."));
			_selectedFormations.Add(item);
		}
		OnSelectedFormationsCollectionChanged();
	}

	public void SelectAllFormations(bool uiFeedback = false)
	{
		SelectAllFormations(Owner, uiFeedback);
	}

	public void ClearSelectedFormations()
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ClearSelectedFormations());
			GameNetwork.EndModuleEventAsClient();
		}
		MBDebug.Print("Selected formations being cleared.");
		_selectedFormations.Clear();
		OnSelectedFormationsCollectionChanged();
	}

	public virtual void SetOrder(OrderType orderType)
	{
		MBDebug.Print(string.Concat("SetOrder ", orderType, "on team"));
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrder(orderType));
			GameNetwork.EndModuleEventAsClient();
		}
		switch (orderType)
		{
		case OrderType.Charge:
			foreach (Formation selectedFormation in SelectedFormations)
			{
				selectedFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
			}
			break;
		case OrderType.StandYourGround:
			foreach (Formation selectedFormation2 in SelectedFormations)
			{
				selectedFormation2.SetMovementOrder(MovementOrder.MovementOrderStop);
			}
			break;
		case OrderType.AdvanceTenPaces:
			foreach (Formation selectedFormation3 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation3);
				if (selectedFormation3.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Move)
				{
					MovementOrder readonlyMovementOrderReference2 = selectedFormation3.GetReadonlyMovementOrderReference();
					readonlyMovementOrderReference2.Advance(selectedFormation3, 7f);
					selectedFormation3.SetMovementOrder(readonlyMovementOrderReference2);
				}
			}
			break;
		case OrderType.FallBackTenPaces:
			foreach (Formation selectedFormation4 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation4);
				if (selectedFormation4.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Move)
				{
					MovementOrder readonlyMovementOrderReference = selectedFormation4.GetReadonlyMovementOrderReference();
					readonlyMovementOrderReference.FallBack(selectedFormation4, 7f);
					selectedFormation4.SetMovementOrder(readonlyMovementOrderReference);
				}
			}
			break;
		case OrderType.Advance:
			foreach (Formation selectedFormation5 in SelectedFormations)
			{
				selectedFormation5.SetMovementOrder(MovementOrder.MovementOrderAdvance);
			}
			break;
		case OrderType.FallBack:
			foreach (Formation selectedFormation6 in SelectedFormations)
			{
				selectedFormation6.SetMovementOrder(MovementOrder.MovementOrderFallBack);
			}
			break;
		case OrderType.Retreat:
			foreach (Formation selectedFormation7 in SelectedFormations)
			{
				selectedFormation7.SetMovementOrder(MovementOrder.MovementOrderRetreat);
			}
			break;
		case OrderType.LookAtEnemy:
		{
			FacingOrder facingOrderLookAtEnemy = FacingOrder.FacingOrderLookAtEnemy;
			foreach (Formation selectedFormation8 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation8);
				selectedFormation8.SetFacingOrder(facingOrderLookAtEnemy);
			}
			break;
		}
		case OrderType.HoldFire:
			foreach (Formation selectedFormation9 in SelectedFormations)
			{
				selectedFormation9.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
			}
			break;
		case OrderType.FireAtWill:
			foreach (Formation selectedFormation10 in SelectedFormations)
			{
				selectedFormation10.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
			}
			break;
		case OrderType.Dismount:
			foreach (Formation selectedFormation11 in SelectedFormations)
			{
				if (selectedFormation11.PhysicalClass.IsMounted() || selectedFormation11.HasAnyMountedUnit)
				{
					TryCancelStopOrder(selectedFormation11);
				}
				selectedFormation11.SetRidingOrder(RidingOrder.RidingOrderDismount);
			}
			break;
		case OrderType.Mount:
			foreach (Formation selectedFormation12 in SelectedFormations)
			{
				if (selectedFormation12.PhysicalClass.IsMounted() || selectedFormation12.HasAnyMountedUnit)
				{
					TryCancelStopOrder(selectedFormation12);
				}
				selectedFormation12.SetRidingOrder(RidingOrder.RidingOrderMount);
			}
			break;
		case OrderType.AIControlOn:
			foreach (Formation selectedFormation13 in SelectedFormations)
			{
				selectedFormation13.SetControlledByAI(isControlledByAI: true);
			}
			break;
		case OrderType.AIControlOff:
			foreach (Formation selectedFormation14 in SelectedFormations)
			{
				selectedFormation14.SetControlledByAI(isControlledByAI: false);
			}
			break;
		case OrderType.ArrangementLine:
			foreach (Formation selectedFormation15 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation15);
				selectedFormation15.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			}
			break;
		case OrderType.ArrangementCloseOrder:
			foreach (Formation selectedFormation16 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation16);
				selectedFormation16.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
			}
			break;
		case OrderType.ArrangementLoose:
			foreach (Formation selectedFormation17 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation17);
				selectedFormation17.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			}
			break;
		case OrderType.ArrangementCircular:
			foreach (Formation selectedFormation18 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation18);
				selectedFormation18.SetArrangementOrder(ArrangementOrder.ArrangementOrderCircle);
			}
			break;
		case OrderType.ArrangementSchiltron:
			foreach (Formation selectedFormation19 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation19);
				selectedFormation19.SetArrangementOrder(ArrangementOrder.ArrangementOrderSquare);
			}
			break;
		case OrderType.ArrangementVee:
			foreach (Formation selectedFormation20 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation20);
				selectedFormation20.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
			}
			break;
		case OrderType.ArrangementColumn:
			foreach (Formation selectedFormation21 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation21);
				selectedFormation21.SetArrangementOrder(ArrangementOrder.ArrangementOrderColumn);
			}
			break;
		case OrderType.ArrangementScatter:
			foreach (Formation selectedFormation22 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation22);
				selectedFormation22.SetArrangementOrder(ArrangementOrder.ArrangementOrderScatter);
			}
			break;
		case OrderType.FormDeep:
			foreach (Formation selectedFormation23 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation23);
				selectedFormation23.SetFormOrder(FormOrder.FormOrderDeep);
			}
			break;
		case OrderType.FormWide:
			foreach (Formation selectedFormation24 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation24);
				selectedFormation24.SetFormOrder(FormOrder.FormOrderWide);
			}
			break;
		case OrderType.FormWider:
			foreach (Formation selectedFormation25 in SelectedFormations)
			{
				TryCancelStopOrder(selectedFormation25);
				selectedFormation25.SetFormOrder(FormOrder.FormOrderWider);
			}
			break;
		default:
			TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Invalid order type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrder", 622);
			break;
		}
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this);
	}

	private void PlayOrderGestures(OrderType orderType)
	{
		if (!LoadingWindow.IsLoadingWindowActive)
		{
			switch (orderType)
			{
			case OrderType.FireAtWill:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FireAtWill, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.HoldFire:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.HoldFire, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.Mount:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Mount, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.Dismount:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Dismount, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.Move:
			case OrderType.MoveToLineSegment:
			case OrderType.MoveToLineSegmentWithHorizontalLayout:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Move, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.Charge:
			case OrderType.ChargeWithTarget:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Charge, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.FollowMe:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Follow, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.Retreat:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Retreat, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.AdvanceTenPaces:
			case OrderType.Advance:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Advance, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.FallBackTenPaces:
			case OrderType.FallBack:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FallBack, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.StandYourGround:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.Stop, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementLine:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormLine, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementCloseOrder:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormShieldWall, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementLoose:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormLoose, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementCircular:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormCircle, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementSchiltron:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormSquare, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementVee:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormSkein, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementColumn:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormColumn, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.ArrangementScatter:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.FormScatter, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.AIControlOn:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.CommandDelegate, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.AIControlOff:
				Owner.MakeVoice(SkinVoiceManager.VoiceType.CommandUndelegate, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				break;
			case OrderType.LookAtEnemy:
				if (Mission.Current.IsNavalBattle)
				{
					Owner.MakeVoice(SkinVoiceManager.VoiceType.BoardAtWill, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				}
				else
				{
					Owner.MakeVoice(SkinVoiceManager.VoiceType.FaceEnemy, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				}
				break;
			case OrderType.LookAtDirection:
				if (Mission.Current.IsNavalBattle)
				{
					Owner.MakeVoice(SkinVoiceManager.VoiceType.AvoidBoarding, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				}
				else
				{
					Owner.MakeVoice(SkinVoiceManager.VoiceType.FaceDirection, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
				}
				break;
			}
		}
		if (_selectedFormations.Count > 0 && Owner != null && Owner.Controller != AgentControllerType.AI)
		{
			MissionWeapon wieldedWeapon = Owner.WieldedWeapon;
			switch ((!wieldedWeapon.IsEmpty) ? wieldedWeapon.Item.PrimaryWeapon.WeaponClass : WeaponClass.Undefined)
			{
			case WeaponClass.Undefined:
			case WeaponClass.Sling:
			case WeaponClass.Stone:
			case WeaponClass.BallistaStone:
				if (Owner.MountAgent == null)
				{
					Owner.SetActionChannel(1, (orderType != OrderType.FollowMe) ? (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_unarmed_leftstance : ActionIndexCache.act_command_unarmed) : (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_follow_unarmed_leftstance : ActionIndexCache.act_command_follow_unarmed), ignorePriority: false, (AnimFlags)0uL);
				}
				else
				{
					Owner.SetActionChannel(1, (orderType == OrderType.FollowMe) ? ActionIndexCache.act_horse_command_follow_unarmed : ActionIndexCache.act_horse_command_unarmed, ignorePriority: false, (AnimFlags)0uL);
				}
				break;
			case WeaponClass.Dagger:
			case WeaponClass.OneHandedSword:
			case WeaponClass.OneHandedAxe:
			case WeaponClass.Mace:
			case WeaponClass.Pick:
			case WeaponClass.OneHandedPolearm:
			case WeaponClass.ThrowingAxe:
			case WeaponClass.ThrowingKnife:
				if (Owner.MountAgent == null)
				{
					Owner.SetActionChannel(1, (orderType != OrderType.FollowMe) ? (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_leftstance : ActionIndexCache.act_command) : (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_follow_leftstance : ActionIndexCache.act_command_follow), ignorePriority: false, (AnimFlags)0uL);
				}
				else
				{
					Owner.SetActionChannel(1, (orderType == OrderType.FollowMe) ? ActionIndexCache.act_horse_command_follow : ActionIndexCache.act_horse_command, ignorePriority: false, (AnimFlags)0uL);
				}
				break;
			case WeaponClass.TwoHandedSword:
			case WeaponClass.TwoHandedAxe:
			case WeaponClass.TwoHandedMace:
			case WeaponClass.TwoHandedPolearm:
			case WeaponClass.LowGripPolearm:
			case WeaponClass.Crossbow:
			case WeaponClass.Javelin:
			case WeaponClass.Pistol:
			case WeaponClass.Musket:
				if (Owner.MountAgent == null)
				{
					Owner.SetActionChannel(1, (orderType != OrderType.FollowMe) ? (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_2h_leftstance : ActionIndexCache.act_command_2h) : (Owner.GetIsLeftStance() ? ActionIndexCache.act_command_follow_2h_leftstance : ActionIndexCache.act_command_follow_2h), ignorePriority: false, (AnimFlags)0uL);
				}
				else
				{
					Owner.SetActionChannel(1, (orderType == OrderType.FollowMe) ? ActionIndexCache.act_horse_command_follow_2h : ActionIndexCache.act_horse_command_2h, ignorePriority: false, (AnimFlags)0uL);
				}
				break;
			case WeaponClass.Bow:
				if (Owner.MountAgent == null)
				{
					Owner.SetActionChannel(1, (orderType == OrderType.FollowMe) ? ActionIndexCache.act_command_follow_bow : ActionIndexCache.act_command_bow, ignorePriority: false, (AnimFlags)0uL);
				}
				else
				{
					Owner.SetActionChannel(1, (orderType == OrderType.FollowMe) ? ActionIndexCache.act_horse_command_follow_bow : ActionIndexCache.act_horse_command_bow, ignorePriority: false, (AnimFlags)0uL);
				}
				break;
			default:
				TaleWorlds.Library.Debug.FailedAssert("Unexpected weapon class.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "PlayOrderGestures", 821);
				break;
			case WeaponClass.Boulder:
			case WeaponClass.BallistaBoulder:
				break;
			}
		}
		foreach (Formation selectedFormation in _selectedFormations)
		{
			Agent medianAgent = selectedFormation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: true, selectedFormation.CachedAveragePosition);
			int countOfUnitsWithCondition = selectedFormation.GetCountOfUnitsWithCondition((Agent agent) => agent.IsFemale);
			int countOfUnits = selectedFormation.CountOfUnits;
			float value = (float)countOfUnitsWithCondition / (float)countOfUnits;
			SoundEventParameter parameter = new SoundEventParameter("GenderRatio", value);
			if (medianAgent == null)
			{
				continue;
			}
			Vec3 position = medianAgent.Position;
			switch (orderType)
			{
			case OrderType.Move:
			case OrderType.MoveToLineSegment:
			case OrderType.MoveToLineSegmentWithHorizontalLayout:
			case OrderType.FollowMe:
			case OrderType.AdvanceTenPaces:
			case OrderType.Advance:
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/nods/move"), ref parameter, position);
				break;
			case OrderType.Charge:
			case OrderType.ChargeWithTarget:
			case OrderType.AttackEntity:
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/nods/attack"), ref parameter, position);
				if (_yellingAfterChargeOrderTimer.Check(reset: true))
				{
					selectedFormation.ApplyActionOnEachUnit(delegate(Agent yellingAgent)
					{
						yellingAgent.YellingBehaviour();
					});
				}
				break;
			case OrderType.StandYourGround:
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/nods/stop"), ref parameter, position);
				break;
			case OrderType.ArrangementLine:
			case OrderType.ArrangementCloseOrder:
			case OrderType.ArrangementLoose:
			case OrderType.ArrangementCircular:
			case OrderType.ArrangementSchiltron:
			case OrderType.ArrangementVee:
			case OrderType.ArrangementColumn:
			case OrderType.ArrangementScatter:
				MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/nods/formation"), ref parameter, position);
				break;
			}
		}
	}

	protected static void PlayFormationSelectedGesture(Formation formation, Agent agent)
	{
		if (formation.SecondaryLogicalClasses.Any())
		{
			agent.MakeVoice(SkinVoiceManager.VoiceType.MixedFormation, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			return;
		}
		switch (formation.LogicalClass)
		{
		case FormationClass.Infantry:
			agent.MakeVoice(SkinVoiceManager.VoiceType.Infantry, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			break;
		case FormationClass.Ranged:
			agent.MakeVoice(SkinVoiceManager.VoiceType.Archers, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			break;
		case FormationClass.Cavalry:
			agent.MakeVoice(SkinVoiceManager.VoiceType.Cavalry, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			break;
		case FormationClass.HorseArcher:
			agent.MakeVoice(SkinVoiceManager.VoiceType.HorseArchers, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			break;
		default:
			TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "PlayFormationSelectedGesture", 906);
			break;
		}
	}

	private void AfterSetOrder(OrderType orderType)
	{
		MBDebug.Print("After set order called, number of selected formations: " + SelectedFormations.Count);
		foreach (Formation selectedFormation in SelectedFormations)
		{
			MBDebug.Print(string.Concat(selectedFormation?.FormationIndex, " formation being processed."));
			if (_formationUpdateEnabledAfterSetOrder)
			{
				bool flag = false;
				if (selectedFormation.IsPlayerTroopInFormation)
				{
					flag = selectedFormation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Follow;
				}
				selectedFormation.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
				}, flag ? Mission.Current.MainAgent : null);
				selectedFormation.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
			}
			MBDebug.Print("Update cached and formation values on each agent complete, number of selected formations: " + SelectedFormations.Count);
			_mission.SetRandomDecideTimeOfAgentsWithIndices(selectedFormation.CollectUnitIndices());
			MBDebug.Print("Set random decide time of agents with indices complete, number of selected formations: " + SelectedFormations.Count);
		}
		MBDebug.Print("After set order loop complete, number of selected formations: " + SelectedFormations.Count);
		if (Owner != null && AreGesturesEnabled())
		{
			PlayOrderGestures(orderType);
		}
	}

	protected void BeforeSetOrder(OrderType orderType)
	{
		foreach (Formation item in SelectedFormations.Where((Formation f) => !IsFormationSelectable(f, Owner)).ToList())
		{
			DeselectFormation(item);
		}
		if (GameNetwork.IsClientOrReplay || orderType == OrderType.AIControlOff || orderType == OrderType.AIControlOn)
		{
			return;
		}
		foreach (Formation selectedFormation in SelectedFormations)
		{
			if (selectedFormation.IsAIControlled && selectedFormation.PlayerOwner != null)
			{
				selectedFormation.SetControlledByAI(isControlledByAI: false);
			}
		}
	}

	public virtual void SetOrderWithAgent(OrderType orderType, Agent agent)
	{
		MBDebug.Print(string.Concat("SetOrderWithAgent ", orderType, " ", agent.Name, "on team"));
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithAgent(orderType, agent.Index));
			GameNetwork.EndModuleEventAsClient();
		}
		if (orderType == OrderType.FollowMe)
		{
			foreach (Formation selectedFormation in SelectedFormations)
			{
				selectedFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(agent));
			}
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Invalid order type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithAgent", 995);
		}
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this, agent);
	}

	public virtual void SetOrderWithPosition(OrderType orderType, WorldPosition orderPosition)
	{
		MBDebug.Print(string.Concat("SetOrderWithPosition ", orderType, " ", orderPosition, "on team"));
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithPosition(orderType, orderPosition.GetGroundVec3()));
			GameNetwork.EndModuleEventAsClient();
		}
		switch (orderType)
		{
		case OrderType.Move:
			foreach (Formation selectedFormation in SelectedFormations)
			{
				selectedFormation.SetMovementOrder(MovementOrder.MovementOrderMove(orderPosition));
			}
			break;
		case OrderType.LookAtDirection:
		{
			FacingOrder facingOrder = FacingOrder.FacingOrderLookAtDirection(GetOrderLookAtDirection(SelectedFormations, orderPosition.AsVec2));
			foreach (Formation selectedFormation2 in SelectedFormations)
			{
				selectedFormation2.SetFacingOrder(facingOrder);
			}
			break;
		}
		case OrderType.FormCustom:
		{
			float orderFormCustomWidth = GetOrderFormCustomWidth(SelectedFormations, orderPosition.GetGroundVec3());
			foreach (Formation selectedFormation3 in SelectedFormations)
			{
				selectedFormation3.SetFormOrder(FormOrder.FormOrderCustom(orderFormCustomWidth));
			}
			break;
		}
		default:
			TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Invalid order type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithPosition", 1045);
			break;
		}
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this, orderPosition);
	}

	public virtual void SetOrderWithFormation(OrderType orderType, Formation orderFormation)
	{
		MBDebug.Print(string.Concat("SetOrderWithFormation ", orderType, " ", orderFormation, "on team"));
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithFormation(orderType, orderFormation.Index));
			GameNetwork.EndModuleEventAsClient();
		}
		switch (orderType)
		{
		case OrderType.Charge:
			foreach (Formation selectedFormation in SelectedFormations)
			{
				selectedFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
				selectedFormation.SetTargetFormation(orderFormation);
			}
			break;
		case OrderType.Advance:
			foreach (Formation selectedFormation2 in SelectedFormations)
			{
				selectedFormation2.SetMovementOrder(MovementOrder.MovementOrderAdvance);
				selectedFormation2.SetTargetFormation(orderFormation);
			}
			break;
		default:
			TaleWorlds.Library.Debug.FailedAssert("Invalid order type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithFormation", 1085);
			break;
		}
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this, orderFormation);
	}

	public void SetOrderWithFormationAndPercentage(OrderType orderType, Formation orderFormation, float percentage)
	{
		int value = (int)(percentage * 100f);
		value = MBMath.ClampInt(value, 0, 100);
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithFormationAndPercentage(orderType, orderFormation.Index, value));
			GameNetwork.EndModuleEventAsClient();
		}
		TaleWorlds.Library.Debug.FailedAssert("Invalid order type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithFormationAndPercentage", 1123);
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this, orderFormation, percentage);
	}

	public void TransferUnitWithPriorityFunction(Formation orderFormation, int number, bool hasShield, bool hasSpear, bool hasThrown, bool isHeavy, bool isRanged, bool isMounted, bool excludeBannerman, List<Agent> excludedAgents)
	{
		TroopFilteringUtilities.GetPriorityFunction(TroopFilteringUtilities.GetFilter(isMounted, isRanged, !isRanged, isHeavy, hasThrown, hasSpear, hasShield), out Func<Agent, int> priorityFunc);
		BeforeSetOrder(OrderType.Transfer);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithFormationAndNumber(OrderType.Transfer, orderFormation.Index, number));
			GameNetwork.EndModuleEventAsClient();
		}
		List<int> list = null;
		int num = SelectedFormations.Sum((Formation f) => f.CountOfUnits);
		int num2 = number;
		int num3 = 0;
		if (SelectedFormations.Count > 1)
		{
			list = new List<int>();
		}
		foreach (Formation selectedFormation in SelectedFormations)
		{
			int countOfUnits = selectedFormation.CountOfUnits;
			int num4 = num2 * countOfUnits / num;
			if (!GameNetwork.IsClientOrReplay)
			{
				selectedFormation.OnMassUnitTransferStart();
				orderFormation.OnMassUnitTransferStart();
				selectedFormation.TransferUnitsWithPriorityFunction(orderFormation, num4, priorityFunc, excludeBannerman, excludedAgents);
				selectedFormation.OnMassUnitTransferEnd();
				orderFormation.OnMassUnitTransferEnd();
			}
			list?.Add(num4);
			num2 -= num4;
			num -= countOfUnits;
			num3 += num4;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			orderFormation.QuerySystem.Expire();
		}
		AfterSetOrder(OrderType.Transfer);
		if (this.OnOrderIssued == null)
		{
			return;
		}
		if (list != null)
		{
			object[] array = new object[list.Count + 1];
			array[0] = number;
			for (int num5 = 0; num5 < list.Count; num5++)
			{
				array[num5 + 1] = list[num5];
			}
			FireOnOrderIssued(OrderType.Transfer, SelectedFormations, this, orderFormation, array);
		}
		else
		{
			FireOnOrderIssued(OrderType.Transfer, SelectedFormations, this, orderFormation, number);
		}
	}

	public void RearrangeFormationsAccordingToFilters(Team team, List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> MassTransferData)
	{
		team.RearrangeFormationsAccordingToFilter(MassTransferData);
	}

	public void SetOrderWithFormationAndNumber(OrderType orderType, Formation orderFormation, int number)
	{
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithFormationAndNumber(orderType, orderFormation.Index, number));
			GameNetwork.EndModuleEventAsClient();
		}
		List<int> list = null;
		if (orderType == OrderType.Transfer)
		{
			int num = SelectedFormations.Sum((Formation f) => f.CountOfUnits);
			int num2 = number;
			int num3 = 0;
			if (SelectedFormations.Count > 1)
			{
				list = new List<int>();
			}
			foreach (Formation selectedFormation in SelectedFormations)
			{
				int countOfUnits = selectedFormation.CountOfUnits;
				int num4 = num2 * countOfUnits / num;
				if (!GameNetwork.IsClientOrReplay)
				{
					selectedFormation.OnMassUnitTransferStart();
					orderFormation.OnMassUnitTransferStart();
					selectedFormation.TransferUnitsAux(orderFormation, num4, isPlayerOrder: true, num4 < countOfUnits && orderFormation.CountOfUnits > 0 && orderFormation.OrderPositionIsValid);
					selectedFormation.OnMassUnitTransferEnd();
					orderFormation.OnMassUnitTransferEnd();
				}
				list?.Add(num4);
				num2 -= num4;
				num -= countOfUnits;
				num3 += num4;
			}
			if (!GameNetwork.IsClientOrReplay)
			{
				orderFormation.QuerySystem.Expire();
			}
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Invalid order type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithFormationAndNumber", 1365);
		}
		AfterSetOrder(orderType);
		if (this.OnOrderIssued == null)
		{
			return;
		}
		if (list != null)
		{
			object[] array = new object[list.Count + 1];
			array[0] = number;
			for (int num5 = 0; num5 < list.Count; num5++)
			{
				array[num5 + 1] = list[num5];
			}
			FireOnOrderIssued(orderType, SelectedFormations, this, orderFormation, array);
		}
		else
		{
			FireOnOrderIssued(orderType, SelectedFormations, this, orderFormation, number);
		}
	}

	public virtual void SetOrderWithTwoPositions(OrderType orderType, WorldPosition position1, WorldPosition position2)
	{
		MBDebug.Print(string.Concat("SetOrderWithTwoPositions ", orderType, " ", position1, " ", position2, "on team"));
		BeforeSetOrder(orderType);
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithTwoPositions(orderType, position1.GetGroundVec3(), position2.GetGroundVec3()));
			GameNetwork.EndModuleEventAsClient();
		}
		if ((uint)(orderType - 2) <= 1u)
		{
			bool isFormationLayoutVertical = orderType == OrderType.MoveToLineSegment;
			IEnumerable<Formation> enumerable = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
			if (enumerable.Any())
			{
				MoveToLineSegment(enumerable, position1, position2, isFormationLayoutVertical);
			}
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("Invalid order type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "SetOrderWithTwoPositions", 1419);
		}
		AfterSetOrder(orderType);
		FireOnOrderIssued(orderType, SelectedFormations, this, position1, position2);
	}

	public virtual void SetOrderWithOrderableObject(IOrderable target)
	{
		BattleSideEnum side = SelectedFormations[0].Team.Side;
		OrderType order = target.GetOrder(side);
		BeforeSetOrder(order);
		MissionObject missionObject = target as MissionObject;
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplyOrderWithMissionObject(missionObject.Id));
			GameNetwork.EndModuleEventAsClient();
		}
		switch (order)
		{
		case OrderType.Use:
		{
			UsableMachine usable = target as UsableMachine;
			ToggleSideOrderUse(SelectedFormations, usable);
			break;
		}
		case OrderType.AttackEntity:
		{
			WeakGameEntity gameEntity = missionObject.GameEntity;
			foreach (Formation selectedFormation in SelectedFormations)
			{
				selectedFormation.SetMovementOrder(MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(gameEntity), !(missionObject is CastleGate)));
			}
			break;
		}
		case OrderType.PointDefence:
		{
			IPointDefendable pointDefendable = target as IPointDefendable;
			foreach (Formation selectedFormation2 in SelectedFormations)
			{
				selectedFormation2.SetMovementOrder(MovementOrder.MovementOrderMove(pointDefendable.MiddleFrame.Origin));
			}
			break;
		}
		case OrderType.Move:
		{
			WorldPosition position = new WorldPosition(_mission.Scene, UIntPtr.Zero, missionObject.GameEntity.GlobalPosition, hasValidZ: false);
			foreach (Formation selectedFormation3 in SelectedFormations)
			{
				selectedFormation3.SetMovementOrder(MovementOrder.MovementOrderMove(position));
			}
			break;
		}
		case OrderType.MoveToLineSegment:
		{
			IPointDefendable obj2 = target as IPointDefendable;
			Vec3 globalPosition3 = obj2.DefencePoints.Last().GameEntity.GlobalPosition;
			Vec3 globalPosition4 = obj2.DefencePoints.First().GameEntity.GlobalPosition;
			IEnumerable<Formation> enumerable2 = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
			if (enumerable2.Any())
			{
				WorldPosition targetLineSegmentBegin2 = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalPosition3, hasValidZ: false);
				WorldPosition targetLineSegmentEnd2 = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalPosition4, hasValidZ: false);
				MoveToLineSegment(enumerable2, targetLineSegmentBegin2, targetLineSegmentEnd2);
			}
			break;
		}
		case OrderType.MoveToLineSegmentWithHorizontalLayout:
		{
			IPointDefendable obj = target as IPointDefendable;
			Vec3 globalPosition = obj.DefencePoints.Last().GameEntity.GlobalPosition;
			Vec3 globalPosition2 = obj.DefencePoints.First().GameEntity.GlobalPosition;
			IEnumerable<Formation> enumerable = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
			if (enumerable.Any())
			{
				WorldPosition targetLineSegmentBegin = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalPosition, hasValidZ: false);
				WorldPosition targetLineSegmentEnd = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalPosition2, hasValidZ: false);
				MoveToLineSegment(enumerable, targetLineSegmentBegin, targetLineSegmentEnd, isFormationLayoutVertical: false);
			}
			break;
		}
		case OrderType.FollowEntity:
		{
			GameEntity waitEntity = (target as UsableMachine).WaitEntity;
			Vec2 direction = waitEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
			foreach (Formation selectedFormation4 in SelectedFormations)
			{
				selectedFormation4.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
				selectedFormation4.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(waitEntity));
			}
			break;
		}
		}
		AfterSetOrder(order);
		FireOnOrderIssued(order, SelectedFormations, this, target);
	}

	public static OrderType GetActiveMovementOrderOf(Formation formation)
	{
		switch (formation.GetReadonlyMovementOrderReference().MovementState)
		{
		case MovementOrder.MovementStateEnum.Charge:
			return OrderType.Charge;
		case MovementOrder.MovementStateEnum.Hold:
			return formation.GetReadonlyMovementOrderReference().OrderType switch
			{
				OrderType.ChargeWithTarget => OrderType.Charge, 
				OrderType.FollowMe => OrderType.FollowMe, 
				OrderType.Advance => OrderType.Advance, 
				OrderType.FallBack => OrderType.FallBack, 
				_ => OrderType.Move, 
			};
		case MovementOrder.MovementStateEnum.Retreat:
			return OrderType.Retreat;
		case MovementOrder.MovementStateEnum.StandGround:
			return OrderType.StandYourGround;
		default:
			TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "GetActiveMovementOrderOf", 1572);
			return OrderType.Move;
		}
	}

	public static OrderType GetActiveFacingOrderOf(Formation formation)
	{
		if (formation.FacingOrder.OrderType == OrderType.LookAtDirection)
		{
			return OrderType.LookAtDirection;
		}
		return OrderType.LookAtEnemy;
	}

	public static OrderType GetActiveRidingOrderOf(Formation formation)
	{
		OrderType orderType = formation.RidingOrder.OrderType;
		if (orderType == OrderType.RideFree)
		{
			return OrderType.Mount;
		}
		return orderType;
	}

	public static OrderType GetActiveArrangementOrderOf(Formation formation)
	{
		return formation.ArrangementOrder.OrderType;
	}

	public static OrderType GetActiveFormOrderOf(Formation formation)
	{
		return formation.FormOrder.OrderType;
	}

	public static OrderType GetActiveFiringOrderOf(Formation formation)
	{
		return formation.FiringOrder.OrderType;
	}

	public static OrderType GetActiveAIControlOrderOf(Formation formation)
	{
		if (formation.IsAIControlled)
		{
			return OrderType.AIControlOn;
		}
		return OrderType.AIControlOff;
	}

	public void SimulateNewOrderWithPositionAndDirection(WorldPosition formationLineBegin, WorldPosition formationLineEnd, out List<WorldPosition> simulationAgentFrames, bool isFormationLayoutVertical)
	{
		IEnumerable<Formation> enumerable = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
		if (enumerable.Any())
		{
			SimulateNewOrderWithPositionAndDirection(enumerable, simulationFormations, formationLineBegin, formationLineEnd, out simulationAgentFrames, isFormationLayoutVertical);
		}
		else
		{
			simulationAgentFrames = new List<WorldPosition>();
		}
	}

	public void SimulateNewFacingOrder(Vec2 direction, out List<WorldPosition> simulationAgentFrames)
	{
		IEnumerable<Formation> enumerable = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
		if (enumerable.Any())
		{
			SimulateNewFacingOrder(enumerable, simulationFormations, direction, out simulationAgentFrames);
		}
		else
		{
			simulationAgentFrames = new List<WorldPosition>();
		}
	}

	public void SimulateNewCustomWidthOrder(float width, out List<WorldPosition> simulationAgentFrames)
	{
		IEnumerable<Formation> enumerable = SelectedFormations.Where((Formation f) => f.CountOfUnitsWithoutDetachedOnes > 0);
		if (enumerable.Any())
		{
			SimulateNewCustomWidthOrder(enumerable, simulationFormations, width, out simulationAgentFrames);
		}
		else
		{
			simulationAgentFrames = new List<WorldPosition>();
		}
	}

	private static void SimulateNewOrderWithPositionAndDirectionAux(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, WorldPosition formationLineBegin, WorldPosition formationLineEnd, bool isSimulatingAgentFrames, out List<WorldPosition> simulationAgentFrames, bool isSimulatingFormationChanges, out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges, out bool isLineShort, bool isFormationLayoutVertical = true)
	{
		float length = (formationLineEnd.AsVec2 - formationLineBegin.AsVec2).Length;
		isLineShort = false;
		if (length < ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius))
		{
			isLineShort = true;
		}
		else
		{
			float num = ((!isFormationLayoutVertical) ? formations.Max((Formation f) => f.Width) : (formations.Sum((Formation f) => f.MinimumWidth) + (float)(formations.Count() - 1) * 1.5f));
			if (length < num)
			{
				isLineShort = true;
			}
		}
		if (isLineShort)
		{
			float num2;
			if (isFormationLayoutVertical)
			{
				num2 = formations.Sum((Formation f) => f.Width);
				num2 += (float)(formations.Count() - 1) * 1.5f;
			}
			else
			{
				num2 = formations.Max((Formation f) => f.Width);
			}
			Vec2 direction = TaleWorlds.Core.Extensions.MaxBy(formations, (Formation f) => f.CountOfUnitsWithoutDetachedOnes).Direction;
			direction.RotateCCW(-System.MathF.PI / 2f);
			direction.Normalize();
			formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 + num2 / 2f * direction, formationLineBegin);
			formationLineBegin = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 - num2 / 2f * direction, formationLineBegin);
		}
		else
		{
			formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineEnd.AsVec2, formationLineBegin);
		}
		if (isFormationLayoutVertical)
		{
			SimulateNewOrderWithVerticalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, out simulationAgentFrames, isSimulatingFormationChanges, out simulationFormationChanges);
		}
		else
		{
			SimulateNewOrderWithHorizontalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, out simulationAgentFrames, isSimulatingFormationChanges, out simulationFormationChanges);
		}
	}

	private static Formation GetSimulationFormation(Formation formation, Dictionary<Formation, Formation> simulationFormations)
	{
		return simulationFormations?[formation];
	}

	private static void SimulateNewFacingOrder(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, Vec2 direction, out List<WorldPosition> simulationAgentFrames)
	{
		simulationAgentFrames = new List<WorldPosition>();
		foreach (Formation formation in formations)
		{
			float formationWidth = formation.Width;
			WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
			int unitSpacingReduction = 0;
			DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in direction, ref formationWidth, ref unitSpacingReduction);
			SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, null, in formationPosition, in direction, formationWidth, unitSpacingReduction, simulateFormationDepth: false, out var _);
		}
	}

	private static void SimulateNewCustomWidthOrder(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, float width, out List<WorldPosition> simulationAgentFrames)
	{
		simulationAgentFrames = new List<WorldPosition>();
		foreach (Formation formation in formations)
		{
			float a = width;
			a = TaleWorlds.Library.MathF.Min(a, formation.MaximumWidth);
			Mat3 identity = Mat3.Identity;
			identity.f = formation.Direction.ToVec3();
			identity.Orthonormalize();
			WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
			int unitSpacingReduction = 0;
			DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, formation.Direction, ref a, ref unitSpacingReduction);
			int count = simulationAgentFrames.Count;
			SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, null, in formationPosition, formation.Direction, a, unitSpacingReduction, simulateFormationDepth: false, out var simulatedFormationDepth);
			float lastSimulatedFormationsOccupationWidthIfLesserThanActualWidth = Formation.GetLastSimulatedFormationsOccupationWidthIfLesserThanActualWidth(GetSimulationFormation(formation, simulationFormations));
			if (lastSimulatedFormationsOccupationWidthIfLesserThanActualWidth > 0f)
			{
				simulationAgentFrames.RemoveRange(count, simulationAgentFrames.Count - count);
				SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, null, in formationPosition, formation.Direction, lastSimulatedFormationsOccupationWidthIfLesserThanActualWidth, unitSpacingReduction, simulateFormationDepth: false, out simulatedFormationDepth);
			}
		}
	}

	public static void SimulateNewOrderWithPositionAndDirection(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, WorldPosition formationLineBegin, WorldPosition formationLineEnd, out List<WorldPosition> simulationAgentFrames, bool isFormationLayoutVertical = true)
	{
		SimulateNewOrderWithPositionAndDirectionAux(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames: true, out simulationAgentFrames, isSimulatingFormationChanges: false, out var _, out var _, isFormationLayoutVertical);
	}

	public static void SimulateNewOrderWithPositionAndDirection(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, WorldPosition formationLineBegin, WorldPosition formationLineEnd, out List<(Formation, int, float, WorldPosition, Vec2)> formationChanges, out bool isLineShort, bool isFormationLayoutVertical = true)
	{
		SimulateNewOrderWithPositionAndDirectionAux(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames: false, out var _, isSimulatingFormationChanges: true, out formationChanges, out isLineShort, isFormationLayoutVertical);
	}

	private static void SimulateNewOrderWithVerticalLayout(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, WorldPosition formationLineBegin, WorldPosition formationLineEnd, bool isSimulatingAgentFrames, out List<WorldPosition> simulationAgentFrames, bool isSimulatingFormationChanges, out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
	{
		simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
		simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
		Vec2 vec = formationLineEnd.AsVec2 - formationLineBegin.AsVec2;
		float length = vec.Length;
		vec.Normalize();
		float num = TaleWorlds.Library.MathF.Max(0f, length - (float)(formations.Count() - 1) * 1.5f);
		float num2 = formations.Sum((Formation f) => f.Width);
		bool flag = num.ApproximatelyEqualsTo(num2, 0.1f);
		float num3 = formations.Sum((Formation f) => f.MinimumWidth);
		formations.Count();
		Vec2 formationDirection = new Vec2(0f - vec.y, vec.x).Normalized();
		float num4 = 0f;
		foreach (Formation formation in formations)
		{
			float minimumWidth = formation.MinimumWidth;
			float a = (flag ? formation.Width : TaleWorlds.Library.MathF.Min((num < num2) ? formation.Width : float.MaxValue, num * (minimumWidth / num3)));
			a = TaleWorlds.Library.MathF.Min(a, formation.MaximumWidth);
			WorldPosition formationPosition = formationLineBegin;
			formationPosition.SetVec2(formationPosition.AsVec2 + vec * (a * 0.5f + num4));
			int unitSpacingReduction = 0;
			DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref a, ref unitSpacingReduction);
			SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, a, unitSpacingReduction, simulateFormationDepth: false, out var _);
			num4 += a + 1.5f;
		}
	}

	private static void DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(Formation formation, Formation simulationFormation, in WorldPosition formationPosition, in Vec2 formationDirection, ref float formationWidth, ref int unitSpacingReduction)
	{
		if (simulationFormation.UnitSpacing != formation.UnitSpacing)
		{
			simulationFormation = new Formation(null, -1);
		}
		int unitIndex = formation.CountOfUnitsWithoutDetachedOnes - 1;
		float actualWidth = formationWidth;
		do
		{
			formation.GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, formationWidth, formation.UnitSpacing - unitSpacingReduction, out var unitSpawnPosition, out var _, out actualWidth);
			if (unitSpawnPosition.HasValue)
			{
				break;
			}
			unitSpacingReduction++;
		}
		while (formation.UnitSpacing - unitSpacingReduction >= 0);
		unitSpacingReduction = TaleWorlds.Library.MathF.Min(unitSpacingReduction, formation.UnitSpacing);
		if (unitSpacingReduction > 0)
		{
			formationWidth = actualWidth;
		}
	}

	private static float GetGapBetweenLinesOfFormation(Formation f, float unitSpacing)
	{
		float num = 0f;
		float num2 = 0.2f;
		if (f.HasAnyMountedUnit && !(f.RidingOrder == RidingOrder.RidingOrderDismount))
		{
			num = 2f;
			num2 = 0.6f;
		}
		return num + unitSpacing * num2;
	}

	private static void SimulateNewOrderWithHorizontalLayout(IEnumerable<Formation> formations, Dictionary<Formation, Formation> simulationFormations, WorldPosition formationLineBegin, WorldPosition formationLineEnd, bool isSimulatingAgentFrames, out List<WorldPosition> simulationAgentFrames, bool isSimulatingFormationChanges, out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
	{
		simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
		simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
		Vec2 vec = formationLineEnd.AsVec2 - formationLineBegin.AsVec2;
		float num = vec.Normalize();
		float num2 = formations.Max((Formation f) => f.MinimumWidth);
		if (num < num2)
		{
			num = num2;
		}
		Vec2 formationDirection = new Vec2(0f - vec.y, vec.x).Normalized();
		float num3 = 0f;
		foreach (Formation formation in formations)
		{
			float a = num;
			a = TaleWorlds.Library.MathF.Min(a, formation.MaximumWidth);
			WorldPosition formationPosition = formationLineBegin;
			formationPosition.SetVec2((formationLineEnd.AsVec2 + formationLineBegin.AsVec2) * 0.5f - formationDirection * num3);
			int unitSpacingReduction = 0;
			DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref a, ref unitSpacingReduction);
			SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, a, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth);
			num3 += simulatedFormationDepth + GetGapBetweenLinesOfFormation(formation, formation.UnitSpacing - unitSpacingReduction);
		}
	}

	private static void SimulateNewOrderWithFrameAndWidth(Formation formation, Formation simulationFormation, List<WorldPosition> simulationAgentFrames, List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges, in WorldPosition formationPosition, in Vec2 formationDirection, float formationWidth, int unitSpacingReduction, bool simulateFormationDepth, out float simulatedFormationDepth)
	{
		int num = 0;
		float num2 = (simulateFormationDepth ? 0f : float.NaN);
		bool flag = Mission.Current.Mode != MissionMode.Deployment || Mission.Current.IsOrderPositionAvailable(in formationPosition, formation.Team);
		foreach (Agent item2 in from u in formation.GetUnitsWithoutDetachedOnes()
			orderby MBCommon.Hash(u.Index, u)
			select u)
		{
			WorldPosition? unitSpawnPosition = null;
			Vec2? unitSpawnDirection = null;
			if (flag)
			{
				formation.GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, num, in formationPosition, in formationDirection, formationWidth, formation.UnitSpacing - unitSpacingReduction, out unitSpawnPosition, out unitSpawnDirection);
			}
			else
			{
				unitSpawnPosition = item2.GetWorldPosition();
				unitSpawnDirection = item2.GetMovementDirection();
			}
			if (unitSpawnPosition.HasValue)
			{
				simulationAgentFrames?.Add(unitSpawnPosition.Value);
				if (simulateFormationDepth)
				{
					float num3 = Vec2.DistanceToLine(formationPosition.AsVec2, formationPosition.AsVec2 + formationDirection.RightVec(), unitSpawnPosition.Value.AsVec2);
					if (num3 > num2)
					{
						num2 = num3;
					}
				}
			}
			num++;
		}
		if (flag)
		{
			simulationFormationChanges?.Add(ValueTuple.Create(formation, unitSpacingReduction, formationWidth, formationPosition, formationDirection));
		}
		else
		{
			WorldPosition item = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
			simulationFormationChanges?.Add(ValueTuple.Create(formation, unitSpacingReduction, formationWidth, item, formation.Direction));
		}
		simulatedFormationDepth = num2 + formation.UnitDiameter;
	}

	public void SimulateDestinationFrames(out List<WorldPosition> simulationAgentFrames, float minDistance = 3f)
	{
		MBReadOnlyList<Formation> selectedFormations = SelectedFormations;
		simulationAgentFrames = new List<WorldPosition>(100);
		float minDistanceSq = minDistance * minDistance;
		foreach (Formation item in selectedFormations)
		{
			item.ApplyActionOnEachUnit(delegate(Agent agent, List<WorldPosition> localSimulationAgentFrames)
			{
				WorldPosition position = ((!_mission.IsTeleportingAgents || agent.CanTeleport()) ? agent.Formation.GetOrderPositionOfUnit(agent) : agent.GetWorldPosition());
				bool flag = position.IsValid;
				if (!GameNetwork.IsMultiplayer && _mission.Mode == MissionMode.Deployment && !_mission.IsNavalBattle)
				{
					IMissionDeploymentPlan deploymentPlan = agent.Mission.DeploymentPlan;
					if (deploymentPlan.SupportsNavmesh(agent.Team))
					{
						deploymentPlan.ProjectPositionToDeploymentBoundaries(agent.Formation.Team, ref position);
					}
					flag = _mission.IsFormationUnitPositionAvailable(ref position, agent.Formation.Team);
				}
				if (flag && agent.Position.AsVec2.DistanceSquared(position.AsVec2) >= minDistanceSq)
				{
					localSimulationAgentFrames.Add(position);
				}
			}, simulationAgentFrames);
		}
	}

	private void ToggleSideOrderUse(IEnumerable<Formation> formations, UsableMachine usable)
	{
		IEnumerable<Formation> enumerable = formations.Where(usable.IsUsedByFormation);
		if (enumerable.IsEmpty())
		{
			foreach (Formation formation in formations)
			{
				formation.StartUsingMachine(usable, isPlayerOrder: true);
			}
			if (!usable.HasWaitFrame)
			{
				return;
			}
			{
				foreach (Formation formation2 in formations)
				{
					formation2.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(usable.WaitEntity));
				}
				return;
			}
		}
		foreach (Formation item in enumerable)
		{
			item.StopUsingMachine(usable, isPlayerOrder: true);
		}
	}

	private static int GetLineOrderByClass(FormationClass formationClass)
	{
		return Array.IndexOf(new FormationClass[8]
		{
			FormationClass.HeavyInfantry,
			FormationClass.Infantry,
			FormationClass.HeavyCavalry,
			FormationClass.Cavalry,
			FormationClass.LightCavalry,
			FormationClass.NumberOfDefaultFormations,
			FormationClass.Ranged,
			FormationClass.HorseArcher
		}, formationClass);
	}

	public static IEnumerable<Formation> SortFormationsForHorizontalLayout(IEnumerable<Formation> formations)
	{
		return formations.OrderBy((Formation f) => GetLineOrderByClass(f.FormationIndex));
	}

	private static IEnumerable<Formation> GetSortedFormations(IEnumerable<Formation> formations, bool isFormationLayoutVertical)
	{
		if (isFormationLayoutVertical)
		{
			return formations;
		}
		return SortFormationsForHorizontalLayout(formations);
	}

	private void MoveToLineSegment(IEnumerable<Formation> formations, WorldPosition TargetLineSegmentBegin, WorldPosition TargetLineSegmentEnd, bool isFormationLayoutVertical = true)
	{
		foreach (Formation formation2 in formations)
		{
			if (actualUnitSpacings.TryGetValue(formation2, out var value))
			{
				formation2.SetPositioning(null, null, value);
			}
			if (actualWidths.TryGetValue(formation2, out var value2))
			{
				formation2.SetFormOrder(FormOrder.FormOrderCustom(value2));
			}
		}
		formations = GetSortedFormations(formations, isFormationLayoutVertical);
		SimulateNewOrderWithPositionAndDirection(formations, simulationFormations, TargetLineSegmentBegin, TargetLineSegmentEnd, out var formationChanges, out var isLineShort, isFormationLayoutVertical);
		if (!formations.Any())
		{
			return;
		}
		foreach (var item6 in formationChanges)
		{
			Formation item = item6.Item1;
			int item2 = item6.Item2;
			float item3 = item6.Item3;
			WorldPosition item4 = item6.Item4;
			Vec2 item5 = item6.Item5;
			int unitSpacing = item.UnitSpacing;
			float width = item.Width;
			if (item2 > 0)
			{
				int value3 = TaleWorlds.Library.MathF.Max(item.UnitSpacing - item2, 0);
				item.SetPositioning(null, null, value3);
				if (item.UnitSpacing != unitSpacing)
				{
					actualUnitSpacings[item] = unitSpacing;
				}
			}
			if (item.Width != item3 && item.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Column)
			{
				item.SetFormOrder(FormOrder.FormOrderCustom(item3));
				if (isLineShort)
				{
					actualWidths[item] = width;
				}
			}
			if (!isLineShort)
			{
				item.SetMovementOrder(MovementOrder.MovementOrderMove(item4));
				item.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(item5));
				item.SetFormOrder(FormOrder.FormOrderCustom(item3));
				MBList<Formation> appliedFormations = new MBList<Formation> { item };
				FireOnOrderIssued(OrderType.Move, appliedFormations, this, item4);
				FireOnOrderIssued(OrderType.LookAtDirection, appliedFormations, this, item5);
				FireOnOrderIssued(OrderType.FormCustom, appliedFormations, this, item3);
				continue;
			}
			Formation formation = TaleWorlds.Core.Extensions.MaxBy(formations, (Formation f) => f.CountOfUnitsWithoutDetachedOnes);
			switch (GetActiveFacingOrderOf(formation))
			{
			case OrderType.LookAtEnemy:
			{
				item.SetMovementOrder(MovementOrder.MovementOrderMove(item4));
				MBList<Formation> appliedFormations3 = new MBList<Formation> { item };
				FireOnOrderIssued(OrderType.Move, appliedFormations3, this, item4);
				FireOnOrderIssued(OrderType.LookAtEnemy, appliedFormations3, this);
				break;
			}
			case OrderType.LookAtDirection:
			{
				item.SetMovementOrder(MovementOrder.MovementOrderMove(item4));
				item.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(formation.Direction));
				MBList<Formation> appliedFormations2 = new MBList<Formation> { item };
				FireOnOrderIssued(OrderType.Move, appliedFormations2, this, item4);
				FireOnOrderIssued(OrderType.LookAtDirection, appliedFormations2, this, formation.Direction);
				break;
			}
			default:
				TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "MoveToLineSegment", 2386);
				break;
			}
		}
	}

	public static Vec2 GetOrderLookAtDirection(IEnumerable<Formation> formations, Vec2 target)
	{
		if (!formations.Any())
		{
			TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", "GetOrderLookAtDirection", 2406);
			return Vec2.One;
		}
		Formation formation = TaleWorlds.Core.Extensions.MaxBy(formations, (Formation f) => f.CountOfUnitsWithoutDetachedOnes);
		return (target - formation.OrderPosition).Normalized();
	}

	public static float GetOrderFormCustomWidth(IEnumerable<Formation> formations, Vec3 orderPosition)
	{
		return (Agent.Main.Position - orderPosition).Length;
	}

	public void TransferUnits(Formation source, Formation target, int count)
	{
		source.TransferUnitsAux(target, count, isPlayerOrder: false, count < source.CountOfUnits && target.CountOfUnits > 0);
		FireOnOrderIssued(OrderType.Transfer, new MBList<Formation> { source }, this, target, count);
	}

	public IEnumerable<Formation> SplitFormation(Formation formation, int count = 2)
	{
		if (!formation.IsSplittableByAI || formation.CountOfUnitsWithoutDetachedOnes < count)
		{
			return new List<Formation> { formation };
		}
		MBDebug.Print(((formation.Team.Side == BattleSideEnum.Attacker) ? "Attacker team" : "Defender team") + " formation " + (int)formation.FormationIndex + " split");
		List<Formation> list = new List<Formation> { formation };
		while (count > 1)
		{
			int num = formation.CountOfUnits / count;
			for (int i = 0; i < 8; i++)
			{
				Formation formation2 = formation.Team.GetFormation((FormationClass)i);
				if (formation2.CountOfUnits == 0)
				{
					formation.TransferUnitsAux(formation2, num, isPlayerOrder: false, useSelectivePop: false);
					list.Add(formation2);
					FireOnOrderIssued(OrderType.Transfer, new MBList<Formation> { formation }, this, formation2, num);
					break;
				}
			}
			count--;
		}
		return list;
	}

	protected void FireOnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		if (this.OnOrderIssued != null)
		{
			this.OnOrderIssued(orderType, appliedFormations, orderController, delegateParams);
		}
	}

	[Conditional("DEBUG")]
	public void TickDebug()
	{
	}

	public void AddOrderOverride(Func<Formation, MovementOrder, MovementOrder> orderOverride)
	{
		if (orderOverrides == null)
		{
			orderOverrides = new List<Func<Formation, MovementOrder, MovementOrder>>();
			overridenOrders = new List<(Formation, OrderType)>();
		}
		orderOverrides.Add(orderOverride);
	}

	public OrderType GetOverridenOrderType(Formation formation)
	{
		if (overridenOrders == null)
		{
			return OrderType.None;
		}
		(Formation, OrderType) tuple = overridenOrders.FirstOrDefault(((Formation, OrderType) oo) => oo.Item1 == formation);
		if (tuple.Item1 != null)
		{
			return tuple.Item2;
		}
		return OrderType.None;
	}

	public void SetFormationUpdateEnabledAfterSetOrder(bool value)
	{
		_formationUpdateEnabledAfterSetOrder = value;
	}

	private void CreateDefaultOrderOverrides()
	{
		AddOrderOverride(delegate(Formation formation, MovementOrder order)
		{
			if (formation.ArrangementOrder.OrderType == OrderType.ArrangementCloseOrder && order.OrderType == OrderType.StandYourGround)
			{
				Vec2 cachedAveragePosition = formation.CachedAveragePosition;
				WorldPosition cachedMedianPosition = formation.CachedMedianPosition;
				cachedMedianPosition.SetVec2(cachedAveragePosition + formation.Direction * formation.Depth * (0.5f + formation.CachedMovementSpeed));
				return MovementOrder.MovementOrderMove(cachedMedianPosition);
			}
			return MovementOrder.MovementOrderStop;
		});
	}

	public static void TryCancelStopOrder(Formation formation)
	{
		if (!GameNetwork.IsClientOrReplay && formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Stop)
		{
			WorldPosition position = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
			if (position.IsValid)
			{
				formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
			}
		}
	}
}
