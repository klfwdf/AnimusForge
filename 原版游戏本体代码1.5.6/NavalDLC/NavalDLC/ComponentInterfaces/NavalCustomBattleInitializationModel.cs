using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.ComponentInterfaces;

public class NavalCustomBattleInitializationModel : BattleInitializationModel
{
	public override List<FormationClass> GetAllAvailableTroopTypes()
	{
		return base.BaseModel.GetAllAvailableTroopTypes();
	}

	protected override bool CanPlayerSideDeployWithOrderOfBattleAux()
	{
		IMissionAgentSpawnLogic missionBehavior = Mission.Current.GetMissionBehavior<IMissionAgentSpawnLogic>();
		if (missionBehavior is DefaultNavalMissionAgentSpawnLogic defaultNavalMissionAgentSpawnLogic)
		{
			return defaultNavalMissionAgentSpawnLogic.DeployablePlayerShipCount > 1;
		}
		if (missionBehavior is NavalRaidMissionAgentSpawnLogic navalRaidMissionAgentSpawnLogic)
		{
			if (navalRaidMissionAgentSpawnLogic.PlayerSide == BattleSideEnum.Attacker)
			{
				return navalRaidMissionAgentSpawnLogic.DeployablePlayerShipCount > 1;
			}
			return navalRaidMissionAgentSpawnLogic.GetNumberOfPlayerControllableTroops() >= 20;
		}
		Debug.FailedAssert("Unable to retrieve mission agent spawn logic behavior for custom mission", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\ComponentInterfaces\\NavalCustomBattleInitializationModel.cs", "CanPlayerSideDeployWithOrderOfBattleAux", 42);
		return false;
	}
}
