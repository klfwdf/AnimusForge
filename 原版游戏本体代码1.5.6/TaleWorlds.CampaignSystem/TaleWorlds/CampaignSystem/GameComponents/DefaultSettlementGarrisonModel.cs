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

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementGarrisonModel : SettlementGarrisonModel
{
	private readonly TextObject RebellionText = GameTexts.FindText("str_rebel_settlement");

	private const int MaximumDailyAutoRecruitmentCount = 1;

	public override ExplainedNumber GetMaximumDailyAutoRecruitmentCount(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(1f, includeDescriptions);
		town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonAutoRecruitment, ref result);
		return result;
	}

	public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		if ((settlement.IsTown || settlement.IsCastle) && settlement.OwnerClan.IsRebelClan && (settlement.OwnerClan.MapFaction == null || !settlement.OwnerClan.MapFaction.IsKingdomFaction))
		{
			explainedNumber.Add(2f, RebellionText);
		}
		Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementGarrison, settlement, ref explainedNumber);
		return explainedNumber;
	}

	public override int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float defaultIdealGarrisonStrengthPerWalledCenter = 0f)
	{
		MobileParty garrisonParty = settlement.Town.GarrisonParty;
		if (garrisonParty == null)
		{
			return 0;
		}
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
		int partySizeLimit = mobileParty.Party.PartySizeLimit;
		int numberOfAllMembers = mobileParty.Party.NumberOfAllMembers;
		float num4 = (float)partySizeLimit / (float)numberOfAllMembers;
		float num5 = MathF.Min(11f, num4 * MathF.Sqrt(num4)) - 1f;
		float num6 = MathF.Pow(num / num2, 1.5f);
		float num7 = ((mobileParty.LeaderHero.Clan.Leader == mobileParty.LeaderHero) ? 2f : 1f);
		int num8 = 0;
		if (num5 * num6 * num7 > 1f)
		{
			num8 = MBRandom.RoundRandomized(num5 * num6 * num7);
		}
		int num9 = 25;
		num9 *= ((!settlement.IsTown) ? 1 : 2);
		if (num8 > garrisonParty.Party.MemberRoster.TotalRegulars - num9)
		{
			num8 = garrisonParty.Party.MemberRoster.TotalRegulars - num9;
		}
		return num8;
	}

	public override float GetMaximumDailyRepairAmount(Settlement settlement)
	{
		if (settlement.IsUnderSiege || settlement.SettlementWallSectionHitPointsRatioList.All((float ratio) => ratio >= 1f))
		{
			return 0f;
		}
		ExplainedNumber result = new ExplainedNumber(settlement.MaxHitPointsOfOneWallSection * (float)settlement.WallSectionCount * 0.04f);
		if (settlement.IsFortification)
		{
			settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.WallRepairSpeed, ref result);
		}
		return result.ResultNumber;
	}
}
