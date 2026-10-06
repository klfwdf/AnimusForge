using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NetworkMessages.FromClient;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.TeamAI;

internal class NavalOrderController : OrderController
{
	private readonly NavalShipsLogic _navalShipsLogic;

	public NavalOrderController(Mission mission, Team team, Agent owner)
		: base(mission, team, owner)
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	protected override void SelectAllFormations(Agent selectorAgent, bool uiFeedback)
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
		_selectedFormations.Clear();
		IEnumerable<Formation> enumerable = Team.FormationsIncludingEmpty.Where((Formation f) => IsFormationSelectable(f, selectorAgent));
		if (enumerable.Count() == 1)
		{
			_selectedFormations.Add(enumerable.First());
		}
		else
		{
			foreach (Formation item in enumerable)
			{
				if (!NavalDLCHelpers.IsAgentCaptainOfFormationShip(selectorAgent, item))
				{
					_selectedFormations.Add(item);
				}
			}
		}
		OnSelectedFormationsCollectionChanged();
	}

	protected override void SelectFormation(Formation formation, Agent selectorAgent)
	{
		if (_selectedFormations.Contains(formation) || !IsFormationSelectable(formation, selectorAgent))
		{
			return;
		}
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SelectFormation(formation.Index));
			GameNetwork.EndModuleEventAsClient();
		}
		if (selectorAgent != null && AreGesturesEnabled())
		{
			OrderController.PlayFormationSelectedGesture(formation, selectorAgent);
		}
		if (NavalDLCHelpers.IsAgentCaptainOfFormationShip(selectorAgent, formation))
		{
			_selectedFormations.Clear();
		}
		else
		{
			_selectedFormations.RemoveAll((Formation x) => NavalDLCHelpers.IsAgentCaptainOfFormationShip(selectorAgent, x));
		}
		_selectedFormations.Add(formation);
		OnSelectedFormationsCollectionChanged();
	}

	public override void SetOrderWithTwoPositions(OrderType orderType, WorldPosition position1, WorldPosition position2)
	{
		SetOrderWithPosition(orderType, position1);
	}

	public override void SetOrderWithPosition(OrderType orderType, WorldPosition position)
	{
		BeforeSetOrder(orderType);
		SetSkirmishState(isSkirmishing: false);
		SetDefensiveState(isDefensive: false);
		MBList<Formation> mBList = (base.SelectedFormations[0].Team.TeamAI as TeamAINavalComponent).TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Where((Formation sf) => base.SelectedFormations.Contains(sf)).ToMBList();
		for (int num = 0; num < mBList.Count; num++)
		{
			Formation formation = mBList[num];
			float num2 = (0f - ((float)mBList.Count - 1f) * 0.5f + (float)num) * 20f;
			Vec2 targetPosition = position.AsVec2 + num2 * ((base.SelectedFormations[0].Team.TeamAI as TeamAINavalComponent).TeamNavalQuerySystem.AverageEnemyShipPosition - (base.SelectedFormations[0].Team.TeamAI as TeamAINavalComponent).TeamNavalQuerySystem.AverageShipPosition).RightVec().Normalized();
			_navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship);
			if (!ship.IsPlayerControlled)
			{
				ship.ShipOrder.SetShipMovementOrder(in targetPosition);
			}
		}
		FireOnOrderIssued(orderType, mBList, this);
	}

	public override void SetOrder(OrderType orderType)
	{
		switch (orderType)
		{
		case OrderType.FollowMe:
			BeforeSetOrder(orderType);
			SetNavalFollowMeOrder();
			SetSkirmishState(isSkirmishing: false);
			SetDefensiveState(isDefensive: false);
			break;
		case OrderType.Charge:
		case OrderType.ChargeWithTarget:
			base.SetOrder(orderType);
			SetDefensiveState(isDefensive: false);
			break;
		case OrderType.Advance:
			BeforeSetOrder(orderType);
			SetNavalEngageWithTargetFormation(null);
			break;
		case OrderType.StandYourGround:
			BeforeSetOrder(orderType);
			SetNavalStop();
			break;
		case OrderType.Retreat:
			BeforeSetOrder(orderType);
			SetNavalRetreat();
			break;
		case OrderType.Dismount:
			BeforeSetOrder(orderType);
			SetNavalSkirmishWithTargetFormation(null);
			SetSkirmishState(isSkirmishing: true);
			SetDefensiveState(isDefensive: false);
			break;
		case OrderType.Mount:
			BeforeSetOrder(orderType);
			SetNavalTroopsDefensive();
			SetSkirmishState(isSkirmishing: false);
			SetDefensiveState(isDefensive: true);
			break;
		default:
			base.SetOrder(orderType);
			break;
		case OrderType.LookAtEnemy:
		case OrderType.LookAtDirection:
			break;
		}
		FireOnOrderIssued(orderType, base.SelectedFormations, this);
	}

	public override void SetOrderWithAgent(OrderType orderType, Agent agent)
	{
		base.SetOrderWithAgent(orderType, agent);
		if (!NavalDLCHelpers.IsShipOrdersAvailable())
		{
			SetSkirmishState(isSkirmishing: false);
			SetDefensiveState(isDefensive: false);
		}
	}

	private void SetSkirmishState(bool isSkirmishing)
	{
		for (int i = 0; i < base.SelectedFormations.Count; i++)
		{
			base.SelectedFormations[i].SetRidingOrder(isSkirmishing ? RidingOrder.RidingOrderDismount : RidingOrder.RidingOrderFree);
		}
	}

	private void SetDefensiveState(bool isDefensive)
	{
		for (int i = 0; i < base.SelectedFormations.Count; i++)
		{
			base.SelectedFormations[i].SetRidingOrder(isDefensive ? RidingOrder.RidingOrderMount : RidingOrder.RidingOrderFree);
		}
	}

	public override void SetOrderWithFormation(OrderType orderType, Formation orderFormation)
	{
		switch (orderType)
		{
		case OrderType.Advance:
			BeforeSetOrder(orderType);
			SetNavalEngageWithTargetFormation(orderFormation);
			FireOnOrderIssued(orderType, base.SelectedFormations, this);
			break;
		case OrderType.Dismount:
			BeforeSetOrder(orderType);
			SetNavalSkirmishWithTargetFormation(orderFormation);
			FireOnOrderIssued(orderType, base.SelectedFormations, this);
			break;
		default:
			base.SetOrderWithFormation(orderType, orderFormation);
			break;
		}
		SetSkirmishState(isSkirmishing: false);
		SetDefensiveState(isDefensive: false);
	}

	public override void SetOrderWithOrderableObject(IOrderable target)
	{
		BeforeSetOrder(OrderType.FollowMe);
		SetNavalFollowOrder(target as MissionShip);
		FireOnOrderIssued(OrderType.FollowMe, base.SelectedFormations, this);
		SetSkirmishState(isSkirmishing: false);
		SetDefensiveState(isDefensive: false);
	}

	private void SetNavalFollowOrder(MissionShip targetShip)
	{
		MBList<Formation> mBList = (base.SelectedFormations[0].Team.TeamAI as TeamAINavalComponent).TeamNavalQuerySystem.FormationsInShipsInLeftToRightOrder.Where((Formation sf) => base.SelectedFormations.Contains(sf)).ToMBList();
		for (int num = 0; num < mBList.Count; num++)
		{
			Formation formation = base.SelectedFormations[num];
			float offsetDistance = (0f - ((float)mBList.Count - 1f) * 0.5f + (float)num) * 20f;
			_navalShipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship);
			if (ship != targetShip)
			{
				ship.ShipOrder.SetShipFollowOrder(targetShip, offsetDistance);
				ship.ShipOrder.SetCutLoose(enable: true);
			}
		}
	}

	private void SetNavalFollowMeOrder()
	{
		MissionShip formationShip = Agent.Main.GetComponent<AgentNavalComponent>().FormationShip;
		SetNavalFollowOrder(formationShip);
	}

	private void SetNavalEngageWithTargetFormation(Formation targetFormation)
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			if (targetFormation == null && selectedFormation.CachedClosestEnemyFormation == null)
			{
				continue;
			}
			bool num = targetFormation != null;
			_navalShipsLogic.GetShip(selectedFormation.Team.TeamSide, selectedFormation.FormationIndex, out var ship);
			if (num)
			{
				_navalShipsLogic.GetShip(targetFormation.Team.TeamSide, targetFormation.FormationIndex, out var ship2);
				ship.ShipOrder.SetShipEngageOrder(ship2);
				ship.ShipOrder.SetBoardingTargetShip(ship2);
				continue;
			}
			ship.ShipOrder.SetShipEngageOrder();
			if (ship.ShipOrder.TargetShip != null)
			{
				ship.ShipOrder.SetBoardingTargetShip(ship.ShipOrder.TargetShip);
			}
		}
	}

	private void SetNavalSkirmishWithTargetFormation(Formation targetFormation)
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			if (targetFormation != null || selectedFormation.CachedClosestEnemyFormation != null)
			{
				_navalShipsLogic.GetShip(selectedFormation.Team.TeamSide, selectedFormation.FormationIndex, out var ship);
				if (targetFormation != null)
				{
					_navalShipsLogic.GetShip(targetFormation.Team.TeamSide, targetFormation.FormationIndex, out var ship2);
					ship.ShipOrder.SetShipSkirmishOrder(ship2);
				}
				else
				{
					ship.ShipOrder.SetShipSkirmishOrder();
				}
				ship.ShipOrder.SetCutLoose(enable: true);
			}
		}
	}

	private void SetNavalStop()
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			_navalShipsLogic.GetShip(selectedFormation.Team.TeamSide, selectedFormation.FormationIndex, out var ship);
			ship.ShipOrder.SetShipStopOrder();
			ship.ShipOrder.SetBoardingTargetShip(null);
			ship.ShipOrder.SetCutLoose(enable: false);
		}
	}

	private void SetNavalRetreat()
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			_navalShipsLogic.GetShip(selectedFormation.Team.TeamSide, selectedFormation.FormationIndex, out var ship);
			ship.ShipOrder.SetShipRetreatOrder();
			ship.ShipOrder.SetCutLoose(enable: true);
		}
	}

	private void SetNavalTroopsAggressive()
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			selectedFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
			selectedFormation.SetRidingOrder(RidingOrder.RidingOrderDismount);
		}
	}

	public static MovementOrder GetNavalDefensiveMovementOrder(MissionShip missionShip)
	{
		missionShip.GetWorldPositionOnDeck(out var worldPosition);
		return MovementOrder.MovementOrderMove(worldPosition);
	}

	private void SetNavalTroopsDefensive()
	{
		foreach (Formation selectedFormation in base.SelectedFormations)
		{
			_navalShipsLogic.GetShip(selectedFormation.Team.TeamSide, selectedFormation.FormationIndex, out var ship);
			ship?.SetPositioningOrdersToRallyPoint(applyToPlayerFormation: true, playersOrder: true);
			selectedFormation.SetRidingOrder(RidingOrder.RidingOrderMount);
		}
	}
}
