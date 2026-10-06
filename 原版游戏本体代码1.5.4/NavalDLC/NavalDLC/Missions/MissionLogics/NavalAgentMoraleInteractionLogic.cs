using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalAgentMoraleInteractionLogic : MissionLogic
{
	private NavalShipsLogic _navalShipsLogic;

	public override void OnBehaviorInitialize()
	{
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipsConnectedEvent += OnShipConnected;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipsConnectedEvent -= OnShipConnected;
	}

	private void OnShipConnected(MissionShip ownerShip, MissionShip targetShip)
	{
		int num = 0;
		foreach (ShipAttachmentMachine attachmentMachine in ownerShip.AttachmentMachines)
		{
			ShipAttachmentMachine.ShipAttachment currentAttachment = attachmentMachine.CurrentAttachment;
			if (currentAttachment != null && currentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				num++;
			}
			if (num > 1)
			{
				break;
			}
		}
		if (num != 1 || ownerShip.Team == null || targetShip?.Team == null || !ownerShip.Team.IsEnemyOf(targetShip.Team))
		{
			return;
		}
		foreach (Agent unitsWithoutDetachedOne in targetShip.Formation.GetUnitsWithoutDetachedOnes())
		{
			if (unitsWithoutDetachedOne.IsAIControlled)
			{
				float delta = MissionGameModels.Current.BattleMoraleModel.CalculateMoraleOnShipsConnected(unitsWithoutDetachedOne, ownerShip.ShipOrigin, targetShip.ShipOrigin);
				unitsWithoutDetachedOne.ChangeMorale(delta);
			}
		}
	}

	public void OnShipSunk(MissionShip ship)
	{
		float delta = MissionGameModels.Current.BattleMoraleModel.CalculateMoraleChangeOnShipSunk(ship.ShipOrigin);
		if (ship.Team == null)
		{
			return;
		}
		foreach (Agent activeAgent in ship.Team.ActiveAgents)
		{
			if (activeAgent.IsAIControlled)
			{
				activeAgent.ChangeMorale(delta);
			}
		}
	}

	public void OnShipRammed(MissionShip rammingShip, MissionShip rammedShip)
	{
		if (rammingShip?.Team == null || rammedShip.Team == null || !rammingShip.Team.IsEnemyOf(rammedShip.Team))
		{
			return;
		}
		foreach (Agent activeAgent in rammingShip.Team.ActiveAgents)
		{
			if (activeAgent.IsAIControlled)
			{
				float delta = MissionGameModels.Current.BattleMoraleModel.CalculateMoraleOnRamming(activeAgent, rammingShip.ShipOrigin, rammedShip.ShipOrigin);
				activeAgent.ChangeMorale(delta);
			}
		}
	}
}
