using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

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

		public float RerankScore;

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

		public string MatchMode;

		public bool Hit;
	}

	private sealed class GuardrailEvalSnapshot
	{
		public string Key;

		public string MatchMode = "none";

		public int IntentCount;

		public int RecallPerIntent;

		public int RerankPerIntent;

		public int ReturnCap;

		public Dictionary<string, GuardrailRuleEval> Rules = new Dictionary<string, GuardrailRuleEval>(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class GuardrailIntentInput
	{
		public string Text;

		public float[] Vector;

		public float Weight = 1f;
	}

	private sealed class GuardrailRuleScore
	{
		public GuardrailRulePromptConfig Rule;

		public float RawScore;

		public float FinalScore;

		public string MatchedSeed;

		public string MatchedIntent;
	}

	private sealed class GuardrailRuleAggregate
	{
		public GuardrailRuleEval Eval;

		public float ScoreSum;

		public int HitCount;

		public int BestRank = int.MaxValue;

		public float BestScore;

		public string MatchedSeed;

		public string MatchedIntent;
	}

	private sealed class StickyGuardrailRuleState
	{
		public string RuleId = "";

		public string Group = "";

		public int Priority;

		public float LastScore;

		public string MatchedSeed = "";

		public int RemainingCarryTurns;

		public int MaxCarryTurns;

		public int CarryTurnIndex;
	}

	private static string BuildSemanticHitRateDetail(string detail, string secondaryText)
	{
		string text = (detail ?? "").Trim();
		string text2 = NormalizeSemanticText(secondaryText);
		string text3 = string.IsNullOrWhiteSpace(text2) ? "off" : "on";
		if (text2.Length > 72)
		{
			text2 = text2.Substring(0, 72);
		}
		text2 = text2.Replace("\r", " ").Replace("\n", " ").Trim();
		string value = $"npcRecall={text3} secondaryLen={(string.IsNullOrWhiteSpace(text2) ? 0 : text2.Length)}";
		if (!string.IsNullOrWhiteSpace(text2))
		{
			value = value + " secondaryPreview=" + JsonConvert.ToString(text2);
		}
		return string.IsNullOrWhiteSpace(text) ? value : (text + " " + value);
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

	private static int _guardrailWarmupState;

	private static long _guardrailWarmupVersion = -1L;

	private static long _guardrailConfigVersion = 1L;

	private const int GuardrailPhraseVecCacheMax = 1024;

	private const int GuardrailInputVecCacheMax = 256;

	private static readonly AsyncLocal<string> _guardrailSemanticRuntimeContext = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetKingdomId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetHeroId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetCharacterId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetTroopId = new AsyncLocal<string>();

	private static readonly AsyncLocal<string> _guardrailRuntimeTargetUnnamedRank = new AsyncLocal<string>();

	private static readonly AsyncLocal<int> _guardrailRuntimeTargetAgentIndex = new AsyncLocal<int>();

	private static readonly object _stickyGuardrailRuleLock = new object();

	private static readonly Dictionary<string, List<StickyGuardrailRuleState>> _stickyGuardrailRules = new Dictionary<string, List<StickyGuardrailRuleState>>(StringComparer.OrdinalIgnoreCase);

	private static readonly string[] StickyGuardrailFollowUpPhrases = new string[17]
	{
		"然后", "然后呢", "接着呢", "接下来呢", "那然后呢", "那接下来呢", "那我该怎么办", "我该怎么办", "下一步呢", "下一步怎么做",
		"具体怎么做", "具体呢", "细说", "继续说", "继续", "展开说说", "后面呢"
	};

	private const int MaxStickyGuardrailRulesPerTarget = 3;

	private static GuardrailEvalSnapshot _lastGuardrailEval;

	public static string GlobalPrompt => _guardrail?.GlobalPrompt ?? "";

	public static string GlobalGuardrail => _guardrail?.GlobalGuardrail ?? "";

	public static string DuelInstruction => _guardrail?.Duel?.TriggerInstruction ?? "";

	public static List<string> DuelTriggerKeywords => _guardrail?.Duel?.AcceptKeywords ?? new List<string>();

	public static bool RewardEnabled => _guardrail?.Reward?.IsEnabled == true;

	public static string RewardInstruction => _guardrail?.Reward?.Instruction ?? "";

	public static List<string> RewardTriggerKeywords => _guardrail?.Reward?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> RewardRuntimeInstructionTemplates => _guardrail?.Reward?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool LoanEnabled => _guardrail?.Loan?.IsEnabled == true;

	public static string LoanInstruction => _guardrail?.Loan?.Instruction ?? "";

	public static List<string> LoanTriggerKeywords => _guardrail?.Loan?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> LoanRuntimeInstructionTemplates => _guardrail?.Loan?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool SurroundingsEnabled => _guardrail?.Surroundings?.IsEnabled == true;

	public static string SurroundingsInstruction => _guardrail?.Surroundings?.Instruction ?? "";

	public static List<string> SurroundingsTriggerKeywords => _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>();

	public static string DuelNonHeroInstruction => _guardrail?.Duel?.NonHeroInstruction ?? "";

	public static string RewardNonHeroInstruction => _guardrail?.Reward?.NonHeroInstruction ?? "";

	public static string LoanNonHeroInstruction => _guardrail?.Loan?.NonHeroInstruction ?? "";

	private static bool GuardrailKnowledgeEnabled => _guardrail?.KnowledgeRetrieval?.IsEnabled ?? true;

	private static bool GuardrailKnowledgeSemanticFirst => _guardrail?.KnowledgeRetrieval?.SemanticFirst ?? true;

	private static int GuardrailKnowledgeTopK => ClampKnowledgeTopK(_guardrail?.KnowledgeRetrieval?.SemanticTopK ?? 4);

	public static bool KnowledgeRetrievalEnabled
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var enabled, out var _, out var _))
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
			if (TryGetKnowledgeFromMcm(out var _, out var semanticFirst, out var _))
			{
				return semanticFirst;
			}
			return GuardrailKnowledgeSemanticFirst;
		}
	}

	public static int KnowledgeSemanticTopK
	{
		get
		{
			if (TryGetKnowledgeFromMcm(out var _, out var _, out var topK))
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

	private static List<string> SplitGuardrailIntents(string input, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
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
				if (list.Count >= Math.Max(1, maxParts))
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
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		if (list.Count <= 0)
		{
			string text9 = NormalizeSemanticText(input);
			if (!string.IsNullOrWhiteSpace(text9))
			{
				list = IntentQueryOptimizer.OptimizeSplitIntents(new List<string> { text9 }, 1);
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

	private static float ComputeBuiltInIntentSemanticEvidence(string ruleTag, List<GuardrailIntentInput> queryInputs, out string bestSeed)
	{
		bestSeed = "";
		try
		{
			if (queryInputs == null || queryInputs.Count <= 0)
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
				for (int j = 0; j < queryInputs.Count; j++)
				{
					GuardrailIntentInput guardrailIntentInput = queryInputs[j];
					if (guardrailIntentInput?.Vector == null || guardrailIntentInput.Vector.Length == 0)
					{
						continue;
					}
					float num2 = DotProductNormalized(guardrailIntentInput.Vector, vec) * Math.Max(0f, guardrailIntentInput.Weight);
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

	internal static void TryStartBackgroundSemanticWarmup(string source)
	{
		try
		{
			long num = Volatile.Read(ref _guardrailConfigVersion);
			if (num <= 0)
			{
				num = 1L;
			}
			if (Volatile.Read(ref _guardrailWarmupState) == 2 && Volatile.Read(ref _guardrailWarmupVersion) == num)
			{
				return;
			}
			if (Interlocked.CompareExchange(ref _guardrailWarmupState, 1, 0) != 0)
			{
				return;
			}
			Interlocked.Exchange(ref _guardrailWarmupVersion, num);
			string warmupSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
			Logger.Log("GuardrailWarmup", $"start source={warmupSource} version={num}");
			Task.Run(delegate
			{
				RunGuardrailSemanticWarmup(warmupSource, num);
			});
		}
		catch
		{
		}
	}

	private static void RunGuardrailSemanticWarmup(string source, long version)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		int num = 0;
		int num2 = 0;
		string text = "";
		try
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			for (int i = 0; i < allEnabledRulePrompts.Count; i++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[i];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				List<string> list = BuildRuleSemanticSeeds(guardrailRulePromptConfig.Id, guardrailRulePromptConfig.Instruction ?? "", guardrailRulePromptConfig.TriggerKeywords);
				for (int j = 0; j < list.Count; j++)
				{
					string item = NormalizeSemanticText(list[j]);
					if (!string.IsNullOrWhiteSpace(item))
					{
						hashSet.Add(item);
					}
				}
			}
			num = hashSet.Count;
			foreach (string item3 in hashSet)
			{
				if (TryGetPhraseEmbedding(item3, out var vec) && vec != null && vec.Length != 0)
				{
					num2++;
				}
			}
		}
		catch (Exception ex)
		{
			text = ex.Message ?? "guardrail warmup exception";
		}
		stopwatch.Stop();
		bool flag = Volatile.Read(ref _guardrailConfigVersion) != version;
		if (flag)
		{
			Interlocked.Exchange(ref _guardrailWarmupState, 0);
			Interlocked.Exchange(ref _guardrailWarmupVersion, -1L);
		}
		else
		{
			Interlocked.Exchange(ref _guardrailWarmupState, 2);
		}
		Logger.Log("GuardrailWarmup", $"complete source={source} version={version} stale={flag} ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} seedCount={num} warmed={num2} error={text}");
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

	private static string BuildGuardrailEvalKey(string userText, string contextText, string secondaryText)
	{
		string text = NormalizeSemanticText(userText);
		string text2 = NormalizeSemanticText(contextText);
		string text3 = NormalizeSemanticText(secondaryText);
		return text + "||" + text2 + "||" + text3;
	}

	private static bool TryGetGuardrailEvalSnapshot(string userText, string secondaryText, out GuardrailEvalSnapshot snapshot)
	{
		snapshot = null;
		List<GuardrailIntentInput> list = new List<GuardrailIntentInput>();
		List<string> list2 = new List<string>();
		try
		{
			string runtimeGuardrailContext = GetRuntimeGuardrailContext();
			string text = BuildGuardrailEvalKey(userText, runtimeGuardrailContext, secondaryText);
			lock (_guardrailSemanticLock)
			{
				if (_lastGuardrailEval != null && string.Equals(_lastGuardrailEval.Key, text, StringComparison.Ordinal))
				{
					snapshot = _lastGuardrailEval;
					return snapshot != null && snapshot.Rules != null && snapshot.Rules.Count > 0;
				}
			}
			appendInputs(SplitGuardrailIntents(userText, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f);
			string text2 = NormalizeSemanticText(secondaryText);
			if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, NormalizeSemanticText(userText), StringComparison.Ordinal))
			{
				appendInputs(SplitGuardrailIntents(text2, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker, 1f);
			}
			if (list.Count <= 0)
			{
				try
				{
					Logger.Log("GuardrailSemantic", $"snapshot_fail reason=no_input_embeddings intentCount={list2.Count} input={NormalizeSemanticText(userText)}");
				}
				catch
				{
				}
				return false;
			}
			if (list2.Count > 1)
			{
				try
				{
					Logger.Log("GuardrailSemantic", string.Format("intent_split count={0} intents={1}", list2.Count, string.Join(" || ", list2)));
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
			Dictionary<string, GuardrailRulePromptConfig> dictionary = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
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
					for (int l = 0; l < list.Count; l++)
					{
						GuardrailIntentInput guardrailIntentInput2 = list[l];
						if (guardrailIntentInput2?.Vector == null || guardrailIntentInput2.Vector.Length == 0)
						{
							continue;
						}
						float num3 = DotProductNormalized(guardrailIntentInput2.Vector, vec3) * Math.Max(0f, guardrailIntentInput2.Weight);
						if (num3 > num)
						{
							num = num3;
							matchedSeed = text3;
							matchedIntent = guardrailIntentInput2.Text;
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
				float mixedRaw = (flag ? (num * (1f - num5) + num2 * num5) : num);
				GuardrailRuleEval guardrailRuleEval = new GuardrailRuleEval
				{
					RuleTag = id,
					MatchedSeed = matchedSeed,
					MatchedIntent = matchedIntent,
					RawInput = num,
					RawContext = num2,
					MixedRaw = mixedRaw,
					AmpScore = mixedRaw,
					RerankScore = mixedRaw
				};
				list4.Add(guardrailRuleEval);
				dictionary[id] = guardrailRulePromptConfig;
				guardrailEvalSnapshot.Rules[id] = guardrailRuleEval;
			}
			int guardrailReturnCapFromMcm = GuardrailRuleReturnCap;
			int num6 = Math.Max(1, list.Count);
			int guardrailRerankBudget = GetGuardrailRerankBudget(guardrailReturnCapFromMcm);
			int guardrailPerIntentRerank = GetGuardrailPerIntentRerank(guardrailRerankBudget, num6);
			int guardrailPerIntentRecall = GetGuardrailPerIntentRecall(guardrailPerIntentRerank);
			guardrailEvalSnapshot.IntentCount = num6;
			guardrailEvalSnapshot.ReturnCap = guardrailReturnCapFromMcm;
			guardrailEvalSnapshot.RerankPerIntent = guardrailPerIntentRerank;
			guardrailEvalSnapshot.RecallPerIntent = guardrailPerIntentRecall;
			OnnxCrossEncoderReranker onnxCrossEncoderReranker = null;
			bool flag2 = false;
			try
			{
				onnxCrossEncoderReranker = OnnxCrossEncoderReranker.Instance;
				flag2 = onnxCrossEncoderReranker != null && onnxCrossEncoderReranker.IsAvailable;
			}
			catch
			{
				flag2 = false;
			}
			string text4 = (flag2 ? ((list.Count > 1) ? "rerank_multi" : "rerank") : ((list.Count > 1) ? "semantic_multi" : "semantic"));
			guardrailEvalSnapshot.MatchMode = text4;
			Dictionary<string, GuardrailRuleAggregate> dictionary2 = new Dictionary<string, GuardrailRuleAggregate>(StringComparer.OrdinalIgnoreCase);
			for (int k = 0; k < list.Count; k++)
			{
				GuardrailIntentInput guardrailIntentInput = list[k];
				if (guardrailIntentInput?.Vector == null || guardrailIntentInput.Vector.Length == 0)
				{
					continue;
				}
				List<GuardrailRuleScore> list5 = new List<GuardrailRuleScore>();
				for (int l = 0; l < allEnabledRulePrompts.Count; l++)
				{
					GuardrailRulePromptConfig guardrailRulePromptConfig2 = allEnabledRulePrompts[l];
					if (guardrailRulePromptConfig2 == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig2.Id))
					{
						continue;
					}
					string id2 = guardrailRulePromptConfig2.Id;
					guardrailEvalSnapshot.Rules.TryGetValue(id2, out var value);
					List<string> list6 = BuildRuleSemanticSeeds(id2, guardrailRulePromptConfig2.Instruction ?? "", guardrailRulePromptConfig2.TriggerKeywords);
					float num7 = 0f;
					string text5 = "";
					for (int m = 0; m < list6.Count; m++)
					{
						string text6 = list6[m];
						if (string.IsNullOrWhiteSpace(text6) || !TryGetPhraseEmbedding(text6, out var vec3) || vec3 == null || vec3.Length == 0)
						{
							continue;
						}
						float num8 = DotProductNormalized(guardrailIntentInput.Vector, vec3) * Math.Max(0f, guardrailIntentInput.Weight);
						if (num8 > num7)
						{
							num7 = num8;
							text5 = text6;
						}
					}
					float num9 = ((flag && value != null) ? value.RawContext : 0f);
					float num10 = 0f;
					float num11 = (flag ? (num7 * (1f - num10) + num9 * num10) : num7);
					list5.Add(new GuardrailRuleScore
					{
						Rule = guardrailRulePromptConfig2,
						RawScore = num11,
						FinalScore = num11,
						MatchedSeed = text5,
						MatchedIntent = guardrailIntentInput.Text
					});
				}
				list5 = list5.OrderByDescending((GuardrailRuleScore x) => x.RawScore).ThenBy((GuardrailRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).Take(guardrailPerIntentRecall).ToList();
				if (list5.Count <= 0)
				{
					continue;
				}
				int num12 = Math.Min(guardrailPerIntentRerank, list5.Count);
				List<GuardrailRuleScore> list7 = new List<GuardrailRuleScore>();
				List<string> rerankTexts = null;
				List<float> rerankScores = null;
				bool flag3 = false;
				if (flag2)
				{
					rerankTexts = new List<string>(num12);
					for (int n = 0; n < num12; n++)
					{
						GuardrailRuleScore guardrailRuleScore = list5[n];
						rerankTexts.Add((guardrailRuleScore?.Rule == null) ? "" : BuildGuardrailRuleRerankText(guardrailRuleScore.Rule));
					}
					flag3 = onnxCrossEncoderReranker.TryScoreBatch(guardrailIntentInput.Text, rerankTexts, out rerankScores) && rerankScores != null && rerankScores.Count == num12;
				}
				for (int n = 0; n < num12; n++)
				{
					GuardrailRuleScore guardrailRuleScore = list5[n];
					if (guardrailRuleScore?.Rule == null)
					{
						continue;
					}
					float num13 = guardrailRuleScore.RawScore;
					if (flag2 && flag3 && rerankTexts != null && n < rerankTexts.Count && !string.IsNullOrWhiteSpace(rerankTexts[n]) && rerankScores != null && n < rerankScores.Count)
					{
						num13 = rerankScores[n] * Math.Max(0f, guardrailIntentInput.Weight);
					}
					list7.Add(new GuardrailRuleScore
					{
						Rule = guardrailRuleScore.Rule,
						RawScore = guardrailRuleScore.RawScore,
						FinalScore = num13,
						MatchedSeed = guardrailRuleScore.MatchedSeed,
						MatchedIntent = guardrailRuleScore.MatchedIntent
					});
				}
				List<GuardrailRuleScore> list8 = SelectGuardrailCandidateScores(list7, (flag2 && flag3) ? "cross_encoder" : "recall_fallback", guardrailIntentInput.Text, num12);
				for (int num14 = 0; num14 < list8.Count; num14++)
				{
					GuardrailRuleScore guardrailRuleScore2 = list8[num14];
					if (guardrailRuleScore2?.Rule == null)
					{
						continue;
					}
					string text8 = (guardrailRuleScore2.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text8) || !guardrailEvalSnapshot.Rules.TryGetValue(text8, out var value2))
					{
						continue;
					}
					if (!dictionary2.TryGetValue(text8, out var value3))
					{
						value3 = new GuardrailRuleAggregate
						{
							Eval = value2
						};
					}
					value3.ScoreSum += guardrailRuleScore2.FinalScore;
					value3.HitCount++;
					if (num14 + 1 < value3.BestRank)
					{
						value3.BestRank = num14 + 1;
					}
					if (guardrailRuleScore2.FinalScore >= value3.BestScore)
					{
						value3.BestScore = guardrailRuleScore2.FinalScore;
						value3.MatchedSeed = guardrailRuleScore2.MatchedSeed;
						value3.MatchedIntent = guardrailRuleScore2.MatchedIntent;
					}
					dictionary2[text8] = value3;
				}
			}
			for (int num15 = 0; num15 < list4.Count; num15++)
			{
				GuardrailRuleEval guardrailRuleEval2 = list4[num15];
				if (guardrailRuleEval2 != null)
				{
					guardrailRuleEval2.Candidate = false;
					guardrailRuleEval2.AmpScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.RerankScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.MatchMode = text4;
				}
			}
			int num16 = 0;
			if (dictionary2.Count > 0)
			{
				int num17 = Math.Max(guardrailReturnCapFromMcm * 2, guardrailPerIntentRerank * Math.Min(list.Count, 3));
				if (num17 < guardrailReturnCapFromMcm)
				{
					num17 = guardrailReturnCapFromMcm;
				}
				if (num17 > 24)
				{
					num17 = 24;
				}
				List<GuardrailRuleAggregate> list9 = (from x in dictionary2.Values
					orderby Math.Min(1f, x.ScoreSum / (float)Math.Max(1, x.HitCount) + (float)(x.HitCount - 1) * 0.08f) descending, x.BestRank
					select x).ThenBy((GuardrailRuleAggregate x) => x?.Eval?.RuleTag ?? "", StringComparer.OrdinalIgnoreCase).Take(num17).ToList();
				num16 = list9.Count;
				for (int num18 = 0; num18 < list9.Count; num18++)
				{
					GuardrailRuleAggregate guardrailRuleAggregate = list9[num18];
					if (guardrailRuleAggregate?.Eval == null)
					{
						continue;
					}
					float num19 = guardrailRuleAggregate.ScoreSum / (float)Math.Max(1, guardrailRuleAggregate.HitCount) + (float)(guardrailRuleAggregate.HitCount - 1) * 0.08f;
					if (num19 > 1f)
					{
						num19 = 1f;
					}
					guardrailRuleAggregate.Eval.Candidate = true;
					guardrailRuleAggregate.Eval.AmpScore = num19;
					guardrailRuleAggregate.Eval.RerankScore = guardrailRuleAggregate.BestScore;
					guardrailRuleAggregate.Eval.MatchMode = text4;
					if (!string.IsNullOrWhiteSpace(guardrailRuleAggregate.MatchedSeed))
					{
						guardrailRuleAggregate.Eval.MatchedSeed = guardrailRuleAggregate.MatchedSeed;
					}
					if (!string.IsNullOrWhiteSpace(guardrailRuleAggregate.MatchedIntent))
					{
						guardrailRuleAggregate.Eval.MatchedIntent = guardrailRuleAggregate.MatchedIntent;
					}
				}
			}
			list4 = list4.OrderByDescending((GuardrailRuleEval x) => x.Candidate ? 1 : 0).ThenByDescending((GuardrailRuleEval x) => x.Candidate ? x.AmpScore : x.MixedRaw).ThenBy((GuardrailRuleEval x) => x.RuleTag, StringComparer.OrdinalIgnoreCase).ToList();
			float num20 = ((list4.Count > 0) ? list4.Average((GuardrailRuleEval x) => x.Candidate ? x.AmpScore : x.MixedRaw) : 0f);
			float num21 = ((list4.Count > 1) ? ((list4[0].Candidate ? list4[0].AmpScore : list4[0].MixedRaw) - (list4[1].Candidate ? list4[1].AmpScore : list4[1].MixedRaw)) : 1f);
			for (int num22 = 0; num22 < list4.Count; num22++)
			{
				GuardrailRuleEval guardrailRuleEval3 = list4[num22];
				guardrailRuleEval3.Mean = num20;
				guardrailRuleEval3.Rank = num22 + 1;
				float num23 = -1f;
				string maxOtherTag = "";
				for (int num24 = 0; num24 < list4.Count; num24++)
				{
					if (num24 == num22)
					{
						continue;
					}
					GuardrailRuleEval guardrailRuleEval4 = list4[num24];
					float num25 = (guardrailRuleEval4.Candidate ? guardrailRuleEval4.AmpScore : guardrailRuleEval4.MixedRaw);
					if (num25 > num23)
					{
						num23 = num25;
						maxOtherTag = guardrailRuleEval4.RuleTag;
					}
				}
				float num26 = guardrailRuleEval3.Candidate ? guardrailRuleEval3.AmpScore : guardrailRuleEval3.MixedRaw;
				float delta = ((num23 < -0.5f) ? num26 : (num26 - num23));
				float num27 = 0f;
				float num28 = 0f;
				string bestSeed = "";
				bool lexicalAnchor = false;
				bool flag3 = guardrailRuleEval3.Candidate && guardrailRuleEval3.Rank <= guardrailReturnCapFromMcm;
				string rejectReason = (flag3 ? (text4 + "_return(" + guardrailRuleEval3.Rank + "/" + guardrailReturnCapFromMcm + ")") : (guardrailRuleEval3.Candidate ? (text4 + "_return_overflow") : (text4 + "_recall_miss")));
				guardrailRuleEval3.MaxOther = num23;
				guardrailRuleEval3.MaxOtherTag = maxOtherTag;
				guardrailRuleEval3.Delta = delta;
				guardrailRuleEval3.TopGap = num21;
				guardrailRuleEval3.IntentEvidence = num27;
				guardrailRuleEval3.IntentGate = num28;
				guardrailRuleEval3.IntentSeed = bestSeed;
				guardrailRuleEval3.LexicalAnchor = lexicalAnchor;
				guardrailRuleEval3.AbsHit = flag3;
				guardrailRuleEval3.RelHit = false;
				guardrailRuleEval3.HighAmpHit = false;
				guardrailRuleEval3.ForceHit = false;
				guardrailRuleEval3.RejectReason = rejectReason;
				guardrailRuleEval3.MatchMode = text4;
				guardrailRuleEval3.Hit = flag3;
			}
			try
			{
				Logger.Log("GuardrailSemantic", $"candidate_pool mode={text4} returnCap={guardrailReturnCapFromMcm} rerankBudget={guardrailRerankBudget} rerankPerIntent={guardrailPerIntentRerank} recallPerIntent={guardrailPerIntentRecall} intents={num6} got={num16}");
			}
			catch
			{
			}
			try
			{
				int count = list4.Count;
				int num29 = 0;
				for (int num30 = 0; num30 < list4.Count; num30++)
				{
					GuardrailRuleEval guardrailRuleEval5 = list4[num30];
					if (guardrailRuleEval5 != null)
					{
						if (guardrailRuleEval5.Hit)
						{
							num29++;
						}
						Logger.RecordHitRate("guardrail", guardrailRuleEval5.RuleTag ?? "__unknown__", guardrailRuleEval5.Hit, BuildSemanticHitRateDetail($"raw={guardrailRuleEval5.RawInput:0.000} rerank={guardrailRuleEval5.RerankScore:0.000} amp={guardrailRuleEval5.AmpScore:0.000} rank={guardrailRuleEval5.Rank} reason={guardrailRuleEval5.RejectReason}", secondaryText), userText);
					}
				}
				Logger.RecordHitRate("guardrail", "__query__", num29 > 0, BuildSemanticHitRateDetail($"hits={num29}/{count} inputLen={userText.Length}", secondaryText), userText);
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

		void appendInputs(List<string> intents, int perSourceLimit, float weight)
		{
			if (intents == null || intents.Count <= 0 || weight <= 0f || perSourceLimit <= 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < intents.Count; i++)
			{
				if (list.Count >= IntentQueryOptimizer.MaxCombinedIntentCount)
				{
					break;
				}
				string text4 = NormalizeSemanticText(intents[i]);
				if (!string.IsNullOrWhiteSpace(text4) && TryGetInputEmbedding(text4, out var vec) && vec != null && vec.Length != 0)
				{
					num++;
					if (num > perSourceLimit)
					{
						break;
					}
					list2.Add(text4);
					list.Add(new GuardrailIntentInput
					{
						Text = text4,
						Vector = vec,
						Weight = weight
					});
				}
			}
		}
	}

	private static string BuildGuardrailRuleRerankText(GuardrailRulePromptConfig rule)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			string text = NormalizeSemanticText(rule.Id);
			string text2 = NormalizeSemanticText(rule.Group);
			string text3 = NormalizeSemanticText(BuildRuleInstructionSeed(rule.Id, rule.Instruction));
			List<string> list = NormalizeStringList(rule.TriggerKeywords, 48);
			if (list.Count > 6)
			{
				list = list.Take(6).ToList();
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine("规则组: " + text2);
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.AppendLine("规则ID: " + text);
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine("用途: " + text3);
			}
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("触发词: " + string.Join(" / ", list));
			}
			return NormalizeSemanticText(stringBuilder.ToString());
		}
		catch
		{
			return "";
		}
	}

	private static int GetGuardrailRerankBudget(int returnCap)
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

	private static int GetGuardrailPerIntentRerank(int rerankBudget, int intentCount)
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

	private static int GetGuardrailPerIntentRecall(int rerankPerIntent)
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

	private static List<GuardrailRuleScore> SelectGuardrailCandidateScores(List<GuardrailRuleScore> scored, string source, string input, int topK)
	{
		List<GuardrailRuleScore> list = new List<GuardrailRuleScore>();
		try
		{
			int num = ((topK <= 0) ? 4 : topK);
			float num2 = 0.21f;
			List<GuardrailRuleScore> list2 = (from x in scored
				where x?.Rule != null && !float.IsNaN(x.FinalScore)
				orderby x.FinalScore descending, x.RawScore descending
				select x).ThenBy((GuardrailRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			float num3 = ((list2.Count > 0) ? list2[0].FinalScore : 0f);
			float num4 = ((list2.Count > 1) ? list2[1].FinalScore : 0f);
			float num5 = ((list2.Count > 0) ? list2[0].RawScore : 0f);
			float num6 = ((list2.Count > 1) ? list2[1].RawScore : 0f);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num7 = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				GuardrailRuleScore guardrailRuleScore = list2[i];
				if (guardrailRuleScore?.Rule == null || guardrailRuleScore.FinalScore < num2)
				{
					continue;
				}
				string text = (guardrailRuleScore.Rule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
				{
					list.Add(guardrailRuleScore);
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
					GuardrailRuleScore guardrailRuleScore2 = list2[j];
					if (guardrailRuleScore2?.Rule == null)
					{
						continue;
					}
					string text2 = (guardrailRuleScore2.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2) || hashSet.Add(text2))
					{
						list.Add(guardrailRuleScore2);
					}
				}
			}
			try
			{
				Logger.Log("GuardrailSemantic", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
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

	private static bool TryGetRuleEval(string userText, string secondaryText, string ruleTag, out GuardrailRuleEval eval)
	{
		eval = null;
		if (!TryGetGuardrailEvalSnapshot(userText, secondaryText, out var snapshot) || snapshot == null || snapshot.Rules == null)
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
		return IsGuardrailSemanticHit(input, null, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, List<string> triggerKeywords, string ruleTag, out string matchedKeyword, out float score)
	{
		return IsGuardrailSemanticHit(input, null, ruleTag, "", triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, string ruleTag, string ruleInstruction, List<string> triggerKeywords, out string matchedKeyword, out float score)
	{
		return IsGuardrailSemanticHit(input, null, ruleTag, ruleInstruction, triggerKeywords, out matchedKeyword, out score);
	}

	public static bool IsGuardrailSemanticHit(string input, string secondaryInput, string ruleTag, string ruleInstruction, List<string> triggerKeywords, out string matchedKeyword, out float score)
	{
		matchedKeyword = "";
		score = 0f;
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (TryGetRuleEval(text, secondaryInput, ruleTag, out var eval))
		{
			matchedKeyword = (string.IsNullOrWhiteSpace(eval.MatchedSeed) ? "semantic_seed" : eval.MatchedSeed);
			score = eval.AmpScore;
			string text2 = NormalizeSemanticText(eval.MatchedIntent);
			if (text2.Length > 48)
			{
				text2 = text2.Substring(0, 48);
			}
			Logger.Log("GuardrailSemantic", $"rule={ruleTag} hit={eval.Hit} mode={eval.MatchMode} raw={eval.RawInput:0.000} ctx={eval.RawContext:0.000} mixed={eval.MixedRaw:0.000} rerank={eval.RerankScore:0.000} amp={eval.AmpScore:0.000} delta={eval.Delta:0.000} topGap={eval.TopGap:0.000} rank={eval.Rank} candidate={eval.Candidate} other={eval.MaxOtherTag}@{eval.MaxOther:0.000} mean={eval.Mean:0.000} absHit={eval.AbsHit} relHit={eval.RelHit} highAmpHit={eval.HighAmpHit} forceHit={eval.ForceHit} intentEvidence={eval.IntentEvidence:0.000} intentGate={eval.IntentGate:0.000} lexicalAnchor={eval.LexicalAnchor} intentSeed={eval.IntentSeed} reason={eval.RejectReason} intent={text2}");
			return eval.Hit;
		}
		if (TryLexicalRuleKeywordHit(input, secondaryInput, triggerKeywords, out var lexicalKeyword))
		{
			matchedKeyword = lexicalKeyword;
			score = 1f;
			Logger.Log("GuardrailSemantic", $"rule={ruleTag} hit=True mode=lexical_fallback matched={lexicalKeyword}");
			return true;
		}
		Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=False mode=semantic_unavailable");
		try
		{
			Logger.RecordHitRate("guardrail", ruleTag ?? "__unknown__", hit: false, BuildSemanticHitRateDetail("reason=semantic_unavailable", secondaryInput), text);
		}
		catch
		{
		}
		return false;
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, int maxCount = 0, bool includeBuiltInRules = false)
	{
		return GetGuardrailSemanticRuleHits(input, null, maxCount, includeBuiltInRules);
	}

	public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount = 0, bool includeBuiltInRules = false)
	{
		List<GuardrailRuleHit> list = new List<GuardrailRuleHit>();
		try
		{
			int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
			string text = NormalizeSemanticText(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			if (!TryGetGuardrailEvalSnapshot(text, secondaryInput, out var snapshot) || snapshot?.Rules == null || snapshot.Rules.Count <= 0)
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
			if (num > 0 && list.Count > num)
			{
				list = list.Take(num).ToList();
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool TryLexicalRuleKeywordHit(string input, string secondaryInput, List<string> triggerKeywords, out string matchedKeyword)
	{
		matchedKeyword = "";
		try
		{
			if (triggerKeywords == null || triggerKeywords.Count <= 0)
			{
				return false;
			}
			string text = NormalizeSemanticText(input);
			string text2 = NormalizeSemanticText(secondaryInput);
			if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			for (int i = 0; i < triggerKeywords.Count; i++)
			{
				string text3 = NormalizeSemanticText(triggerKeywords[i]);
				if (string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				if ((!string.IsNullOrWhiteSpace(text) && text.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrWhiteSpace(text2) && text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					matchedKeyword = text3;
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static List<GuardrailRuleHit> GetGuardrailLexicalRuleHits(string input, string secondaryInput, int maxCount = 0, bool includeBuiltInRules = false)
	{
		List<GuardrailRuleHit> list = new List<GuardrailRuleHit>();
		try
		{
			int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
			Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
			if (dictionary == null || dictionary.Count <= 0)
			{
				return list;
			}
			foreach (GuardrailRulePromptConfig value in dictionary.Values)
			{
				string text = (value?.Id ?? "").Trim().ToLowerInvariant();
				if (value == null || !value.IsEnabled || string.IsNullOrWhiteSpace(text) || (!includeBuiltInRules && IsBuiltInRuleTag(text)))
				{
					continue;
				}
				if (!TryLexicalRuleKeywordHit(input, secondaryInput, value.TriggerKeywords, out var matchedKeyword))
				{
					continue;
				}
				string text2 = (value.Instruction ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.Add(new GuardrailRuleHit
					{
						RuleId = text,
						Group = (value.Group ?? ""),
						Priority = value.Priority,
						Score = 1f,
						MatchedSeed = matchedKeyword,
						Instruction = text2
					});
				}
			}
			list = (from x in list
				orderby x.Priority descending, x.Score descending
				select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
			if (num > 0 && list.Count > num)
			{
				list = list.Take(num).ToList();
			}
			if (list.Count > 0)
			{
				Logger.Log("GuardrailSemantic", $"lexical_rule_fallback count={list.Count} input={NormalizeSemanticText(input)}");
			}
		}
		catch
		{
		}
		return list;
	}

	private static string ResolveGuardrailStickyTargetKey()
	{
		try
		{
			string text = (_guardrailRuntimeTargetHeroId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "hero:" + text;
			}
		}
		catch
		{
		}
		try
		{
			int value = _guardrailRuntimeTargetAgentIndex.Value;
			if (value >= 0)
			{
				return "agent:" + value;
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (_guardrailRuntimeTargetCharacterId.Value ?? _guardrailRuntimeTargetTroopId.Value ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string text3 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text3) ? ("troop:" + text2) : ("troop:" + text2 + "@" + text3);
			}
		}
		catch
		{
		}
		try
		{
			Hero hero = ResolveConversationTargetHero();
			string text4 = (hero?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return "hero:" + text4;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject characterObject = ResolveConversationTargetCharacter();
			string text5 = (characterObject?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				string text6 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text6) ? ("troop:" + text5) : ("troop:" + text5 + "@" + text6);
			}
		}
		catch
		{
		}
		return "";
	}

	private static int GetStickyGuardrailTurnLimit(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		switch (text)
		{
		case "kingdom_service":
		case "marriage":
			return 3;
		default:
			return 0;
		}
	}

	private static bool IsStickyGuardrailFollowUpInput(string input)
	{
		string text = NormalizeSemanticText(input);
		if (string.IsNullOrWhiteSpace(text) || text.Length > 24)
		{
			return false;
		}
		for (int i = 0; i < StickyGuardrailFollowUpPhrases.Length; i++)
		{
			string value = StickyGuardrailFollowUpPhrases[i];
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool DidGuardrailRuleRecentlyComplete(string ruleId, string secondaryInput)
	{
		string text = (secondaryInput ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		switch ((ruleId ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
			return text.IndexOf("[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase) >= 0;
		case "reward":
			return text.IndexOf("[ACTION:GIVE_GOLD:", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:GIVE_ITEM:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "loan":
			return text.IndexOf("[ACTION:DEBT_", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:DEBT_PAY_", StringComparison.OrdinalIgnoreCase) >= 0;
		case "kingdom_service":
			return text.IndexOf("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "marriage":
			return text.IndexOf("[ACTION:MARRIAGE_", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase) >= 0;
		case "lords_hall_access":
			return text.IndexOf("[ACTION:OPEN_LORDS_HALL]", StringComparison.OrdinalIgnoreCase) >= 0;
		default:
			return false;
		}
	}

	private static bool ShouldStartStickyGuardrailRule(string input, string secondaryInput, GuardrailRuleHit hit, int rank)
	{
		if (hit == null)
		{
			return false;
		}
		string text = (hit.RuleId ?? "").Trim();
		if (GetStickyGuardrailTurnLimit(text) <= 0 || DidGuardrailRuleRecentlyComplete(text, secondaryInput))
		{
			return false;
		}
		if (hit.Score >= 0.999f)
		{
			return true;
		}
		if (TryGetRuleEval(input, secondaryInput, text, out var eval) && eval != null && eval.Hit)
		{
			if (eval.ForceHit || eval.HighAmpHit || eval.AbsHit)
			{
				return true;
			}
			if (eval.Rank <= 1 && eval.AmpScore >= 0.48f)
			{
				return true;
			}
		}
		if (rank == 0 && hit.Score >= 0.56f)
		{
			return true;
		}
		return hit.Score >= 0.62f;
	}

	private static bool ShouldContinueStickyGuardrailRule(StickyGuardrailRuleState state, GuardrailRulePromptConfig rule, string input, int currentLiveCount, string secondaryInput)
	{
		if (state == null || rule == null || state.RemainingCarryTurns <= 0 || DidGuardrailRuleRecentlyComplete(state.RuleId, secondaryInput))
		{
			return false;
		}
		if (currentLiveCount > 0)
		{
			return false;
		}
		if (TryLexicalRuleKeywordHit(input, null, rule.TriggerKeywords, out var _))
		{
			return true;
		}
		string text = NormalizeSemanticText(input);
		string text2 = NormalizeSemanticText(state.MatchedSeed);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return IsStickyGuardrailFollowUpInput(input);
	}

	private static float ApplyStickyGuardrailScoreDecay(float score, int maxCarryTurns, int carryTurnIndex)
	{
		float num = ((score > 0f) ? score : 0.6f);
		float num2;
		if (maxCarryTurns >= 3)
		{
			num2 = ((carryTurnIndex <= 1) ? 0.78f : ((carryTurnIndex == 2) ? 0.58f : 0.36f));
		}
		else
		{
			num2 = ((carryTurnIndex <= 1) ? 0.72f : 0.45f);
		}
		return Math.Max(0.18f, num * num2);
	}

	private static List<GuardrailRuleHit> MergeStickyGuardrailRuleHits(string input, string secondaryInput, List<GuardrailRuleHit> liveHits, int maxCount)
	{
		List<GuardrailRuleHit> list = (liveHits ?? new List<GuardrailRuleHit>()).Where((GuardrailRuleHit x) => x != null && !string.IsNullOrWhiteSpace(x.RuleId)).OrderByDescending((GuardrailRuleHit x) => x.Priority).ThenByDescending((GuardrailRuleHit x) => x.Score).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
		int num = ((maxCount > 0) ? ClampGuardrailReturnCap(maxCount) : GuardrailRuleReturnCap);
		string text = ResolveGuardrailStickyTargetKey();
		if (string.IsNullOrWhiteSpace(text))
		{
			return (num > 0 && list.Count > num) ? list.Take(num).ToList() : list;
		}
		Dictionary<string, GuardrailRulePromptConfig> dictionary = BuildRulePromptRegistry();
		if (dictionary == null || dictionary.Count <= 0)
		{
			return (num > 0 && list.Count > num) ? list.Take(num).ToList() : list;
		}
		HashSet<string> hashSet = new HashSet<string>(list.Select((GuardrailRuleHit x) => (x.RuleId ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
		List<StickyGuardrailRuleState> list2 = new List<StickyGuardrailRuleState>();
		lock (_stickyGuardrailRuleLock)
		{
			_stickyGuardrailRules.TryGetValue(text, out var value);
			value = value ?? new List<StickyGuardrailRuleState>();
			List<StickyGuardrailRuleState> list3 = new List<StickyGuardrailRuleState>();
			for (int i = 0; i < value.Count; i++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState = value[i];
				if (stickyGuardrailRuleState == null || string.IsNullOrWhiteSpace(stickyGuardrailRuleState.RuleId) || hashSet.Contains(stickyGuardrailRuleState.RuleId))
				{
					continue;
				}
				string text2 = stickyGuardrailRuleState.RuleId.Trim();
				if (GetStickyGuardrailTurnLimit(text2) <= 0 || !dictionary.TryGetValue(text2, out var value2) || value2 == null || !value2.IsEnabled)
				{
					continue;
				}
				if (!ShouldContinueStickyGuardrailRule(stickyGuardrailRuleState, value2, input, list.Count, secondaryInput))
				{
					continue;
				}
				stickyGuardrailRuleState.CarryTurnIndex = Math.Max(1, stickyGuardrailRuleState.MaxCarryTurns - stickyGuardrailRuleState.RemainingCarryTurns + 1);
				list2.Add(new StickyGuardrailRuleState
				{
					RuleId = text2,
					Group = stickyGuardrailRuleState.Group,
					Priority = stickyGuardrailRuleState.Priority,
					LastScore = stickyGuardrailRuleState.LastScore,
					MatchedSeed = stickyGuardrailRuleState.MatchedSeed,
					RemainingCarryTurns = stickyGuardrailRuleState.RemainingCarryTurns,
					MaxCarryTurns = stickyGuardrailRuleState.MaxCarryTurns,
					CarryTurnIndex = stickyGuardrailRuleState.CarryTurnIndex
				});
				stickyGuardrailRuleState.RemainingCarryTurns = Math.Max(0, stickyGuardrailRuleState.RemainingCarryTurns - 1);
				if (stickyGuardrailRuleState.RemainingCarryTurns > 0)
				{
					list3.Add(stickyGuardrailRuleState);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				GuardrailRuleHit guardrailRuleHit = list[j];
				if (!ShouldStartStickyGuardrailRule(input, secondaryInput, guardrailRuleHit, j))
				{
					continue;
				}
				string text3 = (guardrailRuleHit.RuleId ?? "").Trim();
				int stickyGuardrailTurnLimit = GetStickyGuardrailTurnLimit(text3);
				StickyGuardrailRuleState stickyGuardrailRuleState2 = list3.FirstOrDefault((StickyGuardrailRuleState x) => x != null && string.Equals(x.RuleId, text3, StringComparison.OrdinalIgnoreCase));
				if (stickyGuardrailRuleState2 == null)
				{
					list3.Add(new StickyGuardrailRuleState
					{
						RuleId = text3,
						Group = (guardrailRuleHit.Group ?? ""),
						Priority = guardrailRuleHit.Priority,
						LastScore = guardrailRuleHit.Score,
						MatchedSeed = (guardrailRuleHit.MatchedSeed ?? ""),
						RemainingCarryTurns = stickyGuardrailTurnLimit,
						MaxCarryTurns = stickyGuardrailTurnLimit
					});
					continue;
				}
				stickyGuardrailRuleState2.Group = (guardrailRuleHit.Group ?? stickyGuardrailRuleState2.Group);
				stickyGuardrailRuleState2.Priority = guardrailRuleHit.Priority;
				stickyGuardrailRuleState2.LastScore = guardrailRuleHit.Score;
				stickyGuardrailRuleState2.MatchedSeed = (guardrailRuleHit.MatchedSeed ?? stickyGuardrailRuleState2.MatchedSeed);
				stickyGuardrailRuleState2.RemainingCarryTurns = stickyGuardrailTurnLimit;
				stickyGuardrailRuleState2.MaxCarryTurns = stickyGuardrailTurnLimit;
			}
			list3 = list3.OrderByDescending((StickyGuardrailRuleState x) => x.Priority).ThenByDescending((StickyGuardrailRuleState x) => x.LastScore).ThenBy((StickyGuardrailRuleState x) => x.RuleId, StringComparer.OrdinalIgnoreCase).Take(MaxStickyGuardrailRulesPerTarget).ToList();
			if (list3.Count > 0)
			{
				_stickyGuardrailRules[text] = list3;
			}
			else
			{
				_stickyGuardrailRules.Remove(text);
			}
		}
		if (list2.Count > 0)
		{
			for (int k = 0; k < list2.Count; k++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState3 = list2[k];
				if (stickyGuardrailRuleState3 == null || hashSet.Contains(stickyGuardrailRuleState3.RuleId) || !dictionary.TryGetValue(stickyGuardrailRuleState3.RuleId, out var value3) || value3 == null)
				{
					continue;
				}
				list.Add(new GuardrailRuleHit
				{
					RuleId = stickyGuardrailRuleState3.RuleId,
					Group = stickyGuardrailRuleState3.Group,
					Priority = stickyGuardrailRuleState3.Priority,
					Score = ApplyStickyGuardrailScoreDecay(stickyGuardrailRuleState3.LastScore, stickyGuardrailRuleState3.MaxCarryTurns, stickyGuardrailRuleState3.CarryTurnIndex),
					MatchedSeed = (stickyGuardrailRuleState3.MatchedSeed ?? ""),
					Instruction = (value3.Instruction ?? "")
				});
			}
		}
		list = list.OrderByDescending((GuardrailRuleHit x) => x.Priority).ThenByDescending((GuardrailRuleHit x) => x.Score).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
		if (num > 0 && list.Count > num)
		{
			list = list.Take(num).ToList();
		}
		try
		{
			Logger.Log("GuardrailSemantic", $"sticky_rule_merge target={text} live={hashSet.Count} sticky={list2.Count} final={list.Count}");
		}
		catch
		{
		}
		return list;
	}

	private static string BuildExtraRuleHitDebugDetail(string input, string secondaryInput, GuardrailRuleHit hit)
	{
		try
		{
			if (hit == null)
			{
				return "";
			}
			string text = NormalizeSemanticText(input);
			string text2 = (hit.RuleId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2) && TryGetRuleEval(text, secondaryInput, text2, out var eval) && eval != null)
			{
				string text3 = NormalizeSemanticText(eval.MatchedIntent);
				if (text3.Length > 48)
				{
					text3 = text3.Substring(0, 48);
				}
				string text4 = NormalizeSemanticText(eval.MatchedSeed);
				if (text4.Length > 32)
				{
					text4 = text4.Substring(0, 32);
				}
				return $" raw={eval.RawInput:0.000} ctx={eval.RawContext:0.000} mixed={eval.MixedRaw:0.000} rerank={eval.RerankScore:0.000} amp={eval.AmpScore:0.000} rank={eval.Rank} candidate={eval.Candidate} other={eval.MaxOtherTag}@{eval.MaxOther:0.000} mean={eval.Mean:0.000} reason={eval.RejectReason} lexicalAnchor={eval.LexicalAnchor} matchedSeed={JsonConvert.ToString(text4)} intent={JsonConvert.ToString(text3)}";
			}
			string text5 = NormalizeSemanticText(hit.MatchedSeed);
			if (text5.Length > 32)
			{
				text5 = text5.Substring(0, 32);
			}
			if (!string.IsNullOrWhiteSpace(text5))
			{
				return " source=lexical_fallback matchedSeed=" + JsonConvert.ToString(text5);
			}
			return " source=unknown";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules = 0)
	{
		return BuildMatchedExtraRuleInstructions(input, null, maxRules, hasAnyHero: true);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, int maxRules, bool hasAnyHero)
	{
		return BuildMatchedExtraRuleInstructions(input, null, maxRules, hasAnyHero);
	}

	public static string BuildMatchedExtraRuleInstructions(string input, string secondaryInput, int maxRules, bool hasAnyHero)
	{
		try
		{
			List<GuardrailRuleHit> guardrailSemanticRuleHits = GetGuardrailSemanticRuleHits(input, secondaryInput, maxRules);
			if (guardrailSemanticRuleHits == null || guardrailSemanticRuleHits.Count <= 0)
			{
				guardrailSemanticRuleHits = GetGuardrailLexicalRuleHits(input, secondaryInput, maxRules);
			}
			guardrailSemanticRuleHits = MergeStickyGuardrailRuleHits(input, secondaryInput, guardrailSemanticRuleHits, maxRules);
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
				if (string.Equals(text, "meeting_taunt", StringComparison.OrdinalIgnoreCase))
				{
					string text4 = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter(), ResolveConversationTargetAgentIndex());
					if (!string.IsNullOrWhiteSpace(text4))
					{
						value = text4;
					}
				}
				if (hasAnyHero && string.Equals(text, "npc_major_actions", StringComparison.OrdinalIgnoreCase))
				{
					string text5 = MyBehavior.BuildNpcMajorActionsRuntimeInstructionForExternal(ResolveConversationTargetHero());
					if (!string.IsNullOrWhiteSpace(text5))
					{
						value = text5;
					}
				}
				if (hasAnyHero && string.Equals(text, "npc_recent_actions", StringComparison.OrdinalIgnoreCase))
				{
					string text6 = MyBehavior.BuildNpcRecentActionsRuntimeInstructionForExternal(ResolveConversationTargetHero());
					if (!string.IsNullOrWhiteSpace(text6))
					{
						value = text6;
					}
				}
				if (string.Equals(text, "lords_hall_access", StringComparison.OrdinalIgnoreCase))
				{
					string text7 = BuildRuntimeLordsHallAccessInstruction();
					if (!string.IsNullOrWhiteSpace(text7))
					{
						value = text7;
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
						Logger.Log("GuardrailSemantic", $"extra_rule_hit rule={text} score={guardrailRuleHit.Score:0.000} group={guardrailRuleHit.Group} priority={guardrailRuleHit.Priority} nonHero={!hasAnyHero}{BuildExtraRuleHitDebugDetail(input, secondaryInput, guardrailRuleHit)}");
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

	public static void SetGuardrailRuntimeTargetCharacter(string characterId)
	{
		try
		{
			_guardrailRuntimeTargetCharacterId.Value = (characterId ?? "").Trim();
		}
		catch
		{
			_guardrailRuntimeTargetCharacterId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetTroop(string troopId)
	{
		try
		{
			_guardrailRuntimeTargetTroopId.Value = ((troopId ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetTroopId.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetUnnamedRank(string unnamedRank)
	{
		try
		{
			_guardrailRuntimeTargetUnnamedRank.Value = ((unnamedRank ?? "").Trim().ToLowerInvariant() ?? "");
		}
		catch
		{
			_guardrailRuntimeTargetUnnamedRank.Value = "";
		}
	}

	public static void SetGuardrailRuntimeTargetAgentIndex(int agentIndex)
	{
		try
		{
			_guardrailRuntimeTargetAgentIndex.Value = agentIndex;
		}
		catch
		{
			_guardrailRuntimeTargetAgentIndex.Value = -1;
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

	private static int ResolveRuntimeTrustValue(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			if (targetHero != null)
			{
				return RewardSystemBehavior.Instance?.GetEffectiveTrust(targetHero) ?? 0;
			}
			if (targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
			{
				return RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(Settlement.CurrentSettlement, kind);
			}
		}
		catch
		{
		}
		return 0;
	}

	private static Dictionary<string, string> BuildLoanRuntimeTokens(Hero targetHero, CharacterObject targetCharacter = null)
	{
		int num = 0;
		int trustLevelIndex = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			trustLevelIndex = 6;
		}
		string text = trustLevelIndex switch
		{
			1 => "彻底不信", 
			2 => "极度怀疑", 
			3 => "强烈戒备", 
			4 => "不信任", 
			5 => "保留", 
			_ => "观望", 
		};
		string text2 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = text2,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = trustLevelIndex.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num),
			["trustAttitude"] = text
		};
	}

	public static string BuildRuntimeLoanInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(hero, characterObject));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(LoanRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildLoanRuntimeTokens(hero, characterObject));
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text.Trim();
				}
			}
		}
		catch
		{
		}
		return LoanInstruction;
	}

	private static Dictionary<string, string> BuildRewardRuntimeTokens(Hero targetHero, CharacterObject targetCharacter = null)
	{
		int num = 0;
		int trustLevelIndex = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			trustLevelIndex = 6;
		}
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = text,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = trustLevelIndex.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num)
		};
	}

	public static string BuildRuntimeRewardInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? ResolveConversationTargetHero();
			CharacterObject characterObject = targetCharacter ?? ResolveConversationTargetCharacter();
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(hero, characterObject));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(RewardRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildRewardRuntimeTokens(hero, characterObject));
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (num <= 1)
					{
						return text.Trim();
					}
					string text2 = (RewardInstruction ?? "").Trim();
					return string.IsNullOrWhiteSpace(text2) ? text.Trim() : (text.Trim() + "\n" + text2);
				}
			}
		}
		catch
		{
		}
		return RewardInstruction;
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

	private static CharacterObject ResolveConversationTargetCharacter()
	{
		try
		{
			string text = (_guardrailRuntimeTargetCharacterId.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				CharacterObject characterObject = CharacterObject.All?.FirstOrDefault((CharacterObject x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (characterObject != null)
				{
					return characterObject;
				}
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			if (oneToOneConversationCharacter != null)
			{
				return oneToOneConversationCharacter;
			}
		}
		catch
		{
		}
		return null;
	}

	private static int ResolveConversationTargetAgentIndex()
	{
		try
		{
			return _guardrailRuntimeTargetAgentIndex.Value;
		}
		catch
		{
			return -1;
		}
	}

	private static string ResolveRuntimeTargetUnnamedRank()
	{
		try
		{
			return (_guardrailRuntimeTargetUnnamedRank.Value ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveRuntimeTargetTroopId()
	{
		try
		{
			return (_guardrailRuntimeTargetTroopId.Value ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildRuntimeKingdomServiceInstruction()
	{
		try
		{
			Dictionary<string, string> dictionary = BuildKingdomServiceRuntimeTokens(out var playerClan, out var kingdom, out var flag, out var kingdom2, out var flag2, out var num, out var num2, out var num3, out var num4, out var num5, out var num6);
			if (playerClan == null)
			{
				return "";
			}
			string text = ResolveKingdomServiceRuntimeStateKey(kingdom, flag, kingdom2, flag2, num, num2, num3, num4, num5, num6);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			return ResolveKingdomServiceRuntimeText(text, forConstraint: false, dictionary);
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveKingdomServiceRuntimeStateKey(Kingdom playerKingdom, bool isMercenaryService, Kingdom targetKingdom, bool isSameKingdom, int playerTier, int mercTier, int vassalTier, int mercTrustMin, int vassalTrustMin, int currentTrust)
	{
		if (playerKingdom == null)
		{
			if (currentTrust < mercTrustMin)
			{
				return "no_kingdom_trust_below_merc";
			}
			if (playerTier < mercTier)
			{
				return "no_kingdom_tier_below_merc";
			}
			if (playerTier < vassalTier)
			{
				return "no_kingdom_tier_merc_only";
			}
			if (currentTrust < vassalTrustMin)
			{
				return "no_kingdom_trust_merc_only";
			}
			return "no_kingdom";
		}
		if (isMercenaryService)
		{
			if (targetKingdom == null)
			{
				return "mercenary_target_unknown";
			}
			if (isSameKingdom)
			{
				if (playerTier < vassalTier)
				{
					return "mercenary_same_kingdom_tier_vassal_locked";
				}
				if (currentTrust < vassalTrustMin)
				{
					return "mercenary_same_kingdom_trust_vassal_locked";
				}
				return "mercenary_same_kingdom";
			}
			return "mercenary_other_kingdom";
		}
		if (targetKingdom == null)
		{
			return "vassal_target_unknown";
		}
		if (isSameKingdom)
		{
			return "vassal_same_kingdom";
		}
		return "vassal_other_kingdom";
	}

	private static bool ShouldAppendKingdomServiceConstraint(string stateKey)
	{
		switch ((stateKey ?? "").Trim().ToLowerInvariant())
		{
		case "no_player_clan":
		case "no_kingdom_tier_below_merc":
		case "no_kingdom_tier_merc_only":
		case "no_kingdom_tier_full":
		case "mercenary_same_kingdom_tier_vassal_locked":
		case "mercenary_same_kingdom_tier_vassal_ready":
			return true;
		default:
			return false;
		}
	}

	private static Dictionary<string, string> BuildKingdomServiceRuntimeTokens(out Clan playerClan, out Kingdom playerKingdom, out bool isMercenaryService, out Kingdom targetKingdom, out bool isSameKingdom, out int playerTier, out int mercTier, out int vassalTier, out int mercTrustMin, out int vassalTrustMin, out int currentTrust)
	{
		playerClan = Clan.PlayerClan;
		playerKingdom = playerClan?.Kingdom;
		isMercenaryService = playerClan?.IsUnderMercenaryService == true;
		targetKingdom = ResolveConversationTargetKingdom();
		isSameKingdom = playerKingdom != null && targetKingdom != null && playerKingdom == targetKingdom;
		playerTier = playerClan?.Tier ?? 0;
		mercTier = 1;
		vassalTier = 2;
		mercTrustMin = 5;
		vassalTrustMin = 20;
		currentTrust = 0;
		try
		{
			mercTier = Campaign.Current?.Models?.ClanTierModel?.MercenaryEligibleTier ?? 1;
		}
		catch
		{
			mercTier = 1;
		}
		try
		{
			vassalTier = Campaign.Current?.Models?.ClanTierModel?.VassalEligibleTier ?? 2;
		}
		catch
		{
			vassalTier = 2;
		}
		Hero hero = ResolveConversationTargetHero();
		if (hero == null)
		{
			hero = targetKingdom?.Leader;
		}
		if (hero == null)
		{
			hero = playerKingdom?.Leader;
		}
		try
		{
			currentTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0;
		}
		catch
		{
			currentTrust = 0;
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerKingdom"] = (playerKingdom?.Name?.ToString() ?? ""),
			["playerKingdomId"] = (playerKingdom?.StringId ?? ""),
			["targetKingdom"] = (targetKingdom?.Name?.ToString() ?? ""),
			["targetKingdomId"] = (targetKingdom?.StringId ?? ""),
			["playerTier"] = playerTier.ToString(),
			["mercTier"] = mercTier.ToString(),
			["vassalTier"] = vassalTier.ToString(),
			["trustMerc"] = mercTrustMin.ToString(),
			["trustVassal"] = vassalTrustMin.ToString(),
			["trustCurrent"] = currentTrust.ToString(),
			["trustMercGap"] = Math.Max(0, mercTrustMin - currentTrust).ToString(),
			["trustVassalGap"] = Math.Max(0, vassalTrustMin - currentTrust).ToString()
		};
	}

	private static bool IsRuntimeTargetCastleGuard(int agentIndex)
	{
		try
		{
			if (agentIndex < 0 || Mission.Current == null)
			{
				return false;
			}
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			if (agent == null || !(agent.Character is CharacterObject characterObject) || !characterObject.IsSoldier)
			{
				return false;
			}
			AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			bool flag = false;
			if (agentNavigator != null)
			{
				flag = agentNavigator.TargetUsableMachine != null && agent.IsUsingGameObject && agentNavigator.TargetUsableMachine.GameEntity.HasTag("sp_guard_castle");
				if (!flag && (agentNavigator.SpecialTargetTag == "sp_guard_castle" || agentNavigator.SpecialTargetTag == "sp_guard"))
				{
					Location lordsHallLocation = LocationComplex.Current?.GetLocationWithId("lordshall");
					MissionAgentHandler missionBehavior = Mission.Current.GetMissionBehavior<MissionAgentHandler>();
					if (lordsHallLocation != null && missionBehavior?.TownPassageProps != null)
					{
						UsableMachine usableMachine = missionBehavior.TownPassageProps.FirstOrDefault((UsableMachine x) => x is Passage passage && passage.ToLocation == lordsHallLocation);
						if (usableMachine != null && usableMachine.GameEntity.GlobalPosition.DistanceSquared(agent.Position) < 100f)
						{
							flag = true;
						}
					}
				}
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	private static string DescribeLordsHallAccessReason(SettlementAccessModel.AccessDetails accessDetails)
	{
		try
		{
			switch (accessDetails.AccessLimitationReason)
			{
			case SettlementAccessModel.AccessLimitationReason.ClanTier:
				return "玩家家族等级不足";
			case SettlementAccessModel.AccessLimitationReason.CrimeRating:
				return "玩家在当地有犯罪评级";
			case SettlementAccessModel.AccessLimitationReason.Disguised:
				return "当前只能靠伪装混入";
			case SettlementAccessModel.AccessLimitationReason.LocationEmpty:
				return "领主大厅当前无人可见";
			default:
				return (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess) ? "原生规则允许直接进入" : "原生规则不允许直接进入";
			}
		}
		catch
		{
			return "";
		}
	}

	private static bool TryBuildLordsHallAccessRuntimeState(out string stateKey, out Dictionary<string, string> tokens)
	{
		stateKey = "not_applicable";
		tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			Settlement settlement = Settlement.CurrentSettlement;
			int agentIndex = ResolveConversationTargetAgentIndex();
			string text = ResolveRuntimeTargetUnnamedRank();
			string text2 = ResolveRuntimeTargetTroopId();
			int num = 0;
			int num2 = Hero.MainHero?.Gold ?? 0;
			bool flag = !string.IsNullOrWhiteSpace(text2) && string.Equals(text, "soldier", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace((_guardrailRuntimeTargetHeroId.Value ?? "").Trim());
			bool flag2 = flag && IsRuntimeTargetCastleGuard(agentIndex);
			string text3 = MyBehavior.BuildRuleTargetKeyForExternal(null, null, agentIndex);
			if (string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text2))
			{
				text3 = "troop:" + text2;
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				try
				{
					num = ShoutBehavior.CurrentInstance?.GetRecentNonHeroGoldForRuleTarget(text3) ?? 0;
				}
				catch
				{
					num = 0;
				}
			}
			tokens["settlementName"] = settlement?.Name?.ToString() ?? "";
			tokens["settlementId"] = settlement?.StringId ?? "";
			tokens["troopId"] = text2 ?? "";
			tokens["targetRank"] = text ?? "";
			tokens["targetKey"] = text3 ?? "";
			tokens["prepaidGold"] = num.ToString();
			tokens["playerGold"] = num2.ToString();
			tokens["playerClanTier"] = ((Clan.PlayerClan?.Tier).GetValueOrDefault()).ToString();
			tokens["accessReason"] = "";
			tokens["guideBribeGold"] = "0";
			if (settlement == null || !settlement.IsTown || !flag2)
			{
				stateKey = "not_applicable";
				return true;
			}
			SettlementAccessModel settlementAccessModel = Campaign.Current?.Models?.SettlementAccessModel;
			if (settlementAccessModel == null)
			{
				stateKey = "denied_no_bribe";
				return true;
			}
			SettlementAccessModel.AccessDetails accessDetails = default(SettlementAccessModel.AccessDetails);
			settlementAccessModel.CanMainHeroEnterLordsHall(settlement, out accessDetails);
			bool disableOption = false;
			TextObject disabledText = null;
			bool flag3 = settlementAccessModel.CanMainHeroAccessLocation(settlement, "lordshall", out disableOption, out disabledText);
			int bribeToEnterLordsHall = Campaign.Current?.Models?.BribeCalculationModel?.GetBribeToEnterLordsHall(settlement) ?? 0;
			tokens["accessReason"] = DescribeLordsHallAccessReason(accessDetails);
			tokens["guideBribeGold"] = bribeToEnterLordsHall.ToString();
			if (flag3)
			{
				stateKey = "allowed_directly";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Bribe && bribeToEnterLordsHall > 0)
			{
				stateKey = "denied_but_bribe_available";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess && accessDetails.LimitedAccessSolution == SettlementAccessModel.LimitedAccessSolution.Disguise)
			{
				stateKey = "disguise_only";
				return true;
			}
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.NoAccess && accessDetails.AccessLimitationReason == SettlementAccessModel.AccessLimitationReason.LocationEmpty)
			{
				stateKey = "no_one_inside";
				return true;
			}
			stateKey = "denied_no_bribe";
			return true;
		}
		catch
		{
			stateKey = "not_applicable";
			tokens = tokens ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			return false;
		}
	}

	private static string BuildRuntimeLordsHallAccessInstruction()
	{
		try
		{
			if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
			{
				return ResolveRuleRuntimeText("lords_hall_access", stateKey, forConstraint: false, tokens);
			}
		}
		catch
		{
		}
		return "";
	}

	// Used by shout prompt assembly to keep the guard rule always present for gate guards.
	public static string BuildRuntimeLordsHallAccessInstructionForExternal()
	{
		try
		{
			if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
			{
				string text = (stateKey ?? "").Trim().ToLowerInvariant();
				if (string.Equals(text, "not_applicable", StringComparison.OrdinalIgnoreCase))
				{
					return "";
				}
				return ResolveRuleRuntimeText("lords_hall_access", text, forConstraint: false, tokens);
			}
		}
		catch
		{
		}
		return "";
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
			if (text == "npc_major_actions")
			{
				return MyBehavior.BuildNpcActionsRuntimeConstraintHintForExternal(ResolveConversationTargetHero(), recentOnly: false);
			}
			if (text == "npc_recent_actions")
			{
				return MyBehavior.BuildNpcActionsRuntimeConstraintHintForExternal(ResolveConversationTargetHero(), recentOnly: true);
			}
			if (text == "lords_hall_access")
			{
				if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens))
				{
					return ResolveRuleRuntimeText("lords_hall_access", stateKey, forConstraint: true, tokens);
				}
				return "";
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
			Dictionary<string, string> dictionary = BuildKingdomServiceRuntimeTokens(out playerClan, out var kingdom, out var flag, out var kingdom2, out var flag2, out var num, out var num2, out var num3, out var num4, out var num5, out var num6);
			string text2 = ResolveKingdomServiceRuntimeStateKey(kingdom, flag, kingdom2, flag2, num, num2, num3, num4, num5, num6);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return "";
			}
			if (string.Equals(text2, "no_kingdom", StringComparison.OrdinalIgnoreCase))
			{
				text2 = "no_kingdom_tier_full";
			}
			else if (string.Equals(text2, "mercenary_same_kingdom", StringComparison.OrdinalIgnoreCase))
			{
				text2 = "mercenary_same_kingdom_tier_vassal_ready";
			}
			if (!ShouldAppendKingdomServiceConstraint(text2))
			{
				return "";
			}
			return ResolveKingdomServiceRuntimeText(text2, forConstraint: true, dictionary);
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
		if (v > 12)
		{
			v = 12;
		}
		return v;
	}

	public static int GuardrailRuleReturnCap => GetGuardrailReturnCapFromMcm();

	private static int ClampGuardrailReturnCap(int v)
	{
		if (v < 1)
		{
			v = 1;
		}
		if (v > 12)
		{
			v = 12;
		}
		return v;
	}

	private static int GetGuardrailReturnCapFromMcm()
	{
		try
		{
			DuelSettings duelSettings = TryGetMcmSettings();
			if (duelSettings != null)
			{
				return ClampGuardrailReturnCap(duelSettings.GuardrailDirectTopN);
			}
		}
		catch
		{
		}
		return 4;
	}

	private static bool TryGetKnowledgeFromMcm(out bool enabled, out bool semanticFirst, out int topK)
	{
		enabled = true;
		semanticFirst = true;
		topK = 4;
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
			long num = Interlocked.Increment(ref _guardrailConfigVersion);
			Interlocked.Exchange(ref _guardrailWarmupState, 0);
			Interlocked.Exchange(ref _guardrailWarmupVersion, -1L);
			_guardrailSemanticRuntimeContext.Value = "";
			_guardrailRuntimeTargetKingdomId.Value = "";
			int valueOrDefault = (_guardrail?.Duel?.AcceptKeywords?.Count).GetValueOrDefault();
			int valueOrDefault2 = (_guardrail?.Reward?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault3 = (_guardrail?.Loan?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault4 = (_guardrail?.Surroundings?.TriggerKeywords?.Count).GetValueOrDefault();
			int valueOrDefault5 = (_guardrail?.RulePrompts?.Count).GetValueOrDefault();
			int num2 = 0;
			try
			{
				num2 = GetAllEnabledRulePrompts().Count;
			}
			catch
			{
				num2 = 0;
			}
			string text = (KnowledgeRetrievalFromMcm ? "MCM" : "Guardrail");
			Logger.Log("AIConfig", string.Format("配置加载成功。触发词(决斗/奖励/借贷/地理)={0}/{1}/{2}/{3}，扩展规则={4}，启用规则总数={5}。规则返回上限={6}。知识检索({7})：{8}（语义优先={9}, returnCap={10}）。", valueOrDefault, valueOrDefault2, valueOrDefault3, valueOrDefault4, valueOrDefault5, num2, GetGuardrailReturnCapFromMcm(), text, KnowledgeRetrievalEnabled ? "开启" : "关闭", KnowledgeSemanticFirst, KnowledgeSemanticTopK));
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
		return GetLoreContext(inputText, npcHero, null);
	}

	public static string GetLoreContext(string inputText, Hero npcHero, string secondaryInput)
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
				string text = instance.BuildLoreContext(inputText, npcHero, secondaryInput);
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
		return GetLoreContext(inputText, npcCharacter, kingdomIdOverride, null);
	}

	public static string GetLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride, string secondaryInput)
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
				string text = instance.BuildLoreContext(inputText, npcCharacter, kingdomIdOverride, secondaryInput);
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
