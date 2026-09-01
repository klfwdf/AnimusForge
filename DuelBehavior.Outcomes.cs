using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class DuelBehavior
{
	// Runtime-only observation sidecar. It never owns Mission, death, Economy,
	// Memory, save loading, or recovery, and it cannot replay any Duel effect.
	private static readonly object _duelOutcomeOwnerSync = new object();

	private static DuelOutcomeOwner _duelOutcomeOwner = new DuelOutcomeOwner();

	private static long _duelOutcomeSerial;

	private const int DuelOutcomeSubjectIndexCapacity = 256;

	private static readonly object _duelOutcomeSubjectIndexSync = new object();

	private static readonly Dictionary<string, string> _latestDuelOutcomeIdsBySubject =
		new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly Queue<KeyValuePair<string, string>> _duelOutcomeSubjectIndexOrder =
		new Queue<KeyValuePair<string, string>>();

	private DuelOutcomeStartIdentity _activeDuelOutcomeStart;

	private static bool TryBeginDuelOutcome(
		string subjectId,
		DuelSessionKind sessionKind,
		string source,
		out DuelOutcomeStartIdentity startIdentity)
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
			long serial = Interlocked.Increment(ref _duelOutcomeSerial);
			string serialToken = serial.ToString(CultureInfo.InvariantCulture);
			string generationToken = generation.ToString(CultureInfo.InvariantCulture);
			string kindToken = ((int)sessionKind).ToString(CultureInfo.InvariantCulture);
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
				out DuelOutcomeRequestIdentity request,
				out string requestError))
			{
				DiscardUnboundDuelArtifacts(normalizedSubject);
				LogDuelOutcomeFailure("request", requestError, source);
				return false;
			}

			DuelOutcomeOperationStatus queued = QueueDuelOutcomeRequest(
				request,
				out _,
				out string queueError);
			if (queued != DuelOutcomeOperationStatus.Accepted
				&& queued != DuelOutcomeOperationStatus.Duplicate)
			{
				DiscardUnboundDuelArtifacts(normalizedSubject);
				LogDuelOutcomeFailure("queue", queueError, source);
				return false;
			}

			string duelSessionId = DuelOutcomeFingerprint.Hash(
				"AnimusForge.DuelOutcome.HostSession.v1",
				duelId,
				kindToken,
				NormalizeDuelOutcomeSource(source));
			if (!DuelOutcomeStartIdentity.TryCreate(
				request,
				duelSessionId,
				sessionKind,
				out DuelOutcomeStartIdentity start,
				out string startIdentityError))
			{
				_duelOutcomeOwner.Cancel(request, "start_identity_invalid", out _, out _);
				DiscardUnboundDuelArtifacts(normalizedSubject);
				LogDuelOutcomeFailure("start_identity", startIdentityError, source);
				return false;
			}

			DuelOutcomeOperationStatus started = _duelOutcomeOwner.Start(
				start,
				out _,
				out string startError);
			if (started != DuelOutcomeOperationStatus.Accepted
				&& started != DuelOutcomeOperationStatus.Duplicate)
			{
				_duelOutcomeOwner.Cancel(request, "start_failed", out _, out _);
				DiscardUnboundDuelArtifacts(normalizedSubject);
				LogDuelOutcomeFailure("start", startError, source);
				return false;
			}

			startIdentity = start;
			BindPendingDuelArtifacts(normalizedSubject, duelId);
			IndexDuelOutcome(normalizedSubject, duelId);
			Logger.Log(
				"DuelOutcome",
				"started duelId=" + duelId
				+ " subject=" + normalizedSubject
				+ " kind=" + sessionKind
				+ " source=" + NormalizeDuelOutcomeSource(source)
				+ " recovery=NOT_RECOVERABLE");
			return true;
		}
		catch (Exception ex)
		{
			DiscardUnboundDuelArtifacts(normalizedSubject);
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

	private static void ClearDuelOutcomeSubjectIndex()
	{
		lock (_duelOutcomeSubjectIndexSync)
		{
			_latestDuelOutcomeIdsBySubject.Clear();
			_duelOutcomeSubjectIndexOrder.Clear();
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
