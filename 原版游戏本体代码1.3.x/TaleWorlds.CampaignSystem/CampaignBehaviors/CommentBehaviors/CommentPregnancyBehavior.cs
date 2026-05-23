using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000463 RID: 1123
	public class CommentPregnancyBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004787 RID: 18311 RVA: 0x00166003 File Offset: 0x00164203
		public override void RegisterEvents()
		{
			CampaignEvents.OnChildConceivedEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnChildConceived));
		}

		// Token: 0x06004788 RID: 18312 RVA: 0x0016601C File Offset: 0x0016421C
		private void OnChildConceived(Hero mother)
		{
			LogEntry.AddLogEntry(new PregnancyLogEntry(mother));
		}

		// Token: 0x06004789 RID: 18313 RVA: 0x00166029 File Offset: 0x00164229
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
