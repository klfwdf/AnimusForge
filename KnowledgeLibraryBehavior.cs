using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public class KnowledgeLibraryBehavior : CampaignBehaviorBase
{
	private struct KeywordIndexEntry
	{
		public LoreRule Rule;

		public string Keyword;

		public string KeywordLower;
	}

	private struct LoreContextCacheItem
	{
		public long Version;

		public long Ticks;

		public string Value;
	}

	private class VectorDoc
	{
		public LoreRule Rule;

		public Dictionary<string, int> Tf;

		public bool IsEvidence;
	}

	private class VectorRuleEntry
	{
		public LoreRule Rule;

		public string Seed;

		public Dictionary<string, float> Weights;

		public float Norm;

		public bool IsEvidence;
	}

	private class OnnxRuleEntry
	{
		public LoreRule Rule;

		public string Seed;

		public float[] Vector;

		public bool IsEvidence;
	}

	private class RuleScore
	{
		public LoreRule Rule;

		public float RawScore;

		public float EvidenceScore;
	}

	private class CandidateRules
	{
		public string MatchMode = "none";

		public List<LoreRule> OrderedRules = new List<LoreRule>();
	}

	private struct RuleAggregate
	{
		public LoreRule Rule;

		public float Score;

		public int BestRank;

		public int HitCount;
	}

	public class RuleIndexItem
	{
		public string Id;

		public string Label;
	}

	public class KnowledgeFile
	{
		public int Version = 1;

		public List<LoreRule> Rules = new List<LoreRule>();
	}

	public class LoreRule
	{
		public string Id;

		public List<string> Keywords = new List<string>();

		public List<string> SemanticPrototypes = new List<string>();

		public List<LoreVariant> Variants = new List<LoreVariant>();
	}

	public class LoreVariant
	{
		public int Priority;

		public LoreWhen When;

		public string Content;
	}

	public class LoreWhen
	{
		public List<string> HeroIds;

		public List<string> Cultures;

		public List<string> KingdomIds;

		public List<string> Roles;

		public bool? IsFemale;

		public bool? IsClanLeader;

		public Dictionary<string, int> SkillMin;
	}

	private const string StorageKey = "_knowledge_rules_v1_json";

	private string _storageJson = "";

	private KnowledgeFile _file = new KnowledgeFile();

	private string _editingRuleId;

	private static readonly object _loreLogLock = new object();

	private static Dictionary<string, long> _loreLogLastTicks = new Dictionary<string, long>();

	private static readonly object _skillCacheLock = new object();

	private static Dictionary<string, SkillObject> _skillByIdCache;

	private static long _ruleDataVersion = 1L;

	private static readonly object _keywordIndexLock = new object();

	private static Dictionary<char, List<KeywordIndexEntry>> _keywordIndex;

	private static long _keywordIndexVersion = -1L;

	private static readonly object _vectorIndexLock = new object();

	private static List<VectorRuleEntry> _vectorRuleEntries;

	private static Dictionary<string, float> _vectorIdf;

	private static long _vectorIndexVersion = -1L;

	private static readonly object _onnxIndexLock = new object();

	private static List<OnnxRuleEntry> _onnxRuleEntries;

	private static long _onnxIndexVersion = -1L;

	private static readonly object _loreContextCacheLock = new object();

	private static Dictionary<string, LoreContextCacheItem> _loreContextCache = new Dictionary<string, LoreContextCacheItem>();

	private const int LoreContextCacheMax = 256;

		private const int RuleListPageSize = 60;

	private long _ruleIndexCacheVersion = -1L;

	private List<RuleIndexItem> _ruleIndexCache;

	public static KnowledgeLibraryBehavior Instance { get; private set; }

	private static string TrimPreview(string s, int maxChars)
	{
		s = (s ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.Length <= maxChars)
		{
			return s;
		}
		return s.Substring(0, maxChars).Trim();
	}

	private static string Hash8(string s)
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

	private static void TouchRuleData()
	{
		_ruleDataVersion++;
		if (_ruleDataVersion <= 0)
		{
			_ruleDataVersion = 1L;
		}
		try
		{
			lock (_loreContextCacheLock)
			{
				_loreContextCache.Clear();
			}
		}
		catch
		{
		}
		try
		{
			lock (_keywordIndexLock)
			{
				_keywordIndex = null;
				_keywordIndexVersion = -1L;
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

	private static bool TryGetLoreContextCache(string key, long version, out string value)
	{
		value = "";
		try
		{
			lock (_loreContextCacheLock)
			{
				if (_loreContextCache != null && _loreContextCache.TryGetValue(key, out var value2) && value2.Version == version)
				{
					value2.Ticks = DateTime.UtcNow.Ticks;
					_loreContextCache[key] = value2;
					value = value2.Value ?? "";
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void PutLoreContextCache(string key, long version, string value)
	{
		try
		{
			lock (_loreContextCacheLock)
			{
				if (_loreContextCache == null)
				{
					_loreContextCache = new Dictionary<string, LoreContextCacheItem>();
				}
				if (_loreContextCache.Count >= 256)
				{
					_loreContextCache.Clear();
				}
				_loreContextCache[key] = new LoreContextCacheItem
				{
					Version = version,
					Ticks = DateTime.UtcNow.Ticks,
					Value = (value ?? "")
				};
			}
		}
		catch
		{
		}
	}

	private void EnsureKeywordIndex()
	{
		try
		{
			if (_keywordIndex != null && _keywordIndexVersion == _ruleDataVersion)
			{
				return;
			}
			lock (_keywordIndexLock)
			{
				if (_keywordIndex != null && _keywordIndexVersion == _ruleDataVersion)
				{
					return;
				}
				Dictionary<char, List<KeywordIndexEntry>> dictionary = new Dictionary<char, List<KeywordIndexEntry>>();
				try
				{
					if (_file != null && _file.Rules != null)
					{
						foreach (LoreRule rule in _file.Rules)
						{
							if (rule == null || rule.Keywords == null || rule.Keywords.Count == 0)
							{
								continue;
							}
							for (int i = 0; i < rule.Keywords.Count; i++)
							{
								string text = (rule.Keywords[i] ?? "").Trim();
								if (text.Length != 0)
								{
									string text2 = text.ToLowerInvariant();
									char key = text2[0];
									if (!dictionary.TryGetValue(key, out var value))
									{
										value = (dictionary[key] = new List<KeywordIndexEntry>());
									}
									value.Add(new KeywordIndexEntry
									{
										Rule = rule,
										Keyword = text,
										KeywordLower = text2
									});
								}
							}
						}
					}
				}
				catch
				{
				}
				_keywordIndex = dictionary;
				_keywordIndexVersion = _ruleDataVersion;
			}
		}
		catch
		{
		}
	}

	private static bool StartsWithAt(string inputLower, int start, string kwLower)
	{
		if (string.IsNullOrEmpty(inputLower) || string.IsNullOrEmpty(kwLower))
		{
			return false;
		}
		int length = kwLower.Length;
		if (start < 0 || start + length > inputLower.Length)
		{
			return false;
		}
		for (int i = 0; i < length; i++)
		{
			if (inputLower[start + i] != kwLower[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsAsciiWordChar(char ch)
	{
		return ch < '\u0080' && (char.IsLetterOrDigit(ch) || ch == '_');
	}

	private static bool IsCjkChar(char ch)
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

	private static List<string> ExtractVectorTokens(string text)
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

	private static Dictionary<string, int> CountTokens(List<string> tokens)
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

	private static Dictionary<string, float> BuildVectorWeights(Dictionary<string, int> tf, Dictionary<string, float> idf, out float norm)
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

	private static float DotProduct(Dictionary<string, float> a, Dictionary<string, float> b)
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

	private static string BuildRuleSearchText(LoreRule rule)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(rule.Id))
			{
				stringBuilder.Append(rule.Id).Append(' ');
			}
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
			if (rule.Variants != null)
			{
				for (int j = 0; j < rule.Variants.Count; j++)
				{
					LoreVariant loreVariant = rule.Variants[j];
					if (loreVariant != null)
					{
						string value2 = (loreVariant.Content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(value2))
						{
							stringBuilder.Append(value2).Append(' ');
						}
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

	private static List<string> GetRuleTopicSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Variants != null)
			{
				for (int i = 0; i < rule.Variants.Count; i++)
				{
					string text = (rule.Variants[i]?.Content ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						AddSemanticSeed(list, seen, text, 320);
					}
				}
			}
			if (list.Count <= 0)
			{
				string raw = BuildRuleSearchText(rule);
				AddSemanticSeed(list, seen, raw, 320);
			}
			if (list.Count <= 0)
			{
				AddSemanticSeed(list, seen, rule?.Id, 200);
			}
		}
		catch
		{
		}
		return list;
	}

	private static List<string> GetRuleEvidenceSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			AddSemanticSeed(list, seen, rule.Id, 120);
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
			if (list.Count <= 0 && rule.Variants != null)
			{
				for (int j = 0; j < rule.Variants.Count; j++)
				{
					string text2 = (rule.Variants[j]?.Content ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						AddSemanticSeed(list, seen, text2, 120);
						if (list.Count >= 2)
						{
							break;
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private void EnsureVectorIndex()
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
				if (_file != null && _file.Rules != null)
				{
					foreach (LoreRule rule in _file.Rules)
					{
						LoreRule r = rule;
						if (r != null)
						{
							List<string> ruleTopicSeeds = GetRuleTopicSeeds(r);
							List<string> ruleEvidenceSeeds = GetRuleEvidenceSeeds(r);
							addSeeds(ruleTopicSeeds, isEvidence: false);
							addSeeds(ruleEvidenceSeeds, isEvidence: true);
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
									List<string> tokens = ExtractVectorTokens(text);
									Dictionary<string, int> dictionary3 = CountTokens(tokens);
									if (dictionary3.Count > 0)
									{
										docs.Add(new VectorDoc
										{
											Rule = r,
											Tf = dictionary3,
											IsEvidence = isEvidence
										});
										HashSet<string> hashSet = new HashSet<string>(dictionary3.Keys, StringComparer.Ordinal);
										foreach (string item in hashSet)
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
				Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.Ordinal);
				if (count > 0)
				{
					foreach (KeyValuePair<string, int> item2 in df)
					{
						float value = 1f + (float)Math.Log(((double)count + 1.0) / ((double)item2.Value + 1.0));
						dictionary[item2.Key] = value;
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
					float norm = 0f;
					Dictionary<string, float> dictionary2 = BuildVectorWeights(vectorDoc.Tf, dictionary, out norm);
					if (dictionary2.Count <= 0 || norm <= 0f)
					{
						continue;
					}
					string seed = "";
					try
					{
						if (vectorDoc.Tf != null && vectorDoc.Tf.Count > 0)
						{
							seed = string.Join(" ", from x in vectorDoc.Tf.OrderByDescending((KeyValuePair<string, int> x) => x.Value).Take(6)
								select x.Key);
						}
					}
					catch
					{
						seed = "";
					}
					list.Add(new VectorRuleEntry
					{
						Rule = vectorDoc.Rule,
						Seed = seed,
						Weights = dictionary2,
						Norm = norm,
						IsEvidence = vectorDoc.IsEvidence
					});
				}
				_vectorRuleEntries = list;
				_vectorIdf = dictionary;
				_vectorIndexVersion = _ruleDataVersion;
			}
		}
		catch
		{
		}
	}

	private void EnsureOnnxIndex()
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
				try
				{
					OnnxEmbeddingEngine engine = OnnxEmbeddingEngine.Instance;
					if (engine != null && engine.IsAvailable && _file != null && _file.Rules != null)
					{
						for (int i = 0; i < _file.Rules.Count; i++)
						{
							LoreRule r = _file.Rules[i];
							if (r != null)
							{
								List<string> ruleTopicSeeds = GetRuleTopicSeeds(r);
								List<string> ruleEvidenceSeeds = GetRuleEvidenceSeeds(r);
								addSeeds(ruleTopicSeeds, isEvidence: false);
								addSeeds(ruleEvidenceSeeds, isEvidence: true);
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
									if (!string.IsNullOrWhiteSpace(text) && engine.TryGetEmbedding(text, out var vector) && vector != null && vector.Length != 0)
									{
										entries.Add(new OnnxRuleEntry
										{
											Rule = r,
											Seed = text,
											Vector = vector,
											IsEvidence = isEvidence
										});
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				_onnxRuleEntries = entries;
				_onnxIndexVersion = _ruleDataVersion;
			}
		}
		catch
		{
		}
	}

	private static float DotProduct(float[] a, float[] b)
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

	private static int GetSemanticResultHardCap(int topK)
	{
		if (topK <= 0)
		{
			return 20;
		}
		return Math.Min(topK, 20);
	}

	private static int GetLoreInjectLimit()
	{
		try
		{
			int num = AIConfigHandler.KnowledgeSemanticTopK;
			if (num < 1)
			{
				num = 1;
			}
			if (num > 20)
			{
				num = 20;
			}
			return num;
		}
		catch
		{
			return 4;
		}
	}

	private static List<RuleScore> CollapseRuleScoresByMax(List<RuleScore> scored)
	{
		List<RuleScore> list = new List<RuleScore>();
		try
		{
			if (scored == null || scored.Count <= 0)
			{
				return list;
			}
			Dictionary<string, RuleScore> dictionary = new Dictionary<string, RuleScore>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < scored.Count; i++)
			{
				RuleScore ruleScore = scored[i];
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
					dictionary[text] = new RuleScore
					{
						Rule = ruleScore.Rule,
						RawScore = num,
						EvidenceScore = num2
					};
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
				dictionary[text] = new RuleScore
				{
					Rule = ruleScore.Rule,
					RawScore = num3,
					EvidenceScore = num4
				};
			}
			foreach (KeyValuePair<string, RuleScore> item in dictionary)
			{
				RuleScore value2 = item.Value;
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
					list.Add(new RuleScore
					{
						Rule = value2.Rule,
						RawScore = num5,
						EvidenceScore = num6
					});
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<LoreRule> SelectSemanticCandidates(List<RuleScore> scored, string source, string input, int topK)
	{
		List<LoreRule> list = new List<LoreRule>();
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
			List<RuleScore> list2 = (from x in scored
				where x?.Rule != null
				orderby x.RawScore descending, x.EvidenceScore descending
				select x).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			float num2 = ((list2.Count > 0) ? list2[0].RawScore : 0f);
			float num3 = ((list2.Count > 1) ? list2[1].RawScore : 0f);
			float num4 = ((list2.Count > 0) ? list2[0].EvidenceScore : 0f);
			float num5 = ((list2.Count > 1) ? list2[1].EvidenceScore : 0f);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				RuleScore ruleScore = list2[i];
				if (ruleScore?.Rule != null)
				{
					string text = (ruleScore.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
					{
						list.Add(ruleScore.Rule);
					}
				}
			}
			try
			{
				Logger.Log("LoreMatch", $"semantic_accept source={source} mode=topn_raw selected={list.Count} topN={num} bestRaw={num2:0.000} second={num3:0.000} bestEvidence={num4:0.000} secondEvidence={num5:0.000}");
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

	private List<LoreRule> FindOnnxCandidateRules(string input, int topK)
	{
		List<LoreRule> result = new List<LoreRule>();
		try
		{
			EnsureOnnxIndex();
			if (_onnxRuleEntries == null || _onnxRuleEntries.Count <= 0)
			{
				return result;
			}
			OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
			if (instance == null || !instance.IsAvailable)
			{
				return result;
			}
			if (!instance.TryGetEmbedding(input, out var vector) || vector == null || vector.Length == 0)
			{
				return result;
			}
			List<RuleScore> list = new List<RuleScore>();
			for (int i = 0; i < _onnxRuleEntries.Count; i++)
			{
				OnnxRuleEntry onnxRuleEntry = _onnxRuleEntries[i];
				if (onnxRuleEntry != null && onnxRuleEntry.Rule != null && onnxRuleEntry.Vector != null && onnxRuleEntry.Vector.Length != 0)
				{
					float num = DotProduct(vector, onnxRuleEntry.Vector);
					if (onnxRuleEntry.IsEvidence)
					{
						list.Add(new RuleScore
						{
							Rule = onnxRuleEntry.Rule,
							RawScore = float.NaN,
							EvidenceScore = num
						});
					}
					else
					{
						list.Add(new RuleScore
						{
							Rule = onnxRuleEntry.Rule,
							RawScore = num,
							EvidenceScore = float.NaN
						});
					}
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
			list = list.OrderByDescending((RuleScore x) => x.RawScore).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidates(list, "onnx", input, topK);
		}
		catch
		{
		}
		return result;
	}

	private List<LoreRule> FindSparseCandidateRules(string input, int topK)
	{
		List<LoreRule> result = new List<LoreRule>();
		try
		{
			EnsureVectorIndex();
			if (_vectorRuleEntries == null || _vectorRuleEntries.Count <= 0)
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
			float norm;
			Dictionary<string, float> dictionary2 = BuildVectorWeights(dictionary, _vectorIdf, out norm);
			if (dictionary2.Count <= 0 || norm <= 0f)
			{
				return result;
			}
			List<RuleScore> list2 = new List<RuleScore>();
			for (int i = 0; i < _vectorRuleEntries.Count; i++)
			{
				VectorRuleEntry vectorRuleEntry = _vectorRuleEntries[i];
				if (vectorRuleEntry == null || vectorRuleEntry.Rule == null || vectorRuleEntry.Weights == null || vectorRuleEntry.Weights.Count <= 0 || vectorRuleEntry.Norm <= 0f)
				{
					continue;
				}
				float num = DotProduct(dictionary2, vectorRuleEntry.Weights);
				if (!(num <= 0f))
				{
					float num2 = num / (norm * vectorRuleEntry.Norm);
					if (vectorRuleEntry.IsEvidence)
					{
						list2.Add(new RuleScore
						{
							Rule = vectorRuleEntry.Rule,
							RawScore = float.NaN,
							EvidenceScore = num2
						});
					}
					else
					{
						list2.Add(new RuleScore
						{
							Rule = vectorRuleEntry.Rule,
							RawScore = num2,
							EvidenceScore = float.NaN
						});
					}
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
			list2 = list2.OrderByDescending((RuleScore x) => x.RawScore).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidates(list2, "sparse", input, topK);
		}
		catch
		{
		}
		return result;
	}

	private List<LoreRule> FindVectorCandidateRules(string input, int topK)
	{
		try
		{
			List<LoreRule> list = FindOnnxCandidateRules(input, topK);
			if (list != null && list.Count > 0)
			{
				try
				{
					Logger.Log("LoreMatch", $"semantic_source=onnx top={list.Count}");
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
		List<LoreRule> list2 = FindSparseCandidateRules(input, topK);
		if (list2 != null && list2.Count > 0)
		{
			try
			{
				Logger.Log("LoreMatch", $"semantic_source=sparse top={list2.Count}");
			}
			catch
			{
			}
		}
		return list2;
	}

	private List<LoreRule> CollectKeywordCandidateRules(string input)
	{
		List<LoreRule> list = new List<LoreRule>();
		try
		{
			EnsureKeywordIndex();
			string text = (input ?? "").ToLowerInvariant();
			HashSet<LoreRule> hashSet = null;
			if (_keywordIndex != null && text.Length > 0)
			{
				for (int i = 0; i < text.Length; i++)
				{
					char key = text[i];
					if (!_keywordIndex.TryGetValue(key, out var value) || value == null)
					{
						continue;
					}
					for (int j = 0; j < value.Count; j++)
					{
						KeywordIndexEntry keywordIndexEntry = value[j];
						if (keywordIndexEntry.Rule != null && StartsWithAt(text, i, keywordIndexEntry.KeywordLower))
						{
							if (hashSet == null)
							{
								hashSet = new HashSet<LoreRule>();
							}
							hashSet.Add(keywordIndexEntry.Rule);
						}
					}
				}
			}
			if (hashSet != null && hashSet.Count > 0)
			{
				if (_file != null && _file.Rules != null)
				{
					for (int k = 0; k < _file.Rules.Count; k++)
					{
						LoreRule loreRule = _file.Rules[k];
						if (loreRule != null && hashSet.Contains(loreRule))
						{
							list.Add(loreRule);
						}
					}
				}
				else
				{
					list = hashSet.ToList();
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static List<string> SplitKnowledgeIntents(string input, int maxParts = 3)
	{
		List<string> list = new List<string>();
		try
		{
			string text = (input ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			List<string> list2 = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in text)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、')
				{
					string text2 = stringBuilder.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						list2.Add(text2);
					}
					stringBuilder.Clear();
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			string text3 = stringBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				list2.Add(text3);
			}
			if (list2.Count <= 0)
			{
				list2.Add(text);
			}
			List<string> list3 = new List<string>();
			string[] array = new string[13]
			{
				"然后", "顺便", "另外", "再说", "并且", "而且", "以及", "同时", "还有", "再加上",
				"顺带", "并且还", "以及还"
			};
			for (int j = 0; j < list2.Count; j++)
			{
				string text4 = (list2[j] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text4))
				{
					continue;
				}
				bool flag = false;
				foreach (string text5 in array)
				{
					int num = text4.IndexOf(text5, StringComparison.Ordinal);
					if (num > 1 && num < text4.Length - text5.Length - 1)
					{
						string text6 = text4.Substring(0, num).Trim();
						string text7 = text4.Substring(num + text5.Length).Trim();
						if (text6.Length >= 2)
						{
							list3.Add(text6);
						}
						if (text7.Length >= 2)
						{
							list3.Add(text7);
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list3.Add(text4);
				}
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			list.Add(text);
			hashSet.Add(text);
			for (int l = 0; l < list3.Count; l++)
			{
				if (list.Count >= Math.Max(1, maxParts + 1))
				{
					break;
				}
				string text8 = (list3[l] ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text8) && text8.Length >= 2 && hashSet.Add(text8))
				{
					list.Add(text8);
				}
			}
		}
		catch
		{
		}
		if (list.Count <= 0)
		{
			string text9 = (input ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text9))
			{
				list.Add(text9);
			}
		}
		return list;
	}

	private List<LoreRule> MergeVectorRulesAcrossIntents(List<string> intentInputs, int topK)
	{
		List<LoreRule> result = new List<LoreRule>();
		try
		{
			List<string> list = (intentInputs ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
			if (list.Count <= 0)
			{
				return result;
			}
			Dictionary<LoreRule, RuleAggregate> dictionary = new Dictionary<LoreRule, RuleAggregate>();
			for (int num = 0; num < list.Count; num++)
			{
				string input = list[num];
				List<LoreRule> list2 = FindVectorCandidateRules(input, topK);
				if (list2 == null || list2.Count <= 0)
				{
					continue;
				}
				for (int num2 = 0; num2 < list2.Count; num2++)
				{
					LoreRule loreRule = list2[num2];
					if (loreRule == null)
					{
						continue;
					}
					float num3 = 1f / ((float)num2 + 1f);
					float num4 = ((num == 0) ? 1f : 0.92f);
					float num5 = num3 * num4;
					if (!dictionary.TryGetValue(loreRule, out var value))
					{
						dictionary[loreRule] = new RuleAggregate
						{
							Rule = loreRule,
							Score = num5,
							BestRank = num2 + 1,
							HitCount = 1
						};
						continue;
					}
					value.Score += num5;
					value.HitCount++;
					if (num2 + 1 < value.BestRank)
					{
						value.BestRank = num2 + 1;
					}
					dictionary[loreRule] = value;
				}
			}
			if (dictionary.Count <= 0)
			{
				return result;
			}
			int val = ((topK <= 0) ? 10 : topK);
			val = Math.Min(val, 20);
			result = (from x in (from x in dictionary.Values
					orderby x.Score + (float)(x.HitCount - 1) * 0.12f descending, x.BestRank
					select x).ThenBy((RuleAggregate x) => x.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase)
				select x.Rule into x
				where x != null
				select x).Take(val).ToList();
		}
		catch
		{
		}
		return result;
	}

	private CandidateRules CollectCandidateRules(string input)
	{
		CandidateRules result = new CandidateRules();
		try
		{
			List<string> intentInputs = SplitKnowledgeIntents(input);
			if (intentInputs.Count > 1)
			{
				try
				{
					Logger.Log("LoreMatch", string.Format("intent_split count={0} intents={1}", intentInputs.Count, string.Join(" || ", intentInputs)));
				}
				catch
				{
				}
			}
			bool semanticEnabled = true;
			int injectTopK = 6;
			int candidateTopK = 12;
			try
			{
				semanticEnabled = AIConfigHandler.KnowledgeRetrievalEnabled;
				injectTopK = AIConfigHandler.KnowledgeSemanticTopK;
			}
			catch
			{
			}
			if (injectTopK < 1)
			{
				injectTopK = 1;
			}
			if (injectTopK > 20)
			{
				injectTopK = 20;
			}
			candidateTopK = Math.Max(injectTopK * 4, injectTopK + 8);
			if (candidateTopK > 20)
			{
				candidateTopK = 20;
			}
			if (candidateTopK < injectTopK)
			{
				candidateTopK = injectTopK;
			}
			Func<bool> func = delegate
			{
				if (!semanticEnabled)
				{
					return false;
				}
				List<LoreRule> list = MergeVectorRulesAcrossIntents(intentInputs, candidateTopK);
				if (list == null || list.Count <= 0)
				{
					return false;
				}
				result.MatchMode = ((intentInputs.Count > 1) ? "vector_multi" : "vector");
				result.OrderedRules = list.Where((LoreRule x) => x != null).ToList();
				try
				{
					Logger.Log("LoreMatch", $"candidate_pool mode={result.MatchMode} injectTopN={injectTopK} candidateTopN={candidateTopK} got={result.OrderedRules.Count}");
				}
				catch
				{
				}
				return result.OrderedRules.Count > 0;
			};
			if (!semanticEnabled)
			{
				return result;
			}
			if (func())
			{
				return result;
			}
			return result;
		}
		catch
		{
		}
		return result;
	}

	private static string SanitizeRuleIdPart(string s)
	{
		try
		{
			string text = (s ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			foreach (char c in text)
			{
				stringBuilder.Append(char.IsWhiteSpace(c) ? '_' : c);
			}
			text = stringBuilder.ToString();
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text = text.Replace(oldChar, '_');
			}
			while (text.Contains("__"))
			{
				text = text.Replace("__", "_");
			}
			text = text.Trim('_');
			if (text.Length > 64)
			{
				text = text.Substring(0, 64).Trim('_');
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeKeywordForCompare(string keyword)
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

	private bool TryFindRuleIdByKeyword(string keyword, string excludeRuleId, out string foundRuleId)
	{
		foundRuleId = null;
		try
		{
			string text = NormalizeKeywordForCompare(keyword);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string text2 = (excludeRuleId ?? "").Trim();
			if (_file == null || _file.Rules == null)
			{
				return false;
			}
			foreach (LoreRule rule in _file.Rules)
			{
				if (rule == null)
				{
					continue;
				}
				string text3 = (rule.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text3) || (!string.IsNullOrEmpty(text2) && string.Equals(text3, text2, StringComparison.OrdinalIgnoreCase)) || rule.Keywords == null || rule.Keywords.Count <= 0)
				{
					continue;
				}
				foreach (string keyword2 in rule.Keywords)
				{
					string text4 = NormalizeKeywordForCompare(keyword2);
					if (string.IsNullOrEmpty(text4) || !string.Equals(text4, text, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					foundRuleId = text3;
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private string BuildRuleIdFromKeyword(string keyword)
	{
		string text = SanitizeRuleIdPart(keyword);
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return "rule_" + text;
	}

	private bool HasRuleIdIgnoreCase(string id)
	{
		try
		{
			string tid = (id ?? "").Trim();
			if (string.IsNullOrEmpty(tid))
			{
				return false;
			}
			if (_file == null || _file.Rules == null)
			{
				return false;
			}
			return _file.Rules.Any((LoreRule r) => r != null && string.Equals((r.Id ?? "").Trim(), tid, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	public List<RuleIndexItem> GetRuleIndexItemsForDev(int maxCount = 200)
	{
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreRule rule in _file.Rules)
			{
				if (list.Count >= maxCount)
				{
					break;
				}
				string text = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
				{
					continue;
				}
				string text2 = "";
				try
				{
					text2 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text2 = "";
				}
				string label = (string.IsNullOrWhiteSpace(text2) ? text : (text2 + " (" + text + ")"));
				list.Add(new RuleIndexItem
				{
					Id = text,
					Label = label
				});
			}
			try
			{
				list = list.OrderBy((RuleIndexItem x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch
			{
			}
			if (list.Count > maxCount)
			{
				list.RemoveRange(maxCount, list.Count - maxCount);
			}
		}
		catch
		{
		}
		return list;
	}

	private void EnsureRuleIndexCache()
	{
		try
		{
			if (_ruleIndexCache != null && _ruleIndexCacheVersion == _ruleDataVersion)
			{
				return;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			List<RuleIndexItem> list = new List<RuleIndexItem>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreRule rule in _file.Rules)
			{
				string text = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
				{
					continue;
				}
				string text2 = "";
				try
				{
					text2 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = text;
				}
				string label = $"{text2} (关键词:{(rule?.Keywords?.Count).GetValueOrDefault()} 条, 提示词:{(rule?.Variants?.Count).GetValueOrDefault()} 条)";
				list.Add(new RuleIndexItem
				{
					Id = text,
					Label = label
				});
			}
			try
			{
				list = list.OrderBy((RuleIndexItem x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch
			{
			}
			_ruleIndexCache = list;
			_ruleIndexCacheVersion = _ruleDataVersion;
		}
		catch
		{
			try
			{
				_ruleIndexCache = _ruleIndexCache ?? new List<RuleIndexItem>();
				_ruleIndexCacheVersion = _ruleDataVersion;
			}
			catch
			{
			}
		}
	}

	private static bool IsRuleListMatch(RuleIndexItem it, string[] tokens)
	{
		if (tokens == null || tokens.Length == 0)
		{
			return true;
		}
		string text = it?.Label ?? "";
		string text2 = it?.Id ?? "";
		for (int i = 0; i < tokens.Length; i++)
		{
			string value = (tokens[i] ?? "").Trim();
			if (!string.IsNullOrEmpty(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0 && text2.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool ShouldLogLore(string key, long minIntervalTicks)
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			lock (_loreLogLock)
			{
				if (_loreLogLastTicks == null)
				{
					_loreLogLastTicks = new Dictionary<string, long>();
				}
				if (_loreLogLastTicks.TryGetValue(key, out var value) && ticks - value < minIntervalTicks)
				{
					return false;
				}
				_loreLogLastTicks[key] = ticks;
				if (_loreLogLastTicks.Count > 600)
				{
					_loreLogLastTicks.Clear();
				}
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static void LogLoreMissOnce(string tag, string input, int ruleCount, string heroId, string cultureId, string kingdomId, string role)
	{
		try
		{
			string text = TrimPreview(input, 80);
			string key = tag + ":" + Hash8(text) + ":" + heroId + ":" + cultureId + ":" + kingdomId + ":" + role;
			if (ShouldLogLore(key, TimeSpan.FromMilliseconds(1500.0).Ticks))
			{
				Logger.Log("LoreMatch", string.Format("{0} rules={1} hero={2} culture={3} kingdom={4} role={5} input={6}", tag, ruleCount, heroId ?? "", cultureId ?? "", kingdomId ?? "", role ?? "", text));
			}
		}
		catch
		{
		}
	}

	private static void LogLoreContextTrace(string source, string heroId, string charId, string cultureId, string kingdomId, string role, bool isFemale, bool isClanLeader, string kingdomOverride, string inputText, bool invalidContext = false)
	{
		try
		{
			string text = (source ?? "").Trim();
			string text2 = (heroId ?? "").Trim();
			string text3 = (charId ?? "").Trim();
			string text4 = (cultureId ?? "").Trim().ToLowerInvariant();
			string text5 = (kingdomId ?? "").Trim().ToLowerInvariant();
			string text6 = (role ?? "").Trim().ToLowerInvariant();
			string text7 = (kingdomOverride ?? "").Trim().ToLowerInvariant();
			string text8 = Hash8(inputText ?? "");
			Logger.Log("LoreMatch", $"lore_ctx source={text} invalid={invalidContext} hero={text2} char={text3} culture={text4} kingdom={text5} role={text6} female={isFemale} clanLeader={isClanLeader} kingdomOverride={text7} inputHash={text8}");
		}
		catch
		{
		}
	}

	public KnowledgeLibraryBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		StripSemanticPrototypes();
		try
		{
			_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
		}
		catch
		{
			_storageJson = "";
		}
		dataStore.SyncData("_knowledge_rules_v1_json", ref _storageJson);
		try
		{
			if (!string.IsNullOrWhiteSpace(_storageJson))
			{
				KnowledgeFile knowledgeFile = JsonConvert.DeserializeObject<KnowledgeFile>(_storageJson);
				if (knowledgeFile != null)
				{
					_file = knowledgeFile;
				}
			}
		}
		catch
		{
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		StripSemanticPrototypes();
		TouchRuleData();
	}

	private void StripSemanticPrototypes()
	{
		try
		{
			if (_file == null || _file.Rules == null)
			{
				return;
			}
			for (int i = 0; i < _file.Rules.Count; i++)
			{
				LoreRule loreRule = _file.Rules[i];
				if (loreRule != null)
				{
					if (loreRule.SemanticPrototypes == null)
					{
						loreRule.SemanticPrototypes = new List<string>();
					}
					else if (loreRule.SemanticPrototypes.Count > 0)
					{
						loreRule.SemanticPrototypes.Clear();
					}
				}
			}
		}
		catch
		{
		}
	}

	public string BuildLoreContext(string inputText, Hero npcHero)
	{
		string text = (inputText ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		if (npcHero == null)
		{
			LogLoreContextTrace("hero", "", "", "neutral", "", "commoner", isFemale: false, isClanLeader: false, "", text, invalidContext: true);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=invalid_context source=hero inputLen={text.Length}", text);
			}
			catch
			{
			}
			return "";
		}
		string text2 = (npcHero?.StringId ?? "").Trim();
		string text3 = (npcHero?.Culture?.StringId ?? "neutral").Trim().ToLower();
		string text4 = "";
		try
		{
			text4 = (npcHero?.Clan?.Kingdom?.StringId ?? npcHero?.MapFaction?.StringId ?? "").Trim().ToLower();
		}
		catch
		{
			text4 = "";
		}
		string text5 = "commoner";
		try
		{
			if (npcHero != null)
			{
				text5 = (npcHero.IsLord ? "lord" : ((!npcHero.IsNotable) ? RoleFromOccupation(npcHero.Occupation) : "notable"));
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			flag = npcHero?.IsFemale ?? false;
		}
		catch
		{
		}
		bool flag2 = false;
		try
		{
			flag2 = npcHero != null && npcHero.Clan != null && npcHero.Clan.Leader == npcHero;
		}
		catch
		{
		}
		string text6 = "";
		try
		{
			text6 = (npcHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			text6 = "";
		}
		if (string.IsNullOrWhiteSpace(text6))
		{
			text6 = "该NPC";
		}
		string text7 = (text6 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace("hero", text2, "", text3, text4, text5, flag, flag2, "", text);
		long ruleDataVersion = _ruleDataVersion;
		string key = Hash8($"{ruleDataVersion}|H|{text2}|{text7}|{text3}|{text4}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|{text}");
		if (TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		int num = 0;
		try
		{
			num = (_file?.Rules?.Count).GetValueOrDefault();
		}
		catch
		{
			num = 0;
		}
		if (num <= 0)
		{
			LogLoreMissOnce("rules_empty", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=rules_empty rules={num} inputLen={text.Length} mode=none", text);
			}
			catch
			{
			}
			return "";
		}
		bool flag3 = false;
		int num2 = 0;
		int loreInjectLimit = GetLoreInjectLimit();
		StringBuilder stringBuilder = new StringBuilder();
		CandidateRules candidateRules = CollectCandidateRules(text);
		string text8 = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		if (list == null || list.Count == 0)
		{
			LogLoreMissOnce("rule_miss", text, num, text2, text3, text4, text5);
			PutLoreContextCache(key, ruleDataVersion, "");
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=rule_miss rules={num} inputLen={text.Length} mode={text8}", text);
			}
			catch
			{
			}
			return "";
		}
		foreach (LoreRule item in list)
		{
			if (num2 >= loreInjectLimit)
			{
				break;
			}
			if (item == null)
			{
				continue;
			}
			string text9 = "";
			try
			{
				text9 = item?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text9 = "";
			}
			try
			{
				Logger.Log("LoreMatch", "vector_hit rule=" + item.Id + " mode=" + text8 + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			LoreVariant loreVariant = PickBestVariant(item, npcHero, null, text2, text3, text4, text5, flag, flag2);
			if (loreVariant == null)
			{
				try
				{
					Logger.Log("LoreMatch", "variant_miss rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, "reason=variant_miss mode=" + text8, text);
				}
				catch
				{
				}
				continue;
			}
			string value2 = (loreVariant.Content ?? "").Trim();
			if (string.IsNullOrEmpty(value2))
			{
				try
				{
					Logger.Log("LoreMatch", "content_empty rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, "reason=content_empty mode=" + text8, text);
				}
				catch
				{
				}
				continue;
			}
			try
			{
				Logger.Log("LoreMatch", "variant_hit rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: true, "reason=variant_hit mode=" + text8, text);
			}
			catch
			{
			}
			num2++;
			if (!flag3)
			{
				stringBuilder.AppendLine(" ");
				stringBuilder.AppendLine("玩家说的话让你的脑海里浮现了这些知识");
				flag3 = true;
			}
			string text10 = (string.IsNullOrWhiteSpace(text9) ? (item.Id ?? "相关语义") : text9);
			stringBuilder.AppendLine("【以下是关于（" + text10 + "）的背景知识，" + text6 + "可酌情参考，但不要假设玩家提起过此话题】");
			stringBuilder.AppendLine(value2);
		}
		string text11 = ((num2 > 0) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce("variant_or_content_miss", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={text8} inputLen={text.Length}", text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, $"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={text8} inputLen={text.Length}", text);
			}
			catch
			{
			}
		}
		PutLoreContextCache(key, ruleDataVersion, text11);
		return text11;
	}

	private static string RoleFromOccupation(Occupation occ)
	{
		return occ switch
		{
			Occupation.Soldier => "soldier", 
			Occupation.Villager => "villager", 
			Occupation.Townsfolk => "townsfolk", 
			Occupation.Wanderer => "wanderer", 
			Occupation.Lord => "lord", 
			_ => "commoner", 
		};
	}

	public string BuildLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride = null)
	{
		string text = (inputText ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		Hero hero = null;
		try
		{
			hero = ((npcCharacter != null && npcCharacter.IsHero) ? npcCharacter.HeroObject : null);
		}
		catch
		{
			hero = null;
		}
		string text2 = (hero?.StringId ?? "").Trim();
		string textCharId = (npcCharacter?.StringId ?? "").Trim();
		if (hero == null && npcCharacter == null)
		{
			LogLoreContextTrace("character", "", "", "neutral", "", "commoner", isFemale: false, isClanLeader: false, kingdomIdOverride, text, invalidContext: true);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=invalid_context source=character inputLen={text.Length}", text);
			}
			catch
			{
			}
			return "";
		}
		string text3 = (hero?.Culture?.StringId ?? npcCharacter?.Culture?.StringId ?? "neutral").Trim().ToLower();
		string text4 = (kingdomIdOverride ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				text4 = (hero?.Clan?.Kingdom?.StringId ?? hero?.MapFaction?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				text4 = (MobileParty.ConversationParty?.MapFaction?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				text4 = (Settlement.CurrentSettlement?.OwnerClan?.Kingdom?.StringId ?? Settlement.CurrentSettlement?.MapFaction?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		string text5 = "commoner";
		try
		{
			if (hero != null)
			{
				text5 = (hero.IsLord ? "lord" : ((!hero.IsNotable) ? RoleFromOccupation(hero.Occupation) : "notable"));
			}
			else if (npcCharacter != null)
			{
				text5 = ((!npcCharacter.IsSoldier) ? RoleFromOccupation(npcCharacter.Occupation) : "soldier");
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			flag = hero?.IsFemale ?? npcCharacter?.IsFemale ?? false;
		}
		catch
		{
		}
		bool flag2 = false;
		try
		{
			flag2 = hero != null && hero.Clan != null && hero.Clan.Leader == hero;
		}
		catch
		{
		}
		string text6 = "";
		try
		{
			text6 = (hero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			text6 = "";
		}
		if (string.IsNullOrWhiteSpace(text6))
		{
			try
			{
				text6 = (npcCharacter?.Name?.ToString() ?? "").Trim();
			}
			catch
			{
				text6 = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text6))
		{
			text6 = "该NPC";
		}
		string text7 = (text6 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace((hero != null) ? "character_hero" : "character", text2, textCharId, text3, text4, text5, flag, flag2, kingdomIdOverride, text);
		long ruleDataVersion = _ruleDataVersion;
		string key = Hash8($"{ruleDataVersion}|C|{text2}|{text7}|{text3}|{text4}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|{text}");
		if (TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		int num = 0;
		try
		{
			num = (_file?.Rules?.Count).GetValueOrDefault();
		}
		catch
		{
			num = 0;
		}
		if (num <= 0)
		{
			LogLoreMissOnce("rules_empty", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=rules_empty rules={num} inputLen={text.Length} mode=none", text);
			}
			catch
			{
			}
			return "";
		}
		bool flag3 = false;
		int num2 = 0;
		int loreInjectLimit = GetLoreInjectLimit();
		StringBuilder stringBuilder = new StringBuilder();
		CandidateRules candidateRules = CollectCandidateRules(text);
		string text8 = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		if (list == null || list.Count == 0)
		{
			LogLoreMissOnce("rule_miss", text, num, text2, text3, text4, text5);
			PutLoreContextCache(key, ruleDataVersion, "");
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=rule_miss rules={num} inputLen={text.Length} mode={text8}", text);
			}
			catch
			{
			}
			return "";
		}
		foreach (LoreRule item in list)
		{
			if (num2 >= loreInjectLimit)
			{
				break;
			}
			if (item == null)
			{
				continue;
			}
			string text9 = "";
			try
			{
				text9 = item?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text9 = "";
			}
			try
			{
				Logger.Log("LoreMatch", "vector_hit rule=" + item.Id + " mode=" + text8 + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			LoreVariant loreVariant = PickBestVariant(item, hero, npcCharacter, text2, text3, text4, text5, flag, flag2);
			if (loreVariant == null)
			{
				try
				{
					Logger.Log("LoreMatch", "variant_miss rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, "reason=variant_miss mode=" + text8, text);
				}
				catch
				{
				}
				continue;
			}
			string value2 = (loreVariant.Content ?? "").Trim();
			if (string.IsNullOrEmpty(value2))
			{
				try
				{
					Logger.Log("LoreMatch", "content_empty rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, "reason=content_empty mode=" + text8, text);
				}
				catch
				{
				}
				continue;
			}
			try
			{
				Logger.Log("LoreMatch", "variant_hit rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: true, "reason=variant_hit mode=" + text8, text);
			}
			catch
			{
			}
			num2++;
			if (!flag3)
			{
				stringBuilder.AppendLine(" ");
				stringBuilder.AppendLine("玩家说的话让你的脑海里浮现了这些知识");
				flag3 = true;
			}
			string text10 = (string.IsNullOrWhiteSpace(text9) ? (item.Id ?? "相关语义") : text9);
			stringBuilder.AppendLine("【以下是关于（" + text10 + "）的背景知识，" + text6 + "可酌情参考，但不要假设玩家提起过此话题】");
			stringBuilder.AppendLine(value2);
		}
		string text11 = ((num2 > 0) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce("variant_or_content_miss", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, $"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={text8} inputLen={text.Length}", text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, $"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={text8} inputLen={text.Length}", text);
			}
			catch
			{
			}
		}
		PutLoreContextCache(key, ruleDataVersion, text11);
		return text11;
	}

	public string ExportRulesJson(bool pretty = false)
	{
		try
		{
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			return JsonConvert.SerializeObject(_file, pretty ? Formatting.Indented : Formatting.None);
		}
		catch
		{
			return "";
		}
	}

	public List<string> GetRuleIdsForDev(int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			foreach (string item in (from r in _file.Rules
				where r != null
				select (r.Id ?? "").Trim() into id
				where !string.IsNullOrEmpty(id)
				select id).OrderBy((string id) => id, StringComparer.OrdinalIgnoreCase))
			{
				if (list.Count >= maxCount)
				{
					break;
				}
				list.Add(item);
			}
		}
		catch
		{
		}
		return list;
	}

	public string ExportSingleRuleJson(string ruleId, bool pretty = false)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				return "";
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			LoreRule loreRule = _file.Rules.FirstOrDefault((LoreRule r) => r != null && string.Equals((r.Id ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
			if (loreRule == null)
			{
				return "";
			}
			return JsonConvert.SerializeObject(loreRule, pretty ? Formatting.Indented : Formatting.None);
		}
		catch
		{
			return "";
		}
	}

	public bool ImportSingleRuleJson(string json, bool overwrite = true)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			LoreRule loreRule = JsonConvert.DeserializeObject<LoreRule>(json);
			if (loreRule == null)
			{
				return false;
			}
			string text = (loreRule.Id ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			loreRule.Id = text;
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			int num = -1;
			for (int i = 0; i < _file.Rules.Count; i++)
			{
				LoreRule loreRule2 = _file.Rules[i];
				if (loreRule2 != null && string.Equals((loreRule2.Id ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				if (!overwrite)
				{
					return false;
				}
				_file.Rules[num] = loreRule;
			}
			else
			{
				_file.Rules.Add(loreRule);
			}
			try
			{
				_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
			}
			catch
			{
			}
			TouchRuleData();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool ImportRulesJson(string json, bool overwrite = true)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			KnowledgeFile knowledgeFile = JsonConvert.DeserializeObject<KnowledgeFile>(json);
			if (knowledgeFile == null)
			{
				return false;
			}
			if (knowledgeFile.Rules == null)
			{
				knowledgeFile.Rules = new List<LoreRule>();
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			if (overwrite)
			{
				_file = knowledgeFile;
			}
			else
			{
				foreach (LoreRule r in knowledgeFile.Rules)
				{
					if (r != null)
					{
						if (string.IsNullOrWhiteSpace(r.Id))
						{
							r.Id = "import_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
						}
						if (!_file.Rules.Any((LoreRule x) => x != null && string.Equals(x.Id, r.Id, StringComparison.OrdinalIgnoreCase)))
						{
							_file.Rules.Add(r);
						}
					}
				}
			}
			StripSemanticPrototypes();
			try
			{
				_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
			}
			catch
			{
			}
			TouchRuleData();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static LoreVariant PickBestVariant(LoreRule rule, Hero npcHero, CharacterObject npcCharacter, string heroId, string cultureId, string kingdomId, string role, bool isFemale, bool isClanLeader)
	{
		if (rule == null || rule.Variants == null || rule.Variants.Count == 0)
		{
			return null;
		}
		LoreVariant loreVariant = null;
		int num = int.MinValue;
		int num2 = int.MinValue;
		foreach (LoreVariant variant in rule.Variants)
		{
			if (variant == null)
			{
				continue;
			}
			LoreWhen when = variant.When;
			if (!IsMatch(when, npcHero, npcCharacter, heroId, cultureId, kingdomId, role, isFemale, isClanLeader, out var score))
			{
				continue;
			}
			int num3 = 0;
			try
			{
				if (when != null && when.SkillMin != null && when.SkillMin.Count > 0)
				{
					foreach (KeyValuePair<string, int> item in when.SkillMin)
					{
						int value = item.Value;
						if (value > 0)
						{
							num3 += value;
						}
					}
				}
			}
			catch
			{
				num3 = 0;
			}
			if (loreVariant == null || score > num || (score == num && num3 > num2))
			{
				loreVariant = variant;
				num = score;
				num2 = num3;
			}
		}
		return loreVariant;
	}

	private static void EnsureSkillByIdCache()
	{
		try
		{
			if (_skillByIdCache != null && _skillByIdCache.Count > 0)
			{
				return;
			}
			lock (_skillCacheLock)
			{
				if (_skillByIdCache != null && _skillByIdCache.Count > 0)
				{
					return;
				}
				Dictionary<string, SkillObject> dictionary = new Dictionary<string, SkillObject>(StringComparer.OrdinalIgnoreCase);
				MBReadOnlyList<SkillObject> mBReadOnlyList = Game.Current?.ObjectManager?.GetObjectTypeList<SkillObject>();
				if (mBReadOnlyList != null)
				{
					foreach (SkillObject item in mBReadOnlyList)
					{
						string text = (item?.StringId ?? "").Trim();
						if (!string.IsNullOrEmpty(text) && !dictionary.ContainsKey(text))
						{
							dictionary[text] = item;
						}
					}
				}
				_skillByIdCache = dictionary;
			}
		}
		catch
		{
		}
	}

	private static SkillObject FindSkillById(string skillId)
	{
		try
		{
			string text = (skillId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			EnsureSkillByIdCache();
			if (_skillByIdCache != null && _skillByIdCache.TryGetValue(text, out var value))
			{
				return value;
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool TryGetSkillValueById(string skillId, Hero npcHero, CharacterObject npcCharacter, out int value)
	{
		value = 0;
		try
		{
			SkillObject skillObject = FindSkillById(skillId);
			if (skillObject == null)
			{
				return false;
			}
			if (npcHero != null)
			{
				value = npcHero.GetSkillValue(skillObject);
				return true;
			}
			if (npcCharacter != null)
			{
				value = npcCharacter.GetSkillValue(skillObject);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsMatch(LoreWhen when, Hero npcHero, CharacterObject npcCharacter, string heroId, string cultureId, string kingdomId, string role, bool isFemale, bool isClanLeader, out int score)
	{
		score = 0;
		if (when == null)
		{
			return true;
		}
		if (when.HeroIds != null && when.HeroIds.Count > 0)
		{
			bool flag = false;
			for (int i = 0; i < when.HeroIds.Count; i++)
			{
				string text = (when.HeroIds[i] ?? "").Trim();
				if (!string.IsNullOrEmpty(text) && string.Equals(text, heroId, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			score++;
		}
		if (when.Cultures != null && when.Cultures.Count > 0)
		{
			bool flag2 = false;
			for (int j = 0; j < when.Cultures.Count; j++)
			{
				string text2 = (when.Cultures[j] ?? "").Trim();
				if (!string.IsNullOrEmpty(text2) && string.Equals(text2, cultureId, StringComparison.OrdinalIgnoreCase))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
			score++;
		}
		if (when.KingdomIds != null && when.KingdomIds.Count > 0)
		{
			if (string.IsNullOrEmpty(kingdomId))
			{
				return false;
			}
			bool flag3 = false;
			for (int k = 0; k < when.KingdomIds.Count; k++)
			{
				string text3 = (when.KingdomIds[k] ?? "").Trim();
				if (!string.IsNullOrEmpty(text3) && string.Equals(text3, kingdomId, StringComparison.OrdinalIgnoreCase))
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				return false;
			}
			score++;
		}
		if (when.Roles != null && when.Roles.Count > 0)
		{
			bool flag4 = false;
			for (int l = 0; l < when.Roles.Count; l++)
			{
				string text4 = (when.Roles[l] ?? "").Trim();
				if (!string.IsNullOrEmpty(text4) && string.Equals(text4, role, StringComparison.OrdinalIgnoreCase))
				{
					flag4 = true;
					break;
				}
			}
			if (!flag4)
			{
				return false;
			}
			score++;
		}
		if (when.IsFemale.HasValue)
		{
			if (isFemale != when.IsFemale.Value)
			{
				return false;
			}
			score++;
		}
		if (when.IsClanLeader.HasValue)
		{
			if (isClanLeader != when.IsClanLeader.Value)
			{
				return false;
			}
			score++;
		}
		if (when.SkillMin != null && when.SkillMin.Count > 0)
		{
			foreach (KeyValuePair<string, int> item in when.SkillMin)
			{
				string text5 = (item.Key ?? "").Trim();
				int value = item.Value;
				if (!string.IsNullOrEmpty(text5) && value >= 0)
				{
					if (!TryGetSkillValueById(text5, npcHero, npcCharacter, out var value2))
					{
						return false;
					}
					if (value2 < value)
					{
						return false;
					}
					score++;
				}
			}
		}
		return true;
	}

	public void OpenEditorMenu(Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit", "编辑现有知识", null));
		list.Add(new InquiryElement("create", "创建新知识", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("知识编辑系统", "请选择操作：", list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "edit")
				{
					OpenRuleListMenu(onReturn);
				}
				else if (text == "create")
				{
					CreateNewRule(onReturn);
				}
				else
				{
					onReturn();
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static void TryPersistMcmSettings(DuelSettings s)
	{
		try
		{
			if (s != null)
			{
				MethodInfo method = ((object)s).GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
				if (method != null)
				{
					method.Invoke(s, null);
				}
			}
		}
		catch
		{
		}
	}

	private void OpenRetrievalSettingsMenu(Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置。"));
			onReturn();
			return;
		}
		int topK = settings.KnowledgeSemanticTopK;
		try
		{
			if (settings.KnowledgeDirectTopN > 0)
			{
				topK = settings.KnowledgeDirectTopN;
			}
		}
		catch
		{
		}
		if (topK < 1)
		{
			topK = 1;
		}
		if (topK > 20)
		{
			topK = 20;
		}
		settings.KnowledgeSemanticTopK = topK;
		try
		{
			settings.KnowledgeDirectTopN = topK;
		}
		catch
		{
		}
		string text = (AIConfigHandler.KnowledgeRetrievalFromMcm ? "MCM（当前生效）" : "RuleBehaviorPrompts.json（当前生效）");
		string text2 = "TopN直通";
		string descriptionText = "当前配置来源：" + text + "\n当前模式：" + text2 + "\n语义检索：" + (AIConfigHandler.KnowledgeRetrievalEnabled ? "开启" : "关闭") + "\n语义优先：" + (AIConfigHandler.KnowledgeSemanticFirst ? "是" : "否（关键词优先）") + "\n关键词兜底：" + (AIConfigHandler.KnowledgeKeywordFallback ? "开启" : "关闭") + "\n直通条数上限：" + AIConfigHandler.KnowledgeSemanticTopK;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("source", "配置来源：" + (settings.UseMcmKnowledgeRetrieval ? "切到 RuleBehaviorPrompts.json" : "切到 MCM"), null));
		list.Add(new InquiryElement("enabled", "语义检索：" + (settings.KnowledgeRetrievalEnabled ? "关闭" : "开启"), null));
		list.Add(new InquiryElement("topk", "设置直通条数上限（当前 " + topK + "）", null));
		list.Add(new InquiryElement("reset", "恢复默认（MCM）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("知识检索设置", descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRetrievalSettingsMenu(onReturn);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (string.IsNullOrEmpty(text3))
				{
					OpenRetrievalSettingsMenu(onReturn);
				}
				else
				{
					switch (text3)
					{
					case "source":
						settings.UseMcmKnowledgeRetrieval = !settings.UseMcmKnowledgeRetrieval;
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					case "enabled":
						settings.UseMcmKnowledgeRetrieval = true;
						settings.KnowledgeRetrievalEnabled = !settings.KnowledgeRetrievalEnabled;
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					case "topk":
						InformationManager.ShowTextInquiry(new TextInquiryData("设置直通条数上限", "请输入 1~20 的整数：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
						{
							string s = (input ?? "").Trim();
							if (!int.TryParse(s, out var result))
							{
								InformationManager.DisplayMessage(new InformationMessage("请输入有效整数。"));
								OpenRetrievalSettingsMenu(onReturn);
							}
							else
							{
								if (result < 1)
								{
									result = 1;
								}
								if (result > 20)
								{
									result = 20;
								}
								settings.UseMcmKnowledgeRetrieval = true;
								settings.KnowledgeSemanticTopK = result;
								try
								{
									settings.KnowledgeDirectTopN = result;
								}
								catch
								{
								}
								TryPersistMcmSettings(settings);
								TouchRuleData();
								OpenRetrievalSettingsMenu(onReturn);
							}
						}, delegate
						{
							OpenRetrievalSettingsMenu(onReturn);
						}, shouldInputBeObfuscated: false, null, topK.ToString(CultureInfo.InvariantCulture)));
						break;
					case "reset":
						settings.UseMcmKnowledgeRetrieval = true;
						settings.KnowledgeRetrievalEnabled = true;
						settings.KnowledgeSemanticFirst = true;
						settings.KnowledgeKeywordFallback = false;
						settings.KnowledgeSemanticTopK = 2;
						try
						{
							settings.KnowledgeDirectTopN = 2;
						}
						catch
						{
						}
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					default:
						OpenRetrievalSettingsMenu(onReturn);
						break;
					}
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRuleListMenu(Action onReturn)
	{
		OpenRuleListMenuPaged(onReturn, 0, null);
	}

	private void OpenRuleListMenuPaged(Action onReturn, int page, string query)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		EnsureRuleIndexCache();
		string q = (query ?? "").Trim();
		string[] tokens = null;
		try
		{
			if (!string.IsNullOrWhiteSpace(q))
			{
				tokens = q.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			}
		}
		catch
		{
			tokens = null;
		}
		int num = page * 60;
		int num2 = num + 60;
		int num3 = 0;
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			List<RuleIndexItem> list2 = _ruleIndexCache ?? new List<RuleIndexItem>();
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				RuleIndexItem ruleIndexItem = list2[num4];
				if (ruleIndexItem != null && IsRuleListMatch(ruleIndexItem, tokens))
				{
					if (num3 >= num && num3 < num2)
					{
						list.Add(ruleIndexItem);
					}
					num3++;
				}
			}
		}
		catch
		{
			num3 = 0;
			list = new List<RuleIndexItem>();
		}
		int num5 = ((num3 > 0) ? ((num3 + 60 - 1) / 60) : 0);
		if (num5 > 0 && page >= num5)
		{
			OpenRuleListMenuPaged(onReturn, num5 - 1, query);
			return;
		}
		string text = "编辑现有知识";
		string titleText = ((num5 > 0) ? $"{text}（{num3} 条，{page + 1}/{num5}）" : (text + "（0 条）"));
		string text2 = "请选择要编辑的知识：";
		if (!string.IsNullOrWhiteSpace(q))
		{
			text2 = text2 + "\n当前搜索：" + TrimPreview(q, 80);
		}
		if (num3 <= 0)
		{
			text2 += "\n\n没有匹配项。可使用“搜索”或“创建新知识”。";
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", "搜索（按关键词/ID）", null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement("__clear__", "清空搜索", null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", "上一页", null));
		}
		if (num5 > 0 && page + 1 < num5)
		{
			list3.Add(new InquiryElement("__next__", "下一页", null));
		}
		list3.Add(new InquiryElement("__create__", "创建新知识", null));
		if (list.Count > 0)
		{
			list3.Add(new InquiryElement("__sep__", "----------------", null));
		}
		foreach (RuleIndexItem item in list)
		{
			string text3 = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text3))
			{
				string title = (item?.Label ?? text3).Trim();
				list3.Add(new InquiryElement(text3, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, text2, list3, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRuleListMenuPaged(onReturn, page, query);
			}
			else
			{
				string text4 = selected[0].Identifier as string;
				switch (text4)
				{
				case "__sep__":
					OpenRuleListMenuPaged(onReturn, page, query);
					break;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索知识", "输入关键词或 RuleId（可用空格分隔多个词）：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "取消", delegate(string input)
					{
						OpenRuleListMenuPaged(onReturn, 0, input);
					}, delegate
					{
						OpenRuleListMenuPaged(onReturn, page, query);
					}, shouldInputBeObfuscated: false, null, q));
					break;
				case "__clear__":
					OpenRuleListMenuPaged(onReturn, 0, null);
					break;
				case "__prev__":
					OpenRuleListMenuPaged(onReturn, page - 1, query);
					break;
				case "__next__":
					OpenRuleListMenuPaged(onReturn, page + 1, query);
					break;
				case "__create__":
					CreateNewRule(onReturn);
					break;
				default:
				{
					string id = text4;
					LoreRule loreRule = FindRule(id);
					if (loreRule == null)
					{
						OpenRuleListMenuPaged(onReturn, page, query);
					}
					else
					{
						_editingRuleId = loreRule.Id;
						OpenRuleEditorMenu(loreRule, delegate
						{
							OpenRuleListMenuPaged(onReturn, page, query);
						});
					}
					break;
				}
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void CreateNewRule(Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		InformationManager.ShowTextInquiry(new TextInquiryData("创建新知识", "请输入该知识的“第一个关键词”（将用于该知识条目的ID与导出文件命名，不可重复；后续仍可继续添加更多关键词）：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "创建", "返回", delegate(string input)
		{
			string text = (input ?? "").Trim();
			string text2 = BuildRuleIdFromKeyword(text);
			string foundRuleId;
			if (string.IsNullOrEmpty(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：第一个关键词为空或无法用于命名。"));
				CreateNewRule(onReturn);
			}
			else if (HasRuleIdIgnoreCase(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：已存在同名知识条目（ID=" + text2 + "）。请换一个关键词。"));
				CreateNewRule(onReturn);
			}
			else if (TryFindRuleIdByKeyword(text, null, out foundRuleId))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：关键词已被其他知识条目占用（关键词=" + text + "，RuleId=" + foundRuleId + "）。"));
				CreateNewRule(onReturn);
			}
			else
			{
				LoreRule loreRule = new LoreRule
				{
					Id = text2,
					Keywords = new List<string>()
				};
				if (!string.IsNullOrEmpty(text))
				{
					loreRule.Keywords.Add(text);
				}
				loreRule.Variants = new List<LoreVariant>();
				_file.Rules.Add(loreRule);
				TouchRuleData();
				_editingRuleId = loreRule.Id;
				OpenRuleEditorMenu(loreRule, delegate
				{
					OpenRuleListMenu(onReturn);
				});
			}
		}, delegate
		{
			OpenRuleListMenu(onReturn);
		}));
	}

	private void OpenRuleEditorMenu(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		string text = ((rule.Keywords != null && rule.Keywords.Count > 0) ? rule.Keywords[0] : (rule.Id ?? "知识"));
		string descriptionText = $"关键词：{rule.Keywords?.Count ?? 0} 条\n提示词：{rule.Variants?.Count ?? 0} 条";
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("keywords", "编辑关键词", null));
		list.Add(new InquiryElement("variants", "编辑提示词（条件组合）", null));
		list.Add(new InquiryElement("delete", "删除此知识", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("知识 - " + text, descriptionText, list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRuleEditorMenu(rule, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "keywords":
					OpenKeywordMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "variants":
					OpenVariantMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "delete":
					_file.Rules.RemoveAll((LoreRule r) => r != null && r.Id == rule.Id);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenRuleEditorMenu(rule, onReturn);
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenKeywordMenu(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", "添加关键词（一次一个）", null));
		if (rule.Keywords.Count > 0)
		{
			list.Add(new InquiryElement("__remove__", "删除关键词", null));
		}
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement("__k__" + keyword, "关键词：" + keyword, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑关键词", "一次输入一个关键词，确定后会加入列表。", list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenKeywordMenu(rule, onReturn);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__add__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData("添加关键词", "请输入一个关键词：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						string text2 = (input ?? "").Trim();
						string kwNorm = NormalizeKeywordForCompare(text2);
						if (string.IsNullOrEmpty(kwNorm))
						{
							OpenKeywordMenu(rule, onReturn);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.Keywords.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), kwNorm, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							string foundRuleId;
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：该关键词已存在于本知识条目中。"));
								OpenKeywordMenu(rule, onReturn);
							}
							else if (TryFindRuleIdByKeyword(text2, rule.Id, out foundRuleId))
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：关键词已被其他知识条目占用（关键词=" + text2 + "，RuleId=" + foundRuleId + "）。"));
								OpenKeywordMenu(rule, onReturn);
							}
							else
							{
								rule.Keywords.Add(text2);
								TouchRuleData();
								OpenKeywordMenu(rule, onReturn);
							}
						}
					}, delegate
					{
						OpenKeywordMenu(rule, onReturn);
					}));
				}
				else if (text == "__remove__")
				{
					OpenKeywordRemoveMenu(rule, onReturn);
				}
				else
				{
					OpenKeywordMenu(rule, onReturn);
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenKeywordRemoveMenu(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement(keyword, keyword, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除关键词", "选择要删除的关键词：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenKeywordMenu(rule, onReturn);
			}
			else
			{
				string kw = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(kw))
				{
					rule.Keywords.RemoveAll((string x) => string.Equals(x, kw, StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenKeywordMenu(rule, onReturn);
			}
		}, delegate
		{
			OpenKeywordMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenPrototypeMenu(LoreRule rule, Action onReturn)
	{
		InformationManager.DisplayMessage(new InformationMessage("[知识] 原型句功能已停用。请直接维护“关键词 + 提示词”。"));
		onReturn?.Invoke();
	}

	private void OpenPrototypeViewMenu(LoreRule rule, Action onReturn, int page, string query)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.SemanticPrototypes == null)
		{
			rule.SemanticPrototypes = new List<string>();
		}
		List<string> list = new List<string>();
		try
		{
			foreach (string semanticPrototype in rule.SemanticPrototypes)
			{
				string text = (semanticPrototype ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		string q = (query ?? "").Trim();
		List<string> filtered = list;
		if (!string.IsNullOrWhiteSpace(q))
		{
			string ql = q.ToLowerInvariant();
			filtered = list.Where((string x) => (x ?? "").ToLowerInvariant().Contains(ql)).ToList();
		}
		int count = filtered.Count;
		int num = ((count <= 0) ? 1 : ((int)Math.Ceiling((double)count / 12.0)));
		if (page < 0)
		{
			page = 0;
		}
		if (page >= num)
		{
			page = num - 1;
		}
		if (page < 0)
		{
			page = 0;
		}
		int num2 = page * 12;
		List<string> list2 = filtered.Skip(num2).Take(12).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", "搜索原型句", null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement("__clear__", "清空搜索", null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", "上一页", null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", "下一页", null));
		}
		list3.Add(new InquiryElement("__back__", "返回原型句菜单", null));
		if (list2.Count > 0)
		{
			list3.Add(new InquiryElement("__sep__", "----------------", null));
		}
		for (int num3 = 0; num3 < list2.Count; num3++)
		{
			int num4 = num2 + num3;
			string s = list2[num3];
			string title = $"{num4 + 1}. {TrimPreview(s, 90)}";
			list3.Add(new InquiryElement("__item__" + num4, title, null));
		}
		string text2 = $"共 {count} 条";
		if (!string.IsNullOrWhiteSpace(q))
		{
			text2 = text2 + "（搜索：" + TrimPreview(q, 40) + "）";
		}
		text2 += $"，第 {page + 1}/{num} 页。";
		if (list2.Count <= 0)
		{
			text2 += "\n当前页无内容。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("查看原型句", text2, list3, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				switch (text3)
				{
				case "__back__":
					OpenPrototypeMenu(rule, onReturn);
					return;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索原型句", "输入关键词：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
					{
						OpenPrototypeViewMenu(rule, onReturn, 0, (input ?? "").Trim());
					}, delegate
					{
						OpenPrototypeViewMenu(rule, onReturn, page, q);
					}, shouldInputBeObfuscated: false, null, q));
					return;
				case "__clear__":
					OpenPrototypeViewMenu(rule, onReturn, 0, "");
					return;
				case "__prev__":
					OpenPrototypeViewMenu(rule, onReturn, page - 1, q);
					return;
				case "__next__":
					OpenPrototypeViewMenu(rule, onReturn, page + 1, q);
					return;
				default:
				{
					if (text3.StartsWith("__item__", StringComparison.Ordinal) && int.TryParse(text3.Substring("__item__".Length), out var result) && result >= 0 && result < filtered.Count)
					{
						string text4 = filtered[result] ?? "";
						InformationManager.ShowInquiry(new InquiryData("原型句详情", text4, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回", "", delegate
						{
							OpenPrototypeViewMenu(rule, onReturn, page, q);
						}, null));
						return;
					}
					break;
				}
				case null:
					break;
				}
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
		}, delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildPrototypeGenerationUserPrompt(LoreRule rule, int targetCount, IEnumerable<string> existingForPrompt = null)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			if (targetCount <= 0)
			{
				targetCount = 1;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				stringBuilder.AppendLine("规则ID: " + text);
			}
			List<string> list = new List<string>();
			try
			{
				if (rule.Keywords != null)
				{
					foreach (string keyword in rule.Keywords)
					{
						string text2 = (keyword ?? "").Trim();
						if (!string.IsNullOrEmpty(text2))
						{
							if (list.Count >= 12)
							{
								break;
							}
							list.Add(text2);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("关键词: " + ((list.Count > 0) ? string.Join(" / ", list) : "（无）"));
			List<string> list2 = new List<string>();
			try
			{
				IEnumerable<string> enumerable = existingForPrompt ?? rule.SemanticPrototypes;
				if (enumerable != null)
				{
					foreach (string item in enumerable)
					{
						string text3 = (item ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(text3))
						{
							if (list2.Count >= 12)
							{
								break;
							}
							list2.Add(text3);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("已有原型句: " + ((list2.Count > 0) ? string.Join(" | ", list2) : "（无）"));
			stringBuilder.AppendLine("注意：本次生成只允许参考“关键词”，不要参考提示词内容（Variants）。");
			stringBuilder.AppendLine("请生成 " + targetCount + " 条“语义原型句”，都必须表达同一意图但换不同说法。");
			stringBuilder.AppendLine("必须符合：");
			stringBuilder.AppendLine("1) 每条 8~30 字，中文自然口语；");
			stringBuilder.AppendLine("2) 禁止包含 ACTION 标签、系统词、解释性文字；");
			stringBuilder.AppendLine("3) 不要与“已有原型句”重复；");
			stringBuilder.AppendLine("4) 只输出 JSON 数组字符串，例如 [\"...\",\"...\"]；禁止 Markdown 代码块；");
			stringBuilder.AppendLine("5) 每条必须完整句，不要半句，不要前后引号，不要末尾逗号。");
			stringBuilder.AppendLine("6) 语义范围仅围绕关键词展开，不能引入关键词之外的新主题。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static List<string> ParsePrototypeCandidates(string raw, int maxCount)
	{
		List<string> result = new List<string>();
		if (maxCount <= 0)
		{
			maxCount = 12;
		}
		if (string.IsNullOrWhiteSpace(raw))
		{
			return result;
		}
		string text = raw.Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int num = text.IndexOf('\n');
			if (num >= 0)
			{
				int num2 = text.LastIndexOf("```", StringComparison.Ordinal);
				if (num2 > num)
				{
					text = text.Substring(num + 1, num2 - num - 1).Trim();
				}
			}
		}
		bool flag = false;
		try
		{
			JArray jArray = JArray.Parse(text);
			foreach (JToken item in jArray)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(item?.ToString() ?? "");
			}
			flag = true;
		}
		catch
		{
		}
		if (!flag)
		{
			try
			{
				JObject jObject = JObject.Parse(text);
				JArray jArray2 = null;
				if (jObject["prototypes"] is JArray jArray3)
				{
					jArray2 = jArray3;
				}
				else if (jObject["items"] is JArray jArray4)
				{
					jArray2 = jArray4;
				}
				else if (jObject["data"] is JArray jArray5)
				{
					jArray2 = jArray5;
				}
				if (jArray2 != null)
				{
					foreach (JToken item2 in jArray2)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(item2?.ToString() ?? "");
					}
					flag = true;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			try
			{
				int num3 = text.IndexOf('[');
				int num4 = text.LastIndexOf(']');
				if (num3 >= 0 && num4 > num3)
				{
					string json = text.Substring(num3, num4 - num3 + 1);
					JArray jArray6 = JArray.Parse(json);
					foreach (JToken item3 in jArray6)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(item3?.ToString() ?? "");
					}
					flag = true;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string s in array2)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(s);
			}
		}
		return result;
		void addCandidate(string raw2)
		{
			string t = NormalizePrototypeText(raw2);
			if (!string.IsNullOrWhiteSpace(t) && !result.Any((string x) => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)))
			{
				result.Add(t);
			}
		}
	}

	private static string NormalizePrototypeText(string raw)
	{
		try
		{
			string text = (raw ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.StartsWith("```", StringComparison.Ordinal) || text.Equals("```", StringComparison.Ordinal))
			{
				return string.Empty;
			}
			if (string.Equals(text, "json", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			if (string.Equals(text, "```json", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			if (text.StartsWith("`") && text.EndsWith("`") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			while (text.EndsWith(",") || text.EndsWith("，") || text.EndsWith(";") || text.EndsWith("；"))
			{
				text = text.Substring(0, text.Length - 1).Trim();
			}
			if ((text.StartsWith("\"") && (text.EndsWith("\"") || text.EndsWith("\","))) || (text.StartsWith("'") && (text.EndsWith("'") || text.EndsWith("',"))))
			{
				text = text.Trim().TrimEnd(',').Trim();
			}
			if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			if (text.StartsWith("'") && text.EndsWith("'") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			while (text.StartsWith("-") || text.StartsWith("*") || text.StartsWith("•"))
			{
				text = text.Substring(1).Trim();
			}
			int i;
			for (i = 0; i < text.Length && char.IsDigit(text[i]); i++)
			{
			}
			if (i > 0 && i < text.Length)
			{
				char c = text[i];
				if (c == '.' || c == '、' || c == ')' || c == '）' || c == ':' || c == '：')
				{
					text = text.Substring(i + 1).Trim();
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.IndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return string.Empty;
			}
			if (text.StartsWith("[") || text.EndsWith("]"))
			{
				return string.Empty;
			}
			if (text.Length < 4)
			{
				return string.Empty;
			}
			if (text.Length > 48)
			{
				text = text.Substring(0, 48).Trim();
			}
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static List<string> BuildDeterministicPrototypeFallback(LoreRule rule, int maxCount)
	{
		List<string> list = new List<string>();
		if (maxCount <= 0)
		{
			return list;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> topics = new List<string>();
		HashSet<string> topicSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (rule?.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					addTopic(keyword);
				}
			}
		}
		catch
		{
		}
		if (topics.Count > 24)
		{
			topics = topics.Take(24).ToList();
		}
		string text = ((topics.Count > 0) ? topics[0] : "这件事");
		string[] array = new string[12]
		{
			"你现在想聊{0}吗", "你是在问{0}的情况吗", "我们继续说{0}这件事", "你想确认{0}的细节吗", "这话题和{0}有关吗", "你提到的核心是{0}吗", "先把{0}说清楚", "你更关心{0}的哪一部分", "我们围绕{0}继续谈", "关于{0}你还想知道什么",
			"你是想推进{0}这个话题吗", "你希望我解释{0}吗"
		};
		foreach (string item in topics)
		{
			string[] array2 = array;
			foreach (string format in array2)
			{
				add(string.Format(format, item));
				if (list.Count >= maxCount)
				{
					return list;
				}
			}
		}
		string[] array3 = array;
		foreach (string format2 in array3)
		{
			add(string.Format(format2, text));
			if (list.Count >= maxCount)
			{
				return list;
			}
		}
		int num = 1;
		while (list.Count < maxCount && num <= 20)
		{
			add("我们继续把" + text + "讲具体一些");
			add("你现在主要在问" + text);
			add("这次对话的重点是" + text);
			num++;
		}
		return list;
		void add(string raw)
		{
			if (list.Count < maxCount)
			{
				string text2 = NormalizePrototypeText(raw);
				if (!string.IsNullOrWhiteSpace(text2) && seen.Add(text2))
				{
					list.Add(text2);
				}
			}
		}
		void addTopic(string raw)
		{
			string text2 = NormalizePrototypeText(raw);
			if (!string.IsNullOrWhiteSpace(text2) && topicSeen.Add(text2))
			{
				topics.Add(text2);
			}
		}
	}

	private static string RequestLlmTextOnce(string systemPrompt, string userPrompt, int maxTokens, float? temperature = null)
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected O, but got Unknown
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || string.IsNullOrWhiteSpace(settings.ApiKey))
		{
			throw new Exception("API Key 未配置。");
		}
		string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
		if (string.IsNullOrWhiteSpace(effectiveApiUrl))
		{
			throw new Exception("API 地址为空。");
		}
		JObject jObject = new JObject
		{
			["model"] = settings.ModelName,
			["max_tokens"] = Math.Max(96, Math.Min(800, maxTokens)),
			["stream"] = false
		};
		if (temperature.HasValue)
		{
			float num = Math.Max(0f, Math.Min(1.5f, temperature.Value));
			jObject["temperature"] = num;
		}
		JArray value = new JArray
		{
			new JObject
			{
				["role"] = "system",
				["content"] = systemPrompt ?? ""
			},
			new JObject
			{
				["role"] = "user",
				["content"] = userPrompt ?? ""
			}
		};
		jObject["messages"] = value;
		string text = jObject.ToString(Formatting.None);
		using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(45.0));
		HttpRequestMessage val = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
		try
		{
			val.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
			val.Content = (HttpContent)new StringContent(text, Encoding.UTF8, "application/json");
			HttpResponseMessage result = ((HttpMessageInvoker)DuelSettings.GlobalClient).SendAsync(val, cancellationTokenSource.Token).GetAwaiter().GetResult();
			try
			{
				string result2 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				if (!result.IsSuccessStatusCode)
				{
					string text2 = (result2 ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (text2.Length > 180)
					{
						text2 = text2.Substring(0, 180);
					}
					throw new Exception($"API请求失败: {result.StatusCode} {text2}");
				}
				try
				{
					JObject jObject2 = JObject.Parse(result2);
					string text3 = ((string)jObject2.SelectToken("choices[0].message.content")) ?? ((string)jObject2.SelectToken("choices[0].text")) ?? ((string)jObject2.SelectToken("output_text")) ?? ((string)jObject2.SelectToken("content")) ?? ((string)jObject2.SelectToken("text"));
					if (string.IsNullOrWhiteSpace(text3))
					{
						throw new Exception("API 响应为空。");
					}
					return text3.Trim();
				}
				catch (Exception ex)
				{
					throw new Exception("API 响应解析失败: " + ex.Message);
				}
			}
			finally
			{
				((IDisposable)result)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void GenerateSemanticPrototypesByLlm(LoreRule rule, Action onDone)
	{
		InformationManager.DisplayMessage(new InformationMessage("[知识] 原型句功能已停用。"));
		onDone?.Invoke();
	}

	private void OpenPrototypeRemoveMenu(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.SemanticPrototypes == null)
		{
			rule.SemanticPrototypes = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string semanticPrototype in rule.SemanticPrototypes)
		{
			string text = (semanticPrototype ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement(text, TrimPreview(text, 80), null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除原型句", "选择要删除的原型句：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPrototypeMenu(rule, onReturn);
			}
			else
			{
				string p = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(p))
				{
					rule.SemanticPrototypes.RemoveAll((string x) => string.Equals((x ?? "").Trim(), p.Trim(), StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenPrototypeMenu(rule, onReturn);
			}
		}, delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenVariantMenu(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", "新增提示词", null));
		for (int num = 0; num < rule.Variants.Count; num++)
		{
			LoreVariant loreVariant = rule.Variants[num];
			if (loreVariant != null)
			{
				string arg = BuildWhenLabel(loreVariant.When);
				string text = (loreVariant.Content ?? "").Trim();
				if (text.Length > 24)
				{
					text = text.Substring(0, 24) + "...";
				}
				string title = $"#{num + 1} {arg}  {text}";
				list.Add(new InquiryElement(num, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑提示词", "条件留空=通用；填任意条件=专属。命中时会选最具体的一条。", list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenVariantMenu(rule, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "__add__")
				{
					CreateVariant(rule, delegate
					{
						OpenVariantMenu(rule, onReturn);
					});
				}
				else if (selected[0].Identifier is int num2)
				{
					if (num2 < 0 || num2 >= rule.Variants.Count)
					{
						OpenVariantMenu(rule, onReturn);
					}
					else
					{
						OpenVariantEditor(rule, num2, delegate
						{
							OpenVariantMenu(rule, onReturn);
						});
					}
				}
				else
				{
					OpenVariantMenu(rule, onReturn);
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void CreateVariant(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		LoreVariant loreVariant = new LoreVariant();
		loreVariant.Priority = 0;
		loreVariant.When = new LoreWhen();
		loreVariant.Content = "";
		rule.Variants.Add(loreVariant);
		TouchRuleData();
		int idx = rule.Variants.Count - 1;
		OpenVariantEditor(rule, idx, onReturn);
	}

	private void OpenVariantEditor(LoreRule rule, int idx, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null || idx < 0 || idx >= rule.Variants.Count)
		{
			onReturn();
			return;
		}
		LoreVariant v = rule.Variants[idx];
		if (v == null)
		{
			onReturn();
			return;
		}
		string text = BuildWhenLabel(v.When);
		string text2 = (v.Content ?? "").Trim();
		string text3 = (text2 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		string text4 = TrimPreview(text2, 220);
		bool flag = !string.IsNullOrEmpty(text3) && text3.Length > 220;
		string text5 = "当前条件：" + text + "\n\n当前内容预览：\n" + (string.IsNullOrEmpty(text4) ? "（空）" : text4);
		if (flag)
		{
			text5 += "\n……（内容过长，仅显示前 220 字）";
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("content", "编辑提示词内容", null));
		list.Add(new InquiryElement("when", "编辑条件", null));
		list.Add(new InquiryElement("delete", "删除此提示词", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("提示词编辑", text5, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenVariantEditor(rule, idx, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "content":
					InformationManager.ShowTextInquiry(new TextInquiryData("编辑提示词内容", "请输入内容：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						v.Content = input ?? "";
						TouchRuleData();
						OpenVariantEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenVariantEditor(rule, idx, onReturn);
					}, shouldInputBeObfuscated: false, null, v.Content ?? ""));
					break;
				case "when":
					if (v.When == null)
					{
						v.When = new LoreWhen();
					}
					OpenWhenEditor(v.When, delegate
					{
						OpenVariantEditor(rule, idx, onReturn);
					});
					break;
				case "delete":
					rule.Variants.RemoveAt(idx);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenVariantEditor(rule, idx, onReturn);
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenWhenEditor(LoreWhen when, Action onReturn)
	{
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("hero", "指定NPC（HeroId）", null));
		list.Add(new InquiryElement("culture", "文化（CultureId）", null));
		list.Add(new InquiryElement("kingdom", "势力/王国（KingdomId）", null));
		list.Add(new InquiryElement("role", "身份（lord/notable/commoner/soldier/villager/townsfolk/wanderer）", null));
		list.Add(new InquiryElement("gender", "性别（不限/男/女）", null));
		list.Add(new InquiryElement("clan_leader", "是否家族族长（不限/是/否）", null));
		list.Add(new InquiryElement("skill", "技能等级（SkillId >= 值）", null));
		list.Add(new InquiryElement("clear", "清空全部条件（变成通用）", null));
		string descriptionText = BuildWhenDetail(when);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑条件", descriptionText, list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenWhenEditor(when, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "hero":
					if (when.HeroIds == null)
					{
						when.HeroIds = new List<string>();
					}
					OpenStringListEditor("指定NPC（HeroId）", when.HeroIds, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "culture":
					if (when.Cultures == null)
					{
						when.Cultures = new List<string>();
					}
					OpenStringListEditor("文化（CultureId）", when.Cultures, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "kingdom":
					if (when.KingdomIds == null)
					{
						when.KingdomIds = new List<string>();
					}
					OpenStringListEditor("势力/王国（KingdomId）", when.KingdomIds, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "role":
					OpenRoleEditor(when, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "gender":
					CycleGender(when);
					OpenWhenEditor(when, onReturn);
					break;
				case "clan_leader":
					CycleClanLeader(when);
					OpenWhenEditor(when, onReturn);
					break;
				case "skill":
					OpenSkillMinEditor(when, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "clear":
					when.HeroIds = null;
					when.Cultures = null;
					when.KingdomIds = null;
					when.Roles = null;
					when.IsFemale = null;
					when.IsClanLeader = null;
					when.SkillMin = null;
					TouchRuleData();
					OpenWhenEditor(when, onReturn);
					break;
				default:
					OpenWhenEditor(when, onReturn);
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRoleEditor(LoreWhen when, Action onReturn)
	{
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.Roles == null)
		{
			when.Roles = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("lord", (when.Roles.Contains("lord") ? "[√] " : "[ ] ") + "领主（lord）", null));
		list.Add(new InquiryElement("notable", (when.Roles.Contains("notable") ? "[√] " : "[ ] ") + "要人（notable）", null));
		list.Add(new InquiryElement("wanderer", (when.Roles.Contains("wanderer") ? "[√] " : "[ ] ") + "流浪者（wanderer）", null));
		list.Add(new InquiryElement("soldier", (when.Roles.Contains("soldier") ? "[√] " : "[ ] ") + "士兵（soldier）", null));
		list.Add(new InquiryElement("villager", (when.Roles.Contains("villager") ? "[√] " : "[ ] ") + "村民（villager）", null));
		list.Add(new InquiryElement("townsfolk", (when.Roles.Contains("townsfolk") ? "[√] " : "[ ] ") + "镇民（townsfolk）", null));
		list.Add(new InquiryElement("commoner", (when.Roles.Contains("commoner") ? "[√] " : "[ ] ") + "普通人（commoner）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("身份条件", "可多选。当前对话目标是 Hero 时，会优先按 IsLord/IsNotable 分类；否则按 Occupation（wanderer/soldier/villager/townsfolk）分类。", list, isExitShown: true, 0, 1, "切换", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRoleEditor(when, onReturn);
			}
			else
			{
				string id = selected[0].Identifier as string;
				if (string.IsNullOrWhiteSpace(id))
				{
					OpenRoleEditor(when, onReturn);
				}
				else
				{
					if (when.Roles.Contains(id))
					{
						when.Roles.RemoveAll((string x) => x == id);
					}
					else
					{
						when.Roles.Add(id);
					}
					TouchRuleData();
					OpenRoleEditor(when, onReturn);
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static void CycleGender(LoreWhen when)
	{
		if (when != null)
		{
			if (!when.IsFemale.HasValue)
			{
				when.IsFemale = false;
			}
			else if (!when.IsFemale.Value)
			{
				when.IsFemale = true;
			}
			else
			{
				when.IsFemale = null;
			}
			TouchRuleData();
		}
	}

	private static void CycleClanLeader(LoreWhen when)
	{
		if (when != null)
		{
			if (!when.IsClanLeader.HasValue)
			{
				when.IsClanLeader = true;
			}
			else if (when.IsClanLeader.Value)
			{
				when.IsClanLeader = false;
			}
			else
			{
				when.IsClanLeader = null;
			}
			TouchRuleData();
		}
	}

	private void OpenHeroIdPicker(List<string> list, Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<Hero> list2 = new List<Hero>();
		try
		{
			List<Hero> devEditableHeroListForExternal = MyBehavior.GetDevEditableHeroListForExternal();
			if (devEditableHeroListForExternal != null && devEditableHeroListForExternal.Count > 0)
			{
				list2 = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(h.StringId)).ToList();
			}
		}
		catch
		{
		}
		if (list2.Count == 0)
		{
			try
			{
				foreach (Hero allAliveHero in Hero.AllAliveHeroes)
				{
					if (allAliveHero != null && !string.IsNullOrWhiteSpace(allAliveHero.StringId))
					{
						list2.Add(allAliveHero);
					}
				}
			}
			catch
			{
			}
		}
		list2 = (from h in list2
			orderby (h.Name != null) ? h.Name.ToString() : "", h.StringId
			select h).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选NPC。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (Hero item in list2)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			string text2 = "";
			try
			{
				text2 = (item.IsLord ? "领主" : (item.IsNotable ? "要人" : ((!item.IsWanderer) ? item.Occupation.ToString() : "流浪者")));
			}
			catch
			{
				text2 = "";
			}
			list3.Add(new InquiryElement(item.StringId, text + " (" + text2 + ", " + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择NPC", "可多选，确认后加入列表。", list3, isExitShown: true, 0, list3.Count, "添加", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item2 in selected)
				{
					string id = item2.Identifier as string;
					id = (id ?? "").Trim();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenCultureIdPicker(List<string> list, Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<CultureObject> list2 = new List<CultureObject>();
		try
		{
			MBReadOnlyList<CultureObject> mBReadOnlyList = Game.Current?.ObjectManager?.GetObjectTypeList<CultureObject>();
			if (mBReadOnlyList != null)
			{
				foreach (CultureObject item in mBReadOnlyList)
				{
					if (item != null && !string.IsNullOrWhiteSpace(item.StringId) && !item.IsBandit && !(item.StringId == "neutral_culture"))
					{
						list2.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list2 = (from c in list2
			orderby (c.Name != null) ? c.Name.ToString() : "", c.StringId
			select c).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选文化。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (CultureObject item2 in list2)
		{
			string text = ((item2.Name != null) ? item2.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item2.StringId;
			}
			list3.Add(new InquiryElement(item2.StringId.ToLower(), text + " (" + item2.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择文化", "可多选，确认后加入列表。", list3, isExitShown: true, 0, list3.Count, "添加", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item3 in selected)
				{
					string id = item3.Identifier as string;
					id = (id ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenFactionIdPicker(List<string> list, Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<IFaction> list2 = new List<IFaction>();
		try
		{
			foreach (IFaction faction in Campaign.Current.Factions)
			{
				if (faction != null && !string.IsNullOrWhiteSpace(faction.StringId) && !faction.IsBanditFaction)
				{
					list2.Add(faction);
				}
			}
		}
		catch
		{
		}
		list2 = (from f in list2
			orderby !f.IsKingdomFaction, (f.Name != null) ? f.Name.ToString() : "", f.StringId
			select f).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选势力。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (IFaction item in list2)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			string text2 = (item.IsKingdomFaction ? "王国" : "势力");
			list3.Add(new InquiryElement(item.StringId.ToLower(), text + " (" + text2 + ", " + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择势力/王国", "可多选，确认后加入列表。", list3, isExitShown: true, 0, list3.Count, "添加", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item2 in selected)
				{
					string id = item2.Identifier as string;
					id = (id ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenStringListEditor(string title, List<string> list, Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		bool canPickHero = title != null && title.IndexOf("HeroId", StringComparison.OrdinalIgnoreCase) >= 0;
		bool canPickCulture = title != null && title.IndexOf("CultureId", StringComparison.OrdinalIgnoreCase) >= 0;
		bool canPickKingdom = title != null && title.IndexOf("KingdomId", StringComparison.OrdinalIgnoreCase) >= 0;
		List<InquiryElement> list2 = new List<InquiryElement>();
		if (canPickHero || canPickCulture || canPickKingdom)
		{
			list2.Add(new InquiryElement("__pick__", "从列表选择", null));
		}
		list2.Add(new InquiryElement("__add__", "手动输入（一次一个）", null));
		if (list.Count > 0)
		{
			list2.Add(new InquiryElement("__remove__", "删除", null));
		}
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				list2.Add(new InquiryElement("__v__" + item, item, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, "一次输入一个值，确定后加入列表。", list2, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "__pick__":
				{
					Action action = delegate
					{
						TouchRuleData();
						onReturn();
					};
					if (canPickHero)
					{
						OpenHeroIdPicker(list, action);
					}
					else if (canPickCulture)
					{
						OpenCultureIdPicker(list, action);
					}
					else if (canPickKingdom)
					{
						OpenFactionIdPicker(list, action);
					}
					else
					{
						action();
					}
					break;
				}
				case "__add__":
					InformationManager.ShowTextInquiry(new TextInquiryData(title, "请输入一个值：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						string v = (input ?? "").Trim();
						if (!string.IsNullOrEmpty(v) && !list.Any((string x) => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(v);
						}
						TouchRuleData();
						onReturn();
					}, delegate
					{
						onReturn();
					}));
					break;
				case "__remove__":
					OpenStringListRemoveMenu(title, list, onReturn);
					break;
				default:
					onReturn();
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenStringListRemoveMenu(string title, List<string> list, Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				list2.Add(new InquiryElement(item, item, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除 - " + title, "选择要删除的项：", list2, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string v = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(v))
				{
					list.RemoveAll((string x) => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
				}
				TouchRuleData();
				onReturn();
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinEditor(LoreWhen when, Action onReturn)
	{
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null)
		{
			when.SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__pick__", "从列表选择技能", null));
		list.Add(new InquiryElement("__add__", "手动输入 SkillId", null));
		if (when.SkillMin.Count > 0)
		{
			list.Add(new InquiryElement("__remove__", "删除技能条件", null));
		}
		foreach (KeyValuePair<string, int> item in when.SkillMin.OrderBy((KeyValuePair<string, int> k) => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = (item.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text + " >= " + item.Value, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("技能等级条件", "配置 SkillId >= 数值。数值=0 表示无门槛（Skill>=0）；数值<0 表示移除该技能条件。", list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenSkillMinEditor(when, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				switch (text2)
				{
				case "__pick__":
					OpenSkillPicker(delegate(string id)
					{
						OpenSkillMinValueInput(when, id, delegate
						{
							OpenSkillMinEditor(when, onReturn);
						});
					}, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					});
					break;
				case "__add__":
					InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 SkillId", "请输入 SkillId：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						string text3 = (input ?? "").Trim();
						if (string.IsNullOrEmpty(text3))
						{
							OpenSkillMinEditor(when, onReturn);
						}
						else
						{
							OpenSkillMinValueInput(when, text3, delegate
							{
								OpenSkillMinEditor(when, onReturn);
							});
						}
					}, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					}));
					break;
				case "__remove__":
					OpenSkillMinRemoveMenu(when, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					});
					break;
				default:
					if (!string.IsNullOrWhiteSpace(text2))
					{
						OpenSkillMinValueInput(when, text2, delegate
						{
							OpenSkillMinEditor(when, onReturn);
						});
					}
					else
					{
						OpenSkillMinEditor(when, onReturn);
					}
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinRemoveMenu(LoreWhen when, Action onReturn)
	{
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null || when.SkillMin.Count == 0)
		{
			onReturn();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (KeyValuePair<string, int> item in when.SkillMin.OrderBy((KeyValuePair<string, int> k) => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = (item.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text + " >= " + item.Value, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除技能条件", "选择要删除的项：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = (selected[0].Identifier as string) ?? "";
				text2 = text2.Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					when.SkillMin.Remove(text2);
					if (when.SkillMin.Count == 0)
					{
						when.SkillMin = null;
					}
					TouchRuleData();
				}
				onReturn();
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinValueInput(LoreWhen when, string skillId, Action onReturn)
	{
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null)
		{
			when.SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		string id = (skillId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			onReturn();
			return;
		}
		int num = 0;
		try
		{
			if (when.SkillMin.TryGetValue(id, out var value))
			{
				num = value;
			}
		}
		catch
		{
			num = 0;
		}
		InformationManager.ShowTextInquiry(new TextInquiryData("设置技能下限", "请输入最低技能等级（整数）：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
		{
			string s = (input ?? "").Trim();
			if (!int.TryParse(s, out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入有效整数。"));
				onReturn();
			}
			else
			{
				if (result < 0)
				{
					when.SkillMin.Remove(id);
					if (when.SkillMin.Count == 0)
					{
						when.SkillMin = null;
					}
				}
				else
				{
					when.SkillMin[id] = result;
				}
				TouchRuleData();
				onReturn();
			}
		}, delegate
		{
			onReturn();
		}, shouldInputBeObfuscated: false, null, num.ToString()));
	}

	private void OpenSkillPicker(Action<string> onPickSkillId, Action onReturn)
	{
		if (onPickSkillId == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<SkillObject> list = new List<SkillObject>();
		try
		{
			EnsureSkillByIdCache();
			if (_skillByIdCache != null)
			{
				foreach (SkillObject value in _skillByIdCache.Values)
				{
					if (value != null && !string.IsNullOrWhiteSpace(value.StringId))
					{
						list.Add(value);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((SkillObject s) => (s.Name != null) ? s.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((SkillObject s) => s.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选技能。"));
			onReturn();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (SkillObject item in list)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			list2.Add(new InquiryElement(item.StringId, text + " (" + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择技能", "选择一个技能后，继续设置最低等级。", list2, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = (selected[0].Identifier as string) ?? "";
				text2 = text2.Trim();
				if (string.IsNullOrEmpty(text2))
				{
					onReturn();
				}
				else
				{
					onPickSkillId(text2);
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildWhenLabel(LoreWhen when)
	{
		if (when == null)
		{
			return "通用";
		}
		List<string> list = new List<string>();
		if (when.HeroIds != null && when.HeroIds.Count > 0)
		{
			list.Add("指定NPC");
		}
		if (when.Cultures != null && when.Cultures.Count > 0)
		{
			list.Add("文化");
		}
		if (when.KingdomIds != null && when.KingdomIds.Count > 0)
		{
			list.Add("势力");
		}
		if (when.Roles != null && when.Roles.Count > 0)
		{
			list.Add("身份");
		}
		if (when.IsFemale.HasValue)
		{
			list.Add("性别");
		}
		if (when.IsClanLeader.HasValue)
		{
			list.Add("族长");
		}
		if (when.SkillMin != null && when.SkillMin.Count > 0)
		{
			list.Add("技能");
		}
		if (list.Count == 0)
		{
			return "通用";
		}
		return "专属(" + string.Join("+", list) + ")";
	}

	private static string BuildWhenDetail(LoreWhen when)
	{
		if (when == null)
		{
			return "当前：通用（无条件）";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("当前条件：");
		stringBuilder.AppendLine("HeroId: " + ListOrEmpty(when.HeroIds));
		stringBuilder.AppendLine("CultureId: " + ListOrEmpty(when.Cultures));
		stringBuilder.AppendLine("KingdomId: " + ListOrEmpty(when.KingdomIds));
		stringBuilder.AppendLine("Roles: " + ListOrEmpty(when.Roles));
		string text = "（空）";
		try
		{
			if (when.SkillMin != null && when.SkillMin.Count > 0)
			{
				text = string.Join(", ", from kv in when.SkillMin.Where((KeyValuePair<string, int> kv) => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0).OrderBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.OrdinalIgnoreCase)
					select (kv.Key ?? "").Trim() + ">=" + kv.Value);
			}
		}
		catch
		{
			text = "（空）";
		}
		stringBuilder.AppendLine("SkillMin: " + text);
		stringBuilder.AppendLine("Gender: " + ((!when.IsFemale.HasValue) ? "不限" : (when.IsFemale.Value ? "女" : "男")));
		stringBuilder.AppendLine("ClanLeader: " + ((!when.IsClanLeader.HasValue) ? "不限" : (when.IsClanLeader.Value ? "是" : "否")));
		return stringBuilder.ToString();
	}

	private static string ListOrEmpty(List<string> list)
	{
		if (list == null || list.Count == 0)
		{
			return "（空）";
		}
		return string.Join(", ", list.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

	private LoreRule FindRule(string id)
	{
		if (_file == null || _file.Rules == null)
		{
			return null;
		}
		return _file.Rules.FirstOrDefault((LoreRule r) => r != null && r.Id == id);
	}
}
