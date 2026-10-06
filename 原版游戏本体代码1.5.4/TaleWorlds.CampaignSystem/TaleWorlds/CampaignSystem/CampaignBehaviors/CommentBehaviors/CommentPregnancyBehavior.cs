using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentPregnancyBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnChildConceivedEvent.AddNonSerializedListener(this, OnChildConceived);
	}

	private void OnChildConceived(Hero mother)
	{
		LogEntry.AddLogEntry(new PregnancyLogEntry(mother));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
