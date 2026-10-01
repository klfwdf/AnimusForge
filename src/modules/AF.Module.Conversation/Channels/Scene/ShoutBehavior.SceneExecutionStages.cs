using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;
using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
	internal static LegacyInteractionPipelinePorts CreateSceneShoutMainReplyPorts(PromptPackage preparedMainPrompt)
	{
		if (preparedMainPrompt == null)
		{
			throw new ArgumentNullException(nameof(preparedMainPrompt));
		}
		CapabilitySet capabilities = new CapabilitySet(new[] { "llm.generate", "prompt.compose" });
		return new LegacyInteractionPipelinePorts(
			snapshot => new RuleSelection(new[] { "scene_shout" }, Array.Empty<string>()),
			(envelope, selection, availableCapabilities) => preparedMainPrompt,
			(snapshot, selection, availableCapabilities) => new PostprocessContext(Array.Empty<string>(), Array.Empty<string>(), availableCapabilities),
			(rawText, context) => new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
			(rawText, internalTagFamilies) => LlmVisibleReplyNormalizer.NormalizeComplete(rawText),
			capabilities);
	}

	internal async Task<DetachedInteractionHostResult> GenerateSceneShoutMainReplyAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		InteractionEnvelope envelope,
		int conversationEpoch,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		bool IsCurrent()
		{
			return !cancellationToken.IsCancellationRequested
				&& envelope?.Snapshot?.Identity?.Channel == InteractionChannel.SceneShout
				&& !SaveRuntimeGuard.IsStale(envelope.Snapshot.Trace.RuntimeGeneration, "scene_main_generation")
				&& IsSceneConversationEpochCurrent(conversationEpoch)
				&& envelope.Snapshot.DetachedFacts.TryGetValue("scene_session_id", out string sessionToken)
				&& int.TryParse(sessionToken, out int sceneSessionId)
				&& sceneSessionId == _ports.SceneSessionId();
		}
		DetachedInteractionHostResult Stale()
		{
			return new DetachedInteractionHostResult(string.Empty, false, InteractionStatus.CancelledAsStale, "scene_main_stale", null, null);
		}
		if (!IsCurrent())
		{
			return Stale();
		}
		InteractionResult result;
		try
		{
			// Generation has no action or memory owner. The authoritative scene tail
			// records the cleaned reply for all listeners and performs postprocess once.
			result = await facade.GenerateAsync(envelope, configuration, moduleId, providerId, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return Stale();
		}
		catch (Exception ex)
		{
			return IsCurrent()
				? new DetachedInteractionHostResult(string.Empty, false, InteractionStatus.NonRetryableFailure, "scene_main_" + ex.GetType().Name, null, null)
				: Stale();
		}
		if (!IsCurrent() || result?.Status == InteractionStatus.CancelledAsStale)
		{
			return Stale();
		}
		if (result?.Status == InteractionStatus.Executed || result?.ActionPlan?.Actions.Count > 0)
		{
			return new DetachedInteractionHostResult(string.Empty, false, InteractionStatus.NonRetryableFailure,
				"scene_main_unexpected_action_result", result, null);
		}
		if (result?.Status == InteractionStatus.RetryableFailure
			|| result?.Status == InteractionStatus.DegradedWithoutProvider
			|| result?.Status == InteractionStatus.SkippedByEligibility)
		{
			DetachedInteractionHostResult fallback = await RunDetachedRefactorFallbackAsync(result.ErrorCode, fallbackToLegacy).ConfigureAwait(false);
			return IsCurrent() ? fallback : Stale();
		}
		return new DetachedInteractionHostResult(result?.VisibleReply ?? string.Empty, false,
			result?.Status ?? InteractionStatus.NonRetryableFailure,
			result?.ErrorCode ?? "missing_scene_main_result", result, null);
	}

	internal Task<bool> RecordSceneReplyHistoryOnMainThreadAsync(
		NpcDataPacket speaker, List<NpcDataPacket> audience,
		Hero expectedHero, CharacterObject expectedCharacter, string historyText,
		long generation, int sceneSessionId, int conversationEpoch, bool requireMemoryReceipt = false)
	{
		if (speaker == null || string.IsNullOrWhiteSpace(historyText))
		{
			return Task.FromResult(false);
		}
		return _dispatcher.RunAsync("scene_reply_history", speaker.Name, speaker.AgentIndex, () =>
		{
			if (SaveRuntimeGuard.IsStale(generation, "scene_reply_history")
				|| sceneSessionId != _ports.SceneSessionId()
				|| !IsSceneConversationEpochCurrent(conversationEpoch)
				|| !IsNativeConversationResponseTargetAvailableForActionDispatch(speaker.AgentIndex, expectedHero, expectedCharacter, out _))
			{
				return false;
			}
			bool factAccepted = _ports.RecordResponseForAllNearbySafe(audience, speaker.AgentIndex, speaker.Name, historyText, requireMemoryReceipt);
			bool replyAccepted = _ports.PersistNpcSpeechToNamedHeroes(speaker.AgentIndex, speaker.Name, historyText, audience, requireMemoryReceipt);
			return factAccepted && replyAccepted;
		}, false);
	}

	internal Task<bool> QueueSceneMainReplyOnMainThreadAsync(
		NpcDataPacket speaker, List<NpcDataPacket> audience,
		Hero expectedHero, CharacterObject expectedCharacter, string replyText,
		long generation, int sceneSessionId, int conversationEpoch,
		List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets,
		bool hasPostprocess, float interactionTimeoutSeconds, int participantCount,
		string playerText, bool replyIsDirectPlayerResponse,
		TaskCompletionSource<bool> speechCompletion = null)
	{
		if (speaker == null || string.IsNullOrWhiteSpace(replyText)) return Task.FromResult(false);
		bool CanStillPublish()
		{
			if (!SaveRuntimeGuard.IsCurrentGeneration(generation)
				|| sceneSessionId != _ports.SceneSessionId()
				|| !IsSceneConversationEpochCurrent(conversationEpoch)) return false;
			return !IsBannerlordMainThreadForNativeActions()
				|| IsNativeConversationResponseTargetAvailableForActionDispatch(speaker.AgentIndex, expectedHero, expectedCharacter, out _);
		}
		return _dispatcher.RunAsync("scene_main_speech_enqueue", speaker.Name, speaker.AgentIndex, () =>
		{
			if (!CanStillPublish()) return false;
			if (hasPostprocess)
			{
				// Lower bound for this reply's local natural-action resolution.
				RecordSceneActionReplyCapture(speaker.AgentIndex);
			}
			_ports.EnqueueSpeechLineWithOptions(speaker, replyText, audience,
				commitHistory: false, suppressStare: false, allowPlayerDirectedActions: true,
				conversationEpoch, sceneSummonTargets, sceneGuideTargets,
				hasPostprocess ? "正在处理NPC行为............" : null, speechCompletion,
				interactionTimeoutSeconds, Math.Max(1, participantCount),
				canStillPublish: CanStillPublish,
				playerDirectedActionText: replyIsDirectPlayerResponse ? playerText : string.Empty,
				playerDirectedNpcReplyText: replyIsDirectPlayerResponse ? replyText : string.Empty);
			return true;
		}, false);
	}

	internal int BeginNewPlayerDrivenSceneConversationEpoch()
	{
		_sceneRequestLifetime.Retire();
		_sceneRequestLifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();
		int num = Interlocked.Increment(ref _sceneConversationEpoch);
        ResetImmediateReactions(clearCooldown: false);
		RetireModuleSceneGroup("scene.stale_context");
		_ports.DeactivateMultiSceneMovementSuppression();
		_ports.ClearPendingSceneConversationAttentionRelease();
		_ports.ResetStaringBehavior();
		_ports.ClearQueuedSceneSpeech();
		try
		{
			_ports.StopAllLipSyncPlaybackAndCleanup();
		}
		catch
		{
		}
		return num;
	}

	internal bool IsSceneConversationEpochCurrent(int epoch)
	{
		return epoch <= 0 || epoch == Volatile.Read(ref _sceneConversationEpoch);
	}

	internal bool TryPrepareCompactTownOrdinaryReactionRequest(
		Mission sourceMission,
		long requestId,
		NpcDataPacket targetNpc,
		List<NpcDataPacket> allNpcData,
		bool suppressStare,
		string factText,
		Func<bool> canStillPublish,
		Hero contextHero,
		CharacterObject npcCharacter,
		int minTokens,
		int maxTokens,
		out ImmediateSceneReactionRequest request)
	{
		request = null;
		TownPromptTextCatalog text = GcczTownPromptResourceProvider.GetCatalog();
		string identity = (AfGcczShoutBridge.BuildCompactOrdinaryReactionIdentityOverride(
			contextHero,
			npcCharacter,
			targetNpc.AgentIndex) ?? string.Empty).Trim();
		string voice = (AfGcczShoutBridge.BuildOrdinarySpeakerVoiceContext(contextHero, targetNpc) ?? string.Empty).Trim();
		string compactFact = (AfGcczShoutBridge.BuildCompactAmbientReactionFact(
			targetNpc.AgentIndex,
			factText) ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(compactFact))
		{
			compactFact = (factText ?? string.Empty).Trim();
		}
		string systemPrompt = (text.CompactAmbientSystemPrompt ?? string.Empty).Trim();
		string userPrompt = text.CompactAmbientUserTemplate ?? string.Empty;
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "identity", identity);
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "voice", voice);
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "fact", compactFact);
		userPrompt = ApplyCompactPromptTemplate(
			userPrompt,
			"length_instruction",
			BuildSimpleDialogueReplyLengthInstruction(minTokens, maxTokens));

		List<object> messages = new List<object>
		{
			CreateChatMessage("system", systemPrompt),
			CreateChatMessage("user", userPrompt.Trim()),
		};
		request = new ImmediateSceneReactionRequest
		{
			RequestId = requestId,
			RuntimeGeneration = SaveRuntimeGuard.CurrentGeneration,
			SceneHistorySessionId = _ports.SceneSessionId(),
			SourceMission = sourceMission,
			TargetAgentIndex = targetNpc.AgentIndex,
			TargetNpc = targetNpc,
			AllNpcData = allNpcData,
			SuppressStare = suppressStare,
			FactText = factText ?? string.Empty,
			RunSiegeReactionPostprocess = true,
			CanStillPublish = canStillPublish,
			Messages = messages,
			MaxTokens = maxTokens,
			Temperature = TownOrdinarySpeakerVoiceSession.RecommendedReplyTemperature,
			UsesCompactTownOrdinaryChain = true
		};
		Logger.Log(
			"ShoutBehavior",
			"[ImmediateSceneReaction] compact_town_ordinary request=" + requestId
			+ " targetAgentIndex=" + targetNpc.AgentIndex
			+ " estimatedInputTokens=" + Logger.EstimateTokensFromMessages(messages)
			+ " maxOutputTokens=" + maxTokens);
		return true;
	}

