using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000456 RID: 1110
	public class CommentOnChangeSettlementOwnerBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004752 RID: 18258 RVA: 0x00165A96 File Offset: 0x00163C96
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
		}

		// Token: 0x06004753 RID: 18259 RVA: 0x00165AAF File Offset: 0x00163CAF
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004754 RID: 18260 RVA: 0x00165AB4 File Offset: 0x00163CB4
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			ChangeSettlementOwnerLogEntry changeSettlementOwnerLogEntry = new ChangeSettlementOwnerLogEntry(settlement, newOwner, previousOwner, false);
			LogEntry.AddLogEntry(changeSettlementOwnerLogEntry);
			if (newOwner != null && newOwner.IsHumanPlayerCharacter)
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementOwnerChangedMapNotification(settlement, newOwner, previousOwner, changeSettlementOwnerLogEntry.GetEncyclopediaText()));
			}
		}
	}
}
