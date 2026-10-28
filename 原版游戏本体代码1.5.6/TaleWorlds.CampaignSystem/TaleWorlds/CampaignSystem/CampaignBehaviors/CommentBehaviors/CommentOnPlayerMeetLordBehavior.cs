using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnPlayerMeetLordBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnPlayerMetHeroEvent.AddNonSerializedListener(this, OnPlayerMetCharacter);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnPlayerMetCharacter(Hero hero)
	{
		if (hero.Mother != Hero.MainHero && hero.Father != Hero.MainHero)
		{
			LogEntry.AddLogEntry(new PlayerMeetLordLogEntry(hero));
		}
	}
}
