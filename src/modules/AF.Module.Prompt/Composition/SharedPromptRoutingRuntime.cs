using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;
internal sealed class SharedPromptRoutingWork
{
    internal readonly PromptBuildPhases Phases;
    internal readonly PromptRoutingInput Input;
    internal readonly PromptRoutingPorts Ports;
    internal SharedPromptRoutingWork(PromptBuildPhases phases, PromptRoutingInput input, PromptRoutingPorts ports)
    { Phases = phases ?? throw new ArgumentNullException(nameof(phases)); Input = input ?? throw new ArgumentNullException(nameof(input)); Ports = ports ?? throw new ArgumentNullException(nameof(ports)); }
}
internal static class SharedPromptRoutingRuntime
{
internal static void Run(SharedPromptRoutingWork work)
	{
		PromptBuildPhases phases = work.Phases;
        PromptBuildRequest request = phases.Request;
		if (!request.SuppressDynamicRuleAndLore)
		{
			AIConfigHandler.ClearLatestAuxiliaryMentionedEntitiesForExternal();
		}
		AIConfigHandler.SetGuardrailSemanticContext(request.GuardrailSemanticContext);
		if (request.BypassRulePreprocess)
		{
			Logger.Log("Logic", "[RuleInjectionDebug] stage=single_aux_preprocess skipped=gccz_active targetHero=" + (request.TargetHeroId ?? "null") + " targetCharacter=" + (request.TargetCharacterId ?? "null"));
		}
		PromptRoutingInput routingInput = work.Input;
		PromptRoutingResult routing = PromptTopicRoutingStage.Run(routingInput, work.Ports);
		phases.Routing = routing;
		phases.DirectPreprocessMentions.Merge(routing.AuxiliaryMentions);
		LogPromptRoutingDiagnostics(request, routing, routingInput);
		MyBehavior.LogShoutPromptContextStage("aux_preprocess_done", phases.TotalStopwatch, phases.StageStopwatch, request, "auxHits=" + PromptTopicRoutingStage.DescribeHits(routing.AuxiliaryRuleHitIds) + " forcedHits=" + PromptTopicRoutingStage.DescribeHits(routing.ForcedRuleHitIds, "(none)"));
		PromptRetrievalCapture retrieval = new PromptRetrievalCapture();
		if (!request.SuppressDynamicRuleAndLore)
		{
			// Full mention set for this build: caller-supplied + router-discovered + mention store + latest.
			// The capture phase uses this same detached mention set for Lore and entity candidates.
			MentionedWorldEntities mentions = phases.DirectPreprocessMentions.Clone();
			mentions.Merge(AIConfigHandler.GetAuxiliaryMentionedEntitiesForExternal(request.Input, request.NpcLastUtterance, request.GuardrailSemanticContext));
			mentions.Merge(AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal());
			retrieval.AuxiliaryMentions = mentions;
		}
		phases.Retrieval = retrieval;
	}
internal static PromptRoutingInput CaptureInput(PromptBuildRequest request)
	{
		return new PromptRoutingInput
		{
			Input = request.Input,
			NpcLastUtterance = request.NpcLastUtterance,
			HasAnyHero = request.HasAnyHero,
			AllowRulePreprocess = request.AllowRulePreprocess,
			BypassRulePreprocess = request.BypassRulePreprocess,
			UseAuxiliaryRuleApi = AIConfigHandler.UseAuxiliaryRuleApiRetrieval,
			AuxiliaryReturnCap = AIConfigHandler.GuardrailRuleReturnCap,
			RewardEnabled = AIConfigHandler.RewardEnabled,
			LoanEnabled = AIConfigHandler.LoanEnabled,
			SurroundingsEnabled = AIConfigHandler.SurroundingsEnabled,
			ExcludedRuleIds = request.ExcludedRuleIds == null ? null : new HashSet<string>(request.ExcludedRuleIds, request.ExcludedRuleIds.Comparer),
			PreprocessExcludedRuleIds = request.PreprocessExcludedRuleIds == null ? null : new HashSet<string>(request.PreprocessExcludedRuleIds, request.PreprocessExcludedRuleIds.Comparer),
			ForcedPreprocessRuleIds = request.ForcedPreprocessRuleIds?.ToArray(),
			StickyTargetKey = request.StickyTargetKey
		};
	}
internal static void LogPromptRoutingDiagnostics(PromptBuildRequest request, PromptRoutingResult routing, PromptRoutingInput routingInput)
	{
		string who = "targetHero=" + (request.TargetHeroId ?? "null") + " targetCharacter=" + (request.TargetCharacterId ?? "null");
		if (routing.AuxiliaryFailure != null)
		{
			Logger.Log("Logic", "[RuleInjectionDebug] stage=single_aux_preprocess failed=" + routing.AuxiliaryFailure);
		}
		else if (request.AllowRulePreprocess && routingInput.UseAuxiliaryRuleApi)
		{
			Logger.Log("Logic", "[RuleInjectionDebug] stage=single_aux_preprocess " + who + " hits=" + PromptTopicRoutingStage.DescribeHits(routing.AuxiliaryRuleHitIds, "(none)"));
		}
		if (routing.ForcedRuleHitIds.Count > 0)
		{
			Logger.Log("Logic", "[RuleInjectionDebug] stage=forced_preprocess " + who + " hits=" + string.Join(",", routing.ForcedRuleHitIds));
		}
		if (routing.StickySuppressed)
		{
			Logger.Log("Logic", "[RuleInjectionDebug] stage=sticky_suppressed " + who + " carryDuel=" + routing.CarryDuel + " carryReward=" + routing.CarryReward + " carryLoan=" + routing.CarryLoan + " auxiliaryHits=" + PromptTopicRoutingStage.DescribeHits(routing.AuxiliaryRuleHitIds, "(none)"));
		}
	}
}
