using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.PolicyEffects;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Immutable adapter from the unified new-policy history into the world-diplomacy
/// canonical-history ingestion contract. It does not own or persist policy state.
/// </summary>
internal sealed class PublishedPolicyArtifactLedgerEntry
{
	internal PublishedPolicyArtifactLedgerEntry(
		long sequence,
		long revision,
		string policyId,
		string eventKind,
		int occurredDay,
		string gameDate,
		long createdUtcTicks,
		string scopeKind,
		string kingdomId,
		string kingdomName,
		string policyName,
		string publishedText,
		string contentHash)
	{
		Sequence = sequence;
		Revision = revision;
		PolicyId = policyId ?? string.Empty;
		EventKind = eventKind ?? string.Empty;
		OccurredDay = occurredDay;
		GameDate = gameDate ?? string.Empty;
		CreatedUtcTicks = createdUtcTicks;
		ScopeKind = scopeKind ?? string.Empty;
		KingdomId = kingdomId ?? string.Empty;
		KingdomName = kingdomName ?? string.Empty;
		PolicyName = policyName ?? string.Empty;
		PublishedText = publishedText ?? string.Empty;
		ContentHash = contentHash ?? string.Empty;
	}

	public long Sequence { get; }
	public long Revision { get; }
	public string PolicyId { get; }
	public string EventKind { get; }
	public int OccurredDay { get; }
	public string GameDate { get; }
	public long CreatedUtcTicks { get; }
	public string ScopeKind { get; }
	public string KingdomId { get; }
	public string KingdomName { get; }
	public string PolicyName { get; }
	public string PublishedText { get; }
	public string ContentHash { get; }
}

internal static class WorldDiplomacyPolicyContext
{
	private const int MaxPolicyRecords = 200;
	private const int MaxPublishedPolicyArtifacts = 400;
	private const int OwnPolicyLimit = 3;
	private const int ForeignPressureLimit = 3;
	private const string UnifiedPolicyHistoryLedgerPrefix = "unified-policy-history-v1:";
	private static readonly long RefreshIntervalTicks = Math.Max(1L, Stopwatch.Frequency);
	private static readonly object CacheLock = new object();
	private static readonly Dictionary<string, string> SnapshotByKingdomId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, List<NpcRulerPolicyRecord>> _ownPoliciesByKingdomId = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, List<NpcRulerPolicyRecord>> _foreignPressureByKingdomId = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
	private static List<NpcRulerPolicyRecord> _activeRecords = new List<NpcRulerPolicyRecord>();
	private static long _runtimeGeneration;
	private static long _nextRefreshTimestamp;
	private static ulong _sourceSignature;
	private static long _publishedHistoryRuntimeGeneration = -1L;
	private static long _publishedHistoryNextRefreshTimestamp;
	private static string _publishedHistoryLedgerId = string.Empty;
	private static List<PublishedPolicyArtifactLedgerEntry> _publishedPolicyArtifacts = new List<PublishedPolicyArtifactLedgerEntry>();

	public static string BuildSnapshot(string kingdomId)
	{
		string targetId = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(targetId))
		{
			return "";
		}

