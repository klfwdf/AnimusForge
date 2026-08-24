using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.PolicyEffects;

namespace AnimusForge;

internal sealed class PolicyEffectRuntimeContribution
{
	internal PolicyEffectRuntimeContribution(
		string instanceId,
		string policyId,
		string moduleId,
		string displayName,
		PolicyEffectHook hook,
		PolicyEffectTargetKind targetKind,
		string targetId,
		PolicyEffectAggregationKind aggregation,
		float value)
	{
		if (string.IsNullOrWhiteSpace(instanceId))
		{
			throw new ArgumentException("运行时效果贡献缺少 instanceId。", nameof(instanceId));
		}
		if (string.IsNullOrWhiteSpace(moduleId))
		{
			throw new ArgumentException("运行时效果贡献缺少 moduleId。", nameof(moduleId));
		}
		if (string.IsNullOrWhiteSpace(targetId))
		{
			throw new ArgumentException("运行时效果贡献缺少 targetId。", nameof(targetId));
		}
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			throw new ArgumentOutOfRangeException(nameof(value), "运行时效果贡献必须是有限数字。");
		}

		InstanceId = instanceId.Trim();
		PolicyId = (policyId ?? string.Empty).Trim();
		ModuleId = moduleId.Trim();
		DisplayName = (displayName ?? string.Empty).Trim();
		Hook = hook;
		TargetKind = targetKind;
		TargetId = targetId.Trim();
		Aggregation = aggregation;
		Value = value;
	}

	internal string InstanceId { get; }

	internal string PolicyId { get; }

	internal string ModuleId { get; }

	internal string DisplayName { get; }

	internal PolicyEffectHook Hook { get; }

	internal PolicyEffectTargetKind TargetKind { get; }

	internal string TargetId { get; }

	internal PolicyEffectAggregationKind Aggregation { get; }

	internal float Value { get; }
}

/// <summary>
/// Immutable read snapshot for model hooks. Structural writes rebuild and atomically publish
/// the snapshot; model getters only perform bounded dictionary lookups and array iteration.
/// </summary>
internal sealed class PolicyEffectRuntimeIndex
{
	private sealed class Snapshot
	{
		internal static readonly Snapshot Empty = new Snapshot(
			new Dictionary<PolicyEffectHook, IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>>(),
			0,
			0L);

		internal Snapshot(
			IReadOnlyDictionary<PolicyEffectHook, IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>> byHook,
			int contributionCount,
			long structureVersion)
		{
			ByHook = byHook;
			ContributionCount = contributionCount;
			StructureVersion = structureVersion;
		}

		internal IReadOnlyDictionary<PolicyEffectHook, IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>> ByHook { get; }

		internal int ContributionCount { get; }

		internal long StructureVersion { get; }
	}

	private static readonly IReadOnlyList<PolicyEffectRuntimeContribution> EmptyContributions = Array.Empty<PolicyEffectRuntimeContribution>();

	private readonly object _writeGate = new object();

	private readonly Dictionary<string, PolicyEffectRuntimeContribution[]> _contributionsByInstance =
		new Dictionary<string, PolicyEffectRuntimeContribution[]>(StringComparer.OrdinalIgnoreCase);

	private Snapshot _snapshot = Snapshot.Empty;

	internal int ContributionCount => Volatile.Read(ref _snapshot).ContributionCount;

	internal long StructureVersion => Volatile.Read(ref _snapshot).StructureVersion;

	internal IReadOnlyList<PolicyEffectRuntimeContribution> GetContributions(
		PolicyEffectHook hook,
		PolicyEffectTargetKind targetKind,
		string targetId)
	{
		if (string.IsNullOrWhiteSpace(targetId))
		{
			return EmptyContributions;
		}

		Snapshot snapshot = Volatile.Read(ref _snapshot);
		if (!snapshot.ByHook.TryGetValue(hook, out IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>> byTargetKind)
			|| !byTargetKind.TryGetValue(targetKind, out IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>> byTargetId)
			|| !byTargetId.TryGetValue(targetId.Trim(), out IReadOnlyList<PolicyEffectRuntimeContribution> contributions))
		{
			return EmptyContributions;
		}
		return contributions;
	}

