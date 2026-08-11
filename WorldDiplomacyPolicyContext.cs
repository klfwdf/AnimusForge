using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AnimusForge;

internal static class WorldDiplomacyPolicyContext
{
	private const int MaxPolicyRecords = 200;
	private const int OwnPolicyLimit = 3;
	private const int ForeignPressureLimit = 3;
	private static readonly long RefreshIntervalTicks = Math.Max(1L, Stopwatch.Frequency);
	private static readonly object CacheLock = new object();
	private static readonly Dictionary<string, string> SnapshotByKingdomId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, List<NpcRulerPolicyRecord>> _ownPoliciesByKingdomId = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, List<NpcRulerPolicyRecord>> _foreignPressureByKingdomId = new Dictionary<string, List<NpcRulerPolicyRecord>>(StringComparer.OrdinalIgnoreCase);
	private static List<NpcRulerPolicyRecord> _activeRecords = new List<NpcRulerPolicyRecord>();
	private static long _runtimeGeneration;
	private static long _nextRefreshTimestamp;
	private static ulong _sourceSignature;

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

				foreach (NpcRulerPolicyEffectDto effect in (record.Effects ?? new List<NpcRulerPolicyEffectDto>())
					.Where(IsActiveEffect)
					.Where(item => !string.IsNullOrWhiteSpace(item.TargetKingdomId)
						&& !string.Equals(item.TargetKingdomId.Trim(), issuerId, StringComparison.OrdinalIgnoreCase))
					.GroupBy(item => item.TargetKingdomId.Trim(), StringComparer.OrdinalIgnoreCase)
					.Select(group => group.OrderBy(item => item.EffectId ?? "", StringComparer.OrdinalIgnoreCase).First()))
				{
					string targetId = effect.TargetKingdomId.Trim();
					result.Add(new WorldDiplomacyPolicySignalSnapshot
					{
						SignalKey = "policy:" + record.PolicyId.Trim() + ":" + targetId,
						PolicyId = record.PolicyId.Trim(),
						PolicyKind = string.IsNullOrWhiteSpace(record.PolicyKind) ? "kingdom" : record.PolicyKind.Trim(),
						PolicyName = Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 80),
						PolicySummary = Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 260),
						IssuerKingdomId = issuerId,
						IssuerKingdomName = Limit(FirstNonEmpty(record.KingdomName, issuerId), 60),
						TargetKingdomId = targetId,
						TargetKingdomName = Limit(FirstNonEmpty(effect.TargetKingdomName, targetId), 60),
						DirectEffect = Limit(FirstNonEmpty(effect.Reason, record.ImpactSummary), 180),
						PublishedDay = Math.Max(0, record.Day)
					});
				}
			}
			return result.OrderBy(item => item.PublishedDay).ThenBy(item => item.SignalKey, StringComparer.OrdinalIgnoreCase).ToList();
		}
	}

	public static bool IsForeignPolicySignalActive(string policyId, string ownerKingdomId, string affectedKingdomId)
	{
		string normalizedPolicyId = (policyId ?? "").Trim();
		string normalizedOwnerId = (ownerKingdomId ?? "").Trim();
		string normalizedAffectedId = (affectedKingdomId ?? "").Trim();
		if (normalizedPolicyId.Length == 0 || normalizedOwnerId.Length == 0 || normalizedAffectedId.Length == 0)
		{
			return false;
		}

		try
		{
			NpcRulerPolicyRecord record = NpcRulerPolicyBehavior.GetPolicyRecordForExternal(normalizedPolicyId);
			return record != null
				&& string.Equals((record.PolicyId ?? "").Trim(), normalizedPolicyId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals((record.KingdomId ?? "").Trim(), normalizedOwnerId, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(record.AgendaStatus)
					|| string.Equals(record.AgendaStatus.Trim(), "active", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(record.AgendaStatus.Trim(), "expiry_vote_pending", StringComparison.OrdinalIgnoreCase))
				&& (string.IsNullOrWhiteSpace(record.PolicyKind)
					|| string.Equals(record.PolicyKind.Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
				&& (record.Effects ?? new List<NpcRulerPolicyEffectDto>()).Any(effect => IsActiveEffect(effect)
					&& string.Equals((effect.TargetKingdomId ?? "").Trim(), normalizedAffectedId, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(long afterSequence = 0L, int maxCount = 256)
	{
		return CustomPolicyBehavior.GetPublishedPolicyArtifactLedgerForExternal(afterSequence, maxCount);
	}

	public static string GetPublishedPolicyHistoryLedgerId()
	{
		return CustomPolicyBehavior.GetPublishedPolicyArtifactLedgerIdForExternal();
	}

	public static long GetPublishedPolicyHistoryCurrentSequence()
	{
		return CustomPolicyBehavior.GetPublishedPolicyArtifactCurrentSequenceForExternal();
	}

	public static bool TryAcknowledgePublishedPolicyHistoryThrough(long sequence)
	{
		return CustomPolicyBehavior.TryAcknowledgePublishedPolicyArtifactConsumedThroughForExternal(sequence);
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
		}
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

			foreach (string targetId in (record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
				.Where(IsActiveEffect)
				.Select(effect => (effect.TargetKingdomId ?? "").Trim())
				.Where(targetId => !string.IsNullOrWhiteSpace(targetId)
					&& !string.Equals(targetId, ownerId, StringComparison.OrdinalIgnoreCase))
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
				sb.AppendLine("- 《" + Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 60) + "》："
					+ Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 180)
					+ (string.IsNullOrWhiteSpace(record.ImpactSummary) ? "" : "；政策影响：" + Limit(record.ImpactSummary, 120)));
			}
		}

		if (foreign.Count > 0)
		{
			sb.AppendLine("【外国政策对本国的直接压力】");
			foreach (NpcRulerPolicyRecord record in foreign)
			{
				NpcRulerPolicyEffectDto direct = (record.Effects ?? new List<NpcRulerPolicyEffectDto>())
					.Where(effect => IsActiveEffect(effect)
						&& string.Equals((effect.TargetKingdomId ?? "").Trim(), targetId, StringComparison.OrdinalIgnoreCase))
					.OrderBy(effect => effect.EffectId ?? "", StringComparer.OrdinalIgnoreCase)
					.FirstOrDefault();
				sb.AppendLine("- " + Limit(FirstNonEmpty(record.KingdomName, record.KingdomId), 50)
					+ "《" + Limit(FirstNonEmpty(record.PolicyName, "未命名政策"), 60) + "》："
					+ Limit(FirstNonEmpty(record.PolicyDigest, record.PolicyContent), 150)
					+ (string.IsNullOrWhiteSpace(direct?.Reason) ? "" : "；直接措施：" + Limit(direct.Reason, 100)));
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
		return effect != null && !effect.IsEnded && effect.RemainingDays > 0;
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
			AppendHash(ref hash, record?.Day.ToString());
			AppendHash(ref hash, record?.CreatedUtcTicks.ToString());
			foreach (NpcRulerPolicyEffectDto effect in (record?.Effects ?? new List<NpcRulerPolicyEffectDto>())
				.OrderBy(item => item?.EffectId ?? "", StringComparer.OrdinalIgnoreCase)
				.ThenBy(item => item?.TargetKingdomId ?? "", StringComparer.OrdinalIgnoreCase))
			{
				AppendHash(ref hash, effect?.EffectId);
				AppendHash(ref hash, effect?.TargetKingdomId);
				AppendHash(ref hash, effect?.RemainingDays.ToString());
				AppendHash(ref hash, effect?.IsEnded == true ? "1" : "0");
				AppendHash(ref hash, effect?.Reason);
			}
		}
		return hash;
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
	public string PolicyKind { get; set; } = "kingdom";
	public string PolicyName { get; set; } = "";
	public string PolicySummary { get; set; } = "";
	public string IssuerKingdomId { get; set; } = "";
	public string IssuerKingdomName { get; set; } = "";
	public string TargetKingdomId { get; set; } = "";
	public string TargetKingdomName { get; set; } = "";
	public string DirectEffect { get; set; } = "";
	public int PublishedDay { get; set; }
}