private static SceneActionPostprocessWorkItem PrepareCompactTownOrdinaryAmbientPostprocess(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		string eventContext,
		string replyText)
	{
		string dialogue = StripActionTagsForSceneSpeech(replyText ?? string.Empty);
		if (!AfGcczShoutBridge.ShouldUseCompactOrdinaryReaction(targetHero, targetCharacter, targetAgentIndex)
			|| !AIConfigHandler.CanUseAuxiliaryActionPostprocess())
		{
			return new SceneActionPostprocessWorkItem(EnsureScenePostprocessFallbackMood(dialogue));
		}

		List<PostprocessRuleEntry> rules = TeamModuleServices.Siege.BuildPostprocessRules(
			true,
			targetAgentIndex,
			false,
			eventContext) ?? new List<PostprocessRuleEntry>();
		TownPromptTextCatalog text = GcczTownPromptResourceProvider.GetCatalog();
		string candidateRules = BuildCompactTownAmbientRuleText(rules, text);
		string moodRules = BuildPostprocessRuleTextForScene(AIConfigHandler.ActionPostprocessMoodRules);
		string runtimeContext = AfGcczShoutBridge.BuildCompactAmbientPostprocessContext(
			targetAgentIndex,
			eventContext);
		string systemPrompt = (text.CompactAmbientPostprocessSystemPrompt ?? string.Empty).Trim();
		string userPrompt = text.CompactAmbientPostprocessUserTemplate ?? string.Empty;
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "runtime_context", runtimeContext);
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "candidate_rules", candidateRules);
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "mood_rules", moodRules);
		userPrompt = ApplyCompactPromptTemplate(userPrompt, "reply", dialogue);
		userPrompt = BuildSceneCompositeUserBlock(
			string.Empty,
			userPrompt,
			text.CompactAmbientPostprocessContract);

		int estimatedInputTokens = Logger.EstimateTokens(systemPrompt)
			+ Logger.EstimateTokens(userPrompt)
			+ 10;
		Logger.Log(
			"ShoutBehavior",
			"[CompactTownAmbientPostprocess] targetAgentIndex=" + targetAgentIndex
			+ " ruleCount=" + rules.Count
			+ " estimatedInputTokens=" + estimatedInputTokens);
		return new SceneActionPostprocessWorkItem(systemPrompt, userPrompt, dialogue, rawTags =>
		{
		string normalized = TeamModuleServices.Siege.NormalizePostprocessTags(true, rawTags, rules);
		normalized = AfGcczShoutBridge.ValidateTownPostprocessDecision(normalized);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			normalized = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		Logger.Log(
			"ShoutBehavior",
			"[CompactTownAmbientPostprocess] completed targetAgentIndex=" + targetAgentIndex
			+ " normalized=" + (normalized ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "|"));
		return (dialogue + "\n" + normalized).Trim();
		});
	}
    private static string CompleteCompactTownOrdinaryAmbientPostprocess(SceneActionPostprocessWorkItem work, bool succeeded, string content, string error)
    {
        if (!work.TryBeginCompletion()) throw new InvalidOperationException("Compact scene postprocess cannot be retried.");
        if (!work.RequiresNetwork) return work.ImmediateResult;
        if (!succeeded)
        {
            Logger.Log("ShoutBehavior", "[CompactTownAmbientPostprocess] request failed: " + (error ?? string.Empty));
            return EnsureScenePostprocessFallbackMood(work.ReplyText);
        }
        return work.Normalize(content);
    }
}
