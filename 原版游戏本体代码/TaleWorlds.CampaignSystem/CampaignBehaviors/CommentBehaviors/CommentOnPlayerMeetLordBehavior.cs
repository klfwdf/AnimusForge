using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000462 RID: 1122
	public class CommentOnPlayerMeetLordBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004783 RID: 18307 RVA: 0x00165FB9 File Offset: 0x001641B9
		public override void RegisterEvents()
		{
			CampaignEvents.OnPlayerMetHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnPlayerMetCharacter));
		}

		// Token: 0x06004784 RID: 18308 RVA: 0x00165FD2 File Offset: 0x001641D2
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004785 RID: 18309 RVA: 0x00165FD4 File Offset: 0x001641D4
		private void OnPlayerMetCharacter(Hero hero)
		{
			if (hero.Mother != Hero.MainHero && hero.Father != Hero.MainHero)
			{
				LogEntry.AddLogEntry(new PlayerMeetLordLogEntry(hero));
			}
		}
	}
}
