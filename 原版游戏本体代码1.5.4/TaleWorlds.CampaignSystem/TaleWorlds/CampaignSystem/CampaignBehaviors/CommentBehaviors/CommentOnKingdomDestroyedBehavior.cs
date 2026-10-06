using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnKingdomDestroyedBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		LogEntry.AddLogEntry(new KingdomDestroyedLogEntry(destroyedKingdom));
	}
}
