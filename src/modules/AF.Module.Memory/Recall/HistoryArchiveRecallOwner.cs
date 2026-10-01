using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Detached multi-intent retrieval. Existing ONNX caches and per-intent budgets are preserved.
internal static class HistoryArchiveRecallOwner
{
internal static string BuildHistoryQueryText(List<HistoryLineEntry> recent)
	{
		if (recent == null || recent.Count == 0)
		{
			return "";
		}
		int num = Math.Min(8, recent.Count);
		IEnumerable<string> values = from x in recent.Skip(recent.Count - num)
			select x?.Line ?? "" into x
			where !string.IsNullOrWhiteSpace(x)
			select x;
		return string.Join("\n", values);
	}

internal static List<string> SplitHistoryRecallIntents(string query, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
	{
		List<string> list = new List<string>();
		string text = (query ?? "").Replace("\r", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		list.Add(text);
		try
		{
			string[] array = Regex.Split(text, "[，。！？；：,.!?;:\\n]+");
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = (array[i] ?? "").Trim();
				if (text2.Length >= 2 && !list.Contains(text2))
				{
					list.Add(text2);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		return list;
	}

internal static List<WeightedRecallQueryInput> BuildHistoryRecallQueryInputs(List<HistoryLineEntry> recent, string currentInput, string secondaryInput)
	{
		List<WeightedRecallQueryInput> list = new List<WeightedRecallQueryInput>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = (string.IsNullOrWhiteSpace(currentInput) ? BuildHistoryQueryText(recent) : currentInput.Trim());
		appendInputs(SplitHistoryRecallIntents(text, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker);
		string text2 = (secondaryInput ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			appendInputs(SplitHistoryRecallIntents(text2, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker);
		}
		return list;

		void appendInputs(List<string> intents, int perSourceLimit)
		{
			if (intents == null || intents.Count <= 0 || perSourceLimit <= 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < intents.Count; i++)
			{
				string text3 = (intents[i] ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
				{
					WeightedRecallQueryInput weightedRecallQueryInput = new WeightedRecallQueryInput
					{
						Text = text3,
						Weight = 1f
					};
					weightedRecallQueryInput.Terms = ExtractQueryTerms(text3);
					list.Add(weightedRecallQueryInput);
					num++;
					if (num >= perSourceLimit || list.Count >= IntentQueryOptimizer.MaxCombinedIntentCount)
					{
						break;
					}
				}
			}
		}
	}

internal static List<RecallLineScore> FindHistoryCandidateScores(List<HistoryLineEntry> older, WeightedRecallQueryInput queryInput, int topK, out bool onnxUsed)
	{
		onnxUsed = false;
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			if (older == null || older.Count == 0 || queryInput == null || string.IsNullOrWhiteSpace(queryInput.Text) || topK <= 0)
			{
				return list;
			}
			List<HistoryLineEntry> list2 = older.Where((HistoryLineEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Line)).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			if (list2.Count > 260)
			{
				list2 = list2.Skip(list2.Count - 260).ToList();
			}
			int count = list2.Count;
			float[] array = null;
			OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
			if (instance != null && instance.IsAvailable && queryInput.Text.Trim().Length >= 2 && instance.TryGetEmbedding(queryInput.Text, out var vector) && vector != null && vector.Length != 0)
			{
				array = vector;
				onnxUsed = true;
			}
			for (int i = 0; i < count; i++)
			{
				HistoryLineEntry historyLineEntry = list2[i];
				string text = historyLineEntry.Line ?? "";
				double num = ((count <= 1) ? 1.0 : ((double)i / (double)(count - 1)));
				double num2 = (IsSystemFactLine(text) ? 1.0 : (ContainsStructuredSignal(text) ? 0.78 : 0.35));
				double num3 = ComputeLexicalOverlapScore(text, queryInput.Terms) * (double)Math.Max(0f, queryInput.Weight);
				double num4 = 0.6 * num2 + 0.3 * num + 0.1 * num3;
				double num5 = num4;
				if (array != null)
				{
					string text2 = text.Trim();
					if (text2.Length > 200)
					{
						text2 = text2.Substring(0, 200);
					}
					if (!string.IsNullOrWhiteSpace(text2) && instance.TryGetEmbedding(text2, out var vector2) && vector2 != null && vector2.Length != 0)
					{
						int num6 = Math.Min(array.Length, vector2.Length);
						double num7 = 0.0;
						for (int j = 0; j < num6; j++)
						{
							num7 += (double)array[j] * (double)vector2[j];
						}
						double num8 = (num7 + 1.0) * 0.5 * (double)Math.Max(0f, queryInput.Weight);
						if (num8 < 0.0)
						{
							num8 = 0.0;
						}
						if (num8 > 1.0)
						{
							num8 = 1.0;
						}
						num5 = num8 * 0.9 + num4 * 0.1;
					}
				}
				list.Add(new RecallLineScore
				{
					Entry = historyLineEntry,
					RawScore = num5,
					BaseScore = num4,
					RerankScore = num5
				});
			}
			list = (from x in list
				orderby x.RawScore descending, x.BaseScore descending, (x.Entry != null) ? x.Entry.Index : (-1) descending
				select x).Take(topK).ToList();
		}
		catch
		{
		}
		return list;
	}

internal static List<RecallLineScore> RerankHistoryCandidateScores(string input, List<RecallLineScore> recalled, int rerankTopK, out bool rerankUsed, float scoreWeight = 1f)
	{
		rerankUsed = false;
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			List<RecallLineScore> list2 = (recalled ?? new List<RecallLineScore>()).Where((RecallLineScore x) => x?.Entry != null).OrderByDescending((RecallLineScore x) => x.RawScore).ThenByDescending((RecallLineScore x) => x.BaseScore).ThenByDescending((RecallLineScore x) => x.Entry?.Index ?? (-1)).ToList();
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
			OnnxCrossEncoderReranker onnxCrossEncoderReranker = null;
			bool flag = false;
			try
			{
				onnxCrossEncoderReranker = OnnxCrossEncoderReranker.Instance;
				flag = onnxCrossEncoderReranker != null && onnxCrossEncoderReranker.IsAvailable;
			}
			catch
			{
				flag = false;
			}
			rerankUsed = flag;
			List<string> list3 = null;
			List<float> list4 = null;
			bool flag2 = false;
			if (flag)
			{
				list3 = new List<string>(list2.Count);
				for (int i = 0; i < list2.Count; i++)
				{
					list3.Add((list2[i]?.Entry == null) ? "" : BuildHistoryRerankText(list2[i].Entry));
				}
				flag2 = onnxCrossEncoderReranker.TryScoreBatch(input, list3, out list4) && list4 != null && list4.Count == list2.Count;
			}
			rerankUsed = flag && flag2;
			double num2 = (double)Math.Max(0f, scoreWeight);
			for (int i = 0; i < list2.Count; i++)
			{
				RecallLineScore recallLineScore = list2[i];
				if (recallLineScore?.Entry == null)
				{
					continue;
				}
				double num3 = recallLineScore.RawScore;
				double num4 = num3;
				if (flag && flag2 && list3 != null && i < list3.Count && !string.IsNullOrWhiteSpace(list3[i]) && list4 != null && i < list4.Count)
				{
					num4 = (double)list4[i] * num2;
				}
				list.Add(new RecallLineScore
				{
					Entry = recallLineScore.Entry,
					RawScore = num3,
					BaseScore = recallLineScore.BaseScore,
					RerankScore = num4
				});
			}
			list = SelectHistoryCandidateScores(list, (flag && flag2) ? "cross_encoder" : "recall_fallback", input, num);
		}
		catch
		{
		}
		return list;
	}

internal static List<RecallLineScore> SelectHistoryCandidateScores(List<RecallLineScore> scored, string source, string input, int topK)
	{
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			int num = ((topK <= 0) ? 4 : topK);
			double num2 = 0.21;
			List<RecallLineScore> list2 = (from x in scored
				where x?.Entry != null && !double.IsNaN(x.RerankScore)
				orderby x.RerankScore descending, x.BaseScore descending, (x.Entry != null) ? x.Entry.Index : (-1) descending
				select x).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			double num3 = ((list2.Count > 0) ? list2[0].RerankScore : 0.0);
			double num4 = ((list2.Count > 1) ? list2[1].RerankScore : 0.0);
			double num5 = ((list2.Count > 0) ? list2[0].BaseScore : 0.0);
			double num6 = ((list2.Count > 1) ? list2[1].BaseScore : 0.0);
			HashSet<int> hashSet = new HashSet<int>();
			int num7 = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				RecallLineScore recallLineScore = list2[i];
				int num8 = recallLineScore?.Entry?.Index ?? int.MinValue;
				if (num8 == int.MinValue || recallLineScore.RerankScore < num2 || !hashSet.Add(num8))
				{
					continue;
				}
				list.Add(recallLineScore);
				num7++;
			}
			if (list.Count < num)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					if (list.Count >= num)
					{
						break;
					}
					RecallLineScore recallLineScore2 = list2[j];
					int num9 = recallLineScore2?.Entry?.Index ?? int.MinValue;
					if (num9 != int.MinValue && hashSet.Add(num9))
					{
						list.Add(recallLineScore2);
					}
				}
			}
			try
			{
				Logger.Log("DialogueHistory", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
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

internal static string BuildHistoryRerankText(HistoryLineEntry entry)
	{
		string text = (entry?.Line ?? "").Trim();
		if (text.Length > 220)
		{
			text = text.Substring(0, 220);
		}
		return text;
	}

internal static int GetHistoryRerankBudget(int returnCap)
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

internal static int GetHistoryPerIntentRerank(int rerankBudget, int intentCount)
	{
		int num = ((intentCount > 0) ? intentCount : 1);
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

internal static int GetHistoryPerIntentRecall(int rerankPerIntent)
	{
		int num = (int)Math.Round((double)rerankPerIntent * 2.5, MidpointRounding.AwayFromZero);
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

internal static List<string> ExtractQueryTerms(string query)
	{
		List<string> terms = new List<string>();
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return terms;
		}
		StringBuilder cur = new StringBuilder();
		Action action = delegate
		{
			if (cur.Length < 2)
			{
				cur.Clear();
			}
			else
			{
				string item = cur.ToString();
				if (!terms.Contains(item))
				{
					terms.Add(item);
				}
				cur.Clear();
			}
		};
		foreach (char c in text)
		{
			if (IsTermChar(c))
			{
				cur.Append(c);
			}
			else
			{
				action();
			}
		}
		action();
		if (terms.Count > 24)
		{
			terms = terms.Take(24).ToList();
		}
		return terms;
	}

internal static bool IsTermChar(char c)
	{
		return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= '\u4E00' && c <= '\u9FFF');
	}

internal static double ComputeLexicalOverlapScore(string text, List<string> terms)
	{
		if (string.IsNullOrWhiteSpace(text) || terms == null || terms.Count <= 0)
		{
			return 0.0;
		}
		int num = 0;
		for (int i = 0; i < terms.Count; i++)
		{
			string value = terms[i];
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num++;
			}
		}
		if (num <= 0)
		{
			return 0.0;
		}
		double num2 = (double)num / (double)Math.Max(1, terms.Count);
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > 1.0)
		{
			num2 = 1.0;
		}
		return num2;
	}

internal static bool ContainsStructuredSignal(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("债务id") || text2.Contains("欠款") || text2.Contains("还款") || text2.Contains("赊账") || text2.Contains("借款") || text2.Contains("第纳尔") || text2.Contains("价格") || text2.Contains("交易") || text2.Contains("决斗") || text2.Contains("赌注") || text2.Contains("[action:") || text2.Contains("guideprice=") || text2.Contains("|") || text2.Contains("id:"))
		{
			return true;
		}
		foreach (char c in text)
		{
			if (c >= '0' && c <= '9')
			{
				return true;
			}
		}
		return false;
	}

internal static bool IsSystemFactLine(string line)
	{
		string text = (line ?? "").TrimStart();
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		return text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal);
	}

internal static int ClampHistoryReturnCap(int value)
	{
		if (value < 1)
		{
			value = 1;
		}
		if (value > 12)
		{
			value = 12;
		}
		return value;
	}

internal static List<ArchiveHit> FindRelevantArchiveHits(List<HistoryLineEntry> older, List<WeightedRecallQueryInput> queryInputs, int returnCap, out bool onnxUsed, out string matchMode, out int rerankPerIntent, out int recallPerIntent)
	{
		onnxUsed = false;
		matchMode = "none";
		rerankPerIntent = 0;
		recallPerIntent = 0;
		List<ArchiveHit> list = new List<ArchiveHit>();
		try
		{
			List<WeightedRecallQueryInput> list2 = (queryInputs ?? new List<WeightedRecallQueryInput>()).Where((WeightedRecallQueryInput x) => x != null && !string.IsNullOrWhiteSpace(x.Text) && x.Weight > 0f).ToList();
			if (older == null || older.Count == 0 || returnCap <= 0 || list2.Count == 0)
			{
				return list;
			}
			List<HistoryLineEntry> list3 = older.Where((HistoryLineEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Line)).ToList();
			if (list3.Count <= 0)
			{
				return list;
			}
			int num = ClampHistoryReturnCap(returnCap);
			int num2 = Math.Max(1, list2.Count);
			int historyRerankBudget = GetHistoryRerankBudget(num);
			rerankPerIntent = GetHistoryPerIntentRerank(historyRerankBudget, num2);
			recallPerIntent = GetHistoryPerIntentRecall(rerankPerIntent);
			bool flag = false;
			try
			{
				flag = OnnxCrossEncoderReranker.Instance.IsAvailable;
			}
			catch
			{
				flag = false;
			}
			matchMode = (flag ? ((num2 > 1) ? "rerank_multi" : "rerank") : ((num2 > 1) ? "semantic_multi" : "semantic"));
			Dictionary<int, HistoryRecallAggregate> dictionary = new Dictionary<int, HistoryRecallAggregate>();
			for (int i = 0; i < list2.Count; i++)
			{
				WeightedRecallQueryInput weightedRecallQueryInput = list2[i];
				List<RecallLineScore> list4 = FindHistoryCandidateScores(list3, weightedRecallQueryInput, recallPerIntent, out var onnxUsed2);
				if (onnxUsed2)
				{
					onnxUsed = true;
				}
				if (list4 == null || list4.Count <= 0)
				{
					continue;
				}
				List<RecallLineScore> list5 = RerankHistoryCandidateScores(weightedRecallQueryInput.Text, list4, rerankPerIntent, out var _, weightedRecallQueryInput.Weight);
				if (list5 == null || list5.Count <= 0)
				{
					continue;
				}
				for (int j = 0; j < list5.Count; j++)
				{
					RecallLineScore recallLineScore = list5[j];
					int num3 = recallLineScore?.Entry?.Index ?? int.MinValue;
					if (num3 == int.MinValue)
					{
						continue;
					}
					double num4 = recallLineScore.RerankScore;
					if (!dictionary.TryGetValue(num3, out var value))
					{
						value = new HistoryRecallAggregate
						{
							Hit = new ArchiveHit
							{
								Entry = recallLineScore.Entry,
								Score = num4,
								BaseScore = recallLineScore.BaseScore,
								RerankScore = recallLineScore.RerankScore
							},
							ScoreSum = num4,
							HitCount = 1,
							BestRank = j + 1,
							BestScore = recallLineScore.RerankScore
						};
						dictionary[num3] = value;
						continue;
					}
					value.ScoreSum += num4;
					value.HitCount++;
					if (j + 1 < value.BestRank)
					{
						value.BestRank = j + 1;
					}
					if (recallLineScore.RerankScore >= value.BestScore)
					{
						value.BestScore = recallLineScore.RerankScore;
						value.Hit.Entry = recallLineScore.Entry;
						value.Hit.BaseScore = recallLineScore.BaseScore;
						value.Hit.RerankScore = recallLineScore.RerankScore;
					}
					dictionary[num3] = value;
				}
			}
			if (dictionary.Count <= 0)
			{
				return list;
			}
			List<ArchiveHit> list6 = (from x in dictionary.Values
				let finalScore = Math.Min(1.0, x.ScoreSum / (double)Math.Max(1, x.HitCount) + (double)(x.HitCount - 1) * 0.08)
				orderby finalScore descending, x.BestRank, (x.Hit?.Entry != null) ? x.Hit.Entry.Index : (-1) descending
				select new ArchiveHit
				{
					Entry = x.Hit.Entry,
					Score = finalScore,
					BaseScore = x.Hit.BaseScore,
					RerankScore = x.Hit.RerankScore
				}).ToList();
			HashSet<int> hashSet = new HashSet<int>();
			for (int k = 0; k < list6.Count; k++)
			{
				ArchiveHit archiveHit = list6[k];
				int num5 = archiveHit?.Entry?.Index ?? int.MinValue;
				if (num5 != int.MinValue && !hashSet.Contains(num5))
				{
					list.Add(archiveHit);
					hashSet.Add(num5 - 1);
					hashSet.Add(num5);
					hashSet.Add(num5 + 1);
					if (list.Count >= num)
					{
						break;
					}
				}
			}
			try
			{
				Logger.Log("DialogueHistory", $"candidate_pool mode={matchMode} returnCap={num} rerankBudget={historyRerankBudget} rerankPerIntent={rerankPerIntent} recallPerIntent={recallPerIntent} intents={num2} got={list.Count}");
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
}

internal sealed class HistoryLineEntry
	{
		public int Day;

		public string Date;

		public string Line;

		public int Index;
	}

internal sealed class ArchiveHit
	{
		public HistoryLineEntry Entry;

		public double Score;

		public double BaseScore;

		public double RerankScore;
	}

internal sealed class WeightedRecallQueryInput
	{
		public string Text;

		public float Weight;

		public List<string> Terms = new List<string>();
	}

internal sealed class RecallLineScore
	{
		public HistoryLineEntry Entry;

		public double RawScore;

		public double BaseScore;

		public double RerankScore;
	}

internal sealed class HistoryRecallAggregate
	{
		public ArchiveHit Hit;

		public double ScoreSum;

		public int HitCount;

		public int BestRank = int.MaxValue;

		public double BestScore;
	}
