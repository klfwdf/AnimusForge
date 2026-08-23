using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
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
			plan.IncludeBoundVillageNotableRelations,
			plan.NotableRelationReason);
		result.NotableTrustAdjusted = ApplyNotableTrust(
			settlement,
			plan.NotableTrustDelta,
			plan.IncludeBoundVillageNotableTrust,
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
			+ " notableTrust=" + plan.NotableTrustDelta + "x" + result.NotableTrustAdjusted
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
		bool includeBoundVillages,
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
			foreach (Hero notable in EnumerateEffectNotables(settlement, includeBoundVillages))
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

	private static int ApplyNotableTrust(
		Settlement settlement,
		int delta,
		bool includeBoundVillages,
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
			foreach (Hero notable in EnumerateEffectNotables(settlement, includeBoundVillages))
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

	private static IEnumerable<Hero> EnumerateEffectNotables(Settlement settlement, bool includeBoundVillages)
	{
		if (settlement?.Notables != null)
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
}