	internal void ReplaceInstance(string instanceId, IEnumerable<PolicyEffectRuntimeContribution> contributions)
	{
		string normalizedInstanceId = (instanceId ?? string.Empty).Trim();
		if (normalizedInstanceId.Length <= 0)
		{
			throw new ArgumentException("运行时效果索引缺少 instanceId。", nameof(instanceId));
		}

		PolicyEffectRuntimeContribution[] normalized = (contributions ?? Enumerable.Empty<PolicyEffectRuntimeContribution>())
			.Where(item => item != null)
			.ToArray();
		if (normalized.Any(item => !string.Equals(item.InstanceId, normalizedInstanceId, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidOperationException("运行时效果贡献与替换的 instanceId 不一致。");
		}

		lock (_writeGate)
		{
			if (normalized.Length <= 0)
			{
				_contributionsByInstance.Remove(normalizedInstanceId);
			}
			else
			{
				_contributionsByInstance[normalizedInstanceId] = normalized;
			}
			PublishSnapshotLocked();
		}
	}

	internal bool RemoveInstance(string instanceId)
	{
		string normalizedInstanceId = (instanceId ?? string.Empty).Trim();
		if (normalizedInstanceId.Length <= 0)
		{
			return false;
		}

		lock (_writeGate)
		{
			if (!_contributionsByInstance.Remove(normalizedInstanceId))
			{
				return false;
			}
			PublishSnapshotLocked();
			return true;
		}
	}

	internal void Rebuild(IEnumerable<PolicyEffectRuntimeContribution> contributions)
	{
		PolicyEffectRuntimeContribution[] normalized = (contributions ?? Enumerable.Empty<PolicyEffectRuntimeContribution>())
			.Where(item => item != null)
			.ToArray();

		lock (_writeGate)
		{
			_contributionsByInstance.Clear();
			foreach (IGrouping<string, PolicyEffectRuntimeContribution> group in normalized.GroupBy(item => item.InstanceId, StringComparer.OrdinalIgnoreCase))
			{
				_contributionsByInstance[group.Key] = group.ToArray();
			}
			PublishSnapshotLocked();
		}
	}

	internal void Clear()
	{
		lock (_writeGate)
		{
			if (_contributionsByInstance.Count <= 0 && Volatile.Read(ref _snapshot).ContributionCount <= 0)
			{
				return;
			}
			_contributionsByInstance.Clear();
			PublishSnapshotLocked();
		}
	}

	private void PublishSnapshotLocked()
	{
		Snapshot previous = Volatile.Read(ref _snapshot);
		Dictionary<PolicyEffectHook, Dictionary<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>>> mutable =
			new Dictionary<PolicyEffectHook, Dictionary<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>>>();
		Dictionary<string, PolicyEffectRuntimeContribution> retainedByPolicyModuleTarget =
			new Dictionary<string, PolicyEffectRuntimeContribution>(StringComparer.OrdinalIgnoreCase);
		int contributionCount = 0;

		foreach (PolicyEffectRuntimeContribution contribution in _contributionsByInstance.Values
			.SelectMany(items => items)
			.OrderBy(item => item.InstanceId, StringComparer.Ordinal))
		{
			string policyIdentity = contribution.PolicyId.Length > 0
				? contribution.PolicyId
				: "instance:" + contribution.InstanceId;
			string uniqueKey = policyIdentity + "\u001f"
				+ contribution.ModuleId + "\u001f"
				+ ((int)contribution.TargetKind) + "\u001f"
				+ contribution.TargetId;
			if (retainedByPolicyModuleTarget.TryGetValue(uniqueKey, out PolicyEffectRuntimeContribution retained))
			{
				bool sameValue = retained.Hook == contribution.Hook
					&& retained.Aggregation == contribution.Aggregation
					&& retained.Value.Equals(contribution.Value);
				if (sameValue)
				{
					PolicySystemLog.Write("Effect", "runtime-index-duplicate-contribution",
						"policyId=" + contribution.PolicyId
						+ " moduleId=" + contribution.ModuleId
						+ " targetKind=" + contribution.TargetKind
						+ " targetId=" + contribution.TargetId
						+ " keptInstance=" + retained.InstanceId
						+ " skippedInstance=" + contribution.InstanceId);
				}
				else
				{
					PolicySystemLog.Failure("Effect", "runtime-index-duplicate-contribution-conflict",
						"Duplicate policy/module/target contribution has conflicting runtime values; later contribution skipped.",
						"policyId=" + contribution.PolicyId
						+ " moduleId=" + contribution.ModuleId
						+ " targetKind=" + contribution.TargetKind
						+ " targetId=" + contribution.TargetId
						+ " keptInstance=" + retained.InstanceId
						+ " skippedInstance=" + contribution.InstanceId);
				}
				continue;
			}
			retainedByPolicyModuleTarget.Add(uniqueKey, contribution);

			if (!mutable.TryGetValue(contribution.Hook, out Dictionary<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>> byTargetKind))
			{
				byTargetKind = new Dictionary<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>>();
				mutable.Add(contribution.Hook, byTargetKind);
			}
			if (!byTargetKind.TryGetValue(contribution.TargetKind, out Dictionary<string, List<PolicyEffectRuntimeContribution>> byTargetId))
			{
				byTargetId = new Dictionary<string, List<PolicyEffectRuntimeContribution>>(StringComparer.OrdinalIgnoreCase);
				byTargetKind.Add(contribution.TargetKind, byTargetId);
			}
			if (!byTargetId.TryGetValue(contribution.TargetId, out List<PolicyEffectRuntimeContribution> items))
			{
				items = new List<PolicyEffectRuntimeContribution>();
				byTargetId.Add(contribution.TargetId, items);
			}
			items.Add(contribution);
			contributionCount++;
		}

		Dictionary<PolicyEffectHook, IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>> frozen =
			new Dictionary<PolicyEffectHook, IReadOnlyDictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>>();
		foreach (KeyValuePair<PolicyEffectHook, Dictionary<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>>> hookEntry in mutable)
		{
			Dictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>> frozenByKind =
				new Dictionary<PolicyEffectTargetKind, IReadOnlyDictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>>();
			foreach (KeyValuePair<PolicyEffectTargetKind, Dictionary<string, List<PolicyEffectRuntimeContribution>>> kindEntry in hookEntry.Value)
			{
				Dictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>> frozenById =
					new Dictionary<string, IReadOnlyList<PolicyEffectRuntimeContribution>>(StringComparer.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, List<PolicyEffectRuntimeContribution>> targetEntry in kindEntry.Value)
				{
					frozenById[targetEntry.Key] = targetEntry.Value
						.OrderBy(item => item.ModuleId, StringComparer.Ordinal)
						.ThenBy(item => item.InstanceId, StringComparer.Ordinal)
						.ToArray();
				}
				frozenByKind[kindEntry.Key] = frozenById;
			}
			frozen[hookEntry.Key] = frozenByKind;
		}

		Volatile.Write(ref _snapshot, new Snapshot(frozen, contributionCount, previous.StructureVersion + 1L));
	}
}
