using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.PolicyEffects;

internal sealed class PolicyEffectModuleSelection
{
	internal IPolicyEffectModule Module { get; set; }

	internal float RecallScore { get; set; }

	internal int BestIntentOrdinal { get; set; } = int.MaxValue;

	internal int BestIntentRank { get; set; } = int.MaxValue;

	internal int IntentHitCount { get; set; }

	internal int SelectionRank { get; set; } = -1;

	internal bool CueMatched { get; set; }

	internal string MatchedCueTerm { get; set; }
}

internal sealed class PolicyEffectModuleIntentRecall
{
	internal int IntentOrdinal { get; set; }

	internal bool IsPrimary { get; set; }

	internal string QueryText { get; set; } = string.Empty;

	internal IReadOnlyList<PolicyEffectModuleSelection> Ranked { get; set; } = Array.Empty<PolicyEffectModuleSelection>();
}

internal sealed class PolicyEffectModuleRoutingResult
{
	internal IReadOnlyList<PolicyEffectModuleSelection> Recalled { get; set; }

	internal IReadOnlyList<PolicyEffectModuleSelection> Candidates { get; set; }

	internal IReadOnlyList<PolicyEffectModuleSelection> Details { get; set; }

	internal int RequestedDetailLimit { get; set; }

	internal int EffectiveDetailLimit { get; set; }

	internal bool DetailLimitClamped { get; set; }

	internal int IntentCount { get; set; }

	internal int AdditionalQueryEmbeddingCount { get; set; }

	internal int CueMatchCount { get; set; }

	internal int EnabledModuleCount { get; set; }

	internal bool CandidateLimitTruncated { get; set; }

	internal IReadOnlyList<string> IntentTopModuleIds { get; set; } = Array.Empty<string>();
}

internal enum PolicyEffectMechanismShape
{
	None,
	DirectChange,
	CrossTargetFlow,
	IssuerAcquires,
	IssuerProvides,
	Exchange
}

internal sealed class PolicyEffectMechanismHint
{
	internal PolicyEffectMechanismShape Shape { get; set; }
	internal float Score { get; set; }
	internal float Margin { get; set; }
	internal bool IsHighConfidence { get; set; }
	internal bool RequiresLinked => Shape == PolicyEffectMechanismShape.CrossTargetFlow
		|| Shape == PolicyEffectMechanismShape.IssuerAcquires
		|| Shape == PolicyEffectMechanismShape.IssuerProvides
		|| Shape == PolicyEffectMechanismShape.Exchange;
}

internal static class PolicyEffectModuleRouter
{
	internal const int DenseCandidateLimit = 4;

	internal const int SemanticTopPerQuery = 4;

	internal const int CueReservePerQuery = 2;

	internal const int CandidateHardMaximum = QueryIntentLimit * (SemanticTopPerQuery + CueReservePerQuery);

	// Legacy callers may request the merged candidate view, but this bound is derived from
	// the bounded multi-query workload rather than the MCM detail-injection setting.
	internal const int RecallLimit = CandidateHardMaximum;

	internal const int DetailHardMaximum = DuelSettings.PlayerPolicyEffectModuleEffectiveDetailCountMaximum;

	internal const int QueryIntentLimit = 12;

	internal const int AdditionalQueryIntentLimit = QueryIntentLimit - 1;

	internal const int QueryIntentCharacterLimit = 320;

	internal const int SecondaryIntentDenseCandidateLimit = SemanticTopPerQuery;

	private sealed class IndexedModule
	{
		internal IPolicyEffectModule Module { get; set; }

		internal float[] Vector { get; set; }
	}

	private sealed class IndexedMechanismSeed
	{
		internal PolicyEffectMechanismShape Shape { get; set; }
		internal float[] Vector { get; set; }
	}

	private static readonly Lazy<IReadOnlyDictionary<string, IndexedModule>> Index = new Lazy<IReadOnlyDictionary<string, IndexedModule>>(BuildIndex, true);
	private static readonly Lazy<IReadOnlyList<IndexedMechanismSeed>> MechanismIndex =
		new Lazy<IReadOnlyList<IndexedMechanismSeed>>(BuildMechanismIndex, true);

	private const float MechanismConfidenceMinimum = 0.50f;

	private const float MechanismMarginMinimum = 0.025f;

	internal static float[] GetQueryEmbedding(string query)
	{
		string normalizedQuery = (query ?? string.Empty).Trim();
		if (normalizedQuery.Length <= 0)
		{
			throw new InvalidOperationException("政策效果模块检索文本为空。");
		}
		OnnxEmbeddingEngine embedding = OnnxEmbeddingEngine.Instance;
		if (embedding == null || !embedding.IsAvailable || !embedding.TryGetEmbedding(normalizedQuery, out float[] queryVector) || queryVector == null || queryVector.Length <= 0)
		{
			throw new InvalidOperationException("政策效果模块 ONNX embedding 不可用：" + (embedding?.LastError ?? "unknown"));
		}
		return queryVector;
	}

	internal static IReadOnlyList<string> BuildQueryIntents(string query, string scope)
	{
		return BuildQueryIntents(query, PolicyEffectModuleCatalog.GetModulesForScope(scope));
	}

