using System;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200014C RID: 332
	public class DefaultSettlementMilitiaModel : SettlementMilitiaModel
	{
		// Token: 0x060019E0 RID: 6624 RVA: 0x00081A71 File Offset: 0x0007FC71
		public override int MilitiaToSpawnAfterSiege(Town town)
		{
			return 2 * (45 + MBRandom.RandomInt(10));
		}

		// Token: 0x060019E1 RID: 6625 RVA: 0x00081A7F File Offset: 0x0007FC7F
		public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
		{
			return DefaultSettlementMilitiaModel.CalculateMilitiaChangeInternal(settlement, includeDescriptions);
		}

		// Token: 0x060019E2 RID: 6626 RVA: 0x00081A88 File Offset: 0x0007FC88
		public override ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement)
		{
			ExplainedNumber result = default(ExplainedNumber);
			Hero hero = null;
			if (settlement.IsFortification && settlement.Town.Governor != null)
			{
				hero = settlement.Town.Governor;
			}
			else if (settlement.IsVillage)
			{
				Settlement tradeBound = settlement.Village.TradeBound;
				if (((tradeBound != null) ? tradeBound.Town.Governor : null) != null)
				{
					hero = settlement.Village.TradeBound.Town.Governor;
				}
			}
			if (hero != null)
			{
				if (hero.GetPerkValue(DefaultPerks.Leadership.CitizenMilitia))
				{
					result.Add(DefaultPerks.Leadership.CitizenMilitia.PrimaryBonus, null, null);
				}
				if (hero.GetPerkValue(DefaultPerks.Polearm.Drills))
				{
					result.Add(DefaultPerks.Polearm.Drills.PrimaryBonus, null, null);
				}
				if (hero.GetPerkValue(DefaultPerks.Steward.SevenVeterans))
				{
					result.Add(DefaultPerks.Steward.SevenVeterans.PrimaryBonus, null, null);
				}
			}
			if (settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianMilitiaFeat))
			{
				result.Add(DefaultCulturalFeats.BattanianMilitiaFeat.EffectBonus, null, null);
			}
			if (settlement.IsFortification)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.MilitiaVeterancyChance, ref result);
			}
			if (settlement.OwnerClan.Kingdom != null && settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandGrantsForVeteran))
			{
				result.AddFactor(0.1f, null);
			}
			return result;
		}

		// Token: 0x060019E3 RID: 6627 RVA: 0x00081BD9 File Offset: 0x0007FDD9
		public override void CalculateMilitiaSpawnRate(Settlement settlement, out float meleeTroopRate, out float rangedTroopRate)
		{
			meleeTroopRate = 0.5f;
			rangedTroopRate = 1f - meleeTroopRate;
		}

		// Token: 0x060019E4 RID: 6628 RVA: 0x00081BEC File Offset: 0x0007FDEC
		private static ExplainedNumber CalculateMilitiaChangeInternal(Settlement settlement, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			float militia = settlement.Militia;
			if (settlement.IsFortification)
			{
				result.Add(2f, DefaultSettlementMilitiaModel.BaseText, null);
			}
			float value = -militia * 0.025f;
			result.Add(value, DefaultSettlementMilitiaModel.RetiredText, null);
			if (settlement.IsVillage)
			{
				float value2 = settlement.Village.Hearth / 400f;
				result.Add(value2, DefaultSettlementMilitiaModel.FromHearthsText, null);
			}
			else if (settlement.IsFortification)
			{
				float num = settlement.Town.Prosperity / 1000f;
				result.Add(num, DefaultSettlementMilitiaModel.FromProsperityText, null);
				if (settlement.Town.InRebelliousState)
				{
					float num2 = MBMath.Map(settlement.Town.Loyalty, 0f, (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold, (float)Campaign.Current.Models.SettlementLoyaltyModel.MilitiaBoostPercentage, 0f);
					float value3 = MathF.Abs(num * (num2 * 0.01f));
					result.Add(value3, DefaultSettlementMilitiaModel.LowLoyaltyText, null);
				}
			}
			if (settlement.IsTown)
			{
				int num3 = settlement.Town.SoldItems.Sum(delegate(Town.SellLog x)
				{
					if (x.Category.Properties != ItemCategory.Property.BonusToMilitia)
					{
						return 0;
					}
					return x.Number;
				});
				if (num3 > 0)
				{
					result.Add(0.2f * (float)num3, DefaultSettlementMilitiaModel.MilitiaFromMarketText, null);
				}
				if (settlement.OwnerClan.Kingdom != null)
				{
					if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Serfdom) && settlement.IsTown)
					{
						result.Add(-1f, DefaultPolicies.Serfdom.Name, null);
					}
					if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
					{
						result.Add(1f, DefaultPolicies.Cantons.Name, null);
					}
				}
				if (settlement.OwnerClan.Culture.HasFeat(DefaultCulturalFeats.BattanianMilitiaFeat))
				{
					result.Add(DefaultCulturalFeats.BattanianMilitiaFeat.EffectBonus, DefaultSettlementMilitiaModel.CultureText, null);
				}
			}
			if (settlement.IsCastle || settlement.IsTown)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.Militia, ref result);
				if (settlement.IsCastle && settlement.Town.InRebelliousState)
				{
					settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.MilitiaReduction, ref result);
				}
				DefaultSettlementMilitiaModel.GetSettlementMilitiaChangeDueToPolicies(settlement, ref result);
				DefaultSettlementMilitiaModel.GetSettlementMilitiaChangeDueToPerks(settlement, ref result);
				DefaultSettlementMilitiaModel.GetSettlementMilitiaChangeDueToIssues(settlement, ref result);
			}
			return result;
		}

		// Token: 0x060019E5 RID: 6629 RVA: 0x00081E64 File Offset: 0x00080064
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

		// Token: 0x060019E6 RID: 6630 RVA: 0x00081F08 File Offset: 0x00080108
		private static void GetSettlementMilitiaChangeDueToPolicies(Settlement settlement, ref ExplainedNumber result)
		{
			Kingdom kingdom = settlement.OwnerClan.Kingdom;
			if (kingdom != null && kingdom.ActivePolicies.Contains(DefaultPolicies.Citizenship))
			{
				result.Add(1f, DefaultPolicies.Citizenship.Name, null);
			}
		}

		// Token: 0x060019E7 RID: 6631 RVA: 0x00081F4C File Offset: 0x0008014C
		private static void GetSettlementMilitiaChangeDueToIssues(Settlement settlement, ref ExplainedNumber result)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementMilitia, settlement, ref result);
		}

		// Token: 0x04000894 RID: 2196
		private static readonly TextObject BaseText = new TextObject("{=militarybase}Base", null);

		// Token: 0x04000895 RID: 2197
		private static readonly TextObject FromHearthsText = new TextObject("{=ecdZglky}From Hearths", null);

		// Token: 0x04000896 RID: 2198
		private static readonly TextObject FromProsperityText = new TextObject("{=cTmiNAlI}From Prosperity", null);

		// Token: 0x04000897 RID: 2199
		private static readonly TextObject RetiredText = new TextObject("{=gHnfFi1s}Retired", null);

		// Token: 0x04000898 RID: 2200
		private static readonly TextObject MilitiaFromMarketText = new TextObject("{=7ve3bQxg}Weapons From Market", null);

		// Token: 0x04000899 RID: 2201
		private static readonly TextObject LowLoyaltyText = new TextObject("{=SJ2qsRdF}Low Loyalty", null);

		// Token: 0x0400089A RID: 2202
		private static readonly TextObject CultureText = GameTexts.FindText("str_culture", null);

		// Token: 0x0400089B RID: 2203
		private const int AutoSpawnMilitiaDayMultiplierAfterSiege = 25;

		// Token: 0x0400089C RID: 2204
		private const int BaseMilitiaChange = 2;
	}
}
