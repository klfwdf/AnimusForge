using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>Result of one built-in topic route (auxiliary router, live semantic, or sticky fallback).</summary>
internal struct PromptTopicRoute
{
	internal bool Hit;
	internal string MatchedKeyword;
	internal float Score;

	internal string Describe() => string.IsNullOrWhiteSpace(MatchedKeyword) ? "" : $"{MatchedKeyword}@{Score:0.00}";
}

/// <summary>Semantic evaluator seam: the host supplies the live ONNX/keyword evaluation.</summary>
internal delegate bool PromptTopicSemanticEvaluator(string ruleTag, out string matchedKeyword, out float score);

/// <summary>
/// Pure routing for the built-in main-chain topics. When an auxiliary router result (or
/// forced preselection) exists it is authoritative; otherwise the host's semantic evaluator
/// decides. Runs once per prompt build; no allocation beyond the result struct.
/// </summary>
internal static class PromptBuiltInTopicRouter
{
	internal const string AuxiliaryRouterKeyword = "auxiliary_router";
	internal const string StickyKeyword = "sticky";
	internal const float StickyMinScore = 0.18f;

	internal static PromptTopicRoute Route(string ruleTag, bool allowRulePreprocess, bool topicEnabled, HashSet<string> excludedRuleIds,
		bool useAuxiliaryRuleHitSet, HashSet<string> auxiliaryRuleHitIdSet, PromptTopicSemanticEvaluator evaluate)
	{
		PromptTopicRoute route = default(PromptTopicRoute);
		route.MatchedKeyword = "";
		if (!allowRulePreprocess || !topicEnabled || PromptRuleIdPolicy.IsExcluded(excludedRuleIds, ruleTag))
		{
			return route;
		}
		if (useAuxiliaryRuleHitSet)
		{
			route.Hit = auxiliaryRuleHitIdSet != null && auxiliaryRuleHitIdSet.Contains(ruleTag);
			if (route.Hit)
			{
				route.MatchedKeyword = AuxiliaryRouterKeyword;
				route.Score = 1f;
			}
			return route;
		}
		if (evaluate == null)
		{
			return route;
		}
		route.Hit = evaluate(ruleTag, out route.MatchedKeyword, out route.Score);
		route.MatchedKeyword = route.MatchedKeyword ?? "";
		return route;
	}

	/// <summary>Sticky fallback only when no authoritative router ran and the topic was not already hit.</summary>
	internal static void ApplyStickyFallback(ref PromptTopicRoute route, bool allowStickyFallback, bool carry, HashSet<string> excludedRuleIds, string ruleTag)
	{
		if (allowStickyFallback && !route.Hit && carry && !PromptRuleIdPolicy.IsExcluded(excludedRuleIds, ruleTag))
		{
			route.Hit = true;
			route.MatchedKeyword = StickyKeyword;
			route.Score = Math.Max(route.Score, StickyMinScore);
		}
	}
}
