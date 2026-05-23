using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

public class AmmoSupplyLogic : MissionLogic
{
	private const float CheckTimePeriod = 3f;

	private readonly List<BattleSideEnum> _sideList;

	private readonly BasicMissionTimer _checkTimer;

	public AmmoSupplyLogic(List<BattleSideEnum> sideList)
	{
		_sideList = sideList;
		_checkTimer = new BasicMissionTimer();
	}

	public bool IsAgentEligibleForAmmoSupply(Agent agent)
	{
		if (agent.IsAIControlled && _sideList.Contains(agent.Team.Side))
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!agent.Equipment[equipmentIndex].IsEmpty && agent.Equipment[equipmentIndex].IsAnyAmmo())
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnMissionTick(float dt)
	{
		if (!(_checkTimer.ElapsedTime > 3f))
		{
			return;
		}
		_checkTimer.Reset();
		foreach (Team team in base.Mission.Teams)
		{
			if (_sideList.IndexOf(team.Side) < 0)
			{
				continue;
			}
			foreach (Agent activeAgent in team.ActiveAgents)
			{
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					if (activeAgent.IsAIControlled && !activeAgent.Equipment[equipmentIndex].IsEmpty && activeAgent.Equipment[equipmentIndex].IsAnyAmmo())
					{
						short modifiedMaxAmount = activeAgent.Equipment[equipmentIndex].ModifiedMaxAmount;
						short amount = activeAgent.Equipment[equipmentIndex].Amount;
						short num = modifiedMaxAmount;
						if (modifiedMaxAmount > 1)
						{
							num = (short)(modifiedMaxAmount - 1);
						}
						if (amount < num)
						{
							activeAgent.SetWeaponAmountInSlot(equipmentIndex, num, enforcePrimaryItem: false);
						}
					}
				}
			}
		}
	}
}
