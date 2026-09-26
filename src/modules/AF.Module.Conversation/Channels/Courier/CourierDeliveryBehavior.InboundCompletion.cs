using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
    private sealed class CourierInboundCompletionMemory : IInteractionMemory, IInteractionMemoryBatchCommitter
    {
        private readonly CourierDeliveryBehavior _owner;
        private readonly string _sessionId;
        private readonly IInteractionMemory _inner;
        private readonly IInteractionMemoryBatchCommitter _batch;

        internal CourierInboundCompletionMemory(
            CourierDeliveryBehavior owner,
            string sessionId,
            IInteractionMemory inner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _sessionId = (sessionId ?? string.Empty).Trim();
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _batch = inner as IInteractionMemoryBatchCommitter;
        }

        public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems)
            => _inner.Read(subjectId, maxItems);

        public void Append(
            string subjectId,
            PromptMessage message,
            IEnumerable<FactRecord> confirmedFacts)
            => _inner.Append(subjectId, message, confirmedFacts);

        public MemoryCommitResult Commit(InteractionMemoryCommit commit)
        {
            if (_batch == null)
            {
                return new MemoryCommitResult(
                    MemoryCommitStatus.Rejected,
                    "courier_inbound_batch_memory_required");
            }
            return _owner.CommitCourierInboundMemoryWithCompletionReceipt(
                _sessionId,
                commit,
                _batch);
        }
    }

	private void CompleteCourierInboundDetachedCommit(
		InteractionEnvelope envelope,
		InteractionResult result,
		InteractionCommitResult commit)
	{
		try
		{
			string sessionId = envelope?.Snapshot?.Identity?.SessionId;
			CourierSession session = GetSessionById(sessionId);
			if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session) || !commit.HistoryWritten)
			{
				return;
			}
			if (!CourierInboundCompletionReceipt.TryDeserialize(
				session.InboundCompletionReceipt,
				out CourierInboundCompletionReceipt receipt,
				out string errorCode))
			{
				Log("detached inbound completion receipt missing session=" + (sessionId ?? "")
					+ " error=" + (errorCode ?? ""));
				return;
			}
			string subjectId = envelope?.Snapshot?.Identity?.SubjectId;
			Hero sender = ResolveSender(session);
			string visibleLetter = NormalizeInboundLetterText(result?.VisibleReply, session, sender);
			if (string.IsNullOrWhiteSpace(visibleLetter))
			{
				visibleLetter = NormalizeInboundLetterText(
					session.InboundFallbackLetter ?? session.LetterText,
					session,
					sender);
			}
			if (!string.Equals(receipt.SessionId, sessionId, StringComparison.Ordinal)
				|| !string.Equals(receipt.SenderHeroId, subjectId, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(receipt.Letter, visibleLetter, StringComparison.Ordinal))
			{
				receipt.Quarantine("courier_inbound_after_commit_payload_mismatch");
				session.InboundCompletionReceipt = receipt.Serialize();
				return;
			}
			TryCompleteCourierInboundCompletionReceipt(
				session,
				receipt,
				memoryConfirmed: true,
				"detached_inbound_letter_generated");
		}
		catch (Exception ex)
		{
			Log("detached inbound letter commit failed error=" + ex.Message);
		}
	}

	private void ProcessOneCourierInboundCompletionReceipt()
	{
		List<CourierSession> candidates;
		lock (_sessionLock)
		{
			candidates = _sessions.Values
				.Where(candidate => candidate != null
					&& IsInboundToPlayer(candidate)
					&& !IsTerminalStage(candidate)
					&& !string.IsNullOrWhiteSpace(candidate.InboundCompletionReceipt)
					&& (!candidate.ReplyGenerated || candidate.ReplyGenerationStarted))
				.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
				.ToList();
		}
		if (candidates.Count == 0)
		{
			_courierInboundCompletionScanCursor = string.Empty;
			return;
		}
		int startIndex = candidates.FindIndex(candidate => string.Compare(
			candidate.Id,
			_courierInboundCompletionScanCursor,
			StringComparison.Ordinal) > 0);
		if (startIndex < 0)
		{
			startIndex = 0;
		}
		CourierSession session = candidates[startIndex];
		_courierInboundCompletionScanCursor = session.Id;
		if (!CourierInboundCompletionReceipt.TryDeserialize(
			session.InboundCompletionReceipt,
			out CourierInboundCompletionReceipt receipt,
			out string errorCode))
		{
			Log("courier inbound completion receipt invalid session=" + session.Id
				+ " error=" + (errorCode ?? ""));
			AbortCourierInboundCompletion(session, "invalid_receipt");
			return;
		}
		if (receipt.Lifecycle == CourierInboundCompletionLifecycle.Quarantined)
		{
			AbortCourierInboundCompletion(
				session,
				string.IsNullOrWhiteSpace(receipt.DiagnosticCode)
					? "quarantined_receipt"
					: receipt.DiagnosticCode);
			return;
		}
		TryCompleteCourierInboundCompletionReceipt(
			session,
			receipt,
			memoryConfirmed: false,
			"memory_recovery_completed");
	}

	private void TryCompleteCourierInboundCompletionReceipt(
		CourierSession session,
		CourierInboundCompletionReceipt receipt,
		bool memoryConfirmed,
		string reason)
	{
		if (session == null || receipt == null
			|| receipt.Lifecycle == CourierInboundCompletionLifecycle.Quarantined)
		{
			return;
		}
		if (!receipt.Matches(
			session.Id,
			session.SenderHeroId,
			session.RecipientHeroId,
			session.CourierPartyId,
			receipt.RecoveryId,
			receipt.MemoryPayloadHash)
			|| !IsInboundToPlayer(session)
			|| IsTerminalStage(session)
			|| session.DeliveryApplied
			|| HasCurrentInboundPlayerRecipientMismatch(session))
		{
			receipt.Quarantine("courier_inbound_completion_session_mismatch");
			session.InboundCompletionReceipt = receipt.Serialize();
			return;
		}
		if (!memoryConfirmed
			&& receipt.Lifecycle != CourierInboundCompletionLifecycle.Ready
			&& receipt.Lifecycle != CourierInboundCompletionLifecycle.Applied)
		{
			InteractionMemoryRecoveryLookupStatus status =
				MyBehavior.GetExternalDialogueHistoryRecoveryStatus(
					receipt.RecoveryId,
					receipt.SenderHeroId,
					receipt.MemoryPayloadHash);
			if (status == InteractionMemoryRecoveryLookupStatus.Completed)
			{
				receipt.MarkReady(DateTime.UtcNow.Ticks);
				session.InboundCompletionReceipt = receipt.Serialize();
			}
			else if (status == InteractionMemoryRecoveryLookupStatus.Missing
				|| status == InteractionMemoryRecoveryLookupStatus.Quarantined
				|| status == InteractionMemoryRecoveryLookupStatus.Disabled
				|| status == InteractionMemoryRecoveryLookupStatus.SubjectMismatch
				|| status == InteractionMemoryRecoveryLookupStatus.PayloadMismatch
				|| status == InteractionMemoryRecoveryLookupStatus.Invalid)
			{
				receipt.Quarantine("memory_" + status.ToString().ToLowerInvariant());
				session.InboundCompletionReceipt = receipt.Serialize();
				return;
			}
			else
			{
				return;
			}
		}
		else if (memoryConfirmed)
		{
			receipt.MarkReady(DateTime.UtcNow.Ticks);
			session.InboundCompletionReceipt = receipt.Serialize();
		}

		if (string.IsNullOrWhiteSpace(receipt.Letter))
		{
			receipt.Quarantine("courier_inbound_completion_letter_missing");
			session.InboundCompletionReceipt = receipt.Serialize();
			return;
		}
		if (session.ReplyGenerated)
		{
			// Ready/Applied is the durable authority. Repair any stale session
			// projection from the frozen visible letter before delivery can resume.
			receipt.MarkApplied(DateTime.UtcNow.Ticks);
			session.InboundCompletionReceipt = receipt.Serialize();
			session.LetterText = receipt.Letter;
			session.ReplyGenerationStarted = false;
			return;
		}
		// Publish the applied tombstone before mutating the session fields. If a
		// later owner step fails, load/tick can still idempotently restore them.
		receipt.MarkApplied(DateTime.UtcNow.Ticks);
		session.InboundCompletionReceipt = receipt.Serialize();
		session.LetterText = receipt.Letter;
		session.ReplyGenerated = true;
		session.ReplyGenerationStarted = false;
		Log("courier inbound completion applied session=" + session.Id
			+ " recovery=" + receipt.RecoveryId.Substring(0, 12)
			+ " letterLen=" + receipt.Letter.Length);
		ProcessSessionById(session.Id, reason);
	}

	private static bool IsCourierInboundCompletionReadyForDelivery(
		CourierSession session)
	{
		if (session == null || string.IsNullOrWhiteSpace(session.InboundCompletionReceipt))
		{
			return true;
		}
		if (!CourierInboundCompletionReceipt.TryDeserialize(
			session.InboundCompletionReceipt,
			out CourierInboundCompletionReceipt receipt,
			out _))
		{
			return false;
		}
		return receipt.Lifecycle == CourierInboundCompletionLifecycle.Applied
			&& receipt.Matches(
				session.Id,
				session.SenderHeroId,
				session.RecipientHeroId,
				session.CourierPartyId,
				receipt.RecoveryId,
				receipt.MemoryPayloadHash)
			&& !HasCurrentInboundPlayerRecipientMismatch(session)
			&& session.ReplyGenerated
			&& !session.ReplyGenerationStarted
			&& string.Equals(
				session.LetterText ?? string.Empty,
				receipt.Letter,
				StringComparison.Ordinal);
	}

	private void AbortCourierInboundCompletion(CourierSession session, string reason)
	{
		if (session == null || IsTerminalStage(session))
		{
			return;
		}
		string normalizedReason = string.IsNullOrWhiteSpace(reason)
			? "courier_inbound_completion_failed"
			: reason.Replace("\r", " ").Replace("\n", " ").Trim();
		MobileParty courier = ResolveCourierParty(session);
		session.Stage = CourierStage.Destroyed.ToString();
		session.ReplyGenerationStarted = false;
		session.ReplyWaitPopupShown = false;
		EndCourierReplyWaitPause(session, "inbound_completion_failed");
		lock (_sessionLock)
		{
			_sessions.Remove(session.Id);
		}
		RemoveCourierRuntimeIndex(session);
		try
		{
			if (courier != null && courier.IsActive)
			{
				UntrackCourierMapVisual(courier, "inbound_completion_failed");
				DestroyCourierTemporaryShips(session, courier, "inbound_completion_failed");
				if (courier.IsCurrentlyUsedByAQuest)
				{
					courier.SetPartyUsedByQuest(false);
				}
				DestroyPartyAction.Apply(null, courier);
			}
		}
		catch (Exception ex)
		{
			Log("abort inbound completion courier destroy failed session=" + session.Id
				+ " error=" + ex.Message);
		}
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"信使来信处理失败，信使已安全终止，未交付信件。",
				Colors.Red));
		}
		catch
		{
		}
		Log("courier inbound completion aborted session=" + session.Id
			+ " reason=" + normalizedReason);
	}

	private static IInteractionMemory CreateCourierInboundMemoryFacadeForExternal(
		InteractionEnvelope envelope,
		string expectedSessionId)
	{
		CourierDeliveryBehavior owner = Instance;
		GameInteractionSnapshot snapshot = envelope?.Snapshot;
		string sessionId = snapshot?.Identity?.SessionId;
		if (owner == null || string.IsNullOrWhiteSpace(sessionId)
			|| !string.Equals(
				sessionId,
				(expectedSessionId ?? string.Empty).Trim(),
				StringComparison.Ordinal)
			|| snapshot.Identity.Channel != InteractionChannel.Courier
			|| !snapshot.DetachedFacts.TryGetValue("courier_direction", out string direction)
			|| !string.Equals(direction, "inbound_letter", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		CourierSession session = owner.GetSessionById(sessionId);
		if (session == null || IsTerminalStage(session) || !IsInboundToPlayer(session)
			|| session.DeliveryApplied || session.ReplyGenerated
			|| !session.ReplyGenerationStarted
			|| HasCurrentInboundPlayerRecipientMismatch(session)
			|| !string.Equals(
				session.SenderHeroId,
				snapshot.Identity.SubjectId,
				StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		IInteractionMemory inner = CreateCourierMemoryFacadeForExternal(envelope);
		return inner == null
			? null
			: new CourierInboundCompletionMemory(owner, sessionId, inner);
	}

	private MemoryCommitResult CommitCourierInboundMemoryWithCompletionReceipt(
		string expectedSessionId,
		InteractionMemoryCommit commit,
		IInteractionMemoryBatchCommitter memory)
	{
		if (commit == null || memory == null)
		{
			return new MemoryCommitResult(
				MemoryCommitStatus.Rejected,
				"courier_inbound_completion_dependencies_missing");
		}
		try
		{
			if (!TWParallel.IsMainThread())
			{
				return new MemoryCommitResult(
					MemoryCommitStatus.Rejected,
					"courier_inbound_completion_not_main_thread");
			}
			CourierSession session = GetSessionById(expectedSessionId);
			if (!TryValidateCourierInboundMemoryCommit(session, expectedSessionId, commit, out Hero sender, out string errorCode))
			{
				AbortCourierInboundCompletionBeforeMemory(session, errorCode);
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, errorCode);
			}
			string letter = NormalizeInboundLetterText(commit.AssistantText, session, sender);
			if (string.IsNullOrWhiteSpace(letter))
			{
				letter = NormalizeInboundLetterText(
					session.InboundFallbackLetter ?? session.LetterText,
					session,
					sender);
			}
			if (string.IsNullOrWhiteSpace(letter)
				|| !MyBehavior.TryPrepareExternalDialogueHistoryRecoveryIdentity(
					commit,
					isNonHero: false,
					npcName: null,
					out string recoveryId,
					out string memoryPayloadHash,
					out errorCode)
				|| !CourierInboundCompletionReceipt.TryCreate(
					session.Id,
					session.SenderHeroId,
					session.RecipientHeroId,
					session.CourierPartyId,
					recoveryId,
					memoryPayloadHash,
					letter,
					DateTime.UtcNow.Ticks,
					out CourierInboundCompletionReceipt candidate,
					out errorCode))
			{
				AbortCourierInboundCompletionBeforeMemory(
					session,
					string.IsNullOrWhiteSpace(errorCode)
						? "courier_inbound_completion_intent_invalid"
						: errorCode);
				return new MemoryCommitResult(
					MemoryCommitStatus.Rejected,
					string.IsNullOrWhiteSpace(errorCode)
						? "courier_inbound_completion_intent_invalid"
						: errorCode);
			}

			CourierInboundCompletionReceipt receipt = candidate;
			if (!string.IsNullOrWhiteSpace(session.InboundCompletionReceipt))
			{
				if (!CourierInboundCompletionReceipt.TryDeserialize(
					session.InboundCompletionReceipt,
					out receipt,
					out errorCode))
				{
					AbortCourierInboundCompletionBeforeMemory(
						session,
						string.IsNullOrWhiteSpace(errorCode)
							? "courier_inbound_completion_receipt_invalid"
							: errorCode);
					return new MemoryCommitResult(
						MemoryCommitStatus.Rejected,
						string.IsNullOrWhiteSpace(errorCode)
							? "courier_inbound_completion_receipt_invalid"
							: errorCode);
				}
				if (!receipt.HasSamePayload(candidate)
					|| receipt.Lifecycle == CourierInboundCompletionLifecycle.Quarantined)
				{
					AbortCourierInboundCompletionBeforeMemory(
						session,
						"courier_inbound_completion_payload_conflict");
					return new MemoryCommitResult(
						MemoryCommitStatus.Rejected,
						"courier_inbound_completion_payload_conflict");
				}
			}

			session.ReplyGenerationStarted = true;
			return CourierInboundCompletionCommitCoordinator.Commit(
				receipt,
				wire => session.InboundCompletionReceipt = wire,
				() => memory.Commit(commit),
				() => MyBehavior.GetExternalDialogueHistoryRecoveryStatus(
					receipt.RecoveryId,
					receipt.SenderHeroId,
					receipt.MemoryPayloadHash),
				DateTime.UtcNow.Ticks);
		}
		catch (Exception ex)
		{
			Log("courier inbound completion memory wrapper failed session="
				+ (expectedSessionId ?? "") + " error=" + ex.Message);
			return new MemoryCommitResult(
				MemoryCommitStatus.Failed,
				"courier_inbound_completion_wrapper_failed");
		}
	}

	private void AbortCourierInboundCompletionBeforeMemory(
		CourierSession session,
		string reason)
	{
		if (session != null && IsInboundToPlayer(session) && !IsTerminalStage(session)
			&& !session.DeliveryApplied && !session.ReplyGenerated)
		{
			AbortCourierInboundCompletion(session, reason);
		}
	}

	private bool TryValidateCourierInboundMemoryCommit(
		CourierSession session,
		string expectedSessionId,
		InteractionMemoryCommit commit,
		out Hero sender,
		out string errorCode)
	{
		sender = null;
		errorCode = string.Empty;
		if (session == null || IsTerminalStage(session))
		{
			errorCode = "courier_inbound_session_unavailable";
			return false;
		}
		if (!IsInboundToPlayer(session)
			|| session.DeliveryApplied
			|| session.ReplyGenerated
			|| !session.ReplyGenerationStarted
			|| HasCurrentInboundPlayerRecipientMismatch(session))
		{
			errorCode = "courier_inbound_session_not_committable";
			return false;
		}
		if (commit.Channel != InteractionChannel.Courier
			|| !string.Equals(session.Id, (expectedSessionId ?? string.Empty).Trim(), StringComparison.Ordinal)
			|| !string.Equals(session.Id, commit.SessionId, StringComparison.Ordinal)
			|| !string.Equals(session.SenderHeroId, commit.SubjectId, StringComparison.OrdinalIgnoreCase)
			|| !string.IsNullOrWhiteSpace(commit.UserText))
		{
			errorCode = "courier_inbound_commit_identity_mismatch";
			return false;
		}
		sender = ResolveSender(session);
		if (sender == null || sender.IsDead)
		{
			errorCode = "courier_inbound_sender_unavailable";
			return false;
		}
		return true;
	}

	private static bool HasCurrentInboundPlayerRecipientMismatch(CourierSession session)
	{
		if (Campaign.Current == null)
		{
			return false;
		}
		string playerId = SafeHeroId(Hero.MainHero);
		return string.IsNullOrWhiteSpace(playerId)
			|| !string.Equals(
				session?.RecipientHeroId,
				playerId,
				StringComparison.OrdinalIgnoreCase);
	}
}
