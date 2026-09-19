using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LoreRule = AnimusForge.KnowledgeLibraryBehavior.LoreRule;

namespace AnimusForge;

/// <summary>Ports the index needs from the host: embedding/reranker engines, thresholds and logging.</summary>
internal interface IKnowledgeIndexPorts
{
	bool EmbeddingAvailable { get; }
	bool TryGetEmbedding(string text, out float[] vector);
	bool RerankerAvailable { get; }
	bool TryScoreBatch(string query, IReadOnlyList<string> documents, out List<float> scores);
	/// <summary>Configured semantic top-K (clamped to 1..12 by the index).</summary>
	int SemanticTopK { get; }
	/// <summary>Minimum raw score for strict acceptance (legacy fallback 0.21).</summary>
	float SemanticMinScore { get; }
	void Log(string channel, string message);
}

internal sealed class KnowledgeRuleScore
{
	public LoreRule Rule;
	public float RawScore;
	public float EvidenceScore;
	public float RerankScore;
}

/// <summary>
/// Owner of the rule retrieval indexes (sparse TF-IDF + ONNX embeddings), the ranked candidate cache and
/// the recall → collapse → select → rerank query chain. Versioned: <see cref="Touch"/> bumps the data version,
/// drops every index/cache, and queries against a stale index return nothing until <see cref="EnsureVectorIndex"/> /
/// <see cref="EnsureOnnxIndex"/> are called again (the host only rebuilds on save load — legacy behavior).
/// Rule storage, lore composition and Hero facts stay with the host.
/// </summary>
internal sealed class KnowledgeRuleIndex
{
	private sealed class VectorDoc { public LoreRule Rule; public Dictionary<string, int> Tf; public bool IsEvidence; }
	private sealed class VectorRuleEntry { public LoreRule Rule; public string Seed; public Dictionary<string, float> Weights; public float Norm; public bool IsEvidence; }
	private sealed class OnnxRuleEntry { public LoreRule Rule; public string Seed; public float[] Vector; public bool IsEvidence; }
	private sealed class RankedCacheItem { public long Version; public long Ticks; public List<KnowledgeRuleScore> Scores; }

	internal const int RankedCandidateCacheMax = 512;
	internal const int SemanticResultHardCapMax = 20;

	private readonly IKnowledgeIndexPorts _ports;
	private readonly Func<IReadOnlyList<LoreRule>> _rules;

	private long _ruleDataVersion = 1L;
	private readonly object _vectorIndexLock = new object();
	private List<VectorRuleEntry> _vectorRuleEntries;
	private Dictionary<string, float> _vectorIdf;
	private long _vectorIndexVersion = -1L;
	private readonly object _onnxIndexLock = new object();
	private List<OnnxRuleEntry> _onnxRuleEntries;
	private long _onnxIndexVersion = -1L;
	private readonly object _rankedCacheLock = new object();
	private Dictionary<string, RankedCacheItem> _rankedCache = new Dictionary<string, RankedCacheItem>();

	/// <param name="rules">Live view of the host's rule list (read on demand during index build).</param>
	internal KnowledgeRuleIndex(IKnowledgeIndexPorts ports, Func<IReadOnlyList<LoreRule>> rules)
	{
		_ports = ports ?? throw new ArgumentNullException(nameof(ports));
		_rules = rules ?? (() => null);
	}

	internal long Version => _ruleDataVersion;
	internal bool RerankerAvailable => _ports.RerankerAvailable;
	internal bool SparseReady => _vectorRuleEntries != null && _vectorIndexVersion == _ruleDataVersion;
	internal int SparseEntryCount => _vectorRuleEntries?.Count ?? 0;
	internal bool OnnxReady => _onnxRuleEntries != null && _onnxIndexVersion == _ruleDataVersion;
	internal int OnnxEntryCount => _onnxRuleEntries?.Count ?? 0;

	/// <summary>Rule data changed: bump version (wrapping to 1), drop ranked cache and both indexes.</summary>
	internal void Touch()
	{
		_ruleDataVersion++;
		if (_ruleDataVersion <= 0)
		{
			_ruleDataVersion = 1L;
		}
		try
		{
			lock (_rankedCacheLock)
			{
				_rankedCache.Clear();
			}
		}
		catch
		{
		}
		try
		{
			lock (_vectorIndexLock)
			{
				_vectorRuleEntries = null;
				_vectorIdf = null;
				_vectorIndexVersion = -1L;
			}
		}
		catch
		{
		}
		try
		{
			lock (_onnxIndexLock)
			{
				_onnxRuleEntries = null;
				_onnxIndexVersion = -1L;
			}
		}
		catch
		{
		}
	}

