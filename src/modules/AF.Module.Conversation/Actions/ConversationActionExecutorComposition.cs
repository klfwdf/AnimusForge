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
internal sealed class ConversationActionExecutorComposition
{
    private readonly Func<bool> _isCurrentOwner;
    private readonly SceneMovementController _sceneMovement;
    private readonly NativeConversationGameEffectsRuntime _nativeGameEffects;
    private readonly ConversationActionBoundaryBannerlordAdapter _boundary;
    private readonly ConversationGameThreadDispatcher _dispatcher;
    internal ConversationActionExecutorComposition(Func<bool> isCurrentOwner,SceneMovementController movement,NativeConversationGameEffectsRuntime effects,ConversationActionBoundaryBannerlordAdapter boundary,ConversationGameThreadDispatcher dispatcher)
    { _isCurrentOwner=isCurrentOwner;_sceneMovement=movement;_nativeGameEffects=effects;_boundary=boundary;_dispatcher=dispatcher; }
internal LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal()
	{
		ConversationActionExecutorComposition instance = _isCurrentOwner() ? this : null;
		if (instance == null)
		{
			return null;
		}
		if (!TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string npcName))
		{
			return null;
		}

		int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		string targetUnavailableReason = "";
		if (!IsNativeConversationResponseTargetAvailableForActionDispatch(
			targetAgentIndex,
			targetHero,
			targetCharacter,
			out targetUnavailableReason))
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] detached action executor unavailable target="
				+ (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? "unknown")
				+ " agentIndex=" + targetAgentIndex
				+ " reason=" + (targetUnavailableReason ?? "validation_failed"));
			return null;
		}

		NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
		targetNpc.AgentIndex = targetAgentIndex;
		List<NpcDataPacket> allNpcData = new List<NpcDataPacket> { targetNpc };
		Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
		if (targetAgentIndex >= 0 && targetHero != null)
		{
			resolvedHeroes[targetAgentIndex] = targetHero;
		}
		List<SceneSummonPromptTarget> sceneSummonTargets = targetAgentIndex >= 0
			? instance._sceneMovement.BuildSceneSummonPromptTargets(allNpcData, resolvedHeroes)
			: null;
		int firstGuidePromptId = (sceneSummonTargets != null && sceneSummonTargets.Count > 0
			? sceneSummonTargets.Max(item => item?.PromptId ?? 0)
			: 0) + 1;
		Agent targetAgent = targetAgentIndex >= 0
			? Mission.Current?.Agents?.FirstOrDefault(agent => agent != null && agent.Index == targetAgentIndex)
			: null;
		List<SceneGuidePromptTarget> sceneGuideTargets = targetAgentIndex >= 0
			? instance._sceneMovement.BuildSceneGuidePromptTargets(targetAgent, firstGuidePromptId)
			: null;
		ConversationManager expectedConversationManager = Campaign.Current?.ConversationManager;
		int expectedConversationToken = expectedConversationManager?.ActiveToken ?? int.MinValue;
		string expectedSubjectId = ConversationActionContextBannerlordAdapter.ResolveDetachedInteractionSubjectId(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			targetNpc);
		try
		{
			TryGetNativeConversationPersistentHistoryTargetForExternal(
				out Hero persistentHero,
				out _,
				out string persistentMemoryId);
			expectedSubjectId = !string.IsNullOrWhiteSpace(persistentMemoryId)
				? persistentMemoryId.Trim()
				: persistentHero?.StringId ?? expectedSubjectId;
		}
		catch
		{
		}
		Func<GameInteractionSnapshot, bool> isCurrentNativeContext = snapshot =>
		{
			try
			{
				if (!IsBannerlordMainThreadForNativeActions()
					|| snapshot?.Identity == null
					|| snapshot.Identity.Channel != InteractionChannel.NativeConversation
					|| !string.Equals(snapshot.Identity.SubjectId, expectedSubjectId, StringComparison.Ordinal)
					|| !ReferenceEquals(Campaign.Current?.ConversationManager, expectedConversationManager)
					|| expectedConversationManager == null
					|| expectedConversationManager.ActiveToken != expectedConversationToken
					|| !snapshot.DetachedFacts.TryGetValue("native_conversation_token", out string capturedToken)
					|| !int.TryParse(capturedToken, out int parsedToken)
					|| parsedToken != expectedConversationToken)
				{
					return false;
				}
				return IsNativeConversationResponseTargetAvailableForActionDispatch(
					targetAgentIndex,
					targetHero,
					targetCharacter,
					out _);
			}
			catch
			{
				return false;
			}
		};
		IEconomyRewardDebtMainThreadPort economyPort = ConversationActionContextBannerlordAdapter.CreateEconomyReplayPortForExternal(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			npcName,
			expectedSubjectId);

		return LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
			(actionPlan, snapshot, duelDispatchContext) =>
		{
			if (!isCurrentNativeContext(snapshot))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] detached action rejected because session or target is stale");
				return InteractionStatus.RejectedByValidation;
			}
			string content = actionPlan?.RawPostprocessId ?? "";
            using var diplomacySource = DiplomacyDialogueSourceScope.Begin(content, "native", snapshot.Identity.SessionId, snapshot.PlayerText, content);
			NativeConversationGameActionResult actionResult = instance._nativeGameEffects.ApplyNativeConversationGameActionsLegacyCore(
				targetHero,
				targetCharacter,
				targetNpc,
				allNpcData,
				sceneSummonTargets,
				sceneGuideTargets,
				content,
				snapshot?.PlayerText ?? "",
				expectedConversationManager,
				expectedConversationToken,
				duelDispatchContext);
			return actionResult != null && !actionResult.ResponseDiscarded
				? InteractionStatus.Executed
				: InteractionStatus.RejectedByValidation;
		},
			DuelBehavior.CreateDetachedDuelDispatchOwnerForExternal(),
			allowedTagFamilies: LegacyActionTagCatalog.DefaultAllowedTagFamilies,
			economyPlanner: economyPort == null ? null : new LegacyEconomyRewardDebtAdapter(),
			economyPort: economyPort,
			economyCapabilities: economyPort == null ? null : LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
			economyExecutionGate: (actionPlan, snapshot, isEconomyOnly) =>
				isCurrentNativeContext(snapshot)
					? InteractionStatus.Executed
					: InteractionStatus.RejectedByValidation);
	}
