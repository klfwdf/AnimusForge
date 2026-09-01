using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class DuelBehavior
{
	// Runtime-only observation sidecar. It never owns Mission, death, Economy,
	// Memory, save loading, or recovery, and it cannot replay any Duel effect.
	private static readonly object _duelOutcomeOwnerSync = new object();

	private static DuelOutcomeOwner _duelOutcomeOwner = new DuelOutcomeOwner();

	private static readonly IDetachedDuelDispatchOwner _detachedDuelDispatchOwner =
		new DuelBehaviorDetachedDispatchOwner();

	private const int ExactDuelDispatchSeenCapacity = 4096;

	private static readonly HashSet<string> _exactDuelDispatchIdsSeen =
		new HashSet<string>(StringComparer.Ordinal);

	private static long _duelOutcomeSerial;

	private const int DuelOutcomeSubjectIndexCapacity = 256;

	private static readonly object _duelOutcomeSubjectIndexSync = new object();

	private static readonly Dictionary<string, string> _latestDuelOutcomeIdsBySubject =
		new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly Queue<KeyValuePair<string, string>> _duelOutcomeSubjectIndexOrder =
		new Queue<KeyValuePair<string, string>>();

	private const int DuelOutcomeRequestIndexCapacity = 512;

	private static readonly object _duelOutcomeRequestIndexSync = new object();

	private static readonly Dictionary<string, string> _duelOutcomeIdsByRequest =
		new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly Queue<KeyValuePair<string, string>> _duelOutcomeRequestIndexOrder =
		new Queue<KeyValuePair<string, string>>();

	private DuelOutcomeStartIdentity _activeDuelOutcomeStart;

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

	private static void DiscardDuelArtifactsForRequest(
		DetachedDuelDispatchContext context,
		string subjectId)
	{
		if (context != null)
		{
			string requestSubject = context.RequestIdentity?.SubjectId;
			DiscardBoundDuelArtifacts(requestSubject, context.DuelId);
			DiscardUnboundDuelArtifacts(requestSubject);
			if (string.Equals(requestSubject, subjectId, StringComparison.Ordinal))
			{
				return;
			}
		}
		DiscardUnboundDuelArtifacts(subjectId);
	}

	private void ClearDetachedDuelDispatchesForLoad()
	{
		AbortDetachedDuelDispatch(
			_meetingPendingDuelDispatchContext,
			"save_generation_changed");
		_meetingPendingDuelDispatchContext = null;
		AbortDetachedDuelDispatch(
			_queuedDuelDispatchContext,
			"save_generation_changed");
		_queuedDuelDispatchContext = null;
		AbortDetachedDuelDispatch(
			_openingDuelDispatchContext,
			"save_generation_changed");
		_openingDuelDispatchContext = null;
		if (_wildernessDuelRuntime?.DuelDispatchContext != null)
		{
			AbortDetachedDuelDispatch(
				_wildernessDuelRuntime.DuelDispatchContext,
				"save_generation_changed");
			_wildernessDuelRuntime.DuelDispatchContext = null;
		}
		if (_wildernessDuelRuntime != null)
		{
			_wildernessDuelRuntime.AbortRequested = true;
		}

		_meetingPendingStart = false;
		_meetingPendingNonHeroMemoryId = "";
		_meetingPreFightActive = false;
		_meetingPreFightEndTime = 0f;
		_queuedArenaDuelTarget = null;
		_queuedDuelTargetCharacter = null;
		_queuedDuelNonHeroMemoryId = "";
		_queuedArenaDuelDelay = 0f;
		_queuedWildernessDuel = false;
		_queuedDuelWaitingForConversationExit = false;
		_queuedDuelReadyUtcTicks = 0L;
		_queuedDuelConversationCloseAttempts = 0;
		_leaveSourceMissionRequested = false;
		_leaveSourceMissionReadyTime = 0f;
		_pendingDuelTarget = null;
		_preDuelTimer = 0f;
		_arenaMissionActive = false;
		_arenaMissionLeaveRequested = false;
		_arenaMissionLeaveReadyTime = 0f;
		_arenaMissionOpeningGraceUntilUtcTicks = 0L;
		_arenaMissionStartedOnce = false;
		_returnToMapAfterIndependentDuel = false;
		_wildernessDuelActiveDiagnosticId = 0;
		_wildernessDuelOpenStartedUtcTicks = 0L;
		_wildernessDuelLastOpeningDiagUtcTicks = 0L;
		_wildernessDuelLastOpenScene = "";
		_wildernessDuelEncounterMenuGuardUntilUtcTicks = 0L;
		_wildernessDuelEncounterMenuGuardLastLogUtcTicks = 0L;
		_wildernessDuelEncounterMenuGuardReason = "";
		_wildernessDuelEncounterMenuExitRequested = false;
		_wildernessDuelEncounterMenuExitRequestUntilUtcTicks = 0L;
		_wildernessDuelEncounterMenuExitLastAttemptUtcTicks = 0L;
		_pendingMainHeroDeath = false;
		_pendingMainHeroDeathKiller = null;
		_pendingMainHeroDeathRequestUtcTicks = 0L;
		_openTownMenuRequested = false;
		_pendingNonHeroDuelMemoryId = "";
		_pendingNonHeroDuelMemoryName = "";
		_forcedMainHeroDeath = false;
		_targetHero = null;
		_targetCharacter = null;
		_targetAgentIndex = -1;
		_targetDisplayName = "";
		_isDuelActive = false;
	}

	private static bool TryBeginDuelOutcome(
		string subjectId,
		DuelSessionKind sessionKind,
		string source,
		out DuelOutcomeStartIdentity startIdentity,
		DetachedDuelDispatchContext dispatchContext = null)
	{
		startIdentity = null;
		string normalizedSubject = "";
		try
		{
			normalizedSubject = NormalizeDuelOutcomeSubject(subjectId);
			if (string.IsNullOrWhiteSpace(normalizedSubject))
			{
				return false;
			}

			long generation = Math.Max(0L, SaveRuntimeGuard.CaptureGeneration());
			string generationToken = generation.ToString(CultureInfo.InvariantCulture);
			string kindToken = ((int)sessionKind).ToString(CultureInfo.InvariantCulture);
			DuelOutcomeRequestIdentity request = dispatchContext?.RequestIdentity;
			if (request != null)
			{
				if (!string.Equals(request.SubjectId, normalizedSubject, StringComparison.Ordinal)
					|| request.RuntimeGeneration != generation
					|| request.SaveGeneration != generation)
				{
					RejectDetachedDuelDispatch(dispatchContext, "actual_start_identity_mismatch");
					DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);
					LogDuelOutcomeFailure("exact_start", "duel.dispatch_start_identity_mismatch", source);
					return false;
				}
			}
			else
			{
				long serial = Interlocked.Increment(ref _duelOutcomeSerial);
				string serialToken = serial.ToString(CultureInfo.InvariantCulture);
				string duelId = DuelOutcomeFingerprint.Hash(
					"AnimusForge.DuelOutcome.HostDuel.v1",
					generationToken,
					serialToken,
					normalizedSubject,
					kindToken,
					Guid.NewGuid().ToString("N"));
				string actionFingerprint = DuelOutcomeFingerprint.Hash(
					"AnimusForge.DuelOutcome.LegacyUnboundAction.v1",
					normalizedSubject,
					kindToken,
					NormalizeDuelOutcomeSource(source),
					BuildPendingDuelArtifactFingerprint(normalizedSubject));
				if (!DuelOutcomeRequestIdentity.TryCreate(
					duelId,
					"legacy-unbound-" + serialToken,
					"duel-runtime-" + generationToken + "-" + serialToken,
					DuelOutcomeChannel.Domain,
					"duel-runtime-" + generationToken,
					normalizedSubject,
					generation,
					generation,
					actionFingerprint,
					out request,
					out string requestError))
				{
					DiscardUnboundDuelArtifacts(normalizedSubject);
					LogDuelOutcomeFailure("request", requestError, source);
					return false;
				}
			}
			string exactDuelId = request.DuelId;
			if (request == null)
			{
				return false;
			}

			DuelOutcomeOperationStatus queued = QueueDuelOutcomeRequest(
				request,
				out _,
				out string queueError);
			if (queued != DuelOutcomeOperationStatus.Accepted
				&& queued != DuelOutcomeOperationStatus.Duplicate)
			{
				RejectDetachedDuelDispatch(
					dispatchContext,
					string.IsNullOrWhiteSpace(queueError)
						? "duel.actual_start_queue_failed"
						: queueError);
				DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);
				LogDuelOutcomeFailure("queue", queueError, source);
				return false;
			}

			string duelSessionId = DuelOutcomeFingerprint.Hash(
				"AnimusForge.DuelOutcome.HostSession.v1",
				exactDuelId,
				kindToken,
				NormalizeDuelOutcomeSource(source));
			if (!DuelOutcomeStartIdentity.TryCreate(
				request,
				duelSessionId,
				sessionKind,
				out DuelOutcomeStartIdentity start,
				out string startIdentityError))
			{
				if (dispatchContext != null)
				{
					AbortDetachedDuelDispatch(dispatchContext, "start_identity_invalid");
				}
				else
				{
					_duelOutcomeOwner.Cancel(request, "start_identity_invalid", out _, out _);
				}
				DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);
				LogDuelOutcomeFailure("start_identity", startIdentityError, source);
				return false;
			}

			DuelOutcomeOperationStatus started = _duelOutcomeOwner.Start(
				start,
				out DuelOutcomeReceipt startedReceipt,
				out string startError);
			if (started != DuelOutcomeOperationStatus.Accepted
				&& started != DuelOutcomeOperationStatus.Duplicate)
			{
				if (dispatchContext != null)
				{
					AbortDetachedDuelDispatch(dispatchContext, "start_failed");
				}
				else
				{
					_duelOutcomeOwner.Cancel(request, "start_failed", out _, out _);
				}
				DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);
				LogDuelOutcomeFailure("start", startError, source);
				return false;
			}
			dispatchContext?.ObserveOwnerReceipt(startedReceipt, startError);

			startIdentity = start;
			BindPendingDuelArtifacts(normalizedSubject, exactDuelId);
			IndexDuelOutcome(normalizedSubject, exactDuelId);
			if (request != null)
			{
				IndexDuelOutcomeRequest(request.RequestId, exactDuelId);
			}
			Logger.Log(
				"DuelOutcome",
				"started duelId=" + exactDuelId
				+ " subject=" + normalizedSubject
				+ " kind=" + sessionKind
				+ " source=" + NormalizeDuelOutcomeSource(source)
				+ " recovery=NOT_RECOVERABLE");
			return true;
		}
		catch (Exception ex)
		{
			if (startIdentity != null)
			{
				if (dispatchContext != null)
				{
					AbortDetachedDuelDispatch(dispatchContext, "begin_exception");
				}
				else
				{
					MarkDuelOutcomeUnknown(
						startIdentity,
						"begin_exception",
						source);
				}
			}
			else
			{
				RejectDetachedDuelDispatch(dispatchContext, "begin_exception");
			}
			DiscardDuelArtifactsForRequest(dispatchContext, normalizedSubject);
			LogDuelOutcomeFailure("begin_exception", ex.GetType().Name, source);
			return false;
		}
	}

	private static DuelOutcomeOperationStatus QueueDuelOutcomeRequest(
		DuelOutcomeRequestIdentity request,
		out DuelOutcomeReceipt receipt,
		out string errorCode)
	{
		DuelOutcomeOwner owner = _duelOutcomeOwner;
		DuelOutcomeOperationStatus status = owner.Queue(request, out receipt, out errorCode);
		if (status != DuelOutcomeOperationStatus.CapacityExceeded)
		{
			return status;
		}

		lock (_duelOutcomeOwnerSync)
		{
			owner = _duelOutcomeOwner;
			if (owner.ActiveCount == 0)
			{
				_duelOutcomeOwner = new DuelOutcomeOwner();
				owner = _duelOutcomeOwner;
				ClearDuelOutcomeSubjectIndex();
				Logger.Log(
					"DuelOutcome",
					"terminal retention window rolled over; recovery=NOT_RECOVERABLE");
			}
			return owner.Queue(request, out receipt, out errorCode);
		}
	}

	private static DuelOutcomeOperationStatus RejectDuelOutcomeRequest(
		DuelOutcomeRequestIdentity request,
		string reasonCode,
		out DuelOutcomeReceipt receipt,
		out string errorCode)
	{
		DuelOutcomeOwner owner = _duelOutcomeOwner;
		DuelOutcomeOperationStatus status = owner.Reject(
			request,
			reasonCode,
			out receipt,
			out errorCode);
		if (status != DuelOutcomeOperationStatus.CapacityExceeded)
		{
			return status;
		}

		lock (_duelOutcomeOwnerSync)
		{
			owner = _duelOutcomeOwner;
			if (owner.ActiveCount == 0)
			{
				_duelOutcomeOwner = new DuelOutcomeOwner();
				owner = _duelOutcomeOwner;
				ClearDuelOutcomeSubjectIndex();
				Logger.Log(
					"DuelOutcome",
					"terminal retention window rolled over during exact rejection; recovery=NOT_RECOVERABLE");
			}
			return owner.Reject(request, reasonCode, out receipt, out errorCode);
		}
	}

	private static bool TryRecordDuelOutcome(
		DuelOutcomeStartIdentity start,
		bool playerWon,
		string source,
		out DuelOutcomeResultIdentity result)
	{
		result = null;
		if (start == null)
		{
			return false;
		}
		try
		{
			DuelResultKind resultKind = playerWon
				? DuelResultKind.PlayerWon
				: DuelResultKind.OpponentWon;
			string resultId = DuelOutcomeFingerprint.Hash(
				"AnimusForge.DuelOutcome.HostResult.v1",
				start.DuelId,
				((int)resultKind).ToString(CultureInfo.InvariantCulture));
			if (!DuelOutcomeResultIdentity.TryCreate(
				start,
				resultId,
				resultKind,
				out result,
				out string identityError))
			{
				LogDuelOutcomeFailure("result_identity", identityError, source);
				return false;
			}

			DuelOutcomeOperationStatus recorded = _duelOutcomeOwner.RecordOutcome(
				result,
				out DuelOutcomeReceipt receipt,
				out string recordError);
			if (recorded != DuelOutcomeOperationStatus.Accepted
				&& recorded != DuelOutcomeOperationStatus.Duplicate)
			{
				LogDuelOutcomeFailure("record", recordError, source);
				return false;
			}

			Logger.Log(
				"DuelOutcome",
				"outcome_known duelId=" + start.DuelId
				+ " state=" + receipt.State
				+ " result=" + resultKind
				+ " source=" + NormalizeDuelOutcomeSource(source));
			return true;
		}
		catch (Exception ex)
		{
			LogDuelOutcomeFailure("record_exception", ex.GetType().Name, source);
			result = null;
			return false;
		}
	}

	private static bool TryFinalizeDuelOutcome(
		DuelOutcomeResultIdentity result,
		string source,
		DuelOutcomeEffects effects,
		out DuelOutcomeReceipt receipt)
	{
		receipt = null;
		if (result == null || effects == null)
		{
			return false;
		}
		try
		{
			DuelOutcomeOperationStatus finalized = _duelOutcomeOwner.Finalize(
				result,
				effects,
				out receipt,
				out string finalizeError);
			if (finalized != DuelOutcomeOperationStatus.Accepted
				&& finalized != DuelOutcomeOperationStatus.Duplicate)
			{
				LogDuelOutcomeFailure("finalize", finalizeError, source);
				return false;
			}

			Logger.Log(
				"DuelOutcome",
				"terminal duelId=" + result.DuelId
				+ " state=" + receipt.State
				+ " result=" + result.ResultKind
				+ " source=" + NormalizeDuelOutcomeSource(source)
				+ " recovery=NOT_RECOVERABLE");
			IndexDuelOutcome(receipt.RequestIdentity?.SubjectId, result.DuelId);
			DiscardBoundDuelArtifacts(receipt.RequestIdentity?.SubjectId, result.DuelId);
			return true;
		}
		catch (Exception ex)
		{
			LogDuelOutcomeFailure("finalize_exception", ex.GetType().Name, source);
			return false;
		}
	}

	private static void MarkDuelOutcomeUnknown(
		DuelOutcomeStartIdentity start,
		string reasonCode,
		string source)
	{
		if (start == null)
		{
			return;
		}

		try
		{
			DuelOutcomeOperationStatus status = _duelOutcomeOwner.MarkUnknownAfterStart(
				start,
				reasonCode,
				out DuelOutcomeReceipt receipt,
				out string errorCode);
			if (status == DuelOutcomeOperationStatus.Accepted
				|| status == DuelOutcomeOperationStatus.Duplicate)
			{
				Logger.Log(
					"DuelOutcome",
					"terminal duelId=" + start.DuelId
					+ " state=" + receipt.State
					+ " reason=" + reasonCode
					+ " source=" + NormalizeDuelOutcomeSource(source)
					+ " recovery=NOT_RECOVERABLE");
				IndexDuelOutcome(receipt.RequestIdentity?.SubjectId, start.DuelId);
				DiscardBoundDuelArtifacts(receipt.RequestIdentity?.SubjectId, start.DuelId);
				return;
			}
			LogDuelOutcomeFailure("unknown", errorCode, source);
		}
		catch (Exception ex)
		{
			LogDuelOutcomeFailure("unknown_exception", ex.GetType().Name, source);
		}
	}

	private static bool TryCreateDuelOutcomeEffects(
		DuelOutcomeEffectState memory,
		DuelOutcomeEffectState afef,
		DuelOutcomeEffectState death,
		DuelOutcomeEffectState renown,
		DuelOutcomeEffectState stake,
		out DuelOutcomeEffects effects)
	{
		if (DuelOutcomeEffects.TryCreate(
			memory,
			afef,
			death,
			renown,
			stake,
			out effects,
			out string errorCode))
		{
			return true;
		}
		LogDuelOutcomeFailure("effects", errorCode, "settlement");
		return false;
	}

	internal static bool TryReadDuelOutcome(string duelId, out DuelOutcomeReceipt receipt)
	{
		try
		{
			return _duelOutcomeOwner.TryGet(duelId, out receipt);
		}
		catch
		{
			receipt = null;
			return false;
		}
	}

	internal static bool TryReadLatestDuelOutcome(string subjectId, out DuelOutcomeReceipt receipt)
	{
		receipt = null;
		string normalizedSubject = NormalizeDuelOutcomeSubject(subjectId);
		if (string.IsNullOrWhiteSpace(normalizedSubject))
		{
			return false;
		}
		string duelId;
		lock (_duelOutcomeSubjectIndexSync)
		{
			if (!_latestDuelOutcomeIdsBySubject.TryGetValue(normalizedSubject, out duelId))
			{
				return false;
			}
		}
		return TryReadDuelOutcome(duelId, out receipt)
			&& string.Equals(
				receipt.RequestIdentity?.SubjectId,
				normalizedSubject,
				StringComparison.Ordinal);
	}

	internal static bool TryReadDuelOutcomeByRequestId(
		string requestId,
		out DuelOutcomeReceipt receipt)
	{
		receipt = null;
		string normalizedRequest = (requestId ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(normalizedRequest))
		{
			return false;
		}
		string duelId;
		lock (_duelOutcomeRequestIndexSync)
		{
			if (!_duelOutcomeIdsByRequest.TryGetValue(normalizedRequest, out duelId))
			{
				return false;
			}
		}
		long generation = Math.Max(0L, SaveRuntimeGuard.CaptureGeneration());
		return TryReadDuelOutcome(duelId, out receipt)
			&& string.Equals(
				receipt.RequestIdentity?.RequestId,
				normalizedRequest,
				StringComparison.Ordinal)
			&& receipt.RequestIdentity.RuntimeGeneration == generation
			&& receipt.RequestIdentity.SaveGeneration == generation;
	}

	private static void IndexDuelOutcome(string subjectId, string duelId)
	{
		string normalizedSubject = NormalizeDuelOutcomeSubject(subjectId);
		if (string.IsNullOrWhiteSpace(normalizedSubject)
			|| string.IsNullOrWhiteSpace(duelId))
		{
			return;
		}
		lock (_duelOutcomeSubjectIndexSync)
		{
			if (_latestDuelOutcomeIdsBySubject.TryGetValue(normalizedSubject, out string existing)
				&& string.Equals(existing, duelId, StringComparison.Ordinal))
			{
				return;
			}
			_latestDuelOutcomeIdsBySubject[normalizedSubject] = duelId;
			_duelOutcomeSubjectIndexOrder.Enqueue(
				new KeyValuePair<string, string>(normalizedSubject, duelId));
			while ((_latestDuelOutcomeIdsBySubject.Count > DuelOutcomeSubjectIndexCapacity
				|| _duelOutcomeSubjectIndexOrder.Count > DuelOutcomeSubjectIndexCapacity * 2)
				&& _duelOutcomeSubjectIndexOrder.Count > 0)
			{
				KeyValuePair<string, string> oldest = _duelOutcomeSubjectIndexOrder.Dequeue();
				if (_latestDuelOutcomeIdsBySubject.TryGetValue(oldest.Key, out string current)
					&& string.Equals(current, oldest.Value, StringComparison.Ordinal))
				{
					_latestDuelOutcomeIdsBySubject.Remove(oldest.Key);
				}
			}
		}
	}

	private static void IndexDuelOutcomeRequest(string requestId, string duelId)
	{
		string normalizedRequest = (requestId ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(normalizedRequest)
			|| string.IsNullOrWhiteSpace(duelId))
		{
			return;
		}
		lock (_duelOutcomeRequestIndexSync)
		{
			if (_duelOutcomeIdsByRequest.TryGetValue(normalizedRequest, out string existing)
				&& string.Equals(existing, duelId, StringComparison.Ordinal))
			{
				return;
			}
			_duelOutcomeIdsByRequest[normalizedRequest] = duelId;
			_duelOutcomeRequestIndexOrder.Enqueue(
				new KeyValuePair<string, string>(normalizedRequest, duelId));
			while (_duelOutcomeRequestIndexOrder.Count > DuelOutcomeRequestIndexCapacity)
			{
				KeyValuePair<string, string> oldest = _duelOutcomeRequestIndexOrder.Dequeue();
				if (_duelOutcomeIdsByRequest.TryGetValue(oldest.Key, out string current)
					&& string.Equals(current, oldest.Value, StringComparison.Ordinal))
				{
					_duelOutcomeIdsByRequest.Remove(oldest.Key);
				}
			}
		}
	}

	private static void ClearDuelOutcomeSubjectIndex()
	{
		lock (_duelOutcomeSubjectIndexSync)
		{
			_latestDuelOutcomeIdsBySubject.Clear();
			_duelOutcomeSubjectIndexOrder.Clear();
		}
		lock (_duelOutcomeRequestIndexSync)
		{
			_duelOutcomeIdsByRequest.Clear();
			_duelOutcomeRequestIndexOrder.Clear();
		}
	}

	private static string BuildPendingDuelArtifactFingerprint(string subjectId)
	{
		var tokens = new List<string>
		{
			"AnimusForge.DuelOutcome.PendingArtifacts.v1",
			subjectId ?? ""
		};
		if (_pendingDuelStakes != null
			&& _pendingDuelStakes.TryGetValue(subjectId ?? "", out PendingDuelStake stake)
			&& stake != null
			&& string.IsNullOrWhiteSpace(stake.DuelOutcomeId))
		{
			tokens.Add("stake");
			tokens.Add(stake.Gold.ToString(CultureInfo.InvariantCulture));
			tokens.Add(stake.PlayerGold.ToString(CultureInfo.InvariantCulture));
			tokens.Add(stake.NpcGold.ToString(CultureInfo.InvariantCulture));
			tokens.Add("stake.items");
			AppendDuelStakeItemFingerprint(tokens, stake.Items);
			tokens.Add("stake.player_items");
			AppendDuelStakeItemFingerprint(tokens, stake.PlayerItems);
			tokens.Add("stake.npc_items");
			AppendDuelStakeItemFingerprint(tokens, stake.NpcItems);
		}
		if (_pendingDuelDebtTags != null
			&& _pendingDuelDebtTags.TryGetValue(subjectId ?? "", out PendingDuelDebtTag debt)
			&& debt != null
			&& string.IsNullOrWhiteSpace(debt.DuelOutcomeId))
		{
			tokens.Add("debt");
			tokens.Add(debt.Amount.ToString(CultureInfo.InvariantCulture));
			tokens.Add(debt.DueDays.ToString(CultureInfo.InvariantCulture));
			tokens.Add(DuelOutcomeFingerprint.Hash(debt.Note ?? ""));
		}
		if (Instance?._lastDuelAfterLines != null
			&& Instance._lastDuelAfterLines.TryGetValue(subjectId ?? "", out DuelAfterLines lines)
			&& lines != null
			&& string.IsNullOrWhiteSpace(lines.DuelOutcomeId))
		{
			tokens.Add("after_lines");
			tokens.Add(DuelOutcomeFingerprint.Hash(lines.WinLine ?? ""));
			tokens.Add(DuelOutcomeFingerprint.Hash(lines.LoseLine ?? ""));
		}
		return DuelOutcomeFingerprint.Hash(tokens.ToArray());
	}

	private static void AppendDuelStakeItemFingerprint(
		List<string> tokens,
		Dictionary<string, int> items)
	{
		if (items == null)
		{
			tokens.Add("0");
			return;
		}
		foreach (KeyValuePair<string, int> item in items
			.Where(value => !string.IsNullOrWhiteSpace(value.Key) && value.Value > 0)
			.OrderBy(value => value.Key, StringComparer.OrdinalIgnoreCase))
		{
			tokens.Add(item.Key.Trim());
			tokens.Add(item.Value.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static void BindPendingDuelArtifacts(string subjectId, string duelId)
	{
		if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(duelId))
		{
			return;
		}
		if (_pendingDuelStakes != null
			&& _pendingDuelStakes.TryGetValue(subjectId, out PendingDuelStake stake)
			&& stake != null
			&& string.IsNullOrWhiteSpace(stake.DuelOutcomeId))
		{
			stake.DuelOutcomeId = duelId;
		}
		if (_pendingDuelDebtTags != null
			&& _pendingDuelDebtTags.TryGetValue(subjectId, out PendingDuelDebtTag debt)
			&& debt != null
			&& string.IsNullOrWhiteSpace(debt.DuelOutcomeId))
		{
			debt.DuelOutcomeId = duelId;
		}
		if (Instance?._lastDuelAfterLines != null
			&& Instance._lastDuelAfterLines.TryGetValue(subjectId, out DuelAfterLines lines)
			&& lines != null
			&& string.IsNullOrWhiteSpace(lines.DuelOutcomeId))
		{
			lines.DuelOutcomeId = duelId;
		}
	}

	private static void DiscardUnboundDuelArtifacts(Hero hero)
	{
		DiscardUnboundDuelArtifacts(hero?.StringId);
	}

	private static void DiscardUnboundDuelArtifacts(string subjectId)
	{
		if (string.IsNullOrWhiteSpace(subjectId))
		{
			return;
		}
		if (_pendingDuelStakes != null
			&& _pendingDuelStakes.TryGetValue(subjectId, out PendingDuelStake stake)
			&& stake != null
			&& string.IsNullOrWhiteSpace(stake.DuelOutcomeId))
		{
			_pendingDuelStakes.Remove(subjectId);
		}
		if (_pendingDuelDebtTags != null
			&& _pendingDuelDebtTags.TryGetValue(subjectId, out PendingDuelDebtTag debt)
			&& debt != null
			&& string.IsNullOrWhiteSpace(debt.DuelOutcomeId))
		{
			_pendingDuelDebtTags.Remove(subjectId);
		}
		if (Instance?._lastDuelAfterLines != null
			&& Instance._lastDuelAfterLines.TryGetValue(subjectId, out DuelAfterLines lines)
			&& lines != null
			&& string.IsNullOrWhiteSpace(lines.DuelOutcomeId))
		{
			Instance._lastDuelAfterLines.Remove(subjectId);
		}
	}

	private static void DiscardBoundDuelArtifacts(string subjectId, string duelId)
	{
		if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(duelId))
		{
			return;
		}
		if (_pendingDuelStakes != null
			&& _pendingDuelStakes.TryGetValue(subjectId, out PendingDuelStake stake)
			&& stake != null
			&& string.Equals(stake.DuelOutcomeId, duelId, StringComparison.Ordinal))
		{
			_pendingDuelStakes.Remove(subjectId);
		}
		if (_pendingDuelDebtTags != null
			&& _pendingDuelDebtTags.TryGetValue(subjectId, out PendingDuelDebtTag debt)
			&& debt != null
			&& string.Equals(debt.DuelOutcomeId, duelId, StringComparison.Ordinal))
		{
			_pendingDuelDebtTags.Remove(subjectId);
		}
		if (Instance?._lastDuelAfterLines != null
			&& Instance._lastDuelAfterLines.TryGetValue(subjectId, out DuelAfterLines lines)
			&& lines != null
			&& string.Equals(lines.DuelOutcomeId, duelId, StringComparison.Ordinal))
		{
			Instance._lastDuelAfterLines.Remove(subjectId);
		}
	}

	private static string ResolveDuelOutcomeSubjectId(
		Hero targetHero,
		CharacterObject targetCharacter,
		string nonHeroMemoryId = null)
	{
		string subjectId = targetHero?.StringId;
		if (string.IsNullOrWhiteSpace(subjectId))
		{
			subjectId = nonHeroMemoryId;
		}
		if (string.IsNullOrWhiteSpace(subjectId))
		{
			subjectId = targetCharacter?.StringId;
		}
		if (string.IsNullOrWhiteSpace(subjectId))
		{
			subjectId = "unknown-duel-target";
		}
		return NormalizeDuelOutcomeSubject(subjectId);
	}

	private static string NormalizeDuelOutcomeSubject(string value)
	{
		string normalized = (value ?? "").Trim();
		if (normalized.Length <= DuelOutcomeFingerprint.MaximumTokenLength)
		{
			return normalized;
		}
		return "subject-" + DuelOutcomeFingerprint.Hash(normalized);
	}

	private static string NormalizeDuelOutcomeSource(string value)
	{
		string normalized = (value ?? "unknown").Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return "unknown";
		}
		return normalized.Length <= DuelOutcomeFingerprint.MaximumTokenLength
			? normalized
			: "source-" + DuelOutcomeFingerprint.Hash(normalized);
	}

	private static void LogDuelOutcomeFailure(string stage, string errorCode, string source)
	{
		try
		{
			Logger.Log(
				"DuelOutcome",
				"[WARN] stage=" + (stage ?? "unknown")
				+ " error=" + (errorCode ?? "unknown")
				+ " source=" + NormalizeDuelOutcomeSource(source));
		}
		catch
		{
		}
	}
}
