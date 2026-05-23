using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementMilitiaModel : SettlementMilitiaModel
{
	private static readonly TextObject BaseText = new TextObject("{=militarybase}Base");

	private static readonly TextObject FromHearthsText = new TextObject("{=ecdZglky}From Hearths");

	private static readonly TextObject FromProsperityText = new TextObject("{=cTmiNAlI}From Prosperity");

	private static readonly TextObject RetiredText = new TextObject("{=gHnfFi1s}Retired");

	private static readonly TextObject MilitiaFromMarketText = new TextObject("{=7ve3bQxg}Weapons From Market");

	private static readonly TextObject LowLoyaltyText = new TextObject("{=SJ2qsRdF}Low Loyalty");

	private static readonly TextObject CultureText = GameTexts.FindText("str_culture");

	private const int AutoSpawnMilitiaDayMultiplierAfterSiege = 25;

	private const int BaseFortificationMilitiaChange = 2;

	private const float BaseVillageMilitiaChange = 0.5f;

	public override int MilitiaToSpawnAfterSiege(Town town)
	{
		return 2 * (45 + MBRandom.RandomInt(10));
	}

	public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
	{
		return CalculateMilitiaChangeInternal(settlement, includeDescriptions);
	}

	public override ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement)
	{
		ExplainedNumber result = default(ExplainedNumber);
		Hero hero = null;
		if (settlement.IsFortification && settlement.Town.Governor != null)
		{
			hero = settlement.Town.Governor;
		}
		else if (settlement.IsVillage && settlement.Village.TradeBound?.Town.Governor != null)
		{
			hero = settlement.Village.TradeBound.Town.Governor;
		}
		if (hero != null)
		{
			if (hero.GetPerkValue(DefaultPerks.Leadership.CitizenMilitia))
			{
				result.Add(DefaultPerks.Leadership.CitizenMilitia.PrimaryBonus);
			}
			if (hero.GetPerkValue(DefaultPerks.Polearm.Drills))
			{
				result.Add(DefaultPerks.Polearm.Drills.PrimaryBonus);
			}
			if (hero.GetPerkValue(DefaultPerks.Steward.SevenVeterans))
			{
				result.Add(DefaultPerks.Steward.SevenVeterans.PrimaryBonus);
			}
		}
		if (settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianMilitiaFeat))
		{
			result.Add(DefaultCulturalFeats.BattanianMilitiaFeat.EffectBonus);
		}
		if (settlement.IsFortification)
		{
			settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.MilitiaVeterancyChance, ref result);
		}
		if (settlement.OwnerClan.Kingdom != null && settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandGrantsForVeteran))
		{
			result.AddFactor(0.1f);
		}
		return result;
	}

	public override void CalculateMilitiaSpawnRate(Settlement settlement, out float meleeTroopRate, out float rangedTroopRate)
	{
		meleeTroopRate = 0.5f;
		rangedTroopRate = 1f - meleeTroopRate;
	}

	private static ExplainedNumber CalculateMilitiaChangeInternal(Settlement settlement, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		float militia = settlement.Militia;
		if (settlement.IsFortification)
		{
			result.Add(2f, BaseText);
		}
		else if (settlement.IsVillage)
		{
			result.Add(0.5f, BaseText);
		}
		float value = (0f - militia) * 0.025f;
		result.Add(value, RetiredText);
		if (settlement.IsVillage)
		{
			float value2 = settlement.Village.Hearth / 400f;
			result.Add(value2, FromHearthsText);
		}
		else if (settlement.IsFortification)
		{
			float num = settlement.Town.Prosperity / 1000f;
			result.Add(num, FromProsperityText);
			if (settlement.Town.InRebelliousState)
			{
				float num2 = MBMath.Map(settlement.Town.Loyalty, 0f, Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold, Campaign.Current.Models.SettlementLoyaltyModel.MilitiaBoostPercentage, 0f);
				float value3 = MathF.Abs(num * (num2 * 0.01f));
				result.Add(value3, LowLoyaltyText);
			}
		}
		if (settlement.IsTown)
		{
			int num3 = settlement.Town.SoldItems.Sum((Town.SellLog x) => (x.Category.Properties == ItemCategory.Property.BonusToMilitia) ? x.Number : 0);
			if (num3 > 0)
			{
				result.Add(0.2f * (float)num3, MilitiaFromMarketText);
			}
			if (settlement.OwnerClan.Kingdom != null)
			{
				if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Serfdom) && settlement.IsTown)
				{
					result.Add(-1f, DefaultPolicies.Serfdom.Name);
				}
				if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
				{
					result.Add(1f, DefaultPolicies.Cantons.Name);
				}
			}
			if (settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianMilitiaFeat))
			{
				result.Add(DefaultCulturalFeats.BattanianMilitiaFeat.EffectBonus, CultureText);
			}
		}
		if (settlement.IsCastle || settlement.IsTown)
		{
			settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.Militia, ref result);
			if (settlement.IsCastle && settlement.Town.InRebelliousState)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.MilitiaReduction, ref result);
			}
			GetSettlementMilitiaChangeDueToPolicies(settlement, ref result);
			GetSettlementMilitiaChangeDueToPerks(settlement, ref result);
			GetSettlementMilitiaChangeDueToIssues(settlement, ref result);
		}
		return result;
	}

	private static void GetSettlementMilitiaChangeDueToPerks(Settlement settlement, ref ExplainedNumber result)
	{
		if (settlement.Town != null && settlement.Town.Governor != null)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.SwiftStrike, settlement.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.KeepAtBay, settlement.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.MerryMen, settlement.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Crossbow.LongShots, settlement.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Throwing.SlingingCompetitions, settlement.Town, ref result);
			if (settlement.IsUnderSiege)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.ArmsDealer, settlement.Town, ref result);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.SevenVeterans, settlement.Town, ref result);
		}
	}

	private static void GetSettlementMilitiaChangeDueToPolicies(Settlement settlement, ref ExplainedNumber result)
	{
		Kingdom kingdom = settlement.OwnerClan.Kingdom;
		if (kingdom != null && kingdom.ActivePolicies.Contains(DefaultPolicies.Citizenship))
		{
			result.Add(1f, DefaultPolicies.Citizenship.Name);
		}
	}

	private static void GetSettlementMilitiaChangeDueToIssues(Settlement settlement, ref ExplainedNumber result)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementMilitia, settlement, ref result);
	}
}
