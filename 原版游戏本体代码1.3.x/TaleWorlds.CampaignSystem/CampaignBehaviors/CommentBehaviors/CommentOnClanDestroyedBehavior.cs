using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000459 RID: 1113
	public class CommentOnClanDestroyedBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600475F RID: 18271 RVA: 0x00165D04 File Offset: 0x00163F04
		public override void RegisterEvents()
		{
			CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
		}

		// Token: 0x06004760 RID: 18272 RVA: 0x00165D1D File Offset: 0x00163F1D
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004761 RID: 18273 RVA: 0x00165D1F File Offset: 0x00163F1F
		private void OnClanDestroyed(Clan destroyedClan)
		{
			LogEntry.AddLogEntry(new ClanDestroyedLogEntry(destroyedClan));
		}
	}
}
