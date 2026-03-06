using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace AnimusForge;

public static class AIConfigHandler
{
	private sealed class GuardrailRuleEval
	{
		public string RuleTag;

		public string MatchedSeed;

		public string MatchedIntent;

		public float RawInput;

		public float RawContext;

		public float MixedRaw;

		public float AmpScore;

		public float Delta;

		public float Mean;

		public float MaxOther;

		public string MaxOtherTag;

		public int Rank;

		public bool Candidate;

		public bool AbsHit;

		public bool RelHit;

		public bool HighAmpHit;

		public bool ForceHit;

		public float TopGap;

		public float IntentEvidence;

		public float IntentGate;

		public string IntentSeed;

		public bool LexicalAnchor;

		public string RejectReason;

		public bool Hit;
	}

	private sealed class GuardrailEvalSnapshot
	{
		public string Key;

		public Dictionary<string, GuardrailRuleEval> Rules = new Dictionary<string, GuardrailRuleEval>(StringComparer.OrdinalIgnoreCase);
	}

	private struct GuardrailGateProfile
	{
		public float AmpGate;

		public float ForceHitGate;

		public float RawFloor;

		public float GapBoost;

		public float CenterBoost;

		public float TopGapGate;

		public float AnchorRawFloor;
	}

	private static AIConfigModel _config;

	private static GuardrailConfigModel _guardrail;

	private static readonly object _guardrailSemanticLock = new object();

	private static readonly Dictionary<string, float[]> _guardrailPhraseVecCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private static readonly Dictionary<string, float[]> _guardrailInputVecCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

	private const int GuardrailPhraseVecCacheMax = 1024;

	private const int GuardrailInputVecCacheMax = 256;

	private static readonly AsyncLocal<string> _guardrailSemanticRuntimeContext = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetKingdomId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetHeroId = new AsyncLocal<string>();

	private static GuardrailEvalSnapshot _lastGuardrailEval;

	public static string GlobalPrompt => _guardrail?.GlobalPrompt ?? "";

	public static string GlobalGuardrail => _guardrail?.GlobalGuardrail ?? "";

	public static string DuelInstruction => _guardrail?.Duel?.TriggerInstruction ?? "";

	public static List<string> DuelTriggerKeywords => _guardrail?.Duel?.AcceptKeywords ?? new List<string>();

	public static bool RewardEnabled => _guardrail?.Reward?.IsEnabled == true;

	public static string RewardInstruction => _guardrail?.Reward?.Instruction ?? "";

	public static List<string> RewardTriggerKeywords => _guardrail?.Reward?.TriggerKeywords ?? new List<string>();

	public static bool LoanEnabled => _guardrail?.Loan?.IsEnabled == true;

	public static string LoanInstruction => _guardrail?.Loan?.Instruction ?? "";

	public static List<string> LoanTriggerKeywords => _guardrail?.Loan?.TriggerKeywords ?? new List<string>();

	public static bool SurroundingsEnabled => _guardrail?.Surroundings?.IsEnabled == true;

	public static string SurroundingsInstruction => _guardrail?.Surroundings?.Instruction ?? "";

	public static List<string> SurroundingsTriggerKeywords => _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>();

	public static string DuelNonHeroInstruction => _guardrail?.Duel?.NonHeroInstruction ?? "";

	public static string RewardNonHeroInstruction => _guardrail?.Reward?.NonHeroInstruction ?? "";

	public static string LoanNonHeroInstruction => _guardrail?.Loan?.NonHeroInstruction ?? "";

	private static bool GuardrailKnowledgeEnabled => _guardrail?.KnowledgeRetrieval?.IsEnabled ?? true;

	private static bool GuardrailKnowledgeSemanticFirst => _guardrail?.KnowledgeRetrieval?.SemanticFirst ?? true;

	private static bool GuardrailKnowledgeKeywordFallback => _guardrail?.KnowledgeRetrieval?.EnableKeywordFallback ?? true;

	private static int GuardrailKnowledgeTopK => ClampKnowledgeTopK(_guardrail?.KnowledgeRetrieval?.SemanticTopK ?? 2);

