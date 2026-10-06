using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCClanFinanceModel : ClanFinanceModel
{
	private const int payGarrisonWagesTreshold = 8000;

	private const int payClanPartiesTreshold = 4000;

	public override int PartyGoldLowerThreshold => base.BaseModel.PartyGoldLowerThreshold;

	public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		ExplainedNumber explainedNumber = base.BaseModel.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals, includeDetails);
		if (clan.Kingdom != null && clan.Kingdom.HasPolicy(NavalPolicies.CoastalGuardEdict))
		{
			ExplainedNumber explainedNumber2 = new ExplainedNumber(0f, includeDescriptions: false, null);
			foreach (Town fief in clan.Fiefs)
			{
				if (fief.Settlement.HasPort && fief.GarrisonParty != null && fief.GarrisonParty.IsActive)
				{
					int num = AddPartyExpense(fief.GarrisonParty, clan, explainedNumber, applyWithdrawals);
					explainedNumber2.Add(num);
				}
			}
			explainedNumber.Add(explainedNumber2.ResultNumber * -0.15f, NavalPolicies.CoastalGuardEdict.Name);
		}
		return explainedNumber;
	}

	public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		return base.BaseModel.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);
	}

	public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		ExplainedNumber explainedNumber = base.BaseModel.CalculateClanExpenses(clan, includeDescriptions, applyWithdrawals, includeDetails);
		if (clan.Kingdom != null && clan.Kingdom.HasPolicy(NavalPolicies.CoastalGuardEdict))
		{
			ExplainedNumber explainedNumber2 = new ExplainedNumber(0f, includeDescriptions: false, null);
			foreach (Town fief in clan.Fiefs)
			{
				if (fief.Settlement.HasPort && fief.GarrisonParty != null && fief.GarrisonParty.IsActive)
				{
					int num = AddPartyExpense(fief.GarrisonParty, clan, explainedNumber, applyWithdrawals);
					explainedNumber2.Add(num);
				}
			}
			explainedNumber.Add(explainedNumber2.ResultNumber * 0.15f, NavalPolicies.CoastalGuardEdict.Name);
		}
		return explainedNumber;
	}

	public override ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false)
	{
		ExplainedNumber result = base.BaseModel.CalculateTownIncomeFromTariffs(clan, town, applyWithdrawals);
		if (clan.Kingdom != null && clan.Kingdom.HasPolicy(NavalPolicies.ArsenalDepositoryAct))
		{
			result.AddFactor(-0.1f, NavalPolicies.ArsenalDepositoryAct.Name);
		}
		return result;
	}

	public override int CalculateTownIncomeFromProjects(Town town)
	{
		return base.BaseModel.CalculateTownIncomeFromProjects(town);
	}

	public override int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals)
	{
		return base.BaseModel.CalculateNotableDailyGoldChange(hero, applyWithdrawals);
	}

	public override int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false)
	{
		return base.BaseModel.CalculateVillageIncome(clan, village, applyWithdrawals);
	}

	public override int CalculateOwnerIncomeFromCaravan(MobileParty caravan)
	{
		return base.BaseModel.CalculateOwnerIncomeFromCaravan(caravan);
	}

	public override int CalculateOwnerIncomeFromWorkshop(Workshop workshop)
	{
		return base.BaseModel.CalculateOwnerIncomeFromWorkshop(workshop);
	}

	public override float RevenueSmoothenFraction()
	{
		return base.BaseModel.RevenueSmoothenFraction();
	}

	private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = clan.Gold + (int)goldChange.ResultNumber;
		int num2 = num;
		if (num < (party.IsGarrison ? 8000 : 4000) && applyWithdrawals && clan != Clan.PlayerClan)
		{
			num2 = ((party.LeaderHero != null && party.PartyTradeGold < 500) ? MathF.Min(num, 250) : 0);
		}
		int num3 = CalculatePartyWage(party, num2, applyWithdrawals);
		int partyTradeGold = party.PartyTradeGold;
		if (applyWithdrawals)
		{
			if (party.IsLordParty && party.LeaderHero == null)
			{
				party.ActualClan.Leader.Gold -= num3;
			}
			else
			{
				party.PartyTradeGold -= num3;
			}
		}
		partyTradeGold -= num3;
		if (partyTradeGold < PartyGoldLowerThreshold)
		{
			int num4 = PartyGoldLowerThreshold - partyTradeGold;
			if (party.IsLordParty && party.LeaderHero == null)
			{
				num4 = num3;
			}
			if (applyWithdrawals)
			{
				num4 = MathF.Min(num4, num2);
				party.PartyTradeGold += num4;
			}
			return -num4;
		}
		return 0;
	}

	private static int CalculatePartyWage(MobileParty mobileParty, int budget, bool applyWithdrawals)
	{
		int totalWage = mobileParty.TotalWage;
		int num = totalWage;
		if (applyWithdrawals)
		{
			num = MathF.Min(totalWage, budget);
			ApplyMoraleEffect(mobileParty, totalWage, num);
		}
		return num;
	}

	private static void ApplyMoraleEffect(MobileParty mobileParty, int wage, int paymentAmount)
	{
		if (paymentAmount < wage && wage > 0)
		{
			float num = 1f - (float)paymentAmount / (float)wage;
			float num2 = (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * num;
			if (mobileParty.HasUnpaidWages < num)
			{
				num2 += (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * (num - mobileParty.HasUnpaidWages);
			}
			mobileParty.RecentEventsMorale += num2;
			mobileParty.HasUnpaidWages = num;
			MBTextManager.SetTextVariable("reg1", MathF.Round(MathF.Abs(num2), 1));
			if (mobileParty == MobileParty.MainParty)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_party_loses_moral_due_to_insufficent_funds"));
			}
		}
		else
		{
			mobileParty.HasUnpaidWages = 0f;
		}
	}
}
