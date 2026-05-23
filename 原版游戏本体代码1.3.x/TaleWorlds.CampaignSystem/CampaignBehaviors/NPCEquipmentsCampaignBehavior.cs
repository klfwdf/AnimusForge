using System;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041E RID: 1054
	public class NPCEquipmentsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042C5 RID: 17093 RVA: 0x00142423 File Offset: 0x00140623
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
		}

		// Token: 0x060042C6 RID: 17094 RVA: 0x0014243C File Offset: 0x0014063C
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060042C7 RID: 17095 RVA: 0x00142440 File Offset: 0x00140640
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			foreach (CharacterObject characterObject in CharacterObject.All)
			{
				bool isTemplate = characterObject.IsTemplate;
			}
		}
	}
}
