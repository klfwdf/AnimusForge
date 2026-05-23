using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnChangeSettlementOwnerBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		ChangeSettlementOwnerLogEntry changeSettlementOwnerLogEntry = new ChangeSettlementOwnerLogEntry(settlement, newOwner, previousOwner, bySiege: false);
		LogEntry.AddLogEntry(changeSettlementOwnerLogEntry);
		if (newOwner != null && newOwner.IsHumanPlayerCharacter)
		{
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new SettlementOwnerChangedMapNotification(settlement, newOwner, previousOwner, changeSettlementOwnerLogEntry.GetEncyclopediaText()));
		}
	}
}
