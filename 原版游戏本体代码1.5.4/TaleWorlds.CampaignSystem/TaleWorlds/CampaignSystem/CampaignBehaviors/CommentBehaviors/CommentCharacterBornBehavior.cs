using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentCharacterBornBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, HeroCreated);
	}

	private void HeroCreated(Hero hero, bool isBornNaturally)
	{
		if (isBornNaturally)
		{
			LogEntry.AddLogEntry(new CharacterBornLogEntry(hero));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
