using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace AnimusForge.PolicyTargets;

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanUniverse
{
	PrimaryFiefs,
	Kingdoms,
	Clans
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanScopeAnchor
{
	TargetKingdom,
	IssuerKingdom,
	NamedKingdom
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanEntityType
{
	PrimaryFief,
	Town,
	Castle,
	Kingdom,
	Clan
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanRelation
{
	Any,
	Domestic,
	Enemy,
	Ally,
	Foreign,
	Specific
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanOwnerClanPredicate
{
	Any,
	PlayerClan,
	ProposerClan,
	SpecificClan,
	ExcludePlayerClan,
	ExcludeProposerClan,
	ExcludeSpecificClan
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanExclusion
{
	PublicationParents,
	PlayerClanFiefs,
	ProposerClanFiefs,
	SpecificEntities
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanDirection
{
	Any,
	North,
	South,
	East,
	West
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanDistance
{
	None,
	Nearest,
	Farthest
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanMetric
{
	None,
	Wealth,
	Influence,
	Strength,
	FiefCount,
	Food,
	Prosperity,
	Loyalty,
	Security,
	Hearth,
	Militia
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanMetricComparison
{
	None,
	LessThan,
	LessThanOrEqual,
	GreaterThan,
	GreaterThanOrEqual
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanSortDirection
{
	None,
	Ascending,
	Descending
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanCardinality
{
	All,
	TopN,
	BottomN
}

[Flags]
[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanDependencies
{
	None = 0,
	Structure = 1,
	Relation = 2,
	DailyMetric = 4
}

[JsonConverter(typeof(PolicyTargetPlanEnumConverter))]
internal enum PolicyTargetPlanResolutionStrategy
{
	FixedTargets,
	StructureDynamic,
	RelationDynamic,
	DailyMetricDynamic
}

internal sealed class PolicyTargetPlanEnumConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		Type type = Nullable.GetUnderlyingType(objectType) ?? objectType;
		return type.IsEnum && type.Namespace == typeof(PolicyTargetPlanUniverse).Namespace;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
		if (reader.TokenType == JsonToken.String)
		{
			try
			{
				return Enum.Parse(enumType, (reader.Value as string) ?? string.Empty, true);
			}
			catch (ArgumentException)
			{
				// Unknown names use the same inert sentinel as unknown numeric values.
			}
			catch (OverflowException)
			{
				// Numeric strings outside the enum backing type are also unknown.
			}
		}
		if (reader.TokenType == JsonToken.Integer)
		{
			try
			{
				return Enum.ToObject(enumType, Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture));
			}
			catch (Exception ex) when (ex is OverflowException || ex is InvalidCastException || ex is FormatException)
			{
				// Fall through to the inert sentinel below.
			}
		}
		// Preserve forward compatibility at the object boundary. Validation rejects
		// this undefined sentinel, so unknown operators remain inert and never execute.
		return Enum.ToObject(enumType, -1);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		writer.WriteValue(value?.ToString() ?? string.Empty);
	}
}

internal sealed class PolicyTargetPlanBranchSaveData
{
	[JsonProperty("universe")]
	internal PolicyTargetPlanUniverse Universe { get; set; } = PolicyTargetPlanUniverse.PrimaryFiefs;

	[JsonProperty("scopeAnchor")]
	internal PolicyTargetPlanScopeAnchor ScopeAnchor { get; set; } = PolicyTargetPlanScopeAnchor.TargetKingdom;

	[JsonProperty("anchorKingdomId", NullValueHandling = NullValueHandling.Ignore)]
	internal string AnchorKingdomId { get; set; } = string.Empty;

	[JsonProperty("entityType")]
	internal PolicyTargetPlanEntityType EntityType { get; set; } = PolicyTargetPlanEntityType.PrimaryFief;

	[JsonProperty("relation")]
	internal PolicyTargetPlanRelation Relation { get; set; } = PolicyTargetPlanRelation.Any;

	[JsonProperty("namedKingdomIds")]
	internal List<string> NamedKingdomIds { get; set; } = new List<string>();

	[JsonProperty("ownerClanPredicate")]
	internal PolicyTargetPlanOwnerClanPredicate OwnerClanPredicate { get; set; } = PolicyTargetPlanOwnerClanPredicate.Any;

	[JsonProperty("referenceClanId", NullValueHandling = NullValueHandling.Ignore)]
	internal string ReferenceClanId { get; set; } = string.Empty;

	[JsonProperty("exclusions")]
	internal List<PolicyTargetPlanExclusion> Exclusions { get; set; } = new List<PolicyTargetPlanExclusion>();

	[JsonProperty("entityReferences")]
	internal List<string> EntityReferences { get; set; } = new List<string>();

	[JsonProperty("excludedEntityReferences")]
	internal List<string> ExcludedEntityReferences { get; set; } = new List<string>();

	[JsonProperty("borderOnly")]
	internal bool BorderOnly { get; set; }

	[JsonProperty("direction")]
	internal PolicyTargetPlanDirection Direction { get; set; } = PolicyTargetPlanDirection.Any;

	[JsonProperty("distance")]
	internal PolicyTargetPlanDistance Distance { get; set; } = PolicyTargetPlanDistance.None;

	[JsonProperty("metric")]
	internal PolicyTargetPlanMetric Metric { get; set; } = PolicyTargetPlanMetric.None;

	[JsonProperty("sortDirection")]
	internal PolicyTargetPlanSortDirection SortDirection { get; set; } = PolicyTargetPlanSortDirection.None;

	[JsonProperty("metricComparison")]
	internal PolicyTargetPlanMetricComparison MetricComparison { get; set; } = PolicyTargetPlanMetricComparison.None;

	[JsonProperty("metricThreshold", NullValueHandling = NullValueHandling.Ignore)]
	internal float? MetricThreshold { get; set; }

	[JsonProperty("cardinality")]
	internal PolicyTargetPlanCardinality Cardinality { get; set; } = PolicyTargetPlanCardinality.All;

	[JsonProperty("limit")]
	internal int Limit { get; set; }
}

internal sealed class PolicyTargetPlanSaveData
{
	[JsonProperty("planVersion")]
	internal int PlanVersion { get; set; } = PolicyTargetPlanResolver.CurrentPlanVersion;

	[JsonProperty("resolverVersion")]
	internal int ResolverVersion { get; set; } = PolicyTargetPlanResolver.CurrentResolverVersion;

	[JsonProperty("branches")]
	internal List<PolicyTargetPlanBranchSaveData> Branches { get; set; } = new List<PolicyTargetPlanBranchSaveData>();

	[JsonProperty("dependencies")]
	internal PolicyTargetPlanDependencies Dependencies { get; set; } = PolicyTargetPlanDependencies.None;

	[JsonProperty("resolutionStrategy")]
	internal PolicyTargetPlanResolutionStrategy ResolutionStrategy { get; set; }

	[JsonProperty("normalizedSignature", NullValueHandling = NullValueHandling.Ignore)]
	internal string NormalizedSignature { get; set; } = string.Empty;

	[JsonProperty("legacySelectorId", NullValueHandling = NullValueHandling.Ignore)]
	internal string LegacySelectorId { get; set; } = string.Empty;
}

internal sealed class PolicyTargetPlanResolutionContext
{
	internal string Scope { get; set; } = string.Empty;

	internal string TargetKingdomId { get; set; } = string.Empty;

	internal string IssuerKingdomId { get; set; } = string.Empty;

	internal string PlayerClanId { get; set; } = string.Empty;

	internal string ProposerClanId { get; set; } = string.Empty;

	internal IReadOnlyCollection<string> SourceSettlementIds { get; set; } = Array.Empty<string>();

	internal IReadOnlyCollection<string> AllowedEntityReferenceIds { get; set; } = Array.Empty<string>();

	internal IReadOnlyCollection<string> AllowedKingdomReferenceIds { get; set; } = Array.Empty<string>();

	internal bool AllowPersistedValidatedReferences { get; set; }

	internal PolicyTargetWorldSnapshot Snapshot { get; set; }
}

internal sealed class PolicyTargetPlanResolution
{
	internal IReadOnlyList<string> PrimarySettlementIds { get; set; } = Array.Empty<string>();

	internal IReadOnlyList<string> ClanIds { get; set; } = Array.Empty<string>();

	internal IReadOnlyList<string> KingdomIds { get; set; } = Array.Empty<string>();

	internal bool IsTemporarilyEmpty { get; set; }
}

internal enum PolicyTargetPlanResolutionFailureKind
{
	None = 0,
	MissingAnchor = 1,
	EmptyResult = 2,
	InvalidPlan = 3,
	InvalidReference = 4,
	InternalFailure = 5
}

internal static class PolicyTargetPlanResolver
{
	internal const int LegacyPlanVersion = 2;

	internal const int LegacyResolverVersion = 1;

	internal const int CurrentPlanVersion = 3;

	internal const int CurrentResolverVersion = 2;

	internal const int MaximumBranches = 2;

	internal const int MaximumEntityReferences = 16;

	internal const int MaximumCardinality = 100;

	internal static bool TryNormalizeAndValidate(
		PolicyTargetPlanSaveData source,
		out PolicyTargetPlanSaveData normalized,
		out string error)
	{
		normalized = null;
		error = string.Empty;
		if (source == null)
		{
			error = "TargetPlan 不能为空。";
			return false;
		}
		if (!IsSupportedVersionPair(source.PlanVersion, source.ResolverVersion))
		{
			error = "不支持的 TargetPlan 版本。";
			return false;
		}
		List<PolicyTargetPlanBranchSaveData> branches = source.Branches ?? new List<PolicyTargetPlanBranchSaveData>();
		PolicyTargetPlanDependencies knownDependencies = PolicyTargetPlanDependencies.Structure
			| PolicyTargetPlanDependencies.Relation
			| PolicyTargetPlanDependencies.DailyMetric;
		if ((source.Dependencies & ~knownDependencies) != 0)
		{
			error = "TargetPlan 包含未知依赖标志。";
			return false;
		}
		if (!Enum.IsDefined(typeof(PolicyTargetPlanResolutionStrategy), source.ResolutionStrategy))
		{
			error = "TargetPlan 包含未知解析策略。";
			return false;
		}
		if (branches.Count <= 0 || branches.Count > MaximumBranches)
		{
			error = "TargetPlan 分支数量必须为 1 到 " + MaximumBranches.ToString(CultureInfo.InvariantCulture) + "。";
			return false;
		}
		PolicyTargetPlanUniverse universe = branches[0]?.Universe ?? PolicyTargetPlanUniverse.PrimaryFiefs;
		List<PolicyTargetPlanBranchSaveData> normalizedBranches = new List<PolicyTargetPlanBranchSaveData>(branches.Count);
		foreach (PolicyTargetPlanBranchSaveData branch in branches)
		{
			if (!TryNormalizeBranch(branch, universe, source.PlanVersion, out PolicyTargetPlanBranchSaveData normalizedBranch, out error))
			{
				return false;
			}
			normalizedBranches.Add(normalizedBranch);
		}
		PolicyTargetPlanDependencies dependencies = ComputeDependencies(normalizedBranches);
		normalized = new PolicyTargetPlanSaveData
		{
			PlanVersion = source.PlanVersion,
			ResolverVersion = source.ResolverVersion,
			Branches = normalizedBranches,
			Dependencies = dependencies,
			ResolutionStrategy = ComputeResolutionStrategy(normalizedBranches, dependencies),
			LegacySelectorId = NormalizeId(source.LegacySelectorId)
		};
		normalized.NormalizedSignature = BuildNormalizedSignature(normalized);
		string claimedSignature = NormalizeId(source.NormalizedSignature);
		if (claimedSignature.Length > 0
			&& !string.Equals(claimedSignature, normalized.NormalizedSignature, StringComparison.Ordinal))
		{
			normalized = null;
			error = "TargetPlan 规范化签名不匹配。";
			return false;
		}
		return true;
	}

	internal static bool TryResolve(
		PolicyTargetPlanSaveData source,
		PolicyTargetPlanResolutionContext context,
		out PolicyTargetPlanResolution resolution,
		out string error)
	{
		return TryResolve(source, context, out resolution, out _, out error);
	}

	internal static bool TryResolve(
		PolicyTargetPlanSaveData source,
		PolicyTargetPlanResolutionContext context,
		out PolicyTargetPlanResolution resolution,
		out PolicyTargetPlanResolutionFailureKind failureKind,
		out string error)
	{
		resolution = null;
		failureKind = PolicyTargetPlanResolutionFailureKind.None;
		error = string.Empty;
		try
		{
			if (!TryNormalizeAndValidate(source, out PolicyTargetPlanSaveData plan, out error))
			{
				failureKind = PolicyTargetPlanResolutionFailureKind.InvalidPlan;
				return false;
			}
			if (context?.Snapshot?.Entities == null)
			{
				failureKind = PolicyTargetPlanResolutionFailureKind.InternalFailure;
				error = "TargetPlan 实时世界快照不可用。";
				return false;
			}
			bool allowPersistedReferences = context.AllowPersistedValidatedReferences;
			HashSet<string> primaryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> clanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> kingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (PolicyTargetPlanBranchSaveData branch in plan.Branches)
			{
				if (!TryResolveBranch(branch, context, allowPersistedReferences,
					out List<PolicyTargetEntitySnapshot> entities, out failureKind, out error))
				{
					return false;
				}
				foreach (PolicyTargetEntitySnapshot entity in entities)
				{
					if (branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs)
					{
						primaryIds.Add(entity.EntityId);
					}
					else if (branch.Universe == PolicyTargetPlanUniverse.Clans)
					{
						clanIds.Add(entity.EntityId);
					}
					else
					{
						kingdomIds.Add(entity.EntityId);
					}
				}
			}
			bool empty = primaryIds.Count == 0 && clanIds.Count == 0 && kingdomIds.Count == 0;
			resolution = new PolicyTargetPlanResolution
			{
				PrimarySettlementIds = primaryIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
				ClanIds = clanIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
				KingdomIds = kingdomIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
				IsTemporarilyEmpty = empty
			};
			failureKind = empty
				? PolicyTargetPlanResolutionFailureKind.EmptyResult
				: PolicyTargetPlanResolutionFailureKind.None;
			return true;
		}
		catch (Exception ex)
		{
			resolution = null;
			failureKind = PolicyTargetPlanResolutionFailureKind.InternalFailure;
			error = "TargetPlan 运行时解析异常：" + ex.GetType().Name;
			return false;
		}
	}

	internal static IReadOnlyList<string> ExpandPrimarySettlementIds(
		PolicyTargetPlanResolution resolution,
		PolicyTargetWorldSnapshot snapshot)
	{
		if (resolution == null)
		{
			return Array.Empty<string>();
		}
		List<PolicyTargetEntitySnapshot> primarySettlements = (snapshot?.Entities
			?? Array.Empty<PolicyTargetEntitySnapshot>())
			.Where(entity => entity != null
				&& string.Equals(entity.Kind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
				&& (entity.IsCity || entity.IsCastle)
				&& !string.IsNullOrWhiteSpace(entity.EntityId))
			.ToList();
		HashSet<string> availablePrimaryIds = new HashSet<string>(
			primarySettlements.Select(entity => entity.EntityId.Trim()),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string primaryId in resolution.PrimarySettlementIds ?? Array.Empty<string>())
		{
			string normalized = (primaryId ?? string.Empty).Trim();
			if (normalized.Length > 0 && (snapshot == null || availablePrimaryIds.Contains(normalized)))
			{
				expanded.Add(normalized);
			}
		}
		HashSet<string> clanIds = new HashSet<string>(
			(resolution.ClanIds ?? Array.Empty<string>())
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id.Trim()),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> kingdomIds = new HashSet<string>(
			(resolution.KingdomIds ?? Array.Empty<string>())
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id.Trim()),
			StringComparer.OrdinalIgnoreCase);
		if (clanIds.Count > 0 || kingdomIds.Count > 0)
		{
			foreach (PolicyTargetEntitySnapshot entity in primarySettlements)
			{
				if (clanIds.Contains(entity.OwnerClanId ?? string.Empty)
					|| kingdomIds.Contains(entity.OwnerKingdomId ?? string.Empty))
				{
					expanded.Add(entity.EntityId.Trim());
				}
			}
		}
		return expanded.OrderBy(id => id, StringComparer.Ordinal).ToArray();
	}

	internal static PolicyTargetPlanSaveData Clone(PolicyTargetPlanSaveData source)
	{
		return TryNormalizeAndValidate(source, out PolicyTargetPlanSaveData normalized, out _)
			? normalized
			: null;
	}

	internal static List<PolicyTargetPlanSaveData> NormalizePlans(IEnumerable<PolicyTargetPlanSaveData> plans)
	{
		List<PolicyTargetPlanSaveData> result = new List<PolicyTargetPlanSaveData>();
		HashSet<string> signatures = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyTargetPlanSaveData source in plans ?? Enumerable.Empty<PolicyTargetPlanSaveData>())
		{
			if (TryNormalizeAndValidate(source, out PolicyTargetPlanSaveData normalized, out _)
				&& signatures.Add(normalized.NormalizedSignature))
			{
				result.Add(normalized);
			}
		}
		return result.OrderBy(plan => plan.NormalizedSignature, StringComparer.Ordinal).ToList();
	}

	private static bool TryNormalizeBranch(
		PolicyTargetPlanBranchSaveData source,
		PolicyTargetPlanUniverse requiredUniverse,
		int planVersion,
		out PolicyTargetPlanBranchSaveData normalized,
		out string error)
	{
		normalized = null;
		error = string.Empty;
		if (source == null || source.Universe != requiredUniverse)
		{
			error = "TargetPlan 的 OR 分支必须使用同一目标 universe。";
			return false;
		}
		if (!AreBranchEnumsDefined(source))
		{
			error = "TargetPlan 包含未知操作符。";
			return false;
		}
		if (!IsEntityTypeAllowed(source.Universe, source.EntityType))
		{
			error = "TargetPlan entityType 与 universe 不兼容。";
			return false;
		}
		if (source.Universe == PolicyTargetPlanUniverse.Kingdoms
			&& (source.OwnerClanPredicate != PolicyTargetPlanOwnerClanPredicate.Any
				|| (source.Exclusions ?? new List<PolicyTargetPlanExclusion>()).Any(value =>
					value == PolicyTargetPlanExclusion.PublicationParents
					|| value == PolicyTargetPlanExclusion.PlayerClanFiefs
					|| value == PolicyTargetPlanExclusion.ProposerClanFiefs)))
		{
			error = "王国 TargetPlan 不能使用封地所有者或发布地排除条件。";
			return false;
		}
		if (source.Universe != PolicyTargetPlanUniverse.PrimaryFiefs
			&& (source.Exclusions ?? new List<PolicyTargetPlanExclusion>()).Any(value =>
				value == PolicyTargetPlanExclusion.PublicationParents
				|| value == PolicyTargetPlanExclusion.PlayerClanFiefs
				|| value == PolicyTargetPlanExclusion.ProposerClanFiefs))
		{
			error = "非封地 TargetPlan 不能使用封地排除条件。";
			return false;
		}
		if (source.Universe != PolicyTargetPlanUniverse.PrimaryFiefs
			&& (source.BorderOnly
				|| source.Direction != PolicyTargetPlanDirection.Any
				|| source.Distance != PolicyTargetPlanDistance.None
				|| IsSettlementMetric(source.Metric)))
		{
			error = "非封地 TargetPlan 不能使用封地地理或定居点指标条件。";
			return false;
		}
		if (source.Universe == PolicyTargetPlanUniverse.PrimaryFiefs && IsAggregateMetric(source.Metric))
		{
			error = "封地 TargetPlan 不能使用王国或家族聚合指标。";
			return false;
		}
		if (source.Metric != PolicyTargetPlanMetric.None && source.Distance != PolicyTargetPlanDistance.None)
		{
			error = "TargetPlan 的指标排序与距离排序不能同时出现。";
			return false;
		}
		if (source.Metric == PolicyTargetPlanMetric.None
			&& source.SortDirection != PolicyTargetPlanSortDirection.None)
		{
			error = "TargetPlan 排序方向缺少对应指标。";
			return false;
		}
		bool hasMetricComparison = source.MetricComparison != PolicyTargetPlanMetricComparison.None;
		bool hasMetricThreshold = source.MetricThreshold.HasValue;
		if (planVersion == LegacyPlanVersion
			&& (source.Metric == PolicyTargetPlanMetric.Hearth
				|| source.Metric == PolicyTargetPlanMetric.Militia
				|| hasMetricComparison
				|| hasMetricThreshold))
		{
			error = "TargetPlan v2 不能使用 v3 指标或阈值字段。";
			return false;
		}
		if (hasMetricComparison != hasMetricThreshold)
		{
			error = "TargetPlan 指标比较符与阈值必须同时存在。";
			return false;
		}
		if (hasMetricComparison && source.Metric == PolicyTargetPlanMetric.None)
		{
			error = "TargetPlan 指标阈值缺少对应指标。";
			return false;
		}
		if (hasMetricThreshold
			&& (float.IsNaN(source.MetricThreshold.Value) || float.IsInfinity(source.MetricThreshold.Value)))
		{
			error = "TargetPlan 指标阈值必须是有限数值。";
			return false;
		}
		List<string> entityReferences = NormalizeIds(source.EntityReferences);
		List<string> excludedReferences = NormalizeIds(source.ExcludedEntityReferences);
		List<string> namedKingdomIds = NormalizeIds(source.NamedKingdomIds);
		if (entityReferences.Count > MaximumEntityReferences
			|| excludedReferences.Count > MaximumEntityReferences
			|| namedKingdomIds.Count > MaximumEntityReferences)
		{
			error = "TargetPlan 实体引用超过安全上限。";
			return false;
		}
		string anchorKingdomId = NormalizeId(source.AnchorKingdomId);
		string referenceClanId = NormalizeId(source.ReferenceClanId);
		if ((source.ScopeAnchor == PolicyTargetPlanScopeAnchor.NamedKingdom) != (anchorKingdomId.Length > 0))
		{
			error = "TargetPlan 王国锚点字段与 scopeAnchor 不一致。";
			return false;
		}
		if (source.Relation == PolicyTargetPlanRelation.Specific && namedKingdomIds.Count == 0)
		{
			error = "Specific relation 缺少王国引用。";
			return false;
		}
		if (source.Relation != PolicyTargetPlanRelation.Specific && namedKingdomIds.Count > 0)
		{
			error = "非 Specific relation 不得携带指定王国引用。";
			return false;
		}
		if ((source.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.SpecificClan
				|| source.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan)
			&& referenceClanId.Length == 0)
		{
			error = "TargetPlan 家族谓词缺少参照家族。";
			return false;
		}
		if (source.OwnerClanPredicate != PolicyTargetPlanOwnerClanPredicate.SpecificClan
			&& source.OwnerClanPredicate != PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan
			&& referenceClanId.Length > 0)
		{
			error = "非指定家族谓词不得携带家族引用。";
			return false;
		}
		int limit = source.Limit;
		if (source.Cardinality == PolicyTargetPlanCardinality.All)
		{
			limit = 0;
		}
		else if (limit <= 0 || limit > MaximumCardinality)
		{
			error = "TargetPlan TopN/BottomN 必须在 1 到 " + MaximumCardinality.ToString(CultureInfo.InvariantCulture) + "。";
			return false;
		}
		List<PolicyTargetPlanExclusion> exclusions = (source.Exclusions ?? new List<PolicyTargetPlanExclusion>())
			.Distinct()
			.OrderBy(value => value)
			.ToList();
		if (exclusions.Contains(PolicyTargetPlanExclusion.SpecificEntities) && excludedReferences.Count == 0)
		{
			error = "SpecificEntities exclusion 缺少实体引用。";
			return false;
		}
		if (!exclusions.Contains(PolicyTargetPlanExclusion.SpecificEntities) && excludedReferences.Count > 0)
		{
			error = "非 SpecificEntities exclusion 不得携带排除实体引用。";
			return false;
		}
		normalized = new PolicyTargetPlanBranchSaveData
		{
			Universe = source.Universe,
			ScopeAnchor = source.ScopeAnchor,
			AnchorKingdomId = anchorKingdomId,
			EntityType = source.EntityType,
			Relation = source.Relation,
			NamedKingdomIds = namedKingdomIds,
			OwnerClanPredicate = source.OwnerClanPredicate,
			ReferenceClanId = referenceClanId,
			Exclusions = exclusions,
			EntityReferences = entityReferences,
			ExcludedEntityReferences = excludedReferences,
			BorderOnly = source.BorderOnly,
			Direction = source.Direction,
			Distance = source.Distance,
			Metric = source.Metric,
			SortDirection = source.SortDirection,
			MetricComparison = source.MetricComparison,
			MetricThreshold = source.MetricThreshold,
			Cardinality = source.Cardinality,
			Limit = limit
		};
		return true;
	}

	private static bool AreBranchEnumsDefined(PolicyTargetPlanBranchSaveData branch)
	{
		return Enum.IsDefined(typeof(PolicyTargetPlanUniverse), branch.Universe)
			&& Enum.IsDefined(typeof(PolicyTargetPlanScopeAnchor), branch.ScopeAnchor)
			&& Enum.IsDefined(typeof(PolicyTargetPlanEntityType), branch.EntityType)
			&& Enum.IsDefined(typeof(PolicyTargetPlanRelation), branch.Relation)
			&& Enum.IsDefined(typeof(PolicyTargetPlanOwnerClanPredicate), branch.OwnerClanPredicate)
			&& Enum.IsDefined(typeof(PolicyTargetPlanDirection), branch.Direction)
			&& Enum.IsDefined(typeof(PolicyTargetPlanDistance), branch.Distance)
			&& Enum.IsDefined(typeof(PolicyTargetPlanMetric), branch.Metric)
			&& Enum.IsDefined(typeof(PolicyTargetPlanMetricComparison), branch.MetricComparison)
			&& Enum.IsDefined(typeof(PolicyTargetPlanSortDirection), branch.SortDirection)
			&& Enum.IsDefined(typeof(PolicyTargetPlanCardinality), branch.Cardinality)
			&& (branch.Exclusions ?? new List<PolicyTargetPlanExclusion>())
				.All(value => Enum.IsDefined(typeof(PolicyTargetPlanExclusion), value));
	}

	private static bool TryResolveBranch(
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context,
		bool allowPersistedReferences,
		out List<PolicyTargetEntitySnapshot> resolved,
		out PolicyTargetPlanResolutionFailureKind failureKind,
		out string error)
	{
		resolved = new List<PolicyTargetEntitySnapshot>();
		failureKind = PolicyTargetPlanResolutionFailureKind.None;
		error = string.Empty;
		string anchorKingdomId = ResolveAnchorKingdomId(branch, context);
		if (anchorKingdomId.Length == 0 && !CanResolveWithoutKingdomAnchor(branch, context))
		{
			failureKind = PolicyTargetPlanResolutionFailureKind.MissingAnchor;
			error = "TargetPlan 缺少可解析的王国锚点。";
			return false;
		}
		if (!ValidateReferences(branch, context, allowPersistedReferences, out error))
		{
			failureKind = PolicyTargetPlanResolutionFailureKind.InvalidReference;
			return false;
		}
		IEnumerable<PolicyTargetEntitySnapshot> candidates = context.Snapshot.Entities.Where(entity => EntityMatchesUniverse(entity, branch));
		if (branch.EntityReferences.Count > 0)
		{
			HashSet<string> directIds = new HashSet<string>(branch.EntityReferences, StringComparer.OrdinalIgnoreCase);
			candidates = candidates.Where(entity => directIds.Contains(entity.EntityId ?? string.Empty));
		}
		candidates = ApplyRelation(candidates, branch, context, anchorKingdomId);
		candidates = ApplyOwnerClanPredicate(candidates, branch, context);
		HashSet<string> sourceIds = new HashSet<string>(context.SourceSettlementIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> excludedIds = new HashSet<string>(branch.ExcludedEntityReferences, StringComparer.OrdinalIgnoreCase);
		HashSet<string> excludedClanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> excludedKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (excludedIds.Count > 0)
		{
			foreach (PolicyTargetEntitySnapshot excludedEntity in context.Snapshot.Entities.Where(entity =>
				entity != null && excludedIds.Contains(entity.EntityId ?? string.Empty)))
			{
				if (string.Equals(excludedEntity.Kind, PolicyTargetEntityKinds.Clan, StringComparison.OrdinalIgnoreCase))
				{
					excludedClanIds.Add(excludedEntity.EntityId);
				}
				else if (string.Equals(excludedEntity.Kind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase))
				{
					excludedKingdomIds.Add(excludedEntity.EntityId);
				}
			}
		}
		foreach (PolicyTargetPlanExclusion exclusion in branch.Exclusions)
		{
			switch (exclusion)
			{
				case PolicyTargetPlanExclusion.PublicationParents:
					candidates = candidates.Where(entity => !sourceIds.Contains(entity.EntityId ?? string.Empty));
					break;
				case PolicyTargetPlanExclusion.PlayerClanFiefs:
					candidates = candidates.Where(entity => !string.Equals(entity.OwnerClanId, context.PlayerClanId, StringComparison.OrdinalIgnoreCase));
					break;
				case PolicyTargetPlanExclusion.ProposerClanFiefs:
					candidates = candidates.Where(entity => !string.Equals(entity.OwnerClanId, context.ProposerClanId, StringComparison.OrdinalIgnoreCase));
					break;
				case PolicyTargetPlanExclusion.SpecificEntities:
					candidates = candidates.Where(entity =>
						!excludedIds.Contains(entity.EntityId ?? string.Empty)
						&& !excludedClanIds.Contains(entity.OwnerClanId ?? string.Empty)
						&& !excludedKingdomIds.Contains(entity.OwnerKingdomId ?? string.Empty));
					break;
			}
		}
		if (branch.BorderOnly)
		{
			candidates = candidates.Where(entity => entity.IsBorder);
		}
		if (branch.Direction != PolicyTargetPlanDirection.Any)
		{
			if (!TryReadReferencePosition(context, anchorKingdomId, out float referenceX, out float referenceY))
			{
				resolved = new List<PolicyTargetEntitySnapshot>();
				return true;
			}
			candidates = candidates.Where(entity => IsInDirection(entity, branch.Direction, referenceX, referenceY));
		}
		if (branch.Distance != PolicyTargetPlanDistance.None
			&& !TryReadReferencePosition(context, anchorKingdomId, out _, out _))
		{
			resolved = new List<PolicyTargetEntitySnapshot>();
			return true;
		}
		IEnumerable<PolicyTargetEntitySnapshot> ordered = OrderCandidates(candidates, branch, context, anchorKingdomId);
		if (branch.Cardinality != PolicyTargetPlanCardinality.All)
		{
			ordered = ordered.Take(branch.Limit);
		}
		resolved = ordered
			.GroupBy(entity => entity.EntityId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		return true;
	}

	private static bool CanResolveWithoutKingdomAnchor(
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context)
	{
		if (branch == null || context == null)
		{
			return false;
		}
		if ((branch.EntityReferences?.Count ?? 0) > 0)
		{
			return true;
		}
		if (branch.Relation == PolicyTargetPlanRelation.Specific
			&& (branch.NamedKingdomIds?.Count ?? 0) > 0)
		{
			return true;
		}
		switch (branch.OwnerClanPredicate)
		{
			case PolicyTargetPlanOwnerClanPredicate.PlayerClan:
				return !string.IsNullOrWhiteSpace(context.PlayerClanId);
			case PolicyTargetPlanOwnerClanPredicate.ProposerClan:
				return !string.IsNullOrWhiteSpace(context.ProposerClanId);
			case PolicyTargetPlanOwnerClanPredicate.SpecificClan:
				return !string.IsNullOrWhiteSpace(branch.ReferenceClanId);
			default:
				// Negative predicates and exclusions only subtract from a universe; they
				// never constitute the positive runtime bound required to avoid world scope.
				return false;
		}
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> ApplyRelation(
		IEnumerable<PolicyTargetEntitySnapshot> source,
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context,
		string anchorKingdomId)
	{
		switch (branch.Relation)
		{
			case PolicyTargetPlanRelation.Domestic:
				return source.Where(entity => string.Equals(entity.OwnerKingdomId, anchorKingdomId, StringComparison.OrdinalIgnoreCase));
			case PolicyTargetPlanRelation.Enemy:
				return source.Where(entity => HasPair(context.Snapshot.WarPairs, anchorKingdomId, entity.OwnerKingdomId));
			case PolicyTargetPlanRelation.Ally:
				return source.Where(entity => HasPair(context.Snapshot.AlliancePairs, anchorKingdomId, entity.OwnerKingdomId));
			case PolicyTargetPlanRelation.Foreign:
				return source.Where(entity => !string.Equals(entity.OwnerKingdomId, anchorKingdomId, StringComparison.OrdinalIgnoreCase));
			case PolicyTargetPlanRelation.Specific:
				HashSet<string> kingdomIds = new HashSet<string>(branch.NamedKingdomIds, StringComparer.OrdinalIgnoreCase);
				return source.Where(entity => kingdomIds.Contains(entity.OwnerKingdomId ?? string.Empty));
			default:
				return source;
		}
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> ApplyOwnerClanPredicate(
		IEnumerable<PolicyTargetEntitySnapshot> source,
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context)
	{
		switch (branch.OwnerClanPredicate)
		{
			case PolicyTargetPlanOwnerClanPredicate.PlayerClan:
				return source.Where(entity => SameId(entity.OwnerClanId, context.PlayerClanId));
			case PolicyTargetPlanOwnerClanPredicate.ProposerClan:
				return source.Where(entity => SameId(entity.OwnerClanId, context.ProposerClanId));
			case PolicyTargetPlanOwnerClanPredicate.SpecificClan:
				return source.Where(entity => SameId(entity.OwnerClanId, branch.ReferenceClanId));
			case PolicyTargetPlanOwnerClanPredicate.ExcludePlayerClan:
				return source.Where(entity => !SameId(entity.OwnerClanId, context.PlayerClanId));
			case PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan:
				return source.Where(entity => !SameId(entity.OwnerClanId, context.ProposerClanId));
			case PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan:
				return source.Where(entity => !SameId(entity.OwnerClanId, branch.ReferenceClanId));
			default:
				return source;
		}
	}

	private static IEnumerable<PolicyTargetEntitySnapshot> OrderCandidates(
		IEnumerable<PolicyTargetEntitySnapshot> source,
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context,
		string anchorKingdomId)
	{
		List<PolicyTargetEntitySnapshot> candidates = source.ToList();
		bool descending = branch.SortDirection == PolicyTargetPlanSortDirection.Descending
			|| (branch.SortDirection == PolicyTargetPlanSortDirection.None && branch.Cardinality == PolicyTargetPlanCardinality.TopN);
		if (branch.Metric != PolicyTargetPlanMetric.None)
		{
			List<KeyValuePair<PolicyTargetEntitySnapshot, float>> metricCandidates
				= new List<KeyValuePair<PolicyTargetEntitySnapshot, float>>(candidates.Count);
			foreach (PolicyTargetEntitySnapshot entity in candidates)
			{
				if (TryReadMetric(entity, branch.Metric, out float value)
					&& MetricMatchesThreshold(value, branch.MetricComparison, branch.MetricThreshold))
				{
					metricCandidates.Add(new KeyValuePair<PolicyTargetEntitySnapshot, float>(entity, value));
				}
			}
			return descending
				? metricCandidates.OrderByDescending(item => item.Value)
					.ThenBy(item => item.Key.EntityId, StringComparer.Ordinal)
					.Select(item => item.Key)
				: metricCandidates.OrderBy(item => item.Value)
					.ThenBy(item => item.Key.EntityId, StringComparer.Ordinal)
					.Select(item => item.Key);
		}
		if (branch.Distance != PolicyTargetPlanDistance.None
			&& TryReadReferencePosition(context, anchorKingdomId, out float referenceX, out float referenceY))
		{
			Func<PolicyTargetEntitySnapshot, float> distance = entity => DistanceSquared(entity.PositionX, entity.PositionY, referenceX, referenceY);
			return branch.Distance == PolicyTargetPlanDistance.Farthest
				? candidates.Where(entity => entity.HasPosition).OrderByDescending(distance).ThenBy(entity => entity.EntityId, StringComparer.Ordinal)
				: candidates.Where(entity => entity.HasPosition).OrderBy(distance).ThenBy(entity => entity.EntityId, StringComparer.Ordinal);
		}
		if (branch.Cardinality == PolicyTargetPlanCardinality.BottomN)
		{
			return candidates.OrderByDescending(entity => entity.EntityId, StringComparer.Ordinal);
		}
		return candidates.OrderBy(entity => entity.EntityId, StringComparer.Ordinal);
	}

	private static bool ValidateReferences(
		PolicyTargetPlanBranchSaveData branch,
		PolicyTargetPlanResolutionContext context,
		bool allowPersistedReferences,
		out string error)
	{
		error = string.Empty;
		List<PolicyTargetEntitySnapshot> snapshotEntities = (context.Snapshot?.Entities
			?? Array.Empty<PolicyTargetEntitySnapshot>()).Where(entity => entity != null).ToList();
		HashSet<string> availableEntityIds = new HashSet<string>(
			snapshotEntities.Where(entity => EntityMatchesUniverse(entity, branch))
				.Select(entity => entity.EntityId ?? string.Empty),
			StringComparer.OrdinalIgnoreCase);
		if (branch.EntityReferences.Any(id => !availableEntityIds.Contains(id)))
		{
			error = "TargetPlan 的明确实体引用已经失效。";
			return false;
		}
		if (!string.IsNullOrWhiteSpace(branch.ReferenceClanId)
			&& !snapshotEntities.Any(entity =>
				string.Equals(entity.Kind, PolicyTargetEntityKinds.Clan, StringComparison.OrdinalIgnoreCase)
				&& SameId(entity.EntityId, branch.ReferenceClanId)))
		{
			error = "TargetPlan 的指定家族引用已经失效。";
			return false;
		}
		HashSet<string> availableKingdomIds = new HashSet<string>(
			snapshotEntities.Where(entity => string.Equals(entity.Kind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase))
				.Select(entity => entity.EntityId ?? string.Empty),
			StringComparer.OrdinalIgnoreCase);
		if ((branch.ScopeAnchor == PolicyTargetPlanScopeAnchor.NamedKingdom && !availableKingdomIds.Contains(branch.AnchorKingdomId))
			|| branch.NamedKingdomIds.Any(id => !availableKingdomIds.Contains(id)))
		{
			error = "TargetPlan 的指定王国引用已经失效。";
			return false;
		}
		if (!allowPersistedReferences)
		{
			HashSet<string> allowedEntities = new HashSet<string>(context.AllowedEntityReferenceIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			if (branch.EntityReferences
				.Concat(branch.ExcludedEntityReferences)
				.Concat(new[] { branch.ReferenceClanId })
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Any(id => !allowedEntities.Contains(id)))
			{
				error = "TargetPlan 包含未由 C# 预验证的实体引用。";
				return false;
			}
			HashSet<string> allowedKingdoms = new HashSet<string>(context.AllowedKingdomReferenceIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase)
			{
				context.TargetKingdomId ?? string.Empty,
				context.IssuerKingdomId ?? string.Empty
			};
			if ((branch.ScopeAnchor == PolicyTargetPlanScopeAnchor.NamedKingdom && !allowedKingdoms.Contains(branch.AnchorKingdomId))
				|| branch.NamedKingdomIds.Any(id => !allowedKingdoms.Contains(id)))
			{
				error = "TargetPlan 包含未由 C# 预验证的王国引用。";
				return false;
			}
		}
		return true;
	}

	private static bool EntityMatchesUniverse(PolicyTargetEntitySnapshot entity, PolicyTargetPlanBranchSaveData branch)
	{
		if (entity == null || string.IsNullOrWhiteSpace(entity.EntityId))
		{
			return false;
		}
		if (branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs)
		{
			return string.Equals(entity.Kind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
				&& (entity.IsCity || entity.IsCastle)
				&& (branch.EntityType == PolicyTargetPlanEntityType.PrimaryFief
					|| (branch.EntityType == PolicyTargetPlanEntityType.Town && entity.IsCity)
					|| (branch.EntityType == PolicyTargetPlanEntityType.Castle && entity.IsCastle));
		}
		if (branch.Universe == PolicyTargetPlanUniverse.Clans)
		{
			return string.Equals(entity.Kind, PolicyTargetEntityKinds.Clan, StringComparison.OrdinalIgnoreCase);
		}
		return string.Equals(entity.Kind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsEntityTypeAllowed(PolicyTargetPlanUniverse universe, PolicyTargetPlanEntityType entityType)
	{
		return universe == PolicyTargetPlanUniverse.PrimaryFiefs
			? entityType == PolicyTargetPlanEntityType.PrimaryFief || entityType == PolicyTargetPlanEntityType.Town || entityType == PolicyTargetPlanEntityType.Castle
			: universe == PolicyTargetPlanUniverse.Clans
				? entityType == PolicyTargetPlanEntityType.Clan
				: entityType == PolicyTargetPlanEntityType.Kingdom;
	}

	private static bool IsSettlementMetric(PolicyTargetPlanMetric metric)
	{
		return metric == PolicyTargetPlanMetric.Food
			|| metric == PolicyTargetPlanMetric.Prosperity
			|| metric == PolicyTargetPlanMetric.Loyalty
			|| metric == PolicyTargetPlanMetric.Security
			|| metric == PolicyTargetPlanMetric.Hearth
			|| metric == PolicyTargetPlanMetric.Militia;
	}

	private static bool IsAggregateMetric(PolicyTargetPlanMetric metric)
	{
		return metric == PolicyTargetPlanMetric.Wealth
			|| metric == PolicyTargetPlanMetric.Influence
			|| metric == PolicyTargetPlanMetric.Strength
			|| metric == PolicyTargetPlanMetric.FiefCount;
	}

	private static string ResolveAnchorKingdomId(PolicyTargetPlanBranchSaveData branch, PolicyTargetPlanResolutionContext context)
	{
		if (branch.ScopeAnchor == PolicyTargetPlanScopeAnchor.IssuerKingdom)
		{
			return NormalizeId(context.IssuerKingdomId);
		}
		if (branch.ScopeAnchor == PolicyTargetPlanScopeAnchor.NamedKingdom)
		{
			return NormalizeId(branch.AnchorKingdomId);
		}
		return NormalizeId(context.TargetKingdomId);
	}

	private static bool TryReadReferencePosition(
		PolicyTargetPlanResolutionContext context,
		string anchorKingdomId,
		out float x,
		out float y)
	{
		HashSet<string> sourceIds = new HashSet<string>(context.SourceSettlementIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		List<PolicyTargetEntitySnapshot> sources = context.Snapshot.Entities
			.Where(entity => sourceIds.Contains(entity.EntityId ?? string.Empty) && entity.HasPosition)
			.ToList();
		if (sources.Count > 0)
		{
			x = sources.Average(entity => entity.PositionX);
			y = sources.Average(entity => entity.PositionY);
			return true;
		}
		if (context.Snapshot.Kingdoms != null
			&& context.Snapshot.Kingdoms.TryGetValue(anchorKingdomId ?? string.Empty, out PolicyTargetKingdomSnapshot kingdom)
			&& kingdom.HasPosition)
		{
			x = kingdom.PositionX;
			y = kingdom.PositionY;
			return true;
		}
		List<PolicyTargetEntitySnapshot> anchorEntities = context.Snapshot.Entities
			.Where(entity => SameId(entity.OwnerKingdomId, anchorKingdomId) && entity.HasPosition)
			.ToList();
		if (anchorEntities.Count > 0)
		{
			x = anchorEntities.Average(entity => entity.PositionX);
			y = anchorEntities.Average(entity => entity.PositionY);
			return true;
		}
		x = 0f;
		y = 0f;
		return false;
	}

	private static bool IsInDirection(PolicyTargetEntitySnapshot entity, PolicyTargetPlanDirection direction, float referenceX, float referenceY)
	{
		if (entity?.HasPosition != true)
		{
			return false;
		}
		switch (direction)
		{
			case PolicyTargetPlanDirection.North: return entity.PositionY > referenceY;
			case PolicyTargetPlanDirection.South: return entity.PositionY < referenceY;
			case PolicyTargetPlanDirection.East: return entity.PositionX > referenceX;
			case PolicyTargetPlanDirection.West: return entity.PositionX < referenceX;
			default: return true;
		}
	}

	private static bool TryReadMetric(PolicyTargetEntitySnapshot entity, PolicyTargetPlanMetric metric, out float value)
	{
		value = 0f;
		if (entity == null)
		{
			return false;
		}
		switch (metric)
		{
			case PolicyTargetPlanMetric.Wealth: value = entity.Wealth; break;
			case PolicyTargetPlanMetric.Influence: value = entity.Influence; break;
			case PolicyTargetPlanMetric.Strength: value = entity.Strength; break;
			case PolicyTargetPlanMetric.FiefCount: value = entity.FiefCount; break;
			case PolicyTargetPlanMetric.Food: value = entity.Food; break;
			case PolicyTargetPlanMetric.Prosperity: value = entity.Prosperity; break;
			case PolicyTargetPlanMetric.Loyalty: value = entity.Loyalty; break;
			case PolicyTargetPlanMetric.Security: value = entity.Security; break;
			case PolicyTargetPlanMetric.Hearth:
				if (!entity.Hearth.HasValue) return false;
				value = entity.Hearth.Value;
				break;
			case PolicyTargetPlanMetric.Militia:
				if (!entity.Militia.HasValue) return false;
				value = entity.Militia.Value;
				break;
			default:
				return false;
		}
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static bool MetricMatchesThreshold(
		float value,
		PolicyTargetPlanMetricComparison comparison,
		float? threshold)
	{
		if (comparison == PolicyTargetPlanMetricComparison.None)
		{
			return true;
		}
		if (!threshold.HasValue)
		{
			return false;
		}
		switch (comparison)
		{
			case PolicyTargetPlanMetricComparison.LessThan: return value < threshold.Value;
			case PolicyTargetPlanMetricComparison.LessThanOrEqual: return value <= threshold.Value;
			case PolicyTargetPlanMetricComparison.GreaterThan: return value > threshold.Value;
			case PolicyTargetPlanMetricComparison.GreaterThanOrEqual: return value >= threshold.Value;
			default: return false;
		}
	}

	private static PolicyTargetPlanDependencies ComputeDependencies(IEnumerable<PolicyTargetPlanBranchSaveData> branches)
	{
		PolicyTargetPlanDependencies result = PolicyTargetPlanDependencies.Structure;
		foreach (PolicyTargetPlanBranchSaveData branch in branches)
		{
			if (branch.Relation == PolicyTargetPlanRelation.Enemy || branch.Relation == PolicyTargetPlanRelation.Ally)
			{
				result |= PolicyTargetPlanDependencies.Relation;
			}
			if (branch.Metric != PolicyTargetPlanMetric.None
				|| branch.Distance != PolicyTargetPlanDistance.None
				|| branch.Cardinality != PolicyTargetPlanCardinality.All)
			{
				result |= PolicyTargetPlanDependencies.DailyMetric;
			}
		}
		return result;
	}

	private static PolicyTargetPlanResolutionStrategy ComputeResolutionStrategy(
		IReadOnlyCollection<PolicyTargetPlanBranchSaveData> branches,
		PolicyTargetPlanDependencies dependencies)
	{
		if ((dependencies & PolicyTargetPlanDependencies.DailyMetric) != 0)
		{
			return PolicyTargetPlanResolutionStrategy.DailyMetricDynamic;
		}
		if ((dependencies & PolicyTargetPlanDependencies.Relation) != 0)
		{
			return PolicyTargetPlanResolutionStrategy.RelationDynamic;
		}
		bool fixedTargets = branches != null && branches.Count > 0 && branches.All(branch =>
			branch.EntityReferences.Count > 0
			&& branch.Relation == PolicyTargetPlanRelation.Any
			&& branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.Any
			&& branch.Exclusions.All(value => value == PolicyTargetPlanExclusion.SpecificEntities)
			&& !branch.BorderOnly
			&& branch.Direction == PolicyTargetPlanDirection.Any
			&& branch.Distance == PolicyTargetPlanDistance.None
			&& branch.Metric == PolicyTargetPlanMetric.None
			&& branch.Cardinality == PolicyTargetPlanCardinality.All);
		return fixedTargets
			? PolicyTargetPlanResolutionStrategy.FixedTargets
			: PolicyTargetPlanResolutionStrategy.StructureDynamic;
	}

	private static string BuildNormalizedSignature(PolicyTargetPlanSaveData plan)
	{
		return "TP" + plan.PlanVersion.ToString(CultureInfo.InvariantCulture)
			+ ":R" + plan.ResolverVersion.ToString(CultureInfo.InvariantCulture)
			+ ":D" + ((int)plan.Dependencies).ToString(CultureInfo.InvariantCulture)
			+ ":S" + plan.ResolutionStrategy
			+ ":L" + Escape(plan.LegacySelectorId)
			+ ":" + string.Join("||", plan.Branches.Select(branch => BuildBranchSignature(branch, plan.PlanVersion)));
	}

	private static string BuildBranchSignature(PolicyTargetPlanBranchSaveData branch, int planVersion)
	{
		List<string> parts = new List<string>
		{
			branch.Universe.ToString(), branch.ScopeAnchor.ToString(), Escape(branch.AnchorKingdomId),
			branch.EntityType.ToString(), branch.Relation.ToString(), string.Join(",", branch.NamedKingdomIds.Select(Escape)),
			branch.OwnerClanPredicate.ToString(), Escape(branch.ReferenceClanId),
			string.Join(",", branch.Exclusions.Select(value => value.ToString())),
			string.Join(",", branch.EntityReferences.Select(Escape)), string.Join(",", branch.ExcludedEntityReferences.Select(Escape)),
			branch.BorderOnly ? "1" : "0", branch.Direction.ToString(), branch.Distance.ToString(), branch.Metric.ToString(),
			branch.SortDirection.ToString(), branch.Cardinality.ToString(), branch.Limit.ToString(CultureInfo.InvariantCulture)
		};
		if (planVersion >= CurrentPlanVersion)
		{
			parts.Add(branch.MetricComparison.ToString());
			parts.Add(branch.MetricThreshold.HasValue
				? branch.MetricThreshold.Value.ToString("R", CultureInfo.InvariantCulture)
				: string.Empty);
		}
		return string.Join("|", parts);
	}

	private static bool IsSupportedVersionPair(int planVersion, int resolverVersion)
	{
		return (planVersion == LegacyPlanVersion && resolverVersion == LegacyResolverVersion)
			|| (planVersion == CurrentPlanVersion && resolverVersion == CurrentResolverVersion);
	}

	private static bool HasPair(IReadOnlyCollection<string> pairs, string left, string right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		string key = string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0
			? left + "\n" + right
			: right + "\n" + left;
		return (pairs ?? Array.Empty<string>()).Contains(key, StringComparer.OrdinalIgnoreCase);
	}

	private static float DistanceSquared(float leftX, float leftY, float rightX, float rightY)
	{
		float x = leftX - rightX;
		float y = leftY - rightY;
		return x * x + y * y;
	}

	private static bool SameId(string left, string right)
	{
		return !string.IsNullOrWhiteSpace(left)
			&& !string.IsNullOrWhiteSpace(right)
			&& string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static List<string> NormalizeIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Select(NormalizeId)
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static string NormalizeId(string value)
	{
		return (value ?? string.Empty).Trim();
	}

	private static string Escape(string value)
	{
		return NormalizeId(value).Replace("\\", "\\\\").Replace("|", "\\|").Replace(",", "\\,");
	}
}
