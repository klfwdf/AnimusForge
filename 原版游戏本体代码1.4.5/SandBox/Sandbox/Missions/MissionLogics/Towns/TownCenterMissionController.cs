using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns;

public class TownCenterMissionController : MissionLogic
{
	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = true;
	}

	public override void AfterStart()
	{
		bool isNight = Campaign.Current.IsNight;
		base.Mission.SetMissionMode(MissionMode.StartUp, atStart: true);
		base.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
		base.Mission.IsQuestScreenAccessible = true;
		MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, noHorses: true);
		missionBehavior.SpawnLocationCharacters();
		SandBoxHelpers.MissionHelper.SpawnHorses();
		if (!isNight)
		{
			SandBoxHelpers.MissionHelper.SpawnSheeps();
			SandBoxHelpers.MissionHelper.SpawnCows();
			SandBoxHelpers.MissionHelper.SpawnHogs();
			SandBoxHelpers.MissionHelper.SpawnGeese();
			SandBoxHelpers.MissionHelper.SpawnChicken();
		}
	}
}
