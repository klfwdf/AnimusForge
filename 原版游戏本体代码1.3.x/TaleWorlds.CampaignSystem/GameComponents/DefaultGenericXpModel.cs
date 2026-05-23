using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000117 RID: 279
	public class DefaultGenericXpModel : GenericXpModel
	{
		// Token: 0x060017E2 RID: 6114 RVA: 0x00071B3A File Offset: 0x0006FD3A
		public override float GetXpMultiplier(Hero hero)
		{
			if (hero.IsPlayerCompanion && Hero.MainHero.GetPerkValue(DefaultPerks.Charm.NaturalLeader))
			{
				return 1.2f;
			}
			return 1f;
		}
	}
}
