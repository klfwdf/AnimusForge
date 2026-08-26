using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	private sealed class TownSettlementEffectApplicationResult
	{
		public int BoundVillageTrustAdjusted { get; set; }

		public int NotableRelationsAdjusted { get; set; }

		public int NotableTrustAdjusted { get; set; }
	}

	private static TownSettlementEffectApplicationResult ApplyTownSettlementEffectPlan(
		Settlement settlement,
		TownSettlementEffectPlan plan)
	{
		var result = new TownSettlementEffectApplicationResult();
		if (settlement == null || plan == null || !plan.HasAnyEffect)
		{
			return result;
		}

		ApplySettlementPublicTrust(
			settlement,
			plan.SettlementPublicTrustDelta,
			plan.SettlementPublicTrustReason);
		result.BoundVillageTrustAdjusted = ApplyBoundVillagePublicTrust(
			settlement,
			plan.BoundVillagePublicTrustDelta,
			plan.BoundVillagePublicTrustReason);
		result.NotableRelationsAdjusted = ApplyNotableRelations(
			settlement,
			plan.NotableRelationDelta,
			plan.NotableRelationScope,
			plan.NotableRelationReason);
		result.NotableTrustAdjusted = ApplyNotableTrust(
			settlement,
			plan.NotableTrustDelta,
			plan.NotableTrustScope,
			plan.NotableTrustReason);

		try
		{
			if (settlement.Town != null)
			{
				if (plan.HasLoyaltyFloor)
				{
					settlement.Town.Loyalty = MathF.Max(settlement.Town.Loyalty, plan.LoyaltyFloor);
				}
				else if (Math.Abs(plan.LoyaltyDelta) > 0.001f)
				{
					settlement.Town.Loyalty += plan.LoyaltyDelta;
				}
				if (Math.Abs(plan.SecurityDelta) > 0.001f)
				{
					settlement.Town.Security += plan.SecurityDelta;
				}
				if (Math.Abs(plan.FoodStockDelta) > 0.001f)
				{
					settlement.Town.FoodStocks = MathF.Min(
						settlement.Town.FoodStocks + plan.FoodStockDelta,
						(float)settlement.Town.FoodStocksUpperLimit());
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Town settlement stat application failed. Key=" + plan.Key + ": " + ex.Message);
		}

		GcczDiagnosticLog.Log(
			"SettlementEffect",
			"key=" + plan.Key
			+ " settlement=" + (settlement.StringId ?? "N/A")
			+ " settlementTrust=" + plan.SettlementPublicTrustDelta
			+ " villageTrust=" + plan.BoundVillagePublicTrustDelta + "x" + result.BoundVillageTrustAdjusted
			+ " notableRelation=" + plan.NotableRelationDelta + "x" + result.NotableRelationsAdjusted
			+ " notableRelationScope=" + plan.NotableRelationScope
			+ " notableTrust=" + plan.NotableTrustDelta + "x" + result.NotableTrustAdjusted
			+ " notableTrustScope=" + plan.NotableTrustScope
			+ " loyaltyDelta=" + plan.LoyaltyDelta.ToString("0.##")
			+ " loyaltyFloor=" + (plan.HasLoyaltyFloor ? plan.LoyaltyFloor.ToString("0.##") : "none")
			+ " securityDelta=" + plan.SecurityDelta.ToString("0.##")
			+ " foodStockDelta=" + plan.FoodStockDelta.ToString("0.##"));
		return result;
	}

	private static void ApplySettlementPublicTrust(Settlement settlement, int delta, string reason)
	{
		try
		{
			if (settlement != null && delta != 0 && RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(settlement, delta, reason);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Settlement public trust application failed. Reason=" + (reason ?? "N/A") + ": " + ex.Message);
		}
	}

	private static int ApplyBoundVillagePublicTrust(Settlement settlement, int delta, string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement?.BoundVillages == null || delta == 0 || RewardSystemBehavior.Instance == null)
			{
				return 0;
			}

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Village village in settlement.BoundVillages)
			{
				Settlement villageSettlement = village?.Settlement;
				string key = villageSettlement?.StringId;
				if (villageSettlement == null || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}

				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(
					villageSettlement,
					delta,
					reason ?? "siege_ai_bound_village");
				adjusted++;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Bound village public trust application failed. Reason=" + (reason ?? "N/A") + ": " + ex.Message);
		}
		return adjusted;
	}

	private static int ApplyNotableRelations(
		Settlement settlement,
		int delta,
		TownNotableEffectScope scope,
		string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement == null || delta == 0)
			{
				return 0;
			}

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in EnumerateEffectNotables(settlement, scope))
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}

				try
				{
					ChangeRelationAction.ApplyPlayerRelation(notable, delta, true, true);
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Notable relation application failed. Reason=" + (reason ?? "N/A") + ", Notable=" + key + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Notable relation batch failed. Reason=" + (reason ?? "N/A") + ": " + ex.Message);
		}
		return adjusted;
	}

	private static void ApplyOwnerRelationDelta(Hero owner, int delta)
	{
		ChangeRelationAction.ApplyPlayerRelation(owner, delta, true, true);
	}

	private static int ApplyNotableTrust(
		Settlement settlement,
		int delta,
		TownNotableEffectScope scope,
		string reason)
	{
		int adjusted = 0;
		try
		{
			if (settlement == null || delta == 0 || RewardSystemBehavior.Instance == null)
			{
				return 0;
			}

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Hero notable in EnumerateEffectNotables(settlement, scope))
			{
				string key = notable?.StringId;
				if (notable == null || notable == Hero.MainHero || !notable.IsAlive || string.IsNullOrWhiteSpace(key) || !seen.Add(key))
				{
					continue;
				}

				try
				{
					RewardSystemBehavior.Instance.AdjustPersonalTrustWholeDeltaForExternal(
						notable,
						delta,
						reason ?? "siege_ai_notable_trust");
					adjusted++;
				}
				catch (Exception ex)
				{
					Logger.Log("SiegeAiIntervention", "Notable trust application failed. Reason=" + (reason ?? "N/A") + ", Notable=" + key + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Notable trust batch failed. Reason=" + (reason ?? "N/A") + ": " + ex.Message);
		}
		return adjusted;
	}

	private static IEnumerable<Hero> EnumerateEffectNotables(Settlement settlement, TownNotableEffectScope scope)
	{
		bool includeSettlement = scope != TownNotableEffectScope.BoundVillagesOnly;
		bool includeBoundVillages = scope == TownNotableEffectScope.SettlementAndBoundVillages
			|| scope == TownNotableEffectScope.BoundVillagesOnly;
		if (includeSettlement && settlement?.Notables != null)
		{
			foreach (Hero notable in settlement.Notables)
			{
				yield return notable;
			}
		}
		if (!includeBoundVillages || settlement?.BoundVillages == null)
		{
			yield break;
		}

		foreach (Village village in settlement.BoundVillages)
		{
			if (village?.Settlement?.Notables == null)
			{
				continue;
			}
			foreach (Hero notable in village.Settlement.Notables)
			{
				yield return notable;
			}
		}
	}

	private static void ApplyCivicChoiceSettlementEffects(
		Settlement settlement,
		SiegeCivicChoiceProfile profile,
		string settlementTrustReason,
		string boundVillageTrustReason)
	{
		try
		{
			if (settlement == null || profile == null)
			{
				return;
			}

			ApplyTownSettlementEffectPlan(
				settlement,
				new TownSettlementEffectPlan(
					profile.MessageKey,
					settlementPublicTrustDelta: ReducePositiveIntDeltaForRegionalConflict(profile.SettlementPublicTrustDelta, "civic_settlement_public_trust"),
					settlementPublicTrustReason: settlementTrustReason,
					boundVillagePublicTrustDelta: ReducePositiveIntDeltaForRegionalConflict(profile.BoundVillagePublicTrustDelta, "civic_bound_village_public_trust"),
					boundVillagePublicTrustReason: boundVillageTrustReason,
					loyaltyDelta: profile.LocksLoyalty ? 0f : ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyDelta, "civic_loyalty"),
					securityDelta: ReducePositiveFloatDeltaForRegionalConflict(profile.SecurityDelta, "civic_security"),
					hasLoyaltyFloor: profile.LocksLoyalty,
					loyaltyFloor: profile.LocksLoyalty ? ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyLockValue, "civic_loyalty_lock") : 0f));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivicChoiceSettlementEffects failed: " + ex.Message);
		}
	}

	private static void ApplyFinalizedSettlementOutcomeEffects(
		Settlement settlement,
		SiegeSettlementOutcomeProfile profile,
		float prosperityBeforeNativeAftermath,
		bool applyLegacyRelationshipEffects = true)
	{
		try
		{
			if (settlement == null || profile == null)
			{
				return;
			}

			var relationshipEffects = new TownSettlementEffectApplicationResult();
			if (applyLegacyRelationshipEffects)
			{
				relationshipEffects = ApplyTownSettlementEffectPlan(
					settlement,
					TownSettlementEffectPlan.FromFinalOutcome(profile));
			}
			float prosperityAfterNativeAftermath = settlement.Town?.Prosperity ?? prosperityBeforeNativeAftermath;
			float nativeProsperityDelta = prosperityAfterNativeAftermath - prosperityBeforeNativeAftermath;
			float extraProsperityDelta = 0f;
			if (profile.AppliesAdditionalNativeDevastateProsperityPenalty)
			{
				extraProsperityDelta = ApplyExtraNativeDevastateProsperityPenalty(
					settlement,
					prosperityBeforeNativeAftermath,
					profile.NativeDevastateProsperityMultiplier);
			}
			if (profile.ResetsLoyaltyToInitial && settlement.Town != null)
			{
				settlement.Town.Loyalty = SiegeSettlementOutcomeProfile.CulturalRepopulationInitialLoyalty;
			}
			if (profile.AppliesProsperityGrowthDebuff)
			{
				BeginRepopulationProsperityGrowthDebuff(settlement);
			}
			if (profile.AppliesRecruitmentSlowdown)
			{
				BeginRecruitmentSlowdownDebuff(settlement, profile);
			}
			float prosperityAfterAllEffects = settlement.Town?.Prosperity ?? prosperityAfterNativeAftermath;
			Logger.Log("SiegeAiIntervention", $"Applied finalized GCCZ settlement outcome. Key={profile.Key}, Settlement={settlement.StringId}, RelationshipEffects={applyLegacyRelationshipEffects}, SettlementTrust={(applyLegacyRelationshipEffects ? profile.SettlementPublicTrustDelta : 0)}, VillageTrust={(applyLegacyRelationshipEffects ? profile.BoundVillagePublicTrustDelta : 0)}x{relationshipEffects.BoundVillageTrustAdjusted}, NotableRelation={(applyLegacyRelationshipEffects ? profile.NotableRelationDelta : 0)}x{relationshipEffects.NotableRelationsAdjusted}, NotableTrust={(applyLegacyRelationshipEffects ? profile.NotableTrustDelta : 0)}x{relationshipEffects.NotableTrustAdjusted}, ProsperityBefore={prosperityBeforeNativeAftermath:0.##}, NativeProsperityDelta={nativeProsperityDelta:0.##}, ExtraProsperityDelta={extraProsperityDelta:0.##}, ProsperityAfter={prosperityAfterAllEffects:0.##}, TotalProsperityDelta={prosperityAfterAllEffects - prosperityBeforeNativeAftermath:0.##}");
			GcczDiagnosticLog.Log("SettlementOutcome", $"key={profile.Key} settlement={settlement.StringId} prosperityBefore={prosperityBeforeNativeAftermath:0.##} nativeDelta={nativeProsperityDelta:0.##} extraDelta={extraProsperityDelta:0.##} prosperityAfter={prosperityAfterAllEffects:0.##} totalDelta={prosperityAfterAllEffects - prosperityBeforeNativeAftermath:0.##} recruitmentRateMultiplier={profile.RecruitmentRateMultiplier:0.##}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyFinalizedSettlementOutcomeEffects failed: " + ex.Message);
		}
	}
	private static float ApplyExtraNativeDevastateProsperityPenalty(Settlement settlement, float prosperityBeforeNativeAftermath, float totalMultiplier)
	{
		try
		{
			if (settlement?.Town == null || totalMultiplier <= 1f)
			{
				return 0f;
			}
			float nativeDelta = settlement.Town.Prosperity - prosperityBeforeNativeAftermath;
			if (nativeDelta >= 0f)
			{
				return 0f;
			}
			float prosperityBeforeExtraPenalty = settlement.Town.Prosperity;
			float requestedExtraDelta = nativeDelta * (totalMultiplier - 1f);
			settlement.Town.Prosperity = MathF.Max(0f, prosperityBeforeExtraPenalty + requestedExtraDelta);
			return settlement.Town.Prosperity - prosperityBeforeExtraPenalty;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraNativeDevastateProsperityPenalty failed: " + ex.Message);
			return 0f;
		}
	}

	private static void BeginRepopulationProsperityGrowthDebuff(Settlement settlement)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthDebuffYears);
			_repopulationProsperityDebuffUntilDayBySettlement[key] = untilDay;
			_repopulationProsperityLastObservedBySettlement[key] = settlement.Town.Prosperity;
			Logger.Log("SiegeAiIntervention", $"Applied repopulation prosperity growth debuff. Settlement={key}, UntilDay={untilDay}, Reduction={SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthReductionRatio:P0}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginRepopulationProsperityGrowthDebuff failed: " + ex.Message);
		}
	}

	private static void ApplyRepopulationProsperityGrowthDebuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_repopulationProsperityDebuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_repopulationProsperityDebuffUntilDayBySettlement.Remove(key);
				_repopulationProsperityLastObservedBySettlement.Remove(key);
				return;
			}
			float current = town.Prosperity;
			if (!_repopulationProsperityLastObservedBySettlement.TryGetValue(key, out float last))
			{
				_repopulationProsperityLastObservedBySettlement[key] = current;
				return;
			}
			float growth = current - last;
			if (growth > 0.01f)
			{
				float reduction = growth * SiegeSettlementOutcomeProfile.CulturalRepopulationProsperityGrowthReductionRatio;
				town.Prosperity = MathF.Max(0f, current - reduction);
				current = town.Prosperity;
				Logger.Log("SiegeAiIntervention", $"Repopulation prosperity growth debuff applied. Settlement={key}, Growth={growth:0.##}, Reduction={reduction:0.##}, UntilDay={untilDay}");
			}
			_repopulationProsperityLastObservedBySettlement[key] = current;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRepopulationProsperityGrowthDebuff failed: " + ex.Message);
		}
	}

	private static void ClearRepopulationProsperityDebuffs()
	{
		_repopulationProsperityDebuffUntilDayBySettlement.Clear();
		_repopulationProsperityLastObservedBySettlement.Clear();
	}

	private static void BeginRecruitmentSlowdownDebuff(Settlement settlement, SiegeSettlementOutcomeProfile profile)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null || profile == null || !profile.AppliesRecruitmentSlowdown)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * profile.RecruitmentSlowdownYears);
			_recruitmentSlowdownUntilDayBySettlement[key] = untilDay;
			Logger.Log("SiegeAiIntervention", $"Applied recruitment slowdown. Settlement={key}, UntilDay={untilDay}, Rate={profile.RecruitmentRateMultiplier:P0}, Reason={profile.RecruitmentSlowdownReason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginRecruitmentSlowdownDebuff failed: " + ex.Message);
		}
	}

	private static void ApplyRecruitmentSlowdownDebuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_recruitmentSlowdownUntilDayBySettlement.TryGetValue(key, out int untilDay))
			{
				return;
			}
			if (today > untilDay)
			{
				_recruitmentSlowdownUntilDayBySettlement.Remove(key);
				Logger.Log("SiegeAiIntervention", $"Recruitment slowdown expired. Settlement={key}, UntilDay={untilDay}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRecruitmentSlowdownDebuff failed: " + ex.Message);
		}
	}

	internal static bool TryGetActiveRecruitmentRateMultiplier(Settlement settlement, out float multiplier)
	{
		multiplier = 1f;
		try
		{
			string key = settlement?.IsVillage == true
				? settlement.Village?.Bound?.StringId
				: settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key)
				|| !_recruitmentSlowdownUntilDayBySettlement.TryGetValue(key, out int untilDay))
			{
				return false;
			}
			if (GetCurrentCampaignDay() > untilDay)
			{
				_recruitmentSlowdownUntilDayBySettlement.Remove(key);
				return false;
			}
			multiplier = SiegeSettlementOutcomeProfile.DestructiveRecruitmentRateMultiplier;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "TryGetActiveRecruitmentRateMultiplier failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
			return false;
		}
	}

	private static void ClearRecruitmentSlowdownDebuffs()
	{
		_recruitmentSlowdownUntilDayBySettlement.Clear();
	}


	private static void BeginCivicPositiveBuff(Settlement settlement, SiegeCivicChoiceProfile profile)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key) || settlement?.Town == null || profile == null || profile.EffectYears <= 0)
			{
				return;
			}
			int untilDay = GetCurrentCampaignDay() + Math.Max(1, CampaignTime.DaysInYear * profile.EffectYears);
			if (profile.HasProsperityGrowthBuff)
			{
				_civicProsperityBuffUntilDayBySettlement[key] = untilDay;
				_civicProsperityLastObservedBySettlement[key] = settlement.Town.Prosperity;
				_civicProsperityGrowthMultiplierBySettlement[key] = MathF.Max(profile.ProsperityGrowthMultiplier, ResolveExistingCivicProsperityMultiplier(key));
			}
			if (profile.LocksLoyalty)
			{
				float adjustedLockValue = ReducePositiveFloatDeltaForRegionalConflict(profile.LoyaltyLockValue, "civic_buff_loyalty_lock");
				_rallyOathLoyaltyLockUntilDayBySettlement[key] = untilDay;
				_rallyOathLoyaltyLockValueBySettlement[key] = adjustedLockValue;
				settlement.Town.Loyalty = MathF.Max(settlement.Town.Loyalty, adjustedLockValue);
			}
			if (profile.HasRecruitmentSpeedBuff)
			{
				_rallyOathRecruitmentBuffUntilDayBySettlement[key] = untilDay;
				int changed = ApplyExtraVolunteerProductionForSettlementAndBoundVillages(settlement);
				if (changed > 0)
				{
					Logger.Log("SiegeAiIntervention", $"Applied immediate rally oath recruitment speed buff. Settlement={key}, ChangedSlots={changed}, UntilDay={untilDay}");
				}
			}
			Logger.Log("SiegeAiIntervention", $"Applied civic positive buff. Settlement={key}, UntilDay={untilDay}, ProsperityMultiplier={profile.ProsperityGrowthMultiplier:0.##}, RecruitmentMultiplier={profile.RecruitmentSpeedMultiplier:0.##}, LoyaltyLock={profile.LocksLoyalty}, RegionalConflictIncidents={_regionalConflictIncidentCount}");
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "BeginCivicPositiveBuff failed: " + ex.Message);
		}
	}

	private static float ResolveExistingCivicProsperityMultiplier(string key)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(key) && _civicProsperityGrowthMultiplierBySettlement.TryGetValue(key, out float multiplier)
				? multiplier
				: 1f;
		}
		catch
		{
			return 1f;
		}
	}

	private static void ApplyCivicProsperityGrowthBuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_civicProsperityBuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_civicProsperityBuffUntilDayBySettlement.Remove(key);
				_civicProsperityLastObservedBySettlement.Remove(key);
				_civicProsperityGrowthMultiplierBySettlement.Remove(key);
				return;
			}
			float current = town.Prosperity;
			if (!_civicProsperityLastObservedBySettlement.TryGetValue(key, out float last))
			{
				_civicProsperityLastObservedBySettlement[key] = current;
				return;
			}
			float growth = current - last;
			float multiplier = MathF.Max(1f, ResolveExistingCivicProsperityMultiplier(key));
			if (growth > 0.01f && multiplier > 1.001f)
			{
				float extra = growth * (multiplier - 1f);
				town.Prosperity = MathF.Max(0f, current + extra);
				current = town.Prosperity;
				Logger.Log("SiegeAiIntervention", $"Civic prosperity growth buff applied. Settlement={key}, Growth={growth:0.##}, Extra={extra:0.##}, Multiplier={multiplier:0.##}, UntilDay={untilDay}");
			}
			_civicProsperityLastObservedBySettlement[key] = current;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyCivicProsperityGrowthBuff failed: " + ex.Message);
		}
	}

	private static void ApplyRallyOathLoyaltyLock(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_rallyOathLoyaltyLockUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_rallyOathLoyaltyLockUntilDayBySettlement.Remove(key);
				_rallyOathLoyaltyLockValueBySettlement.Remove(key);
				return;
			}
			float lockValue = _rallyOathLoyaltyLockValueBySettlement.TryGetValue(key, out float savedLockValue)
				? savedLockValue
				: SiegeCivicChoiceProfile.RallyOathLoyaltyValue;
			town.Loyalty = MathF.Max(town.Loyalty, lockValue);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRallyOathLoyaltyLock failed: " + ex.Message);
		}
	}

	private static void ApplyRallyOathRecruitmentBuff(Town town)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			int today = GetCurrentCampaignDay();
			if (!_rallyOathRecruitmentBuffUntilDayBySettlement.TryGetValue(key, out int untilDay) || today > untilDay)
			{
				_rallyOathRecruitmentBuffUntilDayBySettlement.Remove(key);
				return;
			}
			int changed = ApplyExtraVolunteerProductionForSettlementAndBoundVillages(settlement);
			if (changed > 0)
			{
				Logger.Log("SiegeAiIntervention", $"Rally oath recruitment speed buff applied. Settlement={key}, ChangedSlots={changed}, UntilDay={untilDay}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyRallyOathRecruitmentBuff failed: " + ex.Message);
		}
	}

	private static int ApplyExtraVolunteerProductionForSettlementAndBoundVillages(Settlement settlement)
	{
		int changed = ApplyExtraVolunteerProductionForSettlement(settlement);
		try
		{
			if (settlement?.BoundVillages != null)
			{
				foreach (Village village in settlement.BoundVillages)
				{
					changed += ApplyExtraVolunteerProductionForSettlement(village?.Settlement);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForSettlementAndBoundVillages failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static int ApplyExtraVolunteerProductionForSettlement(Settlement settlement)
	{
		int changed = 0;
		try
		{
			if (settlement?.Notables == null || Campaign.Current?.Models?.VolunteerModel == null)
			{
				return 0;
			}
			if (settlement.IsTown && settlement.Town?.InRebelliousState == true)
			{
				return 0;
			}
			if (settlement.IsVillage && settlement.Village?.Bound?.Town?.InRebelliousState == true)
			{
				return 0;
			}
			foreach (Hero hero in settlement.Notables.ToList())
			{
				changed += ApplyExtraVolunteerProductionForNotable(hero, settlement);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForSettlement failed. Settlement=" + (settlement?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static int ApplyExtraVolunteerProductionForNotable(Hero notable, Settlement settlement)
	{
		int changed = 0;
		try
		{
			if (notable == null || !notable.IsAlive || !notable.CanHaveRecruits || notable.VolunteerTypes == null || settlement == null)
			{
				return 0;
			}
			CharacterObject basicVolunteer = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(notable);
			int slots = Math.Min(6, notable.VolunteerTypes.Length);
			for (int i = 0; i < slots; i++)
			{
				if (MBRandom.RandomFloat >= Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(notable, i, settlement))
				{
					continue;
				}
				CharacterObject current = notable.VolunteerTypes[i];
				if (current == null)
				{
					notable.VolunteerTypes[i] = basicVolunteer;
					changed++;
				}
				else if (current.UpgradeTargets != null && current.UpgradeTargets.Length != 0 && current.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
				{
					float upgradeProbability = MathF.Log(MathF.Max(1f, notable.Power) / MathF.Max(1f, (float)current.Tier), 2f) * 0.01f;
					if (MBRandom.RandomFloat < upgradeProbability)
					{
						notable.VolunteerTypes[i] = current.UpgradeTargets[MBRandom.RandomInt(current.UpgradeTargets.Length)];
						changed++;
					}
				}
			}
			if (changed > 0)
			{
				SortVolunteerSlots(notable.VolunteerTypes, slots);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "ApplyExtraVolunteerProductionForNotable failed. Notable=" + (notable?.StringId ?? "N/A") + ": " + ex.Message);
		}
		return changed;
	}

	private static void SortVolunteerSlots(CharacterObject[] volunteerTypes, int slots)
	{
		try
		{
			for (int j = 1; j < slots; j++)
			{
				CharacterObject character = volunteerTypes[j];
				if (character == null)
				{
					continue;
				}
				int emptySlots = 0;
				int previousIndex = j - 1;
				CharacterObject previous = volunteerTypes[previousIndex];
				while (previousIndex >= 0 && (previous == null || GetVolunteerSortValue(character) < GetVolunteerSortValue(previous)))
				{
					if (previous == null)
					{
						previousIndex--;
						emptySlots++;
						if (previousIndex >= 0)
						{
							previous = volunteerTypes[previousIndex];
						}
					}
					else
					{
						volunteerTypes[previousIndex + 1 + emptySlots] = previous;
						previousIndex--;
						emptySlots = 0;
						if (previousIndex >= 0)
						{
							previous = volunteerTypes[previousIndex];
						}
					}
				}
				volunteerTypes[previousIndex + 1 + emptySlots] = character;
			}
		}
		catch
		{
		}
	}

	private static float GetVolunteerSortValue(CharacterObject character)
	{
		return character == null ? float.MaxValue : character.Level + (character.IsMounted ? 0.5f : 0f);
	}

	private static void ClearCivicPositiveBuffForSettlement(Settlement settlement)
	{
		try
		{
			string key = settlement?.StringId;
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			_civicProsperityBuffUntilDayBySettlement.Remove(key);
			_civicProsperityLastObservedBySettlement.Remove(key);
			_civicProsperityGrowthMultiplierBySettlement.Remove(key);
			_rallyOathLoyaltyLockUntilDayBySettlement.Remove(key);
			_rallyOathLoyaltyLockValueBySettlement.Remove(key);
			_rallyOathRecruitmentBuffUntilDayBySettlement.Remove(key);
		}
		catch
		{
		}
	}

	private static void ClearCivicPositiveBuffs()
	{
		_civicProsperityBuffUntilDayBySettlement.Clear();
		_civicProsperityLastObservedBySettlement.Clear();
		_civicProsperityGrowthMultiplierBySettlement.Clear();
		_rallyOathLoyaltyLockUntilDayBySettlement.Clear();
		_rallyOathLoyaltyLockValueBySettlement.Clear();
		_rallyOathRecruitmentBuffUntilDayBySettlement.Clear();
	}

}
