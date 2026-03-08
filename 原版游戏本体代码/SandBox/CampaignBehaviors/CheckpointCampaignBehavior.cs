using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000CC RID: 204
	public class CheckpointCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000905 RID: 2309 RVA: 0x00042334 File Offset: 0x00040534
		public override void RegisterEvents()
		{
		}

		// Token: 0x06000906 RID: 2310 RVA: 0x00042336 File Offset: 0x00040536
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<int>("LastUsedMissionCheckpointId", ref this.LastUsedMissionCheckpointId);
			dataStore.SyncData<List<AgentSaveData>>("CorpseList", ref this.CorpseList);
		}

		// Token: 0x04000460 RID: 1120
		public int LastUsedMissionCheckpointId = -1;

		// Token: 0x04000461 RID: 1121
		public List<AgentSaveData> CorpseList = new List<AgentSaveData>();
	}
}
