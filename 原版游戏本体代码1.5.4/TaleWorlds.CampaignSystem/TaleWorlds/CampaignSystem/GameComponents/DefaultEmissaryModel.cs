using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultEmissaryModel : EmissaryModel
{
	public override int EmissaryRelationBonusForMainClan => 5;

	public override bool IsEmissary(Hero hero)
	{
		if ((hero.CompanionOf == Clan.PlayerClan || hero.Clan == Clan.PlayerClan) && hero.PartyBelongedTo == null && hero.CurrentSettlement != null && hero.CurrentSettlement.IsFortification && !hero.IsPrisoner)
		{
			return hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;
		}
		return false;
	}
}
