using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200006E RID: 110
	public class IndoorMissionController : MissionLogic
	{
		// Token: 0x0600046B RID: 1131 RVA: 0x0001A740 File Offset: 0x00018940
		public override void OnCreated()
		{
			base.OnCreated();
			base.Mission.DoesMissionRequireCivilianEquipment = true;
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0001A754 File Offset: 0x00018954
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		}

		// Token: 0x0600046D RID: 1133 RVA: 0x0001A770 File Offset: 0x00018970
		public override void AfterStart()
		{
			base.AfterStart();
			base.Mission.SetMissionMode(MissionMode.StartUp, true);
			base.Mission.IsInventoryAccessible = !Campaign.Current.IsMainHeroDisguised;
			base.Mission.IsQuestScreenAccessible = true;
			SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.DoesMissionRequireCivilianEquipment, true, false, false, "");
			this._missionAgentHandler.SpawnLocationCharacters(null);
		}

		// Token: 0x0400025D RID: 605
		private MissionAgentHandler _missionAgentHandler;
	}
}
