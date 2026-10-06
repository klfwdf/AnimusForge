using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class NavalBattleInitializationModel : BattleInitializationModel
{
	public override List<FormationClass> GetAllAvailableTroopTypes()
	{
		return base.BaseModel.GetAllAvailableTroopTypes();
	}

	protected override bool CanPlayerSideDeployWithOrderOfBattleAux()
	{
		if (Mission.Current.IsSallyOutBattle)
		{
			return false;
		}
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (MapEvent.PlayerMapEvent == null)
		{
			return false;
		}
		PartyBase leaderParty = playerMapEvent.GetLeaderParty(playerMapEvent.PlayerSide);
		if (leaderParty == PartyBase.MainParty || (leaderParty.IsSettlement && leaderParty.Settlement.OwnerClan.Leader == Hero.MainHero) || playerMapEvent.IsPlayerSergeant())
		{
			IMissionAgentSpawnLogic missionBehavior = Mission.Current.GetMissionBehavior<IMissionAgentSpawnLogic>();
			if (missionBehavior is INavalMissionAgentSpawnLogic navalMissionAgentSpawnLogic)
			{
				return navalMissionAgentSpawnLogic.DeployablePlayerShipCount > 1;
			}
			return missionBehavior.GetNumberOfPlayerControllableTroops() >= 20;
		}
		return false;
	}
}
