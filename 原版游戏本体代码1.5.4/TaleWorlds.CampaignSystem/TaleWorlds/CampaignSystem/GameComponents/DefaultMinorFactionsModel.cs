using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMinorFactionsModel : MinorFactionsModel
{
	public override float DailyMinorFactionHeroSpawnChance => 0.1f;

	public override int MinorFactionHeroLimit => 4;

	public override int GetMercenaryAwardFactorToJoinKingdom(Clan mercenaryClan, Kingdom kingdom, bool neededAmountForClanToJoinCalculation = false)
	{
		float powerRatioToEnemies = FactionHelper.GetPowerRatioToEnemies(kingdom);
		float num = 1f;
		num = ((powerRatioToEnemies > 2f) ? 0.5f : ((powerRatioToEnemies > 1f) ? (1f - (powerRatioToEnemies - 1f) * 0.5f) : ((!(powerRatioToEnemies > 0.5f)) ? (1.5f + (0.5f - powerRatioToEnemies)) : (1f + (1f - powerRatioToEnemies)))));
		float num2 = MathF.Sqrt(FactionHelper.GetPowerRatioToTributePayedKingdoms(kingdom));
		float num3 = ((num2 > 1f) ? 1f : (2f - num2));
		float num4 = 1f + (float)mercenaryClan.Leader.Clan.Tier * 0.15f;
		int num5 = 0;
		float num6 = 0f;
		float num7 = 0f;
		foreach (Clan clan in kingdom.Clans)
		{
			if (!clan.IsUnderMercenaryService)
			{
				int num8 = ((clan.Leader != kingdom.Leader) ? 1 : 2);
				num6 += (float)(num8 * clan.Leader.Gold);
				num5 += num8;
				if (clan.Leader.Gold < 30000)
				{
					num7 += (float)num8 * (1f - (float)clan.Leader.Gold / 30000f);
				}
			}
		}
		int num9 = MathF.Min(1000000, kingdom.KingdomBudgetWallet);
		float num10 = ((num5 > 0) ? ((num6 + (float)num9 * 0.5f) / (float)num5) : 0f);
		float num11 = MathF.Min(3.5f, MathF.Pow((num10 + 2000f) / 10000f, 0.3f)) - 0.5f;
		float num12 = 1f - 0.8f * (num7 / (float)num5);
		int count = kingdom.Clans.Count;
		int num13 = 0;
		foreach (Clan clan2 in kingdom.Clans)
		{
			if (clan2.IsUnderMercenaryService)
			{
				num13++;
			}
		}
		float num14 = ((count < 10) ? (1f + (float)((10 - count) * (10 - count)) / 81f) : 1f);
		int num15 = 0;
		foreach (Town fief in kingdom.Fiefs)
		{
			num15 += ((!fief.IsTown) ? 1 : 2);
		}
		float num16 = ((num15 < 25) ? (1f + MathF.Min(1f, (float)((25 - num15) * (25 - num15)) * 0.003f)) : 1f);
		if (num16 > 1f && num < 1f)
		{
			float num17 = MathF.Pow(1f / num, 1f / num16);
			num *= num17;
		}
		if (num13 > 0)
		{
			if (num16 > 1f)
			{
				num16 = 1f + (num16 - 1f) / MathF.Sqrt(1 + num13);
			}
			if (num > 1f)
			{
				num = 1f + (num - 1f) / MathF.Pow(1 + num13, 0.33f);
			}
			if (num3 > 1f)
			{
				num3 = 1f + (num3 - 1f) / MathF.Sqrt(1 + num13);
			}
		}
		float num18 = ((kingdom.Leader == Hero.MainHero) ? 22f : 20f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 160000f) ? (2f * ((160000f - (float)MathF.Max(80000, mercenaryClan.Leader.Gold)) / 80000f)) : 0f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 80000f) ? (2f * ((80000f - (float)MathF.Max(40000, mercenaryClan.Leader.Gold)) / 40000f)) : 0f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 40000f) ? (2f * ((40000f - (float)MathF.Max(20000, mercenaryClan.Leader.Gold)) / 20000f)) : 0f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 20000f) ? (2f * ((20000f - (float)MathF.Max(10000, mercenaryClan.Leader.Gold)) / 10000f)) : 0f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 10000f) ? (2f * ((10000f - (float)MathF.Max(5000, mercenaryClan.Leader.Gold)) / 5000f)) : 0f);
		num18 -= ((mercenaryClan.Leader != null && (float)mercenaryClan.Leader.Gold < 5000f) ? (2f * ((5000f - (float)mercenaryClan.Leader.Gold) / 5000f)) : 0f);
		int relation = mercenaryClan.Leader.GetRelation(kingdom.Leader);
		float num19 = ((!neededAmountForClanToJoinCalculation) ? 1f : ((relation < 0) ? (1f + MathF.Sqrt(MathF.Abs(relation)) / 10f) : (1f - MathF.Sqrt(relation) / 20f)));
		int num20 = (neededAmountForClanToJoinCalculation ? ((int)(num4 * num18 * num19) * 10) : (MathF.Ceiling(MathF.Max(1f, MathF.Min(80f, num14 * num * num3 * num4 * num11 * num12 * num16 * 6f))) * 10));
		if (mercenaryClan.IsMinorFaction && kingdom != null && kingdom.Leader == Hero.MainHero && kingdom.Leader.GetPerkValue(DefaultPerks.Trade.ManOfMeans))
		{
			num20 = MathF.Round((float)num20 * (1f + DefaultPerks.Trade.ManOfMeans.PrimaryBonus));
		}
		if (mercenaryClan.Culture.HasFeat(DefaultCulturalFeats.VlandianRenownMercenaryFeat))
		{
			num20 += (int)((float)num20 * 0.15f);
		}
		return num20;
	}
}
