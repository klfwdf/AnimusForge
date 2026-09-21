using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	private void OnDailyTick()
	{
		try
		{
			RemoveGeneratedRewardItemsFromMarketRosters("daily_tick");
			RestoreDueNpcBattleEquipment("daily_tick");
			if (_debts == null || _debts.Count <= 0)
			{
				return;
			}
			float nowCampaignDay = GetNowCampaignDay();
			int campaignDayIndex = GetCampaignDayIndex();
			CleanupPendingPlayerTransfers(campaignDayIndex);
			List<string> list = _debts.Keys.ToList();
			foreach (string item in list)
			{
				if (string.IsNullOrWhiteSpace(item) || !_debts.TryGetValue(item, out var value) || value == null)
				{
					continue;
				}
				NormalizeDebtRecord(value);
				if (value.DebtLines == null || value.DebtLines.Count <= 0)
				{
					continue;
				}
				Hero hero = null;
				try
				{
					hero = Hero.Find(item);
				}
				catch
				{
					hero = null;
				}
				if (hero == null)
				{
					if (!TryParseSettlementMerchantDebtKey(item, out var settlementId, out var kind))
					{
						continue;
					}
					Settlement settlement = ResolveSettlementById(settlementId);
					if (settlement == null)
					{
						continue;
					}
					for (int j = 0; j < value.DebtLines.Count; j++)
					{
						DebtRecord.DebtLine debtLine2 = value.DebtLines[j];
						if (debtLine2 == null || debtLine2.RemainingAmount <= 0)
						{
							continue;
						}
						if (debtLine2.IsDueUnlimited)
						{
							int unlimitedDebtValue = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine2);
							int penaltyUnits = EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(debtLine2, unlimitedDebtValue, campaignDayIndex);
							if (penaltyUnits > 0)
							{
								int publicDelta = AdjustSettlementMerchantTrustByExactUnits(settlement, kind, -penaltyUnits, "merchant_unlimited_debt_daily_penalty", out var appliedUnits);
								Logger.Log("Trust", $"[UnlimitedDebtPenalty] settlement={settlement.StringId} market={kind} debtId={debtLine2.DebtId} value={unlimitedDebtValue} personal={FormatTrustUnits(appliedUnits)} public={publicDelta}");
							}
							continue;
						}
						if ((!debtLine2.IsGold && debtLine2.IsItemUnavailableDeclared) || debtLine2.DueDay <= 0f || nowCampaignDay <= debtLine2.DueDay + 0.01f)
						{
							continue;
						}
						int num = EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(nowCampaignDay, debtLine2.DueDay);
						int num2 = Math.Max(0, debtLine2.OverduePenaltyDaysApplied);
						if (num > num2 && !(debtLine2.BestPreDueCoverage >= 0.95f))
						{
							for (int k = num2 + 1; k <= num; k++)
							{
								int num3 = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine2);
								int num4 = EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(num3);
								int num5 = 0;
								if (num4 > 0)
								{
									num5 = AdjustSettlementMerchantTrust(settlement, kind, -num4, "merchant_overdue_weekly_penalty_by_amount", out _);
								}
								Logger.Log("Trust", string.Format("[OverduePenalty] settlement={0} market={1} debtId={2} mode={3} value={4} trust={5} public={6} week={7}/{8}", settlement.StringId, kind, debtLine2.DebtId, "amount_weekly", num3, num4, num5, k, OverduePenaltyMaxWeeks));
							}
							debtLine2.OverduePenaltyDaysApplied = num;
							debtLine2.LastOverduePenaltyDay = campaignDayIndex;
						}
					}
					NormalizeDebtRecord(value);
					if (!HasDebtContent(value))
					{
						_debts.Remove(item);
						continue;
					}
					// The persistent quest journal replaces recurring merchant debt pop-ups; overdue penalties above still apply.
					continue;
				}
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtRecord.DebtLine debtLine = value.DebtLines[i];
					if (debtLine == null || debtLine.RemainingAmount <= 0)
					{
						continue;
					}
					if (debtLine.IsDueUnlimited)
					{
						int unlimitedDebtValue = EstimateDebtLineRemainingValue(hero, debtLine);
						int penaltyUnits = EconomyDebtSchedulePolicy.ConsumeUnlimitedDebtTrustPenaltyUnits(debtLine, unlimitedDebtValue, campaignDayIndex);
						if (penaltyUnits > 0)
						{
							int publicDelta = AdjustTrustByExactUnits(hero, -penaltyUnits, "unlimited_debt_daily_penalty", out var appliedUnits);
							Logger.Log("Trust", $"[UnlimitedDebtPenalty] npc={hero.StringId} debtId={debtLine.DebtId} value={unlimitedDebtValue} personal={FormatTrustUnits(appliedUnits)} public={publicDelta}");
						}
						continue;
					}
					if ((!debtLine.IsGold && debtLine.IsItemUnavailableDeclared) || debtLine.DueDay <= 0f || nowCampaignDay <= debtLine.DueDay + 0.01f)
					{
						continue;
					}
					int num = EconomyDebtSchedulePolicy.ComputeOverdueElapsedWeeks(nowCampaignDay, debtLine.DueDay);
					int num2 = Math.Max(0, debtLine.OverduePenaltyDaysApplied);
					if (num > num2 && !(debtLine.BestPreDueCoverage >= 0.95f))
					{
						for (int k = num2 + 1; k <= num; k++)
						{
							int num3 = EstimateDebtLineRemainingValue(hero, debtLine);
							int num4 = EconomyDebtSchedulePolicy.ComputeWeeklyOverdueTrustPenaltyByDebtValue(num3);
							int num5 = 0;
							if (num4 > 0)
							{
								num5 = AdjustTrust(hero, -num4, 0, "overdue_weekly_penalty_by_amount", out _);
							}
							int num6 = EconomyDebtSchedulePolicy.ComputeWeeklyOverdueRelationPenaltyDelta(k - 1, k, num4);
							if (num6 > 0)
							{
								AdjustRelationWithPlayer(hero, -num6, "overdue_weekly_penalty_by_amount");
							}
							Logger.Log("Trust", string.Format("[OverduePenalty] npc={0} debtId={1} mode={2} value={3} trustPersonal={4} trustPublic={5} relation={6} week={7}/{8}", hero.StringId, debtLine.DebtId, "amount_weekly", num3, num4, num5, num6, k, OverduePenaltyMaxWeeks));
						}
						debtLine.OverduePenaltyDaysApplied = num;
						debtLine.LastOverduePenaltyDay = campaignDayIndex;
					}
				}
				NormalizeDebtRecord(value);
				if (!HasDebtContent(value))
				{
					_debts.Remove(item);
					continue;
				}
				// The persistent quest journal replaces recurring hero debt pop-ups; overdue penalties above still apply.
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] OnDailyTick overdue penalty failed: " + ex.Message);
		}
	}
}
