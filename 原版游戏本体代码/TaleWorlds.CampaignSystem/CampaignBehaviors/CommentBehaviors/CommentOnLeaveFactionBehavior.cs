using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000460 RID: 1120
	public class CommentOnLeaveFactionBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600477B RID: 18299 RVA: 0x00165F0A File Offset: 0x0016410A
		public override void RegisterEvents()
		{
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanLeaveKingdom));
		}

		// Token: 0x0600477C RID: 18300 RVA: 0x00165F23 File Offset: 0x00164123
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600477D RID: 18301 RVA: 0x00165F25 File Offset: 0x00164125
		private void OnClanLeaveKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			LogEntry.AddLogEntry(new ClanChangeKingdomLogEntry(clan, oldKingdom, newKingdom, detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion));
		}
	}
}
