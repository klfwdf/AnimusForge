using System.Collections.Generic;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentChildbirthBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirthEvent);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnGivenBirthEvent(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
		if (!mother.IsHumanPlayerCharacter && mother.Clan != Hero.MainHero.Clan)
		{
			return;
		}
		for (int i = 0; i < stillbornCount; i++)
		{
			ChildbirthLogEntry childbirthLogEntry = new ChildbirthLogEntry(mother, null);
			LogEntry.AddLogEntry(childbirthLogEntry);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ChildBornMapNotification(null, childbirthLogEntry.GetEncyclopediaText(), CampaignTime.Now));
		}
		foreach (Hero aliveChild in aliveChildren)
		{
			ChildbirthLogEntry childbirthLogEntry2 = new ChildbirthLogEntry(mother, aliveChild);
			LogEntry.AddLogEntry(childbirthLogEntry2);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ChildBornMapNotification(aliveChild, childbirthLogEntry2.GetEncyclopediaText(), CampaignTime.Now));
		}
	}
}