	internal static IReadOnlyList<string> BuildQueryIntents(
		string query,
		PolicyEffectRetrievalContext context)
	{
		return BuildQueryIntents(query, PolicyEffectModuleRetrievalSettings.GetEnabledModules(context));
	}

	private static IReadOnlyList<string> BuildQueryIntents(
		string query,
		IReadOnlyList<IPolicyEffectModule> scopedModules)
	{
		string normalizedQuery = (query ?? string.Empty).Trim();
		if (normalizedQuery.Length == 0)
		{
			throw new InvalidOperationException("政策效果模块检索文本为空。");
		}
		scopedModules ??= Array.Empty<IPolicyEffectModule>();
		List<(string Text, int SourceOrdinal, bool CueMatched)> clauses = new List<(string, int, bool)>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		int sourceOrdinal = 0;
		foreach (string part in normalizedQuery.Split(
			new[] { '\r', '\n', '。', '；', ';', '，', ',', '！', '!', '？', '?' },
			StringSplitOptions.RemoveEmptyEntries))
		{
			string clause = LimitText(part.Trim(), QueryIntentCharacterLimit);
			if (clause.Length < 2
				|| string.Equals(clause, normalizedQuery, StringComparison.Ordinal)
				|| !seen.Add(clause))
			{
				sourceOrdinal++;
				continue;
			}
			string normalizedClause = NormalizeCueText(clause);
			bool cueMatched = scopedModules.Any(module =>
				FindMatchedCueTerm(normalizedClause, module?.CueTerms).Length > 0);
			clauses.Add((clause, sourceOrdinal++, cueMatched));
		}

		List<(string Text, int SourceOrdinal, bool CueMatched)> selected = clauses
			.Where(item => item.CueMatched)
			.OrderBy(item => item.SourceOrdinal)
			.Take(AdditionalQueryIntentLimit)
			.ToList();
		if (selected.Count < AdditionalQueryIntentLimit)
		{
			HashSet<int> selectedOrdinals = new HashSet<int>(selected.Select(item => item.SourceOrdinal));
			selected.AddRange(clauses
				.Where(item => !selectedOrdinals.Contains(item.SourceOrdinal))
				.OrderBy(item => item.SourceOrdinal)
				.Take(AdditionalQueryIntentLimit - selected.Count));
		}
		selected = selected.OrderBy(item => item.SourceOrdinal).ToList();
		return new[] { normalizedQuery }
			.Concat(selected.Select(item => item.Text))
			.ToArray();
	}