internal LegacyNativeActionPlanExecutor CreateSceneShoutActionPlanExecutorForExternal(
		int targetAgentIndex,
		int maxActions = 64)
	{
		ConversationActionExecutorComposition instance = _isCurrentOwner() ? this : null;
		if (instance == null || targetAgentIndex < 0)
		{
			return null;
		}
		IReadOnlyList<string> allowedTagFamilies = LegacyActionTagCatalog.DefaultAllowedTagFamilies;
		Agent capturedAgent = Mission.Current?.Agents?.FirstOrDefault(candidate => candidate != null && candidate.Index == targetAgentIndex);
		CharacterObject capturedCharacter = capturedAgent?.Character as CharacterObject;
		Hero capturedHero = capturedCharacter?.HeroObject;
		NpcDataPacket capturedNpc = capturedAgent == null ? null : ShoutUtils.ExtractNpcData(capturedAgent);
		string expectedSceneSubjectId = ConversationActionContextBannerlordAdapter.ResolveDetachedInteractionSubjectId(
			capturedHero,
			capturedCharacter,
			targetAgentIndex,
			capturedNpc);
		Func<GameInteractionSnapshot, bool> isCurrentSceneContext = snapshot =>
		{
			try
			{
				if (!IsBannerlordMainThreadForNativeActions()
					|| snapshot?.Identity == null
					|| snapshot.Identity.Channel != InteractionChannel.SceneShout
					|| !string.Equals(snapshot.Identity.SubjectId, expectedSceneSubjectId, StringComparison.Ordinal)
					|| !snapshot.DetachedFacts.TryGetValue("scene_session_id", out string sceneSessionToken)
					|| !int.TryParse(sceneSessionToken, out int capturedSceneSessionId)
					|| capturedSceneSessionId != GetCurrentSceneHistorySessionIdForExternal())
				{
					return false;
				}
				InteractionCandidate candidate = snapshot.Candidates?.FirstOrDefault(
					item => item != null && item.AgentIndex == targetAgentIndex);
				Agent liveAgent = Mission.Current?.Agents?.FirstOrDefault(
					item => item != null && item.Index == targetAgentIndex);
				if (candidate == null
					|| !candidate.IsAlive
					|| liveAgent == null
					|| !liveAgent.IsActive()
					|| !CanAgentParticipateInSceneSpeech(liveAgent))
				{
					return false;
				}
				CharacterObject liveCharacter = liveAgent.Character as CharacterObject;
				string liveStableId = liveCharacter?.HeroObject?.StringId
					?? liveCharacter?.StringId
					?? "agent:" + targetAgentIndex;
				return string.Equals(candidate.StableId, liveStableId, StringComparison.Ordinal);
			}
			catch
			{
				return false;
			}
		};
		IEconomyRewardDebtMainThreadPort economyPort = ConversationActionContextBannerlordAdapter.CreateEconomyReplayPortForExternal(
			capturedHero,
			capturedCharacter,
			targetAgentIndex,
			capturedCharacter?.Name?.ToString(),
			expectedSceneSubjectId);
		return LegacyNativeActionPlanExecutor.CreateRequestBoundDuelExecutor(
			(actionPlan, snapshot, duelDispatchContext) =>
		{
			if (!isCurrentSceneContext(snapshot))
			{
				return InteractionStatus.RejectedByValidation;
			}
			if (duelDispatchContext != null
				&& (!snapshot.DetachedFacts.TryGetValue("scene_session_id", out string sceneSessionToken)
					|| !int.TryParse(sceneSessionToken, out int capturedSceneSessionId)
					|| capturedSceneSessionId != GetCurrentSceneHistorySessionIdForExternal()))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] exact Duel rejected because scene session is stale");
				return InteractionStatus.RejectedByValidation;
			}
			InteractionCandidate capturedCandidate = snapshot.Candidates?.FirstOrDefault(
				candidate => candidate != null && candidate.AgentIndex == targetAgentIndex);
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(
				candidate => candidate != null && candidate.Index == targetAgentIndex);
			if (capturedCandidate == null
				|| !capturedCandidate.IsAlive
				|| agent == null
				|| !agent.IsActive()
				|| !CanAgentParticipateInSceneSpeech(agent))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] scene target unavailable agent=" + targetAgentIndex);
				return InteractionStatus.RejectedByValidation;
			}
			CharacterObject targetCharacter = agent.Character as CharacterObject;
			Hero targetHero = targetCharacter?.HeroObject;
			string currentStableId = targetHero?.StringId ?? targetCharacter?.StringId ?? "agent:" + targetAgentIndex;
			if (!string.Equals(capturedCandidate.StableId, currentStableId, StringComparison.Ordinal))
			{
				Logger.Log("ShoutBehavior", "[RefactorAction] scene target identity changed agent=" + targetAgentIndex);
				return InteractionStatus.RejectedByValidation;
			}
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc == null)
			{
				return InteractionStatus.RejectedByValidation;
			}
			string content = actionPlan.RawPostprocessId ?? string.Empty;
            using var diplomacySource = DiplomacyDialogueSourceScope.Begin(content, "scene", snapshot.Identity.SessionId, snapshot.PlayerText, content);
			bool consumed = instance._boundary.TryApplyDeferredSceneMoodTag(npc, content);
			content = StripDeferredSceneMoodTags(content);
			if (instance._boundary.TryApplyDeferredScenePostprocessActionTagsDirectly(
				targetHero,
				targetCharacter,
				targetAgentIndex,
				ref content,
				snapshot.PlayerText ?? string.Empty,
				string.Empty,
				"scene-refactor",
				replyIsDirectPlayerResponse: true,
				duelDispatchContext: duelDispatchContext))
			{
				consumed = true;
				content = ExtractDeferredSceneActionTags(content);
			}
			if (instance._sceneMovement.TryExecuteDeferredSceneFollowTagsDirectly(npc, content))
			{
				consumed = true;
			}
			return consumed ? InteractionStatus.Executed : InteractionStatus.RejectedByValidation;
		},
			DuelBehavior.CreateDetachedDuelDispatchOwnerForExternal(),
			maxActions,
		 allowedTagFamilies,
		 economyPort == null ? null : new LegacyEconomyRewardDebtAdapter(),
		 economyPort,
			 economyPort == null ? null : LegacyEconomyRewardDebtAdapter.CreateAllCapabilities(),
			economyExecutionGate: (actionPlan, snapshot, isEconomyOnly) =>
				isCurrentSceneContext(snapshot)
					? InteractionStatus.Executed
					: InteractionStatus.RejectedByValidation);
	}
