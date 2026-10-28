using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnClanDestroyedBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnClanDestroyed(Clan destroyedClan)
	{
		LogEntry.AddLogEntry(new ClanDestroyedLogEntry(destroyedClan));
	}
}
