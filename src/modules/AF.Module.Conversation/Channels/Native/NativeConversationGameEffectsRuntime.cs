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

internal sealed partial class NativeConversationGameEffectsRuntime
{
    private readonly NativeConversationGameEffectPorts _ports;
    private readonly AnimusForge.Refactor.Runtime.PendingOperationRegistry _pendingMainThreadFunctions;
    internal NativeConversationGameEffectsRuntime(NativeConversationGameEffectPorts ports, AnimusForge.Refactor.Runtime.PendingOperationRegistry pendingOperations)
    {
        _ports = ports ?? throw new ArgumentNullException(nameof(ports));
        _pendingMainThreadFunctions = pendingOperations ?? throw new ArgumentNullException(nameof(pendingOperations));
    }

	internal WorldMapPartyCommandBehavior.WorldMapOrderApplyResult ApplyNativeConversationActionTags(
		Hero targetHero,
		CharacterObject targetCharacter,
		ref string content,
		int targetAgentIndexOverride = -1,
		string latestPlayerText = null,
		ConversationManager expectedConversationManager = null,
		int expectedConversationToken = int.MinValue,
		string actionChainName = null,
		string npcReplyTextOverride = null,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
		WorldMapPartyCommandBehavior.WorldMapOrderApplyResult worldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult();
		using var diplomacySource = DiplomacyDialogueSourceScope.Begin(content, "native",
			expectedConversationToken.ToString(), latestPlayerText, npcReplyTextOverride ?? content);
		if (string.IsNullOrWhiteSpace(content))
		{
			return worldMapResult;
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		int resolvedTargetAgentIndex = targetAgentIndexOverride >= 0 ? targetAgentIndexOverride : TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		if (_ports.TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, resolvedTargetAgentIndex, ref content))
		{
			return worldMapResult;
		}
		if (!IsNativeConversationResponseTargetAvailableForActionDispatch(resolvedTargetAgentIndex, targetHero, targetCharacter, out string unavailableReason))
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] dropped postprocess response because target is unavailable target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + resolvedTargetAgentIndex + " reason=" + unavailableReason);
			content = "";
			return worldMapResult;
		}
		PublicExecutionOrderRuntime.Consume(resolvedTargetAgentIndex, ref content);
		NoblePrisonerEscortBehavior.TryProcessSceneExecutionTag(
			resolvedTargetAgentIndex,
			!string.IsNullOrWhiteSpace(latestPlayerText),
			ref content);
		NoblePrisonerExecutionOrderBehavior.TryProcessAcceptedTag(
			targetHero,
			resolvedTargetAgentIndex,
			!string.IsNullOrWhiteSpace(latestPlayerText),
			ref content,
			out _);
		TroopInspectionBehavior.TryProcessPrisonerSlaughterActionTagForExternal(
			resolvedTargetAgentIndex,
			ref content);
		if (string.IsNullOrWhiteSpace(content))
		{
			return worldMapResult;
		}
		try
		{
			LogNativeActionStep("start", targetHero, targetCharacter, content);
			_ports.TryProcessSetsOwnedSettlementMassacreActionTags(resolvedTargetAgentIndex, ref content);
			int siegeAgentIndex = resolvedTargetAgentIndex;
			bool siegeActionHandled;
			LogNativeActionStep("siege_before", targetHero, targetCharacter, content);
			if (TeamModuleServices.Siege.TryProcessActionTags(
				targetHero,
				targetCharacter,
				siegeAgentIndex,
				ref content,
				out siegeActionHandled,
				replyIsDirectPlayerResponse: true,
				playerText: latestPlayerText) && siegeActionHandled)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] siege_intervention handled target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown"));
			}
			LogNativeActionStep("siege_after", targetHero, targetCharacter, content);

			if (targetHero != null)
			{
				LogNativeActionStep("hero_dispatch_before", targetHero, targetCharacter, content);
				TryProcessCustomPolicyAgendaActionTag(targetHero, actionChainName ?? ResolveNativeConversationPostprocessChainName(), latestPlayerText, ref content, npcReplyTextOverride);
				VoteDealBehavior.ProcessAgendaTagsDispatch(targetHero, ref content);
				DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(targetHero, ref content);
				worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(targetHero, ref content);
				DuelBehavior.TryCacheDuelAfterLinesFromText(targetHero, ref content);
				DuelBehavior.TryCacheDuelStakeFromText(targetHero, ref content);
				VanillaIssueOfferBridge.ApplyIssueOfferTags(targetHero, ref content);
				LogNativeActionStep("hero_dispatch_after", targetHero, targetCharacter, content);
				int targetAgentIndex = resolvedTargetAgentIndex;
				bool nativeSceneTauntHandled = TryProcessNativeConversationSceneTauntTags(targetHero, targetCharacter, targetAgentIndex, ref content, out var nativeSceneTauntEscalated);
				if (nativeSceneTauntHandled && string.IsNullOrWhiteSpace(content))
				{
					content = BuildFallbackSceneTauntSpeech(nativeSceneTauntEscalated);
				}
				_ports.TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, targetAgentIndex, ref content);
				LogNativeActionStep("scene_taunt_lords_hall_after", targetHero, targetCharacter, content);
				LogNativeActionStep("noble_gathering_before", targetHero, targetCharacter, content);
				if (TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal(targetHero, ref content, out var nobleFacts, out var nobleNotifications))
				{
					if (nobleFacts != null)
					{
						foreach (string generatedFact in nobleFacts)
						{
							if (!string.IsNullOrWhiteSpace(generatedFact))
							{
								MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, generatedFact);
							}
						}
						_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, nobleFacts);
					}
					foreach (string notification in nobleNotifications ?? new List<string>())
					{
						if (!string.IsNullOrWhiteSpace(notification))
						{
							InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
						}
					}
				}
				LogNativeActionStep("noble_gathering_after", targetHero, targetCharacter, content);
				bool rewardBeforeHasVassalage = ContainsVassalageActionTagForLog(content);
				bool rewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTagForLog(content);
				string rewardChainName = ResolveNativeConversationPostprocessChainName();
				Logger.Log("ShoutBehavior", "[NativeConversation] ApplyRewardTags start chain=" + rewardChainName + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " containsVASSALAGE=" + rewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex);
				LogNativeActionStep("party_transfer_before", targetHero, targetCharacter, content);
				if (MyBehavior.TryApplyPartyTransferTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out var generatedFacts, out var notifications))
				{
					if (generatedFacts != null)
					{
						foreach (string generatedFact in generatedFacts)
						{
							if (!string.IsNullOrWhiteSpace(generatedFact))
							{
								MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, generatedFact);
							}
						}
						_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, generatedFacts);
					}
					if (notifications != null)
					{
						foreach (string notification in notifications)
						{
							if (!string.IsNullOrWhiteSpace(notification))
							{
								InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
							}
						}
					}
				}
				LogNativeActionStep("party_transfer_after", targetHero, targetCharacter, content);
				RewardSystemBehavior rewardSystem = RewardSystemBehavior.Instance;
				LogNativeActionStep("reward_before", targetHero, targetCharacter, content);
				RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(content)
					? CreateRpItemIntroductionContextForReward(
						targetHero,
						null,
						targetCharacter,
						targetAgentIndex,
						targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString(),
						latestPlayerText,
						content,
						includeNativeConversationSessionHistory: true)
					: null;
				rewardSystem?.ApplyRewardTags(targetHero, Hero.MainHero, ref content, rpItemIntroductionContext);
				_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, rewardSystem?.ConsumeLastGeneratedNpcFactLines());
				Logger.Log("ShoutBehavior", "[NativeConversation] ApplyRewardTags done chain=" + rewardChainName + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " beforeVASSALAGE=" + rewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTagForLog(content) + " beforeKINGDOM_ANNEX=" + rewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(content));
				LogNativeActionStep("reward_after", targetHero, targetCharacter, content);
				LogNativeActionStep("marriage_before", targetHero, targetCharacter, content);
				RomanceSystemBehavior.Instance?.ApplyMarriageTags(targetHero, Hero.MainHero, ref content, runPostprocessIfMissing: false);
				LogNativeActionStep("marriage_after", targetHero, targetCharacter, content);
				SexualConceptionBehavior.TryApplyIntimacyTags(targetHero, ref content, rewardChainName);
				LogNativeActionStep("meeting_duel_before", targetHero, targetCharacter, content);
				LordEncounterBehavior.TryProcessMeetingTauntAction(targetHero, ref content, out var escalatedToBattle);
				LordEncounterBehavior.TryConsumeMeetingPlayerReleaseTag(targetHero, ref content, out var meetingReleaseTriggered);
				if (meetingReleaseTriggered)
				{
					LordEncounterBehavior.ScheduleNativeConversationMeetingPlayerRelease(targetHero, "native_conversation_release_tag");
				}
				if (!escalatedToBattle && !meetingReleaseTriggered && !nativeSceneTauntEscalated && Regex.IsMatch(content, "\\[ACTION:DUEL\\]", RegexOptions.IgnoreCase))
				{
					content = Regex.Replace(content, "\\[ACTION:DUEL\\]", "", RegexOptions.IgnoreCase).Trim();
					PrepareDuelFromActionTag(targetHero, 3f, duelDispatchContext);
				}
				LogNativeActionStep("meeting_duel_after", targetHero, targetCharacter, content);
			}
			else if (targetCharacter != null)
			{
				int agentIndex = resolvedTargetAgentIndex;
				RewardSystemBehavior rewardSystem = RewardSystemBehavior.Instance;
				bool isWildernessNonHeroPartyReward = false;
				NpcDataPacket nonHeroNpc = null;
				bool nonHeroJoinTagHandled = false;
				LogNativeActionStep("nonhero_scene_taunt_before", targetHero, targetCharacter, content);
				bool nonHeroSceneTauntHandled = TryProcessNativeConversationSceneTauntTags(targetHero, targetCharacter, agentIndex, ref content, out var nonHeroSceneTauntEscalated);
				if (nonHeroSceneTauntHandled && string.IsNullOrWhiteSpace(content))
				{
					content = BuildFallbackSceneTauntSpeech(nonHeroSceneTauntEscalated);
				}
				_ports.TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, agentIndex, ref content);
				LogNativeActionStep("nonhero_scene_taunt_after", targetHero, targetCharacter, content);
				LogNativeActionStep("nonhero_worldmap_before", targetHero, targetCharacter, content);
				worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(targetHero, targetCharacter, agentIndex, ref content);
				LogNativeActionStep("nonhero_worldmap_after", targetHero, targetCharacter, content);
				LogNativeActionStep("nonhero_party_transfer_before", targetHero, targetCharacter, content);
				if (MyBehavior.TryApplyPartyTransferTagsForExternal(targetHero, targetCharacter, agentIndex, ref content, out var nonHeroTransferFacts, out var nonHeroTransferNotifications))
				{
					_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, nonHeroTransferFacts);
					foreach (string notification in nonHeroTransferNotifications ?? new List<string>())
					{
						if (!string.IsNullOrWhiteSpace(notification))
						{
							InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
						}
					}
				}
				LogNativeActionStep("nonhero_party_transfer_after", targetHero, targetCharacter, content);
				if (rewardSystem != null)
				{
					nonHeroNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
					nonHeroNpc.AgentIndex = agentIndex;
					LogNativeActionStep("nonhero_join_before", targetHero, targetCharacter, content);
					List<string> joinFacts;
					List<string> joinNotifications;
					bool nonHeroJoinHandled;
					if (expectedConversationToken != int.MinValue)
					{
						nonHeroJoinHandled = rewardSystem.TryApplyNonHeroJoinPlayerPartyTagForNativeConversationExternal(targetCharacter, agentIndex, nonHeroNpc.PromptGivenName, nonHeroNpc.PromptDisplayName, expectedConversationManager, expectedConversationToken, ref content, out joinFacts, out joinNotifications);
					}
					else
					{
						nonHeroJoinHandled = rewardSystem.TryApplyNonHeroJoinPlayerPartyTagForExternal(targetCharacter, agentIndex, nonHeroNpc.PromptGivenName, nonHeroNpc.PromptDisplayName, ref content, out joinFacts, out joinNotifications);
					}
					if (nonHeroJoinHandled)
					{
						nonHeroJoinTagHandled = true;
						_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, joinFacts);
						foreach (string notification in joinNotifications ?? new List<string>())
						{
							if (!string.IsNullOrWhiteSpace(notification))
							{
								InformationManager.DisplayMessage(new InformationMessage(notification, notification.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0 ? new Color(1f, 0.45f, 0.25f) : new Color(0.4f, 1f, 0.4f)));
							}
						}
					}
					LogNativeActionStep("nonhero_join_after", targetHero, targetCharacter, content);
				}
				PartyBase wildernessNonHeroParty = null;
				bool hasWildernessNonHeroParty = !nonHeroJoinTagHandled && TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, agentIndex, out wildernessNonHeroParty);
				LogNativeActionStep("nonhero_reward_before", targetHero, targetCharacter, content);
				string npcName = (targetCharacter.Name?.ToString() ?? "对方部队").Trim();
				RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = (rewardSystem == null
					|| !MayContainGeneratedRpItemReward(content))
					? null
					: CreateRpItemIntroductionContextForReward(
						null,
						nonHeroNpc,
						targetCharacter,
						agentIndex,
						npcName,
						latestPlayerText,
						content,
						includeNativeConversationSessionHistory: true);
				if (rewardSystem != null && hasWildernessNonHeroParty)
				{
					rewardSystem.ApplyPartyRewardTags(wildernessNonHeroParty, Hero.MainHero, npcName, targetCharacter, ref content, rpItemIntroductionContext);
					isWildernessNonHeroPartyReward = true;
				}
				else
				{
					rewardSystem?.ApplyMerchantRewardTags(targetCharacter, Hero.MainHero, ref content, rpItemIntroductionContext);
				}
				LogNativeActionStep("nonhero_reward_after", targetHero, targetCharacter, content);
				List<string> list = rewardSystem?.ConsumeLastGeneratedNpcFactLines();
				_ports.RecordGeneratedNpcAfefFactsForNativeConversation(targetHero, targetCharacter, list);
				if (isWildernessNonHeroPartyReward && list != null)
				{
					foreach (string item in list)
					{
						if (!string.IsNullOrWhiteSpace(item))
						{
							AppendWildernessNonHeroMemory(nonHeroNpc, targetHero, targetCharacter, agentIndex, null, null, item);
						}
					}
				}
				if (hasWildernessNonHeroParty)
				{
					LogNativeActionStep("nonhero_meeting_duel_before", targetHero, targetCharacter, content);
					LordEncounterBehavior.TryProcessMeetingTauntAction(null, wildernessNonHeroParty, ref content, out var nonHeroMeetingTauntEscalated);
					LordEncounterBehavior.TryConsumeMeetingPlayerReleaseTag(null, ref content, out var nonHeroMeetingReleaseTriggered);
					if (nonHeroMeetingReleaseTriggered)
					{
						LordEncounterBehavior.ScheduleNativeConversationMeetingPlayerRelease(null, "native_nonhero_conversation_release_tag");
					}
					if (!nonHeroMeetingTauntEscalated && !nonHeroMeetingReleaseTriggered && !nonHeroSceneTauntEscalated && Regex.IsMatch(content, "\\[ACTION:DUEL\\]", RegexOptions.IgnoreCase))
					{
						content = Regex.Replace(content, "\\[ACTION:DUEL\\]", "", RegexOptions.IgnoreCase).Trim();
						Agent duelAgent = (agentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex) : null;
						if (duelAgent != null)
						{
							PrepareDuelFromActionTag(duelAgent, 3f, duelDispatchContext);
						}
						else
						{
							if (nonHeroNpc == null)
							{
								nonHeroNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
								nonHeroNpc.AgentIndex = agentIndex;
							}
							if (TryResolveWildernessNonHeroMemoryForExternal(nonHeroNpc, targetHero, targetCharacter, agentIndex, out var duelMemoryId, out var duelMemoryName))
							{
								DuelBehavior.SetPendingNonHeroDuelMemoryTarget(duelMemoryId, duelMemoryName);
							}
							PrepareDuelFromActionTag(targetCharacter, 3f, duelDispatchContext);
						}
					}
					LogNativeActionStep("nonhero_meeting_duel_after", targetHero, targetCharacter, content);
				}
				else if (!nonHeroSceneTauntEscalated && Regex.IsMatch(content, "\\[ACTION:DUEL\\]", RegexOptions.IgnoreCase))
				{
					LogNativeActionStep("nonhero_duel_before", targetHero, targetCharacter, content);
					content = Regex.Replace(content, "\\[ACTION:DUEL\\]", "", RegexOptions.IgnoreCase).Trim();
					Agent duelAgent = (agentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex) : null;
					if (duelAgent != null)
					{
						PrepareDuelFromActionTag(duelAgent, 3f, duelDispatchContext);
					}
					else
					{
						if (nonHeroNpc == null)
						{
							nonHeroNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
							nonHeroNpc.AgentIndex = agentIndex;
						}
						if (TryResolveWildernessNonHeroMemoryForExternal(nonHeroNpc, targetHero, targetCharacter, agentIndex, out var duelMemoryId, out var duelMemoryName))
						{
							DuelBehavior.SetPendingNonHeroDuelMemoryTarget(duelMemoryId, duelMemoryName);
						}
						PrepareDuelFromActionTag(targetCharacter, 3f, duelDispatchContext);
					}
					LogNativeActionStep("nonhero_duel_after", targetHero, targetCharacter, content);
				}
			}
			LogNativeActionStep("npc_surrender_before", targetHero, targetCharacter, content);
			if (TryConsumeNativeConversationSiegeSurrenderTag(targetHero, targetCharacter, ref content, out var siegeSurrenderAgentIndex))
			{
				Logger.Log("SiegeSurrender", "Consumed native/direct conversation siege surrender tag. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + siegeSurrenderAgentIndex);
			}
			if (TryConsumeNativeConversationNpcSurrenderTag(targetHero, targetCharacter, ref content, out var surrenderAgentIndex))
			{
				_ports.QueueNativeConversationNpcSurrender(targetHero, targetCharacter, surrenderAgentIndex, "native_conversation_npc_surrender_tag");
			}
			LogNativeActionStep("done", targetHero, targetCharacter, content);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] action tag handling failed: " + ex.Message);
		}
		return worldMapResult;
	}

	internal Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(Hero targetHero, CharacterObject targetCharacter, NpcDataPacket npc, List<NpcDataPacket> allNpcData, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string content, string npcName, int targetAgentIndex, string playerText, ConversationManager expectedConversationManager, int expectedConversationToken, NativeConversationAdmission admission, NativeConversationCompletionRequest completion = null)
	{
		long retirementVersion = _pendingMainThreadFunctions.Version;
		if (!_pendingMainThreadFunctions.Accepting)
			return Task.FromResult(new NativeConversationGameActionResult { ResponseDiscarded = true });
		string initial = content ?? "";
		string targetLog = targetHero?.StringId ?? targetCharacter?.StringId ?? npcName ?? npc?.Name ?? "unknown";
		NativeConversationCompletionScope completionScope = null;
		Func<NativeConversationGameActionResult> dispatch = () => ExecuteNativeConversationActionDispatch(admission,
			() =>
			{
				NativeConversationGameActionResult result = ApplyNativeConversationGameActionsCore(targetHero, targetCharacter, npc, allNpcData,
					sceneSummonTargets, sceneGuideTargets, initial, playerText, expectedConversationManager, expectedConversationToken);
				if (completionScope != null && result != null && !result.ResponseDiscarded)
				{
					RunNativeAcceptedReplySideEffects(completion);
					result.FinalVisible = CompleteNativeConversationReplyOnMainThread(completionScope, result);
				}
				return result;
			}, targetLog, targetAgentIndex,
			beforeOwner: completion == null ? null : () => completionScope = CaptureNativeConversationCompletionOnMainThread(
				admission, npc, npcName, targetAgentIndex, completion),
			onDiscard: completion == null ? null : () => _ports.RollbackPendingPlayerHistory(
				admission, completion.PendingPlayerHistoryKey, completion.PendingPlayerHistorySequence, "action_dispatch_target_unavailable"));
		if (IsBannerlordMainThreadForNativeActions())
		{
			try { return Task.FromResult(dispatch()); }
			catch (Exception ex) { return Task.FromException<NativeConversationGameActionResult>(ex); }
		}
		var tcs = new TaskCompletionSource<NativeConversationGameActionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var dispatchClaim = new NativeConversationDispatchClaim();
		IDisposable registration = _pendingMainThreadFunctions.Register(retirementVersion, () =>
		{
			if (dispatchClaim.TryExpireBeforeStart())
				tcs.TrySetResult(new NativeConversationGameActionResult { ResponseDiscarded = true });
		});
		if (registration == null) return tcs.Task;
		try
		{
			_ports.PostMainThread(() =>
			{
				if (!dispatchClaim.TryStart())
					return;
				try { tcs.TrySetResult(dispatch()); }
				catch (Exception ex) { tcs.TrySetException(ex); }
			});
		}
		catch (Exception ex)
		{
			// If a queue published before throwing, cancel its unclaimed callback. If already
			// claimed, only that callback may settle the Task; do not overwrite its real result.
			if (dispatchClaim.TryExpireBeforeStart())
				tcs.TrySetException(new NativeConversationActionDispatchException(false, ex));
			ObserveNativeActionDispatch("queue_exception", targetLog, targetAgentIndex, error: ex);
			return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(tcs.Task, registration);
		}
		ObserveNativeActionDispatch("queued", targetLog, targetAgentIndex);
		return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(AwaitDispatch(), registration);

		async Task<NativeConversationGameActionResult> AwaitDispatch()
		{
			using (var timeout = new CancellationTokenSource())
			{
				try
				{
					Task winner = await Task.WhenAny(tcs.Task,
						Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs, timeout.Token)).ConfigureAwait(false);
					// Only a callback that has never claimed execution can expire. Once claimed,
					// await its real outcome: a timer cannot roll back or safely repeat game actions.
					if (winner != tcs.Task && dispatchClaim.TryExpireBeforeStart())
					{
						var cause = new TimeoutException("native.action_queue_not_consumed");
						tcs.TrySetException(new NativeConversationActionDispatchException(false, cause, queueTimedOut: true));
						ObserveNativeActionDispatch("queue_timeout", targetLog, targetAgentIndex, error: cause);
					}
					return await tcs.Task.ConfigureAwait(false);
				}
				finally
				{
					// Do not retain a per-request delay until its deadline after normal completion.
					timeout.Cancel();
				}
			}
		}
	}

	internal NativeConversationGameActionResult ApplyNativeConversationGameActionsLegacyCore(
		Hero targetHero,
		CharacterObject targetCharacter,
		NpcDataPacket npc,
		List<NpcDataPacket> allNpcData,
		List<SceneSummonPromptTarget> sceneSummonTargets,
		List<SceneGuidePromptTarget> sceneGuideTargets,
		string content,
		string playerText,
		ConversationManager expectedConversationManager,
		int expectedConversationToken,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
		string result = content ?? "";
		int targetAgentIndex = npc?.AgentIndex ?? (-1);
		if (_ports.TryTriggerNativeConversationOpenLordsHallAction(targetHero, targetCharacter, targetAgentIndex, ref result))
		{
			return new NativeConversationGameActionResult
			{
				Content = result ?? "",
				WorldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult()
			};
		}
		if (!IsNativeConversationResponseTargetAvailableForActionDispatch(targetAgentIndex, targetHero, targetCharacter, out string unavailableReason))
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] dropped queued postprocess actions because target is unavailable target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? npc?.Name ?? "unknown") + " agentIndex=" + targetAgentIndex + " reason=" + unavailableReason);
			return new NativeConversationGameActionResult
			{
				Content = "",
				WorldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult(),
				ResponseDiscarded = true
			};
		}
		if (targetHero != null)
		{
			MyBehavior.ApplyPostprocessMoodFromSceneHeroResponseExternal(targetHero, ref result);
		}
		else
		{
			MyBehavior.ApplyPostprocessMoodFromSceneUnnamedResponseExternal(npc?.UnnamedKey, npc?.Name, ref result);
		}
		_ports.TryQueueNativeSceneMechanismActionAfterConversationExit(npc, allNpcData, sceneSummonTargets, sceneGuideTargets, ref result);
		WorldMapPartyCommandBehavior.WorldMapOrderApplyResult worldMapResult = ApplyNativeConversationActionTags(
			targetHero,
			targetCharacter,
			ref result,
			targetAgentIndex,
			playerText,
			expectedConversationManager,
			expectedConversationToken,
			duelDispatchContext: duelDispatchContext);
		return new NativeConversationGameActionResult { Content = result ?? "", WorldMapResult = worldMapResult };
	}

}