internal async Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInCoreAsync(
		LegacyNativeConversationFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		Func<Task<string>> fallbackToLegacyNative,
		CancellationToken cancellationToken)
	{
		if (facade == null)
		{
			return await CompleteNativeConversationOptInFallbackAsync("missing_facade", fallbackToLegacyNative).ConfigureAwait(false);
		}

		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		DetachedInteractionHostResult hostResult = await host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => CreateNativeConversationActionPlanExecutorForExternal(),
			envelope => CreateNativeConversationMemoryFacadeForExternal(),
			(envelope, commit) => DispatchNativeConversationOptInCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				envelope?.Snapshot?.Candidates?.FirstOrDefault()?.AgentIndex ?? (-1)),
			fallbackToLegacyNative,
			cancellationToken).ConfigureAwait(false);
		return new LegacyNativeConversationOptInResult(
			hostResult?.VisibleReply ?? "",
			hostResult?.UsedLegacyFallback ?? false,
			hostResult?.Status ?? InteractionStatus.NonRetryableFailure,
			hostResult?.ErrorCode ?? "missing_host_result",
			hostResult?.DetachedResult,
			hostResult?.Commit);
	}
internal static IInteractionMemory CreateNativeConversationMemoryFacadeForExternal()
	{
		if (!TryResolveNativeConversationTarget(out Hero targetHero, out CharacterObject targetCharacter, out string targetName))
		{
			return null;
		}
		return targetHero != null
			? new MyBehaviorMemoryFacade(targetHero)
			: new MyBehaviorMemoryFacade(
				targetCharacter?.StringId ?? "native:unknown",
				string.IsNullOrWhiteSpace(targetName) ? "NPC" : targetName);
	}