	public static bool KnowledgeRetrievalEnabled
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var enabled, out var _, out var _, out var _))
			{
				return enabled;
			}
			return GuardrailKnowledgeEnabled;
		}
	}

	public static bool KnowledgeSemanticFirst
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var semanticFirst, out var _, out var _))
			{
				return semanticFirst;
			}
			return GuardrailKnowledgeSemanticFirst;
		}
	}

	public static bool KnowledgeKeywordFallback
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var _, out var keywordFallback, out var _))
			{
				return keywordFallback;
			}
			return GuardrailKnowledgeKeywordFallback;
		}
	}

	public static int KnowledgeSemanticTopK
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var _, out var _, out var topK))
			{
				return topK;
			}
			return GuardrailKnowledgeTopK;
		}
	}

	public static float KnowledgeSemanticMinScore
	{
		get
		{
			try
			{
				return _guardrail?.KnowledgeRetrieval?.SemanticMinScore ?? 0.21f;
			}
			catch
			{
				return 0.21f;
			}
		}
	}

	public static bool KnowledgeRetrievalFromMcm => UseMcmKnowledgeRetrieval();

	public static bool DuelStakeEnabled => _guardrail?.DuelStake?.IsEnabled == true;

	public static string DuelStakePlayerWinInstruction => _guardrail?.DuelStake?.PlayerWinInstruction ?? "";

	public static string DuelStakeNpcWinInstruction => _guardrail?.DuelStake?.NpcWinInstruction ?? "";

	public static string DuelStakeInstruction
	{
		get
		{
			DuelStakeConfig duelStakeConfig = _guardrail?.DuelStake;
			if (duelStakeConfig == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrEmpty(duelStakeConfig.PlayerWinInstruction))
			{
				stringBuilder.AppendLine(duelStakeConfig.PlayerWinInstruction);
			}
			if (!string.IsNullOrEmpty(duelStakeConfig.NpcWinInstruction))
			{
				stringBuilder.AppendLine(duelStakeConfig.NpcWinInstruction);
			}
			return stringBuilder.ToString().Trim();
		}
	}

	private static string NormalizeSemanticText(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text2.Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static List<string> SplitGuardrailIntents(string input, int maxParts = 3)
	{
		List<string> list = new List<string>();
		try
		{
			string text = NormalizeSemanticText(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			List<string> list2 = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in text)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、' || c == '\n' || c == '\r')
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
				string text8 = NormalizeSemanticText(list3[l]);
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
			string text9 = NormalizeSemanticText(input);
			if (!string.IsNullOrWhiteSpace(text9))
			{
				list.Add(text9);
			}
		}
		return list;
	}

	private static float DotProductNormalized(float[] a, float[] b)
	{
		try
		{
			if (a == null || b == null || a.Length == 0 || b.Length == 0)
			{
				return 0f;
			}
			int num = Math.Min(a.Length, b.Length);
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

	private static GuardrailGateProfile GetGuardrailGateProfile(string ruleTag, int inputLen)
	{
		GuardrailGateProfile result = new GuardrailGateProfile
		{
			AmpGate = 0.53f,
			ForceHitGate = 0.56f,
			RawFloor = 0.44f,
			GapBoost = 2.35f,
			CenterBoost = 0.72f,
			TopGapGate = 0.045f,
			AnchorRawFloor = 0.62f
		};
		switch ((ruleTag ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			result.AmpGate = 0.57f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.45f;
			result.CenterBoost = 0.78f;
			result.TopGapGate = 0.055f;
			result.AnchorRawFloor = 0.65f;
			break;
		case "reward":
			result.AmpGate = 0.5f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.55f;
			result.CenterBoost = 0.8f;
			result.TopGapGate = 0.035f;
			result.AnchorRawFloor = 0.45f;
			break;
		case "loan":
			result.AmpGate = 0.58f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.6f;
			result.CenterBoost = 0.8f;
			result.TopGapGate = 0.06f;
			result.AnchorRawFloor = 0.67f;
			break;
		case "surroundings":
			result.AmpGate = 0.54f;
			result.RawFloor = 0.44f;
			result.GapBoost = 2.3f;
			result.CenterBoost = 0.7f;
			result.TopGapGate = 0.05f;
			result.AnchorRawFloor = 0.63f;
			break;
		}
		if (result.AmpGate < 0.3f)
		{
			result.AmpGate = 0.3f;
		}
		if (result.AmpGate > 0.95f)
		{
			result.AmpGate = 0.95f;
		}
		if (result.ForceHitGate < 0f)
		{
			result.ForceHitGate = 0f;
		}
		if (result.ForceHitGate > 1f)
		{
			result.ForceHitGate = 1f;
		}
		if (result.RawFloor < 0.1f)
		{
			result.RawFloor = 0.1f;
		}
		if (result.RawFloor > 0.9f)
		{
			result.RawFloor = 0.9f;
		}
		if (result.TopGapGate < 0f)
		{
			result.TopGapGate = 0f;
		}
		if (result.TopGapGate > 0.3f)
		{
			result.TopGapGate = 0.3f;
		}
		if (result.AnchorRawFloor < 0.1f)
		{
			result.AnchorRawFloor = 0.1f;
		}
		if (result.AnchorRawFloor > 0.95f)
		{
			result.AnchorRawFloor = 0.95f;
		}
		return result;
	}

	private static List<string> GetBuiltInIntentAnchorSeeds(string ruleTag)
	{
		List<string> list = new List<string>();
		try
		{
			string text = (ruleTag ?? "").Trim().ToLowerInvariant();
			List<string> guardrailKeywordsByTag = GetGuardrailKeywordsByTag(ruleTag);
			if (guardrailKeywordsByTag != null && guardrailKeywordsByTag.Count > 0)
			{
				list.AddRange(guardrailKeywordsByTag);
			}
			switch (text)
			{
			case "reward":
				list.Add("我想和你做点生意");
				list.Add("我想和你交易");
				list.Add("我们谈谈买卖");
				list.Add("我想买东西");
				list.Add("我想卖东西");
				list.Add("看看你有什么货");
				list.Add("谈个价格");
				list.Add("交换物品");
				break;
			case "loan":
				list.Add("我想借钱周转");
				list.Add("我想赊账");
				list.Add("我欠你钱");
				list.Add("还款期限怎么定");
				list.Add("谈还款日");
				break;
			case "duel":
				list.Add("我想和你决斗");
				list.Add("我们单挑");
				list.Add("来比试一场");
				list.Add("你敢不敢决斗");
				break;
			case "surroundings":
				list.Add("这里是哪里");
				list.Add("附近有什么地方");
				list.Add("离哪座城最近");
				list.Add("这地方属于谁");
				list.Add("往北往南有什么");
				break;
			}
		}
		catch
		{
		}
		return NormalizeStringList(list, 96);
	}

	private static float GetBuiltInIntentEvidenceGate(string ruleTag, int inputLen)
	{
		float num = 0.52f;
		switch ((ruleTag ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			num = 0.52f;
			break;
		case "reward":
			num = 0.47f;
			break;
		case "loan":
			num = 0.52f;
			break;
		case "surroundings":
			num = 0.56f;
			break;
		}
		if (num < 0.2f)
		{
			num = 0.2f;
		}
		if (num > 0.92f)
		{
			num = 0.92f;
		}
		return num;
	}

	private static float ComputeBuiltInIntentSemanticEvidence(string ruleTag, List<float[]> qvecs, out string bestSeed)
	{
		bestSeed = "";
		try
		{
			if (qvecs == null || qvecs.Count <= 0)
			{
				return 0f;
			}
			List<string> builtInIntentAnchorSeeds = GetBuiltInIntentAnchorSeeds(ruleTag);
			if (builtInIntentAnchorSeeds == null || builtInIntentAnchorSeeds.Count <= 0)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < builtInIntentAnchorSeeds.Count; i++)
			{
				string text = NormalizeSemanticText(builtInIntentAnchorSeeds[i]);
				if (string.IsNullOrWhiteSpace(text) || !TryGetPhraseEmbedding(text, out var vec) || vec == null || vec.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < qvecs.Count; j++)
				{
					float num2 = DotProductNormalized(qvecs[j], vec);
					if (num2 > num)
					{
						num = num2;
						bestSeed = text;
					}
				}
			}
			return num;
		}
		catch
		{
			return 0f;
		}
	}

	private static float ApplyGuardrailAmplifiedScore(float raw, float maxOther, float meanAll, GuardrailGateProfile p)
	{
		float num = ((maxOther <= -0.5f) ? raw : (raw - maxOther));
		float num2 = raw - meanAll;
		float num3 = raw + num * p.GapBoost + num2 * p.CenterBoost;
		if (num3 < 0f)
		{
			num3 = 0f;
		}
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		return num3;
	}

	public static void SetGuardrailSemanticContext(string contextText)
	{
		try
		{
			_guardrailSemanticRuntimeContext.Value = NormalizeSemanticText(contextText);
		}
		catch
		{
		}
	}

	private static string GetRuntimeGuardrailContext()
	{
		try
		{
			string text = NormalizeSemanticText(_guardrailSemanticRuntimeContext.Value);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			if (text.Length > 600)
			{
				text = text.Substring(text.Length - 600);
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static bool IsBuiltInRuleTag(string tag)
	{
		string text = (tag ?? "").Trim().ToLowerInvariant();
		int result;
		switch (text)
		{
		default:
			result = ((text == "surroundings") ? 1 : 0);
			break;
		case "duel":
		case "reward":
		case "loan":
			result = 1;
			break;
		}
		return (byte)result != 0;
	}

	private static List<string> NormalizeStringList(List<string> source, int maxLen = 80)
	{
		List<string> list = new List<string>();
		try
		{
			if (source == null || source.Count <= 0)
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < source.Count; i++)
			{
				string text = NormalizeSemanticText(source[i]);
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (maxLen > 0 && text.Length > maxLen)
					{
						text = text.Substring(0, maxLen);
					}
					if (hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static List<string> NormalizeTriggerKeywordList(List<string> source, int minLen = 2, int maxLen = 8)
	{
		List<string> list = new List<string>();
		try
		{
			if (source == null || source.Count <= 0)
			{
				return list;
			}
			if (minLen < 1)
			{
				minLen = 1;
			}
			if (maxLen < minLen)
			{
				maxLen = minLen;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < source.Count; i++)
			{
				string text = NormalizeSemanticText(source[i]);
				if (!string.IsNullOrWhiteSpace(text) && text.Length >= minLen)
				{
					if (text.Length > maxLen)
					{
						text = text.Substring(0, maxLen);
					}
					if (hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static Dictionary<string, string> NormalizeTemplateMap(Dictionary<string, string> source, int maxKeyLen = 80)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (source == null || source.Count <= 0)
			{
				return dictionary;
			}
			foreach (KeyValuePair<string, string> item in source)
			{
				string text = NormalizeSemanticText(item.Key);
				string text2 = (item.Value ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (maxKeyLen > 0 && text.Length > maxKeyLen)
				{
					text = text.Substring(0, maxKeyLen);
				}
				dictionary[text.ToLowerInvariant()] = text2;
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static GuardrailRulePromptConfig BuildLegacyRulePrompt(string id, bool enabled, string instruction, List<string> triggerKeywords, string group, int priority)
	{
		return new GuardrailRulePromptConfig
		{
			Id = (id ?? "").Trim().ToLowerInvariant(),
			IsEnabled = enabled,
			Instruction = (instruction ?? ""),
			TriggerKeywords = NormalizeTriggerKeywordList(triggerKeywords),
			Group = (group ?? "").Trim(),
			Priority = priority
		};
	}

	private static GuardrailRulePromptConfig NormalizeCustomRulePrompt(GuardrailRulePromptConfig src, int autoIndex)
	{
		try
		{
			if (src == null)
			{
				return null;
			}
			string text = (src.Id ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "rule_" + autoIndex;
			}
			return new GuardrailRulePromptConfig
			{
				Id = text,
				IsEnabled = src.IsEnabled,
				Group = (src.Group ?? "").Trim(),
				Priority = src.Priority,
				Instruction = (src.Instruction ?? ""),
				NonHeroInstruction = (src.NonHeroInstruction ?? ""),
				TriggerKeywords = NormalizeTriggerKeywordList(src.TriggerKeywords),
				RuntimeInstructionTemplates = NormalizeTemplateMap(src.RuntimeInstructionTemplates),
				RuntimeConstraintTemplates = NormalizeTemplateMap(src.RuntimeConstraintTemplates)
			};
		}
		catch
		{
			return null;
		}
	}

	private static Dictionary<string, GuardrailRulePromptConfig> BuildRulePromptRegistry()
	{
		Dictionary<string, GuardrailRulePromptConfig> map = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
		try
		{
			upsert(BuildLegacyRulePrompt("duel", _guardrail?.Duel?.IsEnabled ?? true, _guardrail?.Duel?.TriggerInstruction ?? "", _guardrail?.Duel?.AcceptKeywords ?? new List<string>(), "combat", 90));
			upsert(BuildLegacyRulePrompt("reward", _guardrail?.Reward?.IsEnabled ?? true, _guardrail?.Reward?.Instruction ?? "", _guardrail?.Reward?.TriggerKeywords ?? new List<string>(), "trade", 80));
			upsert(BuildLegacyRulePrompt("loan", _guardrail?.Loan?.IsEnabled ?? true, _guardrail?.Loan?.Instruction ?? "", _guardrail?.Loan?.TriggerKeywords ?? new List<string>(), "finance", 85));
			upsert(BuildLegacyRulePrompt("surroundings", _guardrail?.Surroundings?.IsEnabled ?? true, _guardrail?.Surroundings?.Instruction ?? "", _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>(), "world", 70));
			if (_guardrail?.RulePrompts != null && _guardrail.RulePrompts.Count > 0)
			{
				for (int i = 0; i < _guardrail.RulePrompts.Count; i++)
				{
					GuardrailRulePromptConfig rule = NormalizeCustomRulePrompt(_guardrail.RulePrompts[i], i + 1);
					upsert(rule);
				}
			}
		}
		catch
		{
		}
		return map;
		void upsert(GuardrailRulePromptConfig guardrailRulePromptConfig)
		{
			if (guardrailRulePromptConfig != null)
			{
				string text = (guardrailRulePromptConfig.Id ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					guardrailRulePromptConfig.Id = text;
					map[text] = guardrailRulePromptConfig;
				}
			}
		}
	}

	private static List<GuardrailRulePromptConfig> GetAllEnabledRulePrompts()
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			return (from r in dictionary.Values
				where r != null && r.IsEnabled && !string.IsNullOrWhiteSpace(r.Id)
				orderby r.Priority descending
				select r).ThenBy((GuardrailRulePromptConfig r) => r.Id, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return new List<GuardrailRulePromptConfig>();
		}
	}

	private static GuardrailRulePromptConfig GetRulePromptByTag(string ruleTag)
	{
		try
		{
			string text = (ruleTag ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary.TryGetValue(text, out var value) && value != null)
			{
				return value;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetGuardrailInstructionByTag(string ruleTag)
	{
		return GetRulePromptByTag(ruleTag)?.Instruction ?? "";
	}

	private static List<string> GetGuardrailKeywordsByTag(string ruleTag)
	{
		return GetRulePromptByTag(ruleTag)?.TriggerKeywords ?? new List<string>();
	}

	public static string GetGuardrailRuleInstruction(string ruleTag)
	{
		return GetGuardrailInstructionByTag(ruleTag);
	}

	public static List<string> GetGuardrailRuleKeywords(string ruleTag)
	{
		List<string> guardrailKeywordsByTag = GetGuardrailKeywordsByTag(ruleTag);
		return (guardrailKeywordsByTag == null) ? new List<string>() : new List<string>(guardrailKeywordsByTag);
	}

	private static string BuildRuleInstructionSeed(string ruleTag, string ruleInstruction)
	{
		string text = NormalizeSemanticText(ruleTag);
		string text2 = NormalizeSemanticText(ruleInstruction);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		int num = text2.IndexOfAny(new char[9] { '。', '！', '!', '？', '?', '\n', '\r', ';', '；' });
		if (num > 0)
		{
			text2 = text2.Substring(0, num);
		}
		if (text2.Length > 120)
		{
			text2 = text2.Substring(0, 120);
		}
		return string.IsNullOrWhiteSpace(text) ? text2 : (text + " " + text2);
	}

	private static List<string> BuildRuleSemanticSeeds(string ruleTag, string ruleInstruction, List<string> triggerKeywords)
	{
		List<string> seeds = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (triggerKeywords != null)
			{
				for (int i = 0; i < triggerKeywords.Count; i++)
				{
					string text = NormalizeSemanticText(triggerKeywords[i]);
					if (!string.IsNullOrWhiteSpace(text))
					{
						addSeed(text);
					}
				}
			}
			if (seeds.Count <= 0)
			{
				addSeed(ruleTag);
			}
		}
		catch
		{
		}
		return seeds;
		void addSeed(string raw)
		{
			string text2 = NormalizeSemanticText(raw);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				if (text2.Length > 260)
				{
					text2 = text2.Substring(0, 260);
				}
				if (seen.Add(text2))
				{
					seeds.Add(text2);
				}
			}
		}
	}

	private static bool TryGetInputEmbedding(string input, out float[] vec)
	{
		vec = null;
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailInputVecCache.TryGetValue(text, out var value) && value != null && value.Length != 0)
			{
				vec = value;
				return true;
			}
		}
		OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
		if (instance == null || !instance.IsAvailable)
		{
			return false;
		}
		if (!instance.TryGetEmbedding(text, out var vector) || vector == null || vector.Length == 0)
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailInputVecCache.Count >= 256)
			{
				_guardrailInputVecCache.Clear();
			}
			_guardrailInputVecCache[text] = vector;
		}
		vec = vector;
		return true;
	}

	private static bool TryGetPhraseEmbedding(string phraseSeed, out float[] vec)
	{
		vec = null;
		string text = NormalizeSemanticText(phraseSeed);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailPhraseVecCache.TryGetValue(text, out var value) && value != null && value.Length != 0)
			{
				vec = value;
				return true;
			}
		}
		OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
		if (instance == null || !instance.IsAvailable)
		{
			return false;
		}
		if (!instance.TryGetEmbedding(text, out var vector) || vector == null || vector.Length == 0)
		{
			return false;
		}
		lock (_guardrailSemanticLock)
		{
			if (_guardrailPhraseVecCache.Count >= 1024)
			{
				_guardrailPhraseVecCache.Clear();
			}
			_guardrailPhraseVecCache[text] = vector;
		}
		vec = vector;
		return true;
	}

	private static string BuildGuardrailEvalKey(string userText, string contextText)
	{
		string text = NormalizeSemanticText(userText);
		string text2 = NormalizeSemanticText(contextText);
		return text + "||" + text2;
	}

	private static bool TryGetGuardrailEvalSnapshot(string userText, out GuardrailEvalSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			string runtimeGuardrailContext = GetRuntimeGuardrailContext();
			string text = BuildGuardrailEvalKey(userText, runtimeGuardrailContext);
			lock (_guardrailSemanticLock)
			{
				if (_lastGuardrailEval != null && string.Equals(_lastGuardrailEval.Key, text, StringComparison.Ordinal))
				{
					snapshot = _lastGuardrailEval;
					return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
				}
			}
			List<string> list = SplitGuardrailIntents(userText);
			List<float[]> list2 = new List<float[]>();
			List<string> list3 = new List<string>();
			for (int i = 0; i < list.Count; i++)
			{
				string text2 = NormalizeSemanticText(list[i]);
				if (!string.IsNullOrWhiteSpace(text2) && TryGetInputEmbedding(text2, out var vec) && vec != null && vec.Length != 0)
				{
					list3.Add(text2);
					list2.Add(vec);
				}
			}
			if (list2.Count <= 0)
			{
				try
				{
					Logger.Log("GuardrailSemantic", $"snapshot_fail reason=no_input_embeddings intentCount={list.Count} input={NormalizeSemanticText(userText)}");
				}
				catch
				{
				}
				return false;
			}
			if (list3.Count > 1)
			{
				try
				{
					Logger.Log("GuardrailSemantic", string.Format("intent_split count={0} intents={1}", list3.Count, string.Join(" || ", list3)));
				}
				catch
				{
				}
			}
			float[] vec2 = null;
			if (!string.IsNullOrWhiteSpace(runtimeGuardrailContext))
			{
				TryGetInputEmbedding(runtimeGuardrailContext, out vec2);
			}
			bool flag = vec2 != null && vec2.Length != 0;
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			if (allEnabledRulePrompts == null || allEnabledRulePrompts.Count <= 0)
			{
				return false;
			}
			GuardrailEvalSnapshot guardrailEvalSnapshot = new GuardrailEvalSnapshot
			{
				Key = text
			};
			List<GuardrailRuleEval> list4 = new List<GuardrailRuleEval>();
			for (int j = 0; j < allEnabledRulePrompts.Count; j++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[j];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				string id = guardrailRulePromptConfig.Id;
				string ruleInstruction = guardrailRulePromptConfig.Instruction ?? "";
				List<string> list5 = BuildRuleSemanticSeeds(id, ruleInstruction, guardrailRulePromptConfig.TriggerKeywords);
				float num = 0f;
				float num2 = 0f;
				string matchedSeed = "";
				string matchedIntent = "";
				for (int k = 0; k < list5.Count; k++)
				{
					string text3 = list5[k];
					if (string.IsNullOrWhiteSpace(text3) || !TryGetPhraseEmbedding(text3, out var vec3) || vec3 == null || vec3.Length == 0)
					{
						continue;
					}
					for (int l = 0; l < list2.Count; l++)
					{
						float num3 = DotProductNormalized(list2[l], vec3);
						if (num3 > num)
						{
							num = num3;
							matchedSeed = text3;
							matchedIntent = list3[l];
						}
					}
					if (flag)
					{
						float num4 = DotProductNormalized(vec2, vec3);
						if (num4 > num2)
						{
							num2 = num4;
						}
					}
				}
				float num5 = 0f;
				if (flag)
				{
					num5 = (IsBuiltInRuleTag(id) ? 0f : 0.12f);
				}
				float mixedRaw = (flag ? (num * (1f - num5) + num2 * num5) : num);
				GuardrailRuleEval guardrailRuleEval = new GuardrailRuleEval
				{
					RuleTag = id,
					MatchedSeed = matchedSeed,
					MatchedIntent = matchedIntent,
					RawInput = num,
					RawContext = num2,
					MixedRaw = mixedRaw
				};
				list4.Add(guardrailRuleEval);
				guardrailEvalSnapshot.Rules[id] = guardrailRuleEval;
			}
			list4 = list4.OrderByDescending((GuardrailRuleEval x) => x.RawInput).ThenBy((GuardrailRuleEval x) => x.RuleTag, StringComparer.OrdinalIgnoreCase).ToList();
			float num6 = ((list4.Count > 0) ? list4.Average((GuardrailRuleEval x) => x.MixedRaw) : 0f);
			int guardrailTopNFromMcm = GetGuardrailTopNFromMcm();
			int num7 = Math.Max(2, Math.Min(5, (int)Math.Ceiling((float)list4.Count * 0.25f)));
			float topGap = ((list4.Count > 1) ? (list4[0].RawInput - list4[1].RawInput) : 1f);
			for (int num8 = 0; num8 < list4.Count; num8++)
			{
				GuardrailRuleEval guardrailRuleEval2 = list4[num8];
				guardrailRuleEval2.Mean = num6;
				guardrailRuleEval2.Rank = num8 + 1;
				float num9 = -1f;
				string maxOtherTag = "";
				for (int num10 = 0; num10 < list4.Count; num10++)
				{
					if (num10 != num8 && list4[num10].RawInput > num9)
					{
						num9 = list4[num10].RawInput;
						maxOtherTag = list4[num10].RuleTag;
					}
				}
				GuardrailGateProfile guardrailGateProfile = GetGuardrailGateProfile(guardrailRuleEval2.RuleTag, userText.Length);
				float delta = ((num9 < -0.5f) ? guardrailRuleEval2.MixedRaw : (guardrailRuleEval2.MixedRaw - num9));
				float ampScore = ApplyGuardrailAmplifiedScore(guardrailRuleEval2.MixedRaw, num9, num6, guardrailGateProfile);
				bool candidate = guardrailRuleEval2.Rank <= num7;
				bool flag2 = guardrailRuleEval2.MixedRaw >= guardrailGateProfile.RawFloor;
				float num11 = 0f;
				float num12 = 0f;
				string bestSeed = "";
				bool lexicalAnchor = false;
				if (IsBuiltInRuleTag(guardrailRuleEval2.RuleTag))
				{
					num12 = GetBuiltInIntentEvidenceGate(guardrailRuleEval2.RuleTag, userText.Length);
					num11 = ComputeBuiltInIntentSemanticEvidence(guardrailRuleEval2.RuleTag, list2, out bestSeed);
					lexicalAnchor = num11 >= num12;
				}
				bool flag3 = guardrailRuleEval2.Rank <= guardrailTopNFromMcm;
				bool forceHit = false;
				bool absHit = flag3;
				bool relHit = false;
				bool highAmpHit = false;
				string rejectReason = (flag3 ? ("topn_direct(" + guardrailRuleEval2.Rank + "/" + guardrailTopNFromMcm + ")") : "topn_overflow");
				guardrailRuleEval2.MaxOther = num9;
				guardrailRuleEval2.MaxOtherTag = maxOtherTag;
				guardrailRuleEval2.Delta = delta;
				guardrailRuleEval2.AmpScore = ampScore;
				guardrailRuleEval2.TopGap = topGap;
				guardrailRuleEval2.IntentEvidence = num11;
				guardrailRuleEval2.IntentGate = num12;
				guardrailRuleEval2.IntentSeed = bestSeed;
				guardrailRuleEval2.LexicalAnchor = lexicalAnchor;
				guardrailRuleEval2.Candidate = candidate;
				guardrailRuleEval2.AbsHit = absHit;
				guardrailRuleEval2.RelHit = relHit;
				guardrailRuleEval2.HighAmpHit = highAmpHit;
				guardrailRuleEval2.ForceHit = forceHit;
				guardrailRuleEval2.RejectReason = rejectReason;
				guardrailRuleEval2.Hit = flag3;
			}
			try
			{
				int count = list4.Count;
				int num13 = 0;
				for (int num14 = 0; num14 < list4.Count; num14++)
				{
					GuardrailRuleEval guardrailRuleEval3 = list4[num14];
					if (guardrailRuleEval3 != null)
					{
						if (guardrailRuleEval3.Hit)
						{
							num13++;
						}
						Logger.RecordHitRate("guardrail", guardrailRuleEval3.RuleTag ?? "__unknown__", guardrailRuleEval3.Hit, $"raw={guardrailRuleEval3.RawInput:0.000} amp={guardrailRuleEval3.AmpScore:0.000} rank={guardrailRuleEval3.Rank} reason={guardrailRuleEval3.RejectReason}", userText);
					}
				}
				Logger.RecordHitRate("guardrail", "__query__", num13 > 0, $"hits={num13}/{count} inputLen={userText.Length}", userText);
			}
			catch
			{
			}
			lock (_guardrailSemanticLock)
			{
				_lastGuardrailEval = guardrailEvalSnapshot;
			}
			snapshot = guardrailEvalSnapshot;
			return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("GuardrailSemantic", "snapshot_fail reason=exception msg=" + ex.Message + " stack=" + ex.StackTrace?.Replace("\n", " | ")?.Substring(0, Math.Min(ex.StackTrace?.Length ?? 0, 300)));
			}
			catch
			{
			}
			return false;
		}
	}

	private static bool TryGetRuleEval(string userText, string ruleTag, out GuardrailRuleEval eval)
	{
		eval = null;
		if (!TryGetGuardrailEvalSnapshot(userText, out var snapshot) || snapshot == null || snapshot.Rules == null)
		{
			return false;
		}
		string text = NormalizeSemanticText(ruleTag);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return snapshot.Rules.TryGetValue(text, out eval) && eval != null;
	}

	public static bool IsGuardrailSemanticHit(string input, List<string> triggerKeywords, string ruleTag)
	{
		string matchedKeyword;
		float score;
		return IsGuardrailSemanticHit(input, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, List<string> triggerKeywords, string ruleTag, out string matchedKeyword, out float score)
	{
		return IsGuardrailSemanticHit(input, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, string ruleTag, string ruleInstruction, List<string> triggerKeywords, out string matchedKeyword, out float score)
	{
		matchedKeyword = "";
		score = 0f;
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (TryGetRuleEval(text, ruleTag, out var eval))
		{
			matchedKeyword = (string.IsNullOrWhiteSpace(eval.MatchedSeed) ? "semantic_seed" : eval.MatchedSeed);
			score = eval.AmpScore;
			string text2 = NormalizeSemanticText(eval.MatchedIntent);
			if (text2.Length > 48)
			{
				text2 = text2.Substring(0, 48);
			}
			Logger.Log("GuardrailSemantic", $"rule={ruleTag} hit={eval.Hit} mode=semantic_topn raw={eval.RawInput:0.000} ctx={eval.RawContext:0.000} mixed={eval.MixedRaw:0.000} amp={eval.AmpScore:0.000} delta={eval.Delta:0.000} topGap={eval.TopGap:0.000} rank={eval.Rank} candidate={eval.Candidate} other={eval.MaxOtherTag}@{eval.MaxOther:0.000} mean={eval.Mean:0.000} absHit={eval.AbsHit} relHit={eval.RelHit} highAmpHit={eval.HighAmpHit} forceHit={eval.ForceHit} intentEvidence={eval.IntentEvidence:0.000} intentGate={eval.IntentGate:0.000} lexicalAnchor={eval.LexicalAnchor} intentSeed={eval.IntentSeed} reason={eval.RejectReason} intent={text2}");
			return eval.Hit;
		}
		Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=semantic_unavailable");
		try
		{
			Logger.RecordHitRate("guardrail", ruleTag ?? "__unknown__", hit: false, "reason=semantic_unavailable", text);
		}
		catch
		{
		}
		return false;
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, int maxCount = 6, bool includeBuiltInRules = false)
	{
		List<GuardrailRuleHit> list = new List<GuardrailRuleHit>();
		try
		{
			string text = NormalizeSemanticText(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			if (!TryGetGuardrailEvalSnapshot(text, out var snapshot) || snapshot?.Rules == null || snapshot.Rules.Count <= 0)
			{
				return list;
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			foreach (KeyValuePair<string, GuardrailRuleEval> rule in snapshot.Rules)
			{
				string text2 = (rule.Key ?? "").Trim().ToLowerInvariant();
				GuardrailRuleEval value = rule.Value;
				if (!string.IsNullOrWhiteSpace(text2) && value != null && value.Hit && (includeBuiltInRules || !IsBuiltInRuleTag(text2)))
				{
					dictionary.TryGetValue(text2, out var value2);
					string text3 = value2?.Instruction ?? "";
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list.Add(new GuardrailRuleHit
						{
							RuleId = text2,
							Group = (value2?.Group ?? ""),
							Priority = (value2?.Priority ?? 0),
							Score = value.AmpScore,
							MatchedSeed = (value.MatchedSeed ?? ""),
							Instruction = text3
						});
					}
				}
			}
			list = (from x in list
				orderby x.Priority descending, x.Score descending
				select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
			if (maxCount > 0 && list.Count > maxCount)
			{
				list = list.Take(maxCount).ToList();
			}
		}
		catch
		{
		}
		return list;
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules = 4)
	{
		return BuildMatchedExtraRuleInstructions(input, maxRules, hasAnyHero: true);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules, bool hasAnyHero)
	{
		try
		{
			List<GuardrailRuleHit> guardrailSemanticRuleHits = GetGuardrailSemanticRuleHits(input, maxRules);
			if (guardrailSemanticRuleHits == null || guardrailSemanticRuleHits.Count <= 0)
			{
				return "";
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = (hasAnyHero ? null : BuildRulePromptRegistry());
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < guardrailSemanticRuleHits.Count; i++)
			{
				GuardrailRuleHit guardrailRuleHit = guardrailSemanticRuleHits[i];
				if (guardrailRuleHit == null)
				{
					continue;
				}
				string text = (guardrailRuleHit.RuleId ?? "").Trim();
				string value = (guardrailRuleHit.Instruction ?? "").Trim();
				if (!hasAnyHero && dictionary != null)
				{
					dictionary.TryGetValue(text, out var value2);
					string text2 = (value2?.NonHeroInstruction ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						value = text2;
					}
				}
				if (hasAnyHero && string.Equals(text, "kingdom_service", StringComparison.OrdinalIgnoreCase))
				{
					string runtimeKingdomServiceInstruction = BuildRuntimeKingdomServiceInstruction();
					if (!string.IsNullOrWhiteSpace(runtimeKingdomServiceInstruction))
					{
						value = runtimeKingdomServiceInstruction;
					}
				}
				if (hasAnyHero && string.Equals(text, "marriage", StringComparison.OrdinalIgnoreCase))
				{
					string runtimeMarriageInstruction = RomanceSystemBehavior.Instance?.BuildMarriageRuntimeInstruction(ResolveConversationTargetHero()) ?? "";
					if (!string.IsNullOrWhiteSpace(runtimeMarriageInstruction))
					{
						value = runtimeMarriageInstruction;
					}
				}
				if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.AppendLine("【附加规则:" + text + "】");
					stringBuilder.AppendLine(value);
					string value3 = BuildRuntimeRuleConstraintHint(text);
					if (!string.IsNullOrWhiteSpace(value3))
					{
						stringBuilder.AppendLine(value3);
					}
					try
					{
						Logger.Log("GuardrailSemantic", $"extra_rule_hit rule={text} score={guardrailRuleHit.Score:0.000} group={guardrailRuleHit.Group} priority={guardrailRuleHit.Priority} nonHero={!hasAnyHero}");
					}
					catch
					{
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

	private static string GetKingdomServiceRuntimeTemplate(string stateKey, bool forConstraint)
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary == null || !dictionary.TryGetValue("kingdom_service", out var value) || value == null)
			{
				return "";
			}
			Dictionary<string, string> dictionary2 = (forConstraint ? value.RuntimeConstraintTemplates : value.RuntimeInstructionTemplates);
			if (dictionary2 == null || dictionary2.Count <= 0)
			{
				return "";
			}
			string text = (stateKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text) && dictionary2.TryGetValue(text, out var value2) && !string.IsNullOrWhiteSpace(value2))
			{
				return value2;
			}
			if (dictionary2.TryGetValue("__default__", out var value3) && !string.IsNullOrWhiteSpace(value3))
			{
				return value3;
			}
			if (dictionary2.TryGetValue("default", out var value4) && !string.IsNullOrWhiteSpace(value4))
			{
				return value4;
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string GetRuleRuntimeTemplate(string ruleId, string stateKey, bool forConstraint)
	{
		try
		{
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			string text = (ruleId ?? "").Trim().ToLowerInvariant();
			if (dictionary == null || string.IsNullOrWhiteSpace(text) || !dictionary.TryGetValue(text, out var value) || value == null)
			{
				return "";
			}
			Dictionary<string, string> dictionary2 = (forConstraint ? value.RuntimeConstraintTemplates : value.RuntimeInstructionTemplates);
			if (dictionary2 == null || dictionary2.Count <= 0)
			{
				return "";
			}
			string text2 = (stateKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2) && dictionary2.TryGetValue(text2, out var value2) && !string.IsNullOrWhiteSpace(value2))
			{
				return value2;
			}
			if (dictionary2.TryGetValue("__default__", out var value3) && !string.IsNullOrWhiteSpace(value3))
			{
				return value3;
			}
			if (dictionary2.TryGetValue("default", out var value4) && !string.IsNullOrWhiteSpace(value4))
			{
				return value4;
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	public static void SetGuardrailRuntimeTargetKingdom(string kingdomId)
	{
		try
		{
			_guardrailRuntimeTargetKingdomId.Value = ((kingdomId ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetKingdomId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetHero(string heroId)
	{
		try
		{
			_guardrailRuntimeTargetHeroId.Value = (heroId ?? "").Trim();
		}
		catch
		{
			_guardrailRuntimeTargetHeroId.Value = "";
		}
	}

	private static string ApplyRuntimeTemplate(string template, Dictionary<string, string> tokens)
	{
		string text = template ?? "";
		try
		{
			if (string.IsNullOrWhiteSpace(text) || tokens == null || tokens.Count <= 0)
			{
				return text;
			}
			foreach (KeyValuePair<string, string> token in tokens)
			{
				if (!string.IsNullOrWhiteSpace(token.Key))
				{
					text = text.Replace("{" + token.Key + "}", token.Value ?? "");
				}
			}
			return text;
		}
		catch
		{
			return template ?? "";
		}
	}

	private static string ResolveKingdomServiceRuntimeText(string stateKey, bool forConstraint, Dictionary<string, string> tokens)
	{
		try
		{
			string kingdomServiceRuntimeTemplate = GetKingdomServiceRuntimeTemplate(stateKey, forConstraint);
			if (!string.IsNullOrWhiteSpace(kingdomServiceRuntimeTemplate))
			{
				string text = ApplyRuntimeTemplate(kingdomServiceRuntimeTemplate, tokens);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static string ResolveRuleRuntimeText(string ruleId, string stateKey, bool forConstraint, Dictionary<string, string> tokens)
	{
		try
		{
			string ruleRuntimeTemplate = GetRuleRuntimeTemplate(ruleId, stateKey, forConstraint);
			if (!string.IsNullOrWhiteSpace(ruleRuntimeTemplate))
			{
				string text = ApplyRuntimeTemplate(ruleRuntimeTemplate, tokens);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static Kingdom ResolveConversationTargetKingdom()
	{
		try
		{
			string text = (_guardrailRuntimeTargetKingdomId.Value ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Kingdom kingdom = Kingdom.All?.FirstOrDefault((Kingdom k) => k != null && string.Equals((k.StringId ?? "").Trim().ToLowerInvariant(), text, StringComparison.OrdinalIgnoreCase));
				if (kingdom != null)
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Kingdom kingdom = oneToOneConversationHero?.Clan?.Kingdom;
			if (kingdom != null)
			{
				return kingdom;
			}
			Kingdom kingdom2 = oneToOneConversationHero?.MapFaction as Kingdom;
			if (kingdom2 != null)
			{
				return kingdom2;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			Hero heroObject = oneToOneConversationCharacter?.HeroObject;
			Kingdom kingdom3 = heroObject?.Clan?.Kingdom;
			if (kingdom3 != null)
			{
				return kingdom3;
			}
			Kingdom kingdom4 = heroObject?.MapFaction as Kingdom;
			if (kingdom4 != null)
			{
				return kingdom4;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Hero ResolveConversationTargetHero()
	{
		try
		{
			string text = (_guardrailRuntimeTargetHeroId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Hero hero = Hero.Find(text);
				if (hero != null)
				{
					return hero;
				}
				Hero hero2 = Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (hero2 != null)
				{
					return hero2;
				}
			}
		}
		catch
		{
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				return oneToOneConversationHero;
			}
		}
		catch
		{
		}
		try
		{
			Hero heroObject = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter?.HeroObject;
			if (heroObject != null)
			{
				return heroObject;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string BuildRuntimeKingdomServiceInstruction()
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = playerClan?.Kingdom;
			bool flag = playerClan?.IsUnderMercenaryService == true;
			Kingdom kingdom2 = ResolveConversationTargetKingdom();
			bool flag2 = kingdom != null && kingdom2 != null && kingdom == kingdom2;
			string text = kingdom?.Name?.ToString() ?? "";
			string text2 = kingdom2?.Name?.ToString() ?? "";
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["playerKingdom"] = text,
				["playerKingdomId"] = (kingdom?.StringId ?? ""),
				["targetKingdom"] = text2,
				["targetKingdomId"] = (kingdom2?.StringId ?? "")
			};
			if (kingdom == null)
			{
				return ResolveKingdomServiceRuntimeText("no_kingdom", forConstraint: false, dictionary);
			}
			if (flag)
			{
				if (kingdom2 == null)
				{
					return ResolveKingdomServiceRuntimeText("mercenary_target_unknown", forConstraint: false, dictionary);
				}
				if (flag2)
				{
					return ResolveKingdomServiceRuntimeText("mercenary_same_kingdom", forConstraint: false, dictionary);
				}
				return ResolveKingdomServiceRuntimeText("mercenary_other_kingdom", forConstraint: false, dictionary);
			}
			if (kingdom2 == null)
			{
				return ResolveKingdomServiceRuntimeText("vassal_target_unknown", forConstraint: false, dictionary);
			}
			if (flag2)
			{
				return ResolveKingdomServiceRuntimeText("vassal_same_kingdom", forConstraint: false, dictionary);
			}
			return ResolveKingdomServiceRuntimeText("vassal_other_kingdom", forConstraint: false, dictionary);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildRuntimeRuleConstraintHint(string tag)
	{
		try
		{
			string text = (tag ?? "").Trim().ToLowerInvariant();
			if (text == "marriage")
			{
				Hero speaker = ResolveConversationTargetHero();
				return RomanceSystemBehavior.Instance?.BuildMarriageRuntimeConstraintHint(speaker) ?? "";
			}
			if (text != "kingdom_service")
			{
				return "";
			}
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				return ResolveKingdomServiceRuntimeText("no_player_clan", forConstraint: true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			}
			int num = playerClan.Tier;
			int num2 = 1;
			int num3 = 2;
			try
			{
				num2 = Campaign.Current?.Models?.ClanTierModel?.MercenaryEligibleTier ?? 1;
			}
			catch
			{
				num2 = 1;
			}
			try
			{
				num3 = Campaign.Current?.Models?.ClanTierModel?.VassalEligibleTier ?? 2;
			}
			catch
			{
				num3 = 2;
			}
			Kingdom kingdom = playerClan.Kingdom;
			bool flag = playerClan.IsUnderMercenaryService;
			Kingdom kingdom2 = ResolveConversationTargetKingdom();
			bool flag2 = kingdom != null && kingdom2 != null && kingdom == kingdom2;
			int num4 = 5;
			int num5 = 20;
			Hero hero = kingdom2?.Leader;
			if (hero == null)
			{
				hero = ResolveConversationTargetHero();
			}
			if (hero == null)
			{
				hero = kingdom?.Leader;
			}
			int num6 = 0;
			try
			{
				num6 = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
			}
			catch
			{
				num6 = 0;
			}
			int num7 = Math.Max(0, num5 - num6);
			string value = ((num7 > 0) ? $"玩家信任度未达标，还差{num7}" : "玩家信任度已经达标");
			string text2 = kingdom?.Name?.ToString() ?? "";
			string text3 = kingdom2?.Name?.ToString() ?? "";
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["playerKingdom"] = text2,
				["playerKingdomId"] = (kingdom?.StringId ?? ""),
				["targetKingdom"] = text3,
				["targetKingdomId"] = (kingdom2?.StringId ?? ""),
				["playerTier"] = num.ToString(),
				["mercTier"] = num2.ToString(),
				["vassalTier"] = num3.ToString(),
				["trustMerc"] = num4.ToString(),
				["trustVassal"] = num5.ToString(),
				["trustCurrent"] = num6.ToString(),
				["trustVassalGap"] = num7.ToString(),
				["vassalTrustStatus"] = value
			};
			if (kingdom == null)
			{
				if (num < num2)
				{
					return ResolveKingdomServiceRuntimeText("no_kingdom_tier_below_merc", forConstraint: true, dictionary);
				}
				if (num < num3)
				{
					return ResolveKingdomServiceRuntimeText("no_kingdom_tier_merc_only", forConstraint: true, dictionary);
				}
				return ResolveKingdomServiceRuntimeText("no_kingdom_tier_full", forConstraint: true, dictionary);
			}
			if (flag)
			{
				if (kingdom2 == null)
				{
					return ResolveKingdomServiceRuntimeText("mercenary_target_unknown", forConstraint: true, dictionary);
				}
				if (flag2)
				{
					if (num < num3)
					{
						return ResolveKingdomServiceRuntimeText("mercenary_same_kingdom_tier_vassal_locked", forConstraint: true, dictionary);
					}
					return ResolveKingdomServiceRuntimeText("mercenary_same_kingdom_tier_vassal_ready", forConstraint: true, dictionary);
				}
				return ResolveKingdomServiceRuntimeText("mercenary_other_kingdom", forConstraint: true, dictionary);
			}
			if (kingdom2 == null)
			{
				return ResolveKingdomServiceRuntimeText("vassal_target_unknown", forConstraint: true, dictionary);
			}
			if (flag2)
			{
				return ResolveKingdomServiceRuntimeText("vassal_same_kingdom", forConstraint: true, dictionary);
			}
			return ResolveKingdomServiceRuntimeText("vassal_other_kingdom", forConstraint: true, dictionary);
		}
		catch
		{
			return "";
		}
	}

	private static string RuleTopicLabel(string tag)
	{
		return (tag ?? "").Trim().ToLowerInvariant() switch
		{
			"duel" => "决斗挑战", 
			"reward" => "交易/奖励", 
			"loan" => "借贷/赊账", 
			"surroundings" => "地理方位", 
			_ => "当前话题", 
		};
	}

	private static string RuleTopicAskHint(string tag)
	{
		return (tag ?? "").Trim().ToLowerInvariant() switch
		{
			"duel" => "向我发起决斗", 
			"reward" => "谈交易和货物", 
			"loan" => "谈借钱或还款", 
			"surroundings" => "问附近位置", 
			_ => "讨论这件事", 
		};
	}

	public static string BuildGuardrailClarificationHint(string input, bool duelHit, float duelScore, bool rewardHit, float rewardScore, bool loanHit, float loanScore, bool surroundingsHit, float surroundingsScore)
	{
		try
		{
			if (duelHit || rewardHit || loanHit || surroundingsHit)
			{
				return "";
			}
			List<KeyValuePair<string, float>> list = new List<KeyValuePair<string, float>>
			{
				new KeyValuePair<string, float>("duel", duelScore),
				new KeyValuePair<string, float>("reward", rewardScore),
				new KeyValuePair<string, float>("loan", loanScore),
				new KeyValuePair<string, float>("surroundings", surroundingsScore)
			}.OrderByDescending((KeyValuePair<string, float> x) => x.Value).ToList();
			if (list.Count < 2)
			{
				return "";
			}
			KeyValuePair<string, float> keyValuePair = list[0];
			KeyValuePair<string, float> keyValuePair2 = list[1];
			if (keyValuePair.Value < 0.4f)
			{
				return "";
			}
			float num = keyValuePair.Value - keyValuePair2.Value;
			if (num >= 0.07f)
			{
				return "";
			}
			string text = RuleTopicLabel(keyValuePair.Key);
			string text2 = RuleTopicLabel(keyValuePair2.Key);
			string text3 = RuleTopicAskHint(keyValuePair.Key);
			string text4 = RuleTopicAskHint(keyValuePair2.Key);
			return $"[系统-澄清优先] 玩家意图在“{text}”与“{text2}”之间存在歧义（分差={num:0.00}）。本轮先追问一句澄清，不要输出任何 ACTION 标签，也不要直接承诺交易/借贷/决斗。可参考：你是想{text3}，还是在{text4}？";
		}
		catch
		{
			return "";
		}
	}

	private static DuelSettings TryGetMcmSettings()
	{
		try
		{
			return DuelSettings.GetSettings();
		}
		catch
		{
			return null;
		}
	}

	private static bool UseMcmKnowledgeRetrieval()
	{
		try
		{
			DuelSettings duelSettings = TryGetMcmSettings();
			return duelSettings != null;
		}
		catch
		{
			return false;
		}
	}

	private static int ClampKnowledgeTopK(int v)
	{
		if (v < 1)
		{
			v = 1;
		}
		if (v > 64)
		{
			v = 64;
		}
		return v;
	}

	private static int ClampGuardrailTopN(int v)
	{
		if (v < 1)
		{
			v = 1;
		}
		if (v > 4)
		{
			v = 4;
		}
		return v;
	}

	private static int GetGuardrailTopNFromMcm()
	{
		try
		{
			DuelSettings duelSettings = TryGetMcmSettings();
			if (duelSettings != null)
			{
				return ClampGuardrailTopN(duelSettings.GuardrailDirectTopN);
			}
		}
		catch
		{
		}
		return 1;
	}

	private static bool TryGetKnowledgeFromMcm(out bool enabled, out bool semanticFirst, out bool keywordFallback, out int topK)
	{
		enabled = true;
		semanticFirst = true;
		keywordFallback = false;
		topK = 2;
		try
		{
			if (!UseMcmKnowledgeRetrieval())
			{
				return false;
			}
			DuelSettings duelSettings = TryGetMcmSettings();
			if (duelSettings == null)
			{
				return false;
			}
			try
			{
				enabled = duelSettings.KnowledgeRetrievalEnabled;
			}
			catch
			{
				enabled = true;
			}
			try
			{
				semanticFirst = duelSettings.KnowledgeSemanticFirst;
			}
			catch
			{
				semanticFirst = true;
			}
			try
			{
				keywordFallback = duelSettings.KnowledgeKeywordFallback;
			}
			catch
			{
				keywordFallback = false;
			}
			try
			{
				int knowledgeDirectTopN = duelSettings.KnowledgeDirectTopN;
				if (knowledgeDirectTopN > 0)
				{
					topK = knowledgeDirectTopN;
				}
				else
				{
					topK = duelSettings.KnowledgeSemanticTopK;
				}
			}
			catch
			{
			}
			topK = ClampKnowledgeTopK(topK);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void ReloadConfig()
	{
		try
		{
			string basePath = Utilities.GetBasePath();
			string path = System.IO.Path.Combine(basePath, "Modules", "AnimusForge", "ModuleData", "AIConfig.json");
			if (!File.Exists(path))
			{
				Logger.Log("AIConfig", "[错误] 找不到 AIConfig.json");
				_config = new AIConfigModel();
			}
			else
			{
				string value = File.ReadAllText(path);
				_config = JsonConvert.DeserializeObject<AIConfigModel>(value) ?? new AIConfigModel();
			}
			string path2 = System.IO.Path.Combine(basePath, "Modules", "AnimusForge", "ModuleData", "RuleBehaviorPrompts.json");
			if (!File.Exists(path2))
			{
				Logger.Log("AIConfig", "[错误] 找不到 RuleBehaviorPrompts.json");
				_guardrail = new GuardrailConfigModel();
			}
			else
			{
				string value2 = File.ReadAllText(path2);
				_guardrail = JsonConvert.DeserializeObject<GuardrailConfigModel>(value2) ?? new GuardrailConfigModel();
			}
			lock (_guardrailSemanticLock)
			{
				_guardrailPhraseVecCache.Clear();
				_guardrailInputVecCache.Clear();
				_lastGuardrailEval = null;
			}
			_guardrailSemanticRuntimeContext.Value = "";
			_guardrailRuntimeTargetKingdomId.Value = "";
			int valueOrDefault = (_guardrail?.Duel?.AcceptKeywords?.Count).GetValueOrDefault();
			int valueOrDefault2 = (_guardrail?.Reward?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault3 = (_guardrail?.Loan?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault4 = (_guardrail?.Surroundings?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault5 = (_guardrail?.RulePrompts?.Count).GetValueOrDefault();
			int num = 0;
			try
			{
				num = GetAllEnabledRulePrompts().Count;
			}
			catch
			{
				num = 0;
			}
			string text = (KnowledgeRetrievalFromMcm ? "MCM" : "Guardrail");
			Logger.Log("AIConfig", string.Format("配置加载成功。触发词(决斗/奖励/借贷/地理)={0}/{1}/{2}/{3}，扩展规则={4}，启用规则总数={5}。规则TopN={6}。知识命中({7}/TopN): {8}（语义优先={9}, 关键词兜底={10}, topK={11}）。", valueOrDefault, valueOrDefault2, valueOrDefault3, valueOrDefault4, valueOrDefault5, num, GetGuardrailTopNFromMcm(), text, KnowledgeRetrievalEnabled ? "开启" : "关闭", KnowledgeSemanticFirst, KnowledgeKeywordFallback, KnowledgeSemanticTopK));
		}
		catch (Exception ex)
		{
			Logger.Log("AIConfig", "[错误] 加载失败: " + ex.Message);
			_config = new AIConfigModel();
			_guardrail = new GuardrailConfigModel();
		}
	}

	public static string GetLoreContext(string inputText, Hero npcHero)
	{
		if (string.IsNullOrWhiteSpace(inputText))
		{
			return "";
		}
		try
		{
			KnowledgeLibraryBehavior instance = KnowledgeLibraryBehavior.Instance;
			if (instance != null)
			{
				string text = instance.BuildLoreContext(inputText, npcHero);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static string GetLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride = null)
	{
		if (string.IsNullOrWhiteSpace(inputText))
		{
			return "";
		}
		try
		{
			KnowledgeLibraryBehavior instance = KnowledgeLibraryBehavior.Instance;
			if (instance != null)
			{
				string text = instance.BuildLoreContext(inputText, npcCharacter, kingdomIdOverride);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "";
	}
}
