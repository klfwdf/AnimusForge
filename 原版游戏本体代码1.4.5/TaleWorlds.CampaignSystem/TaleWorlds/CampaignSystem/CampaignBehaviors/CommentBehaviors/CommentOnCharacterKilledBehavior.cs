using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;

public class CommentOnCharacterKilledBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnBeforeHeroKilled);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		if (victim.Clan != null && !Clan.BanditFactions.Contains(victim.Clan))
		{
			CharacterKilledLogEntry characterKilledLogEntry = new CharacterKilledLogEntry(victim, killer, detail);
			LogEntry.AddLogEntry(characterKilledLogEntry);
			if (IsRelatedToPlayer(victim) && ((detail != KillCharacterAction.KillCharacterActionDetail.Executed && detail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent) || killer != Hero.MainHero))
			{
				Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new DeathMapNotification(victim, killer, characterKilledLogEntry.GetEncyclopediaText(), detail, CampaignTime.Now));
			}
		}
	}

	private bool IsRelatedToPlayer(Hero victim)
	{
		bool flag = victim == Hero.MainHero.Mother || victim == Hero.MainHero.Father || victim == Hero.MainHero.Spouse || victim == Hero.MainHero;
		if (!flag)
		{
			foreach (Hero child in Hero.MainHero.Children)
			{
				if (victim == child)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			foreach (Hero sibling in Hero.MainHero.Siblings)
			{
				if (victim == sibling)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}
}