internal static async Task<LegacyNativeConversationOptInResult> CompleteNativeConversationOptInFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacyNative)
	{
		if (fallbackToLegacyNative == null)
		{
			return new LegacyNativeConversationOptInResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode,
				null,
				null);
		}
		try
		{
			return new LegacyNativeConversationOptInResult(
				await fallbackToLegacyNative().ConfigureAwait(false),
				true,
				InteractionStatus.Succeeded,
				errorCode,
				null,
				null);
		}
		catch (Exception exception)
		{
			return new LegacyNativeConversationOptInResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode + ";legacy_" + exception.GetType().Name,
				null,
				null);
		}
	}
internal static LegacyInteractionPipelinePorts CreateSceneShoutDetachedPortsForExternal(
		IEnumerable<string> allowedTagFamilies,
		int maxActions = 64)
	{
		List<string> tagFamilies = (allowedTagFamilies ?? Enumerable.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		LegacyDetachedPromptComposer mainComposer = new LegacyDetachedPromptComposer(model: "legacy-scene-shout");
		LegacyDetachedPostprocessPromptComposer postprocessComposer = new LegacyDetachedPostprocessPromptComposer(model: "legacy-scene-shout-postprocess");
		LegacyActionTagParser actionParser = new LegacyActionTagParser(maxActions);
		CapabilitySet capabilities = new CapabilitySet(new[]
		{
			"llm.generate",
			"prompt.compose",
			"postprocess.compose",
			"action.parse"
		});
		return new LegacyInteractionPipelinePorts(
			snapshot => new RuleSelection(new[] { "scene_shout" }, Array.Empty<string>()),
			(envelope, selection, availableCapabilities) => mainComposer.Compose(envelope, selection, availableCapabilities),
			(snapshot, selection, availableCapabilities) => new PostprocessContext(selection?.RuleIds, tagFamilies, availableCapabilities),
			(rawText, context) => actionParser.Parse(rawText, context),
			(rawText, internalTagFamilies) => LlmVisibleReplyNormalizer.NormalizeComplete(rawText),
			capabilities,
			(envelope, selection, visibleReply, rawReply, context) => postprocessComposer.Compose(envelope, selection, visibleReply, rawReply, context));
	}
internal async Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInCoreAsync(
		LegacyChannelInteractionFacade facade,
		RuntimeConfigSnapshot configuration,
		string moduleId,
		string providerId,
		string playerText,
		int targetAgentIndex,
		Func<Task<string>> fallbackToLegacy,
		CancellationToken cancellationToken)
	{
		if (facade == null || targetAgentIndex < 0)
		{
			return await RunDetachedRefactorFallbackAsync("missing_scene_facade", fallbackToLegacy).ConfigureAwait(false);
		}
		DetachedInteractionHost host = new DetachedInteractionHost(
			facade.Capture,
			facade.GenerateAsync,
			facade.Commit);
		DetachedInteractionHostResult result = await host.ExecuteAsync(
			playerText,
			configuration,
			moduleId,
			providerId,
			envelope => CreateSceneShoutActionPlanExecutorForExternal(targetAgentIndex),
			CreateSceneShoutMemoryFacadeForExternal,
			(envelope, commit) => DispatchSceneShoutRefactorCommitAsync(
				commit,
				envelope?.Snapshot?.Identity?.SubjectId ?? "unknown",
				targetAgentIndex),
			fallbackToLegacy,
			cancellationToken).ConfigureAwait(false);
		return result;
	}
internal static IInteractionMemory CreateSceneShoutMemoryFacadeForExternal(InteractionEnvelope envelope)
	{
		GameInteractionSnapshot snapshot = envelope?.Snapshot;
		if (snapshot?.Identity == null)
		{
			return null;
		}
		string memoryKind = snapshot.DetachedFacts.TryGetValue("memory_kind", out string kind)
			? kind
			: string.Empty;
		if (string.Equals(memoryKind, "hero", StringComparison.OrdinalIgnoreCase))
		{
			Hero hero = Hero.Find(snapshot.Identity.SubjectId);
			return hero == null ? null : new MyBehaviorMemoryFacade(hero);
		}
		string memoryId = snapshot.DetachedFacts.TryGetValue("memory_id", out string detachedMemoryId)
			? detachedMemoryId
			: snapshot.Identity.SubjectId;
		if (string.IsNullOrWhiteSpace(memoryId)
			|| !snapshot.DetachedFacts.TryGetValue("memory_kind", out string resolvedKind)
			|| string.Equals(resolvedKind, "unresolved", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string name = snapshot.Candidates?.FirstOrDefault()?.DisplayName;
		return new MyBehaviorMemoryFacade(memoryId, name);
	}
internal static async Task<DetachedInteractionHostResult> RunDetachedRefactorFallbackAsync(
		string errorCode,
		Func<Task<string>> fallbackToLegacy)
	{
		if (fallbackToLegacy == null)
		{
			return new DetachedInteractionHostResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode,
				null,
				null);
		}
		try
		{
			return new DetachedInteractionHostResult(
				await fallbackToLegacy().ConfigureAwait(false),
				true,
				InteractionStatus.Succeeded,
				errorCode,
				null,
				null);
		}
		catch (Exception exception)
		{
			return new DetachedInteractionHostResult(
				string.Empty,
				true,
				InteractionStatus.NonRetryableFailure,
				errorCode + ";legacy_" + exception.GetType().Name,
				null,
				null);
		}
	}
internal Task<InteractionCommitResult> DispatchNativeConversationOptInCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
		return _dispatcher.RunAsync(
			"detached_opt_in_commit",
			targetLog,
			targetAgentIndex,
			commit,
			new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"main_thread_dispatch_failed"));
	}
