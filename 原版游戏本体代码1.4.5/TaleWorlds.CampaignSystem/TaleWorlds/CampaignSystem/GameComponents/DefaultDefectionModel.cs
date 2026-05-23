using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultDefectionModel : DefectionModel
{
	public override bool CanHeroDefectToFaction(Hero hero, Kingdom kingdom)
	{
		if (hero != null && hero.MapFaction != null && hero.MapFaction.IsKingdomFaction && hero.MapFaction != Hero.MainHero.MapFaction && hero.MapFaction.Leader != hero && hero.Clan != null && !hero.Clan.IsMinorFaction && !hero.Clan.IsUnderMercenaryService && hero.Clan.Kingdom != null)
		{
			return !hero.IsPrisoner;
		}
		return false;
	}
}
