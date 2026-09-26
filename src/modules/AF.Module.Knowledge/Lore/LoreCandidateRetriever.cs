using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoreRule = AnimusForge.KnowledgeLibraryBehavior.LoreRule;

namespace AnimusForge;

internal sealed class WeightedKnowledgeInput
{
	public string Text;
	public float Weight = 1f;
}

internal sealed class LoreCandidateRules
{
	public string MatchMode = "none";
	public List<LoreRule> OrderedRules = new List<LoreRule>();
	public int InjectLimit = 2;
	public int RecallPerEntity;
	public int RerankPerEntity;
	public int EntityQueryCount = 1;
}

/// <summary>
/// Pure lore candidate retrieval over a <see cref="KnowledgeRuleIndex"/>: mention terms → weighted entity queries →
/// per-entity ranked recall → round-robin allocation under the inject limit. No game objects; the host supplies the
/// retrieval-enabled flag and the mentioned entity list captured on the game thread.
/// </summary>
internal sealed class LoreCandidateRetriever
{
	internal const int MentionTermHardCap = 32;
	internal const int MentionQueryMaxChars = 80;

	private readonly KnowledgeRuleIndex _index;
	private readonly Action<string, string> _log;

	internal LoreCandidateRetriever(KnowledgeRuleIndex index, Action<string, string> log)
	{
		_index = index ?? throw new ArgumentNullException(nameof(index));
		_log = log ?? ((c, m) => { });
	}

	private void Log(string channel, string message)
	{
		try
		{
			_log(channel, message);
		}
		catch
		{
		}
	}

