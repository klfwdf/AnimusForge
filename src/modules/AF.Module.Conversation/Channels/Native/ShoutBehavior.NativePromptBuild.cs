using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

using static AnimusForge.ShoutBehavior;
using static AnimusForge.NativePromptWorkScheduler;
namespace AnimusForge;

internal sealed partial class NativeConversationTurnRuntime : INativeConversationTurnHost
{
	/// <summary>
	/// Native runs the shared prompt build as five scheduled steps.
	/// Step 1 (game thread, admission re-validated): capture the detached request.
	/// Step 2 (background, below-normal priority, timeout-guarded): topic routing + mention retrieval.
	/// Steps 3/4 prepare and retrieve Knowledge candidates; step 5 captures sections on the game thread.
	/// Returns null when the request was aborted (admission lost, retired, timed out) so the caller
	/// keeps its legacy "preprocess aborted" path; PreprocessFormatException propagates.
	/// </summary>
	private async Task<MyBehavior.ShoutPromptContext> BuildNativePromptContextScheduledAsync(
		NativeConversationAdmission admission, string targetLog, int targetAgentIndex, long runtimeGeneration,
		Hero targetHero, CharacterObject targetCharacter, string routingInput, string extraFact, string cultureId, bool hasAnyHero,
		List<string> preprocessExcludedRuleIds, MyBehavior.WeeklyPromptSnapshot weeklyPromptSnapshot)
	{
		bool ownerAvailable = await _ports.PromptDispatcher.RunAsync("prompt_build_owner", targetLog, targetAgentIndex, _ports.IsPromptOwnerAvailable, false).ConfigureAwait(false);
		if (!ownerAvailable)
		{
			return CreateEmptyNativeConversationPromptContext();
		}
		string target = string.IsNullOrWhiteSpace(targetLog) ? "unknown" : targetLog.Trim();
		Stopwatch sw = Stopwatch.StartNew();

		// Step 1: game thread.
		SharedPromptRoutingWork routingWork = await _ports.PromptDispatcher.RunAsync("prompt_build_begin", target, targetAgentIndex,
			() => _ports.IsNativeConversationAdmissionCurrent(admission, out _)
                ? _ports.CapturePromptRoutingWork(new NativePromptCaptureRequest(targetHero, targetCharacter, routingInput, extraFact, cultureId, hasAnyHero, targetAgentIndex, preprocessExcludedRuleIds))
                : null,
            (SharedPromptRoutingWork)null).ConfigureAwait(false);
		if (routingWork == null)
		{
			FreezeWatchdog.Mark("NativeConversation.prompt_build_begin_aborted", "target=" + target + " agent=" + targetAgentIndex, immediate: true);
			return null;
		}
		if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_prompt_build_begin"))
		{
			return null;
		}

		PromptBuildPhases phases = routingWork.Phases;

		// Step 2: background. The ambient retrieval target is applied on the worker thread for the
		// duration of routing; the existing slot/timeout guard keeps a single in-flight preprocess.
		// The existing background slot/timeout/late-completion guard is typed to ShoutPromptContext;
		// the routing step returns a marker instance so that machinery is reused unchanged.
		MyBehavior.ShoutPromptContext routedMarker = new MyBehavior.ShoutPromptContext();
		Task<MyBehavior.ShoutPromptContext> routingTask = RunNativeConversationBackgroundPreprocessAsync(target, targetAgentIndex, runtimeGeneration, () =>
		{
			using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
			AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
			try
			{
				SharedPromptRoutingRuntime.Run(routingWork);
				return routedMarker;
			}
			finally
			{
				AIConfigHandler.ClearGuardrailRuntimeTarget();
			}
		});
		MyBehavior.ShoutPromptContext routed = await AwaitNativeConversationBackgroundPreprocessAsync(routingTask, target, targetAgentIndex, runtimeGeneration).ConfigureAwait(false);
		if (!ReferenceEquals(routed, routedMarker))
		{
			// null = aborted/timed out; empty context = background failure. Both keep the legacy abort path.
			return null;
		}
		if (SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_prompt_build_routing"))
		{
			return null;
		}

		// Step 3: game thread. Prepare Lore and capture entity candidates after routing discovered mentions.
		PromptKnowledgeWorkInput knowledgeInput = await _ports.PromptDispatcher.RunAsync("prompt_build_knowledge_capture", target, targetAgentIndex,
			() =>
			{
				if (!_ports.IsNativeConversationAdmissionCurrent(admission, out _)) return null;
				return _ports.CapturePromptKnowledge(phases, targetHero ?? targetCharacter?.HeroObject);
			},
			(PromptKnowledgeWorkInput)null).ConfigureAwait(false);
		if (knowledgeInput == null || SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_knowledge_capture"))
		{
			return null;
		}

		// Step 4: background Lore/entity/rule retrieval, with the same slot/timeout guard as routing.
		MyBehavior.ShoutPromptContext knowledgeMarker = new MyBehavior.ShoutPromptContext();
		PromptKnowledgeWorkResult knowledgeResult = null;
		Task<MyBehavior.ShoutPromptContext> knowledgeTask = RunNativeConversationBackgroundPreprocessAsync(target, targetAgentIndex, runtimeGeneration, () =>
		{
			knowledgeResult = MyBehavior.RunSharedKnowledgeRetrieval(knowledgeInput);
			return knowledgeMarker;
		});
		if (!ReferenceEquals(await AwaitNativeConversationBackgroundPreprocessAsync(knowledgeTask, target, targetAgentIndex, runtimeGeneration).ConfigureAwait(false), knowledgeMarker)
			|| SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_knowledge_retrieval"))
		{
			return null;
		}

		// Step 5: game thread, ownership re-validated after the hop.
		MyBehavior.ShoutPromptContext ctx = await _ports.PromptDispatcher.RunAsync("prompt_build_complete", target, targetAgentIndex,
			() =>
			{
				if (!_ports.IsNativeConversationAdmissionCurrent(admission, out _))
				{
					return null;
				}
				using IDisposable scope = AIConfigHandler.BeginGuardrailRuntimeScope();
				AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility);
				try
				{
					MyBehavior.ApplySharedKnowledgeRetrieval(phases, knowledgeResult);
					return _ports.CompletePromptCapture(phases, targetHero, targetCharacter, weeklyPromptSnapshot);
				}
				finally
				{
					AIConfigHandler.ClearGuardrailRuntimeTarget();
				}
			},
			(MyBehavior.ShoutPromptContext)null).ConfigureAwait(false);
		sw.Stop();
		if (ctx == null)
		{
			FreezeWatchdog.Mark("NativeConversation.prompt_build_complete_aborted", "target=" + target + " agent=" + targetAgentIndex + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2), immediate: true);
			return null;
		}
		Logger.Log("Logic", "[NativePerf] prompt_build_scheduled_done target=" + target + " agent=" + targetAgentIndex + " ms=" + Math.Round(sw.Elapsed.TotalMilliseconds, 2) + " thread=" + Thread.CurrentThread.ManagedThreadId);
		return ctx;
	}
}

internal sealed class NativePromptCaptureRequest
{
    internal readonly Hero Hero;
    internal readonly CharacterObject Character;
    internal readonly string Input, ExtraFact, CultureId;
    internal readonly bool HasAnyHero;
    internal readonly int AgentIndex;
    internal readonly List<string> ExcludedRules;
    internal NativePromptCaptureRequest(Hero hero, CharacterObject character, string input, string extraFact, string cultureId, bool hasAnyHero, int agentIndex, List<string> excludedRules)
    { Hero=hero;Character=character;Input=input;ExtraFact=extraFact;CultureId=cultureId;HasAnyHero=hasAnyHero;AgentIndex=agentIndex;ExcludedRules=excludedRules; }
}