	// ---------------------------------------------------------------- budgets

	internal int GetKnowledgeReturnCap()
	{
		try
		{
			int num = _ports.SemanticTopK;
			if (num < 1)
			{
				num = 1;
			}
			if (num > 12)
			{
				num = 12;
			}
			return num;
		}
		catch
		{
			return 4;
		}
	}

	internal static int GetKnowledgeRerankBudget(int returnCap)
	{
		int num = Math.Max(1, returnCap) * 3;
		if (num < 8)
		{
			num = 8;
		}
		if (num > 36)
		{
			num = 36;
		}
		return num;
	}

	internal static int GetKnowledgePerEntityRerank(int rerankBudget, int entityCount)
	{
		int num = ((entityCount > 0) ? entityCount : 1);
		int num2 = (int)Math.Round((double)rerankBudget / (double)num, MidpointRounding.AwayFromZero);
		if (num2 < 4)
		{
			num2 = 4;
		}
		if (num2 > 12)
		{
			num2 = 12;
		}
		return num2;
	}

	internal static int GetKnowledgePerEntityRecall(int rerankPerEntity)
	{
		int num = (int)Math.Round((double)rerankPerEntity * 2.5, MidpointRounding.AwayFromZero);
		if (num < 10)
		{
			num = 10;
		}
		if (num > 30)
		{
			num = 30;
		}
		return num;
	}

	internal static int GetLoreInjectLimit(int returnCap)
	{
		if (returnCap < 1)
		{
			returnCap = 1;
		}
		if (returnCap > 12)
		{
			returnCap = 12;
		}
		return returnCap;
	}

	internal static int GetSemanticResultHardCap(int topK)
	{
		if (topK <= 0)
		{
			return SemanticResultHardCapMax;
		}
		return Math.Min(topK, SemanticResultHardCapMax);
	}

	// ---------------------------------------------------------------- text / tokens

	internal static string Hash8(string s)
	{
		uint num = 2166136261u;
		string text = s ?? "";
		for (int i = 0; i < text.Length; i++)
		{
			num ^= text[i];
			num *= 16777619;
		}
		return num.ToString("x8");
	}

	private static bool IsAsciiWordChar(char ch)
	{
		return ch < '\u0080' && (char.IsLetterOrDigit(ch) || ch == '_');
	}

