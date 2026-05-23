using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnLeaveFactionBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanLeaveKingdom);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnClanLeaveKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		LogEntry.AddLogEntry(new ClanChangeKingdomLogEntry(clan, oldKingdom, newKingdom, detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion));
	}
}
