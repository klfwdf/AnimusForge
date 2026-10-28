using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnDefeatCharacterBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.CharacterDefeated.AddNonSerializedListener(this, OnCharacterDefeated);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnCharacterDefeated(Hero winner, Hero loser)
	{
		LogEntry.AddLogEntry(new DefeatCharacterLogEntry(winner, loser));
	}
}