	internal static bool IsCjkChar(char ch)
	{
		return (ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\u3400' && ch <= '\u4DBF');
	}

	private static void AppendCjkTokens(StringBuilder seq, List<string> tokens)
	{
		if (seq == null || seq.Length <= 0 || tokens == null)
		{
			return;
		}
		if (seq.Length == 1)
		{
			tokens.Add("c1:" + seq[0]);
			return;
		}
		for (int i = 0; i < seq.Length - 1; i++)
		{
			tokens.Add("c2:" + seq.ToString(i, 2));
		}
		for (int j = 0; j < seq.Length; j++)
		{
			tokens.Add("c1:" + seq[j]);
		}
	}

	/// <summary>Lower-cased mixed tokenizer: ASCII words (w:/w1:), CJK bigrams+unigrams (c2:/c1:), other letters (u:).</summary>
	internal static List<string> ExtractVectorTokens(string text)
	{
		List<string> list = new List<string>();
		try
		{
			string text2 = (text ?? "").ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text2))
			{
				return list;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int i = 0; i < text2.Length; i++)
			{
				char c = text2[i];
				if (IsAsciiWordChar(c))
				{
					stringBuilder.Append(c);
					if (stringBuilder2.Length > 0)
					{
						AppendCjkTokens(stringBuilder2, list);
						stringBuilder2.Clear();
					}
					continue;
				}
				if (stringBuilder.Length > 0)
				{
					if (stringBuilder.Length >= 2)
					{
						list.Add("w:" + stringBuilder.ToString());
					}
					else
					{
						list.Add("w1:" + stringBuilder[0]);
					}
					stringBuilder.Clear();
				}
				if (IsCjkChar(c))
				{
					stringBuilder2.Append(c);
					continue;
				}
				if (stringBuilder2.Length > 0)
				{
					AppendCjkTokens(stringBuilder2, list);
					stringBuilder2.Clear();
				}
				if (char.IsLetterOrDigit(c))
				{
					list.Add("u:" + c);
				}
			}
			if (stringBuilder.Length > 0)
			{
				if (stringBuilder.Length >= 2)
				{
					list.Add("w:" + stringBuilder.ToString());
				}
				else
				{
					list.Add("w1:" + stringBuilder[0]);
				}
			}
			if (stringBuilder2.Length > 0)
			{
				AppendCjkTokens(stringBuilder2, list);
			}
		}
		catch
		{
		}
		return list;
	}

	internal static Dictionary<string, int> CountTokens(List<string> tokens)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		try
		{
			if (tokens == null)
			{
				return dictionary;
			}
			for (int i = 0; i < tokens.Count; i++)
			{
				string text = (tokens[i] ?? "").Trim();
				if (!string.IsNullOrEmpty(text))
				{
					if (dictionary.TryGetValue(text, out var value))
					{
						dictionary[text] = value + 1;
					}
					else
					{
						dictionary[text] = 1;
					}
				}
			}
		}
		catch
		{
		}
		return dictionary;
	}

	internal static Dictionary<string, float> BuildVectorWeights(Dictionary<string, int> tf, Dictionary<string, float> idf, out float norm)
	{
		norm = 0f;
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.Ordinal);
		try
		{
			if (tf == null || tf.Count <= 0)
			{
				return dictionary;
			}
			double num = 0.0;
			foreach (KeyValuePair<string, int> item in tf)
			{
				string text = item.Key ?? "";
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				int value = item.Value;
				if (value > 0)
				{
					float num2 = 1f;
					if (idf != null && idf.TryGetValue(text, out var value2))
					{
						num2 = value2;
					}
					float num3 = 1f + (float)Math.Log(1.0 + (double)value);
					float num4 = (dictionary[text] = num3 * num2);
					num += (double)num4 * (double)num4;
				}
			}
			norm = ((num > 0.0) ? ((float)Math.Sqrt(num)) : 0f);
		}
		catch
		{
			norm = 0f;
		}
		return dictionary;
	}

	internal static float DotProduct(Dictionary<string, float> a, Dictionary<string, float> b)
	{
		try
		{
			if (a == null || b == null || a.Count <= 0 || b.Count <= 0)
			{
				return 0f;
			}
			if (a.Count > b.Count)
			{
				Dictionary<string, float> dictionary = a;
				a = b;
				b = dictionary;
			}
			double num = 0.0;
			foreach (KeyValuePair<string, float> item in a)
			{
				if (b.TryGetValue(item.Key, out var value))
				{
					num += (double)item.Value * (double)value;
				}
			}
			return (float)num;
		}
		catch
		{
			return 0f;
		}
	}

	internal static float DotProduct(float[] a, float[] b)
	{
		try
		{
			if (a == null || b == null || a.Length == 0 || b.Length == 0)
			{
				return 0f;
			}
			int num = ((a.Length < b.Length) ? a.Length : b.Length);
			double num2 = 0.0;
			for (int i = 0; i < num; i++)
			{
				num2 += (double)a[i] * (double)b[i];
			}
			return (float)num2;
		}
		catch
		{
			return 0f;
		}
	}

	internal static string BuildRuleSearchText(LoreRule rule)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (rule.Keywords != null)
			{
				for (int i = 0; i < rule.Keywords.Count; i++)
				{
					string value = (rule.Keywords[i] ?? "").Trim();
					if (!string.IsNullOrEmpty(value))
					{
						stringBuilder.Append(value).Append(' ');
					}
				}
			}
			if (rule.RagShortTexts != null && rule.RagShortTexts.Count > 0)
			{
				for (int j = 0; j < rule.RagShortTexts.Count; j++)
				{
					string value2 = (rule.RagShortTexts[j] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrEmpty(value2))
					{
						stringBuilder.Append(value2).Append(' ');
					}
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	internal static string BuildRuleRerankText(LoreRule rule)
	{
		try
		{
			string text = BuildRuleSearchText(rule);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			text = text.Replace("\r", " ").Replace("\n", " ").Trim();
			if (text.Length > 480)
			{
				text = text.Substring(0, 480);
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static void AddSemanticSeed(List<string> list, HashSet<string> seen, string raw, int maxLen = 260)
	{
		try
		{
			string text = (raw ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (text.Length > maxLen)
				{
					text = text.Substring(0, maxLen);
				}
				if (seen.Add(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>Topic seeds: keywords (≤120) and RAG short texts (≤220), de-duplicated case-insensitively.</summary>
	internal static List<string> GetRuleTopicSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Keywords != null)
			{
				for (int i = 0; i < rule.Keywords.Count; i++)
				{
					string text = (rule.Keywords[i] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						AddSemanticSeed(list, seen, text, 120);
					}
				}
			}
			if (rule.RagShortTexts != null)
			{
				for (int j = 0; j < rule.RagShortTexts.Count; j++)
				{
					string text2 = (rule.RagShortTexts[j] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						AddSemanticSeed(list, seen, text2, 220);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Evidence seeds: keywords, "关于"+keyword (≤160) and RAG short texts (≤180).</summary>
	internal static List<string> GetRuleEvidenceSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Keywords != null)
			{
				for (int i = 0; i < rule.Keywords.Count; i++)
				{
					string text = (rule.Keywords[i] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						AddSemanticSeed(list, seen, text, 120);
						AddSemanticSeed(list, seen, "关于" + text, 160);
					}
				}
			}
			if (rule.RagShortTexts != null)
			{
				for (int j = 0; j < rule.RagShortTexts.Count; j++)
				{
					string text2 = (rule.RagShortTexts[j] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						AddSemanticSeed(list, seen, text2, 180);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	// ---------------------------------------------------------------- index build

	internal void EnsureVectorIndex()
	{
		try
		{
			if (_vectorRuleEntries != null && _vectorIndexVersion == _ruleDataVersion)
			{
				return;
			}
			lock (_vectorIndexLock)
			{
				if (_vectorRuleEntries != null && _vectorIndexVersion == _ruleDataVersion)
				{
					return;
				}
				List<VectorDoc> docs = new List<VectorDoc>();
				Dictionary<string, int> df = new Dictionary<string, int>(StringComparer.Ordinal);
				IReadOnlyList<LoreRule> rules = _rules();
				if (rules != null)
				{
					foreach (LoreRule rule in rules)
					{
						LoreRule r = rule;
						if (r != null)
						{
							addSeeds(GetRuleTopicSeeds(r), isEvidence: false);
							addSeeds(GetRuleEvidenceSeeds(r), isEvidence: true);
						}
						void addSeeds(IEnumerable<string> seeds, bool isEvidence)
						{
							if (seeds == null)
							{
								return;
							}
							foreach (string seed2 in seeds)
							{
								string text = (seed2 ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text))
								{
									Dictionary<string, int> tf = CountTokens(ExtractVectorTokens(text));
									if (tf.Count > 0)
									{
										docs.Add(new VectorDoc { Rule = r, Tf = tf, IsEvidence = isEvidence });
										foreach (string item in new HashSet<string>(tf.Keys, StringComparer.Ordinal))
										{
											if (df.TryGetValue(item, out var value2))
											{
												df[item] = value2 + 1;
											}
											else
											{
												df[item] = 1;
											}
										}
									}
								}
							}
						}
					}
				}
				int count = docs.Count;
				Dictionary<string, float> idf = new Dictionary<string, float>(StringComparer.Ordinal);
				if (count > 0)
				{
					foreach (KeyValuePair<string, int> item2 in df)
					{
						idf[item2.Key] = 1f + (float)Math.Log(((double)count + 1.0) / ((double)item2.Value + 1.0));
					}
				}
				List<VectorRuleEntry> list = new List<VectorRuleEntry>();
				for (int i = 0; i < docs.Count; i++)
				{
					VectorDoc vectorDoc = docs[i];
					if (vectorDoc == null || vectorDoc.Rule == null || vectorDoc.Tf == null || vectorDoc.Tf.Count <= 0)
					{
						continue;
					}
					Dictionary<string, float> weights = BuildVectorWeights(vectorDoc.Tf, idf, out float norm);
					if (weights.Count <= 0 || norm <= 0f)
					{
						continue;
					}
					string seed = "";
					try
					{
						seed = string.Join(" ", from x in vectorDoc.Tf.OrderByDescending((KeyValuePair<string, int> x) => x.Value).Take(6) select x.Key);
					}
					catch
					{
						seed = "";
					}
					list.Add(new VectorRuleEntry { Rule = vectorDoc.Rule, Seed = seed, Weights = weights, Norm = norm, IsEvidence = vectorDoc.IsEvidence });
				}
				_vectorRuleEntries = list;
				_vectorIdf = idf;
				_vectorIndexVersion = _ruleDataVersion;
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// Legacy commit rule: engine unavailable → index null; otherwise commit when any embedding succeeded,
	/// or when there were no rules / no seeds (empty index is valid); embeddings all failed → stay unbuilt.
	/// </summary>
	internal void EnsureOnnxIndex()
	{
		try
		{
			if (_onnxRuleEntries != null && _onnxIndexVersion == _ruleDataVersion)
			{
				return;
			}
			lock (_onnxIndexLock)
			{
				if (_onnxRuleEntries != null && _onnxIndexVersion == _ruleDataVersion)
				{
					return;
				}
				List<OnnxRuleEntry> entries = new List<OnnxRuleEntry>();
				int seedCount = 0;
				bool engineAvailable = false;
				IReadOnlyList<LoreRule> rules = null;
				try
				{
					engineAvailable = _ports.EmbeddingAvailable;
					rules = _rules();
					if (engineAvailable && rules != null)
					{
						for (int i = 0; i < rules.Count; i++)
						{
							LoreRule r = rules[i];
							if (r != null)
							{
								addSeeds(GetRuleTopicSeeds(r), isEvidence: false);
								addSeeds(GetRuleEvidenceSeeds(r), isEvidence: true);
							}
							void addSeeds(IEnumerable<string> seeds, bool isEvidence)
							{
								if (seeds == null)
								{
									return;
								}
								foreach (string seed in seeds)
								{
									string text = (seed ?? "").Trim();
									if (!string.IsNullOrWhiteSpace(text))
									{
										seedCount++;
										if (_ports.TryGetEmbedding(text, out var vector) && vector != null && vector.Length != 0)
										{
											entries.Add(new OnnxRuleEntry { Rule = r, Seed = text, Vector = vector, IsEvidence = isEvidence });
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				bool noRules = rules == null || rules.Count == 0;
				bool noSeeds = seedCount <= 0;
				if (!engineAvailable)
				{
					_onnxRuleEntries = null;
					_onnxIndexVersion = -1L;
				}
				else if (entries.Count > 0 || noRules || noSeeds)
				{
					_onnxRuleEntries = entries;
					_onnxIndexVersion = _ruleDataVersion;
				}
				else
				{
					_onnxRuleEntries = null;
					_onnxIndexVersion = -1L;
				}
			}
		}
		catch
		{
		}
	}

	// ---------------------------------------------------------------- scoring

	/// <summary>Per rule id keep max raw / max evidence; NaN → -inf; missing raw falls back to evidence, then 0.</summary>
	internal static List<KnowledgeRuleScore> CollapseRuleScoresByMax(List<KnowledgeRuleScore> scored)
	{
		List<KnowledgeRuleScore> list = new List<KnowledgeRuleScore>();
		try
		{
			if (scored == null || scored.Count <= 0)
			{
				return list;
			}
			Dictionary<string, KnowledgeRuleScore> dictionary = new Dictionary<string, KnowledgeRuleScore>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < scored.Count; i++)
			{
				KnowledgeRuleScore ruleScore = scored[i];
				if (ruleScore?.Rule == null)
				{
					continue;
				}
				string text = (ruleScore.Rule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "rule_" + i.ToString(CultureInfo.InvariantCulture);
				}
				float num = (float.IsNaN(ruleScore.RawScore) ? float.NegativeInfinity : ruleScore.RawScore);
				float num2 = (float.IsNaN(ruleScore.EvidenceScore) ? float.NegativeInfinity : ruleScore.EvidenceScore);
				if (!dictionary.TryGetValue(text, out var value) || value == null)
				{
					dictionary[text] = new KnowledgeRuleScore { Rule = ruleScore.Rule, RawScore = num, EvidenceScore = num2 };
					continue;
				}
				float num3 = (float.IsNaN(value.RawScore) ? float.NegativeInfinity : value.RawScore);
				float num4 = (float.IsNaN(value.EvidenceScore) ? float.NegativeInfinity : value.EvidenceScore);
				if (num > num3)
				{
					num3 = num;
				}
				if (num2 > num4)
				{
					num4 = num2;
				}
				dictionary[text] = new KnowledgeRuleScore { Rule = ruleScore.Rule, RawScore = num3, EvidenceScore = num4 };
			}
			foreach (KeyValuePair<string, KnowledgeRuleScore> item in dictionary)
			{
				KnowledgeRuleScore value2 = item.Value;
				if (value2 != null && value2.Rule != null)
				{
					float num5 = (float.IsNaN(value2.RawScore) ? float.NegativeInfinity : value2.RawScore);
					float num6 = (float.IsNaN(value2.EvidenceScore) ? float.NegativeInfinity : value2.EvidenceScore);
					if (float.IsNegativeInfinity(num5) && !float.IsNegativeInfinity(num6))
					{
						num5 = num6;
					}
					if (float.IsNegativeInfinity(num5))
					{
						num5 = 0f;
					}
					if (float.IsNegativeInfinity(num6))
					{
						num6 = 0f;
					}
					list.Add(new KnowledgeRuleScore { Rule = value2.Rule, RawScore = num5, EvidenceScore = num6 });
				}
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Sort by raw desc / evidence desc / id; strict pass at min score, then fill up to topN (hard-capped at 20) ignoring the threshold.</summary>
	internal List<KnowledgeRuleScore> SelectSemanticCandidateScores(List<KnowledgeRuleScore> scored, string source, string input, int topK)
	{
		List<KnowledgeRuleScore> list = new List<KnowledgeRuleScore>();
		try
		{
			if (scored == null || scored.Count <= 0)
			{
				return list;
			}
			int num = ((topK <= 0) ? 2 : topK);
			int semanticResultHardCap = GetSemanticResultHardCap(num);
			if (semanticResultHardCap > 0 && semanticResultHardCap < num)
			{
				num = semanticResultHardCap;
			}
			if (num < 1)
			{
				num = 1;
			}
			float num2 = 0f;
			try
			{
				num2 = _ports.SemanticMinScore;
			}
			catch
			{
				num2 = 0.21f;
			}
			List<KnowledgeRuleScore> list2 = (from x in scored
				where x?.Rule != null && !float.IsNaN(x.RawScore)
				orderby x.RawScore descending, x.EvidenceScore descending
				select x).ThenBy((KnowledgeRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			float num3 = ((list2.Count > 0) ? list2[0].RawScore : 0f);
			float num4 = ((list2.Count > 1) ? list2[1].RawScore : 0f);
			float num5 = ((list2.Count > 0) ? list2[0].EvidenceScore : 0f);
			float num6 = ((list2.Count > 1) ? list2[1].EvidenceScore : 0f);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num7 = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				KnowledgeRuleScore ruleScore = list2[i];
				if (ruleScore?.Rule == null || ruleScore.RawScore < num2)
				{
					continue;
				}
				string text = (ruleScore.Rule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
				{
					list.Add(ruleScore);
					num7++;
				}
			}
			if (list.Count < num)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					if (list.Count >= num)
					{
						break;
					}
					KnowledgeRuleScore ruleScore2 = list2[j];
					if (ruleScore2?.Rule == null)
					{
						continue;
					}
					string text2 = (ruleScore2.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2) || hashSet.Add(text2))
					{
						list.Add(ruleScore2);
					}
				}
			}
			try
			{
				_ports.Log("LoreMatch", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
			}
			catch
			{
			}
		}
		catch
		{
		}
		return list;
	}

	internal List<KnowledgeRuleScore> FindOnnxCandidateScores(string input, int topK)
	{
		List<KnowledgeRuleScore> result = new List<KnowledgeRuleScore>();
		try
		{
			long version = _ruleDataVersion;
			List<OnnxRuleEntry> entries = _onnxRuleEntries;
			if (entries == null || entries.Count <= 0 || _onnxIndexVersion != version)
			{
				return result;
			}
			if (!_ports.EmbeddingAvailable)
			{
				return result;
			}
			if (!_ports.TryGetEmbedding(input, out var vector) || vector == null || vector.Length == 0)
			{
				return result;
			}
			List<KnowledgeRuleScore> list = new List<KnowledgeRuleScore>();
			for (int i = 0; i < entries.Count; i++)
			{
				OnnxRuleEntry onnxRuleEntry = entries[i];
				if (onnxRuleEntry != null && onnxRuleEntry.Rule != null && onnxRuleEntry.Vector != null && onnxRuleEntry.Vector.Length != 0)
				{
					float num = DotProduct(vector, onnxRuleEntry.Vector);
					list.Add(onnxRuleEntry.IsEvidence
						? new KnowledgeRuleScore { Rule = onnxRuleEntry.Rule, RawScore = float.NaN, EvidenceScore = num }
						: new KnowledgeRuleScore { Rule = onnxRuleEntry.Rule, RawScore = num, EvidenceScore = float.NaN });
				}
			}
			if (list.Count <= 0)
			{
				return result;
			}
			list = CollapseRuleScoresByMax(list);
			if (list.Count <= 0)
			{
				return result;
			}
			list = list.OrderByDescending((KnowledgeRuleScore x) => x.RawScore).ThenBy((KnowledgeRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidateScores(list, "onnx", input, topK);
		}
		catch
		{
		}
		return result;
	}

	internal List<KnowledgeRuleScore> FindSparseCandidateScores(string input, int topK)
	{
		List<KnowledgeRuleScore> result = new List<KnowledgeRuleScore>();
		try
		{
			long version = _ruleDataVersion;
			List<VectorRuleEntry> entries = _vectorRuleEntries;
			Dictionary<string, float> idf = _vectorIdf;
			if (entries == null || entries.Count <= 0 || idf == null || _vectorIndexVersion != version)
			{
				return result;
			}
			List<string> list = ExtractVectorTokens(input);
			if (list == null || list.Count <= 0)
			{
				return result;
			}
			Dictionary<string, int> dictionary = CountTokens(list);
			if (dictionary.Count <= 0)
			{
				return result;
			}
			Dictionary<string, float> dictionary2 = BuildVectorWeights(dictionary, idf, out float norm);
			if (dictionary2.Count <= 0 || norm <= 0f)
			{
				return result;
			}
			List<KnowledgeRuleScore> list2 = new List<KnowledgeRuleScore>();
			for (int i = 0; i < entries.Count; i++)
			{
				VectorRuleEntry vectorRuleEntry = entries[i];
				if (vectorRuleEntry == null || vectorRuleEntry.Rule == null || vectorRuleEntry.Weights == null || vectorRuleEntry.Weights.Count <= 0 || vectorRuleEntry.Norm <= 0f)
				{
					continue;
				}
				float num = DotProduct(dictionary2, vectorRuleEntry.Weights);
				if (!(num <= 0f))
				{
					float num2 = num / (norm * vectorRuleEntry.Norm);
					list2.Add(vectorRuleEntry.IsEvidence
						? new KnowledgeRuleScore { Rule = vectorRuleEntry.Rule, RawScore = float.NaN, EvidenceScore = num2 }
						: new KnowledgeRuleScore { Rule = vectorRuleEntry.Rule, RawScore = num2, EvidenceScore = float.NaN });
				}
			}
			if (list2.Count <= 0)
			{
				return result;
			}
			list2 = CollapseRuleScoresByMax(list2);
			if (list2.Count <= 0)
			{
				return result;
			}
			list2 = list2.OrderByDescending((KnowledgeRuleScore x) => x.RawScore).ThenBy((KnowledgeRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidateScores(list2, "sparse", input, topK);
		}
		catch
		{
		}
		return result;
	}

	/// <summary>ONNX first; sparse only when ONNX yields nothing.</summary>
	internal List<KnowledgeRuleScore> FindVectorCandidateScores(string input, int topK)
	{
		try
		{
			List<KnowledgeRuleScore> list = FindOnnxCandidateScores(input, topK);
			if (list != null && list.Count > 0)
			{
				try
				{
					_ports.Log("LoreMatch", $"semantic_source=onnx top={list.Count}");
				}
				catch
				{
				}
				return list;
			}
		}
		catch
		{
		}
		List<KnowledgeRuleScore> list2 = FindSparseCandidateScores(input, topK);
		if (list2 != null && list2.Count > 0)
		{
			try
			{
				_ports.Log("LoreMatch", $"semantic_source=sparse top={list2.Count}");
			}
			catch
			{
			}
		}
		return list2;
	}

	/// <summary>Cross-encoder rerank of the top-N recalled; without reranker scores are raw×weight ("recall_fallback").</summary>
	internal List<KnowledgeRuleScore> RerankCandidateScores(string input, List<KnowledgeRuleScore> recalled, int rerankTopK, float scoreWeight = 1f)
	{
		List<KnowledgeRuleScore> list = new List<KnowledgeRuleScore>();
		try
		{
			List<KnowledgeRuleScore> list2 = (recalled ?? new List<KnowledgeRuleScore>()).Where((KnowledgeRuleScore x) => x?.Rule != null).OrderByDescending((KnowledgeRuleScore x) => x.RawScore).ThenByDescending((KnowledgeRuleScore x) => x.EvidenceScore).ThenBy((KnowledgeRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			int num = ((rerankTopK <= 0) ? 4 : rerankTopK);
			if (num > list2.Count)
			{
				num = list2.Count;
			}
			list2 = list2.Take(num).ToList();
			bool flag = false;
			try
			{
				flag = _ports.RerankerAvailable;
			}
			catch
			{
				flag = false;
			}
			List<string> list3 = null;
			List<float> list4 = null;
			bool flag2 = false;
			if (flag)
			{
				list3 = new List<string>(list2.Count);
				for (int i = 0; i < list2.Count; i++)
				{
					list3.Add((list2[i]?.Rule == null) ? "" : BuildRuleRerankText(list2[i].Rule));
				}
				flag2 = _ports.TryScoreBatch(input, list3, out list4) && list4 != null && list4.Count == list2.Count;
			}
			float num2 = Math.Max(0f, scoreWeight);
			for (int i = 0; i < list2.Count; i++)
			{
				KnowledgeRuleScore ruleScore = list2[i];
				if (ruleScore?.Rule == null)
				{
					continue;
				}
				float num3 = ruleScore.RawScore;
				if (float.IsNaN(num3) || float.IsNegativeInfinity(num3))
				{
					num3 = ruleScore.EvidenceScore;
				}
				if (float.IsNaN(num3) || float.IsNegativeInfinity(num3))
				{
					num3 = 0f;
				}
				float num4 = num3 * num2;
				float num5 = num4;
				if (flag && flag2 && list3 != null && i < list3.Count && !string.IsNullOrWhiteSpace(list3[i]) && list4 != null && i < list4.Count)
				{
					num5 = list4[i] * num2;
				}
				list.Add(new KnowledgeRuleScore { Rule = ruleScore.Rule, RawScore = num5, EvidenceScore = num4, RerankScore = num5 });
			}
			list = SelectSemanticCandidateScores(list, (flag && flag2) ? "cross_encoder" : "recall_fallback", input, num);
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Recall → rerank with a version-keyed cache (key includes input, budgets, weight and engine availability).</summary>
	internal List<KnowledgeRuleScore> FindRankedVectorCandidateScores(string input, int recallTopK, int rerankTopK, float scoreWeight)
	{
		string normalizedInput = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalizedInput))
		{
			return new List<KnowledgeRuleScore>();
		}
		long version = _ruleDataVersion;
		bool embeddingAvailable = false;
		bool rerankerAvailable = false;
		try
		{
			embeddingAvailable = _ports.EmbeddingAvailable;
		}
		catch
		{
		}
		try
		{
			rerankerAvailable = _ports.RerankerAvailable;
		}
		catch
		{
		}
		string key = Hash8(version.ToString(CultureInfo.InvariantCulture)
			+ "|input=" + normalizedInput
			+ "|recall=" + Math.Max(0, recallTopK).ToString(CultureInfo.InvariantCulture)
			+ "|rerank=" + Math.Max(0, rerankTopK).ToString(CultureInfo.InvariantCulture)
			+ "|weight=" + scoreWeight.ToString("R", CultureInfo.InvariantCulture)
			+ "|embedding=" + (embeddingAvailable ? "1" : "0")
			+ "|reranker=" + (rerankerAvailable ? "1" : "0"));
		if (TryGetRankedCache(key, version, out List<KnowledgeRuleScore> cached))
		{
			return cached;
		}
		List<KnowledgeRuleScore> recalled = FindVectorCandidateScores(normalizedInput, recallTopK);
		if (recalled == null || recalled.Count == 0)
		{
			return new List<KnowledgeRuleScore>();
		}
		List<KnowledgeRuleScore> ranked = RerankCandidateScores(normalizedInput, recalled, rerankTopK, scoreWeight);
		if (ranked != null && ranked.Count > 0 && version == _ruleDataVersion)
		{
			PutRankedCache(key, version, ranked);
		}
		return ranked ?? new List<KnowledgeRuleScore>();
	}

	internal static List<KnowledgeRuleScore> CloneRuleScores(IEnumerable<KnowledgeRuleScore> scores)
	{
		List<KnowledgeRuleScore> result = new List<KnowledgeRuleScore>();
		foreach (KnowledgeRuleScore score in scores ?? Enumerable.Empty<KnowledgeRuleScore>())
		{
			if (score?.Rule == null)
			{
				continue;
			}
			result.Add(new KnowledgeRuleScore { Rule = score.Rule, RawScore = score.RawScore, EvidenceScore = score.EvidenceScore, RerankScore = score.RerankScore });
		}
		return result;
	}

	private bool TryGetRankedCache(string key, long version, out List<KnowledgeRuleScore> scores)
	{
		scores = null;
		try
		{
			lock (_rankedCacheLock)
			{
				if (_rankedCache != null && _rankedCache.TryGetValue(key, out RankedCacheItem item) && item.Version == version && item.Scores != null && item.Scores.Count > 0)
				{
					item.Ticks = DateTime.UtcNow.Ticks;
					_rankedCache[key] = item;
					scores = CloneRuleScores(item.Scores);
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void PutRankedCache(string key, long version, IEnumerable<KnowledgeRuleScore> scores)
	{
		List<KnowledgeRuleScore> copy = CloneRuleScores(scores);
		if (copy.Count == 0)
		{
			return;
		}
		try
		{
			lock (_rankedCacheLock)
			{
				if (_rankedCache == null)
				{
					_rankedCache = new Dictionary<string, RankedCacheItem>();
				}
				if (_rankedCache.Count >= RankedCandidateCacheMax)
				{
					_rankedCache.Clear();
				}
				_rankedCache[key] = new RankedCacheItem { Version = version, Ticks = DateTime.UtcNow.Ticks, Scores = copy };
			}
		}
		catch
		{
		}
	}

	internal int RankedCacheCount
	{
		get
		{
			lock (_rankedCacheLock)
			{
				return _rankedCache?.Count ?? 0;
			}
		}
	}
}
