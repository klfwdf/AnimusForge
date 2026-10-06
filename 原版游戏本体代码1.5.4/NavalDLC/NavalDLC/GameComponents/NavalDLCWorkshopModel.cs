using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCWorkshopModel : WorkshopModel
{
	public override int DaysForPlayerSaveWorkshopFromBankruptcy => base.BaseModel.DaysForPlayerSaveWorkshopFromBankruptcy;

	public override int CapitalLowLimit => base.BaseModel.CapitalLowLimit;

	public override int InitialCapital => base.BaseModel.InitialCapital;

	public override int DailyExpense => base.BaseModel.DailyExpense;

	public override int WarehouseCapacity => base.BaseModel.WarehouseCapacity;

	public override int DefaultWorkshopCountInSettlement => base.BaseModel.DefaultWorkshopCountInSettlement;

	public override int MaximumWorkshopsPlayerCanHave => base.BaseModel.MaximumWorkshopsPlayerCanHave;

	public override int GetMaxWorkshopCountForClanTier(int tier)
	{
		return base.BaseModel.GetMaxWorkshopCountForClanTier(tier);
	}

	public override int GetCostForPlayer(Workshop workshop)
	{
		return base.BaseModel.GetCostForPlayer(workshop);
	}

	public override int GetCostForNotable(Workshop workshop)
	{
		return base.BaseModel.GetCostForNotable(workshop);
	}

	public override Hero GetNotableOwnerForWorkshop(Workshop workshop)
	{
		return base.BaseModel.GetNotableOwnerForWorkshop(workshop);
	}

	public override ExplainedNumber GetEffectiveConversionSpeedOfProduction(Workshop workshop, float speed, bool includeDescriptions)
	{
		ExplainedNumber effectiveConversionSpeedOfProduction = base.BaseModel.GetEffectiveConversionSpeedOfProduction(workshop, speed, includeDescriptions);
		Kingdom kingdom = workshop.Owner.Clan?.Kingdom;
		if (kingdom != null)
		{
			if (kingdom.HasPolicy(NavalPolicies.RoyalNavyPrerogative) && (workshop.WorkshopType.StringId == "smithy" || workshop.WorkshopType.StringId == "wood_WorkshopType"))
			{
				effectiveConversionSpeedOfProduction.AddFactor(-0.05f, NavalPolicies.RoyalNavyPrerogative.Name);
			}
			if (kingdom.HasPolicy(NavalPolicies.MaritimeWealEdict) && workshop.Settlement.HasPort)
			{
				effectiveConversionSpeedOfProduction.AddFactor(0.25f, NavalPolicies.MaritimeWealEdict.Name);
			}
		}
		return effectiveConversionSpeedOfProduction;
	}

	public override int GetConvertProductionCost(WorkshopType workshopType)
	{
		return base.BaseModel.GetConvertProductionCost(workshopType);
	}

	public override bool CanPlayerSellWorkshop(Workshop workshop, out TextObject explanation)
	{
		return base.BaseModel.CanPlayerSellWorkshop(workshop, out explanation);
	}

	public override float GetTradeXpPerWarehouseProduction(EquipmentElement production)
	{
		return base.BaseModel.GetTradeXpPerWarehouseProduction(production);
	}
}
