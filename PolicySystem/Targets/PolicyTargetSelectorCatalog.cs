using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyEffects;

namespace AnimusForge.PolicyTargets;

internal sealed class PolicyTargetSelectorDescriptor
{
	internal PolicyTargetSelectorDescriptor(
		string id,
		string displayName,
		string retrievalText,
		IReadOnlyCollection<string> supportedScopes,
		PolicyEffectTargetKind outputTargetKind,
		bool legacyOnly = false)
	{
		Id = (id ?? string.Empty).Trim();
		DisplayName = (displayName ?? string.Empty).Trim();
		RetrievalText = (retrievalText ?? string.Empty).Trim();
		SupportedScopes = (supportedScopes ?? Array.Empty<string>())
			.Where(scope => !string.IsNullOrWhiteSpace(scope))
			.Select(scope => scope.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		OutputTargetKind = outputTargetKind;
		LegacyOnly = legacyOnly;
	}

	internal string Id { get; }

	internal string DisplayName { get; }

	internal string RetrievalText { get; }

	internal IReadOnlyCollection<string> SupportedScopes { get; }

	internal PolicyEffectTargetKind OutputTargetKind { get; }

	internal bool LegacyOnly { get; }
}

internal sealed class PolicyTargetSelectorEntitySnapshot
{
	internal string EntityId { get; set; }

	internal string OwnerKingdomId { get; set; }

	internal string OwnerClanId { get; set; }

	internal bool IsPrimaryFief { get; set; }

	internal IReadOnlyList<string> BoundVillageIds { get; set; } = Array.Empty<string>();
}

internal sealed class PolicyTargetSelectorResolutionContext
{
	internal string Scope { get; set; }

	internal string TargetKingdomId { get; set; }

	internal string ExcludedClanId { get; set; }

	internal IReadOnlyList<PolicyTargetSelectorEntitySnapshot> Entities { get; set; }
}

internal sealed class PolicyTargetSelectorResolution
{
	internal IReadOnlyList<string> PrimarySettlementIds { get; set; } = Array.Empty<string>();

	internal IReadOnlyList<string> SettlementIds { get; set; } = Array.Empty<string>();
}

internal sealed class PolicyTargetSelectorCandidate
{
	internal PolicyTargetSelectorDescriptor Descriptor { get; set; }

	internal float RecallScore { get; set; }

	internal float RerankScore { get; set; }
}

internal static class PolicyTargetSelectorCatalog
{
	internal const string CurrentKingdomPrimaryFiefsExceptPlayerClanId =
		"currentKingdomPrimaryFiefsExceptPlayerClan";

	internal const int RecallLimit = 6;

	internal const int PromptLimit = 4;

	private static readonly object IndexGate = new object();

	private static readonly IReadOnlyList<PolicyTargetSelectorDescriptor> DescriptorSnapshot =
		new[]
		{
			new PolicyTargetSelectorDescriptor(
				CurrentKingdomPrimaryFiefsExceptPlayerClanId,
				PolicyCompatibilityTexts.Get("Closeout_PolicyTargetSelectorCatalog_Legacy_Closeout_0"),
				PolicyCompatibilityTexts.Get("Closeout_PolicyTargetSelectorCatalog_Legacy_Closeout_1")
				+ PolicyCompatibilityTexts.Get("Closeout_PolicyTargetSelectorCatalog_Legacy_Closeout_2"),
				new[] { PolicyEffectScopes.Local },
				PolicyEffectTargetKind.Settlement,
				legacyOnly: true)
		};

	private static IReadOnlyList<IndexedDescriptor> _index = Array.Empty<IndexedDescriptor>();

	private static volatile bool _indexAttempted;

	static PolicyTargetSelectorCatalog()
	{
		ValidateDescriptors(DescriptorSnapshot);
	}

	internal static IReadOnlyList<PolicyTargetSelectorDescriptor> Descriptors => DescriptorSnapshot;

	internal static bool TryGet(string id, out PolicyTargetSelectorDescriptor descriptor)
	{
		string normalized = (id ?? string.Empty).Trim();
		descriptor = DescriptorSnapshot.FirstOrDefault(item => string.Equals(item.Id, normalized, StringComparison.Ordinal));
		return descriptor != null;
	}

	internal static IReadOnlyList<PolicyTargetSelectorCandidate> RecallAndRerank(
		float[] queryVector,
		string queryText,
		string scope)
	{
		string normalizedScope = (scope ?? string.Empty).Trim();
		string normalizedQuery = (queryText ?? string.Empty).Trim();
		if (queryVector == null || queryVector.Length <= 0 || normalizedQuery.Length <= 0 || !EnsureIndex())
		{
			return Array.Empty<PolicyTargetSelectorCandidate>();
		}

		List<IndexedDescriptor> recalled = _index
			.Where(item => item.Vector != null
				&& !item.Descriptor.LegacyOnly
				&& item.Vector.Length == queryVector.Length
				&& item.Descriptor.SupportedScopes.Contains(normalizedScope, StringComparer.OrdinalIgnoreCase))
			.Select(item => new IndexedDescriptor
			{
				Descriptor = item.Descriptor,
				Vector = item.Vector,
				RecallScore = Dot(queryVector, item.Vector)
			})
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Descriptor.Id, StringComparer.Ordinal)
			.Take(RecallLimit)
			.ToList();
		if (recalled.Count <= 0)
		{
			return Array.Empty<PolicyTargetSelectorCandidate>();
		}

