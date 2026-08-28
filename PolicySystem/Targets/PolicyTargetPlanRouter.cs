using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnimusForge.PolicyTargets;

internal sealed class PolicyTargetPlanIntentAtom
{
	internal string Id { get; set; } = string.Empty;

	internal string Group { get; set; } = string.Empty;

	internal string Document { get; set; } = string.Empty;

	internal IReadOnlyList<string> Seeds { get; set; } = Array.Empty<string>();
}

internal sealed class PolicyTargetPlanCandidate
{
	internal PolicyTargetPlanSaveData Plan { get; set; }

	internal string DisplayName { get; set; } = string.Empty;

	internal string Evidence { get; set; } = string.Empty;

	internal float RecallScore { get; set; }

	internal float SemanticScore { get; set; }

	internal string IntentLeg { get; set; } = string.Empty;

	internal string EvidenceKind { get; set; } = string.Empty;

	internal IReadOnlyList<string> AtomIds { get; set; } = Array.Empty<string>();
}

internal enum PolicyTargetPlanRouteIssueKind
{
	NoIntent,
	InvalidExplicitTarget,
	InternalCandidateInvalid,
	MissingRuntimeAnchor
}

internal sealed class PolicyTargetPlanRouteIssue
{
	internal PolicyTargetPlanRouteIssueKind Kind { get; set; }

	internal string IntentLeg { get; set; } = string.Empty;

	internal string CandidateSignature { get; set; } = string.Empty;

	internal string Stage { get; set; } = string.Empty;

	internal string EvidenceKind { get; set; } = string.Empty;

	internal string Message { get; set; } = string.Empty;
}

internal sealed class PolicyTargetPlanRouteResult
{
	internal IReadOnlyList<PolicyTargetPlanCandidate> Candidates { get; set; }
		= Array.Empty<PolicyTargetPlanCandidate>();

	internal IReadOnlyList<string> MatchedExistingHandleKeys { get; set; }
		= Array.Empty<string>();

	internal IReadOnlyList<PolicyTargetPlanRouteIssue> Issues { get; set; }
		= Array.Empty<PolicyTargetPlanRouteIssue>();

	internal bool HasExplicitTargetIntent { get; set; }

	internal bool HasLegalTarget => Candidates.Count > 0 || MatchedExistingHandleKeys.Count > 0;

	internal bool ShouldRejectPolicy => HasExplicitTargetIntent
		&& !HasLegalTarget
		&& Issues.Any(issue => issue != null
			&& (issue.Kind == PolicyTargetPlanRouteIssueKind.InvalidExplicitTarget
				|| issue.Kind == PolicyTargetPlanRouteIssueKind.MissingRuntimeAnchor));
}

internal static class PolicyTargetPlanRouter
{
	internal const int QueryInputLimit = 6;

	internal const int RecallPerInputLimit = 8;

	private const float SemanticEmbeddingThreshold = 0.45f;

	private const float SemanticEmbeddingMargin = 0.02f;

	private const int MaximumQueryChars = 1600;

	private static readonly object IndexSync = new object();

	private static readonly object QueryEmbeddingSync = new object();

	private static readonly Dictionary<string, float[]> QueryEmbeddingCache
		= new Dictionary<string, float[]>(StringComparer.Ordinal);

	private const int QueryEmbeddingCacheLimit = 64;

	private static readonly IReadOnlyList<PolicyTargetPlanIntentAtom> AtomSnapshot = BuildAtoms();

