using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>Detached input for the topic routing stage: strings, sets and flags only.</summary>
internal sealed class PromptRoutingInput
{
	internal string Input;
	internal string NpcLastUtterance;
	internal bool HasAnyHero;
	internal bool AllowRulePreprocess;
	internal bool BypassRulePreprocess;
	internal bool UseAuxiliaryRuleApi;
	internal int AuxiliaryReturnCap;
	internal bool RewardEnabled;
	internal bool LoanEnabled;
	internal bool SurroundingsEnabled;
	internal HashSet<string> ExcludedRuleIds;
	internal HashSet<string> PreprocessExcludedRuleIds;
	internal IEnumerable<string> ForcedPreprocessRuleIds;
	internal string StickyTargetKey;
}

/// <summary>Host seams the routing stage needs; every delegate may touch network/ONNX but no game objects.</summary>
internal sealed class PromptRoutingPorts
{
	/// <summary>Auxiliary router: returns hits and merges discovered mentions into the supplied accumulator.</summary>
	internal Func<string, string, int, HashSet<string>, MentionedWorldEntities, List<GuardrailRuleHit>> AuxiliaryHits;
	/// <summary>Built-in topic evaluator factory: (ruleTag, input, secondaryInput, excludedRuleIds) → evaluator.</summary>
	internal Func<string, string, string, HashSet<string>, PromptTopicSemanticEvaluator> SemanticEvaluator;
	/// <summary>Runtime gate for preselected ids (kingdom_vassalage/diplomacy/...).</summary>
	internal Func<string, bool, bool> CanInjectGatedRule;
	internal BuiltInRuleStickyCarry StickyCarry;
	internal Action<string, string> Log;
}

/// <summary>Result of the routing stage; the host uses it to decide which sections to capture.</summary>
internal sealed class PromptRoutingResult
{
	internal List<string> AuxiliaryRuleHitIds;
	internal bool UseAuxiliaryRuleHitSet;
	internal List<string> ForcedRuleHitIds = new List<string>();
	internal PromptTopicRoute Duel, Reward, Loan, Surroundings, KingdomService, Marriage, PartyTransfer, WorldMapPartyCommand;
	internal bool LiveDuelSemanticHit, LiveRewardSemanticHit, LiveLoanSemanticHit;
	internal bool StickySuppressed;
	internal bool CarryDuel, CarryReward, CarryLoan;
	internal MentionedWorldEntities AuxiliaryMentions = new MentionedWorldEntities();
	internal string AuxiliaryFailure;
}