		lock (CacheLock)
		{
			RefreshSourceIfNeeded();
			if (SnapshotByKingdomId.TryGetValue(targetId, out string cached))
			{
				return cached ?? "";
			}

			string snapshot = BuildSnapshotCore(targetId);
			SnapshotByKingdomId[targetId] = snapshot;
			return snapshot;
		}
	}

	public static List<WorldDiplomacyPolicySignalSnapshot> GetForeignPolicySignals()
	{
		lock (CacheLock)
		{
			RefreshSourceIfNeeded();
			List<WorldDiplomacyPolicySignalSnapshot> result = new List<WorldDiplomacyPolicySignalSnapshot>();
			foreach (NpcRulerPolicyRecord record in _activeRecords)
			{
				string issuerId = (record?.KingdomId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(issuerId) || string.IsNullOrWhiteSpace(record?.PolicyId))
				{
					continue;
				}

				foreach (string targetId in EnumerateActiveTargetKingdomIds(record)
					.Where(id => !string.Equals(id, issuerId, StringComparison.OrdinalIgnoreCase))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
				{
					NpcRulerPolicyEffectDto metadata = FindEffectMetadata(record, targetId);
					result.Add(new WorldDiplomacyPolicySignalSnapshot
					{
						SignalKey = "policy:" + record.PolicyId.Trim() + ":" + targetId,
						PolicyId = record.PolicyId.Trim(),
						PolicyKind = string.IsNullOrWhiteSpace(record.PolicyKind) ? PolicyEffectScopes.Kingdom : record.PolicyKind.Trim(),
						PolicyName = Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 80),
						PolicySummary = Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 260),
						IssuerKingdomId = issuerId,
						IssuerKingdomName = Limit(FirstNonEmpty(record.KingdomName, issuerId), 60),
						TargetKingdomId = targetId,
						TargetKingdomName = Limit(FirstNonEmpty(metadata?.TargetKingdomName, targetId), 60),
						DirectEffect = Limit(BuildEffectSummary(record, targetId), 180),
						PublishedDay = Math.Max(0, record.Day)
					});
				}
			}
			return result.OrderBy(item => item.PublishedDay).ThenBy(item => item.SignalKey, StringComparer.OrdinalIgnoreCase).ToList();
		}
	}

	public static string GetPublishedPolicyHistoryLedgerId()
	{
		lock (CacheLock)
		{
			RefreshPublishedPolicyHistoryIfNeeded();
			return _publishedHistoryLedgerId;
		}
	}

	public static bool IsForeignPolicySignalActive(string policyId, string ownerKingdomId, string affectedKingdomId)
	{
		string normalizedPolicyId = (policyId ?? string.Empty).Trim();
		string normalizedOwnerId = (ownerKingdomId ?? string.Empty).Trim();
		string normalizedAffectedId = (affectedKingdomId ?? string.Empty).Trim();
		if (normalizedPolicyId.Length == 0 || normalizedOwnerId.Length == 0 || normalizedAffectedId.Length == 0)
		{
			return false;
		}

		lock (CacheLock)
		{
			RefreshSourceIfNeeded();
			NpcRulerPolicyRecord record = _activeRecords.FirstOrDefault(candidate => candidate != null
				&& string.Equals((candidate.PolicyId ?? string.Empty).Trim(), normalizedPolicyId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals((candidate.KingdomId ?? string.Empty).Trim(), normalizedOwnerId, StringComparison.OrdinalIgnoreCase));
			return record != null
				&& (string.IsNullOrWhiteSpace(record.AgendaStatus)
					|| string.Equals(record.AgendaStatus.Trim(), "active", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(record.AgendaStatus.Trim(), "expiry_vote_pending", StringComparison.OrdinalIgnoreCase))
				&& (string.IsNullOrWhiteSpace(record.PolicyKind)
					|| string.Equals(record.PolicyKind.Trim(), PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase))
				&& EnumerateActiveTargetKingdomIds(record)
					.Any(targetId => string.Equals(targetId, normalizedAffectedId, StringComparison.OrdinalIgnoreCase));
		}
	}

	public static long GetPublishedPolicyHistoryCurrentSequence()
	{
		lock (CacheLock)
		{
			RefreshPublishedPolicyHistoryIfNeeded();
			return _publishedPolicyArtifacts.Count;
		}
	}

	public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(
		long afterSequence = 0L,
		int maxCount = 256)
	{
		lock (CacheLock)
		{
			RefreshPublishedPolicyHistoryIfNeeded();
			long cursor = Math.Max(0L, afterSequence);
			int limit = Math.Max(1, Math.Min(1024, maxCount));
			return _publishedPolicyArtifacts
				.Where(entry => entry != null && entry.Sequence > cursor)
				.OrderBy(entry => entry.Sequence)
				.Take(limit)
				.ToList();
		}
	}

	public static bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence)
	{
		lock (CacheLock)
		{
			RefreshPublishedPolicyHistoryIfNeeded();
			return throughSequence >= 0L && throughSequence <= _publishedPolicyArtifacts.Count;
		}
	}

	public static void Clear()
	{
		lock (CacheLock)
		{
			_runtimeGeneration = 0L;
			_nextRefreshTimestamp = 0L;
			_sourceSignature = 0UL;
			_ownPoliciesByKingdomId.Clear();
			_foreignPressureByKingdomId.Clear();
			_activeRecords.Clear();
			SnapshotByKingdomId.Clear();
			_publishedHistoryRuntimeGeneration = -1L;
			_publishedHistoryNextRefreshTimestamp = 0L;
			_publishedHistoryLedgerId = string.Empty;
			_publishedPolicyArtifacts.Clear();
		}
	}

	private static void RefreshPublishedPolicyHistoryIfNeeded()
	{
		long generation = SaveRuntimeGuard.CurrentGeneration;
		long now = Stopwatch.GetTimestamp();
		if (_publishedHistoryRuntimeGeneration == generation && now < _publishedHistoryNextRefreshTimestamp)
		{
			return;
		}

		_publishedHistoryRuntimeGeneration = generation;
		_publishedHistoryNextRefreshTimestamp = now + RefreshIntervalTicks;
		if (!NpcRulerPolicyBehavior.TryCaptureUnifiedPolicyHistorySnapshotForExternal(
			out List<NpcPolicyHistoryEntry> historyEntries,
			out _))
		{
			_publishedHistoryLedgerId = string.Empty;
			_publishedPolicyArtifacts = new List<PublishedPolicyArtifactLedgerEntry>();
			return;
		}

		List<NpcPolicyHistoryEntry> ordered = (historyEntries ?? new List<NpcPolicyHistoryEntry>())
			.Where(entry => entry != null
				&& !string.IsNullOrWhiteSpace(entry.EntryId)
				&& !string.IsNullOrWhiteSpace(entry.PolicyName))
			.OrderByDescending(entry => entry.PublishedDay)
			.ThenByDescending(entry => entry.CreatedUtcTicks)
			.ThenBy(entry => entry.SourceKind ?? string.Empty, StringComparer.Ordinal)
			.ThenBy(entry => entry.EntryId ?? string.Empty, StringComparer.Ordinal)
			.Take(MaxPublishedPolicyArtifacts)
			.OrderBy(entry => entry.PublishedDay)
			.ThenBy(entry => entry.CreatedUtcTicks)
			.ThenBy(entry => entry.SourceKind ?? string.Empty, StringComparer.Ordinal)
			.ThenBy(entry => entry.EntryId ?? string.Empty, StringComparer.Ordinal)
			.ToList();

		List<PublishedPolicyArtifactLedgerEntry> artifacts = new List<PublishedPolicyArtifactLedgerEntry>(ordered.Count);
		for (int index = 0; index < ordered.Count; index++)
		{
			NpcPolicyHistoryEntry entry = ordered[index];
			string policyId = FirstNonEmpty(entry.SourceKind, "policy") + ":" + entry.EntryId.Trim();
			bool isCurrent = string.Equals(entry.HistoryBucket, PolicyHistoryRetrievalService.CurrentBucket, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(entry.PolicyStatus, "active", StringComparison.OrdinalIgnoreCase);
			string publishedText = BuildPublishedPolicyArtifactText(entry);
			ulong contentSignature = 1469598103934665603UL;
			AppendHash(ref contentSignature, policyId);
			AppendHash(ref contentSignature, entry.ScopeKind);
			AppendHash(ref contentSignature, entry.OwnerKingdomId);
			AppendHash(ref contentSignature, entry.OwnerKingdomName);
			AppendHash(ref contentSignature, entry.PolicyName);
			AppendHash(ref contentSignature, publishedText);
			artifacts.Add(new PublishedPolicyArtifactLedgerEntry(
				sequence: index + 1L,
				revision: isCurrent ? 1L : 2L,
				policyId: policyId,
				eventKind: isCurrent ? "policy_published" : "policy_snapshot",
				occurredDay: Math.Max(0, entry.PublishedDay),
				gameDate: string.Empty,
				createdUtcTicks: Math.Max(0L, entry.CreatedUtcTicks),
				scopeKind: entry.ScopeKind,
				kingdomId: entry.OwnerKingdomId,
				kingdomName: entry.OwnerKingdomName,
				policyName: entry.PolicyName,
				publishedText: publishedText,
				contentHash: contentSignature.ToString("X16", CultureInfo.InvariantCulture)));
		}

		ulong ledgerSignature = 1469598103934665603UL;
		foreach (PublishedPolicyArtifactLedgerEntry artifact in artifacts)
		{
			AppendHash(ref ledgerSignature, artifact.PolicyId);
			AppendHash(ref ledgerSignature, artifact.EventKind);
			AppendHash(ref ledgerSignature, artifact.ContentHash);
		}
		_publishedPolicyArtifacts = artifacts;
		_publishedHistoryLedgerId = UnifiedPolicyHistoryLedgerPrefix
			+ ledgerSignature.ToString("X16", CultureInfo.InvariantCulture);
	}

	private static string BuildPublishedPolicyArtifactText(NpcPolicyHistoryEntry entry)
	{
		StringBuilder text = new StringBuilder();
		text.Append("政策状态=").Append(FirstNonEmpty(entry?.RawPolicyStatus, entry?.PolicyStatus, "unknown"));
		if (!string.IsNullOrWhiteSpace(entry?.PolicyContent))
		{
			text.AppendLine().Append(entry.PolicyContent.Trim());
		}
		if (!string.IsNullOrWhiteSpace(entry?.ImpactSummary))
		{
			text.AppendLine().Append("影响：").Append(entry.ImpactSummary.Trim());
		}
		List<string> effects = (entry?.EffectSummaries ?? new List<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (effects.Count > 0)
		{
			text.AppendLine().Append("机械效果：").Append(string.Join("；", effects));
		}
		return text.ToString().Trim();
	}

	private static void RefreshSourceIfNeeded()
	{
		long generation = SaveRuntimeGuard.CurrentGeneration;
		long now = Stopwatch.GetTimestamp();
		if (_runtimeGeneration == generation && now < _nextRefreshTimestamp)
		{
			return;
		}

		List<NpcRulerPolicyRecord> activeRecords;
		try
		{
			activeRecords = NpcRulerPolicyBehavior.GetRecentPolicyRecordsForExternal(null, MaxPolicyRecords)
				.Where(HasActiveEffect)
				.OrderByDescending(record => record.Day)
				.ThenByDescending(record => record.CreatedUtcTicks)
				.ThenBy(record => record.PolicyId ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			activeRecords = new List<NpcRulerPolicyRecord>();
		}

		ulong signature = ComputeSourceSignature(activeRecords);
		_runtimeGeneration = generation;
		_nextRefreshTimestamp = now + RefreshIntervalTicks;
		if (signature == _sourceSignature)
		{
			return;
		}

		_sourceSignature = signature;
		_activeRecords = activeRecords;
		RebuildIndexes(activeRecords);
		SnapshotByKingdomId.Clear();
	}

	private static void RebuildIndexes(List<NpcRulerPolicyRecord> activeRecords)
	{
		Dictionary<string, List<NpcRulerPolicyRecord>> own = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<NpcRulerPolicyRecord>> foreign = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
		foreach (NpcRulerPolicyRecord record in activeRecords ?? new List<NpcRulerPolicyRecord>())
		{
			string ownerId = (record?.KingdomId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(ownerId))
			{
				AddIndexedRecord(own, ownerId, record);
			}

			foreach (string targetId in EnumerateActiveTargetKingdomIds(record)
				.Where(targetId => !string.Equals(targetId, ownerId, StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				AddIndexedRecord(foreign, targetId, record);
			}
		}

		_ownPoliciesByKingdomId = own;
		_foreignPressureByKingdomId = foreign;
	}

	private static void AddIndexedRecord(Dictionary<string, List<NpcRulerPolicyRecord>> index, string kingdomId, NpcRulerPolicyRecord record)
	{
		if (!index.TryGetValue(kingdomId, out List<NpcRulerPolicyRecord> records))
		{
			records = new List<NpcRulerPolicyRecord>();
			index[kingdomId] = records;
		}
		records.Add(record);
	}

	private static string BuildSnapshotCore(string targetId)
	{
		List<NpcRulerPolicyRecord> own = _ownPoliciesByKingdomId.TryGetValue(targetId, out List<NpcRulerPolicyRecord> ownRecords)
			? ownRecords.Take(OwnPolicyLimit).ToList()
			: new List<NpcRulerPolicyRecord>();
		List<NpcRulerPolicyRecord> foreign = _foreignPressureByKingdomId.TryGetValue(targetId, out List<NpcRulerPolicyRecord> foreignRecords)
			? foreignRecords.Take(ForeignPressureLimit).ToList()
			: new List<NpcRulerPolicyRecord>();

		StringBuilder sb = new StringBuilder();
		if (own.Count > 0)
		{
			sb.AppendLine("【本国当前公开政策】");
			foreach (NpcRulerPolicyRecord record in own)
			{
				string effectSummary = BuildEffectSummary(record, null);
				sb.AppendLine("- 《" + Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 60) + "》："
					+ Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 180)
					+ (string.IsNullOrWhiteSpace(effectSummary) ? "" : "；政策影响：" + Limit(effectSummary, 120)));
			}
		}

		if (foreign.Count > 0)
		{
			sb.AppendLine("【外国政策对本国的直接压力】");
			foreach (NpcRulerPolicyRecord record in foreign)
			{
				string effectSummary = BuildEffectSummary(record, targetId);
				sb.AppendLine("- " + Limit(FirstNonEmpty(record.KingdomName, record.KingdomId), 50)
					+ "《" + Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 60) + "》："
					+ Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 150)
					+ (string.IsNullOrWhiteSpace(effectSummary) ? "" : "；直接措施：" + Limit(effectSummary, 100)));
			}
		}
		return sb.ToString().TrimEnd();
	}

	private static bool HasActiveEffect(NpcRulerPolicyRecord record)
	{
		return record?.Effects?.Any(IsActiveEffect) == true;
	}

	private static bool IsActiveEffect(NpcRulerPolicyEffectDto effect)
	{
		return effect?.ModuleEffects?.Any(IsActiveContinuousInstance) == true;
	}

	private static bool IsActiveContinuousInstance(PolicyEffectInstanceSaveData instance)
	{
		return instance?.LifecycleState == PolicyEffectLifecycleState.Active
			&& PolicyEffectModuleCatalog.TryGetCanonical(instance.ModuleId, out IPolicyEffectModule module)
			&& module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot;
	}

	private static bool IsDescribableInstance(NpcRulerPolicyRecord record, PolicyEffectInstanceSaveData instance)
	{
		if (instance == null
			|| !PolicyEffectModuleCatalog.TryGetCanonical(instance.ModuleId, out IPolicyEffectModule module))
		{
			return false;
		}
		if (instance.LifecycleState == PolicyEffectLifecycleState.Active
			&& module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot)
		{
			return true;
		}
		return module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot
			&& instance.LifecycleState == PolicyEffectLifecycleState.Completed
			&& HasCompletedReceipt(record, instance);
	}

	private static bool HasCompletedReceipt(NpcRulerPolicyRecord record, PolicyEffectInstanceSaveData instance)
	{
		PolicyEffectExecutionReceipt receipt = instance?.ExecutionReceipt;
		if (IsAppliedReceipt(receipt))
		{
			return true;
		}
		return (record?.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
			.Any(item => IsAppliedReceipt(item)
				&& string.Equals(item.InstanceId, instance?.InstanceId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(item.ModuleId, instance?.ModuleId, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsAppliedReceipt(PolicyEffectExecutionReceipt receipt)
	{
		return receipt != null
			&& (receipt.Status == PolicyEffectExecutionStatus.Applied
				|| receipt.Status == PolicyEffectExecutionStatus.AlreadyApplied);
	}

	private static IEnumerable<string> EnumerateActiveTargetKingdomIds(NpcRulerPolicyRecord record)
	{
		foreach (NpcRulerPolicyEffectDto effect in record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
		{
			foreach (PolicyEffectInstanceSaveData instance in (effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Where(IsActiveContinuousInstance))
			{
				foreach (string kingdomId in instance.TargetSet?.KingdomIds ?? new List<string>())
				{
					string normalized = (kingdomId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(normalized))
					{
						yield return normalized;
					}
				}
			}
		}
	}

	private static NpcRulerPolicyEffectDto FindEffectMetadata(NpcRulerPolicyRecord record, string targetKingdomId)
	{
		return (record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
			.Where(effect => effect != null && (effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Any(instance => IsActiveContinuousInstance(instance)
					&& (instance.TargetSet?.KingdomIds ?? new List<string>())
						.Any(id => string.Equals((id ?? "").Trim(), targetKingdomId, StringComparison.OrdinalIgnoreCase))))
			.OrderBy(effect => effect.EffectId ?? "", StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault();
	}

	private static string BuildEffectSummary(NpcRulerPolicyRecord record, string targetKingdomId)
	{
		return string.Join("；", (record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
			.SelectMany(effect => effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			.Where(instance => IsDescribableInstance(record, instance))
			.Where(instance => string.IsNullOrWhiteSpace(targetKingdomId)
				|| (instance.TargetSet?.KingdomIds ?? new List<string>())
					.Any(id => string.Equals((id ?? "").Trim(), targetKingdomId, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(instance => instance.ModuleId ?? "", StringComparer.OrdinalIgnoreCase)
			.ThenBy(instance => instance.InstanceId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(PolicyEffectSaveCodec.DescribeInstance)
			.Where(text => !string.IsNullOrWhiteSpace(text))
			.Distinct(StringComparer.Ordinal));
	}

	private static ulong ComputeSourceSignature(IEnumerable<NpcRulerPolicyRecord> records)
	{
		ulong hash = 14695981039346656037UL;
		foreach (NpcRulerPolicyRecord record in records ?? Enumerable.Empty<NpcRulerPolicyRecord>())
		{
			AppendHash(ref hash, record?.PolicyId);
			AppendHash(ref hash, record?.KingdomId);
			AppendHash(ref hash, record?.KingdomName);
			AppendHash(ref hash, record?.AgendaStatus);
			AppendHash(ref hash, record?.PolicyName);
			AppendHash(ref hash, record?.PolicyDigest);
			AppendHash(ref hash, record?.PolicyContent);
			AppendHash(ref hash, record?.ImpactSummary);
			AppendHash(ref hash, record?.Day.ToString(CultureInfo.InvariantCulture));
			AppendHash(ref hash, record?.CreatedUtcTicks.ToString(CultureInfo.InvariantCulture));
			foreach (NpcRulerPolicyEffectDto effect in (record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
				.OrderBy(item => item?.EffectId ?? "", StringComparer.OrdinalIgnoreCase)
				.ThenBy(item => item?.TargetKingdomId ?? "", StringComparer.OrdinalIgnoreCase))
			{
				AppendHash(ref hash, effect?.EffectId);
				AppendHash(ref hash, effect?.TargetKingdomId);
				AppendHash(ref hash, effect?.TargetKingdomName);
				AppendHash(ref hash, effect?.Reason);
				foreach (PolicyEffectInstanceSaveData instance in (effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
					.OrderBy(item => item?.ModuleId ?? "", StringComparer.OrdinalIgnoreCase)
					.ThenBy(item => item?.InstanceId ?? "", StringComparer.OrdinalIgnoreCase))
				{
					AppendInstanceHash(ref hash, instance);
				}
			}
			foreach (PolicyEffectExecutionReceipt receipt in (record?.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
				.OrderBy(item => item?.ModuleId ?? "", StringComparer.OrdinalIgnoreCase)
				.ThenBy(item => item?.InstanceId ?? "", StringComparer.OrdinalIgnoreCase)
				.ThenBy(item => item?.ReceiptId ?? "", StringComparer.OrdinalIgnoreCase))
			{
				AppendReceiptHash(ref hash, receipt);
			}
		}
		return hash;
	}

	private static void AppendInstanceHash(ref ulong hash, PolicyEffectInstanceSaveData instance)
	{
		AppendHash(ref hash, instance?.InstanceId);
		AppendHash(ref hash, instance?.PolicyId);
		AppendHash(ref hash, ResolveCanonicalModuleId(instance?.ModuleId));
		AppendHash(ref hash, ResolveCanonicalModuleId(instance?.SourceModuleId));
		AppendHash(ref hash, instance?.PayloadSchemaVersion.ToString(CultureInfo.InvariantCulture));
		AppendJsonHash(ref hash, instance?.Payload);
		AppendTargetSetHash(ref hash, instance?.TargetSet);
		AppendHash(ref hash, instance?.LifecycleState.ToString());
		AppendHash(ref hash, instance?.StateSchemaVersion.ToString(CultureInfo.InvariantCulture));
		AppendJsonHash(ref hash, instance?.RuntimeState);
		AppendHash(ref hash, instance?.StartDay.ToString("R", CultureInfo.InvariantCulture));
		AppendHash(ref hash, instance?.EndDay.ToString("R", CultureInfo.InvariantCulture));
		AppendHash(ref hash, instance?.SourceScope);
		AppendHash(ref hash, instance?.Reason);
		AppendReceiptHash(ref hash, instance?.ExecutionReceipt);
	}

	private static void AppendReceiptHash(ref ulong hash, PolicyEffectExecutionReceipt receipt)
	{
		AppendHash(ref hash, receipt?.ReceiptId);
		AppendHash(ref hash, receipt?.InstanceId);
		AppendHash(ref hash, receipt?.PolicyId);
		AppendHash(ref hash, ResolveCanonicalModuleId(receipt?.ModuleId));
		AppendTargetSetHash(ref hash, receipt?.TargetSet);
		AppendHash(ref hash, receipt?.Status.ToString());
		AppendHash(ref hash, receipt?.RequestedValue.ToString("R", CultureInfo.InvariantCulture));
		AppendHash(ref hash, receipt?.AppliedValue.ToString("R", CultureInfo.InvariantCulture));
		AppendJsonHash(ref hash, receipt?.RequestedPayload);
		AppendJsonHash(ref hash, receipt?.AppliedPayload);
		AppendHash(ref hash, receipt?.CampaignDay.ToString("R", CultureInfo.InvariantCulture));
		AppendHash(ref hash, receipt?.Message);
	}

	private static void AppendTargetSetHash(ref ulong hash, PolicyEffectCanonicalTargetSet targetSet)
	{
		AppendHash(ref hash, targetSet?.StructureVersion.ToString(CultureInfo.InvariantCulture));
		AppendHashValues(ref hash, targetSet?.SelectorHandles);
		AppendHashValues(ref hash, targetSet?.SettlementIds);
		AppendHashValues(ref hash, targetSet?.TownIds);
		AppendHashValues(ref hash, targetSet?.VillageIds);
		AppendHashValues(ref hash, targetSet?.ClanIds);
		AppendHashValues(ref hash, targetSet?.KingdomIds);
		AppendHashValues(ref hash, targetSet?.ParentSettlementIds);
		AppendHash(ref hash, targetSet?.FollowCurrentRulingClan == true ? "1" : "0");
	}

	private static void AppendHashValues(ref ulong hash, IEnumerable<string> values)
	{
		foreach (string value in (values ?? Enumerable.Empty<string>()).OrderBy(item => item ?? "", StringComparer.Ordinal))
		{
			AppendHash(ref hash, value);
		}
		AppendHash(ref hash, null);
	}

	private static void AppendJsonHash(ref ulong hash, Newtonsoft.Json.Linq.JToken value)
	{
		AppendHash(ref hash, value?.ToString(Formatting.None));
	}

	private static string ResolveCanonicalModuleId(string moduleId)
	{
		return PolicyEffectModuleCatalog.TryGetCanonical(moduleId, out IPolicyEffectModule module)
			? module.Id
			: (moduleId ?? "").Trim();
	}

	private static void AppendHash(ref ulong hash, string value)
	{
		foreach (char ch in value ?? "")
		{
			hash ^= ch;
			hash *= 1099511628211UL;
		}
		hash ^= 255UL;
		hash *= 1099511628211UL;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
	}

	private static string Limit(string value, int maxChars)
	{
		string text = (value ?? "").Trim();
		return text.Length <= maxChars ? text : text.Substring(0, Math.Max(0, maxChars));
	}
}

internal sealed class WorldDiplomacyPolicySignalSnapshot
{
	public string SignalKey { get; set; } = "";
	public string PolicyId { get; set; } = "";
	public string PolicyKind { get; set; } = PolicyEffectScopes.Kingdom;
	public string PolicyName { get; set; } = "";
	public string PolicySummary { get; set; } = "";
	public string IssuerKingdomId { get; set; } = "";
	public string IssuerKingdomName { get; set; } = "";
	public string TargetKingdomId { get; set; } = "";
	public string TargetKingdomName { get; set; } = "";
	public string DirectEffect { get; set; } = "";
	public int PublishedDay { get; set; }
}