		return recalled
			.Select(item => new PolicyTargetSelectorCandidate
			{
				Descriptor = item.Descriptor,
				RecallScore = item.RecallScore,
				// Keep the compatibility field aligned with the embedding-only ranking score.
				RerankScore = item.RecallScore
			})
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Descriptor.Id, StringComparer.Ordinal)
			.Take(PromptLimit)
			.ToArray();
	}

	internal static bool TryResolve(
		string selectorId,
		PolicyTargetSelectorResolutionContext context,
		out PolicyTargetSelectorResolution resolution,
		out string error)
	{
		resolution = null;
		error = string.Empty;
		if (!TryGet(selectorId, out PolicyTargetSelectorDescriptor descriptor))
		{
			error = "Unknown policy target selector: " + (selectorId ?? string.Empty).Trim();
			return false;
		}
		if (context == null
			|| !descriptor.SupportedScopes.Contains((context.Scope ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase))
		{
			error = "Policy target selector does not support current scope.";
			return false;
		}
		if (!string.Equals(descriptor.Id, CurrentKingdomPrimaryFiefsExceptPlayerClanId, StringComparison.Ordinal))
		{
			error = "Policy target selector resolver not registered: " + descriptor.Id;
			return false;
		}

		string kingdomId = (context.TargetKingdomId ?? string.Empty).Trim();
		string excludedClanId = (context.ExcludedClanId ?? string.Empty).Trim();
		if (kingdomId.Length <= 0 || excludedClanId.Length <= 0)
		{
			error = "Policy target selector missing fixed kingdom or excluded clans.";
			return false;
		}

		List<PolicyTargetSelectorEntitySnapshot> primaryFiefs = (context.Entities ?? Array.Empty<PolicyTargetSelectorEntitySnapshot>())
			.Where(entity => entity != null
				&& entity.IsPrimaryFief
				&& !string.IsNullOrWhiteSpace(entity.EntityId)
				&& !string.IsNullOrWhiteSpace(entity.OwnerClanId)
				&& string.Equals(entity.OwnerKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(entity.OwnerClanId, excludedClanId, StringComparison.OrdinalIgnoreCase))
			.GroupBy(entity => entity.EntityId.Trim(), StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.OrderBy(entity => entity.EntityId, StringComparer.Ordinal)
			.ToList();
		if (primaryFiefs.Count <= 0)
		{
			error = "Policy target selector currently has no executable fiefs.";
			return false;
		}

		List<string> primaryIds = NormalizeIds(primaryFiefs.Select(entity => entity.EntityId));
		List<string> settlementIds = NormalizeIds(primaryFiefs
			.SelectMany(entity => new[] { entity.EntityId }.Concat(entity.BoundVillageIds ?? Array.Empty<string>())));
		resolution = new PolicyTargetSelectorResolution
		{
			PrimarySettlementIds = primaryIds,
			SettlementIds = settlementIds
		};
		return true;
	}

	private static bool EnsureIndex()
	{
		if (_indexAttempted)
		{
			return _index.Count > 0;
		}
		lock (IndexGate)
		{
			if (_indexAttempted)
			{
				return _index.Count > 0;
			}
			_indexAttempted = true;
			List<IndexedDescriptor> built = new List<IndexedDescriptor>(DescriptorSnapshot.Count);
			foreach (PolicyTargetSelectorDescriptor descriptor in DescriptorSnapshot)
			{
				if (descriptor.LegacyOnly)
				{
					continue;
				}
				if (!OnnxEmbeddingEngine.Instance.TryGetEmbedding(descriptor.RetrievalText, out float[] vector)
					|| vector == null
					|| vector.Length <= 0)
				{
					_index = Array.Empty<IndexedDescriptor>();
					return false;
				}
				built.Add(new IndexedDescriptor { Descriptor = descriptor, Vector = vector });
			}
			_index = built;
			return true;
		}
	}

	private static void ValidateDescriptors(IReadOnlyList<PolicyTargetSelectorDescriptor> descriptors)
	{
		if (descriptors == null
			|| descriptors.Count <= 0
			|| descriptors.Any(item => item == null
				|| string.IsNullOrWhiteSpace(item.Id)
				|| string.IsNullOrWhiteSpace(item.DisplayName)
				|| string.IsNullOrWhiteSpace(item.RetrievalText)
				|| item.SupportedScopes == null
				|| item.SupportedScopes.Count <= 0)
			|| descriptors.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != descriptors.Count)
		{
			throw new InvalidOperationException("Policy target selector descriptor catalog is invalid.");
		}
	}

	private static float Dot(IReadOnlyList<float> left, IReadOnlyList<float> right)
	{
		float score = 0f;
		for (int index = 0; index < left.Count; index++)
		{
			score += left[index] * right[index];
		}
		return score;
	}

	private static List<string> NormalizeIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private sealed class IndexedDescriptor
	{
		internal PolicyTargetSelectorDescriptor Descriptor { get; set; }

		internal float[] Vector { get; set; }

		internal float RecallScore { get; set; }
	}
}
