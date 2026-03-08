using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x0200045F RID: 1119
	public class CommentOnKingdomDestroyedBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004777 RID: 18295 RVA: 0x00165EDA File Offset: 0x001640DA
		public override void RegisterEvents()
		{
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
		}

		// Token: 0x06004778 RID: 18296 RVA: 0x00165EF3 File Offset: 0x001640F3
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004779 RID: 18297 RVA: 0x00165EF5 File Offset: 0x001640F5
		private void OnKingdomDestroyed(Kingdom destroyedKingdom)
		{
			LogEntry.AddLogEntry(new KingdomDestroyedLogEntry(destroyedKingdom));
		}
	}
}
