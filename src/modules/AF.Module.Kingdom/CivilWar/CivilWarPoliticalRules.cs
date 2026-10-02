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
	internal static int GoldOffer(int tier) => tier == 1 ? 5000 : tier == 2 ? 10000 : 20000;
	internal static int InfluenceOffer(int tier) => tier == 1 ? 50 : tier == 2 ? 100 : 200;
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
