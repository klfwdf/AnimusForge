using System;
using TaleWorlds.CampaignSystem.LogEntries;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000455 RID: 1109
	public class CommentOnChangeRomanticStateBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600474E RID: 18254 RVA: 0x00165A38 File Offset: 0x00163C38
		public override void RegisterEvents()
		{
			CampaignEvents.RomanticStateChanged.AddNonSerializedListener(this, new Action<Hero, Hero, Romance.RomanceLevelEnum>(this.OnRomanticStateChanged));
		}

		// Token: 0x0600474F RID: 18255 RVA: 0x00165A51 File Offset: 0x00163C51
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004750 RID: 18256 RVA: 0x00165A53 File Offset: 0x00163C53
		private void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum level)
		{
			if (hero1 == Hero.MainHero || hero2 == Hero.MainHero || hero1.Clan.Leader == hero1 || hero2.Clan.Leader == hero2)
			{
				LogEntry.AddLogEntry(new ChangeRomanticStateLogEntry(hero1, hero2, level));
			}
		}
	}
}