	/// <summary>Collapses runs of whitespace (incl. newlines) to one space and trims.</summary>
	internal static string NormalizeKeywordForCompare(string keyword)
	{
		try
		{
			string text = (keyword ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			foreach (char c in text)
			{
				if (char.IsWhiteSpace(c))
				{
					if (!flag)
					{
						stringBuilder.Append(' ');
					}
					flag = true;
				}
				else
				{
					stringBuilder.Append(c);
					flag = false;
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	/// <summary>Normalized, de-duplicated (case-insensitive), ≤80 chars, first 32 mention entities in order.</summary>
	internal static List<string> BuildMentionTerms(IEnumerable<string> mentionedEntities)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (mentionedEntities != null)
			{
				foreach (string value in mentionedEntities)
				{
					if (result.Count >= MentionTermHardCap)
					{
						break;
					}
					string text = NormalizeKeywordForCompare(value);
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					if (text.Length > 80)
					{
						text = text.Substring(0, 80).Trim();
					}
					if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
					{
						result.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	internal static int CountMentionTerms(IEnumerable<string> mentionedEntities)
	{
		return BuildMentionTerms(mentionedEntities).Count;
	}

	/// <summary>"mentions=&lt;hash8&gt;:&lt;count&gt;:cap&lt;injectLimit&gt;" or "mentions=empty".</summary>
	internal static string BuildMentionSignature(IEnumerable<string> mentionedEntities, int injectLimit)
	{
		try
		{
			List<string> terms = BuildMentionTerms(mentionedEntities);
			if (terms.Count <= 0)
			{
				return "mentions=empty";
			}
			string joined = string.Join("|", terms.Select((string x) => (x ?? "").Trim().ToLowerInvariant()));
			return "mentions=" + KnowledgeRuleIndex.Hash8(joined) + ":" + terms.Count + ":cap" + injectLimit;
		}
		catch
		{
			return "mentions=error";
		}
	}

	internal static string FormatMentionCounts(IReadOnlyCollection<string> mentionedEntities)
	{
		if (mentionedEntities == null)
		{
			return "entities=0";
		}
		return "entities=" + mentionedEntities.Count;
	}

	/// <summary>Up to min(12, maxQueryCount) weight-1 queries from the mention terms (each ≤80 chars, unique).</summary>
	internal static List<WeightedKnowledgeInput> BuildQueryInputsFromMentions(IEnumerable<string> mentionedEntities, int maxQueryCount, out int mentionTermCount)
	{
		List<WeightedKnowledgeInput> list = new List<WeightedKnowledgeInput>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		mentionTermCount = 0;
		try
		{
			List<string> terms = BuildMentionTerms(mentionedEntities);
			mentionTermCount = terms.Count;
			if (terms.Count <= 0)
			{
				return list;
			}
			int queryLimit = Math.Max(1, Math.Min(12, maxQueryCount));
			for (int i = 0; i < terms.Count; i++)
			{
				if (list.Count >= queryLimit)
				{
					break;
				}
				string term = (terms[i] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(term))
				{
					continue;
				}
				if (term.Length > MentionQueryMaxChars)
				{
					term = term.Substring(0, MentionQueryMaxChars).Trim();
				}
				if (string.IsNullOrWhiteSpace(term) || !hashSet.Add(term))
				{
					continue;
				}
				list.Add(new WeightedKnowledgeInput { Text = term, Weight = 1f });
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool TryAddFirstUniqueRankedEntityCandidate(List<LoreRule> result, HashSet<LoreRule> selectedRules, HashSet<string> selectedRuleIds, List<KnowledgeRuleScore> candidates, out int selectedRank)
	{
		selectedRank = -1;
		if (candidates == null)
		{
			return false;
		}
		for (int rank = 0; rank < candidates.Count; rank++)
		{
			if (TryAddRankedEntityCandidate(result, selectedRules, selectedRuleIds, candidates, rank))
			{
				selectedRank = rank;
				return true;
			}
		}
		return false;
	}

	private static bool TryAddRankedEntityCandidate(List<LoreRule> result, HashSet<LoreRule> selectedRules, HashSet<string> selectedRuleIds, List<KnowledgeRuleScore> candidates, int rank)
	{
		if (result == null || selectedRules == null || selectedRuleIds == null || candidates == null || rank < 0 || rank >= candidates.Count)
		{
			return false;
		}
		LoreRule rule = candidates[rank]?.Rule;
		if (rule == null || selectedRules.Contains(rule))
		{
			return false;
		}
		string ruleId = (rule.Id ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(ruleId) && selectedRuleIds.Contains(ruleId))
		{
			return false;
		}
		selectedRules.Add(rule);
		if (!string.IsNullOrWhiteSpace(ruleId))
		{
			selectedRuleIds.Add(ruleId);
		}
		result.Add(rule);
		return true;
	}

	/// <summary>
	/// One ranked candidate list per entity query; first pass takes each entity's best unused rule (falling back to lower
	/// ranks on collision), second pass (only when the limit exceeds the entity count) sweeps rank 1, 2, … across entities.
	/// </summary>
	internal List<LoreRule> SelectVectorRulesPerEntity(List<WeightedKnowledgeInput> entityInputs, int totalEntityCount, int recallTopK, int rerankTopK, int injectLimit, out string matchMode)
	{
		List<LoreRule> result = new List<LoreRule>();
		matchMode = "none";
		try
		{
			List<WeightedKnowledgeInput> list = (entityInputs ?? new List<WeightedKnowledgeInput>()).Where((WeightedKnowledgeInput x) => x != null && !string.IsNullOrWhiteSpace(x.Text) && x.Weight > 0f).ToList();
			if (list.Count <= 0)
			{
				return result;
			}
			bool flag = false;
			try
			{
				flag = _index.RerankerAvailable;
			}
			catch
			{
				flag = false;
			}
			matchMode = flag ? "rerank_per_entity" : "semantic_per_entity";
			List<List<KnowledgeRuleScore>> rankedCandidates = new List<List<KnowledgeRuleScore>>(list.Count);
			for (int num = 0; num < list.Count; num++)
			{
				WeightedKnowledgeInput weightedKnowledgeInput = list[num];
				List<KnowledgeRuleScore> list3 = _index.FindRankedVectorCandidateScores(weightedKnowledgeInput.Text, recallTopK, rerankTopK, weightedKnowledgeInput.Weight);
				if (list3 == null || list3.Count <= 0)
				{
					rankedCandidates.Add(new List<KnowledgeRuleScore>());
					Log("LoreMatch", $"entity_query priority={num + 1} noun={QuoteJson(weightedKnowledgeInput.Text)} candidates=0");
					continue;
				}
				rankedCandidates.Add(list3.Where((KnowledgeRuleScore x) => x?.Rule != null).ToList());
				KnowledgeRuleScore best = rankedCandidates[num].FirstOrDefault();
				Log("LoreMatch", $"entity_query priority={num + 1} noun={QuoteJson(weightedKnowledgeInput.Text)} candidates={rankedCandidates[num].Count} best={(best?.Rule?.Id ?? "(none)")} score={(best?.RawScore ?? 0f):0.000}");
			}
			int limit = KnowledgeRuleIndex.GetLoreInjectLimit(injectLimit);
			HashSet<LoreRule> selectedRules = new HashSet<LoreRule>();
			HashSet<string> selectedRuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int primarySelected = 0;
			int primaryCollisionFallbacks = 0;
			List<string> allocationDetails = new List<string>(rankedCandidates.Count);
			for (int entityIndex = 0; entityIndex < rankedCandidates.Count && result.Count < limit; entityIndex++)
			{
				if (TryAddFirstUniqueRankedEntityCandidate(result, selectedRules, selectedRuleIds, rankedCandidates[entityIndex], out var selectedRank))
				{
					primarySelected++;
					if (selectedRank > 0)
					{
						primaryCollisionFallbacks++;
					}
					string selectedRuleId = result.LastOrDefault()?.Id ?? "(none)";
					allocationDetails.Add($"{entityIndex + 1}:{list[entityIndex].Text}->{selectedRuleId}@{selectedRank + 1}");
				}
				else
				{
					allocationDetails.Add($"{entityIndex + 1}:{list[entityIndex].Text}->(none)");
				}
			}
			bool allowLowerRanks = limit > Math.Max(0, totalEntityCount);
			int secondarySelected = 0;
			if (allowLowerRanks && result.Count < limit)
			{
				int maxRankCount = rankedCandidates.Count <= 0 ? 0 : rankedCandidates.Max((List<KnowledgeRuleScore> x) => x?.Count ?? 0);
				for (int rank = 1; rank < maxRankCount && result.Count < limit; rank++)
				{
					for (int entityIndex = 0; entityIndex < rankedCandidates.Count && result.Count < limit; entityIndex++)
					{
						if (TryAddRankedEntityCandidate(result, selectedRules, selectedRuleIds, rankedCandidates[entityIndex], rank))
						{
							secondarySelected++;
						}
					}
				}
			}
			string allocationSummary = $"entity_allocation nounsTotal={totalEntityCount} nounsQueried={rankedCandidates.Count} injectLimit={limit} primary={primarySelected} collisionFallbacks={primaryCollisionFallbacks} secondary={secondarySelected} allowLowerRanks={allowLowerRanks} selected={result.Count} assignments={string.Join("|", allocationDetails)}";
			Log("LoreMatch", allocationSummary);
			Log("KnowledgeRetrieval", allocationSummary);
		}
		catch
		{
		}
		return result;
	}

	/// <summary>Full candidate pool for a mention list; empty when retrieval is disabled or nothing is mentioned.</summary>
	internal LoreCandidateRules CollectCandidateRules(IReadOnlyCollection<string> mentionedEntities, bool retrievalEnabled)
	{
		LoreCandidateRules result = new LoreCandidateRules();
		try
		{
			if (!retrievalEnabled)
			{
				return result;
			}
			int knowledgeReturnCap = _index.GetKnowledgeReturnCap();
			int loreInjectLimit = KnowledgeRuleIndex.GetLoreInjectLimit(knowledgeReturnCap);
			List<WeightedKnowledgeInput> entityQueries = BuildQueryInputsFromMentions(mentionedEntities, loreInjectLimit, out var mentionTermCount);
			if (entityQueries.Count <= 0)
			{
				Log("LoreMatch", "knowledge_mentions skip reason=no_mentions " + FormatMentionCounts(mentionedEntities));
				return result;
			}
			Log("LoreMatch", $"knowledge_mentions terms={mentionTermCount} entityQueries={entityQueries.Count} returnCap={loreInjectLimit} {FormatMentionCounts(mentionedEntities)} signature={BuildMentionSignature(mentionedEntities, loreInjectLimit)}");
			int rerankBudget = KnowledgeRuleIndex.GetKnowledgeRerankBudget(knowledgeReturnCap);
			int num = Math.Max(1, entityQueries.Count);
			int knowledgePerEntityRerank = KnowledgeRuleIndex.GetKnowledgePerEntityRerank(rerankBudget, num);
			int knowledgePerEntityRecall = KnowledgeRuleIndex.GetKnowledgePerEntityRecall(knowledgePerEntityRerank);
			result.EntityQueryCount = num;
			result.RerankPerEntity = knowledgePerEntityRerank;
			result.RecallPerEntity = knowledgePerEntityRecall;
			result.InjectLimit = loreInjectLimit;
			List<LoreRule> list4 = SelectVectorRulesPerEntity(entityQueries, mentionTermCount, knowledgePerEntityRecall, knowledgePerEntityRerank, loreInjectLimit, out var matchMode);
			if (list4 != null && list4.Count > 0)
			{
				result.MatchMode = "mentions_" + matchMode;
				result.OrderedRules = list4.Where((LoreRule x) => x != null).ToList();
				Log("LoreMatch", $"candidate_pool mode={result.MatchMode} returnCap={loreInjectLimit} rerankBudget={rerankBudget} rerankPerEntity={knowledgePerEntityRerank} recallPerEntity={knowledgePerEntityRecall} mentionTerms={mentionTermCount} entityQueries={num} got={result.OrderedRules.Count}");
				return result;
			}
			return result;
		}
		catch
		{
		}
		return result;
	}

	/// <summary>JSON string literal (same escaping as JsonConvert.ToString for the characters that matter in logs).</summary>
	internal static string QuoteJson(string value)
	{
		string text = value ?? "";
		StringBuilder sb = new StringBuilder(text.Length + 2);
		sb.Append('"');
		foreach (char c in text)
		{
			switch (c)
			{
				case '"': sb.Append("\\\""); break;
				case '\\': sb.Append("\\\\"); break;
				case '\n': sb.Append("\\n"); break;
				case '\r': sb.Append("\\r"); break;
				case '\t': sb.Append("\\t"); break;
				case '\b': sb.Append("\\b"); break;
				case '\f': sb.Append("\\f"); break;
				default:
					if (c < ' ')
					{
						sb.Append("\\u").Append(((int)c).ToString("x4"));
					}
					else
					{
						sb.Append(c);
					}
					break;
			}
		}
		sb.Append('"');
		return sb.ToString();
	}
}
