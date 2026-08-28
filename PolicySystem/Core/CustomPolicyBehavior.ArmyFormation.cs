using System;
using System.Collections.Generic;
using System.Reflection;
using AnimusForge.PolicyEffects;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private const string PolicyArmyFormationHarmonyId = "com.AnimusForge.custompolicy.armyformation";

	private static bool _policyArmyFormationPatchesApplied;

	private static void ApplyPolicyArmyFormationPatchesOnce()
	{
		if (_policyArmyFormationPatchesApplied)
		{
			return;
		}

		try
		{
			Type tupleType = typeof(ValueTuple<AIBehaviorData, float>);
			MethodInfo target = AccessTools.Method(
				typeof(PartyThinkParams),
				nameof(PartyThinkParams.AddBehaviorScore),
				new[] { tupleType.MakeByRefType() });
			ParameterInfo[] parameters = target?.GetParameters();
			if (target == null
				|| parameters == null
				|| parameters.Length != 1
				|| !parameters[0].ParameterType.IsByRef
				|| parameters[0].ParameterType.GetElementType() != tupleType
				|| !parameters[0].IsIn
				|| parameters[0].IsOut)
			{
				throw new MissingMethodException(
					typeof(PartyThinkParams).FullName,
					"AddBehaviorScore(in ValueTuple<AIBehaviorData, float>)");
			}

			Harmony harmony = new Harmony(PolicyArmyFormationHarmonyId);
			harmony.Patch(
				target,
				prefix: new HarmonyMethod(
					typeof(CustomPolicyBehavior),
					nameof(Patch_PolicyArmyFormationScore_Prefix)));
			_policyArmyFormationPatchesApplied = true;
			PolicySystemLog.Write(
				"Effect",
				"army-formation-score-patch-applied",
				"AF policy effects now scale vanilla-qualified WillGatherArmy candidate scores");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "army-formation-score-patch-failed", ex.ToString());
		}
	}

	private static void Patch_PolicyArmyFormationScore_Prefix(
		PartyThinkParams __instance,
		ref ValueTuple<AIBehaviorData, float> __0)
	{
		if (!__0.Item1.WillGatherArmy)
		{
			return;
		}

		CustomPolicyBehavior behavior = Instance;
		MobileParty mobileParty = __instance?.MobilePartyOf;
		string clanId = mobileParty?.LeaderHero?.Clan?.StringId;
		if (behavior == null || string.IsNullOrEmpty(clanId))
		{
			return;
		}

		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = behavior._policyEffectRuntimeIndex.GetContributions(
			PolicyEffectHook.ArmyFormationScore,
			PolicyEffectTargetKind.Clan,
			clanId);
		if (contributions.Count == 0)
		{
			return;
		}

		float adjustedScore = CalculatePolicyArmyFormationAdjustedScore(__0.Item2, contributions);
		if (!adjustedScore.Equals(__0.Item2))
		{
			__0 = new ValueTuple<AIBehaviorData, float>(__0.Item1, adjustedScore);
		}
	}

	internal static float CalculatePolicyArmyFormationAdjustedScore(
		float originalScore,
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions)
	{
		if (float.IsNaN(originalScore)
			|| float.IsInfinity(originalScore)
			|| contributions == null
			|| contributions.Count == 0)
		{
			return originalScore;
		}

		double totalPercent = 0d;
		bool hasContribution = false;
		for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			if (contribution == null
				|| contribution.Aggregation != PolicyEffectAggregationKind.Additive
				|| float.IsNaN(contribution.Value)
				|| float.IsInfinity(contribution.Value))
			{
				continue;
			}
			totalPercent += contribution.Value;
			hasContribution = true;
		}
		if (!hasContribution)
		{
			return originalScore;
		}

		double multiplier = Math.Max(0d, Math.Min(3d, 1d + totalPercent / 100d));
		double adjustedScore = originalScore * multiplier;
		if (adjustedScore >= float.MaxValue)
		{
			return float.MaxValue;
		}
		if (adjustedScore <= float.MinValue)
		{
			return float.MinValue;
		}
		return (float)adjustedScore;
	}
}
