using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000102 RID: 258
	public class DefaultClanFinanceModel : ClanFinanceModel
	{
		// Token: 0x17000639 RID: 1593
		// (get) Token: 0x060016C8 RID: 5832 RVA: 0x00067F94 File Offset: 0x00066194
		public override int PartyGoldLowerThreshold
		{
			get
			{
				return 5000;
			}
		}

		// Token: 0x060016C9 RID: 5833 RVA: 0x00067F9C File Offset: 0x0006619C
		public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateClanIncomeInternal(clan, ref result, applyWithdrawals, includeDetails);
			this.CalculateClanExpensesInternal(clan, ref result, applyWithdrawals, includeDetails);
			return result;
		}

		// Token: 0x060016CA RID: 5834 RVA: 0x00067FD0 File Offset: 0x000661D0
		public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateClanIncomeInternal(clan, ref result, applyWithdrawals, includeDetails);
			return result;
		}

		// Token: 0x060016CB RID: 5835 RVA: 0x00067FF8 File Offset: 0x000661F8
		private void CalculateClanIncomeInternal(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false, bool includeDetails = false)
		{
			if (clan.IsEliminated)
			{
				return;
			}
			Kingdom kingdom = clan.Kingdom;
			if (((kingdom != null) ? kingdom.RulingClan : null) == clan)
			{
				this.AddRulingClanIncome(clan, ref goldChange, applyWithdrawals, includeDetails);
			}
			if (clan != Clan.PlayerClan && (!clan.MapFaction.IsKingdomFaction || clan.IsUnderMercenaryService) && clan.Fiefs.Count == 0)
			{
				int num = clan.Tier * (80 + (clan.IsUnderMercenaryService ? 40 : 0));
				goldChange.Add((float)num, null, null);
			}
			this.AddMercenaryIncome(clan, ref goldChange, applyWithdrawals);
			this.AddSettlementIncome(clan, ref goldChange, applyWithdrawals, includeDetails);
			this.CalculateHeroIncomeFromWorkshops(clan.Leader, ref goldChange, applyWithdrawals);
			this.AddIncomeFromParties(clan, ref goldChange, applyWithdrawals, includeDetails);
			if (clan == Clan.PlayerClan)
			{
				this.AddPlayerClanIncomeFromOwnedAlleys(ref goldChange);
			}
			if (!clan.IsUnderMercenaryService)
			{
				this.AddIncomeFromTribute(clan, ref goldChange, applyWithdrawals, includeDetails);
				this.AddIncomeFromCallToWarAgrements(clan, ref goldChange, applyWithdrawals);
			}
			if (clan.Gold < 30000 && clan.Kingdom != null && clan.Leader != Hero.MainHero && !clan.IsUnderMercenaryService)
			{
				this.AddIncomeFromKingdomBudget(clan, ref goldChange, applyWithdrawals);
			}
			Hero leader = clan.Leader;
			if (leader != null && leader.GetPerkValue(DefaultPerks.Trade.SpringOfGold))
			{
				int num2 = MathF.Min(1000, MathF.Round((float)clan.Leader.Gold * DefaultPerks.Trade.SpringOfGold.PrimaryBonus));
				goldChange.Add((float)num2, DefaultPerks.Trade.SpringOfGold.Name, null);
			}
		}

		// Token: 0x060016CC RID: 5836 RVA: 0x00068158 File Offset: 0x00066358
		public void CalculateClanExpensesInternal(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false, bool includeDetails = false)
		{
			this.AddExpensesFromPartiesAndGarrisons(clan, ref goldChange, applyWithdrawals, includeDetails);
			if (!clan.IsUnderMercenaryService)
			{
				this.AddExpensesForHiredMercenaries(clan, ref goldChange, applyWithdrawals);
				this.AddExpensesForTributes(clan, ref goldChange, applyWithdrawals);
			}
			this.AddExpensesForAutoRecruitment(clan, ref goldChange, applyWithdrawals);
			if (clan.Gold > 100000 && clan.Kingdom != null && clan.Leader != Hero.MainHero && !clan.IsUnderMercenaryService)
			{
				int num = (int)(((float)clan.Gold - 100000f) * 0.01f);
				if (applyWithdrawals)
				{
					clan.Kingdom.KingdomBudgetWallet += num;
				}
				goldChange.Add((float)(-(float)num), DefaultClanFinanceModel._kingdomBudgetText, null);
			}
			if (clan.DebtToKingdom > 0)
			{
				this.AddPaymentForDebts(clan, ref goldChange, applyWithdrawals);
			}
			if (Clan.PlayerClan == clan)
			{
				this.AddPlayerExpenseForWorkshops(ref goldChange);
			}
			if (!clan.IsUnderMercenaryService)
			{
				this.AddExpensesForCallToWarAgreements(clan, ref goldChange, applyWithdrawals);
			}
		}

		// Token: 0x060016CD RID: 5837 RVA: 0x0006822C File Offset: 0x0006642C
		private void AddPlayerExpenseForWorkshops(ref ExplainedNumber goldChange)
		{
			int num = 0;
			foreach (Workshop workshop in Hero.MainHero.OwnedWorkshops)
			{
				if (workshop.Capital < Campaign.Current.Models.WorkshopModel.CapitalLowLimit)
				{
					num -= workshop.Expense;
				}
			}
			goldChange.Add((float)num, DefaultClanFinanceModel._shopExpenseText, null);
		}

		// Token: 0x060016CE RID: 5838 RVA: 0x000682B4 File Offset: 0x000664B4
		public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateClanExpensesInternal(clan, ref result, applyWithdrawals, includeDetails);
			return result;
		}

		// Token: 0x060016CF RID: 5839 RVA: 0x000682DC File Offset: 0x000664DC
		private void AddPaymentForDebts(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			if (clan.Kingdom != null && clan.DebtToKingdom > 0)
			{
				int num = clan.DebtToKingdom;
				if (applyWithdrawals)
				{
					num = MathF.Min(num, (int)((float)clan.Gold + goldChange.ResultNumber));
					clan.DebtToKingdom -= num;
				}
				goldChange.Add((float)(-(float)num), DefaultClanFinanceModel._debtText, null);
			}
		}

		// Token: 0x060016D0 RID: 5840 RVA: 0x00068338 File Offset: 0x00066538
		private void AddRulingClanIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions, null);
			int num = 0;
			int num2 = 0;
			bool flag = clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax);
			float num3 = 0f;
			foreach (Town town in clan.Fiefs)
			{
				num += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, false).ResultNumber;
				num2++;
			}
			if (flag)
			{
				foreach (Village village in clan.Kingdom.Villages)
				{
					if (!village.IsOwnerUnassigned && village.Settlement.OwnerClan != clan && village.VillageState != Village.VillageStates.Looted && village.VillageState != Village.VillageStates.BeingRaided)
					{
						int num4 = (int)((float)village.TradeTaxAccumulated / this.RevenueSmoothenFraction());
						num3 += (float)num4 * 0.05f;
					}
				}
				if (num3 > 1E-05f)
				{
					explainedNumber.Add((float)((int)num3), DefaultPolicies.LandTax.Name, null);
				}
			}
			Kingdom kingdom = clan.Kingdom;
			if (kingdom.RulingClan == clan)
			{
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.WarTax))
				{
					int num5 = (int)((float)num * 0.05f);
					explainedNumber.Add((float)num5, DefaultPolicies.WarTax.Name, null);
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.DebasementOfTheCurrency))
				{
					explainedNumber.Add((float)(num2 * 100), DefaultPolicies.DebasementOfTheCurrency.Name, null);
				}
			}
			int num6 = 0;
			int num7 = 0;
			foreach (Settlement settlement in clan.Settlements)
			{
				if (settlement.IsTown)
				{
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.RoadTolls))
					{
						int num8 = settlement.Town.TradeTaxAccumulated / 30;
						if (applyWithdrawals)
						{
							settlement.Town.TradeTaxAccumulated -= num8;
						}
						num6 += num8;
					}
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.StateMonopolies))
					{
						num7 += (int)((float)settlement.Town.Workshops.Sum((Workshop t) => t.ProfitMade) * 0.05f);
					}
					if (num6 > 0)
					{
						explainedNumber.Add((float)num6, DefaultPolicies.RoadTolls.Name, null);
					}
					if (num7 > 0)
					{
						explainedNumber.Add((float)num7, DefaultPolicies.StateMonopolies.Name, null);
					}
				}
			}
			if (!explainedNumber.ResultNumber.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				if (!includeDetails)
				{
					goldChange.Add(explainedNumber.ResultNumber, GameTexts.FindText("str_policies", null), null);
					return;
				}
				goldChange.AddFromExplainedNumber(explainedNumber, GameTexts.FindText("str_policies", null));
			}
		}

		// Token: 0x060016D1 RID: 5841 RVA: 0x00068668 File Offset: 0x00066868
		private void AddExpensesForHiredMercenaries(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			Kingdom kingdom = clan.Kingdom;
			if (kingdom != null)
			{
				float num = DefaultClanFinanceModel.CalculateShareFactor(clan);
				if (kingdom.MercenaryWallet < 0)
				{
					int num2 = (int)((float)(-(float)kingdom.MercenaryWallet) * num);
					DefaultClanFinanceModel.ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num2, DefaultClanFinanceModel._mercenaryExpensesText);
					if (applyWithdrawals)
					{
						kingdom.MercenaryWallet += num2;
					}
				}
			}
		}

		// Token: 0x060016D2 RID: 5842 RVA: 0x000686BC File Offset: 0x000668BC
		private void AddExpensesForTributes(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			Kingdom kingdom = clan.Kingdom;
			if (kingdom != null)
			{
				float num = DefaultClanFinanceModel.CalculateShareFactor(clan);
				if (kingdom.TributeWallet < 0)
				{
					int num2 = (int)((float)(-(float)kingdom.TributeWallet) * num);
					DefaultClanFinanceModel.ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num2, DefaultClanFinanceModel._tributeExpensesText);
					if (applyWithdrawals)
					{
						kingdom.TributeWallet += num2;
					}
				}
			}
		}

		// Token: 0x060016D3 RID: 5843 RVA: 0x00068710 File Offset: 0x00066910
		private void AddExpensesForCallToWarAgreements(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			Kingdom kingdom = clan.Kingdom;
			if (kingdom != null && kingdom.CallToWarWallet < 0)
			{
				float num = DefaultClanFinanceModel.CalculateShareFactor(clan);
				int num2 = (int)((float)(-(float)kingdom.CallToWarWallet) * num);
				int num3 = num2;
				int num4 = (int)((float)clan.Gold + goldChange.ResultNumber);
				if (applyWithdrawals && num4 - num3 < 5000)
				{
					num3 = MathF.Max(0, num4 - 5000);
					clan.DebtToKingdom += num2 - num3;
				}
				DefaultClanFinanceModel.ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num3, DefaultClanFinanceModel._callToWarExpenses);
				if (applyWithdrawals)
				{
					kingdom.CallToWarWallet += num2;
				}
			}
		}

		// Token: 0x060016D4 RID: 5844 RVA: 0x000687A4 File Offset: 0x000669A4
		private static void ApplyShareForExpenses(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, int expenseShare, TextObject mercenaryExpensesText)
		{
			if (applyWithdrawals)
			{
				int num = (int)((float)clan.Gold + goldChange.ResultNumber);
				if (expenseShare > num)
				{
					int num2 = expenseShare - num;
					expenseShare = num;
					clan.DebtToKingdom += num2;
				}
			}
			goldChange.Add((float)(-(float)expenseShare), mercenaryExpensesText, null);
		}

		// Token: 0x060016D5 RID: 5845 RVA: 0x000687EC File Offset: 0x000669EC
		private void AddSettlementIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions, null);
			foreach (Town town in clan.Fiefs)
			{
				ExplainedNumber explainedNumber2 = Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, false);
				ExplainedNumber explainedNumber3 = Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromTariffs(clan, town, applyWithdrawals);
				int num = Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromProjects(town);
				if (explainedNumber.IncludeDescriptions)
				{
					explainedNumber.Add((float)((int)explainedNumber2.ResultNumber), Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._townTaxStr, null), town.Name);
					explainedNumber.Add((float)((int)explainedNumber3.ResultNumber), Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._tariffTaxStr, null), town.Name);
					explainedNumber.Add((float)num, DefaultClanFinanceModel._projectsIncomeText, null);
				}
				else
				{
					explainedNumber.Add((float)((int)explainedNumber2.ResultNumber), null, null);
					explainedNumber.Add((float)((int)explainedNumber3.ResultNumber), null, null);
					explainedNumber.Add((float)num, null, null);
				}
				foreach (Village village in town.Villages)
				{
					int num2 = this.CalculateVillageIncome(clan, village, applyWithdrawals);
					explainedNumber.Add((float)num2, village.Name, null);
				}
			}
			if (!includeDetails)
			{
				goldChange.Add(explainedNumber.ResultNumber, DefaultClanFinanceModel._settlementIncome, null);
				return;
			}
			goldChange.AddFromExplainedNumber(explainedNumber, DefaultClanFinanceModel._settlementIncome);
		}

		// Token: 0x060016D6 RID: 5846 RVA: 0x000689D0 File Offset: 0x00066BD0
		public override ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false)
		{
			ExplainedNumber result = new ExplainedNumber((float)((int)((float)town.TradeTaxAccumulated / this.RevenueSmoothenFraction())), false, null);
			int num = MathF.Round(result.ResultNumber);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.ContentTrades, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Crossbow.Steady, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.SaltTheEarth, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.GivingHands, town, ref result);
			this.CalculateSettlementProjectTariffBonuses(town, ref result);
			if (applyWithdrawals)
			{
				town.TradeTaxAccumulated -= num;
				if (clan == Clan.PlayerClan)
				{
					CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.Taxes, (int)result.ResultNumber);
				}
			}
			return result;
		}

		// Token: 0x060016D7 RID: 5847 RVA: 0x00068A6D File Offset: 0x00066C6D
		private void CalculateSettlementProjectTariffBonuses(Town town, ref ExplainedNumber result)
		{
			town.AddEffectOfBuildings(BuildingEffectEnum.TariffIncome, ref result);
		}

		// Token: 0x060016D8 RID: 5848 RVA: 0x00068A78 File Offset: 0x00066C78
		public override int CalculateTownIncomeFromProjects(Town town)
		{
			ExplainedNumber explainedNumber = default(ExplainedNumber);
			if (town.CurrentDefaultBuilding != null && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.ArchitecturalCommisions))
			{
				explainedNumber.Add((float)((int)DefaultPerks.Engineering.ArchitecturalCommisions.SecondaryBonus), null, null);
			}
			town.AddEffectOfBuildings(BuildingEffectEnum.DenarByBoundVillageHeartPerDay, ref explainedNumber);
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x060016D9 RID: 5849 RVA: 0x00068AD8 File Offset: 0x00066CD8
		public override int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false)
		{
			int num = ((village.VillageState == Village.VillageStates.Looted || village.VillageState == Village.VillageStates.BeingRaided) ? 0 : ((int)((float)village.TradeTaxAccumulated / this.RevenueSmoothenFraction())));
			int num2 = num;
			if (clan.Kingdom != null && clan.Kingdom.RulingClan != clan && clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax))
			{
				num -= (int)(0.05f * (float)num);
			}
			if (village.Bound.Town != null && village.Bound.Town.Governor != null && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Scouting.ForestKin))
			{
				num += MathF.Round((float)num * DefaultPerks.Scouting.ForestKin.SecondaryBonus);
			}
			Settlement bound = village.Bound;
			bool flag;
			if (bound == null)
			{
				flag = null != null;
			}
			else
			{
				Town town = bound.Town;
				flag = ((town != null) ? town.Governor : null) != null;
			}
			if (flag && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Steward.Logistician))
			{
				num += MathF.Round((float)num * DefaultPerks.Steward.Logistician.SecondaryBonus);
			}
			if (applyWithdrawals)
			{
				village.TradeTaxAccumulated -= num2;
				if (clan == Clan.PlayerClan)
				{
					CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.Taxes, num);
				}
			}
			return num;
		}

		// Token: 0x060016DA RID: 5850 RVA: 0x00068C0C File Offset: 0x00066E0C
		private static float CalculateShareFactor(Clan clan)
		{
			Kingdom kingdom = clan.Kingdom;
			int num = kingdom.Fiefs.Sum(delegate(Town x)
			{
				if (!x.IsCastle)
				{
					return 3;
				}
				return 1;
			}) + 1 + kingdom.Clans.Count;
			return (float)(clan.Fiefs.Sum(delegate(Town x)
			{
				if (!x.IsCastle)
				{
					return 3;
				}
				return 1;
			}) + ((clan == kingdom.RulingClan) ? 1 : 0) + 1) / (float)num;
		}

		// Token: 0x060016DB RID: 5851 RVA: 0x00068C98 File Offset: 0x00066E98
		private void AddMercenaryIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			if (clan.IsUnderMercenaryService && clan.Leader != null && clan.Kingdom != null)
			{
				int num = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) * clan.MercenaryAwardMultiplier;
				if (applyWithdrawals)
				{
					clan.Kingdom.MercenaryWallet -= num;
				}
				goldChange.Add((float)num, DefaultClanFinanceModel._mercenaryText, null);
			}
		}

		// Token: 0x060016DC RID: 5852 RVA: 0x00068D10 File Offset: 0x00066F10
		private void AddIncomeFromKingdomBudget(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			int num = ((clan.Gold < 5000) ? 2000 : ((clan.Gold < 10000) ? 1500 : ((clan.Gold < 20000) ? 1000 : 500)));
			num *= ((clan.Kingdom.KingdomBudgetWallet > 1000000) ? 2 : 1);
			num *= ((clan.Leader == clan.Kingdom.Leader) ? 2 : 1);
			int num2 = MathF.Min(clan.Kingdom.KingdomBudgetWallet, num);
			if (applyWithdrawals)
			{
				clan.Kingdom.KingdomBudgetWallet -= num2;
			}
			goldChange.Add((float)num2, DefaultClanFinanceModel._kingdomSupportText, null);
		}

		// Token: 0x060016DD RID: 5853 RVA: 0x00068DC8 File Offset: 0x00066FC8
		private void AddPlayerClanIncomeFromOwnedAlleys(ref ExplainedNumber goldChange)
		{
			int num = 0;
			foreach (Alley alley in Hero.MainHero.OwnedAlleys)
			{
				num += Campaign.Current.Models.AlleyModel.GetDailyIncomeOfAlley(alley);
			}
			goldChange.Add((float)num, DefaultClanFinanceModel._alleyText, null);
		}

		// Token: 0x060016DE RID: 5854 RVA: 0x00068E40 File Offset: 0x00067040
		private void AddIncomeFromTribute(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions, null);
			IFaction mapFaction = clan.MapFaction;
			float num = 1f;
			if (clan.Kingdom != null)
			{
				num = DefaultClanFinanceModel.CalculateShareFactor(clan);
			}
			foreach (StanceLink stanceLink in FactionHelper.GetStances(mapFaction))
			{
				IFaction faction = ((stanceLink.Faction1 == mapFaction) ? stanceLink.Faction2 : stanceLink.Faction1);
				int dailyTributeToPay = stanceLink.GetDailyTributeToPay(mapFaction);
				if (!mapFaction.IsAtWarWith(faction) && dailyTributeToPay < 0)
				{
					int num2 = (int)((float)dailyTributeToPay * num);
					if (applyWithdrawals)
					{
						faction.TributeWallet += num2;
						if (stanceLink.Faction1 == mapFaction)
						{
							stanceLink.TotalTributePaidFrom2To1 += -num2;
						}
						if (stanceLink.Faction2 == mapFaction)
						{
							stanceLink.TotalTributePaidFrom1To2 += -num2;
						}
						CampaignEventDispatcher.Instance.OnClanEarnedGoldFromTribute(clan, faction);
						if (clan == Clan.PlayerClan)
						{
							CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.TributesEarned, -num2);
						}
					}
					explainedNumber.Add((float)(-(float)num2), Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._tributeIncomeStr, null), faction.InformalName);
				}
			}
			if (!includeDetails)
			{
				goldChange.Add(explainedNumber.ResultNumber, DefaultClanFinanceModel._tributeIncomes, null);
				return;
			}
			goldChange.AddFromExplainedNumber(explainedNumber, DefaultClanFinanceModel._tributeIncomes);
		}

		// Token: 0x060016DF RID: 5855 RVA: 0x00068FB8 File Offset: 0x000671B8
		private void AddIncomeFromCallToWarAgrements(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			if (clan.Kingdom != null && clan.Kingdom.CallToWarWallet > 0)
			{
				float num = DefaultClanFinanceModel.CalculateShareFactor(clan);
				int num2 = (int)((float)clan.Kingdom.CallToWarWallet * num);
				if (applyWithdrawals)
				{
					clan.Kingdom.CallToWarWallet -= num2;
					if (clan == Clan.PlayerClan)
					{
						CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.TributesEarned, num2);
					}
				}
				goldChange.Add((float)num2, DefaultClanFinanceModel._callToWarIncomes, null);
			}
		}

		// Token: 0x060016E0 RID: 5856 RVA: 0x0006902C File Offset: 0x0006722C
		private void AddIncomeFromParties(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions, null);
			foreach (Hero hero in clan.AliveLords)
			{
				foreach (CaravanPartyComponent caravanPartyComponent in hero.OwnedCaravans)
				{
					if (caravanPartyComponent.MobileParty.IsActive && caravanPartyComponent.MobileParty.LeaderHero != clan.Leader && (caravanPartyComponent.MobileParty.IsLordParty || caravanPartyComponent.MobileParty.IsGarrison || caravanPartyComponent.MobileParty.IsCaravan))
					{
						int num = this.AddIncomeFromParty(caravanPartyComponent.MobileParty, clan, ref goldChange, applyWithdrawals);
						explainedNumber.Add((float)num, Game.Current.GameTextManager.FindText(caravanPartyComponent.MobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability ? DefaultClanFinanceModel._convoyIncomeStr : DefaultClanFinanceModel._caravanIncomeStr, null), (caravanPartyComponent.Leader != null) ? caravanPartyComponent.Leader.Name : caravanPartyComponent.Name);
					}
				}
			}
			foreach (Hero hero2 in clan.Companions)
			{
				foreach (CaravanPartyComponent caravanPartyComponent2 in hero2.OwnedCaravans)
				{
					if (caravanPartyComponent2.MobileParty.IsActive && caravanPartyComponent2.MobileParty.LeaderHero != clan.Leader && (caravanPartyComponent2.MobileParty.IsLordParty || caravanPartyComponent2.MobileParty.IsGarrison || caravanPartyComponent2.MobileParty.IsCaravan))
					{
						int num2 = this.AddIncomeFromParty(caravanPartyComponent2.MobileParty, clan, ref goldChange, applyWithdrawals);
						explainedNumber.Add((float)num2, Game.Current.GameTextManager.FindText(caravanPartyComponent2.MobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability ? DefaultClanFinanceModel._convoyIncomeStr : DefaultClanFinanceModel._caravanIncomeStr, null), (caravanPartyComponent2.Leader != null) ? caravanPartyComponent2.Leader.Name : caravanPartyComponent2.Name);
					}
				}
			}
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.IsActive && warPartyComponent.MobileParty.LeaderHero != clan.Leader && (warPartyComponent.MobileParty.IsLordParty || warPartyComponent.MobileParty.IsGarrison || warPartyComponent.MobileParty.IsCaravan))
				{
					int num3 = this.AddIncomeFromParty(warPartyComponent.MobileParty, clan, ref goldChange, applyWithdrawals);
					explainedNumber.Add((float)num3, Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._partyIncomeStr, null), warPartyComponent.MobileParty.Name);
				}
			}
			if (!includeDetails)
			{
				goldChange.Add(explainedNumber.ResultNumber, DefaultClanFinanceModel._caravanAndPartyIncome, null);
				return;
			}
			goldChange.AddFromExplainedNumber(explainedNumber, DefaultClanFinanceModel._caravanAndPartyIncome);
		}

		// Token: 0x060016E1 RID: 5857 RVA: 0x000693EC File Offset: 0x000675EC
		private int AddIncomeFromParty(MobileParty party, Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			int num = 0;
			if (party.IsActive && party.LeaderHero != clan.Leader && (party.IsLordParty || party.IsGarrison || party.IsCaravan))
			{
				int partyTradeGold = party.PartyTradeGold;
				if (partyTradeGold > 10000)
				{
					num = (partyTradeGold - 10000) / 10;
					if (applyWithdrawals)
					{
						party.PartyTradeGold -= num;
						if (party.LeaderHero != null && num > 0)
						{
							SkillLevelingManager.OnTradeProfitMade(party.LeaderHero, num);
						}
						Hero owner = party.Party.Owner;
						bool flag;
						if (owner == null)
						{
							flag = null != null;
						}
						else
						{
							Clan clan2 = owner.Clan;
							flag = ((clan2 != null) ? clan2.Leader : null) != null;
						}
						if (flag && party.IsCaravan && party.Party.Owner.Clan.Leader.GetPerkValue(DefaultPerks.Trade.GreatInvestor) && num > 0)
						{
							party.Party.Owner.Clan.AddRenown(DefaultPerks.Trade.GreatInvestor.PrimaryBonus, true);
						}
						if (clan == Clan.PlayerClan && party.IsCaravan)
						{
							CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.Caravan, num);
						}
					}
				}
			}
			return num;
		}

		// Token: 0x060016E2 RID: 5858 RVA: 0x0006950C File Offset: 0x0006770C
		private void AddExpensesFromPartiesAndGarrisons(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions, null);
			int num = this.AddExpenseFromLeaderParty(clan, goldChange, applyWithdrawals);
			explainedNumber.Add((float)num, DefaultClanFinanceModel._mainPartywageText, null);
			foreach (Hero hero in clan.AliveLords)
			{
				foreach (CaravanPartyComponent caravanPartyComponent in hero.OwnedCaravans)
				{
					if (caravanPartyComponent.MobileParty.IsActive && caravanPartyComponent.MobileParty.LeaderHero != clan.Leader)
					{
						int num2 = this.AddPartyExpense(caravanPartyComponent.MobileParty, clan, goldChange, applyWithdrawals);
						if (explainedNumber.IncludeDescriptions)
						{
							explainedNumber.Add((float)num2, Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._partyExpensesStr, null), caravanPartyComponent.Name);
						}
						else
						{
							explainedNumber.Add((float)num2, null, null);
						}
					}
				}
			}
			foreach (Hero hero2 in clan.Companions)
			{
				foreach (CaravanPartyComponent caravanPartyComponent2 in hero2.OwnedCaravans)
				{
					int num3 = this.AddPartyExpense(caravanPartyComponent2.MobileParty, clan, goldChange, applyWithdrawals);
					if (explainedNumber.IncludeDescriptions)
					{
						explainedNumber.Add((float)num3, Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._partyExpensesStr, null), caravanPartyComponent2.Name);
					}
					else
					{
						explainedNumber.Add((float)num3, null, null);
					}
				}
			}
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				if (warPartyComponent.MobileParty.IsActive && warPartyComponent.MobileParty.LeaderHero != clan.Leader)
				{
					int num4 = this.AddPartyExpense(warPartyComponent.MobileParty, clan, goldChange, applyWithdrawals);
					if (explainedNumber.IncludeDescriptions)
					{
						explainedNumber.Add((float)num4, Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._partyExpensesStr, null), warPartyComponent.Name);
					}
					else
					{
						explainedNumber.Add((float)num4, null, null);
					}
				}
			}
			foreach (Town town in clan.Fiefs)
			{
				if (town.GarrisonParty != null && town.GarrisonParty.IsActive)
				{
					int num5 = this.AddPartyExpense(town.GarrisonParty, clan, goldChange, applyWithdrawals);
					if (explainedNumber.IncludeDescriptions)
					{
						TextObject textObject = new TextObject("{=fsTBcLvA}{SETTLEMENT} Garrison", null);
						textObject.SetTextVariable("SETTLEMENT", town.Name);
						explainedNumber.Add((float)num5, Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._partyExpensesStr, null), textObject);
					}
					else
					{
						explainedNumber.Add((float)num5, null, null);
					}
				}
			}
			if (!includeDetails)
			{
				goldChange.Add(explainedNumber.ResultNumber, DefaultClanFinanceModel._garrisonAndPartyExpenses, null);
				return;
			}
			goldChange.AddFromExplainedNumber(explainedNumber, DefaultClanFinanceModel._garrisonAndPartyExpenses);
		}

		// Token: 0x060016E3 RID: 5859 RVA: 0x000698B8 File Offset: 0x00067AB8
		private void AddExpensesForAutoRecruitment(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
		{
			int num = clan.AutoRecruitmentExpenses / 5;
			if (applyWithdrawals)
			{
				clan.AutoRecruitmentExpenses -= num;
			}
			goldChange.Add((float)(-(float)num), DefaultClanFinanceModel._autoRecruitmentText, null);
		}

		// Token: 0x060016E4 RID: 5860 RVA: 0x000698F0 File Offset: 0x00067AF0
		private int AddExpenseFromLeaderParty(Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
		{
			Hero leader = clan.Leader;
			MobileParty mobileParty = ((leader != null) ? leader.PartyBelongedTo : null);
			if (mobileParty != null)
			{
				int num = clan.Gold + (int)goldChange.ResultNumber;
				if (num < 2000 && applyWithdrawals && clan != Clan.PlayerClan)
				{
					num = 0;
				}
				return -this.CalculatePartyWage(mobileParty, num, applyWithdrawals);
			}
			return 0;
		}

		// Token: 0x060016E5 RID: 5861 RVA: 0x00069948 File Offset: 0x00067B48
		private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
		{
			int num = clan.Gold + (int)goldChange.ResultNumber;
			int num2 = num;
			if (num < (party.IsGarrison ? 8000 : 4000) && applyWithdrawals && clan != Clan.PlayerClan)
			{
				num2 = ((party.LeaderHero != null && party.PartyTradeGold < 500) ? MathF.Min(num, 250) : 0);
			}
			int num3 = this.CalculatePartyWage(party, num2, applyWithdrawals);
			int num4 = party.PartyTradeGold;
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
			num4 -= num3;
			if (num4 < this.PartyGoldLowerThreshold)
			{
				int num5 = this.PartyGoldLowerThreshold - num4;
				if (party.IsLordParty && party.LeaderHero == null)
				{
					num5 = num3;
				}
				if (applyWithdrawals)
				{
					num5 = MathF.Min(num5, num2);
					party.PartyTradeGold += num5;
				}
				return -num5;
			}
			return 0;
		}

		// Token: 0x060016E6 RID: 5862 RVA: 0x00069A49 File Offset: 0x00067C49
		public override int CalculateOwnerIncomeFromCaravan(MobileParty caravan)
		{
			return (int)((float)MathF.Max(0, caravan.PartyTradeGold - Campaign.Current.Models.CaravanModel.GetInitialTradeGold(caravan.Owner, caravan.CaravanPartyComponent.CanHaveNavalNavigationCapability, false)) / this.RevenueSmoothenFraction());
		}

		// Token: 0x060016E7 RID: 5863 RVA: 0x00069A87 File Offset: 0x00067C87
		public override int CalculateOwnerIncomeFromWorkshop(Workshop workshop)
		{
			return (int)((float)MathF.Max(0, workshop.ProfitMade) / this.RevenueSmoothenFraction());
		}

		// Token: 0x060016E8 RID: 5864 RVA: 0x00069AA0 File Offset: 0x00067CA0
		private void CalculateHeroIncomeFromAssets(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			int num = 0;
			foreach (CaravanPartyComponent caravanPartyComponent in hero.OwnedCaravans)
			{
				if (caravanPartyComponent.MobileParty.PartyTradeGold > Campaign.Current.Models.CaravanModel.GetInitialTradeGold(caravanPartyComponent.Owner, caravanPartyComponent.CanHaveNavalNavigationCapability, false))
				{
					int num2 = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromCaravan(caravanPartyComponent.MobileParty);
					if (applyWithdrawals)
					{
						caravanPartyComponent.MobileParty.PartyTradeGold -= num2;
						SkillLevelingManager.OnTradeProfitMade(hero, num2);
					}
					if (num2 > 0)
					{
						num += num2;
					}
				}
			}
			goldChange.Add((float)num, goldChange.IncludeDescriptions ? Game.Current.GameTextManager.FindText(DefaultClanFinanceModel._caravanIncomeStr, null) : null, null);
			this.CalculateHeroIncomeFromWorkshops(hero, ref goldChange, applyWithdrawals);
			if (hero.CurrentSettlement != null)
			{
				foreach (Alley alley in hero.CurrentSettlement.Alleys)
				{
					if (alley.Owner == hero)
					{
						goldChange.Add(30f, alley.Name, null);
					}
				}
			}
		}

		// Token: 0x060016E9 RID: 5865 RVA: 0x00069BF8 File Offset: 0x00067DF8
		private void CalculateHeroIncomeFromWorkshops(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
		{
			int num = 0;
			int num2 = 0;
			foreach (Workshop workshop in hero.OwnedWorkshops)
			{
				int num3 = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(workshop);
				num += num3;
				if (applyWithdrawals && num3 > 0)
				{
					workshop.ChangeGold(-num3);
					if (hero == Hero.MainHero)
					{
						CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType.Workshop, num3);
					}
				}
				if (num3 > 0)
				{
					num2++;
				}
			}
			goldChange.Add((float)num, DefaultClanFinanceModel._shopIncomeText, null);
			bool flag;
			if (hero.Clan != null)
			{
				Hero leader = hero.Clan.Leader;
				flag = leader != null && leader.GetPerkValue(DefaultPerks.Trade.ArtisanCommunity);
			}
			else
			{
				flag = false;
			}
			if (flag && applyWithdrawals && num2 > 0)
			{
				hero.Clan.AddRenown((float)num2 * DefaultPerks.Trade.ArtisanCommunity.PrimaryBonus, true);
			}
		}

		// Token: 0x060016EA RID: 5866 RVA: 0x00069CEC File Offset: 0x00067EEC
		public override float RevenueSmoothenFraction()
		{
			return 5f;
		}

		// Token: 0x060016EB RID: 5867 RVA: 0x00069CF4 File Offset: 0x00067EF4
		private int CalculatePartyWage(MobileParty mobileParty, int budget, bool applyWithdrawals)
		{
			int totalWage = mobileParty.TotalWage;
			int num = totalWage;
			if (applyWithdrawals)
			{
				num = MathF.Min(totalWage, budget);
				DefaultClanFinanceModel.ApplyMoraleEffect(mobileParty, totalWage, num);
			}
			return num;
		}

		// Token: 0x060016EC RID: 5868 RVA: 0x00069D20 File Offset: 0x00067F20
		public override int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
			this.CalculateHeroIncomeFromAssets(hero, ref explainedNumber, applyWithdrawals);
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x060016ED RID: 5869 RVA: 0x00069D50 File Offset: 0x00067F50
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
				MBTextManager.SetTextVariable("reg1", MathF.Round(MathF.Abs(num2), 1), 2);
				if (mobileParty == MobileParty.MainParty)
				{
					MBInformationManager.AddQuickInformation(GameTexts.FindText("str_party_loses_moral_due_to_insufficent_funds", null), 0, null, null, "");
					return;
				}
			}
			else
			{
				mobileParty.HasUnpaidWages = 0f;
			}
		}

		// Token: 0x04000789 RID: 1929
		private static readonly string _townTaxStr = "str_finance_town_tax";

		// Token: 0x0400078A RID: 1930
		private static readonly string _partyIncomeStr = "str_finance_party_income";

		// Token: 0x0400078B RID: 1931
		private static readonly string _partyExpensesStr = "str_finance_party_expenses";

		// Token: 0x0400078C RID: 1932
		private static readonly string _tributeIncomeStr = "str_finance_tribute_income";

		// Token: 0x0400078D RID: 1933
		private static readonly string _tariffTaxStr = "str_finance_tariff_tax";

		// Token: 0x0400078E RID: 1934
		private static readonly string _caravanIncomeStr = "str_finance_caravan_income";

		// Token: 0x0400078F RID: 1935
		private static readonly string _convoyIncomeStr = "str_finance_convoy_income";

		// Token: 0x04000790 RID: 1936
		private static readonly TextObject _projectsIncomeText = Game.Current.GameTextManager.FindText("str_finance_projects_income", null);

		// Token: 0x04000791 RID: 1937
		private static readonly TextObject _shopExpenseText = Game.Current.GameTextManager.FindText("str_finance_shop_expense", null);

		// Token: 0x04000792 RID: 1938
		private static readonly TextObject _mercenaryText = Game.Current.GameTextManager.FindText("str_finance_mercenary", null);

		// Token: 0x04000793 RID: 1939
		private static readonly TextObject _mercenaryExpensesText = Game.Current.GameTextManager.FindText("str_finance_mercenary_expenses", null);

		// Token: 0x04000794 RID: 1940
		private static readonly TextObject _tributeExpensesText = Game.Current.GameTextManager.FindText("str_finance_tribute_expenses", null);

		// Token: 0x04000795 RID: 1941
		private static readonly TextObject _tributeIncomes = Game.Current.GameTextManager.FindText("str_finance_tribute_incomes", null);

		// Token: 0x04000796 RID: 1942
		private static readonly TextObject _callToWarExpenses = Game.Current.GameTextManager.FindText("str_finance_call_to_war_expenses", null);

		// Token: 0x04000797 RID: 1943
		private static readonly TextObject _callToWarIncomes = Game.Current.GameTextManager.FindText("str_finance_call_to_war_incomes", null);

		// Token: 0x04000798 RID: 1944
		private static readonly TextObject _settlementIncome = Game.Current.GameTextManager.FindText("str_finance_settlement_income", null);

		// Token: 0x04000799 RID: 1945
		private static readonly TextObject _mainPartywageText = Game.Current.GameTextManager.FindText("str_finance_main_party_wage", null);

		// Token: 0x0400079A RID: 1946
		private static readonly TextObject _caravanAndPartyIncome = Game.Current.GameTextManager.FindText("str_finance_caravan_and_party_income", null);

		// Token: 0x0400079B RID: 1947
		private static readonly TextObject _garrisonAndPartyExpenses = Game.Current.GameTextManager.FindText("str_finance_garrison_and_party_expenses", null);

		// Token: 0x0400079C RID: 1948
		private static readonly TextObject _debtText = Game.Current.GameTextManager.FindText("str_finance_debt", null);

		// Token: 0x0400079D RID: 1949
		private static readonly TextObject _kingdomSupportText = Game.Current.GameTextManager.FindText("str_finance_kingdom_support", null);

		// Token: 0x0400079E RID: 1950
		private static readonly TextObject _kingdomBudgetText = Game.Current.GameTextManager.FindText("str_finance_kingdom_budget", null);

		// Token: 0x0400079F RID: 1951
		private static readonly TextObject _autoRecruitmentText = Game.Current.GameTextManager.FindText("str_finance_auto_recruitment", null);

		// Token: 0x040007A0 RID: 1952
		private static readonly TextObject _alleyText = Game.Current.GameTextManager.FindText("str_finance_alley", null);

		// Token: 0x040007A1 RID: 1953
		private static readonly TextObject _shopIncomeText = Game.Current.GameTextManager.FindText("str_finance_shop_income", null);

		// Token: 0x040007A2 RID: 1954
		private const int PartyGoldIncomeThreshold = 10000;

		// Token: 0x040007A3 RID: 1955
		private const int payGarrisonWagesTreshold = 8000;

		// Token: 0x040007A4 RID: 1956
		private const int payClanPartiesTreshold = 4000;

		// Token: 0x040007A5 RID: 1957
		private const int payLeaderPartyWageTreshold = 2000;

		// Token: 0x02000575 RID: 1397
		private enum TransactionType
		{
			// Token: 0x04001726 RID: 5926
			Income = 1,
			// Token: 0x04001727 RID: 5927
			Both = 0,
			// Token: 0x04001728 RID: 5928
			Expense = -1
		}

		// Token: 0x02000576 RID: 1398
		public enum AssetIncomeType
		{
			// Token: 0x0400172A RID: 5930
			Workshop,
			// Token: 0x0400172B RID: 5931
			Caravan,
			// Token: 0x0400172C RID: 5932
			Taxes,
			// Token: 0x0400172D RID: 5933
			TributesEarned
		}
	}
}