	internal static IReadOnlyList<string> BuildPostAssessmentQueries(
		string policyName,
		string policyContent,
		string impactSummary,
		string numericIntent,
		IReadOnlyList<IPolicyEffectModule> enabledModules)
	{
		string authoritative = ((policyName ?? string.Empty).Trim() + "\n" + (policyContent ?? string.Empty).Trim()).Trim();
		if (authoritative.Length == 0)
		{
			throw new InvalidOperationException("政策效果模块检索文本为空。");
		}
		// The first query is the frozen policy name plus the complete original text. The tokenizer owns
		// model-length truncation; C# must not replace the authoritative query with a narrative fragment.
		List<string> queries = new List<string>(QueryIntentLimit) { authoritative };
		HashSet<string> seen = new HashSet<string>(queries, StringComparer.Ordinal);
		IReadOnlyList<IPolicyEffectModule> modules = enabledModules ?? Array.Empty<IPolicyEffectModule>();
		List<(string Text, int Ordinal, bool CueMatched, bool NumericOrTimed)> clauses = new List<(string, int, bool, bool)>();
		int ordinal = 0;
		foreach (string part in (policyContent ?? string.Empty).Split(
			new[] { '\r', '\n', '。', '；', ';', '，', ',', '！', '!', '？', '?' },
			StringSplitOptions.RemoveEmptyEntries))
		{
			string clause = LimitText(part.Trim(), QueryIntentCharacterLimit);
			if (clause.Length < 2 || !seen.Add(clause))
			{
				ordinal++;
				continue;
			}
			string normalizedClause = NormalizeCueText(clause);
			bool cueMatched = modules.Any(module => FindMatchedCueTerm(normalizedClause, module?.CueTerms).Length > 0);
			bool numericOrTimed = clause.Any(char.IsDigit)
				|| clause.IndexOf("每日", StringComparison.Ordinal) >= 0
				|| clause.IndexOf("每天", StringComparison.Ordinal) >= 0
				|| clause.IndexOf("一次性", StringComparison.Ordinal) >= 0
				|| clause.IndexOf("单次", StringComparison.Ordinal) >= 0
				|| clause.IndexOf("第纳尔", StringComparison.Ordinal) >= 0
				|| clause.IndexOf("百分", StringComparison.Ordinal) >= 0;
			clauses.Add((clause, ordinal++, cueMatched, numericOrTimed));
		}

		List<string> supplements = new List<string>();
		if (!string.IsNullOrWhiteSpace(impactSummary))
		{
			supplements.Add("影响概述：" + impactSummary.Trim());
		}
		if (!string.IsNullOrWhiteSpace(numericIntent))
		{
			supplements.Add("数值意图：" + numericIntent.Trim());
		}
		int currentLimit = Math.Max(1, QueryIntentLimit - supplements.Count);
		Action<IEnumerable<string>> addQueries = values =>
		{
			foreach (string value in values ?? Array.Empty<string>())
			{
				string clean = LimitText(value, QueryIntentCharacterLimit);
				if (queries.Count >= currentLimit)
				{
					break;
				}
				if (clean.Length >= 2 && seen.Add(clean))
				{
					queries.Add(clean);
				}
			}
		};
		addQueries(clauses.Where(item => item.CueMatched).OrderBy(item => item.Ordinal).Select(item => item.Text));
		addQueries(clauses.Where(item => item.NumericOrTimed).OrderBy(item => item.Ordinal).Select(item => item.Text));
		addQueries(clauses.OrderBy(item => item.Ordinal).Select(item => item.Text));
		currentLimit = QueryIntentLimit;
		addQueries(supplements);
		return queries;
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> Recall(float[] queryVector, string scope)
	{
		return Recall(queryVector, PolicyEffectModuleCatalog.GetModulesForScope(scope));
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> Recall(
		float[] queryVector,
		PolicyEffectRetrievalContext context)
	{
		return Recall(queryVector, PolicyEffectModuleRetrievalSettings.GetEnabledModules(context));
	}

	private static IReadOnlyList<PolicyEffectModuleSelection> Recall(
		float[] queryVector,
		IReadOnlyList<IPolicyEffectModule> scopedModules)
	{
		if (queryVector == null || queryVector.Length <= 0)
		{
			throw new ArgumentException("政策效果模块 query embedding 无效。", nameof(queryVector));
		}
		List<IndexedModule> indexedModules = (scopedModules ?? Array.Empty<IPolicyEffectModule>())
			.Select(module => Index.Value[module.Id])
			.ToList();
		if (indexedModules.Count <= 0)
		{
			throw new InvalidOperationException("当前政策作用域没有可用的效果模块。");
		}
		if (!IsUsableEmbedding(queryVector, indexedModules[0].Vector.Length))
		{
			throw new ArgumentException("政策效果模块 query embedding 无效。", nameof(queryVector));
		}
		List<PolicyEffectModuleSelection> recalled = indexedModules
			.Select(item => new PolicyEffectModuleSelection
			{
				Module = item.Module,
				RecallScore = Cosine(queryVector, item.Vector)
			})
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Module.Order)
			.ThenBy(item => item.Module.Id, StringComparer.Ordinal)
			.Take(RecallLimit)
			.ToList();
		return recalled;
	}

	private static PolicyEffectModuleIntentRecall RecallIntent(
		float[] queryVector,
		string queryText,
		IReadOnlyList<IPolicyEffectModule> scopedModules,
		int intentOrdinal,
		bool isPrimary)
	{
		if (queryVector == null || queryVector.Length <= 0)
		{
			throw new ArgumentException("政策效果模块 query embedding 无效。", nameof(queryVector));
		}
		List<IndexedModule> indexedModules = (scopedModules ?? Array.Empty<IPolicyEffectModule>())
			.Select(module => Index.Value[module.Id])
			.ToList();
		if (indexedModules.Count == 0)
		{
			throw new InvalidOperationException("当前政策作用域没有可用的效果模块。");
		}
		if (!IsUsableEmbedding(queryVector, indexedModules[0].Vector.Length))
		{
			throw new InvalidOperationException("政策效果模块 query embedding 维度或数值无效：intent="
				+ intentOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
		List<PolicyEffectModuleSelection> ranked = indexedModules
			.Select(item => new PolicyEffectModuleSelection
			{
				Module = item.Module,
				RecallScore = Cosine(queryVector, item.Vector),
				BestIntentOrdinal = intentOrdinal
			})
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.Module.Order)
			.ThenBy(item => item.Module.Id, StringComparer.Ordinal)
			.ToList();
		for (int rank = 0; rank < ranked.Count; rank++)
		{
			ranked[rank].BestIntentRank = rank;
		}
		return new PolicyEffectModuleIntentRecall
		{
			IntentOrdinal = intentOrdinal,
			IsPrimary = isPrimary,
			QueryText = queryText ?? string.Empty,
			Ranked = ranked
		};
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> MergeCandidates(
		IReadOnlyList<PolicyEffectModuleIntentRecall> intentRecalls,
		IReadOnlyList<IPolicyEffectModule> scopedModules,
		string query,
		out int cueMatchCount,
		out bool candidateLimitTruncated)
	{
		if (intentRecalls == null
			|| intentRecalls.Count == 0
			|| intentRecalls.Any(intent => intent == null || intent.Ranked == null || intent.Ranked.Count == 0))
		{
			throw new ArgumentException("政策效果模块意图召回为空或无效。", nameof(intentRecalls));
		}
		List<IPolicyEffectModule> modules = (scopedModules ?? Array.Empty<IPolicyEffectModule>())
			.Where(module => module != null && !string.IsNullOrWhiteSpace(module.Id))
			.GroupBy(module => module.Id, StringComparer.Ordinal)
			.Select(group => group.First())
			.OrderBy(module => module.Order)
			.ThenBy(module => module.Id, StringComparer.Ordinal)
			.ToList();
		if (modules.Count == 0)
		{
			throw new ArgumentException("政策效果模块作用域目录为空。", nameof(scopedModules));
		}

		Dictionary<string, PolicyEffectModuleSelection> aggregate = modules.ToDictionary(
			module => module.Id,
			module => new PolicyEffectModuleSelection
			{
				Module = module,
				RecallScore = float.NegativeInfinity
			},
			StringComparer.Ordinal);
		List<PolicyEffectModuleIntentRecall> orderedIntents = intentRecalls
			.OrderBy(intent => intent.IntentOrdinal)
			.ToList();
		foreach (PolicyEffectModuleIntentRecall intent in orderedIntents)
		{
			int denseLimit = SemanticTopPerQuery;
			for (int rank = 0; rank < intent.Ranked.Count; rank++)
			{
				PolicyEffectModuleSelection source = intent.Ranked[rank];
				if (source?.Module == null || !aggregate.TryGetValue(source.Module.Id, out PolicyEffectModuleSelection target))
				{
					continue;
				}
				if (rank < denseLimit)
				{
					target.IntentHitCount++;
				}
				if (source.RecallScore > target.RecallScore
					|| (source.RecallScore.Equals(target.RecallScore)
						&& (intent.IntentOrdinal < target.BestIntentOrdinal
							|| (intent.IntentOrdinal == target.BestIntentOrdinal && rank < target.BestIntentRank))))
				{
					target.RecallScore = source.RecallScore;
					target.BestIntentOrdinal = intent.IntentOrdinal;
					target.BestIntentRank = rank;
				}
			}
		}

		string normalizedQuery = NormalizeCueText(query);
		foreach (PolicyEffectModuleSelection selection in aggregate.Values)
		{
			string matchedCueTerm = FindMatchedCueTerm(normalizedQuery, selection.Module.CueTerms);
			selection.CueMatched = matchedCueTerm.Length > 0;
			selection.MatchedCueTerm = matchedCueTerm;
		}
		HashSet<string> candidateIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectModuleIntentRecall intent in orderedIntents)
		{
			foreach (PolicyEffectModuleSelection selection in intent.Ranked.Take(SemanticTopPerQuery))
			{
				if (selection?.Module != null)
				{
					candidateIds.Add(selection.Module.Id);
				}
			}
			string normalizedIntent = NormalizeCueText(intent.QueryText);
			foreach (PolicyEffectModuleSelection selection in intent.Ranked
				.Where(item => item?.Module != null
					&& FindMatchedCueTerm(normalizedIntent, item.Module.CueTerms).Length > 0)
				.Take(CueReservePerQuery))
			{
				candidateIds.Add(selection.Module.Id);
			}
		}
		List<PolicyEffectModuleSelection> candidates = aggregate.Values
			.Where(selection => candidateIds.Contains(selection.Module.Id))
			.OrderByDescending(selection => selection.CueMatched)
			.ThenByDescending(selection => selection.IntentHitCount)
			.ThenByDescending(selection => selection.RecallScore)
			.ThenBy(selection => selection.BestIntentOrdinal)
			.ThenBy(selection => selection.BestIntentRank)
			.ThenBy(selection => selection.Module.Order)
			.ThenBy(selection => selection.Module.Id, StringComparer.Ordinal)
			.ToList();
		for (int index = 0; index < candidates.Count; index++)
		{
			candidates[index].SelectionRank = index;
		}
		cueMatchCount = candidates.Count(selection => selection.CueMatched);
		candidateLimitTruncated = candidates.Count < modules.Count;
		return candidates;
	}

	internal static PolicyEffectMechanismHint RouteMechanism(float[] queryVector)
	{
		if (queryVector == null || queryVector.Length <= 0)
		{
			return new PolicyEffectMechanismHint { Shape = PolicyEffectMechanismShape.None };
		}
		List<KeyValuePair<PolicyEffectMechanismShape, float>> ranked = MechanismIndex.Value
			.Where(seed => seed.Vector?.Length == queryVector.Length)
			.GroupBy(seed => seed.Shape)
			.Select(group => new KeyValuePair<PolicyEffectMechanismShape, float>(
				group.Key,
				group.Max(seed => Cosine(queryVector, seed.Vector))))
			.OrderByDescending(pair => pair.Value)
			.ThenBy(pair => pair.Key)
			.ToList();
		if (ranked.Count == 0)
		{
			return new PolicyEffectMechanismHint { Shape = PolicyEffectMechanismShape.None };
		}
		float score = ranked[0].Value;
		float runnerUp = ranked.Count > 1 ? ranked[1].Value : float.NegativeInfinity;
		float margin = float.IsNegativeInfinity(runnerUp) ? score : score - runnerUp;
		bool confident = score >= MechanismConfidenceMinimum && margin >= MechanismMarginMinimum;
		return new PolicyEffectMechanismHint
		{
			Shape = confident ? ranked[0].Key : PolicyEffectMechanismShape.None,
			Score = score,
			Margin = margin,
			IsHighConfidence = confident
		};
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> SelectFromRecallScores(
		IReadOnlyList<PolicyEffectModuleSelection> recalled,
		int selectionLimit)
	{
		return SelectDetails(recalled, selectionLimit);
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> SelectCandidates(
		IReadOnlyList<PolicyEffectModuleSelection> recalled,
		string query)
	{
		EnsureSelectionsAvailable(recalled, nameof(recalled));
		string normalizedQuery = NormalizeCueText(query);
		HashSet<string> denseIds = new HashSet<string>(
			recalled
				.OrderByDescending(item => item.RecallScore)
				.ThenBy(item => item.Module.Order)
				.ThenBy(item => item.Module.Id, StringComparer.Ordinal)
				.Take(DenseCandidateLimit)
				.Select(item => item.Module.Id),
			StringComparer.Ordinal);
		List<PolicyEffectModuleSelection> candidates = new List<PolicyEffectModuleSelection>(CandidateHardMaximum);
		foreach (PolicyEffectModuleSelection selection in recalled)
		{
			string matchedCueTerm = FindMatchedCueTerm(normalizedQuery, selection?.Module?.CueTerms);
			selection.CueMatched = matchedCueTerm.Length > 0;
			selection.MatchedCueTerm = matchedCueTerm;
			if (selection.CueMatched || denseIds.Contains(selection.Module.Id))
			{
				candidates.Add(selection);
			}
		}
		return candidates
			.OrderByDescending(item => item.CueMatched)
			.ThenByDescending(item => item.RecallScore)
			.ThenBy(item => item.Module.Order)
			.ThenBy(item => item.Module.Id, StringComparer.Ordinal)
			.Take(CandidateHardMaximum)
			.ToArray();
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> SelectDetails(
		IReadOnlyList<PolicyEffectModuleSelection> candidates,
		int requestedDetailLimit)
	{
		EnsureSelectionsAvailable(candidates, nameof(candidates));
		int effectiveDetailLimit = NormalizeDetailLimit(requestedDetailLimit);
		bool hasMergedOrder = candidates.All(item => item.SelectionRank >= 0);
		IEnumerable<PolicyEffectModuleSelection> ordered = hasMergedOrder
			? candidates.OrderBy(item => item.SelectionRank)
			: candidates
				.OrderByDescending(item => item.CueMatched)
				.ThenByDescending(item => item.RecallScore)
				.ThenBy(item => item.Module.Order)
				.ThenBy(item => item.Module.Id, StringComparer.Ordinal);
		return ordered.Take(Math.Min(effectiveDetailLimit, candidates.Count)).ToArray();
	}

	internal static PolicyEffectModuleRoutingResult RouteAfterAssessment(
		string policyName,
		string policyContent,
		string impactSummary,
		string numericIntent,
		PolicyEffectRetrievalContext context,
		IEnumerable<string> frozenEnabledModuleIds,
		int requestedDetailLimit,
		PolicyTextEmbeddingSession embeddingSession)
	{
		if (embeddingSession == null)
		{
			throw new ArgumentNullException(nameof(embeddingSession));
		}
		HashSet<string> enabledIds = new HashSet<string>(
			(frozenEnabledModuleIds ?? Array.Empty<string>())
				.Select(value => (value ?? string.Empty).Trim())
				.Where(value => value.Length > 0),
			StringComparer.Ordinal);
		IReadOnlyList<IPolicyEffectModule> enabledModules = PolicyEffectModuleCatalog.Modules
			.Where(module => module?.Descriptor?.PromptVisible == true
				&& enabledIds.Contains(module.Id)
				&& PolicyEffectModuleRetrievalSettings.IsContextSupported(module, context))
			.OrderBy(module => module.Order)
			.ThenBy(module => module.Id, StringComparer.Ordinal)
			.ToArray();
		int effectiveDetailLimit = NormalizeDetailLimit(requestedDetailLimit);
		if (enabledModules.Count == 0)
		{
			return new PolicyEffectModuleRoutingResult
			{
				Recalled = Array.Empty<PolicyEffectModuleSelection>(),
				Candidates = Array.Empty<PolicyEffectModuleSelection>(),
				Details = Array.Empty<PolicyEffectModuleSelection>(),
				RequestedDetailLimit = requestedDetailLimit,
				EffectiveDetailLimit = effectiveDetailLimit,
				DetailLimitClamped = effectiveDetailLimit != requestedDetailLimit,
				EnabledModuleCount = 0
			};
		}

		IReadOnlyList<string> queries = BuildPostAssessmentQueries(
			policyName,
			policyContent,
			impactSummary,
			numericIntent,
			enabledModules);
		List<PolicyEffectModuleIntentRecall> recalls = new List<PolicyEffectModuleIntentRecall>(queries.Count);
		for (int index = 0; index < queries.Count; index++)
		{
			recalls.Add(RecallIntent(
				embeddingSession.GetEmbedding(queries[index]),
				queries[index],
				enabledModules,
				index,
				index == 0));
		}
		string authoritative = ((policyName ?? string.Empty).Trim() + "\n" + (policyContent ?? string.Empty).Trim()).Trim();
		IReadOnlyList<PolicyEffectModuleSelection> candidates = MergeCandidates(
			recalls,
			enabledModules,
			authoritative,
			out int cueMatchCount,
			out bool candidatePoolBounded);
		return new PolicyEffectModuleRoutingResult
		{
			Recalled = candidates,
			Candidates = candidates,
			Details = candidates.Count == 0
				? Array.Empty<PolicyEffectModuleSelection>()
				: SelectDetails(candidates, requestedDetailLimit),
			RequestedDetailLimit = requestedDetailLimit,
			EffectiveDetailLimit = effectiveDetailLimit,
			DetailLimitClamped = effectiveDetailLimit != requestedDetailLimit,
			IntentCount = recalls.Count,
			AdditionalQueryEmbeddingCount = Math.Max(0, recalls.Count - 1),
			CueMatchCount = cueMatchCount,
			CandidateLimitTruncated = candidatePoolBounded,
			EnabledModuleCount = enabledModules.Count,
			IntentTopModuleIds = recalls.Select(recall => string.Join(",", recall.Ranked
				.Take(SemanticTopPerQuery)
				.Select(selection => selection.Module.Id))).ToArray()
		};
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		string scope)
	{
		int requestedDetailLimit = DuelSettings.GetPlayerPolicyEffectModuleDetailCountForExternal();
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleCatalog.GetModulesForScope(scope),
			requestedDetailLimit,
			GetQueryEmbedding);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		string scope,
		Func<string, float[]> additionalQueryEmbeddingProvider)
	{
		int requestedDetailLimit = DuelSettings.GetPlayerPolicyEffectModuleDetailCountForExternal();
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleCatalog.GetModulesForScope(scope),
			requestedDetailLimit,
			additionalQueryEmbeddingProvider);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		string scope,
		int requestedDetailLimit)
	{
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleCatalog.GetModulesForScope(scope),
			requestedDetailLimit,
			GetQueryEmbedding);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		string scope,
		int requestedDetailLimit,
		Func<string, float[]> additionalQueryEmbeddingProvider)
	{
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleCatalog.GetModulesForScope(scope),
			requestedDetailLimit,
			additionalQueryEmbeddingProvider);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		PolicyEffectRetrievalContext context)
	{
		int requestedDetailLimit = DuelSettings.GetPlayerPolicyEffectModuleDetailCountForExternal();
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleRetrievalSettings.GetEnabledModules(context),
			requestedDetailLimit,
			GetQueryEmbedding);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		PolicyEffectRetrievalContext context,
		Func<string, float[]> additionalQueryEmbeddingProvider)
	{
		int requestedDetailLimit = DuelSettings.GetPlayerPolicyEffectModuleDetailCountForExternal();
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleRetrievalSettings.GetEnabledModules(context),
			requestedDetailLimit,
			additionalQueryEmbeddingProvider);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		PolicyEffectRetrievalContext context,
		int requestedDetailLimit)
	{
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleRetrievalSettings.GetEnabledModules(context),
			requestedDetailLimit,
			GetQueryEmbedding);
	}

	internal static PolicyEffectModuleRoutingResult Route(
		float[] queryVector,
		string query,
		PolicyEffectRetrievalContext context,
		int requestedDetailLimit,
		Func<string, float[]> additionalQueryEmbeddingProvider)
	{
		return RouteWithModules(
			queryVector,
			query,
			PolicyEffectModuleRetrievalSettings.GetEnabledModules(context),
			requestedDetailLimit,
			additionalQueryEmbeddingProvider);
	}

	private static PolicyEffectModuleRoutingResult RouteWithModules(
		float[] queryVector,
		string query,
		IReadOnlyList<IPolicyEffectModule> scopedModules,
		int requestedDetailLimit,
		Func<string, float[]> additionalQueryEmbeddingProvider)
	{
		if (queryVector == null || queryVector.Length <= 0)
		{
			throw new ArgumentException("政策效果模块 query embedding 无效。", nameof(queryVector));
		}
		if (additionalQueryEmbeddingProvider == null)
		{
			throw new ArgumentNullException(nameof(additionalQueryEmbeddingProvider));
		}
		IReadOnlyList<IPolicyEffectModule> enabledModules = scopedModules ?? Array.Empty<IPolicyEffectModule>();
		if (enabledModules.Count == 0)
		{
			throw new InvalidOperationException("当前政策检索渠道没有启用的效果模块。");
		}
		IReadOnlyList<string> intentTexts = BuildQueryIntents(query, enabledModules);
		List<PolicyEffectModuleIntentRecall> intentRecalls = new List<PolicyEffectModuleIntentRecall>(intentTexts.Count)
		{
			RecallIntent(queryVector, intentTexts[0], enabledModules, 0, isPrimary: true)
		};
		for (int intentOrdinal = 1; intentOrdinal < intentTexts.Count; intentOrdinal++)
		{
			float[] intentVector = additionalQueryEmbeddingProvider(intentTexts[intentOrdinal]);
			if (intentVector == null || intentVector.Length == 0)
			{
				throw new InvalidOperationException("政策效果模块子意图 embedding 为空：intent="
					+ intentOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
			intentRecalls.Add(RecallIntent(
				intentVector,
				intentTexts[intentOrdinal],
				enabledModules,
				intentOrdinal,
				isPrimary: false));
		}
		Dictionary<string, PolicyEffectModuleSelection> aggregate = new Dictionary<string, PolicyEffectModuleSelection>(StringComparer.Ordinal);
		foreach (PolicyEffectModuleIntentRecall intent in intentRecalls)
		{
			foreach (PolicyEffectModuleSelection selection in intent.Ranked)
			{
				if (!aggregate.TryGetValue(selection.Module.Id, out PolicyEffectModuleSelection existing)
					|| selection.RecallScore > existing.RecallScore
					|| (selection.RecallScore.Equals(existing.RecallScore)
						&& (selection.BestIntentOrdinal < existing.BestIntentOrdinal
							|| (selection.BestIntentOrdinal == existing.BestIntentOrdinal
								&& selection.BestIntentRank < existing.BestIntentRank))))
				{
					aggregate[selection.Module.Id] = new PolicyEffectModuleSelection
					{
						Module = selection.Module,
						RecallScore = selection.RecallScore,
						BestIntentOrdinal = selection.BestIntentOrdinal,
						BestIntentRank = selection.BestIntentRank
					};
				}
			}
		}
		IReadOnlyList<PolicyEffectModuleSelection> recalled = aggregate.Values
			.OrderByDescending(item => item.RecallScore)
			.ThenBy(item => item.BestIntentOrdinal)
			.ThenBy(item => item.BestIntentRank)
			.ThenBy(item => item.Module.Order)
			.ThenBy(item => item.Module.Id, StringComparer.Ordinal)
			.Take(RecallLimit)
			.ToArray();
		IReadOnlyList<PolicyEffectModuleSelection> candidates = MergeCandidates(
			intentRecalls,
			enabledModules,
			query,
			out int cueMatchCount,
			out bool candidateLimitTruncated);
		int effectiveDetailLimit = NormalizeDetailLimit(requestedDetailLimit);
		return new PolicyEffectModuleRoutingResult
		{
			Recalled = recalled,
			Candidates = candidates,
			Details = SelectDetails(candidates, requestedDetailLimit),
			RequestedDetailLimit = requestedDetailLimit,
			EffectiveDetailLimit = effectiveDetailLimit,
			DetailLimitClamped = effectiveDetailLimit != requestedDetailLimit,
			IntentCount = intentRecalls.Count,
			AdditionalQueryEmbeddingCount = Math.Max(0, intentRecalls.Count - 1),
			CueMatchCount = cueMatchCount,
			CandidateLimitTruncated = candidateLimitTruncated,
			IntentTopModuleIds = intentRecalls
				.Select(intent => intent.Ranked.FirstOrDefault()?.Module?.Id ?? string.Empty)
				.ToArray()
		};
	}

	internal static IReadOnlyList<PolicyEffectModuleSelection> Select(string query, string scope)
	{
		string normalizedQuery = (query ?? string.Empty).Trim();
		float[] queryVector = GetQueryEmbedding(normalizedQuery);
		return Route(queryVector, normalizedQuery, scope).Details;
	}

	private static int NormalizeDetailLimit(int requestedDetailLimit)
	{
		return Math.Max(
			DuelSettings.PlayerPolicyEffectModuleDetailCountMinimum,
			Math.Min(DetailHardMaximum, requestedDetailLimit));
	}

	internal static int NormalizeDetailLimitForContractTests(int requestedDetailLimit)
	{
		return NormalizeDetailLimit(requestedDetailLimit);
	}

	private static void EnsureSelectionsAvailable(
		IReadOnlyList<PolicyEffectModuleSelection> selections,
		string parameterName)
	{
		if (selections == null
			|| selections.Count <= 0
			|| selections.Any(item => item?.Module == null || string.IsNullOrWhiteSpace(item.Module.Id)))
		{
			throw new ArgumentException("政策效果模块选择结果为空或无效。", parameterName);
		}
	}

	private static string FindMatchedCueTerm(string normalizedQuery, IReadOnlyCollection<string> cueTerms)
	{
		if (normalizedQuery.Length <= 0 || cueTerms == null || cueTerms.Count <= 0)
		{
			return string.Empty;
		}
		string bestMatch = string.Empty;
		foreach (string cueTerm in cueTerms)
		{
			string normalizedCueTerm = NormalizeCueText(cueTerm);
			if (normalizedCueTerm.Length <= 0
				|| normalizedQuery.IndexOf(normalizedCueTerm, StringComparison.Ordinal) < 0)
			{
				continue;
			}
			if (normalizedCueTerm.Length > bestMatch.Length
				|| (normalizedCueTerm.Length == bestMatch.Length
					&& string.CompareOrdinal(normalizedCueTerm, bestMatch) < 0))
			{
				bestMatch = normalizedCueTerm;
			}
		}
		return bestMatch;
	}

	private static string NormalizeCueText(string value)
	{
		return (value ?? string.Empty).Trim().ToLowerInvariant();
	}

	private static string LimitText(string value, int maximumCharacters)
	{
		string text = (value ?? string.Empty).Trim();
		int limit = Math.Max(1, maximumCharacters);
		return text.Length <= limit ? text : text.Substring(0, limit);
	}

	private static IReadOnlyDictionary<string, IndexedModule> BuildIndex()
	{
		OnnxEmbeddingEngine embedding = OnnxEmbeddingEngine.Instance;
		if (embedding == null || !embedding.IsAvailable)
		{
			throw new InvalidOperationException("政策效果模块 ONNX embedding 不可用：" + (embedding?.LastError ?? "unknown"));
		}
		Dictionary<string, IndexedModule> result = new Dictionary<string, IndexedModule>(StringComparer.Ordinal);
		int embeddingDimension = 0;
		foreach (IPolicyEffectModule module in PolicyEffectModuleCatalog.Modules)
		{
			if (!embedding.TryGetEmbedding(module.RetrievalText, out float[] vector) || vector == null || vector.Length <= 0)
			{
				throw new InvalidOperationException("政策效果模块向量构建失败：" + module.Id + "：" + (embedding.LastError ?? "unknown"));
			}
			if (embeddingDimension == 0)
			{
				embeddingDimension = vector.Length;
			}
			if (!IsUsableEmbedding(vector, embeddingDimension))
			{
				throw new InvalidOperationException("政策效果模块向量维度或数值无效：" + module.Id);
			}
			result.Add(module.Id, new IndexedModule { Module = module, Vector = vector });
		}
		return result;
	}

	private static IReadOnlyList<IndexedMechanismSeed> BuildMechanismIndex()
	{
		OnnxEmbeddingEngine embedding = OnnxEmbeddingEngine.Instance;
		if (embedding == null || !embedding.IsAvailable)
		{
			throw new InvalidOperationException("政策效果机制 ONNX embedding 不可用：" + (embedding?.LastError ?? "unknown"));
		}
		Dictionary<PolicyEffectMechanismShape, string[]> seeds = new Dictionary<PolicyEffectMechanismShape, string[]>
		{
			[PolicyEffectMechanismShape.DirectChange] = new[]
			{
				"直接提高目标地区的繁荣与治安", "降低指定城堡的民兵", "让这些城镇获得每日忠诚变化", "只改变目标本身的指标"
			},
			[PolicyEffectMechanismShape.CrossTargetFlow] = new[]
			{
				"从一批地区调拨资源到另一批地区", "把敌国粮食转运到本国", "从其他地方转移税收到这里", "跨目标夺取并输送资源"
			},
			[PolicyEffectMechanismShape.IssuerAcquires] = new[]
			{
				"发布者从其他地区获得税收", "让我国从外国获取资源", "地方发布者得到粮食和财政收入", "把外地收益归于发布方"
			},
			[PolicyEffectMechanismShape.IssuerProvides] = new[]
			{
				"发布者向目标地区提供援助", "我国出资支援外国城镇", "地方拿出粮食救济其他地区", "由发布方向受益方输送资源"
			},
			[PolicyEffectMechanismShape.Exchange] = new[]
			{
				"以财政成本换取建设收益", "付出粮食换取稳定与忠诚", "一项成本对应另一项收益", "交换资源并获得不同机制的回报"
			}
		};
		List<IndexedMechanismSeed> result = new List<IndexedMechanismSeed>();
		foreach (KeyValuePair<PolicyEffectMechanismShape, string[]> pair in seeds)
		{
			foreach (string seed in pair.Value)
			{
				if (!embedding.TryGetEmbedding(seed, out float[] vector) || vector == null || vector.Length == 0)
				{
					throw new InvalidOperationException("政策效果机制向量构建失败：" + pair.Key + " / " + (embedding.LastError ?? "unknown"));
				}
				result.Add(new IndexedMechanismSeed { Shape = pair.Key, Vector = vector });
			}
		}
		return result;
	}

	private static float Cosine(float[] left, float[] right)
	{
		if (left == null || right == null || left.Length <= 0 || left.Length != right.Length)
		{
			return float.NegativeInfinity;
		}
		double dot = 0d;
		double leftNorm = 0d;
		double rightNorm = 0d;
		for (int index = 0; index < left.Length; index++)
		{
			dot += (double)left[index] * right[index];
			leftNorm += (double)left[index] * left[index];
			rightNorm += (double)right[index] * right[index];
		}
		if (leftNorm <= 0d || rightNorm <= 0d)
		{
			return float.NegativeInfinity;
		}
		return (float)(dot / Math.Sqrt(leftNorm * rightNorm));
	}

	private static bool IsUsableEmbedding(float[] vector, int expectedDimension)
	{
		if (vector == null
			|| vector.Length <= 0
			|| expectedDimension <= 0
			|| vector.Length != expectedDimension)
		{
			return false;
		}
		bool hasNonZeroValue = false;
		foreach (float value in vector)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				return false;
			}
			hasNonZeroValue |= value != 0f;
		}
		return hasNonZeroValue;
	}
}
