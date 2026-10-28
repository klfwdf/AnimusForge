using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalFerryModel : FerryModel
{
	public override int MaximumFerryCapacityForPassengers => 50;

	public override ExplainedNumber GetFerryCost(Settlement departureVillage, bool includeReason = false)
	{
		float baseNumber = 500f;
		ExplainedNumber result = new ExplainedNumber(baseNumber, includeReason);
		if (departureVillage.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			result.AddFactor(2f, new TextObject("{=qZvPOkb4}Departing from enemy village."));
		}
		if (departureVillage.FerryTarget.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			result.AddFactor(2f, new TextObject("{=gROUTRVu}Arriving at enemy village."));
		}
		float num = MobileParty.MainParty.MemberRoster.TotalManCount + MobileParty.MainParty.PrisonRoster.TotalManCount;
		int maximumFerryCapacityForPassengers = Campaign.Current.Models.FerryModel.MaximumFerryCapacityForPassengers;
		float value = num / (float)maximumFerryCapacityForPassengers;
		result.AddFactor(value, new TextObject("{=wwS4BHsv}Number of passenger affect."));
		return result;
	}
}
