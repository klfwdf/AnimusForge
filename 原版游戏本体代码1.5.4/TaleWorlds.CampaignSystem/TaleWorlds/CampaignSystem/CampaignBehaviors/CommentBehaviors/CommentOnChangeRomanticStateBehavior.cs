using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnChangeRomanticStateBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.RomanticStateChanged.AddNonSerializedListener(this, OnRomanticStateChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum level)
	{
		if (hero1 == Hero.MainHero || hero2 == Hero.MainHero || hero1.Clan.Leader == hero1 || hero2.Clan.Leader == hero2)
		{
			LogEntry.AddLogEntry(new ChangeRomanticStateLogEntry(hero1, hero2, level));
		}
	}
}
