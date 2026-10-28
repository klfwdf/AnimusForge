using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnClanLeaderChangedBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
	}

	private static void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		LogEntry.AddLogEntry(new ClanLeaderChangedLogEntry(oldLeader, newLeader));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
