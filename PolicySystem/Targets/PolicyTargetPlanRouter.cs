using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

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
	private static readonly char PolicyLegacyCharacter0 = PolicyCompatibilityTexts.Get("Closeout_PolicyTargetPlanRouter_Legacy_Closeout_0")[0];
	private static readonly char PolicyLegacyCharacter1 = PolicyCompatibilityTexts.Get("Closeout_PolicyTargetPlanRouter_Legacy_Closeout_1")[0];
	private static readonly char PolicyLegacyCharacter2 = PolicyCompatibilityTexts.Get("Closeout_PolicyTargetPlanRouter_Legacy_Closeout_2")[0];
	private static readonly char PolicyLegacyCharacter3 = PolicyCompatibilityTexts.Get("Closeout_PolicyTargetPlanRouter_Legacy_Closeout_3")[0];

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

	// Fixed bilingual input aliases are built once, independent of the UI/content package language.
	private static readonly IReadOnlyDictionary<string, string[]> EnglishIntentSeeds =
		new Dictionary<string, string[]>(StringComparer.Ordinal)
		{
			{ "relation_domestic", new[] { "our kingdom", "domestic settlements", "settlements in our kingdom" } },
			{ "relation_enemy", new[] { "enemy kingdom", "enemy settlements", "settlements belonging to enemies" } },
			{ "relation_ally", new[] { "allied kingdom", "allied settlements", "settlements belonging to allies" } },
			{ "relation_foreign", new[] { "foreign kingdoms", "other kingdoms", "settlements outside our kingdom" } },
			{ "anchor_issuer", new[] { "issuing kingdom", "the kingdom issuing this policy", "our own kingdom" } },
			{ "type_primary_fief", new[] { "towns and castles", "towns or castles", "major settlements" } },
			{ "type_town", new[] { "towns", "cities", "town settlements", "town", "city" } },
			{ "type_castle", new[] { "castles", "castle settlements", "fortresses", "castle", "fortress" } },
			{ "type_clan", new[] { "clans", "families", "noble clans", "clan" } },
			{ "type_kingdom", new[] { "kingdoms", "nations", "kingdom entities", "kingdom" } },
			{ "exclude_publication", new[] { "exclude the publication settlement", "except the issuing settlement", "all other locations" } },
			{ "exclude_specific_entity", new[] { "exclude a specified settlement", "except the named settlement", "exclude specified entities" } },
			{ "exclude_player_clan", new[] { "exclude the player clan", "except my clan", "not the player clan" } },
			{ "owner_other_clans", new[] { "other clans", "clans other than the issuing clan", "exclude the proposer clan" } },
			{ "owner_proposer_clan", new[] { "the issuing clan", "the proposer clan", "the clan that proposed this policy" } },
			{ "geography_border", new[] { "border settlements", "frontier settlements", "settlements on the border" } },
			{ "direction_north", new[] { "northern settlements", "settlements in the north", "north" } },
			{ "direction_south", new[] { "southern settlements", "settlements in the south", "south" } },
			{ "direction_east", new[] { "eastern settlements", "settlements in the east", "east" } },
			{ "direction_west", new[] { "western settlements", "settlements in the west", "west" } },
			{ "distance_nearest", new[] { "nearest settlements", "closest settlements", "the nearest castle", "nearest", "closest" } },
			{ "distance_farthest", new[] { "farthest settlements", "most distant settlements", "the farthest castle", "farthest", "furthest" } },
			{ "cardinality_all", new[] { "all settlements", "every settlement", "all" } },
			{ "cardinality_top", new[] { "top ranked settlements", "the top few settlements", "top" } },
			{ "cardinality_bottom", new[] { "bottom ranked settlements", "the bottom few settlements", "bottom" } },
			{ "metric_wealth_high", new[] { "richest", "highest wealth", "wealth is high", "settlements with the highest wealth" } },
			{ "metric_wealth_low", new[] { "poorest", "lowest wealth", "wealth is low", "settlements with the lowest wealth" } },
			{ "metric_influence_high", new[] { "most influential", "highest influence", "influence is high", "settlements with the highest influence" } },
			{ "metric_influence_low", new[] { "least influential", "lowest influence", "influence is low", "settlements with the lowest influence" } },
			{ "metric_strength_high", new[] { "strongest", "highest strength", "strength is high", "settlements with the highest strength" } },
			{ "metric_strength_low", new[] { "weakest", "lowest strength", "strength is low", "settlements with the lowest strength" } },
			{ "metric_fiefs_high", new[] { "most fiefs", "highest number of fiefs", "number of fiefs is high", "settlements with the highest number of fiefs" } },
			{ "metric_fiefs_low", new[] { "fewest fiefs", "lowest number of fiefs", "number of fiefs is low", "settlements with the lowest number of fiefs" } },
			{ "metric_food_high", new[] { "most food", "highest food", "food is high", "settlements with the highest food" } },
			{ "metric_food_low", new[] { "least food", "lowest food", "food is low", "settlements with the lowest food" } },
			{ "metric_prosperity_high", new[] { "most prosperous", "highest prosperity", "prosperity is high", "settlements with the highest prosperity" } },
			{ "metric_prosperity_low", new[] { "least prosperous", "lowest prosperity", "prosperity is low", "settlements with the lowest prosperity" } },
			{ "metric_loyalty_high", new[] { "most loyal", "highest loyalty", "loyalty is high", "settlements with the highest loyalty" } },
			{ "metric_loyalty_low", new[] { "least loyal", "lowest loyalty", "loyalty is low", "settlements with the lowest loyalty" } },
			{ "metric_security_high", new[] { "most secure", "highest security", "security is high", "settlements with the highest security" } },
			{ "metric_security_low", new[] { "least secure", "lowest security", "security is low", "settlements with the lowest security" } },
			{ "metric_hearth_high", new[] { "most village hearths", "highest village hearths", "village hearths is high", "settlements with the highest village hearths" } },
			{ "metric_hearth_low", new[] { "fewest village hearths", "lowest village hearths", "village hearths is low", "settlements with the lowest village hearths" } },
			{ "metric_militia_high", new[] { "most militia", "highest militia", "militia is high", "settlements with the highest militia" } },
			{ "metric_militia_low", new[] { "least militia", "lowest militia", "militia is low", "settlements with the lowest militia" } },
		};
	private static readonly IReadOnlyList<PolicyTargetPlanIntentAtom> AtomSnapshot = BuildAtoms();

	private static readonly KeyValuePair<PolicyTargetPlanMetric, string>[] MetricThresholdAliases =
	{
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, PolicyCompatibilityTexts.Get("Targets_Compat_e6692b7928d8")),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, "hearth"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, PolicyCompatibilityTexts.Get("Targets_Compat_cfab635d1c1b")),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Hearth, PolicyCompatibilityTexts.Get("Targets_Compat_4cfd3da9115f")),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Militia, "militia"),
		new KeyValuePair<PolicyTargetPlanMetric, string>(PolicyTargetPlanMetric.Militia, PolicyCompatibilityTexts.Get("Module_militiaPerDay_playerDisplayName"))
	};

	private static readonly KeyValuePair<string, PolicyTargetPlanMetricComparison>[] MetricComparisonCues =
	{
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_80c4751b0481"), PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_671e1f421e67"), PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_035d78382d6f"), PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("<=", PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_4c0b2b9f4253"), PolicyTargetPlanMetricComparison.LessThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_19cd915c5a14"), PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_e1feef07d1b8"), PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(">=", PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_7ef93a55e5a1"), PolicyTargetPlanMetricComparison.GreaterThanOrEqual),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_e57d3dc19d83"), PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_30f5e365f11d"), PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_a39accd3595f"), PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>("<", PolicyTargetPlanMetricComparison.LessThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_47d524d0f1e0"), PolicyTargetPlanMetricComparison.GreaterThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_a2e0be841cce"), PolicyTargetPlanMetricComparison.GreaterThan),
		new KeyValuePair<string, PolicyTargetPlanMetricComparison>(PolicyCompatibilityTexts.Get("Targets_Compat_b4614ab230a5"), PolicyTargetPlanMetricComparison.GreaterThan),
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
			? result.Issues.Select(issue => issue?.Message).FirstOrDefault(message => !string.IsNullOrWhiteSpace(message)) ?? PolicyGenerationPromptTexts.Get("Targets_Text_8d174b7a7be8", "The explicit target cannot be resolved safely.")
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
		return HasExplicitTargetBoundary(FindExplicitAtomIds(normalizedQuery, typeCueQuery, query));
	}

	private static bool IsMissingRuntimeAnchorError(string error)
	{
		return !string.IsNullOrWhiteSpace(error)
			&& (error.IndexOf(PolicyCompatibilityTexts.Get("Targets_Compat_1821ae192b56"), StringComparison.Ordinal) >= 0
				|| error.IndexOf(PolicyCompatibilityTexts.Get("Targets_Compat_e8026f4654f7"), StringComparison.Ordinal) >= 0
				|| string.Equals(error, PolicyGenerationPromptTexts.Get("Targets_Text_764dfa9fd71d", "\"Other places\" lacks the current publication-location anchor."), StringComparison.Ordinal));
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
			PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_10"),
			PolicyCompatibilityTexts.Get("Targets_Compat_308dbfed9d76"),
			PolicyCompatibilityTexts.Get("Targets_Compat_9bfd29d0623b"),
			PolicyCompatibilityTexts.Get("Targets_Compat_93c03d4eae31"),
			PolicyCompatibilityTexts.Get("Targets_Compat_01146936a29e"),
			PolicyCompatibilityTexts.Get("Targets_Compat_e4d1db5f493a"),
			PolicyCompatibilityTexts.Get("Targets_Compat_f2ddec4de5b8"),
			PolicyCompatibilityTexts.Get("Targets_Compat_c49c99bce7fd"),
			PolicyCompatibilityTexts.Get("Targets_Compat_407617e38225"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2ead8ff6bf82"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2d521840207f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_90343c1795db"),
			PolicyCompatibilityTexts.Get("Targets_Compat_69b71ae2b12a"),
			PolicyCompatibilityTexts.Get("Targets_Compat_f6cadea2a00f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_a1dff4058f6b"),
			PolicyCompatibilityTexts.Get("Targets_Compat_ffca6a30dc50"),
			PolicyCompatibilityTexts.Get("Targets_Compat_6aa5c62f0b3a"));
		bool allTargetKingdomClansCue = ContainsAny(
			normalizedQuery,
			PolicyCompatibilityTexts.Get("Targets_Compat_b2f74dad5cf2"),
			PolicyCompatibilityTexts.Get("Targets_Compat_da7cea859a8e"),
			PolicyCompatibilityTexts.Get("Targets_Compat_836af340d63e"),
			PolicyCompatibilityTexts.Get("Targets_Compat_68755450ac21"),
			PolicyCompatibilityTexts.Get("Targets_Compat_8282d0fc8389"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2f66616fcfa5"),
			PolicyCompatibilityTexts.Get("Targets_Compat_13217d378272"),
			PolicyCompatibilityTexts.Get("Targets_Compat_38f20f31dd4f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2f78fba165bb"),
			PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_107"),
			PolicyCompatibilityTexts.Get("Targets_Compat_8b032bde3294"),
			PolicyCompatibilityTexts.Get("Targets_Compat_36225a806046"),
			PolicyCompatibilityTexts.Get("Targets_Compat_39e6e62091a3"),
			PolicyCompatibilityTexts.Get("Targets_Compat_351eb55a6290"));
		bool otherTargetKingdomClansCue = ContainsAny(
			normalizedQuery,
			PolicyCompatibilityTexts.Get("Targets_Compat_47b37d096fd8"),
			PolicyCompatibilityTexts.Get("Targets_Compat_801435c1ac44"),
			PolicyCompatibilityTexts.Get("Targets_Compat_5e0e22137b90"),
			PolicyCompatibilityTexts.Get("Targets_Compat_55f77e17d365"),
			PolicyCompatibilityTexts.Get("Targets_Compat_fb09a5c2ffff"),
			PolicyCompatibilityTexts.Get("Targets_Compat_4807ef250d2d"),
			PolicyCompatibilityTexts.Get("Targets_Compat_63f197695d20"),
			PolicyCompatibilityTexts.Get("Targets_Compat_329714d7b714"),
			PolicyCompatibilityTexts.Get("Targets_Compat_cec8e18146e7"),
			PolicyCompatibilityTexts.Get("Targets_Compat_597bb26ac400"),
			PolicyCompatibilityTexts.Get("Targets_Compat_335822ee7736"),
			PolicyCompatibilityTexts.Get("Targets_Compat_3a980cebd71b"),
			PolicyCompatibilityTexts.Get("Targets_Compat_66a2f31d716a"),
			PolicyCompatibilityTexts.Get("Targets_Compat_fc732f694f6d"));
		bool proposerClanCue = ContainsAny(
			normalizedQuery,
			PolicyCompatibilityTexts.Get("Targets_Compat_02142ecca8da"),
			PolicyCompatibilityTexts.Get("Targets_Compat_0f73d761314f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_37a90292077f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_171719c3632d"),
			PolicyCompatibilityTexts.Get("Targets_Compat_c8c147056a97"),
			PolicyCompatibilityTexts.Get("Targets_Compat_d7e6b2737138"),
			PolicyCompatibilityTexts.Get("Targets_Compat_932904029459"),
			PolicyCompatibilityTexts.Get("Targets_Compat_ca56861814e2"),
			PolicyCompatibilityTexts.Get("Targets_Compat_06042131a9ac"),
			PolicyCompatibilityTexts.Get("Targets_Compat_5fd81e3a63a4"))
			|| (SameId(context.PlayerClanId, context.ProposerClanId)
				&& ContainsAny(
					normalizedQuery,
					PolicyCompatibilityTexts.Get("Targets_Compat_4c35ed8cec1d"),
					PolicyCompatibilityTexts.Get("Targets_Compat_adc8fbd40507"),
					PolicyCompatibilityTexts.Get("Targets_Compat_a4d7e1087b11"),
					PolicyCompatibilityTexts.Get("Targets_Compat_82338d0ddf67")));
		bool affectedRegionOwnerCue = ContainsAny(
			normalizedQuery,
			PolicyCompatibilityTexts.Get("Targets_Compat_3a0328d5a4b8"),
			PolicyCompatibilityTexts.Get("Targets_Compat_b7160b7f46c5"),
			PolicyCompatibilityTexts.Get("Targets_Compat_787997f12422"),
			PolicyCompatibilityTexts.Get("Targets_Compat_df6c3de2f742"),
			PolicyCompatibilityTexts.Get("Targets_Compat_7beb5e4a458d"),
			PolicyCompatibilityTexts.Get("Targets_Compat_9434a66c3541"),
			PolicyCompatibilityTexts.Get("Targets_Compat_47a262e16727"),
			PolicyCompatibilityTexts.Get("Targets_Compat_860fb13fd938"),
			PolicyCompatibilityTexts.Get("Targets_Compat_c1aa0c85dc47"),
			PolicyCompatibilityTexts.Get("Targets_Compat_4c9405aa2ecc"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2c521c628075"),
			PolicyCompatibilityTexts.Get("Targets_Compat_9da653307395"),
			PolicyCompatibilityTexts.Get("Targets_Compat_bc8f898e366d"),
			PolicyCompatibilityTexts.Get("Targets_Compat_c60b8c15ceb4"));
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
			PolicyCompatibilityTexts.Get("Targets_Compat_99fe7bff6c95"),
			PolicyCompatibilityTexts.Get("Targets_Compat_ca257753a7b9"),
			PolicyCompatibilityTexts.Get("Targets_Compat_31564e1f372e"),
			PolicyCompatibilityTexts.Get("Targets_Compat_eef932646f02"),
			PolicyCompatibilityTexts.Get("Targets_Compat_ce989e71b6f5"),
			PolicyCompatibilityTexts.Get("Targets_Compat_2059d7f1e966"),
			PolicyCompatibilityTexts.Get("Targets_Compat_087269aeaf58"),
			PolicyCompatibilityTexts.Get("Targets_Compat_78e20875d600"),
			PolicyCompatibilityTexts.Get("Targets_Compat_551ee9cf1235"),
			PolicyCompatibilityTexts.Get("Targets_Compat_67bad5bd2b80"),
			PolicyCompatibilityTexts.Get("Targets_Compat_8257f728a0d6"),
			PolicyCompatibilityTexts.Get("Targets_Compat_997a5e6e513f"),
			PolicyCompatibilityTexts.Get("Targets_Compat_da0a1e4badc7"),
			PolicyCompatibilityTexts.Get("Targets_Compat_8672864e9041"),
			PolicyCompatibilityTexts.Get("Targets_Compat_8b5f2a61b797"),
			PolicyCompatibilityTexts.Get("Targets_Compat_eea3b2d2f03b"),
			PolicyCompatibilityTexts.Get("Targets_Compat_477e3eac4043"),
			PolicyCompatibilityTexts.Get("Targets_Compat_64dfcc065cea"),
			PolicyCompatibilityTexts.Get("Targets_Compat_788d5efc1161"),
			PolicyCompatibilityTexts.Get("Targets_Compat_a3865f53eaef"),
			PolicyCompatibilityTexts.Get("Targets_Compat_09e09a1a634e"),
			PolicyCompatibilityTexts.Get("Targets_Compat_6570a98f2850"),
			PolicyCompatibilityTexts.Get("Targets_Compat_7ec3939922dc"),
			PolicyCompatibilityTexts.Get("Targets_Compat_5c523ba29f3d"))
			|| FindExactMentionedEntities(
				context.Snapshot.Entities,
				normalizedQuery,
				PolicyTargetEntityKinds.Clan,
				context.StrictEntityEvidence).Count > 0;

		// “家族封地/领主领地”描述的是地理目标，不得被“其他家族”
		// 的词面提示错误收窄成家族执行目标。
		bool explicitClanFiefCue = ContainsAny(
			normalizedQuery,
			PolicyCompatibilityTexts.Get("Targets_Compat_88867878def8"),
			PolicyCompatibilityTexts.Get("Targets_Compat_e98151e2e218"),
			PolicyCompatibilityTexts.Get("Targets_Compat_7e837cd23ba3"),
			PolicyCompatibilityTexts.Get("Targets_Compat_a217b3ac878c"),
			PolicyCompatibilityTexts.Get("Targets_Compat_34c329c15408"),
			PolicyCompatibilityTexts.Get("Targets_Compat_ac0814be9fc8"),
			PolicyCompatibilityTexts.Get("Targets_Compat_5d56a2cfbf9c"),
			PolicyCompatibilityTexts.Get("Targets_Compat_dae9aaeba326"));
		string typeCueQuery = BuildUnboundTypeCueText(normalizedQuery, context.Snapshot.Entities);
		HashSet<string> explicitAtomIds = FindExplicitAtomIds(normalizedQuery, typeCueQuery, query);
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
				PolicyGenerationPromptTexts.Get("Targets_Text_b2f74dad5cf2", "All clans in the target kingdom"),
				"deterministic=target-kingdom-all-clans",
				out error))
		{
			return false;
		}
		if (otherTargetKingdomClansCue
			&& !TryAddDeterministicCandidate(
				result,
				CreateClanTargetPlan(PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan, PolicyTargetPlanRelation.Domestic),
				PolicyGenerationPromptTexts.Get("Targets_Text_eb4ca338a487", "Other clans in the target kingdom (excluding the publishing clan)"),
				"deterministic=target-kingdom-other-clans",
				out error))
		{
			return false;
		}
		if (proposerClanCue
			&& !TryAddDeterministicCandidate(
				result,
				CreateClanTargetPlan(PolicyTargetPlanOwnerClanPredicate.ProposerClan, PolicyTargetPlanRelation.Any),
				PolicyGenerationPromptTexts.Get("Targets_Text_02142ecca8da", "Publishing clan"),
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
					PolicyGenerationPromptTexts.Get("Targets_Text_1b8974cbc9b3", "Current owner clans of the affected area"),
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
		HashSet<string> explicitAtomIds = FindExplicitAtomIds(normalizedQuery, typeCueQuery, query);
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
		bool explicitMetricAscending = ContainsAny(normalizedQuery, PolicyCompatibilityTexts.Get("Targets_Compat_ca715c0d8d83"), PolicyCompatibilityTexts.Get("Targets_Compat_1606ac42755a"), PolicyCompatibilityTexts.Get("Targets_Compat_96209b5f421b"));
		bool explicitMetricDescending = ContainsAny(normalizedQuery, PolicyCompatibilityTexts.Get("Targets_Compat_4b77c3546823"), PolicyCompatibilityTexts.Get("Targets_Compat_5d1671517ac1"), PolicyCompatibilityTexts.Get("Targets_Compat_22c3b494ee6e"));
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
		bool fiefCue = ContainsAny(normalizedQuery, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"), PolicyCompatibilityTexts.Get("Targets_Compat_f9bab1c5399d"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_052"), PolicyCompatibilityTexts.Get("Targets_Compat_1462a7d8531e"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095"), PolicyCompatibilityTexts.Get("Targets_Compat_2059d7f1e966"), PolicyCompatibilityTexts.Get("Targets_Compat_661b85c39112"), PolicyCompatibilityTexts.Get("Targets_Compat_b28c4dbf4295"), PolicyCompatibilityTexts.Get("Targets_Compat_eaf086f666cf"), PolicyCompatibilityTexts.Get("Targets_Compat_fe5e1225977e"), PolicyCompatibilityTexts.Get("Targets_Compat_4cfd3da9115f"), PolicyCompatibilityTexts.Get("Targets_Compat_cfab635d1c1b"), "hearth", PolicyCompatibilityTexts.Get("Module_militiaPerDay_playerDisplayName"), "militia");
		bool clanFiefCue = ContainsAny(normalizedQuery, PolicyCompatibilityTexts.Get("Targets_Compat_88867878def8"), PolicyCompatibilityTexts.Get("Targets_Compat_e98151e2e218"), PolicyCompatibilityTexts.Get("Targets_Compat_7e837cd23ba3"), PolicyCompatibilityTexts.Get("Targets_Compat_a217b3ac878c"), PolicyCompatibilityTexts.Get("Targets_Compat_5d56a2cfbf9c"), PolicyCompatibilityTexts.Get("Targets_Compat_dae9aaeba326"));
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
			error = PolicyGenerationPromptTexts.Get("Targets_Text_764dfa9fd71d", "\"Other places\" lacks the current publication-location anchor.");
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
				error = PolicyGenerationPromptTexts.Get("Targets_Text_239f71f7f829", "\"Other clans\" has multiple possible reference clans.");
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
			error = PolicyGenerationPromptTexts.Get("Targets_Text_764dfa9fd71d", "\"Other places\" lacks the current publication-location anchor.");
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
				error = PolicyGenerationPromptTexts.Get("Targets_Text_764dfa9fd71d", "\"Other places\" lacks the current publication-location anchor.");
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
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_5c55a67935af"), PolicyCompatibilityTexts.Get("Targets_Compat_ae99f297e9f5"), PolicyCompatibilityTexts.Get("Targets_Compat_57e34a600712"), PolicyCompatibilityTexts.Get("Targets_Compat_752b90b744bd"), PolicyCompatibilityTexts.Get("Targets_Compat_fe5591bd9cc1"), PolicyCompatibilityTexts.Get("Targets_Compat_e755b0716035"), PolicyCompatibilityTexts.Get("Targets_Compat_3b8c14de9027"), PolicyCompatibilityTexts.Get("Targets_Compat_988ba5fd2a5f"), PolicyCompatibilityTexts.Get("Targets_Compat_78b43744dd81"), PolicyCompatibilityTexts.Get("Targets_Compat_6d47ebde28fd"), PolicyCompatibilityTexts.Get("Targets_Compat_08b602275aee")))
		{
			branch.Cardinality = PolicyTargetPlanCardinality.All;
			branch.Limit = 0;
			return;
		}
		int count = ParseRequestedCount(query);
		if (count > 0)
		{
			branch.Cardinality = ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_60be66d61586"), PolicyCompatibilityTexts.Get("Targets_Compat_8b5f2a61b797"), PolicyCompatibilityTexts.Get("Targets_Compat_477e3eac4043"), PolicyCompatibilityTexts.Get("Targets_Compat_788d5efc1161"), PolicyCompatibilityTexts.Get("Targets_Compat_09e09a1a634e"), PolicyCompatibilityTexts.Get("Targets_Compat_ef2e2e8ee22a"))
                || Regex.IsMatch(query, @"bottom(?=[0-9])", RegexOptions.CultureInvariant)
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
		string[] chinese = { PolicyCompatibilityTexts.Get("Targets_Compat_9489a4a6f3fb"), PolicyCompatibilityTexts.Get("Targets_Compat_51a75f4634df"), PolicyCompatibilityTexts.Get("Targets_Compat_084b42f6e95e"), PolicyCompatibilityTexts.Get("Targets_Compat_a4c3313deb18"), PolicyCompatibilityTexts.Get("Targets_Compat_754a9d5828d5"), PolicyCompatibilityTexts.Get("Targets_Compat_c9b87f516a38"), PolicyCompatibilityTexts.Get("Targets_Compat_de07b5383874"), PolicyCompatibilityTexts.Get("Targets_Compat_1b0542878c67"), PolicyCompatibilityTexts.Get("Targets_Compat_2c467d367392"), PolicyCompatibilityTexts.Get("Targets_Compat_df897da6e02e"), PolicyCompatibilityTexts.Get("Targets_Compat_fc7edd399a5c") };
		for (int value = 10; value >= 1; value--)
		{
			if (query.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_6044e9f095e0") + chinese[value])
				|| query.Contains(PolicyCompatibilityTexts.Get("Targets_Compat_60be66d61586") + chinese[value])
				|| query.Contains(chinese[value] + PolicyCompatibilityTexts.Get("Targets_Compat_001d9c64f7f1"))
				|| query.Contains(chinese[value] + PolicyCompatibilityTexts.Get("Targets_Compat_34ee554a469c"))
				|| query.Contains(chinese[value] + PolicyCompatibilityTexts.Get("Targets_Compat_51a5d681a99c"))
				|| query.Contains(chinese[value] + PolicyCompatibilityTexts.Get("Targets_Compat_c8ae97be80f3")))
			{
				return value;
			}
		}
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_5fad1e271651"), PolicyCompatibilityTexts.Get("Targets_Compat_60999bbd1d51"), PolicyCompatibilityTexts.Get("Targets_Compat_dfc348f2b17b"), PolicyCompatibilityTexts.Get("Targets_Compat_fd8fcaec5221"), PolicyCompatibilityTexts.Get("Targets_Compat_a427863be055"), PolicyCompatibilityTexts.Get("Targets_Compat_59675f8778f6")))
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
			? new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c"), PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151"), PolicyCompatibilityTexts.Get("Targets_Compat_24c60e89e082") }
			: string.Equals(entityKind, PolicyTargetEntityKinds.Clan, StringComparison.OrdinalIgnoreCase)
				? new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091"), PolicyCompatibilityTexts.Get("Targets_Compat_12af41cb2d29"), PolicyCompatibilityTexts.Get("Targets_Compat_b8003d4ad0d6") }
				: string.Equals(entityKind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
					? new[] { PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_051"), PolicyCompatibilityTexts.Get("Targets_Compat_61a401cf32d4"), PolicyCompatibilityTexts.Get("Targets_Compat_76f47004cc12"), PolicyCompatibilityTexts.Get("Targets_Compat_32bd97013331") }
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

	private static HashSet<string> FindExplicitAtomIds(string query, string typeCueQuery, string originalQuery = null)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
		string typeQuery = string.IsNullOrWhiteSpace(typeCueQuery) ? query : typeCueQuery;
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_c873aa60bb4e"), PolicyCompatibilityTexts.Get("Targets_Compat_d6d0d79ca0cd"), PolicyCompatibilityTexts.Get("Targets_Compat_5bbb51e32bb1"), PolicyCompatibilityTexts.Get("Targets_Compat_4c4db24b0391"), PolicyCompatibilityTexts.Get("Targets_Compat_678d0ef06078"), PolicyCompatibilityTexts.Get("Targets_Compat_c3703d114def"))) result.Add("exclude_publication");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_b88dfb017a3d"), PolicyCompatibilityTexts.Get("Targets_Compat_6a912db4db2b"), PolicyCompatibilityTexts.Get("Targets_Compat_1470f04c3e45"), PolicyCompatibilityTexts.Get("Targets_Compat_842368f6a52f"))) result.Add("exclude_player_clan");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_63f197695d20"), PolicyCompatibilityTexts.Get("Targets_Compat_329714d7b714"), PolicyCompatibilityTexts.Get("Targets_Compat_cec8e18146e7"), PolicyCompatibilityTexts.Get("Targets_Compat_597bb26ac400"))) result.Add("owner_other_clans");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_02142ecca8da"), PolicyCompatibilityTexts.Get("Targets_Compat_0f73d761314f"), PolicyCompatibilityTexts.Get("Targets_Compat_37a90292077f"), PolicyCompatibilityTexts.Get("Targets_Compat_171719c3632d"), PolicyCompatibilityTexts.Get("Targets_Compat_c8c147056a97"), PolicyCompatibilityTexts.Get("Targets_Compat_d7e6b2737138"), PolicyCompatibilityTexts.Get("Targets_Compat_932904029459"), PolicyCompatibilityTexts.Get("Targets_Compat_ca56861814e2"), PolicyCompatibilityTexts.Get("Targets_Compat_06042131a9ac"), PolicyCompatibilityTexts.Get("Targets_Compat_5fd81e3a63a4")))
		{
			result.Add("owner_proposer_clan");
			result.Add("type_clan");
		}
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_f752e00f622d"), PolicyCompatibilityTexts.Get("Targets_Compat_cec69ce03690"), PolicyCompatibilityTexts.Get("Targets_Compat_5f9569f60b3b"))) result.Add("relation_domestic");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_99fe7bff6c95"), PolicyCompatibilityTexts.Get("Targets_Compat_ca257753a7b9"), PolicyCompatibilityTexts.Get("Targets_Compat_7a9cd001d20f"))) result.Add("relation_enemy");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_31564e1f372e"), PolicyCompatibilityTexts.Get("Targets_Compat_35f0f6278ac7"), PolicyCompatibilityTexts.Get("Targets_Compat_4a3527415471"))) result.Add("relation_ally");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_eef932646f02"), PolicyCompatibilityTexts.Get("Targets_Compat_ce989e71b6f5"), PolicyCompatibilityTexts.Get("Targets_Compat_3b8a4a9a31d2"), PolicyCompatibilityTexts.Get("Targets_Compat_927841294109"), PolicyCompatibilityTexts.Get("Targets_Compat_979c8c7e1957"), PolicyCompatibilityTexts.Get("Targets_Compat_915d13c50c2b"), PolicyCompatibilityTexts.Get("Targets_Compat_14db7c0fdab1"))) result.Add("relation_foreign");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_53667078705b"), PolicyCompatibilityTexts.Get("Targets_Compat_598c015fde8c"), PolicyCompatibilityTexts.Get("Targets_Compat_22d694c339ae"), PolicyCompatibilityTexts.Get("Targets_Compat_3cfde176aac7"))) result.Add("anchor_issuer");
		if (ContainsAny(typeQuery, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"))) result.Add("type_town");
		if (typeQuery.Contains(PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"))) result.Add("type_castle");
		if (ContainsAny(typeQuery, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_052"), PolicyCompatibilityTexts.Get("Targets_Compat_f9bab1c5399d"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095"))) result.Add("type_primary_fief");
		if (ContainsAny(typeQuery, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091"), PolicyCompatibilityTexts.Get("Targets_Compat_12af41cb2d29"), PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54"), PolicyCompatibilityTexts.Get("Targets_Compat_f49dc9dc440f"))) result.Add("type_clan");
		if (ContainsAny(typeQuery, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c"), PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151"))) result.Add("type_kingdom");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_2059d7f1e966"), PolicyCompatibilityTexts.Get("Targets_Compat_4f4e7575b174"), PolicyCompatibilityTexts.Get("Targets_Compat_087269aeaf58"))) result.Add("geography_border");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_78e20875d600"), PolicyCompatibilityTexts.Get("Targets_Compat_10aaace7ab59"), PolicyCompatibilityTexts.Get("Targets_Compat_9ec7c9b52e1b"))) result.Add("direction_north");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_551ee9cf1235"), PolicyCompatibilityTexts.Get("Targets_Compat_95261be7958c"), PolicyCompatibilityTexts.Get("Targets_Compat_3a9a886ae29d"))) result.Add("direction_south");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_67bad5bd2b80"), PolicyCompatibilityTexts.Get("Targets_Compat_529c35b3573b"), PolicyCompatibilityTexts.Get("Targets_Compat_46db57d7fc3b"))) result.Add("direction_east");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_8257f728a0d6"), PolicyCompatibilityTexts.Get("Targets_Compat_dca3ed381925"), PolicyCompatibilityTexts.Get("Targets_Compat_f9a7268fde03"))) result.Add("direction_west");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_997a5e6e513f"), PolicyCompatibilityTexts.Get("Targets_Compat_128befdd4693"), PolicyCompatibilityTexts.Get("Targets_Compat_a37b34464c33"))) result.Add("distance_nearest");
		if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_da0a1e4badc7"), PolicyCompatibilityTexts.Get("Targets_Compat_61e29b9f4464"))) result.Add("distance_farthest");
		bool all = ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_5c55a67935af"), PolicyCompatibilityTexts.Get("Targets_Compat_ae99f297e9f5"), PolicyCompatibilityTexts.Get("Targets_Compat_57e34a600712"), PolicyCompatibilityTexts.Get("Targets_Compat_752b90b744bd"), PolicyCompatibilityTexts.Get("Targets_Compat_fe5591bd9cc1"), PolicyCompatibilityTexts.Get("Targets_Compat_e755b0716035"), PolicyCompatibilityTexts.Get("Targets_Compat_3b8c14de9027"), PolicyCompatibilityTexts.Get("Targets_Compat_988ba5fd2a5f"), PolicyCompatibilityTexts.Get("Targets_Compat_78b43744dd81"), PolicyCompatibilityTexts.Get("Targets_Compat_6d47ebde28fd"), PolicyCompatibilityTexts.Get("Targets_Compat_08b602275aee"));
		if (all)
		{
			result.Add("cardinality_all");
		}
		else
		{
			int requestedCount = ParseRequestedCount(query);
			if (requestedCount > 0 || ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_8672864e9041"), PolicyCompatibilityTexts.Get("Targets_Compat_eea3b2d2f03b"), PolicyCompatibilityTexts.Get("Targets_Compat_64dfcc065cea"), PolicyCompatibilityTexts.Get("Targets_Compat_a3865f53eaef"), PolicyCompatibilityTexts.Get("Targets_Compat_041e0910b5cc")))
			{
				result.Add("cardinality_top");
			}
			if (ContainsAny(query, PolicyCompatibilityTexts.Get("Targets_Compat_8b5f2a61b797"), PolicyCompatibilityTexts.Get("Targets_Compat_477e3eac4043"), PolicyCompatibilityTexts.Get("Targets_Compat_788d5efc1161"), PolicyCompatibilityTexts.Get("Targets_Compat_09e09a1a634e"), PolicyCompatibilityTexts.Get("Targets_Compat_ef2e2e8ee22a")))
			{
				result.Remove("cardinality_top");
				result.Add("cardinality_bottom");
			}
		}
		AddMetricExplicitCues(result, query);
		AddEnglishExplicitAtomCues(result, originalQuery, typeQuery);
		return result;
	}


    private static void AddEnglishExplicitAtomCues(ISet<string> result, string originalQuery, string typeCueQuery)
    {
        if (string.IsNullOrWhiteSpace(originalQuery)) return;
        string text = originalQuery.ToLowerInvariant();
        // These aliases identify target attributes only; uncertain/questions do not gain explicit evidence.
        if (text.IndexOf('?') >= 0 || HasEnglishPhrase(text, "maybe") || HasEnglishPhrase(text, "perhaps")
            || HasEnglishPhrase(text, "possibly") || HasEnglishPhrase(text, "might")
            || HasEnglishPhrase(text, "not sure") || HasEnglishPhrase(text, "whether")
            || StartsEnglishQuestion(text)) return;
        foreach (KeyValuePair<string, string[]> entry in EnglishIntentSeeds)
        {
            bool exclusion = entry.Key.StartsWith("exclude_", StringComparison.Ordinal)
                || entry.Key == "owner_other_clans";
            foreach (string phrase in entry.Value)
            {
                int start = 0;
                while (start < text.Length)
                {
                    int index = text.IndexOf(phrase, start, StringComparison.Ordinal);
                    if (index < 0) break;
                    start = index + phrase.Length;
                    if (!IsEnglishPhraseBoundary(text, index, phrase.Length)) continue;
                    // Names already removed by the existing entity cue path cannot become entity types.
                    if (entry.Key.StartsWith("type_", StringComparison.Ordinal)
                        && !(typeCueQuery ?? string.Empty).Contains(NormalizeText(phrase))) continue;
                    if (!exclusion && IsEnglishCueNegated(text, index)) continue;
                    result.Add(entry.Key);
                    break;
                }
                if (result.Contains(entry.Key)) break;
            }
        }
    }

    private static bool StartsEnglishQuestion(string text)
    {
        string trimmed = text.TrimStart();
        return trimmed.StartsWith("should ", StringComparison.Ordinal)
            || trimmed.StartsWith("could ", StringComparison.Ordinal)
            || trimmed.StartsWith("would ", StringComparison.Ordinal)
            || trimmed.StartsWith("can ", StringComparison.Ordinal)
            || trimmed.StartsWith("is ", StringComparison.Ordinal)
            || trimmed.StartsWith("are ", StringComparison.Ordinal)
            || trimmed.StartsWith("what ", StringComparison.Ordinal)
            || trimmed.StartsWith("which ", StringComparison.Ordinal)
            || trimmed.StartsWith("how ", StringComparison.Ordinal);
    }

    private static bool HasEnglishPhrase(string text, string phrase)
    {
        int start = 0;
        while (start < text.Length)
        {
            int index = text.IndexOf(phrase, start, StringComparison.Ordinal);
            if (index < 0) return false;
            if (IsEnglishPhraseBoundary(text, index, phrase.Length)) return true;
            start = index + phrase.Length;
        }
        return false;
    }

    private static bool IsEnglishPhraseBoundary(string text, int index, int length)
        => (index == 0 || !char.IsLetterOrDigit(text[index - 1]))
            && (index + length == text.Length || !char.IsLetterOrDigit(text[index + length]));

    private static bool IsEnglishCueNegated(string text, int index)
    {
        int begin = index;
        while (begin > 0 && ",;.!?\n".IndexOf(text[begin - 1]) < 0) begin--;
        string prefix = text.Substring(begin, index - begin);
        return HasEnglishPhrase(prefix, "not") || HasEnglishPhrase(prefix, "don't")
            || HasEnglishPhrase(prefix, "never") || HasEnglishPhrase(prefix, "exclude")
            || HasEnglishPhrase(prefix, "excluding") || HasEnglishPhrase(prefix, "except")
            || HasEnglishPhrase(prefix, "without") || HasEnglishPhrase(prefix, "rather than");
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
					new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c"), PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151") },
					new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091"), PolicyCompatibilityTexts.Get("Targets_Compat_12af41cb2d29"), PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54"), PolicyCompatibilityTexts.Get("Targets_Compat_f49dc9dc440f") })))
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
					new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), PolicyCompatibilityTexts.Get("Targets_Compat_0cc659a9f57c"), PolicyCompatibilityTexts.Get("Targets_Compat_f1377277c151") },
					new[] { PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_052"), PolicyCompatibilityTexts.Get("Targets_Compat_f9bab1c5399d"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095") })))
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
				new[] { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091"), PolicyCompatibilityTexts.Get("Targets_Compat_12af41cb2d29"), PolicyCompatibilityTexts.Get("Targets_Compat_90c42cbd6c54"), PolicyCompatibilityTexts.Get("Targets_Compat_f49dc9dc440f") },
				new[] { PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), PolicyCompatibilityTexts.Get("Targets_Compat_ed84a1665f79"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"), PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_052"), PolicyCompatibilityTexts.Get("Targets_Compat_f9bab1c5399d"), PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095") }))
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
						|| ContainsAny(bridge, PolicyCompatibilityTexts.Get("Targets_Compat_8af2350cfd65"), PolicyCompatibilityTexts.Get("Targets_Compat_dae8c66b6329"), PolicyCompatibilityTexts.Get("Targets_Compat_bd09d69fa2f3"), PolicyCompatibilityTexts.Get("Targets_Compat_74713a5e576a"), PolicyCompatibilityTexts.Get("Targets_Compat_5f9569f60b3b"), PolicyCompatibilityTexts.Get("Targets_Compat_6d47ebde28fd"), PolicyCompatibilityTexts.Get("Targets_Compat_166f5a8e8ab3"), PolicyCompatibilityTexts.Get("Targets_Compat_a70dafd11ed0"), PolicyCompatibilityTexts.Get("Targets_Compat_4f51d0285e88"), PolicyCompatibilityTexts.Get("Targets_Compat_4bc81e480383"), PolicyCompatibilityTexts.Get("Targets_Compat_5c55a67935af"), PolicyCompatibilityTexts.Get("Targets_Compat_ae99f297e9f5")))
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
				error = PolicyGenerationPromptTexts.Get("Targets_Text_b6f7213758fb", "The TargetPlan clause contains mutually contradictory ") + group.Key + PolicyGenerationPromptTexts.Get("Targets_Text_3d10ed03caa2", " operators.");
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
		AddMetricCue(result, query, "wealth", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_a3865f53eaef"), PolicyCompatibilityTexts.Get("Targets_Compat_ebf7ba6c6238"), PolicyCompatibilityTexts.Get("Targets_Compat_dbf66e702a25") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_09e09a1a634e"), PolicyCompatibilityTexts.Get("Targets_Compat_c445dab91a30"), PolicyCompatibilityTexts.Get("Targets_Compat_95fdea83adeb") });
		AddMetricCue(result, query, "influence", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_afeca66471bc"), PolicyCompatibilityTexts.Get("Targets_Compat_711558272138") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_86f70cf134c1"), PolicyCompatibilityTexts.Get("Targets_Compat_d25dfb0d4e1f") });
		AddMetricCue(result, query, "strength", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_b966d70c4e21"), PolicyCompatibilityTexts.Get("Targets_Compat_b7091dd57763"), PolicyCompatibilityTexts.Get("Targets_Compat_524cf8127828") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_8a2170491cda"), PolicyCompatibilityTexts.Get("Targets_Compat_5ef87dfbb51b"), PolicyCompatibilityTexts.Get("Targets_Compat_a9373eb73daa") });
		AddMetricCue(result, query, "fiefs", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_29dd8f5d0c70"), PolicyCompatibilityTexts.Get("Targets_Compat_54b7110bbe10") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_69aebe97fc87"), PolicyCompatibilityTexts.Get("Targets_Compat_3bc922351fac") });
		AddMetricCue(result, query, "food", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_5fc1f9f360a9"), PolicyCompatibilityTexts.Get("Targets_Compat_53b72f94c899"), PolicyCompatibilityTexts.Get("Targets_Compat_8947fe3b3ae5") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_6b78594b10cd"), PolicyCompatibilityTexts.Get("Targets_Compat_2bb4bece0314"), PolicyCompatibilityTexts.Get("Targets_Compat_08ea9309c0d7") });
		AddMetricCue(result, query, "prosperity", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_22f308cf5845"), PolicyCompatibilityTexts.Get("Targets_Compat_72455a2cd877") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_c5ba611faf9c"), PolicyCompatibilityTexts.Get("Targets_Compat_9e757df3cd99"), PolicyCompatibilityTexts.Get("Targets_Compat_7e1e2461c3d6") });
		AddMetricCue(result, query, "loyalty", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_debfa4109450"), PolicyCompatibilityTexts.Get("Targets_Compat_ded05fb591bc") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_0ff1ac94f916"), PolicyCompatibilityTexts.Get("Targets_Compat_ffb566d5fc86") });
		AddMetricCue(result, query, "security", new[] { PolicyCompatibilityTexts.Get("Targets_Compat_7db09fccb350"), PolicyCompatibilityTexts.Get("Targets_Compat_d44bfb35d6f7") }, new[] { PolicyCompatibilityTexts.Get("Targets_Compat_64199b73d82c"), PolicyCompatibilityTexts.Get("Targets_Compat_872f15c89c33"), PolicyCompatibilityTexts.Get("Targets_Compat_85b599c97400") });
		AddMetricCue(result, query, "hearth",
			new[] { PolicyCompatibilityTexts.Get("Targets_Compat_f921402d4807"), PolicyCompatibilityTexts.Get("Targets_Compat_636caa462621"), PolicyCompatibilityTexts.Get("Targets_Compat_ff17650ae0ee"), PolicyCompatibilityTexts.Get("Targets_Compat_d4c83cae1dc0"), PolicyCompatibilityTexts.Get("Targets_Compat_d5f26c1fd2ee") },
			new[] { PolicyCompatibilityTexts.Get("Targets_Compat_61fc5322baef"), PolicyCompatibilityTexts.Get("Targets_Compat_3319d42d3d70"), PolicyCompatibilityTexts.Get("Targets_Compat_ad4ed611cd05"), PolicyCompatibilityTexts.Get("Targets_Compat_fc7da7050c40"), PolicyCompatibilityTexts.Get("Targets_Compat_90cc1aa3969a") });
		AddMetricCue(result, query, "militia",
			new[] { PolicyCompatibilityTexts.Get("Targets_Compat_feb498c95623"), PolicyCompatibilityTexts.Get("Targets_Compat_685f1c9b2982"), PolicyCompatibilityTexts.Get("Targets_Compat_3e9e251395f7"), PolicyCompatibilityTexts.Get("Targets_Compat_a5f7a0092c1d"), PolicyCompatibilityTexts.Get("Targets_Compat_59ecc8526552") },
			new[] { PolicyCompatibilityTexts.Get("Targets_Compat_58f6804bec79"), PolicyCompatibilityTexts.Get("Targets_Compat_9223743744c3"), PolicyCompatibilityTexts.Get("Targets_Compat_4f75ddadcfe1"), PolicyCompatibilityTexts.Get("Targets_Compat_099a268c5f6d"), PolicyCompatibilityTexts.Get("Targets_Compat_4cb65dffb51d") });
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
				|| query[index] == PolicyLegacyCharacter0 || query[index] == PolicyLegacyCharacter1))
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
				PolicyCompatibilityTexts.Get("Targets_Compat_ac83d3972fe9") + normalizedAlias,
				PolicyCompatibilityTexts.Get("Targets_Compat_682a7700683c") + normalizedAlias,
				PolicyCompatibilityTexts.Get("Targets_Compat_64765e93acd6") + normalizedAlias,
				PolicyCompatibilityTexts.Get("Targets_Compat_8587db1ec106") + normalizedAlias,
				PolicyCompatibilityTexts.Get("Targets_Compat_322272e54a20") + normalizedAlias,
				PolicyCompatibilityTexts.Get("Targets_Compat_d213ae206e7e") + normalizedAlias,
				normalizedAlias + PolicyCompatibilityTexts.Get("Targets_Compat_8555187709af"),
				normalizedAlias + PolicyCompatibilityTexts.Get("Targets_Compat_649dd16df123")))
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
			Atom("relation_domestic", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_c2eb9e61e5d9"), PolicyCompatibilityTexts.Get("Targets_Compat_3da4f655d5e1"), PolicyCompatibilityTexts.Get("Targets_Compat_552d1d1b49bf"), PolicyCompatibilityTexts.Get("Targets_Compat_4d04abd03e50")),
			Atom("relation_enemy", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_73b4c8b6ec50"), PolicyCompatibilityTexts.Get("Targets_Compat_e1becc67c0e4"), PolicyCompatibilityTexts.Get("Targets_Compat_3f3ab38a4c6b"), PolicyCompatibilityTexts.Get("Targets_Compat_7a9cd001d20f")),
			Atom("relation_ally", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_1923127a8c96"), PolicyCompatibilityTexts.Get("Targets_Compat_138cd23b77aa"), PolicyCompatibilityTexts.Get("Targets_Compat_926b75840ca8"), PolicyCompatibilityTexts.Get("Targets_Compat_6f24fb5eacd8")),
			Atom("relation_foreign", "relation", PolicyCompatibilityTexts.Get("Targets_Compat_b729e4d8817f"), PolicyCompatibilityTexts.Get("Targets_Compat_c896abd925f3"), PolicyCompatibilityTexts.Get("Targets_Compat_e518dc3a15b8"), PolicyCompatibilityTexts.Get("Targets_Compat_8d222a08c6ab"), PolicyCompatibilityTexts.Get("Targets_Compat_7a5f23e7a08a")),
			Atom("anchor_issuer", "anchor", PolicyCompatibilityTexts.Get("Targets_Compat_f62b93848377"), PolicyCompatibilityTexts.Get("Targets_Compat_2561a66d3b99"), PolicyCompatibilityTexts.Get("Targets_Compat_50b4d1d1e3a8"), PolicyCompatibilityTexts.Get("Targets_Compat_1c93240c971f")),
			Atom("type_primary_fief", "type", PolicyCompatibilityTexts.Get("Targets_Compat_46cdb4c0571b"), PolicyCompatibilityTexts.Get("Targets_Compat_77f6c56690d3"), PolicyCompatibilityTexts.Get("Targets_Compat_97e36126b464"), PolicyCompatibilityTexts.Get("Targets_Compat_1a76a34f43b8")),
			Atom("type_town", "type", PolicyCompatibilityTexts.Get("Targets_Compat_3552bd1e9b83"), PolicyCompatibilityTexts.Get("Targets_Compat_2e1cf1f16353"), PolicyCompatibilityTexts.Get("Targets_Compat_f15e9e679eec"), PolicyCompatibilityTexts.Get("Targets_Compat_3df1b2b7911a")),
			Atom("type_castle", "type", PolicyCompatibilityTexts.Get("Targets_Compat_d0560a023f77"), PolicyCompatibilityTexts.Get("Targets_Compat_681be0936234"), PolicyCompatibilityTexts.Get("Targets_Compat_3661ad04a5d6"), PolicyCompatibilityTexts.Get("Targets_Compat_ef3973cdec6a")),
			Atom("type_clan", "type", PolicyCompatibilityTexts.Get("Targets_Compat_2b3fab65749e"), PolicyCompatibilityTexts.Get("Targets_Compat_63f197695d20"), PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_134"), PolicyCompatibilityTexts.Get("Targets_Compat_e653a3ed9305")),
			Atom("type_kingdom", "type", PolicyCompatibilityTexts.Get("Targets_Compat_a3d56b8d6749"), PolicyCompatibilityTexts.Get("Targets_Compat_61c71c424030"), PolicyCompatibilityTexts.Get("Targets_Compat_0b5b16b3da4e"), PolicyCompatibilityTexts.Get("Targets_Compat_4a3527415471")),
			Atom("exclude_publication", "exclusion", PolicyCompatibilityTexts.Get("Targets_Compat_644caa7dbd79"), PolicyCompatibilityTexts.Get("Targets_Compat_c873aa60bb4e"), PolicyCompatibilityTexts.Get("Targets_Compat_84e7364fe0b3"), PolicyCompatibilityTexts.Get("Targets_Compat_9c86cfd7e73d"), PolicyCompatibilityTexts.Get("Targets_Compat_d6d0d79ca0cd")),
			Atom("exclude_specific_entity", "specific_exclusion", PolicyCompatibilityTexts.Get("Targets_Compat_de116e04ffdf"), PolicyCompatibilityTexts.Get("Targets_Compat_76ee3673cebe"), PolicyCompatibilityTexts.Get("Targets_Compat_0300fdfee0c2"), PolicyCompatibilityTexts.Get("Targets_Compat_19d0098da62d")),
			Atom("exclude_player_clan", "owner", PolicyCompatibilityTexts.Get("Targets_Compat_f539184104bd"), PolicyCompatibilityTexts.Get("Targets_Compat_bb7901720fd8"), PolicyCompatibilityTexts.Get("Targets_Compat_d1c494ec57e1"), PolicyCompatibilityTexts.Get("Targets_Compat_a3350baa3c8f")),
			Atom("owner_other_clans", "owner", PolicyCompatibilityTexts.Get("Targets_Compat_1667acdaa01b"), PolicyCompatibilityTexts.Get("Targets_Compat_30776c1cfe9b"), PolicyCompatibilityTexts.Get("Targets_Compat_64cfe70a42ba"), PolicyCompatibilityTexts.Get("Targets_Compat_c3792b224d4e")),
			Atom("owner_proposer_clan", "owner", PolicyCompatibilityTexts.Get("Targets_Compat_2f9d81e90990"), PolicyCompatibilityTexts.Get("Targets_Compat_02142ecca8da"), PolicyCompatibilityTexts.Get("Targets_Compat_37a90292077f"), PolicyCompatibilityTexts.Get("Targets_Compat_c8c147056a97")),
			Atom("geography_border", "geography", PolicyCompatibilityTexts.Get("Targets_Compat_c8c7b653f2f2"), PolicyCompatibilityTexts.Get("Targets_Compat_ef1efbc6a333"), PolicyCompatibilityTexts.Get("Targets_Compat_cd3317df4510"), PolicyCompatibilityTexts.Get("Targets_Compat_ba6089cb607a")),
			Atom("direction_north", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_6d7b8484a86b"), PolicyCompatibilityTexts.Get("Targets_Compat_67cd9d04f13c"), PolicyCompatibilityTexts.Get("Targets_Compat_8e0494fb6a42"), PolicyCompatibilityTexts.Get("Targets_Compat_c9ca9e1d5e79")),
			Atom("direction_south", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_1ee9cbaa1b6c"), PolicyCompatibilityTexts.Get("Targets_Compat_856fb909cc6e"), PolicyCompatibilityTexts.Get("Targets_Compat_65a6826b90e1"), PolicyCompatibilityTexts.Get("Targets_Compat_e3a1156a373e")),
			Atom("direction_east", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_525a72f7f130"), PolicyCompatibilityTexts.Get("Targets_Compat_7437d5f4a0ed"), PolicyCompatibilityTexts.Get("Targets_Compat_2b20bbd7aad2"), PolicyCompatibilityTexts.Get("Targets_Compat_f16c5467f191")),
			Atom("direction_west", "direction", PolicyCompatibilityTexts.Get("Targets_Compat_195d6473ec71"), PolicyCompatibilityTexts.Get("Targets_Compat_5756cdf12dae"), PolicyCompatibilityTexts.Get("Targets_Compat_1ca04a016247"), PolicyCompatibilityTexts.Get("Targets_Compat_1ae6d1b5ef32")),
			Atom("distance_nearest", "distance", PolicyCompatibilityTexts.Get("Targets_Compat_4ec0fd9c78f2"), PolicyCompatibilityTexts.Get("Targets_Compat_909ea6d2fa17"), PolicyCompatibilityTexts.Get("Targets_Compat_9465689da422"), PolicyCompatibilityTexts.Get("Targets_Compat_2db212be08af")),
			Atom("distance_farthest", "distance", PolicyCompatibilityTexts.Get("Targets_Compat_95a24920fad2"), PolicyCompatibilityTexts.Get("Targets_Compat_ce5cb8681767"), PolicyCompatibilityTexts.Get("Targets_Compat_12494c85f78f"), PolicyCompatibilityTexts.Get("Targets_Compat_39ffa0ee6cec")),
			Atom("cardinality_all", "cardinality", PolicyCompatibilityTexts.Get("Targets_Compat_ca995b37b137"), PolicyCompatibilityTexts.Get("Targets_Compat_488f030d6275"), PolicyCompatibilityTexts.Get("Targets_Compat_9b9f42f4801f"), PolicyCompatibilityTexts.Get("Targets_Compat_754441091486")),
			Atom("cardinality_top", "cardinality", PolicyCompatibilityTexts.Get("Targets_Compat_e6d5e682f8b3"), PolicyCompatibilityTexts.Get("Targets_Compat_ff4e4dd8e487"), PolicyCompatibilityTexts.Get("Targets_Compat_d6f412844bb4"), PolicyCompatibilityTexts.Get("Targets_Compat_0e99e9d0ef4a")),
			Atom("cardinality_bottom", "cardinality", PolicyCompatibilityTexts.Get("Targets_Compat_a7feb405a131"), PolicyCompatibilityTexts.Get("Targets_Compat_2d8c195105ed"), PolicyCompatibilityTexts.Get("Targets_Compat_14920b0482df"), PolicyCompatibilityTexts.Get("Targets_Compat_e7a59d88d3d0"))
		};
		result.AddRange(MetricAtoms("wealth", PolicyCompatibilityTexts.Get("Targets_Compat_8610e9b28806"), PolicyCompatibilityTexts.Get("Targets_Compat_f6e57fd96e1f"), PolicyCompatibilityTexts.Get("Targets_Compat_95fdea83adeb")));
		result.AddRange(MetricAtoms("influence", PolicyCompatibilityTexts.Get("Module_clanInfluence_Original_7"), PolicyCompatibilityTexts.Get("Targets_Compat_afeca66471bc"), PolicyCompatibilityTexts.Get("Targets_Compat_86f70cf134c1")));
		result.AddRange(MetricAtoms("strength", PolicyCompatibilityTexts.Get("Targets_Compat_3b75cb0169b7"), PolicyCompatibilityTexts.Get("Targets_Compat_b966d70c4e21"), PolicyCompatibilityTexts.Get("Targets_Compat_8a2170491cda")));
		result.AddRange(MetricAtoms("fiefs", PolicyCompatibilityTexts.Get("Targets_Compat_c7dcae5c6d8e"), PolicyCompatibilityTexts.Get("Targets_Compat_29dd8f5d0c70"), PolicyCompatibilityTexts.Get("Targets_Compat_69aebe97fc87")));
		result.AddRange(MetricAtoms("food", PolicyCompatibilityTexts.Get("Module_foodPerDay_playerDisplayName"), PolicyCompatibilityTexts.Get("Targets_Compat_5fc1f9f360a9"), PolicyCompatibilityTexts.Get("Targets_Compat_6b78594b10cd")));
		result.AddRange(MetricAtoms("prosperity", PolicyCompatibilityTexts.Get("Targets_Compat_7dbfe9cc7f6b"), PolicyCompatibilityTexts.Get("Targets_Compat_22f308cf5845"), PolicyCompatibilityTexts.Get("Targets_Compat_c5ba611faf9c")));
		result.AddRange(MetricAtoms("loyalty", PolicyCompatibilityTexts.Get("Targets_Compat_b81e967d8c41"), PolicyCompatibilityTexts.Get("Targets_Compat_debfa4109450"), PolicyCompatibilityTexts.Get("Targets_Compat_0ff1ac94f916")));
		result.AddRange(MetricAtoms("security", PolicyCompatibilityTexts.Get("Targets_Compat_fe5e1225977e"), PolicyCompatibilityTexts.Get("Targets_Compat_7db09fccb350"), PolicyCompatibilityTexts.Get("Targets_Compat_64199b73d82c")));
		result.AddRange(MetricAtoms("hearth", PolicyCompatibilityTexts.Get("Targets_Compat_e6692b7928d8"), PolicyCompatibilityTexts.Get("Targets_Compat_f921402d4807"), PolicyCompatibilityTexts.Get("Targets_Compat_61fc5322baef")));
		result.AddRange(MetricAtoms("militia", PolicyCompatibilityTexts.Get("Module_militiaPerDay_playerDisplayName"), PolicyCompatibilityTexts.Get("Targets_Compat_feb498c95623"), PolicyCompatibilityTexts.Get("Targets_Compat_58f6804bec79")));
		return result;
	}

	private static PolicyTargetPlanIntentAtom Atom(string id, string group, string document, params string[] seeds)
	{
		return new PolicyTargetPlanIntentAtom
		{
			Id = id,
			Group = group,
			Document = document,
			Seeds = (seeds ?? Array.Empty<string>())
				.Concat(EnglishIntentSeeds.TryGetValue(id, out string[] englishSeeds) ? englishSeeds : Array.Empty<string>())
				.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray()
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
			if (query[index] != PolicyLegacyCharacter2)
			{
				continue;
			}
			int currentLength = index + 1 < query.Length
				&& (query[index + 1] == PolicyLegacyCharacter3 || query[index + 1] == PolicyLegacyCharacter1)
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
		return string.Join(PolicyGenerationPromptTexts.Get("Targets_Text_60ab743988bb", " or "), plan.Branches.Select(branch =>
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
				return PolicyGenerationPromptTexts.Get("Targets_Text_c686ba462211", "Current target kingdom");
			}
			string relation = branch.Relation == PolicyTargetPlanRelation.Domestic ? PolicyGenerationPromptTexts.Get("Targets_Text_f752e00f622d", "Domestic ")
				: branch.Relation == PolicyTargetPlanRelation.Enemy ? PolicyGenerationPromptTexts.Get("Targets_Text_99fe7bff6c95", "Enemy ")
				: branch.Relation == PolicyTargetPlanRelation.Ally ? PolicyGenerationPromptTexts.Get("Targets_Text_31564e1f372e", "Allied ")
				: branch.Relation == PolicyTargetPlanRelation.Foreign ? PolicyGenerationPromptTexts.Get("Targets_Text_eef932646f02", "Foreign ")
				: branch.Relation == PolicyTargetPlanRelation.Specific ? PolicyGenerationPromptTexts.Get("Targets_Text_0b5b16b3da4e", "Specified kingdom ") : string.Empty;
			string type = branch.EntityType == PolicyTargetPlanEntityType.Town ? PolicyGenerationPromptTexts.Get("Targets_PlanTown", "towns")
				: branch.EntityType == PolicyTargetPlanEntityType.Castle ? PolicyGenerationPromptTexts.Get("Targets_PlanCastle", "castles")
				: branch.EntityType == PolicyTargetPlanEntityType.Clan ? PolicyGenerationPromptTexts.Get("Targets_PlanClan", "clans")
				: branch.EntityType == PolicyTargetPlanEntityType.Kingdom ? PolicyGenerationPromptTexts.Get("Targets_PlanKingdom", "kingdoms") : PolicyGenerationPromptTexts.Get("Targets_PlanPrimaryFief", "towns/castles");
			List<string> qualifiers = new List<string>();
			if (branch.Exclusions.Contains(PolicyTargetPlanExclusion.PublicationParents)) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_061d12705975", "excluding the publication location"));
			if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludePlayerClan) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_a8f949c2197f", "excluding the player clan"));
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludeProposerClan) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_63f197695d20", "other clans"));
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ProposerClan) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_02142ecca8da", "Publishing clan"));
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.SpecificClan) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_db2c3a511732", "specified clan"));
			else if (branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ExcludeSpecificClan) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_6471baf9aa1b", "excluding the specified clan"));
			if (branch.BorderOnly) qualifiers.Add(PolicyGenerationPromptTexts.Get("Targets_Text_2059d7f1e966", "border"));
			if (branch.Direction != PolicyTargetPlanDirection.Any) qualifiers.Add(DirectionText(branch.Direction));
			if (branch.Distance != PolicyTargetPlanDistance.None) qualifiers.Add(branch.Distance == PolicyTargetPlanDistance.Nearest ? PolicyGenerationPromptTexts.Get("Targets_Text_997a5e6e513f", "nearest") : PolicyGenerationPromptTexts.Get("Targets_Text_da0a1e4badc7", "farthest"));
			if (branch.MetricComparison != PolicyTargetPlanMetricComparison.None && branch.MetricThreshold.HasValue)
			{
				qualifiers.Add(MetricText(branch.Metric) + MetricComparisonText(branch.MetricComparison)
					+ branch.MetricThreshold.Value.ToString("R", CultureInfo.InvariantCulture));
			}
			else if (branch.Metric != PolicyTargetPlanMetric.None)
			{
				qualifiers.Add(MetricText(branch.Metric)
					+ (branch.SortDirection == PolicyTargetPlanSortDirection.Ascending ? PolicyGenerationPromptTexts.Get("Targets_Text_aa9e366f68d3", " low") : PolicyGenerationPromptTexts.Get("Targets_Text_b1c27820fec2", " high")));
			}
			string exclusion = qualifiers.Count > 0 ? "（" + string.Join("、", qualifiers) + "）" : string.Empty;
			string count = branch.Cardinality == PolicyTargetPlanCardinality.All ? PolicyGenerationPromptTexts.Get("Targets_Text_5c55a67935af", "all ")
				: branch.Cardinality == PolicyTargetPlanCardinality.TopN ? PolicyGenerationPromptTexts.Get("Targets_Text_6044e9f095e0", "top ") + branch.Limit.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("Targets_Text_3a116815bc9d", " ")
				: PolicyGenerationPromptTexts.Get("Targets_Text_60be66d61586", "bottom ") + branch.Limit.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("Targets_Text_3a116815bc9d", " ");
			return relation + count + type + exclusion;
		}));
	}

	private static string DirectionText(PolicyTargetPlanDirection direction)
	{
		switch (direction)
		{
			case PolicyTargetPlanDirection.North: return PolicyGenerationPromptTexts.Get("Targets_Text_78e20875d600", "north");
			case PolicyTargetPlanDirection.South: return PolicyGenerationPromptTexts.Get("Targets_Text_551ee9cf1235", "south");
			case PolicyTargetPlanDirection.East: return PolicyGenerationPromptTexts.Get("Targets_Text_67bad5bd2b80", "east");
			case PolicyTargetPlanDirection.West: return PolicyGenerationPromptTexts.Get("Targets_Text_8257f728a0d6", "west");
			default: return string.Empty;
		}
	}

	private static string MetricText(PolicyTargetPlanMetric metric)
	{
		switch (metric)
		{
			case PolicyTargetPlanMetric.Wealth: return PolicyGenerationPromptTexts.Get("Targets_Text_8610e9b28806", "wealth");
			case PolicyTargetPlanMetric.Influence: return PolicyGenerationPromptTexts.Get("Targets_Text_58d673e6a21c", "influence");
			case PolicyTargetPlanMetric.Strength: return PolicyGenerationPromptTexts.Get("Targets_Text_3b75cb0169b7", "strength");
			case PolicyTargetPlanMetric.FiefCount: return PolicyGenerationPromptTexts.Get("Targets_Text_68e5650dd93b", "fief count");
			case PolicyTargetPlanMetric.Food: return PolicyGenerationPromptTexts.Get("Targets_Text_661b85c39112", "food");
			case PolicyTargetPlanMetric.Prosperity: return PolicyGenerationPromptTexts.Get("Targets_Text_b28c4dbf4295", "prosperity");
			case PolicyTargetPlanMetric.Loyalty: return PolicyGenerationPromptTexts.Get("Targets_Text_eaf086f666cf", "loyalty");
			case PolicyTargetPlanMetric.Security: return PolicyGenerationPromptTexts.Get("Targets_Text_fe5e1225977e", "security");
			case PolicyTargetPlanMetric.Hearth: return PolicyGenerationPromptTexts.Get("Targets_Text_e6692b7928d8", "average attached-village hearths");
			case PolicyTargetPlanMetric.Militia: return PolicyGenerationPromptTexts.Get("AFPOL_UI_Effect_MilitiaPerDayName", "Militia");
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
			Atom("metric_" + id + "_high", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_3d491caf91bc") + noun + PolicyCompatibilityTexts.Get("Targets_Compat_9f892922ac29"),
				high, noun + PolicyCompatibilityTexts.Get("Targets_Compat_8672864e9041"), noun + PolicyCompatibilityTexts.Get("Targets_Compat_eea3b2d2f03b"), noun + PolicyCompatibilityTexts.Get("Targets_Compat_6109b50aef73")),
			Atom("metric_" + id + "_low", "metric", PolicyCompatibilityTexts.Get("Targets_Compat_3d491caf91bc") + noun + PolicyCompatibilityTexts.Get("Targets_Compat_ce2a7f70d23d"),
				low, noun + PolicyCompatibilityTexts.Get("Targets_Compat_8b5f2a61b797"), noun + PolicyCompatibilityTexts.Get("Targets_Compat_477e3eac4043"), noun + PolicyCompatibilityTexts.Get("Targets_Compat_b76034647250"))
		};
	}

	private static bool ContainsAny(string source, params string[] values)
	{
		return (values ?? Array.Empty<string>()).Any(value => source.Contains(value));
	}

	private static bool HasRequestedCountContext(string before, string after)
	{
		return before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_6044e9f095e0"), StringComparison.Ordinal)
			|| before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_60be66d61586"), StringComparison.Ordinal)
			|| before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_eea3b2d2f03b"), StringComparison.Ordinal)
			|| before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_4c0b2b9f4253"), StringComparison.Ordinal)
			|| before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_6de090e745df"), StringComparison.Ordinal)
			|| before.EndsWith(PolicyCompatibilityTexts.Get("Targets_Compat_63cc257d10b4"), StringComparison.Ordinal)
			|| before.EndsWith("top", StringComparison.Ordinal)
			|| before.EndsWith("bottom", StringComparison.Ordinal)
			|| after.StartsWith(PolicyCompatibilityTexts.Get("Targets_Compat_3a116815bc9d"), StringComparison.Ordinal)
			|| after.StartsWith(PolicyCompatibilityTexts.Get("Targets_Compat_001d9c64f7f1"), StringComparison.Ordinal)
			|| after.StartsWith(PolicyCompatibilityTexts.Get("Targets_Compat_34ee554a469c"), StringComparison.Ordinal)
			|| after.StartsWith(PolicyCompatibilityTexts.Get("Targets_Compat_51a5d681a99c"), StringComparison.Ordinal)
			|| after.StartsWith(PolicyCompatibilityTexts.Get("Targets_Compat_c8ae97be80f3"), StringComparison.Ordinal);
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
