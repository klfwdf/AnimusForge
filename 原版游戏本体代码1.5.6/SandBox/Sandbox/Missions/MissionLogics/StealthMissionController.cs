using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class StealthMissionController : MissionLogic
{
	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		base.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
		base.Mission.IsQuestScreenAccessible = true;
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: false, noHorses: true);
		Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters();
	}
}
