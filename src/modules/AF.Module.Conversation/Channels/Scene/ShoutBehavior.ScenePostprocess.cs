using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using static AnimusForge.SceneMovementController;
using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
	internal enum ScenePostprocessStatus { Completed, NoAction, Stale, TargetUnavailable, TimedOut, Failed }

	internal sealed class ScenePostprocessOutcome
	{
		internal ScenePostprocessOutcome(ScenePostprocessStatus status, int relayTargetAgentIndex = -1)
		{
			Status = status;
			RelayTargetAgentIndex = relayTargetAgentIndex;
		}
		internal ScenePostprocessStatus Status { get; }
		internal int RelayTargetAgentIndex { get; }
		internal bool Succeeded => Status == ScenePostprocessStatus.Completed || Status == ScenePostprocessStatus.NoAction;
	}

	internal Task<ScenePostprocessOutcome> QueueDeferredScenePostprocessActions(NpcDataPacket currentSpeaker, List<NpcDataPacket> allNpcData, Hero speakingHero, CharacterObject npcCharacter, string privateRecentWindowSection, string scenePublicHistorySection, string playerText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, bool siegeInterventionRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, int conversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, string entityPostprocessContext = null, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, long expectedRuntimeGeneration = 0L, int expectedSceneSessionId = -1)
	{
		if (currentSpeaker == null || string.IsNullOrWhiteSpace(replyText))
		{
			return Task.FromResult(new ScenePostprocessOutcome(ScenePostprocessStatus.Failed));
		}
		long queuedRuntimeGeneration = expectedRuntimeGeneration > 0L ? expectedRuntimeGeneration : SaveRuntimeGuard.CaptureGeneration();
		int queuedSceneSessionId = expectedSceneSessionId >= 0 ? expectedSceneSessionId : _ports.SceneSessionId();
		int capturedConversationEpoch = conversationEpoch > 0 ? conversationEpoch : Volatile.Read(ref _sceneConversationEpoch);
		var requestLifetime = _sceneRequestLifetime;
		CancellationTokenSource networkCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestLifetime.Token);
		ExecutionContext requestExecutionContext = ExecutionContext.Capture();
		object runtimeScopeLock = new object();
		int requestRetired = 0;
		TaskCompletionSource<ScenePostprocessOutcome> postprocessCompletion = new TaskCompletionSource<ScenePostprocessOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
		bool Complete(ScenePostprocessStatus status, int relayTargetAgentIndex = -1)
			=> postprocessCompletion.TrySetResult(new ScenePostprocessOutcome(status, relayTargetAgentIndex));
		List<string> preprocessRuleSnapshot = (preprocessRuleHits ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		NpcDataPacket speakerSnapshot = CloneNpcDataPacket(currentSpeaker);
		List<NpcDataPacket> contextSnapshot = CloneNpcDataSnapshot(allNpcData);
		List<NpcDataPacket> relayCandidateSnapshot = CloneNpcDataSnapshot(relayCandidates);
		List<SceneSummonPromptTarget> summonSnapshot = SceneMovementController.CloneSceneSummonPromptTargets(sceneSummonTargets);
		List<SceneGuidePromptTarget> guideSnapshot = SceneMovementController.CloneSceneGuidePromptTargets(sceneGuideTargets);
		List<PostprocessRuleEntry> sceneMechanismRuleSnapshot = (sceneMechanismRules ?? new List<PostprocessRuleEntry>()).Where((PostprocessRuleEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Tag)).Select((PostprocessRuleEntry x) => new PostprocessRuleEntry
		{
			Tag = x.Tag,
			Description = x.Description
		}).ToList();
		string privateRecentWindowForPostprocess = TrimPrivateRecentWindowForActionPostprocess(privateRecentWindowSection, 5);
		privateRecentWindowForPostprocess = FilterHistorySectionAgainstScenePublicHistory(privateRecentWindowForPostprocess, scenePublicHistorySection);
		string historyForPostprocess = BuildSceneCompositeUserBlock("", privateRecentWindowForPostprocess, scenePublicHistorySection);
		if (string.IsNullOrWhiteSpace(historyForPostprocess))
		{
			historyForPostprocess = playerText;
		}
		string replySnapshot = replyText;
		int runtimeTargetAgentIndex = speakerSnapshot.AgentIndex;
		string runtimeTargetHeroId = "";
		string runtimeTargetCharacterId = "";
		string runtimeTargetTroopId = "";
		string runtimeTargetUnnamedRank = "";
		string runtimeTargetKingdomId = "";
		string scenePostprocessChainName = "";
		string targetLog = speakerSnapshot.Name ?? "unknown";
		bool explicitlyNoRules = false;

		bool IsRequestCurrent()
		{
			return !networkCancellation.IsCancellationRequested && Volatile.Read(ref requestRetired) == 0
				&& SaveRuntimeGuard.IsCurrentGeneration(queuedRuntimeGeneration)
				&& queuedSceneSessionId == _ports.SceneSessionId()
				&& capturedConversationEpoch == Volatile.Read(ref _sceneConversationEpoch);
		}

		// This validation is only called from the game-thread phases, never by the network worker.
		bool ValidateCurrentTarget(string phase)
		{
			if (!IsRequestCurrent())
			{
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] skipped stale request phase=" + phase + " npc=" + targetLog);
				Complete(ScenePostprocessStatus.Stale);
				return false;
			}
			if (!IsNativeConversationResponseTargetAvailableForActionDispatch(runtimeTargetAgentIndex, speakingHero, npcCharacter, out string unavailableReason))
			{
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] dropped response because target is unavailable npc=" + targetLog + " agentIndex=" + runtimeTargetAgentIndex + " phase=" + phase + " reason=" + unavailableReason);
				Complete(ScenePostprocessStatus.TargetUnavailable);
				return false;
			}
			return true;
		}

		bool CanStillPublish()
		{
			if (!IsRequestCurrent())
			{
				return false;
			}
			// Speech checks this on both threads. Live availability is resolved only on the game thread.
			return !IsBannerlordMainThreadForNativeActions() || ValidateCurrentTarget("speech_publish");
		}

		T RunInRuntimeScope<T>(Func<T> action)
		{
			T result = default(T);
			void Invoke()
			{
				using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
				AIConfigHandler.SetGuardrailRuntimeTargetKingdom(runtimeTargetKingdomId);
				AIConfigHandler.SetGuardrailRuntimeTargetHero(runtimeTargetHeroId);
				AIConfigHandler.SetGuardrailRuntimeTargetCharacter(runtimeTargetCharacterId);
				AIConfigHandler.SetGuardrailRuntimeTargetTroop(runtimeTargetTroopId);
				AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank(runtimeTargetUnnamedRank);
				AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(runtimeTargetAgentIndex);
				try
				{
					result = action();
				}
				finally
				{
					AIConfigHandler.ClearGuardrailRuntimeTarget();
				}
			}
			// Preserve this request's AsyncLocal mentioned-entity snapshot while restoring
			// the game thread's previous context on scope exit.
			ExecutionContext scope;
			lock (runtimeScopeLock)
			{
				if (!IsRequestCurrent())
				{
					return default(T);
				}
				scope = requestExecutionContext?.CreateCopy();
			}
			if (scope == null)
			{
				Invoke();
			}
			else
			{
				// The callback owns this copy. Worker timeout can retire/dispose the captured
				// source context, but cannot dispose a copy while the game callback uses it.
				using (scope)
				{
					ExecutionContext.Run(scope, _ => Invoke(), null);
				}
			}
			return result;
		}

		async Task EnforceRequestDeadlineAsync(CancellationToken cancellationToken)
		{
			try
			{
				await Task.Delay(ScenePostprocessGateWaitTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
				Interlocked.Exchange(ref requestRetired, 1);
				networkCancellation.Cancel();
				if (Complete(ScenePostprocessStatus.TimedOut))
				{
					Logger.Log("ShoutBehavior", "[DeferredPostprocess] request deadline exceeded npc=" + targetLog + " timeoutMs=" + ScenePostprocessGateWaitTimeoutMilliseconds);
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// A completed request cancels its own timer; no shared gate or queue is changed.
			}
		}

		Task<ScenePostprocessOutcome> task = postprocessCompletion.Task;
		RegisterScenePostprocessGateTask(task);
		CancellationTokenSource deadlineCancellation = new CancellationTokenSource();
		Task deadlineTask = EnforceRequestDeadlineAsync(deadlineCancellation.Token);
		_ = Task.Run(async delegate
		{
			try
			{
				using IDisposable requestWorker = requestLifetime.Enter();
				using IDisposable cancellationScope = LlmNonStreamingTransport.PushOwnerCancellation(networkCancellation.Token);
				if (!IsRequestCurrent())
				{
					Complete(ScenePostprocessStatus.Stale);
					return;
				}
				Stopwatch postprocessWatch = Stopwatch.StartNew();
				SceneActionPostprocessWorkItem workItem = await _dispatcher.RunAsync(
					"scene_postprocess_prepare", targetLog, runtimeTargetAgentIndex, () =>
					{
						if (!ValidateCurrentTarget("before_prepare"))
						{
							return (SceneActionPostprocessWorkItem)null;
						}
						runtimeTargetHeroId = speakingHero?.StringId ?? npcCharacter?.HeroObject?.StringId ?? "";
						runtimeTargetCharacterId = npcCharacter?.StringId ?? "";
						runtimeTargetTroopId = npcCharacter?.StringId ?? "";
						runtimeTargetUnnamedRank = (speakingHero == null && npcCharacter != null) ? (npcCharacter.IsSoldier ? "soldier" : "commoner") : "";
						runtimeTargetKingdomId = "";
						try
						{
							Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == runtimeTargetAgentIndex);
							runtimeTargetKingdomId = TryGetKingdomIdOverrideFromAgent(agent);
						}
						catch
						{
							runtimeTargetKingdomId = "";
						}
						if (string.IsNullOrWhiteSpace(runtimeTargetKingdomId))
						{
							try
							{
								runtimeTargetKingdomId = (speakingHero?.Clan?.Kingdom?.StringId ?? npcCharacter?.HeroObject?.Clan?.Kingdom?.StringId ?? "").Trim();
							}
							catch
							{
								runtimeTargetKingdomId = "";
							}
						}
						return RunInRuntimeScope(() =>
						{
							bool npcSurrenderPostprocessSelected = IsNpcSurrenderPostprocessContext();
							bool nobleGatheringRuleInjected = HasPreprocessRuleHit(preprocessRuleSnapshot, "noble_gathering");
							bool proposeAgendaRuleInjected = false;
							bool persistentAdpDebtRuleInjected = HasPreprocessRuleHit(preprocessRuleSnapshot, PersistentAdpDebtPostprocessRuleId);
							customPolicyAgendaRuleInjected = replyIsDirectPlayerResponse && (customPolicyAgendaRuleInjected || HasPreprocessRuleHit(preprocessRuleSnapshot, CustomPolicyAgendaPostprocessRuleId));
							// 合格国王的每轮回复都要进入王位让渡后处理；不要用 diplomacy 话题命中限制这个常驻规则。
							bool royalPostprocessSelected = AIConfigHandler.CanUseAuxiliaryActionPostprocess()
								&& AIConfigHandler.IsRoyalAbdicationPostprocessTargetForExternal(speakingHero ?? npcCharacter?.HeroObject);
							bool independentClanPeaceResident = replyIsDirectPlayerResponse
								&& DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(speakingHero, npcCharacter);
							diplomacyRuleInjected = diplomacyRuleInjected || independentClanPeaceResident;
							if (!HasPreprocessRuleHit(preprocessRuleSnapshot, PublicExecutionOrderPolicy.RuleId) && !duelRuleInjected && !rewardRuleInjected && !loanRuleInjected && !persistentAdpDebtRuleInjected && !kingdomServiceRuleInjected && !kingdomVassalageRuleInjected && !kingdomAnnexationRuleInjected && !lordsHallRuleInjected && !meetingReleaseRuleInjected && !vanillaIssueRuleInjected && !heroJoinPartyRuleInjected && !sceneMechanismRuleInjected && !partyTransferRuleInjected && !voteDealRuleInjected && !customPolicyAgendaRuleInjected && !diplomacyRuleInjected && !worldMapPartyCommandRuleInjected && !nobleGatheringRuleInjected && !proposeAgendaRuleInjected && !marriageRuleInjected && !siegeInterventionRuleInjected && !relayRuleInjected && !npcSurrenderPostprocessSelected && !royalPostprocessSelected)
							{
								explicitlyNoRules = true;
								return (SceneActionPostprocessWorkItem)null;
							}
							scenePostprocessChainName = ResolveScenePostprocessChainName();
							Logger.Log("ShoutBehavior", "[DeferredPostprocess] queued chain=" + scenePostprocessChainName
								+ " npc=" + (speakingHero?.StringId ?? currentSpeaker?.Name ?? "unknown")
								+ " preprocessHits=" + (preprocessRuleSnapshot.Count == 0 ? "(none)" : string.Join(",", preprocessRuleSnapshot))
								+ " kingdomVassalageInjected=" + kingdomVassalageRuleInjected
								+ " kingdomAnnexationInjected=" + kingdomAnnexationRuleInjected
								+ " proposeAgendaInjected=" + proposeAgendaRuleInjected
								+ " royalSelected=" + royalPostprocessSelected
								+ " independentClanPeaceResident=" + independentClanPeaceResident
								+ " npcSurrenderSelected=" + npcSurrenderPostprocessSelected);
							if (kingdomVassalageRuleInjected)
							{
								VassalageDiagnosticLog.Event("postprocess.scene.deferred_queued", new Dictionary<string, object>
								{
									["chain"] = scenePostprocessChainName,
									["speaker"] = VassalageDiagnosticLog.DescribeHero(speakingHero),
									["npcCharacterId"] = npcCharacter?.StringId ?? "",
									["agentIndex"] = currentSpeaker?.AgentIndex ?? -1,
									["playerText"] = VassalageDiagnosticLog.Preview(playerText, 1000),
									["replyText"] = VassalageDiagnosticLog.Preview(replyText, 2000),
									["conversationEpoch"] = conversationEpoch,
									["preprocessHits"] = preprocessRuleSnapshot
								});
							}
							if (kingdomServiceRuleInjected)
							{
								Logger.Log("ShoutBehavior", "[KingdomServicePostprocess] queued npc=" + (speakingHero?.StringId ?? currentSpeaker?.Name ?? "unknown") + " epoch=" + conversationEpoch);
								Logger.Log("ShoutBehavior", "[KingdomServicePostprocess] runtime_target hero=" + runtimeTargetHeroId + " character=" + runtimeTargetCharacterId + " troop=" + runtimeTargetTroopId + " kingdom=" + runtimeTargetKingdomId + " agentIndex=" + runtimeTargetAgentIndex);
								Logger.Log("ShoutBehavior", "[KingdomServicePostprocess] precomputed_rules=" + ((kingdomServiceRules == null || kingdomServiceRules.Count == 0) ? "（无）" : string.Join(",", kingdomServiceRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
							}
							return PrepareSceneUnifiedActionPostprocess(speakingHero, npcCharacter, runtimeTargetAgentIndex, GetSceneNpcHistoryNameForPrompt(speakerSnapshot), playerText, historyForPostprocess, replySnapshot, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRuleSnapshot, summonSnapshot, guideSnapshot, entityPostprocessContext, siegeInterventionRuleInjected: siegeInterventionRuleInjected, replyIsDirectPlayerResponse: replyIsDirectPlayerResponse, preprocessRuleHits: preprocessRuleSnapshot, chainName: scenePostprocessChainName, relayRuleInjected: relayRuleInjected, relayCandidates: relayCandidateSnapshot, relayPrimaryTargetAgentIndex: relayPrimaryTargetAgentIndex, relaySingleFramedNpc: relaySingleFramedNpc, customPolicyAgendaRuleInjected: customPolicyAgendaRuleInjected, offerSceneActionDirective: true);
						});
					}, (SceneActionPostprocessWorkItem)null).ConfigureAwait(false);
				if (workItem == null || !IsRequestCurrent())
				{
					Complete(!IsRequestCurrent() ? ScenePostprocessStatus.Stale
						: explicitlyNoRules ? ScenePostprocessStatus.NoAction : ScenePostprocessStatus.Failed);
					return;
				}

				// Only immutable request strings enter the blocking auxiliary network call.
				PostprocessNetworkRequest networkRequest = workItem.NetworkRequest;
				string content = null;
				string error = null;
				bool succeeded = !workItem.RequiresNetwork
					|| TryRequestSceneUnifiedActionPostprocess(networkRequest.SystemPrompt, networkRequest.UserPrompt, out content, out error);
				if (!IsRequestCurrent())
				{
					Complete(ScenePostprocessStatus.Stale);
					return;
				}

				int relayTargetAgentIndex = -1;
				TaskCompletionSource<bool> speechCompletion = null;
				bool dispatched = await _dispatcher.RunAsync(
					"scene_postprocess_complete_and_dispatch", targetLog, runtimeTargetAgentIndex, () => RunInRuntimeScope(() =>
					{
						if (!ValidateCurrentTarget("before_complete"))
						{
							return false;
						}
						string text = CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error);
						// Strip now; submit only after the before_dispatch target check below.
						string sceneActionDirective = ExtractSceneActionDirective(ref text, runtimeTargetAgentIndex);
						postprocessWatch.Stop();
						Logger.Log("ShoutBehavior", "[DeferredPostprocess] call_done npc=" + targetLog + " elapsedMs=" + Math.Round(postprocessWatch.Elapsed.TotalMilliseconds, 2) + " textLen=" + (text?.Length ?? 0));
						string relayTag = relayRuleInjected ? NormalizeAutoGroupRelayPostprocessTagsForScene(text, relayCandidateSnapshot, runtimeTargetAgentIndex) : "";
						if (!string.IsNullOrWhiteSpace(relayTag))
						{
							TryParseAutoGroupRelayTargetAgentIndex(relayTag, out relayTargetAgentIndex);
						}
						string text2 = ExtractDeferredSceneActionTags(text);
						if (kingdomVassalageRuleInjected || (text2 ?? "").IndexOf("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							VassalageDiagnosticLog.Event("postprocess.scene.deferred_extracted", new Dictionary<string, object>
							{
								["speaker"] = VassalageDiagnosticLog.DescribeHero(speakingHero),
								["npcCharacterId"] = npcCharacter?.StringId ?? "",
								["agentIndex"] = runtimeTargetAgentIndex,
								["rawPostprocessText"] = VassalageDiagnosticLog.Preview(text, 4000),
								["extractedTags"] = text2 ?? ""
							});
						}
						Logger.Log("ShoutBehavior", "[DeferredPostprocess] npc=" + (speakingHero?.StringId ?? currentSpeaker?.Name ?? "unknown") + " raw=" + ((text ?? "").Replace("\r", "\\r").Replace("\n", "\\n")) + " tags=" + ((text2 ?? "").Replace("\r", "\\r").Replace("\n", "\\n")) + " relay=" + relayTargetAgentIndex);
						if (!ValidateCurrentTarget("before_dispatch"))
						{
							return false;
						}
						SubmitSceneActionDirective(sceneActionDirective, runtimeTargetAgentIndex, replySnapshot);
						if (string.IsNullOrWhiteSpace(text2))
						{
							Complete(succeeded ? ScenePostprocessStatus.Completed : ScenePostprocessStatus.Failed, relayTargetAgentIndex);
							return true;
						}
						string deferredTags = text2;
						string text3 = ExtractDeferredSceneActionTags(deferredTags);
						if (!CommitDeferredSceneActionPlan(
							text3,
							speakerSnapshot,
							contextSnapshot,
							speakingHero,
							npcCharacter,
							runtimeTargetAgentIndex,
							playerText,
							replySnapshot,
							scenePostprocessChainName,
							replyIsDirectPlayerResponse,
							siegeInterventionRuleInjected,
							capturedConversationEpoch,
							summonSnapshot,
							guideSnapshot,
							() => ValidateCurrentTarget("before_speech_enqueue"),
							CanStillPublish,
							out speechCompletion))
						{
							return false;
						}
						if (speechCompletion != null)
						{
							// The queued speech/action callback owns the terminal result.
							// Do not publish the relay before that callback revalidates
							// generation/session/epoch/target and reports completion.
							return true;
						}
						if (!ValidateCurrentTarget("before_relay_publish"))
						{
							return false;
						}
						Complete(succeeded ? ScenePostprocessStatus.Completed : ScenePostprocessStatus.Failed, relayTargetAgentIndex);
						return true;
					}), false).ConfigureAwait(false);
				if (!dispatched)
				{
					Complete(ScenePostprocessStatus.Failed);
					return;
				}
				if (speechCompletion != null)
				{
					// Clearing another scene/epoch can remove this queue item without completing it.
					// Bound only this request; do not flush other speech or retry actions already applied.
					Task completed = await Task.WhenAny(speechCompletion.Task, Task.Delay(NativeConversationMainThreadPreprocessTimeoutMs)).ConfigureAwait(false);
					if (completed != speechCompletion.Task || !IsRequestCurrent())
					{
						Interlocked.Exchange(ref requestRetired, 1);
						Complete(completed != speechCompletion.Task
							? ScenePostprocessStatus.TimedOut : ScenePostprocessStatus.Stale);
						return;
					}
					bool speechSucceeded = await speechCompletion.Task.ConfigureAwait(false);
					bool published = await _dispatcher.RunAsync(
						"scene_postprocess_relay_publish", targetLog, runtimeTargetAgentIndex, () =>
						{
							if (!ValidateCurrentTarget("after_speech"))
							{
								return false;
							}
							Complete(speechSucceeded && succeeded ? ScenePostprocessStatus.Completed : ScenePostprocessStatus.Failed,
								speechSucceeded ? relayTargetAgentIndex : -1);
							return true;
						}, false).ConfigureAwait(false);
					if (!published)
					{
						Complete(ScenePostprocessStatus.Failed);
					}
				}
			}
			catch (OperationCanceledException) when (networkCancellation.IsCancellationRequested)
			{
				Complete(requestLifetime.Token.IsCancellationRequested ? ScenePostprocessStatus.Stale : ScenePostprocessStatus.TimedOut);
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[ERROR] QueueDeferredScenePostprocessActions: " + ex.Message);
				Complete(ScenePostprocessStatus.Failed);
			}
			finally
			{
				Interlocked.Exchange(ref requestRetired, 1);
				Complete(ScenePostprocessStatus.Failed);
				deadlineCancellation.Cancel();
				await deadlineTask.ConfigureAwait(false);
				deadlineCancellation.Dispose();
				networkCancellation.Dispose();
				lock (runtimeScopeLock)
				{
					requestExecutionContext?.Dispose();
					requestExecutionContext = null;
				}
			}
		});
		return task;
	}

	internal bool CommitDeferredSceneActionPlan(
		string rawTags,
		NpcDataPacket speakerSnapshot,
		List<NpcDataPacket> contextSnapshot,
		Hero speakingHero,
		CharacterObject npcCharacter,
		int targetAgentIndex,
		string playerText,
		string replyText,
		string chainName,
		bool replyIsDirectPlayerResponse,
		bool siegeInterventionRuleInjected,
		int conversationEpoch,
		List<SceneSummonPromptTarget> summonTargets,
		List<SceneGuidePromptTarget> guideTargets,
		Func<bool> validateCurrentTarget,
		Func<bool> canStillPublish,
		out TaskCompletionSource<bool> speechCompletion)
	{
		speechCompletion = null;
		var actionCommitter = new LegacyChannelActionCommitter();
		LegacyChannelActionCommitResult prepared = actionCommitter.Prepare(rawTags);
		if (!prepared.HasActions)
		{
			if (prepared.Execution.Status != InteractionStatus.Succeeded)
			{
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] action protocol rejected error="
					+ prepared.Execution.ErrorCode);
			}
			return prepared.Execution.Status == InteractionStatus.Succeeded;
		}

		InteractionEnvelope envelope;
		try
		{
			string subjectId = ResolveDetachedInteractionSubjectId(
				speakingHero,
				npcCharacter,
				targetAgentIndex,
				speakerSnapshot);
			envelope = LegacyInteractionSnapshotAdapters.CaptureActionCommit(
				InteractionChannel.SceneShout,
				"scene-action:" + GetCurrentSceneHistorySessionIdForExternal()
					+ ":" + conversationEpoch + ":" + targetAgentIndex,
				subjectId,
				playerText ?? string.Empty,
				new[]
				{
					new InteractionCandidate(
						subjectId,
						speakingHero?.Name?.ToString()
							?? npcCharacter?.Name?.ToString()
							?? speakerSnapshot?.Name
							?? string.Empty,
						targetAgentIndex,
						true)
				},
				new Dictionary<string, string>(StringComparer.Ordinal)
				{
					["scene_session_id"] = GetCurrentSceneHistorySessionIdForExternal().ToString(),
					["target_agent_index"] = targetAgentIndex.ToString()
				});
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[DeferredPostprocess] action snapshot rejected error="
				+ ex.GetType().Name);
			return false;
		}
		if (envelope?.Snapshot?.Identity == null)
		{
			return false;
		}

		bool targetRejected = false;
		TaskCompletionSource<bool> pendingSpeech = null;
		var executor = new LegacyChannelActionPlanExecutor(
			envelope.Snapshot.Identity.Channel,
			envelope.Snapshot.Identity.SessionId,
			envelope.Snapshot.Identity.SubjectId,
			(plan, snapshot) =>
			{
				if (validateCurrentTarget == null || !validateCurrentTarget())
				{
					targetRejected = true;
					return InteractionStatus.RejectedByValidation;
				}

				string remaining = plan.RawPostprocessId;
				bool consumed = false;
				if (_ports.TryApplyDeferredSceneMoodTag(speakerSnapshot, remaining))
				{
					consumed = true;
					remaining = StripDeferredSceneMoodTags(remaining);
				}
				if (siegeInterventionRuleInjected)
				{
					string beforeSiegeTags = remaining;
					TeamModuleServices.Siege.TryProcessActionTags(
						speakingHero,
						npcCharacter,
						targetAgentIndex,
						ref remaining,
						out bool siegeActionHandled,
						replyIsDirectPlayerResponse,
						replyIsDirectPlayerResponse ? playerText : string.Empty,
						replyText);
					if (siegeActionHandled
						|| !string.Equals(beforeSiegeTags, remaining, StringComparison.Ordinal))
					{
						consumed = true;
						Logger.Log("ShoutBehavior", "[DeferredPostprocess] siege_intervention handled="
							+ siegeActionHandled + " npc="
							+ (speakingHero?.StringId ?? speakerSnapshot?.Name ?? "unknown"));
						remaining = ExtractDeferredSceneActionTags(remaining);
					}
				}
				if (_ports.TryApplyDeferredScenePostprocessActionTagsDirectly(
					speakingHero,
					npcCharacter,
					targetAgentIndex,
					ref remaining,
					replyIsDirectPlayerResponse ? playerText : string.Empty,
					replyText,
					chainName,
					replyIsDirectPlayerResponse))
				{
					consumed = true;
					remaining = ExtractDeferredSceneActionTags(remaining);
				}
				if (HasNonMoodDeferredSceneActionTag(remaining))
				{
					if (_sceneMovement.TryExecuteDeferredSceneFollowTagsDirectly(speakerSnapshot, remaining))
					{
						consumed = true;
					}
					else
					{
						if (validateCurrentTarget == null || !validateCurrentTarget())
						{
							targetRejected = true;
							return InteractionStatus.RejectedByValidation;
						}
						pendingSpeech = new TaskCompletionSource<bool>(
							TaskCreationOptions.RunContinuationsAsynchronously);
						_ports.EnqueueSpeechLineWithOptions(
							speakerSnapshot,
							remaining,
							contextSnapshot,
							commitHistory: false,
							suppressStare: true,
							allowPlayerDirectedActions: true,
							requiredConversationEpoch: conversationEpoch,
							summonTargets,
							guideTargets,
							null,
							pendingSpeech,
							canStillPublish: canStillPublish,
							playerDirectedActionText: replyIsDirectPlayerResponse ? playerText : string.Empty,
							playerDirectedNpcReplyText: replyText);
						consumed = true;
					}
				}
				return pendingSpeech != null
					? InteractionStatus.NonRetryableFailure
					: consumed
						? InteractionStatus.Executed
						: InteractionStatus.RejectedByValidation;
			});

		LegacyChannelActionCommitResult committed = actionCommitter.Commit(
			prepared.ActionPlan,
			envelope.Snapshot,
			executor);
		speechCompletion = pendingSpeech;
		Logger.Log("ShoutBehavior", "[DeferredPostprocess] action commit status="
			+ committed.Execution.Status + " effect=" + committed.Execution.EffectState
			+ " error=" + committed.Execution.ErrorCode);
		return !targetRejected && (pendingSpeech != null
				&& committed.Execution.Status == InteractionStatus.NonRetryableFailure
				&& committed.Execution.EffectState == ActionExecutionEffectState.UnknownAfterStart
			|| committed.Execution.Status == InteractionStatus.Executed
			|| committed.Execution.Status == InteractionStatus.Succeeded);
	}



}
