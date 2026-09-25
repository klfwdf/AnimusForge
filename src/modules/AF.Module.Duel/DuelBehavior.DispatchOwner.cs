using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class DuelBehavior
{
	private static readonly IDetachedDuelDispatchOwner _detachedDuelDispatchOwner =
		new DuelBehaviorDetachedDispatchOwner();

	private const int ExactDuelDispatchSeenCapacity = 4096;

	private static readonly HashSet<string> _exactDuelDispatchIdsSeen =
		new HashSet<string>(StringComparer.Ordinal);

	internal static IDetachedDuelDispatchOwner CreateDetachedDuelDispatchOwnerForExternal()
	{
		return _detachedDuelDispatchOwner;
	}

	private sealed class DuelBehaviorDetachedDispatchOwner : IDetachedDuelDispatchOwner
	{
		public bool TryQueue(
			DetachedDuelDispatchContext context,
			out bool shouldDispatch,
			out string errorCode)
		{
			shouldDispatch = false;
			errorCode = "duel.dispatch_identity_invalid";
			if (!IsSceneDuelBridgeEnabled())
			{
				errorCode = "duel.bridge_disabled";
				context?.MarkRejected(errorCode);
				return false;
			}
			if (!ValidateDetachedDuelDispatchContext(context, out errorCode))
			{
				context?.MarkRejected(errorCode);
				return false;
			}

			DuelOutcomeOperationStatus status;
			DuelOutcomeReceipt receipt;
			lock (_duelOutcomeOwnerSync)
			{
				if (_exactDuelDispatchIdsSeen.Contains(context.DuelId))
				{
					if (!_duelOutcomeOwner.TryGet(context.DuelId, out receipt)
						|| receipt?.RequestIdentity == null)
					{
						context.MarkRejected("duel.dispatch_retention_expired");
						errorCode = "duel.dispatch_retention_expired";
						return false;
					}
					if (!context.ObserveOwnerReceipt(receipt, string.Empty))
					{
						errorCode = "duel.dispatch_identity_conflict";
						return false;
					}
					shouldDispatch = false;
					errorCode = string.Empty;
					return true;
				}
				if (_exactDuelDispatchIdsSeen.Count >= ExactDuelDispatchSeenCapacity)
				{
					context.MarkRejected("duel.dispatch_exact_capacity");
					errorCode = "duel.dispatch_exact_capacity";
					return false;
				}
				// Exact detached requests fail closed at owner capacity. They never
				// trigger the legacy terminal-window rollover.
				status = _duelOutcomeOwner.Queue(
					context.RequestIdentity,
					out receipt,
					out errorCode);
				if ((status == DuelOutcomeOperationStatus.Accepted
						|| status == DuelOutcomeOperationStatus.Duplicate)
					&& context.ObserveOwnerReceipt(receipt, errorCode))
				{
					_exactDuelDispatchIdsSeen.Add(context.DuelId);
				}
			}
			if (status != DuelOutcomeOperationStatus.Accepted
				&& status != DuelOutcomeOperationStatus.Duplicate)
			{
				if (context.Snapshot() == null)
				{
					context.MarkRejected(string.IsNullOrWhiteSpace(errorCode)
						? "duel.dispatch_queue_rejected"
						: errorCode);
				}
				return false;
			}
			DetachedDuelDispatchReceipt observedDispatch = context.Snapshot();
			if (observedDispatch?.State == DetachedDuelDispatchState.Rejected)
			{
				errorCode = string.IsNullOrWhiteSpace(observedDispatch.ErrorCode)
					? "duel.dispatch_identity_conflict"
					: observedDispatch.ErrorCode;
				return false;
			}
			shouldDispatch = status == DuelOutcomeOperationStatus.Accepted;
			IndexDuelOutcomeRequest(context.RequestIdentity.RequestId, context.DuelId);
			IndexDuelOutcome(context.RequestIdentity.SubjectId, context.DuelId);
			return true;
		}

		public void Reject(DetachedDuelDispatchContext context, string reasonCode)
		{
			if (context?.RequestIdentity == null)
			{
				return;
			}
			lock (_duelOutcomeOwnerSync)
			{
				if (!_exactDuelDispatchIdsSeen.Contains(context.DuelId))
				{
					if (_exactDuelDispatchIdsSeen.Count >= ExactDuelDispatchSeenCapacity)
					{
						context.MarkRejected("duel.dispatch_exact_capacity");
						return;
					}
					_exactDuelDispatchIdsSeen.Add(context.DuelId);
				}
			}
			DuelOutcomeOperationStatus status = RejectDuelOutcomeRequest(
				context.RequestIdentity,
				reasonCode,
				out DuelOutcomeReceipt receipt,
				out string errorCode);
			context.ObserveOwnerReceipt(
				receipt,
				status == DuelOutcomeOperationStatus.Accepted
					|| status == DuelOutcomeOperationStatus.Duplicate
					? "duel." + NormalizeDuelOutcomeReason(reasonCode, "dispatch_rejected")
					: errorCode);
			if (status == DuelOutcomeOperationStatus.Accepted)
			{
				DiscardBoundDuelArtifacts(
					context.RequestIdentity.SubjectId,
					context.DuelId);
				DiscardUnboundDuelArtifacts(context.RequestIdentity.SubjectId);
			}
			IndexDuelOutcomeRequest(context.RequestIdentity.RequestId, context.DuelId);
			IndexDuelOutcome(context.RequestIdentity.SubjectId, context.DuelId);
		}

		public void Cancel(DetachedDuelDispatchContext context, string reasonCode)
		{
			if (context?.RequestIdentity == null)
			{
				return;
			}
			DuelOutcomeOperationStatus status = _duelOutcomeOwner.Cancel(
				context.RequestIdentity,
				reasonCode,
				out DuelOutcomeReceipt receipt,
				out string errorCode);
			context.ObserveOwnerReceipt(
				receipt,
				status == DuelOutcomeOperationStatus.Accepted
					|| status == DuelOutcomeOperationStatus.Duplicate
					? "duel." + NormalizeDuelOutcomeReason(reasonCode, "dispatch_cancelled")
					: errorCode);
			if (status == DuelOutcomeOperationStatus.Accepted)
			{
				DiscardBoundDuelArtifacts(
					context.RequestIdentity.SubjectId,
					context.DuelId);
				DiscardUnboundDuelArtifacts(context.RequestIdentity.SubjectId);
			}
		}

		public void MarkUnknownAfterStart(DetachedDuelDispatchContext context, string reasonCode)
		{
			DuelOutcomeStartIdentity start = context?.StartIdentity;
			if (context == null)
			{
				return;
			}
			string normalizedError = "duel."
				+ NormalizeDuelOutcomeReason(reasonCode, "unknown_after_start");
			context.MarkUnknownAfterStart(normalizedError);
			if (start != null)
			{
				DuelOutcomeOperationStatus status = _duelOutcomeOwner.MarkUnknownAfterStart(
					start,
					reasonCode,
					out DuelOutcomeReceipt receipt,
					out string errorCode);
				context.ObserveOwnerReceipt(
					receipt,
					status == DuelOutcomeOperationStatus.Accepted
						|| status == DuelOutcomeOperationStatus.Duplicate
						? normalizedError
						: errorCode);
				if (status == DuelOutcomeOperationStatus.Accepted
					|| status == DuelOutcomeOperationStatus.Duplicate)
				{
					DiscardBoundDuelArtifacts(
						context.RequestIdentity.SubjectId,
						context.DuelId);
				}
			}
			else
			{
				// A Mission/opening boundary may already have crossed before the
				// actual Duel session identity exists. Move the request itself to an
				// observable Unknown terminal without inventing a StartIdentity.
				DuelOutcomeOperationStatus unknown = _duelOutcomeOwner.MarkUnknownAfterDispatch(
					context.RequestIdentity,
					reasonCode,
					out DuelOutcomeReceipt unknownReceipt,
					out string unknownError);
				context.ObserveOwnerReceipt(unknownReceipt, unknownError);
				if (unknown == DuelOutcomeOperationStatus.Accepted
					|| unknown == DuelOutcomeOperationStatus.Duplicate)
				{
					DiscardBoundDuelArtifacts(
						context.RequestIdentity.SubjectId,
						context.DuelId);
					DiscardUnboundDuelArtifacts(context.RequestIdentity.SubjectId);
				}
			}
		}
	}

	private static bool ValidateDetachedDuelDispatchContext(
		DetachedDuelDispatchContext context,
		out string errorCode)
	{
		if (context?.RequestIdentity == null)
		{
			errorCode = "duel.dispatch_identity_invalid";
			return false;
		}
		long generation = Math.Max(0L, SaveRuntimeGuard.CaptureGeneration());
		if (context.RequestIdentity.RuntimeGeneration != generation
			|| context.RequestIdentity.SaveGeneration != generation)
		{
			errorCode = "duel.dispatch_stale_generation";
			return false;
		}
		errorCode = string.Empty;
		return true;
	}

	private static string NormalizeDuelOutcomeReason(string value, string fallback)
	{
		return DuelOutcomeFingerprint.TryNormalizeReasonCode(value, out string normalized)
			? normalized
			: fallback;
	}

	internal static void PrepareDuelForDetachedRequest(
		Hero target,
		float delaySeconds,
		DetachedDuelDispatchContext context)
	{
		if (!ValidateDetachedDuelTarget(
			context,
			ResolveDuelOutcomeSubjectId(target, target?.CharacterObject),
			"hero_target_mismatch"))
		{
			return;
		}
		BindPendingDuelArtifacts(context.RequestIdentity.SubjectId, context.DuelId);
		PrepareDuel(target, delaySeconds, context);
	}

	internal static void PrepareDuelForDetachedRequest(
		Agent targetAgent,
		float delaySeconds,
		DetachedDuelDispatchContext context)
	{
		CharacterObject targetCharacter = targetAgent?.Character as CharacterObject;
		if (targetCharacter != null && targetCharacter.HeroObject == null)
		{
			TryCapturePendingNonHeroDuelMemoryFromAgent(targetAgent);
		}
		if (!ValidateDetachedDuelTarget(
			context,
			ResolveDuelOutcomeSubjectId(
				targetCharacter?.HeroObject,
				targetCharacter,
				_pendingNonHeroDuelMemoryId),
			"agent_target_mismatch"))
		{
			return;
		}
		BindPendingDuelArtifacts(context.RequestIdentity.SubjectId, context.DuelId);
		PrepareDuel(targetAgent, delaySeconds, context);
	}

	internal static void PrepareDuelForDetachedRequest(
		CharacterObject targetCharacter,
		float delaySeconds,
		DetachedDuelDispatchContext context)
	{
		if (!ValidateDetachedDuelTarget(
			context,
			ResolveDuelOutcomeSubjectId(
				targetCharacter?.HeroObject,
				targetCharacter,
				_pendingNonHeroDuelMemoryId),
			"character_target_mismatch"))
		{
			return;
		}
		BindPendingDuelArtifacts(context.RequestIdentity.SubjectId, context.DuelId);
		PrepareDuel(targetCharacter, delaySeconds, context);
	}

	internal static void RejectDetachedDuelDispatchForExternal(
		DetachedDuelDispatchContext context,
		string reasonCode)
	{
		_detachedDuelDispatchOwner.Reject(context, reasonCode);
	}

	private static bool ValidateDetachedDuelTarget(
		DetachedDuelDispatchContext context,
		string subjectId,
		string reasonCode)
	{
		if (context?.RequestIdentity == null
			|| !string.Equals(
				context.RequestIdentity.SubjectId,
				NormalizeDuelOutcomeSubject(subjectId),
				StringComparison.Ordinal))
		{
			_detachedDuelDispatchOwner.Reject(context, reasonCode);
			return false;
		}
		return true;
	}

	private static void AcceptDetachedDuelDispatch(DetachedDuelDispatchContext context)
	{
		context?.MarkHostAccepted();
	}

	private static void MarkDetachedDuelSideEffectBoundaryCrossed(
		DetachedDuelDispatchContext context)
	{
		context?.MarkSideEffectBoundaryCrossed();
	}

	private static void MarkDetachedDuelDispatchUnknownAfterStart(
		DetachedDuelDispatchContext context,
		string reasonCode)
	{
		if (context != null)
		{
			_detachedDuelDispatchOwner.MarkUnknownAfterStart(context, reasonCode);
		}
	}

	private static bool IsDetachedDuelDispatchReadyForDelayedHost(
		DetachedDuelDispatchContext context)
	{
		if (context == null)
		{
			return true;
		}
		DetachedDuelDispatchReceipt receipt = context.Snapshot();
		return receipt?.State == DetachedDuelDispatchState.Queued
			&& receipt.HostAccepted;
	}

	private static void RejectDetachedDuelDispatch(
		DetachedDuelDispatchContext context,
		string reasonCode)
	{
		if (context != null)
		{
			DetachedDuelDispatchReceipt receipt = context.Snapshot();
			if (context.SideEffectBoundaryCrossed
				|| receipt?.State == DetachedDuelDispatchState.Started
				|| receipt?.State == DetachedDuelDispatchState.UnknownAfterStart)
			{
				_detachedDuelDispatchOwner.MarkUnknownAfterStart(context, reasonCode);
			}
			else
			{
				_detachedDuelDispatchOwner.Reject(context, reasonCode);
			}
		}
	}

	private static void AbortDetachedDuelDispatch(
		DetachedDuelDispatchContext context,
		string reasonCode)
	{
		DetachedDuelDispatchReceipt receipt = context?.Snapshot();
		if (context?.SideEffectBoundaryCrossed == true
			|| receipt?.State == DetachedDuelDispatchState.Started
			|| receipt?.State == DetachedDuelDispatchState.UnknownAfterStart)
		{
			_detachedDuelDispatchOwner.MarkUnknownAfterStart(context, reasonCode);
		}
		else if (context != null)
		{
			_detachedDuelDispatchOwner.Cancel(context, reasonCode);
		}
	}

	private void MarkActiveDuelUnknown(
		DetachedDuelDispatchContext context,
		string reasonCode,
		string source)
	{
		if (context != null)
		{
			AbortDetachedDuelDispatch(context, reasonCode);
		}
		else
		{
			MarkDuelOutcomeUnknown(_activeDuelOutcomeStart, reasonCode, source);
		}
		_activeDuelOutcomeStart = null;
	}

	private static void ReplaceDetachedDuelDispatch(
		ref DetachedDuelDispatchContext holder,
		DetachedDuelDispatchContext replacement,
		string reasonCode)
	{
		if (holder != null && !ReferenceEquals(holder, replacement))
		{
			AbortDetachedDuelDispatch(holder, reasonCode);
		}
		holder = replacement;
	}

}
