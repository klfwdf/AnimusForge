using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCBuildingConstructionModel : BuildingConstructionModel
{
	public override int TownBoostCost => base.BaseModel.TownBoostCost;

	public override int TownBoostBonus => base.BaseModel.TownBoostBonus;

	public override int CastleBoostCost => base.BaseModel.CastleBoostCost;

	public override int CastleBoostBonus => base.BaseModel.CastleBoostBonus;

	public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateDailyConstructionPower(town, includeDescriptions);
		Kingdom kingdom = town.OwnerClan?.Kingdom;
		if (kingdom != null && kingdom.HasPolicy(NavalPolicies.MaritimeWealEdict) && !town.Settlement.HasPort)
		{
			result.AddFactor(0.2f, NavalPolicies.MaritimeWealEdict.Name);
		}
		return result;
	}

	public override int CalculateDailyConstructionPowerWithoutBoost(Town town)
	{
		return base.BaseModel.CalculateDailyConstructionPowerWithoutBoost(town);
	}

	public override int GetBoostCost(Town town)
	{
		return base.BaseModel.GetBoostCost(town);
	}

	public override int GetBoostAmount(Town town)
	{
		return base.BaseModel.GetBoostAmount(town);
	}
}
