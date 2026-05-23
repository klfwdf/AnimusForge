using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBuildingConstructionModel : BuildingConstructionModel
{
	private const float HammerMultiplier = 0.01f;

	private const int VeryLowLoyaltyValue = 25;

	private const float MediumLoyaltyValue = 50f;

	private const float HighLoyaltyValue = 75f;

	private const float HighestLoyaltyValue = 100f;

	private static readonly TextObject ProductionFromMarketText = new TextObject("{=vaZDJGMx}Construction from Market");

	private static readonly TextObject BoostText = new TextObject("{=yX1RycON}Boost from Reserve");

	private static readonly TextObject HighLoyaltyBonusText = new TextObject("{=aSniKUJv}High Loyalty");

	private static readonly TextObject LowLoyaltyPenaltyText = new TextObject("{=SJ2qsRdF}Low Loyalty");

	private static readonly TextObject VeryLowLoyaltyPenaltyText = new TextObject("{=CcQzFnpN}Very Low Loyalty");

	private readonly TextObject CultureText = GameTexts.FindText("str_culture");

	public override int TownBoostCost => 500;

	public override int TownBoostBonus => 50;

	public override int CastleBoostCost => 250;

	public override int CastleBoostBonus => 20;

	public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		CalculateDailyConstructionPowerInternal(town, ref result);
		return result;
	}

	public override int CalculateDailyConstructionPowerWithoutBoost(Town town)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: false, null);
		return CalculateDailyConstructionPowerInternal(town, ref result, omitBoost: true);
	}

	public override int GetBoostAmount(Town town)
	{
		int num = (town.IsCastle ? CastleBoostBonus : TownBoostBonus);
		float num2 = 0f;
		if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Steward.Relocation))
		{
			num2 += DefaultPerks.Steward.Relocation.SecondaryBonus;
		}
		if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Trade.SpringOfGold))
		{
			num2 += DefaultPerks.Trade.SpringOfGold.SecondaryBonus;
		}
		return num + (int)((float)num * num2);
	}

	public override int GetBoostCost(Town town)
	{
		if (!town.IsCastle)
		{
			return TownBoostCost;
		}
		return CastleBoostCost;
	}

	private int CalculateDailyConstructionPowerInternal(Town town, ref ExplainedNumber result, bool omitBoost = false)
	{
		float value = town.Prosperity * 0.01f;
		result.Add(value, GameTexts.FindText("str_prosperity"));
		if (!omitBoost && town.BoostBuildingProcess > 0)
		{
			int num = (town.IsCastle ? CastleBoostCost : TownBoostCost);
			int boostAmount = GetBoostAmount(town);
			float num2 = MathF.Min(1f, (float)town.BoostBuildingProcess / (float)num);
			float num3 = 0f;
			if (town.IsTown && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.Clockwork))
			{
				num3 += DefaultPerks.Engineering.Clockwork.SecondaryBonus;
			}
			boostAmount += MathF.Round((float)boostAmount * num3);
			result.Add((float)boostAmount * num2, BoostText);
		}
		if (town.Governor != null && town.Governor.CurrentSettlement?.Town == town)
		{
			SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.TownProjectBuildingBonus, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.ForcedLabor, town, ref result);
		}
		if (town.Governor != null && town.Governor.CurrentSettlement?.Town == town && !town.BuildingsInProgress.IsEmpty())
		{
			if (town.Governor.GetPerkValue(DefaultPerks.Steward.ForcedLabor) && town.Settlement.Party.PrisonRoster.TotalManCount > 0)
			{
				float value2 = MathF.Min(0.3f, (float)town.Settlement.Party.PrisonRoster.TotalManCount / 3f * DefaultPerks.Steward.ForcedLabor.SecondaryBonus);
				result.AddFactor(value2, DefaultPerks.Steward.ForcedLabor.Name);
			}
			if (town.IsCastle && town.Governor.GetPerkValue(DefaultPerks.Engineering.MilitaryPlanner))
			{
				result.AddFactor(DefaultPerks.Engineering.MilitaryPlanner.SecondaryBonus, DefaultPerks.Engineering.MilitaryPlanner.Name);
			}
			else if (town.IsTown && town.Governor.GetPerkValue(DefaultPerks.Engineering.Carpenters))
			{
				result.AddFactor(DefaultPerks.Engineering.Carpenters.SecondaryBonus, DefaultPerks.Engineering.Carpenters.Name);
			}
			Building building = town.BuildingsInProgress.Peek();
			if ((building.BuildingType == DefaultBuildingTypes.SettlementFortifications || building.BuildingType == DefaultBuildingTypes.CastleBarracks || building.BuildingType == DefaultBuildingTypes.SettlementBarracks) && town.Governor.GetPerkValue(DefaultPerks.Engineering.Stonecutters))
			{
				result.AddFactor(DefaultPerks.Engineering.Stonecutters.PrimaryBonus, DefaultPerks.Engineering.Stonecutters.Name);
			}
		}
		int num4 = town.SoldItems.Sum((Town.SellLog x) => (x.Category.Properties == ItemCategory.Property.BonusToProduction) ? x.Number : 0);
		if (num4 > 0)
		{
			result.Add(0.25f * (float)num4, ProductionFromMarketText);
		}
		BuildingType buildingType = (town.BuildingsInProgress.IsEmpty() ? null : town.BuildingsInProgress.Peek().BuildingType);
		if (buildingType != null && buildingType.IsMilitaryProject)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Confidence, town, ref result);
		}
		if (buildingType == DefaultBuildingTypes.SettlementMarketplace)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.SelfMadeMan, town, ref result);
		}
		town.AddEffectOfBuildings(BuildingEffectEnum.ConstructionPerDay, ref result);
		if (town.Loyalty >= 75f)
		{
			float value3 = MBMath.Map(town.Loyalty, 75f, 100f, 0f, 0.2f);
			result.AddFactor(value3, HighLoyaltyBonusText);
		}
		else if (town.Loyalty > 25f && town.Loyalty <= 50f)
		{
			float num5 = MBMath.Map(town.Loyalty, 25f, 50f, 0.5f, 0f);
			result.AddFactor(0f - num5, LowLoyaltyPenaltyText);
		}
		else if (town.Loyalty <= 25f)
		{
			result.LimitMax(0f, VeryLowLoyaltyPenaltyText);
		}
		if (town.Loyalty > 25f && town.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianConstructionFeat))
		{
			result.AddFactor(DefaultCulturalFeats.BattanianConstructionFeat.EffectBonus, CultureText);
		}
		result.LimitMin(0f);
		return (int)result.ResultNumber;
	}
}
