using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200014A RID: 330
	public class DefaultSettlementGarrisonModel : SettlementGarrisonModel
	{
		// Token: 0x060019B2 RID: 6578 RVA: 0x00080D4E File Offset: 0x0007EF4E
		public override int GetMaximumDailyAutoRecruitmentCount(Town town)
		{
			return 1;
		}

		// Token: 0x060019B3 RID: 6579 RVA: 0x00080D54 File Offset: 0x0007EF54
		public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			if ((settlement.IsTown || settlement.IsCastle) && settlement.OwnerClan.IsRebelClan && (settlement.OwnerClan.MapFaction == null || !settlement.OwnerClan.MapFaction.IsKingdomFaction))
			{
				result.Add(2f, this.RebellionText, null);
			}
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementGarrison, settlement, ref result);
			return result;
		}

		// Token: 0x060019B4 RID: 6580 RVA: 0x00080DDC File Offset: 0x0007EFDC
		public override int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float defaultIdealGarrisonStrengthPerWalledCenter = 0f)
		{
			MobileParty garrisonParty = settlement.Town.GarrisonParty;
			if (garrisonParty != null)
			{
				float num = garrisonParty.Party.CalculateCurrentStrength();
				float num2;
				if (garrisonParty.HasLimitedWage())
				{
					num2 = (float)garrisonParty.PaymentLimit / Campaign.Current.AverageWage;
					num2 /= 1.5f;
				}
				else
				{
					num2 = ((defaultIdealGarrisonStrengthPerWalledCenter > 0.1f) ? defaultIdealGarrisonStrengthPerWalledCenter : FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mobileParty.MapFaction as Kingdom, settlement.OwnerClan));
					float num3 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
					num2 *= num3;
					num2 *= (settlement.IsTown ? 2f : 1f);
				}
				float partySizeLimit = (float)mobileParty.Party.PartySizeLimit;
				int numberOfAllMembers = mobileParty.Party.NumberOfAllMembers;
				float num4 = partySizeLimit / (float)numberOfAllMembers;
				float num5 = MathF.Min(11f, num4 * MathF.Sqrt(num4)) - 1f;
				float num6 = MathF.Pow(num / num2, 1.5f);
				float num7 = ((mobileParty.LeaderHero.Clan.Leader == mobileParty.LeaderHero) ? 2f : 1f);
				int num8 = 0;
				if (num5 * num6 * num7 > 1f)
				{
					num8 = MBRandom.RoundRandomized(num5 * num6 * num7);
				}
				int num9 = 25;
				num9 *= (settlement.IsTown ? 2 : 1);
				if (num8 > garrisonParty.Party.MemberRoster.TotalRegulars - num9)
				{
					num8 = garrisonParty.Party.MemberRoster.TotalRegulars - num9;
				}
				return num8;
			}
			return 0;
		}

		// Token: 0x060019B5 RID: 6581 RVA: 0x00080F58 File Offset: 0x0007F158
		public override int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement)
		{
			MobileParty garrisonParty = settlement.Town.GarrisonParty;
			float num = 0f;
			if (garrisonParty != null)
			{
				num = garrisonParty.Party.CalculateCurrentStrength();
			}
			float num2;
			if (garrisonParty != null && garrisonParty.HasLimitedWage())
			{
				num2 = (float)garrisonParty.PaymentLimit / Campaign.Current.AverageWage;
			}
			else
			{
				num2 = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(mobileParty.MapFaction as Kingdom, settlement.OwnerClan);
				float num3 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
				float num4 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement.Town);
				float num5 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
				num2 *= num3;
				num2 *= num4;
				num2 *= num5;
			}
			if (num < num2)
			{
				int numberOfRegularMembers = mobileParty.Party.NumberOfRegularMembers;
				float num6 = 1f + (float)mobileParty.Party.MemberRoster.TotalWoundedRegulars / (float)mobileParty.Party.NumberOfRegularMembers;
				int partySizeLimit = mobileParty.Party.PartySizeLimit;
				float num7 = MathF.Pow(MathF.Min(2f, (float)numberOfRegularMembers / (float)partySizeLimit), 1.2f) * 0.75f;
				float num8 = (1f - num / num2) * (1f - num / num2);
				float num9 = 1f;
				if (mobileParty.Army != null)
				{
					num8 = MathF.Min(num8, 0.7f);
					num9 = 0.3f + mobileParty.Army.CalculateCurrentStrength() / mobileParty.Party.CalculateCurrentStrength() * 0.025f;
				}
				float num10 = (settlement.Town.IsOwnerUnassigned ? 0.75f : 0.5f);
				if (settlement.OwnerClan == mobileParty.LeaderHero.Clan || settlement.OwnerClan == mobileParty.Party.Owner.MapFaction.Leader.Clan)
				{
					num10 = 1f;
				}
				float num11 = MathF.Min(0.7f, num7 * num8 * num10 * num6 * num9);
				if ((float)numberOfRegularMembers * num11 > 1f)
				{
					return MBRandom.RoundRandomized((float)numberOfRegularMembers * num11);
				}
			}
			return 0;
		}

		// Token: 0x060019B6 RID: 6582 RVA: 0x00081140 File Offset: 0x0007F340
		public override float GetMaximumDailyRepairAmount(Settlement settlement)
		{
			if (!settlement.IsUnderSiege)
			{
				if (!settlement.SettlementWallSectionHitPointsRatioList.All((float ratio) => ratio >= 1f))
				{
					ExplainedNumber explainedNumber = new ExplainedNumber(settlement.MaxHitPointsOfOneWallSection * (float)settlement.WallSectionCount * 0.02f, false, null);
					if (settlement.IsFortification)
					{
						settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.WallRepairSpeed, ref explainedNumber);
					}
					return explainedNumber.ResultNumber;
				}
			}
			return 0f;
		}

		// Token: 0x04000879 RID: 2169
		private static readonly TextObject TownWallsText = new TextObject("{=SlmhqqH8}Town Walls", null);

		// Token: 0x0400087A RID: 2170
		private static readonly TextObject MoraleText = new TextObject("{=UjL7jVYF}Morale", null);

		// Token: 0x0400087B RID: 2171
		private static readonly TextObject FoodShortageText = new TextObject("{=qTFKvGSg}Food Shortage", null);

		// Token: 0x0400087C RID: 2172
		private readonly TextObject SurplusFoodText = GameTexts.FindText("str_surplus_food", null);

		// Token: 0x0400087D RID: 2173
		private readonly TextObject VillageBeingRaided = GameTexts.FindText("str_village_being_raided", null);

		// Token: 0x0400087E RID: 2174
		private readonly TextObject VillageLooted = GameTexts.FindText("str_village_looted", null);

		// Token: 0x0400087F RID: 2175
		private readonly TextObject TownIsUnderSiege = GameTexts.FindText("str_villages_under_siege", null);

		// Token: 0x04000880 RID: 2176
		private readonly TextObject RetiredText = GameTexts.FindText("str_retired", null);

		// Token: 0x04000881 RID: 2177
		private readonly TextObject PaymentIsLessText = GameTexts.FindText("str_payment_is_less", null);

		// Token: 0x04000882 RID: 2178
		private readonly TextObject UnpaidWagesText = GameTexts.FindText("str_unpaid_wages", null);

		// Token: 0x04000883 RID: 2179
		private readonly TextObject RebellionText = GameTexts.FindText("str_rebel_settlement", null);

		// Token: 0x04000884 RID: 2180
		private const int MaximumDailyAutoRecruitmentCount = 1;
	}
}
