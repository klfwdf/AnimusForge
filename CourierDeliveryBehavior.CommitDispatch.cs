using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.Library;

namespace AnimusForge;

// Final authoritative commit remains owned by the original Courier session. This boundary
// only schedules it, tracks retirement, and preserves the receipt of an already claimed commit.
public partial class CourierDeliveryBehavior
{
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
					completion.TrySetResult(new InteractionCommitResult(
						InteractionStatus.RejectedByValidation,
						false,
						false,
						"main_thread_commit_exception"));
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
				?? new InteractionCommitResult(
					InteractionStatus.RejectedByValidation,
					false,
					false,
					"missing_commit_result");
		}
		catch (Exception ex)
		{
			Log("detached courier commit failed session=" + (sessionId ?? "")
				+ " target=" + (targetLog ?? "") + " error=" + ex.Message);
			result = new InteractionCommitResult(
				InteractionStatus.RejectedByValidation,
				false,
				false,
				"main_thread_commit_exception");
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
						"courier_inbound_completion_receipt_missing");
				}
			}
			catch (Exception ex)
			{
				Log("inbound commit cleanup failed session=" + (sessionId ?? "")
					+ " error=" + ex.Message);
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
