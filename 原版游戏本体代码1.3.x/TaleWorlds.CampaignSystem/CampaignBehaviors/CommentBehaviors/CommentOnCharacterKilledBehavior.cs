using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000458 RID: 1112
	public class CommentOnCharacterKilledBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600475A RID: 18266 RVA: 0x00165B95 File Offset: 0x00163D95
		public override void RegisterEvents()
		{
			CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnBeforeHeroKilled));
		}

		// Token: 0x0600475B RID: 18267 RVA: 0x00165BAE File Offset: 0x00163DAE
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x0600475C RID: 18268 RVA: 0x00165BB0 File Offset: 0x00163DB0
		private void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (victim.Clan != null && !Clan.BanditFactions.Contains(victim.Clan))
			{
				CharacterKilledLogEntry characterKilledLogEntry = new CharacterKilledLogEntry(victim, killer, detail);
				LogEntry.AddLogEntry(characterKilledLogEntry);
				if (this.IsRelatedToPlayer(victim) && ((detail != KillCharacterAction.KillCharacterActionDetail.Executed && detail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent) || killer != Hero.MainHero))
				{
					Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new DeathMapNotification(victim, killer, characterKilledLogEntry.GetEncyclopediaText(), detail, CampaignTime.Now));
				}
			}
		}

		// Token: 0x0600475D RID: 18269 RVA: 0x00165C24 File Offset: 0x00163E24
		private bool IsRelatedToPlayer(Hero victim)
		{
			bool flag = victim == Hero.MainHero.Mother || victim == Hero.MainHero.Father || victim == Hero.MainHero.Spouse || victim == Hero.MainHero;
			if (!flag)
			{
				foreach (Hero hero in Hero.MainHero.Children)
				{
					if (victim == hero)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				foreach (Hero hero2 in Hero.MainHero.Siblings)
				{
					if (victim == hero2)
					{
						flag = true;
						break;
					}
				}
			}
			return flag;
		}
	}
}
