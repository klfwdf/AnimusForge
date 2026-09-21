using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private void DeliverToRecipient(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null || recipient == null)
		{
			return;
		}
		if (!session.DeliveryApplied)
		{
			ApplyDeliveryPayload(session, courier, recipient);
			session.DeliveryApplied = true;
			session.DeliveryFactText = BuildDeliveryFactText(session, delivered: true, recipient, commitPlayerCraftInspection: true);
			string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
			}
			MyBehavior.AppendExternalDialogueHistory(recipient, "【来信】" + playerName + "通过信使写道：" + session.LetterText, null, session.DeliveryFactText);
			Log("delivered session=" + session.Id + " recipient=" + SafeHeroId(recipient) + " factLen=" + (session.DeliveryFactText ?? "").Length);
		}
		if (!session.ReplyGenerated)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
			ShowCourierReplyWaitPopupAndPause(session, recipient);
			StartCourierReplyGeneration(session, "delivered");
			MaintainReplyWaitAtRecipient(session, courier, recipient);
			return;
		}
		CommitGeneratedReplyAtRecipient(session, recipient);
		EndCourierReplyWaitPause(session, "delivered_reply_ready");
		session.Stage = CourierStage.Returning.ToString();
		RouteToSender(session, courier);
	}

	private void StartCourierReplyGeneration(CourierSession session, string reason)
	{
		if (session == null || session.ReplyGenerated || session.ReplyGenerationStarted)
		{
			return;
		}
		if (session.DeliveryApplied)
		{
			session.Stage = CourierStage.GeneratingReply.ToString();
		}
		session.ReplyGenerationStarted = true;
		Log("reply generation queued session=" + session.Id + " reason=" + (reason ?? ""));
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string sessionId = session.Id;
		CourierPromptRun promptRun = BeginCourierPromptRun(session, runtimeGeneration);
		EnqueueMainThreadActionForGeneration(runtimeGeneration, () => BeginCourierReplyGenerationOnMainThread(sessionId, runtimeGeneration, promptRun), "reply_prepare");
	}

	private void StartInboundLetterGeneration(CourierSession session, string reason)
	{
		if (session == null || !IsInboundToPlayer(session) || session.ReplyGenerated || session.ReplyGenerationStarted)
		{
			return;
		}
		session.ReplyGenerationStarted = true;
		Log("inbound letter generation queued session=" + session.Id + " reason=" + (reason ?? ""));
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string sessionId = session.Id;
		CourierPromptRun promptRun = BeginCourierPromptRun(session, runtimeGeneration);
		EnqueueMainThreadActionForGeneration(runtimeGeneration, () => BeginInboundLetterGenerationOnMainThread(sessionId, runtimeGeneration, promptRun), "inbound_prepare");
	}

	private void HoldInboundCourierAtPlayer(CourierSession session, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return;
		}
		string key = "inbound_letter_wait:" + session.Id;
		if (ShouldRefreshRoute(session, key, courier, AiBehavior.Hold))
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "inbound_letter_wait_hold");
			LogVerbose("inbound_letter_wait_hold:" + session.Id, "inbound letter wait hold session=" + session.Id, 5.0);
		}
	}

	private void MaintainReplyWaitAtRecipient(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null)
		{
			return;
		}
		if (recipient != null && TryGetRecipientTarget(recipient, out var targetParty, out var targetSettlement) && !IsAtRecipient(courier, targetParty, targetSettlement))
		{
			RouteToRecipient(session, courier, targetParty, targetSettlement);
			return;
		}
		string key = BuildReplyWaitRouteKey(courier, recipient);
		if (ShouldRefreshRoute(session, key, courier, AiBehavior.Hold))
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "reply_wait_hold");
			LogVerbose("reply_wait_hold:" + session.Id, "reply wait hold session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private static string BuildReplyWaitRouteKey(MobileParty courier, Hero recipient)
	{
		string recipientId = SafeHeroId(recipient);
		CampaignVec2 position = courier?.Position ?? MobileParty.MainParty?.Position ?? default;
		int x = (int)MathF.Round(position.X * 2f);
		int y = (int)MathF.Round(position.Y * 2f);
		return "reply_wait:" + recipientId + ":" + x + ":" + y;
	}

	private void BeginCourierReplyGenerationOnMainThread(string sessionId, long runtimeGeneration, CourierPromptRun promptRun)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_reply_prepare_start"))
			{
				return;
			}
			if (!IsCourierPromptRunCurrent(promptRun)) return;
			_ = Task.Run(() => PrepareAndGenerateCourierReplyOffMainThreadAsync(sessionId, runtimeGeneration, promptRun));
		}
		catch (Exception ex)
		{
			Log("queue background reply prepare failed session=" + sessionId + " error=" + ex);
			if (IsCourierPromptRunCurrent(promptRun)) FailCourierReplyGenerationOnMainThread(sessionId, runtimeGeneration, "reply_generation_failed");
		}
	}

	private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(string sessionId, long runtimeGeneration, CourierPromptRun promptRun)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_reply_prepare_background_start"))
			{
				return;
			}
			CourierPreparationAdmission admission = await RunCourierOwnerPhaseAsync(runtimeGeneration,
				"courier_reply_admission", () => IsCourierPromptRunCurrent(promptRun) ? CaptureCourierPreparationAdmission(sessionId, false, runtimeGeneration) : null, CancellationToken.None).ConfigureAwait(false);
			if (admission == null) return;
			CourierSession session = admission.Session;
			Hero recipient = admission.Participant;
			if (!await EnsureCourierPersonaContextReadyAsync(recipient, "reply", sessionId, session, runtimeGeneration).ConfigureAwait(false)) return;
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_reply_persona_ready"))
			{
				return;
			}
			CourierPreparedHistory preparedHistory = await PrepareCourierHistoryAsync(sessionId, session, recipient, false, runtimeGeneration).ConfigureAwait(false);
			if (preparedHistory == null) return;
			CourierReplyGenerationRequest request = await PrepareCourierPromptRequestAsync(sessionId, session, recipient, false, null, runtimeGeneration, preparedHistory, promptRun, BuildReplyRequestFromPreparedPrompt).ConfigureAwait(false);
			if (request == null) return;
			ShoutNetwork.RecordPrimaryRequestBodyForTokenStats(request.Messages, MainReplyMaxTokens, "courier_reply_preflight");
			// Preflight replies must retain their deferred, arrival-time action commit.
			if (IsCourierBridgeEnabled() && session.DeliveryApplied)
			{
				bool detachedSubmitted = false;
				bool legacyFallbackStarted = false;
				try
				{
					PromptPackage preparedMainPrompt = LegacyPromptPackageAdapter.FromLegacyMessages(request.Messages, MainReplyMaxTokens, "legacy-courier-reply");
					InteractionEnvelope preparedEnvelope = await RunCourierOwnerPhaseAsync(runtimeGeneration,
						"reply_capture_prepared", () =>
						{
							CourierSession current = GetSessionById(request.SessionId);
							Hero currentRecipient = current == null ? null : ResolveRecipient(current);
							if (current == null || IsTerminalStage(current) || currentRecipient == null || currentRecipient.IsDead
								|| !string.Equals(SafeHeroId(currentRecipient), request.RecipientHeroId, StringComparison.Ordinal))
								throw new OperationCanceledException("Courier prepared request expired.");
							return CapturePreparedCourierReplyEnvelope(request, currentRecipient, preparedMainPrompt);
						}, CancellationToken.None).ConfigureAwait(false);
					LegacyInteractionPipelinePorts ports = CreateCourierDetachedPorts(LegacyActionTagCatalog.DefaultAllowedTagFamilies, true, 64, preparedMainPrompt);
					ILlmGateway gateway = new LegacyShoutNetworkGateway();
					using (LegacyChannelInteractionFacade facade = LegacyInteractionSnapshotAdapters.CreateCourierInteractionFacade(ports, gateway, _ => preparedEnvelope))
					{
						if (facade != null)
						{
							RuntimeConfigSnapshot configuration = CaptureCourierReplyRefactorConfigurationForExternal();
							string moduleId = LegacyInteractionSnapshotAdapters.NativeConversationModuleId;
							string providerId = configuration?.Providers?.Keys?.FirstOrDefault() ?? LegacyInteractionSnapshotAdapters.LegacyShoutNetworkProviderId;
							detachedSubmitted = true;
							DetachedInteractionHostResult hostResult = await SubmitCourierReplyRefactorOptInForExternalAsync(
								facade,
								configuration,
								moduleId,
								providerId,
								sessionId,
								session.LetterText ?? "",
								async () =>
								{
									legacyFallbackStarted = true;
									await GenerateNpcReplyAsync(request).ConfigureAwait(false);
									return session.ReplyText ?? "";
								},
								CancellationToken.None).ConfigureAwait(false);
							if (hostResult?.Status == InteractionStatus.CancelledAsStale)
							{
								return;
							}
							// The legacy callback may have only queued its main-thread completion so far.
							if (legacyFallbackStarted || hostResult?.UsedLegacyFallback == true)
							{
								return;
							}
							if (hostResult != null && (hostResult.Status == InteractionStatus.Executed || hostResult.Status == InteractionStatus.Succeeded))
							{
								EnqueueMainThreadActionForGeneration(runtimeGeneration, () =>
								{
									FinalizeCourierReplyGenerationOnMainThread(request, hostResult.VisibleReply, session.ReplyPostprocessedText, " [detached_refactor]");
								}, "reply_detached_finalized");
								return;
							}
							EnqueueMainThreadActionForGeneration(runtimeGeneration,
								() => FailDetachedCourierReplyOnMainThread(sessionId, runtimeGeneration, hostResult?.ErrorCode ?? "missing_host_result"),
								"reply_detached_stopped");
							return;
						}
					}
				}
				catch (Exception ex)
				{
					Log("detached courier reply cutover error=" + ex.Message);
					if (detachedSubmitted)
					{
						if (!legacyFallbackStarted)
						{
							EnqueueMainThreadActionForGeneration(runtimeGeneration,
								() => FailDetachedCourierReplyOnMainThread(sessionId, runtimeGeneration, "host_exception_" + ex.GetType().Name),
								"reply_detached_exception");
						}
						return;
					}
				}
			}
			await GenerateNpcReplyAsync(request).ConfigureAwait(false);
		}
		catch (PreprocessFormatException ex)
		{
			Log("background prepare reply preprocess failed session=" + sessionId + " error=" + ex.Message);
			EnqueueMainThreadActionForGeneration(runtimeGeneration, () =>
			{
				if (!IsCourierPromptRunCurrent(promptRun)) return;
				try
				{
					LlmRetryPrompt.ShowFailurePopup("信使回信前处理失败", ex.Message);
				}
				catch
				{
				}
				FailCourierReplyGenerationOnMainThread(sessionId, runtimeGeneration, "reply_preprocess_failed");
			}, "reply_preprocess_failed");
		}
		catch (Exception ex)
		{
			Log("background prepare reply failed session=" + sessionId + " error=" + ex);
			EnqueueMainThreadActionForGeneration(runtimeGeneration, () => { if (IsCourierPromptRunCurrent(promptRun)) FailCourierReplyGenerationOnMainThread(sessionId, runtimeGeneration, "reply_generation_failed"); }, "reply_prepare_failed");
		}
	}


	private async Task GenerateNpcReplyAsync(CourierReplyGenerationRequest request)
	{
		try
		{
			if (request == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_reply_api_start"))
			{
				return;
			}
			string output = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(request.Messages, MainReplyMaxTokens);
			EnqueueMainThreadActionForGeneration(request.RuntimeGeneration, () => CompleteCourierReplyGenerationOnMainThread(request, output), "reply_generated");
		}
		catch (Exception ex)
		{
			Log("generate reply failed session=" + request?.SessionId + " error=" + ex);
			if (request != null)
			{
				EnqueueMainThreadActionForGeneration(request.RuntimeGeneration, () => FailCourierReplyGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, "reply_generation_failed"), "reply_generation_failed");
			}
		}
	}

	private void CompleteCourierReplyGenerationOnMainThread(CourierReplyGenerationRequest request, string output)
	{
		if (request == null)
		{
			return;
		}
		try
		{
			if (SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_reply_response"))
			{
				return;
			}
			CourierSession session = GetSessionById(request.SessionId);
			if (session == null || IsTerminalStage(session))
			{
				return;
			}
			Hero recipient = ResolveRecipient(session);
			if (recipient == null || recipient.IsDead)
			{
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				ProcessSessionById(request.SessionId, "reply_generated_recipient_invalid");
				return;
			}
			string postprocessReply = PrepareNpcReplyForActionPostprocess(output);
			if (LooksLikeApiError(postprocessReply))
			{
				Log("llm main failed session=" + session.Id + " output=" + postprocessReply);
				if (ShowCourierReplyGenerationRetryPrompt(request, postprocessReply))
				{
					return;
				}
				postprocessReply = "";
			}
			if (string.IsNullOrWhiteSpace(postprocessReply))
			{
				session.ReplyText = "";
				session.ReplyPostprocessedText = "";
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				Log("npc no reply session=" + session.Id);
				ProcessSessionById(request.SessionId, "reply_generated_empty");
				return;
			}
			// The letter UI intentionally removes stage directions, but the action
			// postprocessor must receive the original prose as execution evidence.
			string reply = CleanNpcReply(postprocessReply);
			string extras = request.Extras ?? "";
			List<string> selectedRuleHits = request.SelectedRuleHits ?? new List<string>();
			bool duelInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "duel");
			bool rewardInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "reward");
			bool loanInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "loan");
			bool lordsHallInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "lords_hall_access");
			bool meetingReleaseInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "encounter_release_player");
			bool vanillaIssueInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "vanilla_issue");
			bool sceneMechanismInjected = false;
			bool partyTransferInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "party_transfer");
			bool voteDealInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_agenda");
			bool diplomacyInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "diplomacy");
			bool diplomacySelected = HasPreprocessRuleHit(selectedRuleHits, "diplomacy");
			diplomacyInjected = diplomacyInjected || diplomacySelected;
			bool worldMapPartyCommandInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "worldmap_party_command");
			bool kingdomServiceInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_service");
			bool heroJoinPartyInjected = kingdomServiceInjected;
			bool kingdomVassalageRuleBlockInjected = ShoutBehavior.HasInjectedRuleBlockForExternal(extras, "kingdom_vassalage");
			bool kingdomVassalageSelected = HasPreprocessRuleHit(selectedRuleHits, "kingdom_vassalage");
			bool kingdomVassalageInjected = kingdomVassalageRuleBlockInjected || kingdomVassalageSelected;
			bool kingdomAnnexationInjected = false;
			Log("postprocess setup chain=courier session=" + session.Id
				+ " selectedRuleHits=" + ((selectedRuleHits == null || selectedRuleHits.Count == 0) ? "(none)" : string.Join(",", selectedRuleHits))
				+ " kingdom_vassalage_selected=" + kingdomVassalageSelected
				+ " diplomacy_selected=" + diplomacySelected
				+ " kingdom_vassalage_block=" + kingdomVassalageRuleBlockInjected
				+ " diplomacy_block=" + diplomacyInjected
				+ " kingdomVassalageInjected=" + kingdomVassalageInjected
				+ " kingdomAnnexationInjected=" + kingdomAnnexationInjected);
			string completionLogDetails = " preprocessHits=" + ((selectedRuleHits == null || selectedRuleHits.Count == 0) ? "(none)" : string.Join(",", selectedRuleHits)) + " duel=" + duelInjected + " reward=" + rewardInjected + " loan=" + loanInjected + " kingdom=" + kingdomServiceInjected + " kingdomVassalage=" + kingdomVassalageInjected + " kingdomAnnexation=" + kingdomAnnexationInjected + " lordsHall=" + lordsHallInjected + " meetingRelease=" + meetingReleaseInjected + " vanillaIssue=" + vanillaIssueInjected + " heroJoin=" + heroJoinPartyInjected + " sceneMechanism=" + sceneMechanismInjected + " partyTransfer=" + partyTransferInjected + " voteDeal=" + voteDealInjected + " diplomacy=" + diplomacyInjected + " worldMap=" + worldMapPartyCommandInjected;
			try
			{
				if (!ShoutBehavior.TryPrepareCourierActionPostprocessForExternal(recipient, recipient.CharacterObject, recipient.Name?.ToString() ?? request.RecipientName ?? "NPC", request.LetterText, request.HistoryText, postprocessReply, duelInjected, rewardInjected, loanInjected, kingdomServiceInjected, lordsHallInjected, meetingReleaseInjected, vanillaIssueInjected, heroJoinPartyInjected, sceneMechanismInjected, partyTransferInjected, out ShoutBehavior.CourierActionPostprocessWorkItem workItem, out string immediateResult, voteDealInjected, diplomacyInjected, worldMapPartyCommandInjected, selectedRuleHits, request.EntityPostprocessContext, -1, true, true, kingdomVassalageInjected, kingdomAnnexationInjected, "courier"))
				{
					FinalizeCourierReplyGenerationOnMainThread(request, reply, immediateResult, completionLogDetails);
					return;
				}
				Task.Run(delegate
				{
					RunCourierReplyPostprocessOffMainThread(request, reply, workItem, completionLogDetails);
				});
				Log("postprocess http queued chain=courier session=" + session.Id);
				return;
			}
			catch (Exception ex)
			{
				Log("courier postprocess failed session=" + session.Id + " error=" + ex);
				FinalizeCourierReplyGenerationOnMainThread(request, reply, reply, completionLogDetails);
				return;
			}
		}
		catch (Exception ex)
		{
			Log("complete reply failed session=" + request.SessionId + " error=" + ex);
			FailCourierReplyGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, "reply_generation_failed");
		}
	}

	private void RunCourierReplyPostprocessOffMainThread(CourierReplyGenerationRequest request, string reply, ShoutBehavior.CourierActionPostprocessWorkItem workItem, string completionLogDetails)
	{
		if (request == null || workItem == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_reply_postprocess_start"))
		{
			return;
		}
		bool success = false;
		string content = "";
		string error = "";
		try
		{
			success = AIConfigHandler.TryCallAuxiliaryActionPostprocess(workItem.SystemPrompt, workItem.UserPrompt, 5000, 0f, out content, out error);
		}
		catch (Exception ex)
		{
			error = ex.ToString();
		}
		EnqueueMainThreadActionForGeneration(request.RuntimeGeneration, delegate
		{
			CompleteCourierReplyPostprocessOnMainThread(request, reply, workItem, success, content, error, completionLogDetails);
		}, "reply_postprocess_generated");
	}

	private void CompleteCourierReplyPostprocessOnMainThread(CourierReplyGenerationRequest request, string reply, ShoutBehavior.CourierActionPostprocessWorkItem workItem, bool success, string content, string error, string completionLogDetails)
	{
		if (request == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_reply_postprocess"))
		{
			return;
		}
		try
		{
			CourierSession session = GetSessionById(request.SessionId);
			if (session == null || IsTerminalStage(session))
			{
				return;
			}
			Hero recipient = ResolveRecipient(session);
			if (recipient == null || recipient.IsDead)
			{
				session.ReplyGenerated = true;
				session.ReplyGenerationStarted = false;
				ProcessSessionById(request.SessionId, "reply_generated_recipient_invalid");
				return;
			}
			string postprocessed;
			if (success)
			{
				postprocessed = workItem.CompleteOnMainThread(content);
			}
			else
			{
				Logger.Log("CourierDelivery", "[UnifiedPostprocess] 调用失败: " + error);
				postprocessed = workItem.FallbackText;
			}
			FinalizeCourierReplyGenerationOnMainThread(request, reply, postprocessed, completionLogDetails);
		}
		catch (Exception ex)
		{
			Log("complete courier postprocess failed session=" + request.SessionId + " error=" + ex);
			FailCourierReplyGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, "reply_postprocess_failed");
		}
	}

	private void FinalizeCourierReplyGenerationOnMainThread(CourierReplyGenerationRequest request, string reply, string postprocessed, string completionLogDetails)
	{
		if (request == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_reply_finalize"))
		{
			return;
		}
		CourierSession session = GetSessionById(request.SessionId);
		if (session == null || IsTerminalStage(session))
		{
			return;
		}
		string replyText = reply ?? "";
		session.ReplyText = replyText;
		session.ReplyPostprocessedText = string.IsNullOrWhiteSpace(postprocessed) ? replyText : postprocessed;
		session.ReplyGenerated = true;
		session.ReplyGenerationStarted = false;
		Log("llm main done session=" + session.Id + " replyLen=" + replyText.Length + " postLen=" + (session.ReplyPostprocessedText ?? "").Length + (completionLogDetails ?? ""));
		ProcessSessionById(request.SessionId, "reply_generated");
	}

	private bool ShowCourierReplyGenerationRetryPrompt(CourierReplyGenerationRequest request, string error)
	{
		if (request == null || !LlmRetryPrompt.IsRetryableLlmError(error))
		{
			return false;
		}
		try
		{
			CourierSession session = GetSessionById(request.SessionId);
			if (session == null || IsTerminalStage(session))
			{
				return false;
			}
			// 此窗口仅用于选择重试或放弃；完整接口错误改由左下角消息和日志承载。
			NonBlockingErrorReport.Show("信使回信生成失败", "信使回信正文生成失败：\n\n" + (error ?? "未知错误"));
			InformationManager.ShowInquiry(new InquiryData(
				"信使回信生成失败",
				LlmRetryPrompt.BuildRetryDescription("信使回信正文生成", error),
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				"重试",
				"放弃",
				delegate
				{
					CourierSession retrySession = GetSessionById(request.SessionId);
					if (retrySession == null || IsTerminalStage(retrySession))
					{
						return;
					}
					retrySession.ReplyGenerated = false;
					retrySession.ReplyGenerationStarted = true;
					Log("retry courier reply generation session=" + request.SessionId);
					_ = Task.Run(() => GenerateNpcReplyAsync(request));
				},
				delegate
				{
					FailCourierReplyGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, "reply_generation_abandoned_after_api_error");
				}),
				pauseGameActiveState: true,
				prioritize: true);
			return true;
		}
		catch (Exception ex)
		{
			Log("show courier reply retry prompt failed session=" + request?.SessionId + " error=" + ex.Message);
			return false;
		}
	}

	private void FailDetachedCourierReplyOnMainThread(string sessionId, long runtimeGeneration, string errorCode)
	{
		if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_detached_reply_stop"))
		{
			return;
		}
		CourierSession session = GetSessionById(sessionId);
		if (session == null || IsTerminalStage(session) || session.ReplyGenerated)
		{
			return;
		}
		// Failure advances the existing session state machine. Seal unconfirmed tags
		// first so neither arrival nor return can retry a partial/unknown action.
		session.PostprocessConsumed = true;
		session.ReplyText = string.Empty;
		session.ReplyPostprocessedText = string.Empty;
		Log("detached courier reply stopped session=" + sessionId + " error=" + (errorCode ?? ""));
		FailCourierReplyGenerationOnMainThread(sessionId, runtimeGeneration, "reply_detached_stopped");
		NonBlockingErrorReport.Show("信使回信已停止", "本次回信未能安全完成，系统不会自动重试可能已生效的动作。请查看日志并核对实际结果。");
	}

	private void FailCourierReplyGenerationOnMainThread(string sessionId, long runtimeGeneration, string reason)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_reply_fail"))
			{
				return;
			}
			CourierSession session = GetSessionById(sessionId);
			if (session == null || IsTerminalStage(session))
			{
				return;
			}
			session.ReplyGenerated = true;
			session.ReplyGenerationStarted = false;
			ProcessSessionById(sessionId, string.IsNullOrWhiteSpace(reason) ? "reply_generation_failed" : reason);
		}
		catch (Exception ex)
		{
			Log("fail reply cleanup failed session=" + sessionId + " error=" + ex);
		}
	}

	private void BeginInboundLetterGenerationOnMainThread(string sessionId, long runtimeGeneration, CourierPromptRun promptRun)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_inbound_prepare_start"))
			{
				return;
			}
			if (!IsCourierPromptRunCurrent(promptRun)) return;
			_ = Task.Run(() => PrepareAndGenerateInboundLetterOffMainThreadAsync(sessionId, runtimeGeneration, promptRun));
		}
		catch (Exception ex)
		{
			Log("queue background inbound letter prepare failed session=" + sessionId + " error=" + ex);
			if (IsCourierPromptRunCurrent(promptRun)) FailInboundLetterGenerationOnMainThread(sessionId, runtimeGeneration, null, "inbound_letter_generation_failed");
		}
	}

	private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync(string sessionId, long runtimeGeneration, CourierPromptRun promptRun)
	{
		string fallbackLetter = null;
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_inbound_prepare_background_start"))
			{
				return;
			}
			CourierPreparationAdmission admission = await RunCourierOwnerPhaseAsync(runtimeGeneration,
				"courier_inbound_admission", () => IsCourierPromptRunCurrent(promptRun) ? CaptureCourierPreparationAdmission(sessionId, true, runtimeGeneration) : null, CancellationToken.None).ConfigureAwait(false);
			if (admission == null) return;
			CourierSession session = admission.Session;
			Hero sender = admission.Participant;
			fallbackLetter = admission.FallbackLetter;
			if (!await EnsureCourierPersonaContextReadyAsync(sender, "inbound", sessionId, session, runtimeGeneration).ConfigureAwait(false)) return;
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_inbound_persona_ready"))
			{
				return;
			}
			CourierPreparedHistory preparedHistory = await PrepareCourierHistoryAsync(sessionId, session, sender, true, runtimeGeneration).ConfigureAwait(false);
			if (preparedHistory == null) return;
			InboundLetterGenerationRequest request = await PrepareCourierPromptRequestAsync(sessionId, session, sender, true, fallbackLetter, runtimeGeneration, preparedHistory, promptRun, BuildInboundRequestFromPreparedPrompt).ConfigureAwait(false);
			if (request == null) return;
			ShoutNetwork.RecordPrimaryRequestBodyForTokenStats(request.Messages, MainReplyMaxTokens, "courier_inbound_letter_preflight");
			await GenerateInboundNpcLetterAsync(request).ConfigureAwait(false);
		}
		catch (PreprocessFormatException ex)
		{
			Log("background prepare inbound letter preprocess failed session=" + sessionId + " error=" + ex.Message);
			EnqueueMainThreadActionForGeneration(runtimeGeneration, () =>
			{
				if (!IsCourierPromptRunCurrent(promptRun)) return;
				try
				{
					LlmRetryPrompt.ShowFailurePopup("信使来信前处理失败", ex.Message);
				}
				catch
				{
				}
				FailInboundLetterGenerationOnMainThread(sessionId, runtimeGeneration, fallbackLetter, "inbound_letter_preprocess_failed");
			}, "inbound_letter_preprocess_failed");
		}
		catch (Exception ex)
		{
			Log("background prepare inbound letter failed session=" + sessionId + " error=" + ex);
			EnqueueMainThreadActionForGeneration(runtimeGeneration, () => { if (IsCourierPromptRunCurrent(promptRun)) FailInboundLetterGenerationOnMainThread(sessionId, runtimeGeneration, fallbackLetter, "inbound_letter_generation_failed"); }, "inbound_letter_prepare_failed");
		}
	}


	private async Task GenerateInboundNpcLetterAsync(InboundLetterGenerationRequest request)
	{
		try
		{
			if (request == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_inbound_api_start"))
			{
				return;
			}
			string output = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(request.Messages, MainReplyMaxTokens);
			EnqueueMainThreadActionForGeneration(request.RuntimeGeneration, () => CompleteInboundLetterGenerationOnMainThread(request, output), "inbound_letter_generated");
		}
		catch (Exception ex)
		{
			Log("generate inbound letter failed session=" + request?.SessionId + " error=" + ex);
			if (request != null)
			{
				EnqueueMainThreadActionForGeneration(request.RuntimeGeneration, () => FailInboundLetterGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, request.FallbackLetter, "inbound_letter_generation_failed"), "inbound_letter_generation_failed");
			}
		}
	}

	private void CompleteInboundLetterGenerationOnMainThread(InboundLetterGenerationRequest request, string output)
	{
		if (request == null)
		{
			return;
		}
		try
		{
			if (SaveRuntimeGuard.IsStale(request.RuntimeGeneration, "courier_inbound_response"))
			{
				return;
			}
			CourierSession session = GetSessionById(request.SessionId);
			if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session))
			{
				return;
			}
			Hero sender = ResolveSender(session);
			string fallbackLetter = string.IsNullOrWhiteSpace(request.FallbackLetter) ? NormalizeInboundLetterText(session.LetterText, session, sender) : request.FallbackLetter;
			string letter = NormalizeInboundLetterText(output, session, sender);
			if (LooksLikeApiError(letter))
			{
				Log("inbound letter llm failed session=" + session.Id + " output=" + letter);
				if (ShowInboundLetterGenerationRetryPrompt(request, letter, fallbackLetter))
				{
					return;
				}
				letter = fallbackLetter;
			}
			if (string.IsNullOrWhiteSpace(letter))
			{
				letter = fallbackLetter;
			}
			session.LetterText = letter;
			session.ReplyGenerated = true;
			session.ReplyGenerationStarted = false;
			Log("inbound letter llm done session=" + session.Id + " letterLen=" + letter.Length + " preprocessHits=" + ((request.SelectedRuleHits == null || request.SelectedRuleHits.Count == 0) ? "(none)" : string.Join(",", request.SelectedRuleHits)));
			ProcessSessionById(request.SessionId, "inbound_letter_generated");
		}
		catch (Exception ex)
		{
			Log("complete inbound letter failed session=" + request.SessionId + " error=" + ex);
			FailInboundLetterGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, request.FallbackLetter, "inbound_letter_generation_failed");
		}
	}

	private bool ShowInboundLetterGenerationRetryPrompt(InboundLetterGenerationRequest request, string error, string fallbackLetter)
	{
		if (request == null || !LlmRetryPrompt.IsRetryableLlmError(error))
		{
			return false;
		}
		try
		{
			CourierSession session = GetSessionById(request.SessionId);
			if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session))
			{
				return false;
			}
			// 此窗口仅用于选择重试或放弃；完整接口错误改由左下角消息和日志承载。
			NonBlockingErrorReport.Show("信使来信生成失败", "信使来信正文生成失败：\n\n" + (error ?? "未知错误"));
			InformationManager.ShowInquiry(new InquiryData(
				"信使来信生成失败",
				LlmRetryPrompt.BuildRetryDescription("信使来信正文生成", error),
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				"重试",
				"放弃",
				delegate
				{
					CourierSession retrySession = GetSessionById(request.SessionId);
					if (retrySession == null || IsTerminalStage(retrySession) || !IsInboundToPlayer(retrySession))
					{
						return;
					}
					retrySession.ReplyGenerated = false;
					retrySession.ReplyGenerationStarted = true;
					Log("retry inbound letter generation session=" + request.SessionId);
					_ = Task.Run(() => GenerateInboundNpcLetterAsync(request));
				},
				delegate
				{
					FailInboundLetterGenerationOnMainThread(request.SessionId, request.RuntimeGeneration, fallbackLetter, "inbound_letter_generation_abandoned_after_api_error");
				}),
				pauseGameActiveState: true,
				prioritize: true);
			return true;
		}
		catch (Exception ex)
		{
			Log("show inbound retry prompt failed session=" + request?.SessionId + " error=" + ex.Message);
			return false;
		}
	}

	private void FailInboundLetterGenerationOnMainThread(string sessionId, long runtimeGeneration, string fallbackLetter, string reason)
	{
		try
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "courier_inbound_fail"))
			{
				return;
			}
			CourierSession session = GetSessionById(sessionId);
			if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session))
			{
				return;
			}
			Hero sender = ResolveSender(session);
			session.LetterText = NormalizeInboundLetterText(string.IsNullOrWhiteSpace(fallbackLetter) ? session.LetterText : fallbackLetter, session, sender);
			session.ReplyGenerated = true;
			session.ReplyGenerationStarted = false;
			ProcessSessionById(sessionId, string.IsNullOrWhiteSpace(reason) ? "inbound_letter_generation_failed" : reason);
		}
		catch (Exception ex)
		{
			Log("fail inbound cleanup failed session=" + sessionId + " error=" + ex);
		}
	}

	private void ProcessSessionById(string sessionId, string reason)
	{
		try
		{
			CourierSession session = null;
			lock (_sessionLock)
			{
				_sessions.TryGetValue(sessionId ?? "", out session);
			}
			if (session == null || IsTerminalStage(session))
			{
				return;
			}
			Log("process session by id session=" + session.Id + " reason=" + (reason ?? ""));
			ProcessSession(session);
		}
		catch (Exception ex)
		{
			Log("process session by id failed session=" + (sessionId ?? "") + " error=" + ex);
		}
	}
}
