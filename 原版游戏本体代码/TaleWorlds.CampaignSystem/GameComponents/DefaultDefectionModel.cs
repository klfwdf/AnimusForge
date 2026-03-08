using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200010B RID: 267
	public class DefaultDefectionModel : DefectionModel
	{
		// Token: 0x06001736 RID: 5942 RVA: 0x0006C258 File Offset: 0x0006A458
		public override bool CanHeroDefectToFaction(Hero hero, Kingdom kingdom)
		{
			return hero != null && hero.MapFaction != null && hero.MapFaction.IsKingdomFaction && hero.MapFaction != Hero.MainHero.MapFaction && hero.MapFaction.Leader != hero && hero.Clan != null && !hero.Clan.IsMinorFaction && !hero.Clan.IsUnderMercenaryService && hero.Clan.Kingdom != null && !hero.IsPrisoner;
		}
	}
}
