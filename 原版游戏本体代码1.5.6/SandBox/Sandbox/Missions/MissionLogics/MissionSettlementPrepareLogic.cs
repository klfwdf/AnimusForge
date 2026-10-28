using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionSettlementPrepareLogic : MissionLogic
{
	public override void AfterStart()
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign && Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.IsCastle))
		{
			OpenGates();
		}
	}

	private void OpenGates()
	{
		foreach (CastleGate item in Mission.Current.ActiveMissionObjects.FindAllWithType<CastleGate>().ToList())
		{
			item.OpenDoorAndDisableGateForCivilianMission();
		}
	}
}
