using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200008A RID: 138
	public class VillageMissionController : MissionLogic
	{
		// Token: 0x0600055D RID: 1373 RVA: 0x00023BBB File Offset: 0x00021DBB
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = false;
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x00023BD0 File Offset: 0x00021DD0
		public override void AfterStart()
		{
			base.AfterStart();
			bool isNight = Campaign.Current.IsNight;
			base.Mission.IsInventoryAccessible = true;
			base.Mission.IsQuestScreenAccessible = true;
			MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, false, false, false, "");
			missionBehavior.SpawnLocationCharacters(null);
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
}
