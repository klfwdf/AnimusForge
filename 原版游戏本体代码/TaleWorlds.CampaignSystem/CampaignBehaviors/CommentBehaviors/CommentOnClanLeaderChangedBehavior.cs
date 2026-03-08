using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x0200045A RID: 1114
	public class CommentOnClanLeaderChangedBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004763 RID: 18275 RVA: 0x00165D34 File Offset: 0x00163F34
		public override void RegisterEvents()
		{
			CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(CommentOnClanLeaderChangedBehavior.OnClanLeaderChanged));
		}

		// Token: 0x06004764 RID: 18276 RVA: 0x00165D4D File Offset: 0x00163F4D
		private static void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
		{
			LogEntry.AddLogEntry(new ClanLeaderChangedLogEntry(oldLeader, newLeader));
		}

		// Token: 0x06004765 RID: 18277 RVA: 0x00165D5B File Offset: 0x00163F5B
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
