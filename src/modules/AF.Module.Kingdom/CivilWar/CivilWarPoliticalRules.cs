using System;
using System.Collections.Generic;

namespace AnimusForge;

internal static class CivilWarPoliticalRules
{
	internal const int PoliticalResultPriority = 80;
	internal static string PoliticalResultKind(string key)
	{
		return key.Contains(":war:outbreak") ? "civil_war"
			: key.Contains(":war:resolved") ? "civil_war_resolution" : "civil_war_politics";
	}

	internal const int ExitDays = 7, ActionDays = 7, ReplyDays = 3;
	internal const int SuppressCost = 100, DissolveCost = 200;
	internal const int SuppressInfluenceLoss = 20, SuppressLeaderGrievance = 15, SuppressMemberGrievance = 8;
	internal const int SuppressRelationLoss = 5, DissolveGrievance = 15, DissolveRelationLoss = 10;
	internal const int ExitLeaderRelationLoss = 20, ExitMemberRelationLoss = 10;
	internal const int FoundRulerRelationLoss = 10;
	internal const float SuppressLeaveMultiplier = 2.5f;
	// Compensation floats with the faction's weight instead of fixed tiers: leader clan tier, faction power and
	// grievance scale it, tier 1/2/3 multiply it x1/x2/x4, and a stable per-faction-week jitter (±15%) varies it
	// without changing between the quote and the execution of the same offer.
	internal static int GoldOffer(int tier, int leaderTier, float factionPower, float grievance01, string jitterKey)
		=> RoundTo(TierMultiplier(tier) * (1500f + 600f * Math.Max(0, leaderTier)) * Weight(factionPower, grievance01) * Jitter(jitterKey), 100, 500);
	internal static int InfluenceOffer(int tier, int leaderTier, float factionPower, float grievance01, string jitterKey)
		=> RoundTo(TierMultiplier(tier) * (25f + 8f * Math.Max(0, leaderTier)) * Weight(factionPower, grievance01) * Jitter(jitterKey), 5, 20);
	// Redress for an accepted demand: scales with the leader's tier, grievance and member count; never more than a quarter of the royal purse.
	internal static int RedressGold(int leaderTier, float grievance01, int members, int kingGold, string jitterKey)
	{
		float raw = (2000f + 1200f * Math.Max(0, leaderTier)) * (0.7f + 0.6f * CivilWarRules.Clamp(grievance01, 0f, 1f) + 0.1f * Math.Min(5, Math.Max(0, members - 1))) * Jitter(jitterKey);
		return Math.Min(Math.Max(0, kingGold / 4), RoundTo(raw, 100, 500));
	}
	private static float TierMultiplier(int tier) => tier <= 1 ? 1f : tier == 2 ? 2f : 4f;
	private static float Weight(float factionPower, float grievance01)
		=> 0.6f + 1.2f * CivilWarRules.Clamp(factionPower, 0f, 1f) + 0.4f * CivilWarRules.Clamp(grievance01, 0f, 1f);
	private static int RoundTo(float value, int step, int min) => Math.Max(min, (int)Math.Round(value / step) * step);
	// FNV-1a: string.GetHashCode is not stable across runtimes.
	internal static float Jitter(string key)
	{
		uint hash = 2166136261;
		foreach (char c in key ?? "") { hash ^= c; hash *= 16777619; }
		return 0.85f + 0.30f * (hash % 1000u) / 999f;
	}
	// Retention uses the existing leave model, scaled by how the new demand fits this clan's own grievances.
	internal static float DemandRetentionChance(float oldAffinity, float newAffinity, float leaveRaw, CivilWarTuning tuning)
		=> CivilWarRules.Shape((1f - CivilWarRules.Clamp(leaveRaw, 0f, 1f))
			* CivilWarRules.Clamp(newAffinity / Math.Max(.05f, oldAffinity), 0f, 1f), tuning);
	private static float F(IReadOnlyDictionary<string, float> f, string key) => f.TryGetValue(key, out float v) ? v : 0f;
	internal static float NegotiationChance(IReadOnlyDictionary<string, float> f, int tier, CivilWarTuning tuning)
		=> CivilWarRules.Shape(CivilWarCatalog.LeaveOpposition.Raw(f) + .30f * tier
			+ .10f * F(f, CivilWarFeature.LeaderCalculating) + .10f * F(f, CivilWarFeature.LeaderMercy)
			- .10f * F(f, CivilWarFeature.LeaderValor), tuning);
	internal static float GovernanceWeight(string action, IReadOnlyDictionary<string, float> f, CivilWarDemandDef demand)
	{
		float power = F(f, CivilWarFeature.FactionPower), authoritarian = F(f, CivilWarFeature.KingAuthoritarian);
		if (action == "negotiate") return Math.Max(0, .4f + .2f * F(f, CivilWarFeature.KingMercy) + .2f * F(f, CivilWarFeature.KingGenerosity) + .2f * power);
		if (action == "suppress") return Math.Max(0, .3f + .2f * authoritarian + .2f * (1 - power));
		if (action == "dissolve") return Math.Max(0, .1f + .4f * authoritarian + .3f * (1 - power) - .2f * F(f, CivilWarFeature.FactionGrievance));
		return Math.Max(0, demand?.Accept?.Raw(f) ?? 0);
	}
}
