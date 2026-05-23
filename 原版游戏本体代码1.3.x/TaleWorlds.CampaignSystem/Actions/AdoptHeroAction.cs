using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000490 RID: 1168
	public static class AdoptHeroAction
	{
		// Token: 0x0600493A RID: 18746 RVA: 0x001701F9 File Offset: 0x0016E3F9
		private static void ApplyInternal(Hero adoptedHero)
		{
			if (Hero.MainHero.IsFemale)
			{
				adoptedHero.Mother = Hero.MainHero;
			}
			else
			{
				adoptedHero.Father = Hero.MainHero;
			}
			adoptedHero.Clan = Clan.PlayerClan;
		}

		// Token: 0x0600493B RID: 18747 RVA: 0x0017022A File Offset: 0x0016E42A
		public static void Apply(Hero adoptedHero)
		{
			AdoptHeroAction.ApplyInternal(adoptedHero);
		}
	}
}
