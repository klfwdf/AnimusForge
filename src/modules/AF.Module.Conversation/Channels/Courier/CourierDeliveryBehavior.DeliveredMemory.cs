using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private const int MaximumPendingInboundDeliveredMemoryIntents = 128;

	// Serialized inside the existing NPC-letter JSON. This is memory-only: it must
	// never re-enter delivery, inventory, action, notification or party cleanup.
	private sealed class CourierInboundDeliveredMemoryIntent
	{
		public string SessionId;
		public string SenderHeroId;
		public string HistoryLine;
		public string DeliveryFactText;
		public int OriginGameDay;
		public int OriginGameHour;
		public string OriginLocationId;
		public string RecoveryId;
		public string MemoryPayloadHash;
		public bool Attempted;
		public bool Quarantined;
	}

	private Dictionary<string, CourierInboundDeliveredMemoryIntent> _pendingInboundDeliveredMemoryIntents =
		new Dictionary<string, CourierInboundDeliveredMemoryIntent>(StringComparer.Ordinal);
	private string _pendingInboundDeliveredMemoryCursor = string.Empty;
	private bool _pendingInboundDeliveredMemoryOverflow;

	private Dictionary<string, CourierInboundDeliveredMemoryIntent> CapturePendingInboundDeliveredMemoryIntents()
	{
		lock (_sessionLock)
			return new Dictionary<string, CourierInboundDeliveredMemoryIntent>(
				_pendingInboundDeliveredMemoryIntents, StringComparer.Ordinal);
	}

	private void RestorePendingInboundDeliveredMemoryIntents(
		Dictionary<string, CourierInboundDeliveredMemoryIntent> stored)
	{
		lock (_sessionLock)
		{
			_pendingInboundDeliveredMemoryIntents = new Dictionary<string, CourierInboundDeliveredMemoryIntent>(
				StringComparer.Ordinal);
			_pendingInboundDeliveredMemoryCursor = string.Empty;
			_pendingInboundDeliveredMemoryOverflow = false;
			if (stored == null) return; // Old saves have no optional field.
			if (stored.Count > MaximumPendingInboundDeliveredMemoryIntents)
			{
				// Preserve all unexpected save data, but do not scan/replay an
				// unbounded queue or admit new deliveries until it is inspected.
				_pendingInboundDeliveredMemoryIntents = stored;
				_pendingInboundDeliveredMemoryOverflow = true;
				Log("inbound delivered memory intent overflow on load; automatic recovery disabled");
				return;
			}
			foreach (KeyValuePair<string, CourierInboundDeliveredMemoryIntent> entry in stored)
			{
				CourierInboundDeliveredMemoryIntent intent = entry.Value;
				if (intent == null || string.IsNullOrWhiteSpace(entry.Key)) continue;
				if (!IsValidInboundDeliveredMemoryIntent(intent)
					|| !string.Equals(entry.Key, intent.SessionId, StringComparison.Ordinal))
					intent.Quarantined = true;
				_pendingInboundDeliveredMemoryIntents[entry.Key] = intent;
			}
		}
	}

	private static bool IsValidInboundDeliveredMemoryIntent(CourierInboundDeliveredMemoryIntent intent)
		=> intent != null
			&& !string.IsNullOrWhiteSpace(intent.SessionId)
			&& !string.IsNullOrWhiteSpace(intent.SenderHeroId)
			&& !string.IsNullOrWhiteSpace(intent.HistoryLine)
			&& !string.IsNullOrWhiteSpace(intent.DeliveryFactText)
			&& intent.SessionId.Length <= 256
			&& intent.SenderHeroId.Length <= 256
			&& intent.HistoryLine.Length <= 32768
			&& intent.DeliveryFactText.Length <= 32768
			&& intent.OriginGameDay >= 0
			&& intent.OriginGameHour >= 0 && intent.OriginGameHour <= 23;

	private bool TryReserveInboundDeliveredMemoryIntent(
		CourierSession session, Hero sender, string historyLine, string deliveryFactText)
	{
		if (session == null) return false;
		CourierInboundDeliveredMemoryIntent candidate = new CourierInboundDeliveredMemoryIntent
		{
			SessionId = session.Id,
			SenderHeroId = string.IsNullOrWhiteSpace(SafeHeroId(sender))
				? (session.SenderHeroId ?? string.Empty).Trim()
				: SafeHeroId(sender),
			HistoryLine = historyLine,
			DeliveryFactText = deliveryFactText,
			OriginGameDay = Math.Max(0, (int)CampaignTime.Now.ToDays),
			OriginGameHour = Math.Max(0, Math.Min(23, (int)(CampaignTime.Now.ToHours % 24))),
			OriginLocationId = MyBehavior.ResolveCurrentMemorySceneLabelForExternal()
		};
		if (!IsValidInboundDeliveredMemoryIntent(candidate)) return false;
		lock (_sessionLock)
		{
			if (_pendingInboundDeliveredMemoryOverflow) return false;
			if (_pendingInboundDeliveredMemoryIntents.TryGetValue(candidate.SessionId, out var existing))
				return existing != null && !existing.Quarantined
					&& string.Equals(existing.SenderHeroId, candidate.SenderHeroId, StringComparison.Ordinal)
					&& string.Equals(existing.HistoryLine, candidate.HistoryLine, StringComparison.Ordinal)
					&& string.Equals(existing.DeliveryFactText, candidate.DeliveryFactText, StringComparison.Ordinal);
			if (_pendingInboundDeliveredMemoryIntents.Count >= MaximumPendingInboundDeliveredMemoryIntents)
				return false;
			_pendingInboundDeliveredMemoryIntents.Add(candidate.SessionId, candidate);
			return true;
		}
	}

	private void ProcessOnePendingInboundDeliveredMemoryIntent()
	{
		if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
			|| _pendingInboundDeliveredMemoryOverflow) return;
		CourierInboundDeliveredMemoryIntent first = null, next = null;
		lock (_sessionLock)
		{
			foreach (CourierInboundDeliveredMemoryIntent candidate in _pendingInboundDeliveredMemoryIntents.Values)
			{
				if (candidate == null || candidate.Quarantined) continue;
				if (first == null || string.Compare(candidate.SessionId, first.SessionId, StringComparison.Ordinal) < 0)
					first = candidate;
				if (string.Compare(candidate.SessionId, _pendingInboundDeliveredMemoryCursor, StringComparison.Ordinal) > 0
					&& (next == null || string.Compare(candidate.SessionId, next.SessionId, StringComparison.Ordinal) < 0))
					next = candidate;
			}
		}
		CourierInboundDeliveredMemoryIntent intent = next ?? first;
		if (intent == null)
		{
			_pendingInboundDeliveredMemoryCursor = string.Empty;
			return;
		}
		_pendingInboundDeliveredMemoryCursor = intent.SessionId;
		ProcessInboundDeliveredMemoryIntent(intent);
	}

	private void ProcessPendingInboundDeliveredMemoryIntent(string sessionId)
	{
		if (!TWParallel.IsMainThread() || !ReferenceEquals(Instance, this)
			|| _pendingInboundDeliveredMemoryOverflow
			|| string.IsNullOrWhiteSpace(sessionId)) return;
		CourierInboundDeliveredMemoryIntent intent;
		lock (_sessionLock) _pendingInboundDeliveredMemoryIntents.TryGetValue(sessionId, out intent);
		if (intent != null) ProcessInboundDeliveredMemoryIntent(intent);
	}

	private void ProcessInboundDeliveredMemoryIntent(CourierInboundDeliveredMemoryIntent intent)
	{
		if (!IsValidInboundDeliveredMemoryIntent(intent))
		{
			intent.Quarantined = true;
			return;
		}
		if (intent.Attempted)
		{
			InteractionMemoryRecoveryLookupStatus status =
				MyBehavior.GetExternalDialogueHistoryRecoveryStatus(
					intent.RecoveryId, intent.SenderHeroId, intent.MemoryPayloadHash);
			if (status == InteractionMemoryRecoveryLookupStatus.Completed)
			{
				lock (_sessionLock) _pendingInboundDeliveredMemoryIntents.Remove(intent.SessionId);
			}
			else if (status != InteractionMemoryRecoveryLookupStatus.Pending
				&& status != InteractionMemoryRecoveryLookupStatus.Unavailable)
			{
				// Missing after an attempted commit may mean a partial write. Never replay it.
				intent.Quarantined = true;
				Log("inbound delivered memory intent quarantined session=" + intent.SessionId
					+ " status=" + status);
			}
			return;
		}
		InteractionMemoryCommit commit = new InteractionMemoryCommit(
			"courier-inbound-delivered:" + intent.SessionId,
			InteractionChannel.Courier,
			intent.SessionId,
			intent.SenderHeroId,
			string.Empty,
			intent.HistoryLine,
			new[] { new FactRecord("courier_delivery", intent.SenderHeroId, intent.DeliveryFactText) },
			0L, 0L, "courier-inbound-delivered:" + intent.SessionId,
			intent.OriginGameDay, intent.OriginGameHour,
			intent.OriginLocationId ?? string.Empty,
			-1, -1, string.Empty);
		if (!MyBehavior.TryPrepareExternalDialogueHistoryRecoveryIdentity(
			commit, isNonHero: false, npcName: null,
			out string recoveryId, out string payloadHash, out string preparationError))
		{
			if (preparationError != "memory_owner_missing"
				&& preparationError != "memory_recovery_not_activated")
			{
				intent.Quarantined = true;
				Log("inbound delivered memory intent preparation quarantined session=" + intent.SessionId
					+ " error=" + preparationError);
			}
			return; // Owner unavailable: preserve the intent; never redeliver the letter.
		}
		intent.RecoveryId = recoveryId;
		intent.MemoryPayloadHash = payloadHash;
		intent.Attempted = true;
		MemoryCommitResult result = MyBehavior.CommitExternalDialogueHistoryRecoverable(
			commit, isNonHero: false, npcName: null);
		if (result.HistoryWritten)
		{
			lock (_sessionLock) _pendingInboundDeliveredMemoryIntents.Remove(intent.SessionId);
		}
		else if (MyBehavior.GetExternalDialogueHistoryRecoveryStatus(
			intent.RecoveryId, intent.SenderHeroId, intent.MemoryPayloadHash)
			== InteractionMemoryRecoveryLookupStatus.Missing)
		{
			// A synchronous Missing means Begin never accepted this attempt.
			// A later Missing from a persisted attempt is ambiguous and quarantined above.
			intent.Attempted = false;
			intent.RecoveryId = string.Empty;
			intent.MemoryPayloadHash = string.Empty;
		}
	}
}
