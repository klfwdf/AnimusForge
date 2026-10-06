using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCSettlementMilitiaModel : SettlementMilitiaModel
{
	public override int MilitiaToSpawnAfterSiege(Town town)
	{
		return base.BaseModel.MilitiaToSpawnAfterSiege(town);
	}

	public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
	{
		ExplainedNumber bonuses = base.BaseModel.CalculateMilitiaChange(settlement, includeDescriptions);
		if (settlement.IsTown && settlement.HasPort)
		{
			PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.AccuracyTraining, settlement.Town, isPrimaryBonus: false, ref bonuses);
		}
		else if (settlement.IsVillage)
		{
			Town town = settlement.Village.Bound.Town;
			if (town != null && town.Settlement.HasPort)
			{
				PerkHelper.AddPerkBonusForTown(NavalPerks.Boatswain.AccuracyTraining, town, isPrimaryBonus: false, ref bonuses);
			}
		}
		Kingdom kingdom = settlement.OwnerClan?.Kingdom;
		if (kingdom != null && kingdom.HasPolicy(NavalPolicies.BolsterTheFyrd))
		{
			bonuses.AddFactor(0.25f, NavalPolicies.BolsterTheFyrd.Name);
		}
		return bonuses;
	}

	public override ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement)
	{
		ExplainedNumber bonuses = base.BaseModel.CalculateVeteranMilitiaSpawnChance(settlement);
		if (settlement.IsTown && settlement.HasPort)
		{
			PerkHelper.AddPerkBonusForTown(NavalPerks.Mariner.NavalFightingTraining, settlement.Town, isPrimaryBonus: false, ref bonuses);
		}
		if (settlement.IsVillage && settlement.Village.Bound.HasPort)
		{
			PerkHelper.AddPerkBonusForTown(NavalPerks.Mariner.NavalFightingTraining, settlement.Village.Bound.Town, isPrimaryBonus: false, ref bonuses);
		}
		return bonuses;
	}

	public override void CalculateMilitiaSpawnRate(Settlement settlement, out float meleeTroopRate, out float rangedTroopRate)
	{
		base.BaseModel.CalculateMilitiaSpawnRate(settlement, out meleeTroopRate, out rangedTroopRate);
	}
}