internal Task<InteractionCommitResult> DispatchSceneShoutRefactorCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		int targetAgentIndex)
	{
		return _dispatcher.RunAsync(
			"detached_scene_commit",
			targetLog,
			targetAgentIndex,
			commit,
			new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"main_thread_dispatch_failed"));
	}
internal static string RunCourierActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected = false, bool diplomacyRuleInjected = false, bool worldMapPartyCommandRuleInjected = false, List<string> preprocessRuleHits = null, string entityPostprocessContext = null, int targetAgentIndex = -1, bool latestReplyHasPlayerInput = true, bool forceLooseWeeklyMemoryMaterialSession = false, bool kingdomVassalageRuleInjected = false, bool kingdomAnnexationRuleInjected = false, string chainName = null)
	{
		if (!TryPrepareCourierActionPostprocessForExternal(targetHero, targetCharacter, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, out CourierActionPostprocessWorkItem workItem, out string immediateResult, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, preprocessRuleHits, entityPostprocessContext, targetAgentIndex, latestReplyHasPlayerInput, forceLooseWeeklyMemoryMaterialSession, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, chainName))
		{
			return immediateResult;
		}
		if (!AIConfigHandler.TryCallAuxiliaryActionPostprocess(workItem.SystemPrompt, workItem.UserPrompt, 5000, 0f, out string content, out string error))
		{
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] 调用失败: " + error);
			return workItem.FallbackText;
		}
		return workItem.CompleteOnMainThread(content);
	}
}