	private static readonly KeyValuePair<PolicyTargetPlanMetric, string>[] MetricThresholdAliases =
	{
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, "附属村庄平均户数"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, "hearth"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, "炉户"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, "户数"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Militia, "militia"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Militia, "民兵")
	};

	private static readonly KeyValuePair<string, PolicyTargetPlanMetricComparison>[] MetricComparisonCues =
	{
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("不高于", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("不超过", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("小于等于", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("<=", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("至多", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("不低于", PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("大于等于", PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(">=", PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("至少", PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("低于", PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("少于", PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("小于", PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("<", PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("高于", PolicyTargetPlanMetricComparison.GreaterThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("超过", PolicyTargetPlanMetricComparison.GreaterThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("大于", PolicyTargetPlanMetricComparison.GreaterThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(">", PolicyTargetPlanMetricComparison.GreaterThan)
	};

	private static IReadOnlyList<IndexedIntentSeed> _index = Array.Empty<IndexedIntentSeed>();

	private static volatile bool _indexAttempted;

	internal static IReadOnlyList<PolicyTargetPlanIntentAtom> Atoms => AtomSnapshot;

	internal static PolicyTargetPlanRouteResult Route(
		string queryText,
		PolicyTargetSemanticContext context)
	{
		return RouteCore(queryText, context, allowSemanticRecall: true);
	}

	internal static PolicyTargetPlanRouteResult RouteDeterministicForPlayer(
		string queryText,
		PolicyTargetSemanticContext context)
	{
		return RouteCore(queryText, context, allowSemanticRecall: false);
	}

	private static PolicyTargetPlanRouteResult RouteCore(
		string queryText,
		PolicyTargetSemanticContext context,
		bool allowSemanticRecall)
	{
		string query = Limit((queryText ?? string.Empty).Trim(), MaximumQueryChars);
		bool hasExplicitTargetIntent = HasExplicitTargetIntent(query, context);
		bool succeeded = TryRouteCore(
			query,
			context,
			allowSemanticRecall,
			out IReadOnlyList<PolicyTargetPlanCandidate> candidates,
			out string error);
		if (succeeded && candidates.Count > 0)
		{
			return new PolicyTargetPlanRouteResult
			{
				Candidates = candidates,
				HasExplicitTargetIntent = hasExplicitTargetIntent
			};
		}
		if (succeeded || string.IsNullOrWhiteSpace(error))
		{
			return new PolicyTargetPlanRouteResult
			{
				HasExplicitTargetIntent = hasExplicitTargetIntent,
				Issues = new[]
				{
					new PolicyTargetPlanRouteIssue
					{
						Kind = PolicyTargetPlanRouteIssueKind.NoIntent,
						Stage = "route",
						EvidenceKind = hasExplicitTargetIntent ? "explicit-unresolved" : "abstain"
					}
				}
			};
		}
		PolicyTargetPlanRouteIssueKind kind = hasExplicitTargetIntent
			? IsMissingRuntimeAnchorError(error)
				? PolicyTargetPlanRouteIssueKind.MissingRuntimeAnchor
				: PolicyTargetPlanRouteIssueKind.InvalidExplicitTarget
			: PolicyTargetPlanRouteIssueKind.InternalCandidateInvalid;
		return new PolicyTargetPlanRouteResult
		{
			HasExplicitTargetIntent = hasExplicitTargetIntent,
			Issues = new[]
			{
				new PolicyTargetPlanRouteIssue
				{
					Kind = kind,
					Stage = "route",
					EvidenceKind = hasExplicitTargetIntent ? "explicit" : "embedding-only",
					Message = error
				}
			}
		};
	}

	internal static bool TryRoute(
		string queryText,
		PolicyTargetSemanticContext context,
		out IReadOnlyList<PolicyTargetPlanCandidate> candidates,
		out string error)
	{
		PolicyTargetPlanRouteResult result = Route(queryText, context);
		candidates = result.Candidates;
		error = result.ShouldRejectPolicy
			? result.Issues.Select(issue => issue?.Message).FirstOrDefault(message => !string.IsNullOrWhiteSpace(message)) ?? "明确目标无法安全解析。"
			: string.Empty;
		return !result.ShouldRejectPolicy;
	}

	private static bool TryRouteCore(
		string queryText,
		PolicyTargetSemanticContext context,
		bool allowSemanticRecall,
		out IReadOnlyList<PolicyTargetPlanCandidate> candidates,
		out string error)
	{
		candidates = Array.Empty<PolicyTargetPlanCandidate>();
		error = string.Empty;
		string query = Limit((queryText ?? string.Empty).Trim(), MaximumQueryChars);
		if (query.Length == 0 || context?.Snapshot?.Entities == null)
		{
			return true;
		}
		if (!TryBuildDeterministicClanTargetCandidates(
			query,
			context,
			out IReadOnlyList<PolicyTargetPlanCandidate> deterministicClanCandidates,
			out bool deterministicClanIntent,
			out error))
		{
			return false;
		}
		if (deterministicClanIntent)
		{
			candidates = deterministicClanCandidates;
			return true;
		}
		List<PolicyTargetPlanBranchSaveData> branches = new List<PolicyTargetPlanBranchSaveData>();
		List<string> evidence = new List<string>();
		float recallScore = 0f;
		float semanticScore = 0f;
		PolicyTargetPlanSaveData normalized = null;
		bool acceptedUnion = false;
		if (TrySplitSingleUnionClauses(query, out string leftClause, out string rightClause))
		{
			bool leftBuilt = TryBuildBranch(
				leftClause,
				context,
				out PolicyTargetPlanBranchSaveData leftBranch,
				out string leftEvidence,
				out float leftRecall,
				out float leftSemanticScore,
				allowSemanticRecall,
				out _);
			bool rightBuilt = TryBuildBranch(
				rightClause,
				context,
				out PolicyTargetPlanBranchSaveData rightBranch,
				out string rightEvidence,
				out float rightRecall,
				out float rightSemanticScore,
				allowSemanticRecall,
				out _);
			if (leftBuilt
				&& rightBuilt
				&& leftBranch.Universe == rightBranch.Universe)
			{
				PolicyTargetPlanSaveData unionPlan = new PolicyTargetPlanSaveData
				{
					Branches = new List<PolicyTargetPlanBranchSaveData> { leftBranch, rightBranch }
				};
				if (PolicyTargetPlanResolver.TryNormalizeAndValidate(
					unionPlan,
					out PolicyTargetPlanSaveData normalizedUnion,
					out _))
				{
					branches.Add(leftBranch);
					branches.Add(rightBranch);
					evidence.Add(leftEvidence);
					evidence.Add(rightEvidence);
					recallScore = Math.Max(leftRecall, rightRecall);
					semanticScore = Math.Max(leftSemanticScore, rightSemanticScore);
					normalized = normalizedUnion;
					acceptedUnion = true;
				}
			}
		}
		if (!acceptedUnion)
		{
			if (!TryBuildBranch(
				query,
				context,
				out PolicyTargetPlanBranchSaveData branch,
				out string branchEvidence,
				out float branchRecall,
				out float branchSemanticScore,
				allowSemanticRecall,
				out string branchError))
			{
				if (!string.IsNullOrWhiteSpace(branchError))
				{
					error = branchError;
					return false;
				}
				return true;
			}
			branches.Add(branch);
			evidence.Add(branchEvidence);
			recallScore = branchRecall;
			semanticScore = branchSemanticScore;
		}
		if (normalized == null
			&& !PolicyTargetPlanResolver.TryNormalizeAndValidate(
				new PolicyTargetPlanSaveData { Branches = branches },
				out normalized,
				out error))
		{
			return false;
		}
		candidates = BuildBoundedPlanCandidates(
			normalized,
			context,
			string.Join("；", evidence.Where(value => !string.IsNullOrWhiteSpace(value))),
			recallScore,
			semanticScore);
		return true;
	}

	private static bool HasExplicitTargetIntent(string query, PolicyTargetSemanticContext context)
	{
		if (string.IsNullOrWhiteSpace(query) || context?.Snapshot?.Entities == null)
		{
			return false;
		}
		string normalizedQuery = NormalizeText(query);
		List<PolicyTargetEntitySnapshot> mentionedEntities = FindExactMentionedEntities(
			context.Snapshot.Entities,
			normalizedQuery,
			PolicyTargetEntityKinds.Settlement,
			context.StrictEntityEvidence)
			.Concat(FindExactMentionedEntities(context.Snapshot.Entities, normalizedQuery, PolicyTargetEntityKinds.Clan, context.StrictEntityEvidence))
			.Concat(FindExactMentionedEntities(context.Snapshot.Entities, normalizedQuery, PolicyTargetEntityKinds.Kingdom, context.StrictEntityEvidence))
			.ToList();
		if (mentionedEntities.Count > 0)
		{
			return true;
		}
		string typeCueQuery = BuildUnboundTypeCueText(normalizedQuery, mentionedEntities);
		return HasExplicitTargetBoundary(FindExplicitAtomIds(normalizedQuery, typeCueQuery));
	}

	private static bool IsMissingRuntimeAnchorError(string error)
	{
		return !string.IsNullOrWhiteSpace(error)
			&& (error.IndexOf("锚点", StringComparison.Ordinal) >= 0
				|| error.IndexOf("引用已经失效", StringComparison.Ordinal) >= 0);
	}

	private static IReadOnlyList<PolicyTargetPlanCandidate> BuildBoundedPlanCandidates(
		PolicyTargetPlanSaveData normalized,
		PolicyTargetSemanticContext context,
		string evidence,
		float recallScore,
		float semanticScore)
	{
		PolicyTargetPlanBranchSaveData branch = normalized?.Branches?.Count == 1
			? normalized.Branches[0]
			: null;
		List<string> namedKingdomIds = branch?.Relation == PolicyTargetPlanRelation.Specific
			? branch.NamedKingdomIds
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.Ordinal)
				.ToList()
			: new List<string>();
		if (namedKingdomIds.Count <= 1 || namedKingdomIds.Count > PolicyTargetPlanResolver.MaximumBranches)
		{
			return new[] { CreatePlanCandidate(normalized, context, evidence, recallScore, semanticScore) };
		}

		// A full policy can mention the issuer, a recipient and several co-targets in
		// one document. Do not guess their grammatical role and never widen that
		// ambiguity to Relation.Any. Emit one exact, bounded handle per mentioned
		// kingdom so the effect postprocessor can bind each effect to the right one.
		List<PolicyTargetPlanCandidate> result = new List<PolicyTargetPlanCandidate>(namedKingdomIds.Count);
		foreach (string kingdomId in namedKingdomIds)
		{
			PolicyTargetPlanSaveData variant = PolicyTargetPlanResolver.Clone(normalized);
			if (variant?.Branches?.Count != 1)
			{
				continue;
			}
			variant.Branches[0].NamedKingdomIds = new List<string> { kingdomId };
			variant.NormalizedSignature = string.Empty;
			if (PolicyTargetPlanResolver.TryNormalizeAndValidate(variant, out PolicyTargetPlanSaveData bounded, out _))
			{
				result.Add(CreatePlanCandidate(
					bounded,
					context,
					evidence + ";bound-kingdom=" + kingdomId,
					recallScore,
					semanticScore));
			}
		}
		return result.Count > 0
			? result
			: new[] { CreatePlanCandidate(normalized, context, evidence, recallScore, semanticScore) };
	}

	private static PolicyTargetPlanCandidate CreatePlanCandidate(
		PolicyTargetPlanSaveData plan,
		PolicyTargetSemanticContext context,
		string evidence,
		float recallScore,
		float semanticScore)
	{
		string namedKingdomName = plan?.Branches?.Count == 1
			? plan.Branches[0].NamedKingdomIds
				.Select(id => context?.Snapshot?.Entities?.FirstOrDefault(entity => entity != null
					&& string.Equals(entity.Kind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase)
					&& SameId(entity.EntityId, id))?.DisplayName)
				.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
			: string.Empty;
		return new PolicyTargetPlanCandidate
		{
			Plan = plan,
			DisplayName = (namedKingdomName ?? string.Empty).Trim().Length > 0
				? namedKingdomName.Trim() + " · " + BuildDisplayName(plan)
				: BuildDisplayName(plan),
			Evidence = evidence ?? string.Empty,
			RecallScore = recallScore,
			SemanticScore = semanticScore,
			EvidenceKind = (evidence ?? string.Empty).IndexOf("entities=", StringComparison.Ordinal) >= 0
				? "verified-entity"
				: "explicit-lexical",
			AtomIds = ExtractEvidenceAtomIds(evidence)
		};
	}

	private static IReadOnlyList<string> ExtractEvidenceAtomIds(string evidence)
	{
		return (evidence ?? string.Empty)
			.Replace('；', ';')
			.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Where(part => part.StartsWith("atoms=", StringComparison.Ordinal))
			.SelectMany(part => part.Substring("atoms=".Length)
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			.Select(atomId => atomId.Trim())
			.Where(atomId => atomId.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(atomId => atomId, StringComparer.Ordinal)
			.ToArray();
	}

	private static bool TryBuildDeterministicClanTargetCandidates(
		string query,
		PolicyTargetSemanticContext context,
		out IReadOnlyList<PolicyTargetPlanCandidate> candidates,
		out bool matched,
		out string error)
	{
		List<PolicyTargetPlanCandidate> result = new List<PolicyTargetPlanCandidate>();
		candidates = result;
		matched = false;
		error = string.Empty;
		string normalizedQuery = NormalizeText(query);
		bool centralizationCue = ContainsAny(
			normalizedQuery,
			"中央集权",
			"加强集权",
			"强化集权",
			"权力集中",
			"权力收归中央",
			"收归中央",
			"削弱领主",
			"降低领主影响力",
			"领主影响力降低",
			"领主的影响力降低",
			"减少领主影响力",
			"领主影响力减少",
			"领主的影响力减少",
			"家族影响力降低",
			"家族的影响力降低",
			"家族影响力减少",
			"家族的影响力减少");
		bool allTargetKingdomClansCue = ContainsAny(
			normalizedQuery,
			"目标王国全部家族",
			"目标王国所有家族",
			"王国内全部家族",
			"王国内所有家族",
			"本国全部家族",
			"本国所有家族",
			"国内全部家族",
			"国内所有家族",
			"全国领主",
			"全体领主",
			"所有领主",
			"全国贵族",
			"全体贵族",
			"所有贵族");
		bool otherTargetKingdomClansCue = ContainsAny(
			normalizedQuery,
			"除发布者家族外",
			"发布者家族以外",
			"除提案方家族外",
			"提案方家族以外",
			"除发布者氏族外",
			"发布者氏族以外",
			"其他家族",
			"别的家族",
			"其余家族",
			"其他氏族",
			"其他领主",
			"其余领主",
			"其他贵族",
			"其余贵族");
		bool proposerClanCue = ContainsAny(
			normalizedQuery,
			"发布者家族",
			"政策发布者家族",
			"提案方家族",
			"提案者家族",
			"发布者氏族",
			"提案方氏族",
			"发布者影响力",
			"发布者的影响力",
			"提案者影响力",
			"提案者的影响力")
			|| (SameId(context.PlayerClanId, context.ProposerClanId)
				&& ContainsAny(
					normalizedQuery,
					"玩家家族",
					"玩家氏族",
					"玩家影响力",
					"玩家的影响力"));
		bool affectedRegionOwnerCue = ContainsAny(
			normalizedQuery,
			"受影响地区的领主",
			"受影响地区领主",
			"影响地区的领主",
			"影响地区领主",
			"作用地区的领主",
			"政策地区的领主",
			"这些地区的领主",
			"相关地区的领主",
			"受影响地区的家族",
			"影响地区的家族",
			"受影响地区所有者",
			"影响地区所有者",
			"当地领主",
			"当地家族");
		if (!centralizationCue
			&& !allTargetKingdomClansCue
			&& !otherTargetKingdomClansCue
			&& !proposerClanCue
			&& !affectedRegionOwnerCue)
		{
			// 绝大多数政策不需要家族集合快捷句柄；避免在普通生成路径
			// 额外扫描一次实体快照，继续交给既有组合路由即可。
			return true;
		}
		bool compositionalClanFilterCue = ContainsAny(
			normalizedQuery,
			"敌国",
			"交战国",
			"盟国",
			"外国",
			"国外",
			"边境",
			"前线",
			"北方",
			"南方",
			"东方",
			"西方",
			"最近",
			"最远",
			"最高",
			"最低",
			"最多",
			"最少",
			"最强",
			"最弱",
			"最富",
			"最穷",
			"排名",
			"前几个",
			"后几个")
			|| FindExactMentionedEntities(
				context.Snapshot.Entities,
				normalizedQuery,
				PolicyTargetEntityKinds.Clan,
				context.StrictEntityEvidence).Count > 0;

		// “家族封地/领主领地”描述的是地理目标，不得被“其他家族”
		// 的词面提示错误收窄成家族执行目标。
		bool explicitClanFiefCue = ContainsAny(
			normalizedQuery,
			"家族封地",
			"家族领地",
			"氏族封地",
			"氏族领地",
			"领主封地",
			"领主领地",
			"贵族封地",
			"贵族领地");
		string typeCueQuery = BuildUnboundTypeCueText(normalizedQuery, context.Snapshot.Entities);
		HashSet<string> explicitAtomIds = FindExplicitAtomIds(normalizedQuery, typeCueQuery);
		NormalizeExplicitTypeCues(explicitAtomIds, explicitClanFiefCue, typeCueQuery);
		bool hasExplicitSettlementTarget = explicitAtomIds.Contains("type_primary_fief")
			|| explicitAtomIds.Contains("type_town")
			|| explicitAtomIds.Contains("type_castle");
		bool hasExplicitOwnerCondition = explicitAtomIds.Contains("exclude_player_clan")
			|| explicitAtomIds.Contains("owner_other_clans")
			|| explicitAtomIds.Contains("owner_proposer_clan");
		if ((explicitClanFiefCue || hasExplicitSettlementTarget && hasExplicitOwnerCondition)
			&& !affectedRegionOwnerCue)
		{
			// Towns, castles and fiefs remain the executable targets. Clan wording only
			// constrains their owner, so the compound TargetPlan router must compose it.
			return true;
		}
		if (compositionalClanFilterCue && !centralizationCue && !affectedRegionOwnerCue)
		{
			// 排名、关系、地理和点名条件继续交给通用 TargetPlan 组合器，
			// 避免确定性快捷句柄丢失已有过滤语义。
			return true;
		}

		if (centralizationCue)
		{
			otherTargetKingdomClansCue = true;
			proposerClanCue = true;
			allTargetKingdomClansCue = false;
		}
		else if (allTargetKingdomClansCue && proposerClanCue)
		{
			// 两条腿都被明确提及时保留“全部家族”和“发布者家族”两个句柄；
			// 只有集权语义才把前者确定性改写为“除发布者外”。
			otherTargetKingdomClansCue = false;
		}

		if (allTargetKingdomClansCue
			&& !TryAddDeterministicCandidate(
				result,
				CreateClanTargetPlan(PolicyTargetPlanOwnerClanPredicate.Any, PolicyTargetPlanRelation.Domestic),
				"目标王国全部家族",
				"deterministic=target-kingdom-all-clans",
				out error))
		{
			return false;
		}
		if (otherTargetKingdomClansCue
			&& !TryAddDeterministicCandidate(
				result,
				CreateClanTargetPlan(PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan, PolicyTargetPlanRelation.Domestic),
				"目标王国其他家族（不含发布者家族）",
				"deterministic=target-kingdom-other-clans",
				out error))
		{
			return false;
		}
		if (proposerClanCue
			&& !TryAddDeterministicCandidate(
				result,
				CreateClanTargetPlan(PolicyTargetPlanOwnerClanPredicate.ProposerClan, PolicyTargetPlanRelation.Any),
				"发布者家族",
				"deterministic=proposer-clan",
				out error))
		{
			return false;
		}
		if (affectedRegionOwnerCue)
		{
			HashSet<string> sourceSettlementIds = new HashSet<string>(
				(context.SourceSettlementIds ?? Array.Empty<string>())
					.Where(id => !string.IsNullOrWhiteSpace(id))
					.Select(id => id.Trim()),
				StringComparer.OrdinalIgnoreCase);
			List<string> primarySettlementIds = context.Snapshot.Entities
				.Where(entity => entity != null
					&& string.Equals(entity.Kind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
					&& (entity.IsCity || entity.IsCastle)
					&& sourceSettlementIds.Contains(entity.EntityId ?? string.Empty))
				.Select(entity => entity.EntityId.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.Ordinal)
				.ToList();
			if (primarySettlementIds.Count > 0
				&& !TryAddDeterministicCandidate(
					result,
					CreateAffectedRegionOwnerPlan(primarySettlementIds),
					"受影响地区当前所有者家族",
					"deterministic=affected-region-current-owner-clans",
					out error))
			{
				return false;
			}
		}
		// 明确写了“受影响地区所有者”但当前请求没有一级封地锚点时，
		// 安全放弃该句柄，不得回落并扩大成目标王国全部家族。
		matched = result.Count > 0 || affectedRegionOwnerCue;
		candidates = result;
		return true;
	}

	private static PolicyTargetPlanSaveData CreateClanTargetPlan(
		PolicyTargetPlanOwnerClanPredicate ownerPredicate,
		PolicyTargetPlanRelation relation)
	{
		return new PolicyTargetPlanSaveData
		{
			Branches = new List<PolicyTargetPlanBranchSaveData>
			{
				new PolicyTargetPlanBranchSaveData
				{
					Universe = PolicyTargetPlanUniverse.Clans,
					ScopeAnchor = PolicyTargetPlanScopeAnchor.TargetKingdom,
					EntityType = PolicyTargetPlanEntityType.Clan,
					Relation = relation,
					OwnerClanPredicate = ownerPredicate,
					Cardinality = PolicyTargetPlanCardinality.All
				}
			}
		};
	}

	private static PolicyTargetPlanSaveData CreateAffectedRegionOwnerPlan(IReadOnlyCollection<string> settlementIds)
	{
		return new PolicyTargetPlanSaveData
		{
			Branches = new List<PolicyTargetPlanBranchSaveData>
			{
				new PolicyTargetPlanBranchSaveData
				{
					Universe = PolicyTargetPlanUniverse.PrimaryFiefs,
					ScopeAnchor = PolicyTargetPlanScopeAnchor.TargetKingdom,
					EntityType = PolicyTargetPlanEntityType.PrimaryFief,
					Relation = PolicyTargetPlanRelation.Any,
					OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.Any,
					EntityReferences = (settlementIds ?? Array.Empty<string>()).ToList(),
					Cardinality = PolicyTargetPlanCardinality.All
				}
			}
		};
	}

	private static bool TryAddDeterministicCandidate(
		ICollection<PolicyTargetPlanCandidate> result,
		PolicyTargetPlanSaveData source,
		string displayName,
		string evidence,
		out string error)
	{
		error = string.Empty;
		if (!PolicyTargetPlanResolver.TryNormalizeAndValidate(source, out PolicyTargetPlanSaveData normalized, out error))
		{
			return false;
		}
		if (result.Any(candidate => string.Equals(
			candidate?.Plan?.NormalizedSignature,
			normalized.NormalizedSignature,
			StringComparison.Ordinal)))
		{
			return true;
		}
		result.Add(new PolicyTargetPlanCandidate
		{
			Plan = normalized,
			DisplayName = displayName ?? string.Empty,
			Evidence = evidence ?? string.Empty,
			RecallScore = 1f,
			SemanticScore = 1f,
			EvidenceKind = "explicit-deterministic"
		});
		return true;
	}

	private static bool TryBuildBranch(
		string query,
		PolicyTargetSemanticContext context,
		out PolicyTargetPlanBranchSaveData branch,
		out string evidence,
		out float recallScore,
		out float semanticScore,
		bool allowSemanticRecall,
		out string error)
	{
		branch = null;
		evidence = string.Empty;
		recallScore = 0f;
		semanticScore = 0f;
		error = string.Empty;
		string normalizedQuery = NormalizeText(query);
		List<PolicyTargetEntitySnapshot> mentionedSettlements = FindExactMentionedEntities(
			context.Snapshot.Entities,
			normalizedQuery,
			PolicyTargetEntityKinds.Settlement,
			context.StrictEntityEvidence);
		List<PolicyTargetEntitySnapshot> mentionedClans = FindExactMentionedEntities(
			context.Snapshot.Entities,
			normalizedQuery,
			PolicyTargetEntityKinds.Clan,
			context.StrictEntityEvidence);
		List<PolicyTargetEntitySnapshot> mentionedKingdoms = FindExactMentionedEntities(
			context.Snapshot.Entities,
			normalizedQuery,
			PolicyTargetEntityKinds.Kingdom,
			context.StrictEntityEvidence);
		List<PolicyTargetEntitySnapshot> excludedSettlements = mentionedSettlements
			.Where(entity => IsExplicitlyExcludedEntity(normalizedQuery, entity))
			.ToList();
		List<PolicyTargetEntitySnapshot> includedSettlements = mentionedSettlements
			.Except(excludedSettlements)
			.ToList();
		List<PolicyTargetEntitySnapshot> excludedClans = mentionedClans
			.Where(entity => IsExplicitlyExcludedEntity(normalizedQuery, entity))
			.ToList();
		List<PolicyTargetEntitySnapshot> includedClans = mentionedClans
			.Except(excludedClans)
			.ToList();
		List<PolicyTargetEntitySnapshot> excludedKingdoms = mentionedKingdoms
			.Where(entity => IsExplicitlyExcludedEntity(normalizedQuery, entity))
			.ToList();
		List<PolicyTargetEntitySnapshot> includedKingdoms = mentionedKingdoms
			.Except(excludedKingdoms)
			.ToList();

		string typeCueQuery = BuildUnboundTypeCueText(
			normalizedQuery,
			mentionedSettlements.Concat(mentionedClans).Concat(mentionedKingdoms));
		HashSet<string> explicitAtomIds = FindExplicitAtomIds(normalizedQuery, typeCueQuery);
		bool rejectedGenericForeignExpansion = context.StrictEntityEvidence
			&& explicitAtomIds.Remove("relation_foreign");
		if (rejectedGenericForeignExpansion
			&& mentionedSettlements.Count == 0
			&& mentionedClans.Count == 0
			&& mentionedKingdoms.Count == 0)
		{
			return false;
		}
		TryParseMetricThreshold(
			normalizedQuery,
			out bool hasMetricThreshold,
			out PolicyTargetPlanMetric thresholdMetric,
			out PolicyTargetPlanMetricComparison thresholdComparison,
			out float thresholdValue,
			out bool ambiguousMetricThreshold);
		if (ambiguousMetricThreshold)
		{
			return false;
		}
		bool explicitMetricAscending = ContainsAny(normalizedQuery, "升序", "从低到高", "由低到高");
		bool explicitMetricDescending = ContainsAny(normalizedQuery, "降序", "从高到低", "由高到低");
		if (explicitMetricAscending && explicitMetricDescending)
		{
			return false;
		}
		if (hasMetricThreshold)
		{
			explicitAtomIds.Remove(BuildMetricAtomId(thresholdMetric, false));
			explicitAtomIds.Remove(BuildMetricAtomId(thresholdMetric, true));
			bool high = explicitMetricDescending
				|| (!explicitMetricAscending
					&& (thresholdComparison == PolicyTargetPlanMetricComparison.GreaterThan
						|| thresholdComparison == PolicyTargetPlanMetricComparison.GreaterThanOrEqual));
			explicitAtomIds.Add(BuildMetricAtomId(
				thresholdMetric,
				high));
		}
		if (excludedSettlements.Count > 0 || excludedClans.Count > 0 || excludedKingdoms.Count > 0)
		{
			explicitAtomIds.Add("exclude_specific_entity");
		}
		bool fiefCue = ContainsAny(normalizedQuery, "城镇", "城市", "城堡", "领地", "封地", "地方", "定居点", "边境", "粮食", "繁荣", "忠诚", "治安", "户数", "炉户", "hearth", "民兵", "militia");
		bool clanFiefCue = ContainsAny(normalizedQuery, "家族封地", "家族领地", "氏族封地", "氏族领地", "贵族封地", "贵族领地");
		bool isLocalScope = string.Equals(context.Scope, "local", StringComparison.OrdinalIgnoreCase);
		NormalizeExplicitTypeCues(explicitAtomIds, clanFiefCue, typeCueQuery);
		if (includedSettlements.Count > 0 || includedClans.Count > 0 || includedKingdoms.Count > 0)
		{
			// Exact runtime-verified entity names are stronger than generic relation
			// words in the same policy prose. Phrases such as “敌国南帝国境内”
			// and “充实本国财政” must not be rejected before the named scope is bound.
			explicitAtomIds.Remove("relation_domestic");
			explicitAtomIds.Remove("relation_enemy");
			explicitAtomIds.Remove("relation_ally");
			explicitAtomIds.Remove("relation_foreign");
		}
		if (!TryValidateExplicitAtomGroups(explicitAtomIds, out error))
		{
			if (error.IndexOf(" type ", StringComparison.Ordinal) >= 0)
			{
				// The router sees the whole policy document, so two executable target
				// types can represent separate effects rather than corrupt technical data.
				// Do not collapse them by precedence and do not abort policy evaluation;
				// abstain so fixed/semantic handles and effect-specific validation remain.
				error = string.Empty;
			}
			return false;
		}
		if (isLocalScope
			&& explicitAtomIds.Contains("exclude_publication")
			&& (context.SourceSettlementIds?.Count ?? 0) == 0)
		{
			// Explicit operands are validated before ONNX so an unavailable model cannot
			// hide a deterministic request error behind an embedding failure.
			error = "“其他地方”缺少当前发布地锚点。";
			return false;
		}
		bool hasExactEntityOperand = mentionedSettlements.Count > 0
			|| mentionedClans.Count > 0
			|| mentionedKingdoms.Count > 0;
		if (!hasExactEntityOperand && !HasExplicitTargetBoundary(explicitAtomIds))
		{
			// Embedding is a ranker, not an authority for hard target operators.
			// Ordinary policy prose without a verified target boundary abstains before
			// any ONNX work, avoiding both false targets and unnecessary allocations.
			return false;
		}
		bool strictNamedEntityFallback = (mentionedSettlements.Count > 0 || mentionedClans.Count > 0 || mentionedKingdoms.Count > 0)
			&& explicitAtomIds.All(IsTypeAtom);
		Dictionary<string, ScoredAtom> selected = new Dictionary<string, ScoredAtom>(StringComparer.Ordinal);
		bool selectedWithEmbedding = false;
		if (allowSemanticRecall)
		{
			selectedWithEmbedding = TrySelectAtoms(
				query,
				explicitAtomIds,
				out selected,
				out recallScore,
				out semanticScore);
		}
		else
		{
			foreach (PolicyTargetPlanIntentAtom atom in AtomSnapshot.Where(atom => explicitAtomIds.Contains(atom.Id)))
			{
				selected[atom.Group] = new ScoredAtom { Atom = atom };
			}
		}
		bool hasDeterministicNewMetric = explicitAtomIds.Contains("metric_hearth_high")
			|| explicitAtomIds.Contains("metric_hearth_low")
			|| explicitAtomIds.Contains("metric_militia_high")
			|| explicitAtomIds.Contains("metric_militia_low");
		bool deterministicMetricFallback = hasDeterministicNewMetric
			&& (hasMetricThreshold || HasExplicitAtomInGroup(explicitAtomIds, "type"));
		bool deterministicExplicitFallback = HasExplicitTargetBoundary(explicitAtomIds)
			|| mentionedSettlements.Count > 0
			|| mentionedClans.Count > 0
			|| mentionedKingdoms.Count > 0;
		if (!selectedWithEmbedding
			&& !strictNamedEntityFallback
			&& !deterministicMetricFallback
			&& !deterministicExplicitFallback)
		{
			// No explicit, runtime-verifiable target boundary exists. Model availability
			// must not turn an internal recall miss into a user-facing policy failure.
			return false;
		}
		if (!selectedWithEmbedding && (deterministicMetricFallback || deterministicExplicitFallback))
		{
			foreach (PolicyTargetPlanIntentAtom atom in AtomSnapshot.Where(atom => explicitAtomIds.Contains(atom.Id)))
			{
				selected[atom.Group] = new ScoredAtom { Atom = atom };
			}
		}
		bool hasValidatedSpecificExclusion = excludedSettlements.Count > 0
			|| excludedClans.Count > 0
			|| excludedKingdoms.Count > 0;
		if (!TryGetUnboundSemanticAtomIds(
			selected.Values.Select(item => item.Atom.Id).ToArray(),
			explicitAtomIds,
			isLocalScope,
			(context.SourceSettlementIds?.Count ?? 0) > 0,
			hasValidatedSpecificExclusion,
			out IReadOnlyList<string> operandPrunedAtomIds,
			out error))
		{
			return false;
		}
		FindUnverifiedHardAtomIds(
			selected.Values.Select(item => item.Atom.Id).ToArray(),
			explicitAtomIds,
			out IReadOnlyList<string> unverifiedHardAtomIds);
		HashSet<string> prunedAtomIds = new HashSet<string>(
			operandPrunedAtomIds.Concat(unverifiedHardAtomIds),
			StringComparer.Ordinal);
		foreach (string prunedAtomId in prunedAtomIds)
		{
			string group = selected
				.Where(pair => string.Equals(pair.Value.Atom.Id, prunedAtomId, StringComparison.Ordinal))
				.Select(pair => pair.Key)
				.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(group))
			{
				selected.Remove(group);
			}
		}
		bool exactKingdomTarget = includedKingdoms.Count > 0
			&& mentionedSettlements.Count == 0
			&& mentionedClans.Count == 0
			&& !fiefCue
			&& !clanFiefCue
			&& !explicitAtomIds.Contains("type_primary_fief")
			&& !explicitAtomIds.Contains("type_town")
			&& !explicitAtomIds.Contains("type_castle")
			&& !explicitAtomIds.Contains("type_clan");
		if (exactKingdomTarget)
		{
			// Exact entity binding already supplies the executable kingdom operand.
			// Embedding may recall the type, but it must not invent unrelated owner,
			// geography, exclusion, ranking or relation constraints around that name.
			foreach (string group in selected.Keys.ToArray())
			{
				if (!HasExplicitAtomInGroup(explicitAtomIds, group)
					&& !string.Equals(group, "type", StringComparison.Ordinal))
				{
					selected.Remove(group);
				}
			}
		}
		if (explicitAtomIds.Contains("exclude_publication")
			&& !explicitAtomIds.Contains("exclude_player_clan")
			&& !explicitAtomIds.Contains("owner_other_clans"))
		{
			// Hard negative: “其他地方” is geographic exclusion of the publication
			// parent, never an implicit exclusion of the player's whole clan or a cue
			// for foreign/ranked/nearest targets. Explicit modifiers still compose.
			selected.Remove("owner");
			foreach (string group in new[] { "relation", "anchor", "type", "geography", "direction", "distance", "metric", "cardinality" })
			{
				if (!HasExplicitAtomInGroup(explicitAtomIds, group))
				{
					selected.Remove(group);
				}
			}
		}
		if (explicitAtomIds.Contains("cardinality_all"))
		{
			// “All” must not be silently narrowed by an embedding-only border,
			// direction, distance or metric guess. Those filters still compose when
			// the policy text contains an explicit cue for their group.
			foreach (string group in new[] { "geography", "direction", "distance", "metric" })
			{
				if (!HasExplicitAtomInGroup(explicitAtomIds, group))
				{
					selected.Remove(group);
				}
			}
		}
		if (HasExplicitAtomInGroup(explicitAtomIds, "metric")
			&& !HasExplicitAtomInGroup(explicitAtomIds, "distance"))
		{
			selected.Remove("distance");
		}
		else if (HasExplicitAtomInGroup(explicitAtomIds, "distance")
			&& !HasExplicitAtomInGroup(explicitAtomIds, "metric"))
		{
			selected.Remove("metric");
		}
		foreach (string prunedAtomId in prunedAtomIds)
		{
			explicitAtomIds.Remove(prunedAtomId);
		}
		bool hasExactEntity = hasExactEntityOperand;
		bool hasTargetBoundary = hasExactEntity || HasExplicitTargetBoundary(explicitAtomIds);
		if (!hasTargetBoundary)
		{
			return false;
		}
		branch = new PolicyTargetPlanBranchSaveData
		{
			Universe = PolicyTargetPlanUniverse.PrimaryFiefs,
			ScopeAnchor = PolicyTargetPlanScopeAnchor.TargetKingdom,
			EntityType = PolicyTargetPlanEntityType.PrimaryFief,
			Relation = PolicyTargetPlanRelation.Any,
			Cardinality = PolicyTargetPlanCardinality.All
		};
		ApplySelectedAtoms(selected, branch);
		if (hasMetricThreshold)
		{
			branch.Metric = thresholdMetric;
			branch.MetricComparison = thresholdComparison;
			branch.MetricThreshold = thresholdValue;
			branch.SortDirection = explicitMetricAscending
				? PolicyTargetPlanSortDirection.Ascending
				: explicitMetricDescending
					? PolicyTargetPlanSortDirection.Descending
					: thresholdComparison == PolicyTargetPlanMetricComparison.GreaterThan
						|| thresholdComparison == PolicyTargetPlanMetricComparison.GreaterThanOrEqual
						? PolicyTargetPlanSortDirection.Descending
						: PolicyTargetPlanSortDirection.Ascending;
		}

		if (includedSettlements.Count > 0)
		{
			branch.Universe = PolicyTargetPlanUniverse.PrimaryFiefs;
			branch.EntityReferences = includedSettlements.Select(entity => entity.EntityId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (includedSettlements.All(entity => entity.IsCity)) branch.EntityType = PolicyTargetPlanEntityType.Town;
			else if (includedSettlements.All(entity => entity.IsCastle)) branch.EntityType = PolicyTargetPlanEntityType.Castle;
			else branch.EntityType = PolicyTargetPlanEntityType.PrimaryFief;
		}
		else if (excludedSettlements.Count > 0 || clanFiefCue || selected.ContainsKey("exclusion") || selected.ContainsKey("specific_exclusion") || selected.ContainsKey("geography")
			|| selected.ContainsKey("direction") || selected.ContainsKey("distance")
			|| IsSettlementMetric(branch.Metric) || fiefCue)
		{
			branch.Universe = PolicyTargetPlanUniverse.PrimaryFiefs;
			if (branch.EntityType == PolicyTargetPlanEntityType.Kingdom || branch.EntityType == PolicyTargetPlanEntityType.Clan)
			{
				branch.EntityType = PolicyTargetPlanEntityType.PrimaryFief;
			}
		}
		else if (mentionedClans.Count > 0 || selected.TryGetValue("type", out ScoredAtom typeAtom)
			&& string.Equals(typeAtom.Atom.Id, "type_clan", StringComparison.Ordinal))
		{
			branch.Universe = PolicyTargetPlanUniverse.Clans;
			branch.EntityType = PolicyTargetPlanEntityType.Clan;
			branch.EntityReferences = includedClans.Select(entity => entity.EntityId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		else
		{
			branch.Universe = PolicyTargetPlanUniverse.Kingdoms;
			branch.EntityType = PolicyTargetPlanEntityType.Kingdom;
			branch.EntityReferences = includedKingdoms.Select(entity => entity.EntityId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		List<string> excludedReferences = excludedSettlements
			.Concat(excludedClans)
			.Concat(excludedKingdoms)
			.Select(entity => entity.EntityId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (excludedReferences.Count > 0)
		{
			branch.Exclusions.Add(PolicyTargetPlanExclusion.SpecificEntities);
			branch.ExcludedEntityReferences = excludedReferences;
		}
		if (branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs
			&& branch.Metric == PolicyTargetPlanMetric.Wealth)
		{
			// 城镇/城堡没有独立财富字段；“富裕城镇”使用实时繁荣度，
			// 不允许把默认 0 值当作可排序指标。
			branch.Metric = PolicyTargetPlanMetric.Prosperity;
		}

		if (clanFiefCue && mentionedClans.Count == 1)
		{
			branch.OwnerClanPredicate = excludedClans.Count == 1
				? PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan
				: PolicyTargetPlanOwnerClanPredicate.SpecificClan;
			branch.ReferenceClanId = mentionedClans[0].EntityId;
		}
		if (selected.TryGetValue("owner", out ScoredAtom ownerAtom)
			&& string.Equals(ownerAtom.Atom.Id, "owner_other_clans", StringComparison.Ordinal))
		{
			if (mentionedClans.Count > 1)
			{
				error = "“其他家族”存在多个可能的参照家族。";
				return false;
			}
			if (mentionedClans.Count == 1)
			{
				branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan;
				branch.ReferenceClanId = mentionedClans[0].EntityId;
			}
			else
			{
				branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan;
			}
		}
		if (branch.Universe == PolicyTargetPlanUniverse.Kingdoms
			&& branch.EntityReferences.Count == 0
			&& branch.Relation == PolicyTargetPlanRelation.Any)
		{
			// Unqualified kingdom/nation wording is anchored to the policy's target
			// kingdom. Only exact entities or an explicit relation may cross it.
			branch.Relation = PolicyTargetPlanRelation.Domestic;
		}
		if (branch.Universe == PolicyTargetPlanUniverse.Clans
			&& branch.EntityReferences.Count == 0
			&& branch.Relation == PolicyTargetPlanRelation.Any
			&& branch.OwnerClanPredicate != PolicyTargetPlanOwnerClanPredicate.ProposerClan)
		{
			// 未点名外国或具体家族的家族集合默认属于目标王国；否则
			// “所有/其他家族”会意外扩展到世界上每个王国。
			branch.Relation = PolicyTargetPlanRelation.Domestic;
		}
		if ((branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs
				|| branch.Universe == PolicyTargetPlanUniverse.Clans)
			&& branch.EntityReferences.Count == 0
			&& includedKingdoms.Count > 0)
		{
			List<string> mentionedKingdomIds = includedKingdoms
				.Select(entity => entity.EntityId)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.Ordinal)
				.ToList();
			if (mentionedKingdomIds.Count == 1 && SameId(mentionedKingdomIds[0], context.TargetKingdomId))
			{
				branch.Relation = PolicyTargetPlanRelation.Domestic;
				branch.NamedKingdomIds.Clear();
			}
			else if (mentionedKingdomIds.Count > 0)
			{
				// Exact entity binding is a stronger and safer boundary than a generic
				// enemy/ally/foreign cue. Multiple names remain exact and are split into
				// separate handles after validation rather than degrading to world scope.
				branch.Relation = PolicyTargetPlanRelation.Specific;
				branch.NamedKingdomIds = mentionedKingdomIds;
			}
		}
		else if (isLocalScope
			&& branch.Relation == PolicyTargetPlanRelation.Any
			&& branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.Any
			&& branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs
			&& branch.EntityReferences.Count == 0
			&& branch.Exclusions.Contains(PolicyTargetPlanExclusion.PublicationParents)
			&& string.IsNullOrWhiteSpace(context.TargetKingdomId)
			&& string.IsNullOrWhiteSpace(context.IssuerKingdomId)
			&& !string.IsNullOrWhiteSpace(context.ProposerClanId))
		{
			// An independent local issuer has no kingdom relation anchor. “Other places”
			// remains positively bounded to that issuer's clan and subtracts only the
			// verified publication parents; it must never widen to the world.
			branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ProposerClan;
		}
		else if (branch.Relation == PolicyTargetPlanRelation.Any
			&& branch.Universe == PolicyTargetPlanUniverse.PrimaryFiefs
			&& includedSettlements.Count == 0)
		{
			branch.Relation = PolicyTargetPlanRelation.Domestic;
		}
		ApplyCardinalityFromText(normalizedQuery, branch);
		if (branch.Metric != PolicyTargetPlanMetric.None || branch.Distance != PolicyTargetPlanDistance.None)
		{
			if (branch.Cardinality == PolicyTargetPlanCardinality.All
				&& !explicitAtomIds.Contains("cardinality_all"))
			{
				branch.Cardinality = PolicyTargetPlanCardinality.TopN;
				branch.Limit = 1;
			}
		}
		if (isLocalScope
			&& branch.Exclusions.Contains(PolicyTargetPlanExclusion.PublicationParents)
			&& (context.SourceSettlementIds?.Count ?? 0) == 0)
		{
			error = "“其他地方”缺少当前发布地锚点。";
			return false;
		}
		evidence = "atoms=" + string.Join(",", selected.Values.Select(item => item.Atom.Id).OrderBy(value => value, StringComparer.Ordinal))
			+ (prunedAtomIds.Count > 0 ? ";pruned=" + string.Join(",", prunedAtomIds.OrderBy(value => value, StringComparer.Ordinal)) : string.Empty)
			+ (hasExactEntity ? ";entities=" + string.Join(",", branch.EntityReferences) : string.Empty);
		return true;
	}

	private static bool TryGetUnboundSemanticAtomIds(
		IReadOnlyCollection<string> selectedAtomIds,
		ISet<string> explicitAtomIds,
		bool isLocalScope,
		bool hasPublicationAnchor,
		bool hasValidatedSpecificExclusion,
		out IReadOnlyList<string> prunedAtomIds,
		out string error)
	{
		error = string.Empty;
		List<string> pruned = new List<string>(2);
		if (selectedAtomIds.Contains("exclude_publication")
			&& (!isLocalScope || !hasPublicationAnchor))
		{
			if (isLocalScope && explicitAtomIds.Contains("exclude_publication"))
			{
				prunedAtomIds = Array.Empty<string>();
				error = "“其他地方”缺少当前发布地锚点。";
				return false;
			}
			pruned.Add("exclude_publication");
		}
		if (selectedAtomIds.Contains("exclude_specific_entity") && !hasValidatedSpecificExclusion)
		{
			// A semantic match cannot invent the concrete entity reference required by
			// SpecificEntities. Only C#-resolved and validated exclusions may survive.
			pruned.Add("exclude_specific_entity");
		}
		prunedAtomIds = pruned
			.Distinct(StringComparer.Ordinal)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToArray();
		return true;
	}

	private static void FindUnverifiedHardAtomIds(
		IReadOnlyCollection<string> selectedAtomIds,
		ISet<string> explicitAtomIds,
		out IReadOnlyList<string> unverifiedAtomIds)
	{
		HashSet<string> explicitIds = new HashSet<string>(
			explicitAtomIds ?? new HashSet<string>(StringComparer.Ordinal),
			StringComparer.Ordinal);
		unverifiedAtomIds = (selectedAtomIds ?? Array.Empty<string>())
			.Where(atomId => AtomSnapshot.Any(atom => string.Equals(atom.Id, atomId, StringComparison.Ordinal))
				&& !explicitIds.Contains(atomId))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(atomId => atomId, StringComparer.Ordinal)
			.ToArray();
	}

	private static bool TrySelectAtoms(
		string query,
		ISet<string> explicitAtomIds,
		out Dictionary<string, ScoredAtom> selected,
		out float topRecall,
		out float topSemanticScore)
	{
		selected = new Dictionary<string, ScoredAtom>(StringComparer.Ordinal);
		topRecall = 0f;
		topSemanticScore = 0f;
		if (!EnsureIndex())
		{
			return false;
		}
		bool embeddedAnyInput = false;
		foreach (string input in BuildQueryInputs(query))
		{
			if (!TryGetQueryEmbedding(input, out float[] vector) || vector == null || vector.Length == 0)
			{
				continue;
			}
			embeddedAnyInput = true;
			List<ScoredAtom> recalled = _index
				.Where(item => item.Vector?.Length == vector.Length)
				.GroupBy(item => item.Atom.Id, StringComparer.Ordinal)
				.Select(group => new ScoredAtom
				{
					Atom = group.First().Atom,
					RecallScore = group.Max(item => Dot(vector, item.Vector))
				})
				.OrderByDescending(item => item.RecallScore)
				.ThenBy(item => item.Atom.Id, StringComparer.Ordinal)
				.Take(RecallPerInputLimit)
				.ToList();
			foreach (string explicitId in explicitAtomIds)
			{
				if (recalled.All(item => !string.Equals(item.Atom.Id, explicitId, StringComparison.Ordinal)))
				{
					PolicyTargetPlanIntentAtom explicitAtom = AtomSnapshot.FirstOrDefault(item => string.Equals(item.Id, explicitId, StringComparison.Ordinal));
					if (explicitAtom != null)
					{
						recalled.Add(new ScoredAtom { Atom = explicitAtom, RecallScore = 0f });
					}
				}
			}
			if (recalled.Count == 0)
			{
				continue;
			}
			foreach (ScoredAtom item in recalled)
			{
				item.SemanticScore = item.RecallScore;
				topRecall = Math.Max(topRecall, item.RecallScore);
				topSemanticScore = Math.Max(topSemanticScore, item.SemanticScore);
			}
			foreach (IGrouping<string, ScoredAtom> group in recalled.GroupBy(item => item.Atom.Group, StringComparer.Ordinal))
			{
				List<ScoredAtom> ordered = group.OrderByDescending(item => item.SemanticScore)
					.ThenBy(item => item.Atom.Id, StringComparer.Ordinal)
					.ToList();
				ScoredAtom explicitWinner = ordered.FirstOrDefault(item => explicitAtomIds.Contains(item.Atom.Id));
				ScoredAtom winner = explicitWinner ?? ordered.FirstOrDefault();
				if (winner == null)
				{
					continue;
				}
				float runnerUp = ordered.Where(item => !ReferenceEquals(item, winner)).Select(item => item.SemanticScore).DefaultIfEmpty(0f).Max();
				if (explicitWinner == null
					&& (winner.SemanticScore < SemanticEmbeddingThreshold
						|| winner.SemanticScore - runnerUp < SemanticEmbeddingMargin))
				{
					continue;
				}
				if (!selected.TryGetValue(group.Key, out ScoredAtom existing)
					|| winner.SemanticScore > existing.SemanticScore)
				{
					selected[group.Key] = winner;
				}
			}
		}
		if (embeddedAnyInput)
		{
			// Exact lexical cues are deterministic C# evidence. Keep them after the bounded
			// embedding recall so a top-8 tie cannot drop an explicitly named operator.
			foreach (PolicyTargetPlanIntentAtom explicitAtom in AtomSnapshot.Where(atom => explicitAtomIds.Contains(atom.Id)))
			{
				selected[explicitAtom.Group] = new ScoredAtom { Atom = explicitAtom };
			}
		}
		return embeddedAnyInput;
	}

	private static void ApplySelectedAtoms(IReadOnlyDictionary<string, ScoredAtom> selected, PolicyTargetPlanBranchSaveData branch)
	{
		foreach (ScoredAtom selectedAtom in selected.Values)
		{
			switch (selectedAtom.Atom.Id)
			{
				case "relation_domestic": branch.Relation = PolicyTargetPlanRelation.Domestic; break;
				case "relation_enemy": branch.Relation = PolicyTargetPlanRelation.Enemy; break;
			case "relation_ally": branch.Relation = PolicyTargetPlanRelation.Ally; break;
			case "relation_foreign": branch.Relation = PolicyTargetPlanRelation.Foreign; break;
			case "anchor_issuer": branch.ScopeAnchor = PolicyTargetPlanScopeAnchor.IssuerKingdom; break;
				case "type_town": branch.EntityType = PolicyTargetPlanEntityType.Town; break;
				case "type_castle": branch.EntityType = PolicyTargetPlanEntityType.Castle; break;
				case "type_primary_fief": branch.EntityType = PolicyTargetPlanEntityType.PrimaryFief; break;
				case "type_clan": branch.EntityType = PolicyTargetPlanEntityType.Clan; break;
				case "type_kingdom": branch.EntityType = PolicyTargetPlanEntityType.Kingdom; break;
				case "exclude_publication": branch.Exclusions.Add(PolicyTargetPlanExclusion.PublicationParents); break;
				case "exclude_specific_entity": branch.Exclusions.Add(PolicyTargetPlanExclusion.SpecificEntities); break;
				case "exclude_player_clan": branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ExcludePlayerClan; break;
				case "owner_other_clans": branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan; break;
				case "owner_proposer_clan": branch.OwnerClanPredicate = PolicyTargetPlanOwnerClanPredicate.ProposerClan; break;
				case "geography_border": branch.BorderOnly = true; break;
				case "direction_north": branch.Direction = PolicyTargetPlanDirection.North; break;
				case "direction_south": branch.Direction = PolicyTargetPlanDirection.South; break;
				case "direction_east": branch.Direction = PolicyTargetPlanDirection.East; break;
				case "direction_west": branch.Direction = PolicyTargetPlanDirection.West; break;
				case "distance_nearest": branch.Distance = PolicyTargetPlanDistance.Nearest; break;
			case "distance_farthest": branch.Distance = PolicyTargetPlanDistance.Farthest; break;
			case "cardinality_all": branch.Cardinality = PolicyTargetPlanCardinality.All; branch.Limit = 0; break;
			case "cardinality_top": branch.Cardinality = PolicyTargetPlanCardinality.TopN; branch.Limit = 1; break;
			case "cardinality_bottom": branch.Cardinality = PolicyTargetPlanCardinality.BottomN; branch.Limit = 1; break;
				case "metric_wealth_high": SetMetric(branch, PolicyTargetPlanMetric.Wealth, true); break;
				case "metric_wealth_low": SetMetric(branch, PolicyTargetPlanMetric.Wealth, false); break;
				case "metric_influence_high": SetMetric(branch, PolicyTargetPlanMetric.Influence, true); break;
				case "metric_influence_low": SetMetric(branch, PolicyTargetPlanMetric.Influence, false); break;
				case "metric_strength_high": SetMetric(branch, PolicyTargetPlanMetric.Strength, true); break;
				case "metric_strength_low": SetMetric(branch, PolicyTargetPlanMetric.Strength, false); break;
				case "metric_fiefs_high": SetMetric(branch, PolicyTargetPlanMetric.FiefCount, true); break;
				case "metric_fiefs_low": SetMetric(branch, PolicyTargetPlanMetric.FiefCount, false); break;
				case "metric_food_high": SetMetric(branch, PolicyTargetPlanMetric.Food, true); break;
				case "metric_food_low": SetMetric(branch, PolicyTargetPlanMetric.Food, false); break;
				case "metric_prosperity_high": SetMetric(branch, PolicyTargetPlanMetric.Prosperity, true); break;
				case "metric_prosperity_low": SetMetric(branch, PolicyTargetPlanMetric.Prosperity, false); break;
				case "metric_loyalty_high": SetMetric(branch, PolicyTargetPlanMetric.Loyalty, true); break;
				case "metric_loyalty_low": SetMetric(branch, PolicyTargetPlanMetric.Loyalty, false); break;
				case "metric_security_high": SetMetric(branch, PolicyTargetPlanMetric.Security, true); break;
				case "metric_security_low": SetMetric(branch, PolicyTargetPlanMetric.Security, false); break;
				case "metric_hearth_high": SetMetric(branch, PolicyTargetPlanMetric.Hearth, true); break;
				case "metric_hearth_low": SetMetric(branch, PolicyTargetPlanMetric.Hearth, false); break;
				case "metric_militia_high": SetMetric(branch, PolicyTargetPlanMetric.Militia, true); break;
				case "metric_militia_low": SetMetric(branch, PolicyTargetPlanMetric.Militia, false); break;
			}
		}
	}

	private static void ApplyCardinalityFromText(string query, PolicyTargetPlanBranchSaveData branch)
	{
		if (ContainsAny(query, "全部", "所有", "每个", "每一", "各地", "各领地", "各城镇", "各城堡", "各村庄", "全境", "全体"))
		{
			branch.Cardinality = PolicyTargetPlanCardinality.All;
			branch.Limit = 0;
			return;
		}
		int count = ParseRequestedCount(query);
		if (count > 0)
		{
			branch.Cardinality = ContainsAny(query, "后", "最低", "最少", "最弱", "最穷", "最差")
				? PolicyTargetPlanCardinality.BottomN
				: PolicyTargetPlanCardinality.TopN;
			branch.Limit = count;
		}
	}

	private static int ParseRequestedCount(string query)
	{
		for (int index = 0; index < query.Length; index++)
		{
			if (!char.IsDigit(query[index]))
			{
				continue;
			}
			int end = index + 1;
			while (end < query.Length && char.IsDigit(query[end])) end++;
			string before = query.Substring(Math.Max(0, index - 6), Math.Min(6, index));
			string after = query.Substring(end, Math.Min(6, query.Length - end));
			bool hasCountContext = HasRequestedCountContext(before, after);
			if (hasCountContext
				&& int.TryParse(query.Substring(index, end - index), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
			{
				return Math.Max(1, value);
			}
		}
		string[] chinese = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
		for (int value = 10; value >= 1; value--)
		{
			if (query.Contains("前" + chinese[value])
				|| query.Contains("后" + chinese[value])
				|| query.Contains(chinese[value] + "座")
				|| query.Contains(chinese[value] + "处")
				|| query.Contains(chinese[value] + "名")
				|| query.Contains(chinese[value] + "家"))
			{
				return value;
			}
		}
		if (ContainsAny(query, "前两", "后两", "两座", "两处", "两名", "两家"))
		{
			return 2;
		}
		return 0;
	}

	private static string BuildUnboundTypeCueText(
		string normalizedQuery,
		IEnumerable<PolicyTargetEntitySnapshot> entities)
	{
		string query = normalizedQuery ?? string.Empty;
		if (query.Length == 0)
		{
			return string.Empty;
		}
		bool[] masked = new bool[query.Length];
		foreach (PolicyTargetEntitySnapshot entity in entities ?? Enumerable.Empty<PolicyTargetEntitySnapshot>())
		{
			if (entity?.MentionAliases == null)
			{
				continue;
			}
			foreach (string rawAlias in entity.MentionAliases)
			{
				string alias = NormalizeText(rawAlias);
				if (alias.Length < 2)
				{
					continue;
				}
				int searchStart = 0;
				while (searchStart < query.Length)
				{
					int index = query.IndexOf(alias, searchStart, StringComparison.Ordinal);
					if (index < 0)
					{
						break;
					}
					int end = ExtendNamedEntityTypeSuffix(query, index + alias.Length, entity.Kind);
					for (int position = index; position < end; position++)
					{
						masked[position] = true;
					}
					searchStart = Math.Max(end, index + alias.Length);
				}
			}
		}
		return new string(query.Where((character, index) => !masked[index]).ToArray());
	}

	private static int ExtendNamedEntityTypeSuffix(string query, int start, string entityKind)
	{
		IReadOnlyList<string> suffixes = string.Equals(entityKind, PolicyTargetEntityKinds.Kingdom, StringComparison.OrdinalIgnoreCase)
			? new[] { "王国", "国家", "政权", "帝国" }
			: string.Equals(entityKind, PolicyTargetEntityKinds.Clan, StringComparison.OrdinalIgnoreCase)
				? new[] { "家族", "氏族", "部族" }
				: string.Equals(entityKind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
					? new[] { "城镇", "城市", "城堡", "村庄", "堡", "城", "村" }
					: Array.Empty<string>();
		foreach (string suffix in suffixes.OrderByDescending(value => value.Length))
		{
			if (start + suffix.Length <= query.Length
				&& string.CompareOrdinal(query, start, suffix, 0, suffix.Length) == 0)
			{
				return start + suffix.Length;
			}
		}
		return start;
	}

	private static HashSet<string> FindExplicitAtomIds(string query, string typeCueQuery)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
		string typeQuery = string.IsNullOrWhiteSpace(typeCueQuery) ? query : typeCueQuery;
		if (ContainsAny(query, "其他地方", "别的地方", "其余地方", "剩余地方", "除发布地", "发布地以外")) result.Add("exclude_publication");
		if (ContainsAny(query, "除玩家家族", "排除玩家家族", "玩家家族以外", "非玩家家族")) result.Add("exclude_player_clan");
		if (ContainsAny(query, "其他家族", "别的家族", "其余家族", "其他氏族")) result.Add("owner_other_clans");
		if (ContainsAny(query, "发布者家族", "政策发布者家族", "提案方家族", "提案者家族", "发布者氏族", "提案方氏族", "发布者影响力", "发布者的影响力", "提案者影响力", "提案者的影响力"))
		{
			result.Add("owner_proposer_clan");
			result.Add("type_clan");
		}
		if (ContainsAny(query, "本国", "国内", "境内")) result.Add("relation_domestic");
		if (ContainsAny(query, "敌国", "交战国", "敌对国家")) result.Add("relation_enemy");
		if (ContainsAny(query, "盟国", "同盟国", "盟友国家")) result.Add("relation_ally");
		if (ContainsAny(query, "外国", "国外", "其他国家", "其他王国", "别国", "别的王国", "其余王国")) result.Add("relation_foreign");
		if (ContainsAny(query, "发布者王国", "提案方王国", "发布方王国", "宗主国为参照")) result.Add("anchor_issuer");
		if (ContainsAny(typeQuery, "城镇", "城市")) result.Add("type_town");
		if (typeQuery.Contains("城堡")) result.Add("type_castle");
		if (ContainsAny(typeQuery, "封地", "领地", "定居点")) result.Add("type_primary_fief");
		if (ContainsAny(typeQuery, "家族", "氏族", "领主", "贵族")) result.Add("type_clan");
		if (ContainsAny(typeQuery, "王国", "国家", "政权")) result.Add("type_kingdom");
		if (ContainsAny(query, "边境", "边疆", "前线")) result.Add("geography_border");
		if (ContainsAny(query, "北方", "北部", "以北")) result.Add("direction_north");
		if (ContainsAny(query, "南方", "南部", "以南")) result.Add("direction_south");
		if (ContainsAny(query, "东方", "东部", "以东")) result.Add("direction_east");
		if (ContainsAny(query, "西方", "西部", "以西")) result.Add("direction_west");
		if (ContainsAny(query, "最近", "就近", "邻近")) result.Add("distance_nearest");
		if (ContainsAny(query, "最远", "偏远")) result.Add("distance_farthest");
		bool all = ContainsAny(query, "全部", "所有", "每个", "每一", "各地", "各领地", "各城镇", "各城堡", "各村庄", "全境", "全体");
		if (all)
		{
			result.Add("cardinality_all");
		}
		else
		{
			int requestedCount = ParseRequestedCount(query);
			if (requestedCount > 0 || ContainsAny(query, "最高", "最多", "最强", "最富", "最好"))
			{
				result.Add("cardinality_top");
			}
			if (ContainsAny(query, "最低", "最少", "最弱", "最穷", "最差"))
			{
				result.Remove("cardinality_top");
				result.Add("cardinality_bottom");
			}
		}
		AddMetricExplicitCues(result, query);
		return result;
	}

	private static void NormalizeExplicitTypeCues(ISet<string> atomIds, bool clanFiefCue, string typeCueQuery)
	{
		if (clanFiefCue)
		{
			atomIds.Remove("type_clan");
			atomIds.Add("type_primary_fief");
		}
		if (atomIds.Contains("type_town") && atomIds.Contains("type_castle"))
		{
			atomIds.Remove("type_town");
			atomIds.Remove("type_castle");
			atomIds.Add("type_primary_fief");
		}
		if (atomIds.Contains("type_clan")
			&& atomIds.Contains("type_kingdom")
			&& (HasExplicitRelationCue(atomIds)
				|| HasHierarchicalContainmentCue(
					typeCueQuery,
					new[] { "王国", "国家", "政权" },
					new[] { "家族", "氏族", "领主", "贵族" })))
		{
			// The higher-level kingdom phrase constrains clan membership.
			atomIds.Remove("type_kingdom");
		}
		bool hasSettlementType = atomIds.Contains("type_primary_fief")
			|| atomIds.Contains("type_town")
			|| atomIds.Contains("type_castle");
		if (hasSettlementType
			&& atomIds.Contains("type_kingdom")
			&& (HasExplicitRelationCue(atomIds)
				|| HasHierarchicalContainmentCue(
					typeCueQuery,
					new[] { "王国", "国家", "政权" },
					new[] { "城镇", "城市", "城堡", "封地", "领地", "定居点" })))
		{
			// A kingdom is a scope container only when relation or containment syntax
			// binds a lower-level settlement target. Independent kingdom and settlement
			// targets remain a real multi-target ambiguity instead of being erased.
			atomIds.Remove("type_kingdom");
		}
		if (hasSettlementType
			&& atomIds.Contains("type_clan")
			&& HasHierarchicalContainmentCue(
				typeCueQuery,
				new[] { "家族", "氏族", "领主", "贵族" },
				new[] { "城镇", "城市", "城堡", "封地", "领地", "定居点" }))
		{
			atomIds.Remove("type_clan");
		}
		if (hasSettlementType && (atomIds.Contains("exclude_player_clan")
			|| atomIds.Contains("owner_other_clans")
			|| atomIds.Contains("owner_proposer_clan")))
		{
			// Clan is the owner predicate in these phrases, while towns/castles remain
			// the executable policy objects.
			atomIds.Remove("type_clan");
		}
		if (atomIds.Contains("exclude_player_clan"))
		{
			atomIds.Remove("owner_other_clans");
		}
	}

	private static bool HasExplicitRelationCue(ISet<string> atomIds)
	{
		return atomIds.Contains("relation_domestic")
			|| atomIds.Contains("relation_enemy")
			|| atomIds.Contains("relation_ally")
			|| atomIds.Contains("relation_foreign");
	}

	private static bool HasHierarchicalContainmentCue(
		string query,
		IReadOnlyCollection<string> containerTerms,
		IReadOnlyCollection<string> memberTerms)
	{
		string normalized = query ?? string.Empty;
		foreach (string container in containerTerms ?? Array.Empty<string>())
		{
			int searchStart = 0;
			while (searchStart < normalized.Length)
			{
				int containerIndex = normalized.IndexOf(container, searchStart, StringComparison.Ordinal);
				if (containerIndex < 0)
				{
					break;
				}
				int containerEnd = containerIndex + container.Length;
				foreach (string member in memberTerms ?? Array.Empty<string>())
				{
					int memberIndex = normalized.IndexOf(member, containerEnd, StringComparison.Ordinal);
					if (memberIndex < 0 || memberIndex - containerEnd > 12)
					{
						continue;
					}
					string bridge = normalized.Substring(containerEnd, memberIndex - containerEnd);
					if (bridge.Length == 0
						|| ContainsAny(bridge, "的", "所属", "辖下", "辖内", "境内", "全境", "范围内", "拥有", "控制", "各", "全部", "所有"))
					{
						return true;
					}
				}
				searchStart = containerEnd;
			}
		}
		return false;
	}

	private static bool TryValidateExplicitAtomGroups(ISet<string> atomIds, out string error)
	{
		error = string.Empty;
		foreach (IGrouping<string, PolicyTargetPlanIntentAtom> group in AtomSnapshot
			.Where(atom => atomIds.Contains(atom.Id))
			.GroupBy(atom => atom.Group, StringComparer.Ordinal))
		{
			if (group.Count() > 1)
			{
				error = "TargetPlan 子句包含互相矛盾的 " + group.Key + " 操作符。";
				return false;
			}
		}
		return true;
	}

	private static bool IsTypeAtom(string atomId)
	{
		return AtomSnapshot.Any(atom => string.Equals(atom.Id, atomId, StringComparison.Ordinal)
			&& string.Equals(atom.Group, "type", StringComparison.Ordinal));
	}

	private static bool HasExplicitAtomInGroup(ISet<string> atomIds, string group)
	{
		return AtomSnapshot.Any(atom => atomIds.Contains(atom.Id)
			&& string.Equals(atom.Group, group, StringComparison.Ordinal));
	}

	private static bool HasExplicitTargetBoundary(ISet<string> atomIds)
	{
		if (atomIds == null || atomIds.Count == 0)
		{
			return false;
		}
		return AtomSnapshot.Any(atom => atomIds.Contains(atom.Id)
			&& !string.Equals(atom.Group, "cardinality", StringComparison.Ordinal)
			&& !string.Equals(atom.Group, "anchor", StringComparison.Ordinal));
	}

	private static void AddMetricExplicitCues(ISet<string> result, string query)
	{
		AddMetricCue(result, query, "wealth", new[] { "最富", "财富最高", "最有钱" }, new[] { "最穷", "财富最低", "最贫穷" });
		AddMetricCue(result, query, "influence", new[] { "影响力最高", "最有权势" }, new[] { "影响力最低", "最无权势" });
		AddMetricCue(result, query, "strength", new[] { "实力最强", "最强大", "军力最强" }, new[] { "实力最弱", "最弱小", "军力最弱" });
		AddMetricCue(result, query, "fiefs", new[] { "封地最多", "领地最多" }, new[] { "封地最少", "领地最少" });
		AddMetricCue(result, query, "food", new[] { "粮食最多", "储粮最高", "粮食充足" }, new[] { "粮食最少", "储粮最低", "缺粮" });
		AddMetricCue(result, query, "prosperity", new[] { "最繁荣", "繁荣度最高" }, new[] { "最贫困", "繁荣度最低", "最落后" });
		AddMetricCue(result, query, "loyalty", new[] { "忠诚最高", "最忠诚" }, new[] { "忠诚最低", "最不忠" });
		AddMetricCue(result, query, "security", new[] { "治安最好", "安全度最高" }, new[] { "治安最差", "安全度最低", "最危险" });
		AddMetricCue(result, query, "hearth",
			new[] { "户数最高", "炉户最多", "hearth最高", "户数降序", "hearth降序" },
			new[] { "户数最低", "炉户最少", "hearth最低", "户数升序", "hearth升序" });
		AddMetricCue(result, query, "militia",
			new[] { "民兵最高", "民兵最多", "militia最高", "民兵降序", "militia降序" },
			new[] { "民兵最低", "民兵最少", "militia最低", "民兵升序", "militia升序" });
	}

	private static void AddMetricCue(ISet<string> result, string query, string metric, IEnumerable<string> high, IEnumerable<string> low)
	{
		if (high.Any(query.Contains)) result.Add("metric_" + metric + "_high");
		if (low.Any(query.Contains)) result.Add("metric_" + metric + "_low");
	}

	private static bool TryParseMetricThreshold(
		string query,
		out bool found,
		out PolicyTargetPlanMetric metric,
		out PolicyTargetPlanMetricComparison comparison,
		out float threshold,
		out bool ambiguous)
	{
		found = false;
		metric = PolicyTargetPlanMetric.None;
		comparison = PolicyTargetPlanMetricComparison.None;
		threshold = 0f;
		ambiguous = false;
		foreach (KeyValuePair<PolicyTargetPlanMetric, string> alias in MetricThresholdAliases)
		{
			int searchStart = 0;
			while (searchStart < query.Length)
			{
				int metricIndex = query.IndexOf(alias.Value, searchStart, StringComparison.Ordinal);
				if (metricIndex < 0)
				{
					break;
				}
				int metricEnd = metricIndex + alias.Value.Length;
				if (TryFindMetricComparison(query, metricEnd, out int comparisonEnd, out PolicyTargetPlanMetricComparison parsedComparison)
					&& TryReadMetricThresholdNumber(query, comparisonEnd, out float parsedThreshold))
				{
					if (found && (metric != alias.Key || comparison != parsedComparison || threshold != parsedThreshold))
					{
						ambiguous = true;
						return false;
					}
					found = true;
					metric = alias.Key;
					comparison = parsedComparison;
					threshold = parsedThreshold;
				}
				searchStart = metricEnd;
			}
		}
		return found;
	}

	private static bool TryFindMetricComparison(
		string query,
		int start,
		out int comparisonEnd,
		out PolicyTargetPlanMetricComparison comparison)
	{
		comparisonEnd = 0;
		comparison = PolicyTargetPlanMetricComparison.None;
		int maximumIndex = Math.Min(query.Length, start + 12);
		int bestIndex = int.MaxValue;
		int bestLength = -1;
		foreach (KeyValuePair<string, PolicyTargetPlanMetricComparison> cue in MetricComparisonCues)
		{
			int index = query.IndexOf(cue.Key, start, StringComparison.Ordinal);
			if (index < start || index > maximumIndex)
			{
				continue;
			}
			if (index < bestIndex || (index == bestIndex && cue.Key.Length > bestLength))
			{
				bestIndex = index;
				bestLength = cue.Key.Length;
				comparisonEnd = index + cue.Key.Length;
				comparison = cue.Value;
			}
		}
		return comparison != PolicyTargetPlanMetricComparison.None;
	}

	private static bool TryReadMetricThresholdNumber(string query, int start, out float value)
	{
		value = 0f;
		int index = start;
		while (index < query.Length
			&& (query[index] == '=' || query[index] == ':' || query[index] == '：'
				|| query[index] == '为' || query[index] == '是'))
		{
			index++;
		}
		int numberStart = index;
		if (index < query.Length && (query[index] == '+' || query[index] == '-'))
		{
			index++;
		}
		bool hasDigit = false;
		bool hasDecimalPoint = false;
		while (index < query.Length)
		{
			if (char.IsDigit(query[index]))
			{
				hasDigit = true;
				index++;
				continue;
			}
			if (query[index] == '.' && !hasDecimalPoint)
			{
				hasDecimalPoint = true;
				index++;
				continue;
			}
			break;
		}
		return hasDigit
			&& float.TryParse(query.Substring(numberStart, index - numberStart), NumberStyles.Float,
				CultureInfo.InvariantCulture, out value)
			&& !float.IsNaN(value)
			&& !float.IsInfinity(value);
	}

	private static string BuildMetricAtomId(PolicyTargetPlanMetric metric, bool high)
	{
		string id = metric == PolicyTargetPlanMetric.Hearth ? "hearth"
			: metric == PolicyTargetPlanMetric.Militia ? "militia"
			: string.Empty;
		return id.Length == 0 ? string.Empty : "metric_" + id + (high ? "_high" : "_low");
	}

	private static List<PolicyTargetEntitySnapshot> FindExactMentionedEntities(
		IEnumerable<PolicyTargetEntitySnapshot> entities,
		string normalizedQuery,
		string kind,
		bool strictEntityEvidence = false)
	{
		if (strictEntityEvidence)
		{
			return PolicyTargetObjectiveEvidence.FindStrictMentionedEntities(
				entities,
				normalizedQuery,
				kind).ToList();
		}
		return (entities ?? Enumerable.Empty<PolicyTargetEntitySnapshot>())
			.Where(entity => entity != null
				&& string.Equals(entity.Kind, kind, StringComparison.OrdinalIgnoreCase)
				&& entity.MentionAliases != null
				&& entity.MentionAliases.Any(alias =>
				{
					string normalizedAlias = NormalizeText(alias);
					return normalizedAlias.Length >= 2 && normalizedQuery.Contains(normalizedAlias);
				}))
			.GroupBy(entity => entity.EntityId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.OrderBy(entity => entity.EntityId, StringComparer.Ordinal)
			.ToList();
	}

	private static bool IsExplicitlyExcludedEntity(string normalizedQuery, PolicyTargetEntitySnapshot entity)
	{
		if (entity?.MentionAliases == null || string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return false;
		}
		foreach (string alias in entity.MentionAliases)
		{
			string normalizedAlias = NormalizeText(alias);
			if (normalizedAlias.Length < 2)
			{
				continue;
			}
			if (ContainsAny(
				normalizedQuery,
				"排除" + normalizedAlias,
				"不包括" + normalizedAlias,
				"不含" + normalizedAlias,
				"除去" + normalizedAlias,
				"除了" + normalizedAlias,
				"除" + normalizedAlias,
				normalizedAlias + "以外",
				normalizedAlias + "之外"))
			{
				return true;
			}
		}
		return false;
	}

	private static bool EnsureIndex()
	{
		if (_indexAttempted)
		{
			return _index.Count > 0;
		}
		lock (IndexSync)
		{
			if (_indexAttempted)
			{
				return _index.Count > 0;
			}
			_indexAttempted = true;
			List<IndexedIntentSeed> built = new List<IndexedIntentSeed>();
			foreach (PolicyTargetPlanIntentAtom atom in AtomSnapshot)
			{
				foreach (string seed in atom.Seeds)
				{
					if (!OnnxEmbeddingEngine.Instance.TryGetEmbedding(seed, out float[] vector) || vector == null || vector.Length == 0)
					{
						_index = Array.Empty<IndexedIntentSeed>();
						return false;
					}
					built.Add(new IndexedIntentSeed { Atom = atom, Seed = seed, Vector = vector });
				}
			}
			_index = built;
			return _index.Count > 0;
		}
	}

	private static bool TryGetQueryEmbedding(string input, out float[] vector)
	{
		string key = (input ?? string.Empty).Trim();
		lock (QueryEmbeddingSync)
		{
			if (QueryEmbeddingCache.TryGetValue(key, out vector))
			{
				return true;
			}
		}
		if (!OnnxEmbeddingEngine.Instance.TryGetEmbedding(key, out vector)
			|| vector == null
			|| vector.Length == 0)
		{
			vector = null;
			return false;
		}
		lock (QueryEmbeddingSync)
		{
			if (QueryEmbeddingCache.Count >= QueryEmbeddingCacheLimit)
			{
				QueryEmbeddingCache.Clear();
			}
			QueryEmbeddingCache[key] = vector;
		}
		return true;
	}

	private static IReadOnlyList<PolicyTargetPlanIntentAtom> BuildAtoms()
	{
		List<PolicyTargetPlanIntentAtom> result = new List<PolicyTargetPlanIntentAtom>
		{
			Atom("relation_domestic", "relation", "目标属于当前政策王国境内的本国对象。", "本国全部领地", "国内各地", "境内城镇城堡"),
			Atom("relation_enemy", "relation", "目标属于当前与锚点王国交战的敌国。", "敌国领地", "交战国城镇", "敌对国家"),
			Atom("relation_ally", "relation", "目标属于当前与锚点王国结盟的盟国。", "盟国领地", "同盟国家城镇", "盟友王国"),
			Atom("relation_foreign", "relation", "目标属于锚点王国以外的外国，不要求敌对或结盟。", "外国领地", "其他国家城镇", "其他王国的定居点", "国外对象"),
			Atom("anchor_issuer", "anchor", "关系、方向和距离以政策发布者或提案方所属王国为锚点。", "以发布者王国为准", "相对提案方王国", "以宗主国为参照"),
			Atom("type_primary_fief", "type", "一级政策地理对象是城市或城堡，不包含独立村庄。", "城镇和城堡", "全部封地", "主要领地"),
			Atom("type_town", "type", "一级政策地理对象只包含城市或城镇。", "所有城市", "指定城镇", "商业城市"),
			Atom("type_castle", "type", "一级政策地理对象只包含城堡或要塞。", "所有城堡", "边境要塞", "指定堡垒"),
			Atom("type_clan", "type", "政策机械对象是家族或氏族。", "其他家族", "指定氏族", "贵族家族"),
			Atom("type_kingdom", "type", "政策机械对象是整个国家、王国或政权。", "全部敌国", "指定王国", "盟友国家"),
			Atom("exclude_publication", "exclusion", "从目标集合中排除当前政策发布地对应的父级城镇或城堡，不排除玩家整个家族。", "其他地方", "除当前发布地外", "发布地以外的其余地区", "别的地方"),
			Atom("exclude_specific_entity", "specific_exclusion", "从目标集合中排除句中明确命名并由 C# 实体目录验证的王国、家族、城镇或城堡。", "排除指定目标", "不包括点名对象", "除明确对象之外"),
			Atom("exclude_player_clan", "owner", "从本国一级封地中排除玩家家族拥有的全部城市和城堡。", "除玩家家族外", "排除玩家家族封地", "非玩家家族领地"),
			Atom("owner_other_clans", "owner", "目标是参照家族以外的其他家族或其他家族封地。", "其他家族封地", "别的家族领地", "其余氏族"),
			Atom("owner_proposer_clan", "owner", "目标只包含政策发布者或提案方自己的家族。", "发布者家族", "提案方家族", "发布者氏族"),
			Atom("geography_border", "geography", "目标必须是当前边境或前线的城市和城堡。", "边境城镇", "前线城堡", "边疆领地"),
			Atom("direction_north", "direction", "目标位于发布地或王国中心以北。", "北方领地", "北部城市", "以北城堡"),
			Atom("direction_south", "direction", "目标位于发布地或王国中心以南。", "南方领地", "南部城市", "以南城堡"),
			Atom("direction_east", "direction", "目标位于发布地或王国中心以东。", "东方领地", "东部城市", "以东城堡"),
			Atom("direction_west", "direction", "目标位于发布地或王国中心以西。", "西方领地", "西部城市", "以西城堡"),
			Atom("distance_nearest", "distance", "按地图距离选择最近或邻近目标。", "最近的城镇", "就近领地", "邻近城堡"),
			Atom("distance_farthest", "distance", "按地图距离选择最远或偏远目标。", "最远的城镇", "偏远领地", "距离最远的城堡"),
			Atom("cardinality_all", "cardinality", "选择满足条件的全部目标。", "全部目标", "所有符合条件者", "每一个对象"),
			Atom("cardinality_top", "cardinality", "从既定排序中选择前 N 个目标。", "前几个目标", "排名靠前对象", "取前 N 个"),
			Atom("cardinality_bottom", "cardinality", "从既定排序中选择后 N 个或最低的目标。", "后几个目标", "排名靠后对象", "取后 N 个")
		};
		result.AddRange(MetricAtoms("wealth", "财富", "最富有", "最贫穷"));
		result.AddRange(MetricAtoms("influence", "影响力", "影响力最高", "影响力最低"));
		result.AddRange(MetricAtoms("strength", "实力", "实力最强", "实力最弱"));
		result.AddRange(MetricAtoms("fiefs", "封地数量", "封地最多", "封地最少"));
		result.AddRange(MetricAtoms("food", "粮食储备", "粮食最多", "粮食最少"));
		result.AddRange(MetricAtoms("prosperity", "繁荣度", "最繁荣", "最贫困"));
		result.AddRange(MetricAtoms("loyalty", "忠诚度", "忠诚最高", "忠诚最低"));
		result.AddRange(MetricAtoms("security", "治安", "治安最好", "治安最差"));
		result.AddRange(MetricAtoms("hearth", "附属村庄平均户数", "户数最高", "户数最低"));
		result.AddRange(MetricAtoms("militia", "民兵", "民兵最高", "民兵最低"));
		return result;
	}

	private static PolicyTargetPlanIntentAtom Atom(string id, string group, string document, params string[] seeds)
	{
		return new PolicyTargetPlanIntentAtom
		{
			Id = id,
			Group = group,
			Document = document,
			Seeds = (seeds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray()
		};
	}

	private static List<string> BuildQueryInputs(string query)
	{
		List<string> result = new List<string> { Limit(query.Trim(), 800) };
		foreach (string part in query.Split(new[] { '\n', '。', '；', ';', '，', ',', '！', '!', '？', '?' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string normalized = Limit(part.Trim(), 320);
			if (normalized.Length >= 2 && !result.Contains(normalized, StringComparer.Ordinal))
			{
				result.Add(normalized);
			}
			if (result.Count >= QueryInputLimit) break;
		}
		return result;
	}

	private static bool TrySplitSingleUnionClauses(string query, out string leftClause, out string rightClause)
	{
		leftClause = string.Empty;
		rightClause = string.Empty;
		if (string.IsNullOrWhiteSpace(query))
		{
			return false;
		}
		int connectorIndex = -1;
		int connectorLength = 0;
		for (int index = 0; index < query.Length; index++)
		{
			if (query[index] != '或')
			{
				continue;
			}
			int currentLength = index + 1 < query.Length
				&& (query[index + 1] == '者' || query[index + 1] == '是')
				? 2
				: 1;
			if (connectorIndex >= 0)
			{
				return false;
			}
			connectorIndex = index;
			connectorLength = currentLength;
			index += currentLength - 1;
		}
		if (connectorIndex < 0)
		{
			return false;
		}
		leftClause = query.Substring(0, connectorIndex).Trim();
		rightClause = query.Substring(connectorIndex + connectorLength).Trim();
		return leftClause.Length > 0 && rightClause.Length > 0;
	}

	private static void SetMetric(PolicyTargetPlanBranchSaveData branch, PolicyTargetPlanMetric metric, bool high)
	{
		branch.Metric = metric;
		branch.SortDirection = high ? PolicyTargetPlanSortDirection.Descending : PolicyTargetPlanSortDirection.Ascending;
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

	private static string BuildDisplayName(PolicyTargetPlanSaveData plan)
	{
		return string.Join(" 或 ", plan.Branches.Select(branch =>
		{
			if (branch.Universe == PolicyTargetPlanUniverse.Kingdoms
				&& branch.Relation == PolicyTargetPlanRelation.Domestic
				&& branch.EntityReferences.Count == 0
				&& branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.Any
				&& branch.Exclusions.Count == 0
				&& !branch.BorderOnly
				&& branch.Direction == PolicyTargetPlanDirection.Any
				&& branch.Distance == PolicyTargetPlanDistance.None
				&& branch.Metric == PolicyTargetPlanMetric.None
				&& branch.Cardinality == PolicyTargetPlanCardinality.All)
			{
				return "当前目标王国";
			}
			string relation = branch.Relation == PolicyTargetPlanRelation.Domestic ? "本国"
				: branch.Relation == PolicyTargetPlanRelation.Enemy ? "敌国"
				: branch.Relation == PolicyTargetPlanRelation.Ally ? "盟国"
				: branch.Relation == PolicyTargetPlanRelation.Foreign ? "外国"
				: branch.Relation == PolicyTargetPlanRelation.Specific ? "指定王国" : string.Empty;
			string type = branch.EntityType == PolicyTargetPlanEntityType.Town ? "城镇"
				: branch.EntityType == PolicyTargetPlanEntityType.Castle ? "城堡"
				: branch.EntityType == PolicyTargetPlanEntityType.Clan ? "家族"
				: branch.EntityType == PolicyTargetPlanEntityType.Kingdom ? "王国" : "城镇/城堡";
			List<string> qualifiers = new List<string>();
			if (branch.Exclusions.Contains(PolicyTargetPlanExclusion.PublicationParents)) qualifiers.Add("不含发布地");
			if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludePlayerClan) qualifiers.Add("不含玩家家族");
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan) qualifiers.Add("其他家族");
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ProposerClan) qualifiers.Add("发布者家族");
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.SpecificClan) qualifiers.Add("指定家族");
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan) qualifiers.Add("排除指定家族");
			if (branch.BorderOnly) qualifiers.Add("边境");
			if (branch.Direction != PolicyTargetPlanDirection.Any) qualifiers.Add(DirectionText(branch.Direction));
			if (branch.Distance != PolicyTargetPlanDistance.None) qualifiers.Add(branch.Distance == PolicyTargetPlanDistance.Nearest ? "最近" : "最远");
			if (branch.MetricComparison != PolicyTargetPlanMetricComparison.None && branch.MetricThreshold.HasValue)
			{
				qualifiers.Add(MetricText(branch.Metric) + MetricComparisonText(branch.MetricComparison)
					+ branch.MetricThreshold.Value.ToString("R", CultureInfo.InvariantCulture));
			}
			else if (branch.Metric != PolicyTargetPlanMetric.None)
			{
				qualifiers.Add(MetricText(branch.Metric)
					+ (branch.SortDirection == PolicyTargetPlanSortDirection.Ascending ? "低" : "高"));
			}
			string exclusion = qualifiers.Count > 0 ? "（" + string.Join("、", qualifiers) + "）" : string.Empty;
			string count = branch.Cardinality == PolicyTargetPlanCardinality.All ? "全部"
				: branch.Cardinality == PolicyTargetPlanCardinality.TopN ? "前" + branch.Limit.ToString(CultureInfo.InvariantCulture) + "个"
				: "后" + branch.Limit.ToString(CultureInfo.InvariantCulture) + "个";
			return relation + count + type + exclusion;
		}));
	}

	private static string DirectionText(PolicyTargetPlanDirection direction)
	{
		switch (direction)
		{
			case PolicyTargetPlanDirection.North: return "北方";
			case PolicyTargetPlanDirection.South: return "南方";
			case PolicyTargetPlanDirection.East: return "东方";
			case PolicyTargetPlanDirection.West: return "西方";
			default: return string.Empty;
		}
	}

	private static string MetricText(PolicyTargetPlanMetric metric)
	{
		switch (metric)
		{
			case PolicyTargetPlanMetric.Wealth: return "财富";
			case PolicyTargetPlanMetric.Influence: return "影响力";
			case PolicyTargetPlanMetric.Strength: return "实力";
			case PolicyTargetPlanMetric.FiefCount: return "封地数";
			case PolicyTargetPlanMetric.Food: return "粮食";
			case PolicyTargetPlanMetric.Prosperity: return "繁荣";
			case PolicyTargetPlanMetric.Loyalty: return "忠诚";
			case PolicyTargetPlanMetric.Security: return "治安";
			case PolicyTargetPlanMetric.Hearth: return "附属村庄平均户数";
			case PolicyTargetPlanMetric.Militia: return "民兵";
			default: return string.Empty;
		}
	}

	private static string MetricComparisonText(PolicyTargetPlanMetricComparison comparison)
	{
		switch (comparison)
		{
			case PolicyTargetPlanMetricComparison.LessThan: return "<";
			case PolicyTargetPlanMetricComparison.LessThanOrEqual: return "<=";
			case PolicyTargetPlanMetricComparison.GreaterThan: return ">";
			case PolicyTargetPlanMetricComparison.GreaterThanOrEqual: return ">=";
			default: return string.Empty;
		}
	}

	private static PolicyTargetPlanIntentAtom[] MetricAtoms(string id, string noun, string high, string low)
	{
		return new[]
		{
			Atom("metric_" + id + "_high", "metric", "按当前" + noun + "从高到低选择目标。",
				high, noun + "最高", noun + "最多", noun + "高的目标"),
			Atom("metric_" + id + "_low", "metric", "按当前" + noun + "从低到高选择目标。",
				low, noun + "最低", noun + "最少", noun + "低的目标")
		};
	}

	private static bool ContainsAny(string source, params string[] values)
	{
		return (values ?? Array.Empty<string>()).Any(value => source.Contains(value));
	}

	private static bool HasRequestedCountContext(string before, string after)
	{
		return before.EndsWith("前", StringComparison.Ordinal)
			|| before.EndsWith("后", StringComparison.Ordinal)
			|| before.EndsWith("最多", StringComparison.Ordinal)
			|| before.EndsWith("至多", StringComparison.Ordinal)
			|| before.EndsWith("取前", StringComparison.Ordinal)
			|| before.EndsWith("取后", StringComparison.Ordinal)
			|| before.EndsWith("top", StringComparison.Ordinal)
			|| before.EndsWith("bottom", StringComparison.Ordinal)
			|| after.StartsWith("个", StringComparison.Ordinal)
			|| after.StartsWith("座", StringComparison.Ordinal)
			|| after.StartsWith("处", StringComparison.Ordinal)
			|| after.StartsWith("名", StringComparison.Ordinal)
			|| after.StartsWith("家", StringComparison.Ordinal);
	}

	private static string NormalizeText(string value)
	{
		return new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(character => !char.IsWhiteSpace(character)).ToArray());
	}

	private static bool SameId(string left, string right)
	{
		return !string.IsNullOrWhiteSpace(left)
			&& !string.IsNullOrWhiteSpace(right)
			&& string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static float Dot(IReadOnlyList<float> left, IReadOnlyList<float> right)
	{
		float score = 0f;
		for (int index = 0; index < left.Count; index++) score += left[index] * right[index];
		return score;
	}

	private static string Limit(string value, int maximum)
	{
		string normalized = value ?? string.Empty;
		return normalized.Length <= maximum ? normalized : normalized.Substring(0, maximum);
	}

	private sealed class IndexedIntentSeed
	{
		internal PolicyTargetPlanIntentAtom Atom { get; set; }

		internal string Seed { get; set; }

		internal float[] Vector { get; set; }
	}

	private sealed class ScoredAtom
	{
		internal PolicyTargetPlanIntentAtom Atom { get; set; }

		internal float RecallScore { get; set; }

		internal float SemanticScore { get; set; }
	}
}