/// <summary>
/// Routing stage of the shared main-chain prompt: auxiliary router → forced preselection →
/// built-in topic evaluation → sticky fallback/prime. Runs once per prompt build; the only
/// state it mutates is the supplied sticky carry. Exceptions of type PreprocessFormatException
/// propagate (legacy contract); other auxiliary failures fall back to live semantic routing.
/// </summary>
internal static class PromptTopicRoutingStage
{
	internal static PromptRoutingResult Run(PromptRoutingInput input, PromptRoutingPorts ports)
	{
		if (input == null) throw new ArgumentNullException(nameof(input));
		if (ports == null) throw new ArgumentNullException(nameof(ports));
		PromptRoutingResult result = new PromptRoutingResult();
		HashSet<string> auxiliarySet = null;
		if (input.AllowRulePreprocess && input.UseAuxiliaryRuleApi && ports.AuxiliaryHits != null)
		{
			try
			{
				List<GuardrailRuleHit> hits = ports.AuxiliaryHits(input.Input, input.NpcLastUtterance, input.AuxiliaryReturnCap, input.PreprocessExcludedRuleIds, result.AuxiliaryMentions);
				result.AuxiliaryRuleHitIds = PromptRuleIdPolicy.CollectAuxiliaryHitIds(hits, input.PreprocessExcludedRuleIds);
				auxiliarySet = new HashSet<string>(result.AuxiliaryRuleHitIds, StringComparer.OrdinalIgnoreCase);
				result.UseAuxiliaryRuleHitSet = true;
			}
			catch (PreprocessFormatException)
			{
				throw;
			}
			catch (Exception ex)
			{
				result.AuxiliaryFailure = ex.Message;
				result.AuxiliaryRuleHitIds = null;
				auxiliarySet = null;
				result.UseAuxiliaryRuleHitSet = false;
			}
		}
		if (!input.BypassRulePreprocess)
		{
			foreach (string id in PromptRuleIdPolicy.NormalizePreselectedRuleIds(input.ForcedPreprocessRuleIds))
			{
				if (PromptRuleIdPolicy.IsExcluded(input.PreprocessExcludedRuleIds, id)) continue;
				if (PromptRuleIdPolicy.IsRuntimeGatedPreprocessRuleId(id) && !(ports.CanInjectGatedRule != null && ports.CanInjectGatedRule(id, input.HasAnyHero))) continue;
				result.ForcedRuleHitIds.Add(id);
			}
		}
		if (result.ForcedRuleHitIds.Count > 0)
		{
			if (result.AuxiliaryRuleHitIds == null) result.AuxiliaryRuleHitIds = new List<string>();
			PromptRuleIdPolicy.MergeForcedHitIds(result.AuxiliaryRuleHitIds, result.ForcedRuleHitIds);
			auxiliarySet = new HashSet<string>(result.AuxiliaryRuleHitIds, StringComparer.OrdinalIgnoreCase);
			result.UseAuxiliaryRuleHitSet = true;
		}

		PromptTopicSemanticEvaluator Eval(string tag) => ports.SemanticEvaluator?.Invoke(tag, input.Input, input.NpcLastUtterance, input.ExcludedRuleIds);
		bool allow = input.AllowRulePreprocess;
		bool useAux = result.UseAuxiliaryRuleHitSet;
		result.Duel = PromptBuiltInTopicRouter.Route("duel", allow, true, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("duel"));
		result.LiveDuelSemanticHit = result.Duel.Hit;
		result.Reward = PromptBuiltInTopicRouter.Route("reward", allow, input.RewardEnabled, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("reward"));
		result.LiveRewardSemanticHit = result.Reward.Hit;
		result.Loan = PromptBuiltInTopicRouter.Route("loan", allow, input.LoanEnabled, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("loan"));
		result.LiveLoanSemanticHit = result.Loan.Hit;
		result.Surroundings = PromptBuiltInTopicRouter.Route("surroundings", allow, input.SurroundingsEnabled, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("surroundings"));
		result.KingdomService = PromptBuiltInTopicRouter.Route("kingdom_service", allow, true, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("kingdom_service"));
		result.Marriage = PromptBuiltInTopicRouter.Route("marriage", allow, true, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("marriage"));
		result.PartyTransfer = PromptBuiltInTopicRouter.Route("party_transfer", allow, true, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("party_transfer"));
		result.WorldMapPartyCommand = PromptBuiltInTopicRouter.Route("worldmap_party_command", allow, true, input.ExcludedRuleIds, useAux, auxiliarySet, Eval("worldmap_party_command"));

		BuiltInRuleStickyCarry carry = ports.StickyCarry;
		if (allow && carry != null && carry.TryConsume(input.StickyTargetKey, input.Input, out result.CarryDuel, out result.CarryReward, out result.CarryLoan, out string consumeLog))
		{
			ports.Log?.Invoke("GuardrailSemantic", consumeLog);
			// A completed auxiliary/preselected routing result is authoritative for this turn.
			// A generic short acknowledgement must not resurrect a stale topic that the router omitted.
			bool allowStickyFallback = !useAux;
			result.StickySuppressed = !allowStickyFallback && (result.CarryDuel || result.CarryReward || result.CarryLoan);
			PromptBuiltInTopicRouter.ApplyStickyFallback(ref result.Duel, allowStickyFallback, result.CarryDuel, input.ExcludedRuleIds, "duel");
			PromptBuiltInTopicRouter.ApplyStickyFallback(ref result.Reward, allowStickyFallback, result.CarryReward, input.ExcludedRuleIds, "reward");
			PromptBuiltInTopicRouter.ApplyStickyFallback(ref result.Loan, allowStickyFallback, result.CarryLoan, input.ExcludedRuleIds, "loan");
		}
		if (allow && carry != null && carry.Prime(input.StickyTargetKey, result.LiveDuelSemanticHit, result.LiveRewardSemanticHit, result.LiveLoanSemanticHit, out string primeLog))
		{
			ports.Log?.Invoke("GuardrailSemantic", primeLog);
		}
		return result;
	}

	internal static string DescribeHits(List<string> ids, string whenNull = "(skip)")
		=> ids == null ? whenNull : (ids.Count == 0 ? "(none)" : string.Join(",", ids));
}
