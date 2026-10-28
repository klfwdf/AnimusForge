using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultInformationRestrictionModel : InformationRestrictionModel
{
	public bool IsDisabledByCheat;

	public override bool DoesPlayerKnowDetailsOf(Settlement settlement)
	{
		if (settlement.MapFaction == PartyBase.MainParty.MapFaction || settlement.IsInspected)
		{
			return true;
		}
		Settlement settlement2 = (settlement.IsVillage ? settlement.Village.Bound : settlement);
		if (!settlement2.IsFortification)
		{
			return true;
		}
		EmissaryModel emissaryModel = Campaign.Current.Models.EmissaryModel;
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			if (emissaryModel.IsEmissary(hero) && hero.CurrentSettlement == settlement2)
			{
				return true;
			}
		}
		foreach (Workshop ownedWorkshop in Hero.MainHero.OwnedWorkshops)
		{
			if (ownedWorkshop.Settlement == settlement2)
			{
				return true;
			}
		}
		foreach (Alley ownedAlley in Hero.MainHero.OwnedAlleys)
		{
			if (ownedAlley.Settlement == settlement2)
			{
				return true;
			}
		}
		if (IsDisabledByCheat)
		{
			return true;
		}
		return false;
	}

	public override bool DoesPlayerKnowDetailsOf(Hero hero)
	{
		if (hero.Clan != Clan.PlayerClan && !hero.IsDead && (hero.MapFaction == null || !hero.MapFaction.IsKingdomFaction || hero.MapFaction.Leader != hero) && !hero.IsKnownToPlayer)
		{
			return IsDisabledByCheat;
		}
		return true;
	}
}
