using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCSettlementProsperityModel : SettlementProsperityModel
{
	public override ExplainedNumber CalculateProsperityChange(Town fortification, bool includeDescriptions = false)
	{
		return base.BaseModel.CalculateProsperityChange(fortification, includeDescriptions);
	}

	public override ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false)
	{
		ExplainedNumber bonuses = base.BaseModel.CalculateHearthChange(village, includeDescriptions);
		if (village.Bound.HasPort && village.Bound.IsFortification)
		{
			PerkHelper.AddPerkBonusForTown(NavalPerks.Shipmaster.FairWinds, village.Bound.Town, isPrimaryBonus: false, ref bonuses);
		}
		return bonuses;
	}
}
