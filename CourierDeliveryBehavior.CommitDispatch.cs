using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

// Final authoritative commit remains owned by the original Courier session. This boundary
// only schedules it, tracks retirement, and preserves the receipt of an already claimed commit.
public partial class CourierDeliveryBehavior
{
    /// <summary>
    /// Default Courier compatibility boundary. Generation may prepare text,
    /// but this method is still called only by the delivery/session owner at
    /// the recipient. The shared action boundary therefore cannot move game
    /// effects into pre-generation or make an old retry affect a new session.
    /// </summary>
    private void CommitGeneratedReplyAtRecipient(
        CourierSession session,
        Hero recipient,
        bool persistHistory = true)
    {
        if (session == null || session.PostprocessConsumed || !session.DeliveryApplied)
        {
            return;
        }
        string raw = session.ReplyPostprocessedText ?? session.ReplyText ?? string.Empty;
        if (recipient == null || recipient.IsDead)
        {
            CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory);
            return;
        }

        var actionCommitter = new LegacyChannelActionCommitter();
        LegacyChannelActionCommitResult prepared = actionCommitter.Prepare(raw);
        if (!prepared.HasActions)
        {
            if (prepared.Execution.Status == InteractionStatus.Succeeded)
            {
                CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory);
                return;
            }
            RejectCourierActionPlan(session, recipient, raw, persistHistory, prepared.Execution.ErrorCode);
            return;
        }

        InteractionEnvelope envelope;
        try
        {
            envelope = LegacyInteractionSnapshotAdapters.CaptureCourier(
                recipient,
                session.LetterText ?? string.Empty,
                session.Id,
                string.Empty);
        }
        catch (Exception ex)
        {
            Log("courier action snapshot rejected session=" + session.Id + " error=" + ex.GetType().Name);
            RejectCourierActionPlan(session, recipient, raw, persistHistory, "courier.action_snapshot_invalid");
            return;
        }
        if (envelope?.Snapshot?.Identity == null)
        {
            RejectCourierActionPlan(session, recipient, raw, persistHistory, "courier.action_snapshot_missing");
            return;
        }

        bool ownerInvoked = false;
        var executor = new LegacyChannelActionPlanExecutor(
            envelope.Snapshot.Identity.Channel,
            envelope.Snapshot.Identity.SessionId,
            envelope.Snapshot.Identity.SubjectId,
            (plan, snapshot) =>
            {
                ownerInvoked = true;
                session.ReplyPostprocessedText = plan.RawPostprocessId;
                CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory);
                if (!session.PostprocessConsumed)
                {
                    return InteractionStatus.RejectedByValidation;
                }
                LegacyChannelActionCommitResult remaining = actionCommitter.Prepare(
                    session.ReplyPostprocessedText);
                return remaining.HasActions
                    || remaining.Execution.Status != InteractionStatus.Succeeded
                    ? InteractionStatus.NonRetryableFailure
                    : InteractionStatus.Executed;
            });
        LegacyChannelActionCommitResult committed = actionCommitter.Commit(
            prepared.ActionPlan,
            envelope.Snapshot,
            executor);
        if (!ownerInvoked)
        {
            RejectCourierActionPlan(session, recipient, raw, persistHistory, committed.Execution.ErrorCode);
            return;
        }
        Log("courier action commit session=" + session.Id
            + " status=" + committed.Execution.Status
            + " effect=" + committed.Execution.EffectState);
    }

    private void RejectCourierActionPlan(
        CourierSession session,
        Hero recipient,
        string raw,
        bool persistHistory,
        string errorCode)
    {
        string visible = LegacyActionTagParser.RemoveProtocolTags(raw ?? string.Empty, _ => true).Trim();
        session.ReplyPostprocessedText = visible;
        session.PostprocessConsumed = true;
        if (persistHistory && recipient != null && !recipient.IsDead)
        {
            PersistCourierReplyToHistories(session, recipient, visible);
        }
        Log("courier action plan rejected without retry session=" + session.Id
            + " error=" + (errorCode ?? "action_not_executed"));
    }

    // Entering a commit is not proof of either full success or full rollback. Preserve
    // uncertainty on an exception/missing receipt; callers must not replay the whole turn.
    private static InteractionCommitResult CreateUnconfirmedCourierCommit(string errorCode)
    {
        return new InteractionCommitResult(InteractionStatus.NonRetryableFailure, false, false,
            errorCode, ActionExecutionEffectState.UnknownAfterStart);
    }

	private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync(
		Func<InteractionCommitResult> commit,
		string targetLog,
		string sessionId,
		bool abortInboundWithoutReceipt = false)
	{
		long retirementVersion = _pendingOwnerPhases.Version;
		if (!_pendingOwnerPhases.Accepting)
			return Task.FromResult(new InteractionCommitResult(InteractionStatus.RejectedByValidation, false, false, "courier_owner_retired"));
		if (commit == null)
		{
			return Task.FromResult(new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"missing_commit"));
		}
		bool isMainThread = false;
		try
		{
			isMainThread = TWParallel.IsMainThread();
		}
		catch
		{
		}
		if (isMainThread)
		{
			return Task.FromResult(InvokeCourierRefactorCommit(
				commit,
				targetLog,
				sessionId,
				abortInboundWithoutReceipt));
		}
		TaskCompletionSource<InteractionCommitResult> completion = new TaskCompletionSource<InteractionCommitResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		int state = 0; // Unclaimed=0, executing=1, retired before execution=2.
		bool Retire(string reason)
		{
			if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
			completion.TrySetResult(new InteractionCommitResult(InteractionStatus.RejectedByValidation, false, false, reason));
			return true;
		}
		IDisposable registration = _pendingOwnerPhases.Register(retirementVersion, () => Retire("courier_owner_retired"));
		if (registration == null) return completion.Task;
		try
		{
			MainThreadActions.Enqueue(() =>
			{
				if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
				{
					return;
				}
				try
				{
					completion.TrySetResult(InvokeCourierRefactorCommit(
						commit,
						targetLog,
						sessionId,
						abortInboundWithoutReceipt));
				}
				catch (Exception ex)
				{
					try { Log("detached courier commit failed session=" + (sessionId ?? "") + " target=" + (targetLog ?? "") + " error=" + ex.Message); }
					catch { }
					completion.TrySetResult(CreateUnconfirmedCourierCommit("main_thread_commit_exception"));
				}
			});
		}
		catch (Exception ex)
		{
			Retire("main_thread_dispatch_failed");
			try { Log("detached courier commit queue failed session=" + (sessionId ?? "") + " error=" + ex.Message); }
			catch { } // Diagnostics cannot replace a claimed result.
		}
		return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(
			AwaitCourierRefactorCommitAsync(completion.Task, () => Retire("main_thread_dispatch_timeout"), sessionId), registration);
	}

	private InteractionCommitResult InvokeCourierRefactorCommit(
		Func<InteractionCommitResult> commit,
		string targetLog,
		string sessionId,
		bool abortInboundWithoutReceipt)
	{
		InteractionCommitResult result;
		try
		{
			result = commit()
				?? CreateUnconfirmedCourierCommit("missing_commit_result");
		}
		catch (Exception ex)
		{
			try { Log("detached courier commit failed session=" + (sessionId ?? "")
				+ " target=" + (targetLog ?? "") + " error=" + ex.Message); }
			catch { } // Diagnostics do not own the commit outcome.
			result = CreateUnconfirmedCourierCommit("main_thread_commit_exception");
		}
		if (abortInboundWithoutReceipt)
		{
			try
			{
				CourierSession session = GetSessionById(sessionId);
				if (session != null && IsInboundToPlayer(session) && !IsTerminalStage(session)
					&& !session.DeliveryApplied && !session.ReplyGenerated
					&& string.IsNullOrWhiteSpace(session.InboundCompletionReceipt))
				{
					AbortCourierInboundCompletion(
						session,
						string.IsNullOrWhiteSpace(result.ErrorCode)
							? "commit_without_completion_receipt"
							: result.ErrorCode);
					result = new InteractionCommitResult(
						InteractionStatus.NonRetryableFailure,
						result.HistoryWritten,
						result.ActionsExecuted,
						"courier_inbound_completion_receipt_missing",
						result.EffectState);
				}
			}
			catch (Exception ex)
			{
				try { Log("inbound commit cleanup failed session=" + (sessionId ?? "")
					+ " error=" + ex.Message); }
				catch { }
			}
		}
		return result;
	}

	private static async Task<InteractionCommitResult> AwaitCourierRefactorCommitAsync(
		Task<InteractionCommitResult> task,
		Func<bool> tryExpire,
		string sessionId)
	{
		using (var timeout = new CancellationTokenSource())
		{
			try
			{
				Task completed = await Task.WhenAny(task, Task.Delay(30000, timeout.Token)).ConfigureAwait(false);
				if (completed != task && tryExpire())
				{
					try { Log("detached courier commit timeout session=" + (sessionId ?? "")); }
					catch { }
				}
				// A timer/retirement cannot undo a claimed history/action/delivery commit.
				// Preserve its real receipt rather than report a retryable no-effect failure.
				return await task.ConfigureAwait(false);
			}
			finally { timeout.Cancel(); }
		}
	}
}
