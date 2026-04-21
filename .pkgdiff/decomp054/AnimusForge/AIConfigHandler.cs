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
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

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

	public static string GlobalPrompt => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.GlobalPrompt ?? "");

	public static string GlobalGuardrail => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.GlobalGuardrail ?? "");

	public static string DuelInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.TriggerInstruction ?? "");

	public static List<string> DuelTriggerKeywords => _guardrail?.Duel?.AcceptKeywords ?? new List<string>();

	public static bool RewardEnabled
	{
		get
		{
			GuardrailConfigModel guardrail = _guardrail;
			return guardrail != null && guardrail.Reward?.IsEnabled == true;
		}
	}

	public static string RewardInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Reward?.Instruction ?? "");

	public static List<string> RewardTriggerKeywords => _guardrail?.Reward?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> RewardRuntimeInstructionTemplates => _guardrail?.Reward?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool LoanEnabled
	{
		get
		{
			GuardrailConfigModel guardrail = _guardrail;
			return guardrail != null && guardrail.Loan?.IsEnabled == true;
		}
	}

	public static string LoanInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Loan?.Instruction ?? "");

	public static List<string> LoanTriggerKeywords => _guardrail?.Loan?.TriggerKeywords ?? new List<string>();

	public static Dictionary<string, string> LoanRuntimeInstructionTemplates => _guardrail?.Loan?.RuntimeInstructionTemplates ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool SurroundingsEnabled
	{
		get
		{
			GuardrailConfigModel guardrail = _guardrail;
			return guardrail != null && guardrail.Surroundings?.IsEnabled == true;
		}
	}

	public static string SurroundingsInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Surroundings?.Instruction ?? "");

	public static List<string> SurroundingsTriggerKeywords => _guardrail?.Surroundings?.TriggerKeywords ?? new List<string>();

	public static string DuelNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Duel?.NonHeroInstruction ?? "");

	public static string RewardNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Reward?.NonHeroInstruction ?? "");

	public static string LoanNonHeroInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.Loan?.NonHeroInstruction ?? "");

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

	public static bool DuelStakeEnabled
	{
		get
		{
			GuardrailConfigModel guardrail = _guardrail;
			return guardrail != null && guardrail.DuelStake?.IsEnabled == true;
		}
	}

	public static string DuelStakePlayerWinInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.DuelStake?.PlayerWinInstruction ?? "");

	public static string DuelStakeNpcWinInstruction => ApplyPlayerDisplayNameToGuardrailText(_guardrail?.DuelStake?.NpcWinInstruction ?? "");

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
			return ApplyPlayerDisplayNameToGuardrailText(stringBuilder.ToString().Trim());
		}
	}

	public static int GuardrailRuleReturnCap => GetGuardrailReturnCapFromMcm();

	private static string BuildSemanticHitRateDetail(string detail, string secondaryText)
	{
		string text = (detail ?? "").Trim();
		string text2 = NormalizeSemanticText(secondaryText);
		string arg = (string.IsNullOrWhiteSpace(text2) ? "off" : "on");
		if (text2.Length > 72)
		{
			text2 = text2.Substring(0, 72);
		}
		text2 = text2.Replace("\r", " ").Replace("\n", " ").Trim();
		string text3 = $"npcRecall={arg} secondaryLen={((!string.IsNullOrWhiteSpace(text2)) ? text2.Length : 0)}";
		if (!string.IsNullOrWhiteSpace(text2))
		{
			text3 = text3 + " secondaryPreview=" + JsonConvert.ToString(text2);
		}
		return string.IsNullOrWhiteSpace(text) ? text3 : (text + " " + text3);
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

	private static List<string> SplitGuardrailIntents(string input, int maxParts = 4)
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
			string text2 = text;
			foreach (char c in text2)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、' || c == '\n' || c == '\r')
				{
					string text3 = stringBuilder.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list2.Add(text3);
					}
					stringBuilder.Clear();
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			string text4 = stringBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list2.Add(text4);
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
				string text5 = (list2[j] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text5))
				{
					continue;
				}
				bool flag = false;
				string[] array2 = array;
				foreach (string text6 in array2)
				{
					int num = text5.IndexOf(text6, StringComparison.Ordinal);
					if (num > 1 && num < text5.Length - text6.Length - 1)
					{
						string text7 = text5.Substring(0, num).Trim();
						string text8 = text5.Substring(num + text6.Length).Trim();
						if (text7.Length >= 2)
						{
							list3.Add(text7);
						}
						if (text8.Length >= 2)
						{
							list3.Add(text8);
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list3.Add(text5);
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
				string text9 = NormalizeSemanticText(list3[l]);
				if (!string.IsNullOrWhiteSpace(text9) && text9.Length >= 2 && hashSet.Add(text9))
				{
					list.Add(text9);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		if (list.Count <= 0)
		{
			string text10 = NormalizeSemanticText(input);
			if (!string.IsNullOrWhiteSpace(text10))
			{
				list = IntentQueryOptimizer.OptimizeSplitIntents(new List<string> { text10 }, 1);
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
					if (guardrailIntentInput?.Vector != null && guardrailIntentInput.Vector.Length != 0)
					{
						float num2 = DotProductNormalized(guardrailIntentInput.Vector, vec) * Math.Max(0f, guardrailIntentInput.Weight);
						if (num2 > num)
						{
							num = num2;
							bestSeed = text;
						}
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
		int num;
		switch (text)
		{
		default:
			num = ((text == "surroundings") ? 1 : 0);
			break;
		case "duel":
		case "reward":
		case "loan":
			num = 1;
			break;
		}
		return (byte)num != 0;
	}

	private static bool IsRuleCurrentlyEligibleForRag(string ruleId)
	{
		string text = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!string.Equals(text, "vanilla_issue", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		try
		{
			return VanillaIssueOfferBridge.IsRagEligibleForExternal(ResolveConversationTargetHero());
		}
		catch
		{
			return false;
		}
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
				string value = (item.Value ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
				{
					if (maxKeyLen > 0 && text.Length > maxKeyLen)
					{
						text = text.Substring(0, maxKeyLen);
					}
					dictionary[text.ToLowerInvariant()] = value;
				}
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
					GuardrailRulePromptConfig guardrailRulePromptConfig = NormalizeCustomRulePrompt(_guardrail.RulePrompts[i], i + 1);
					upsert(guardrailRulePromptConfig);
				}
			}
		}
		catch
		{
		}
		return map;
		void upsert(GuardrailRulePromptConfig guardrailRulePromptConfig2)
		{
			if (guardrailRulePromptConfig2 != null)
			{
				string text = (guardrailRulePromptConfig2.Id ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					guardrailRulePromptConfig2.Id = text;
					map[text] = guardrailRulePromptConfig2;
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
				where r != null && r.IsEnabled && !string.IsNullOrWhiteSpace(r.Id) && IsRuleCurrentlyEligibleForRag(r.Id)
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
			if ((Volatile.Read(ref _guardrailWarmupState) != 2 || Volatile.Read(ref _guardrailWarmupVersion) != num) && Interlocked.CompareExchange(ref _guardrailWarmupState, 1, 0) == 0)
			{
				Interlocked.Exchange(ref _guardrailWarmupVersion, num);
				string warmupSource = (string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim());
				Logger.Log("GuardrailWarmup", $"start source={warmupSource} version={num}");
				Task.Run(delegate
				{
					RunGuardrailSemanticWarmup(warmupSource, num);
				});
			}
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
					string text2 = NormalizeSemanticText(list[j]);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						hashSet.Add(text2);
					}
				}
			}
			num = hashSet.Count;
			foreach (string item in hashSet)
			{
				if (TryGetPhraseEmbedding(item, out var vec) && vec != null && vec.Length != 0)
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
			appendInputs(SplitGuardrailIntents(userText, 2), 2, 1f);
			string text2 = NormalizeSemanticText(secondaryText);
			if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, NormalizeSemanticText(userText), StringComparison.Ordinal))
			{
				appendInputs(SplitGuardrailIntents(text2, 2), 2, 1f);
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
			float[] vec = null;
			if (!string.IsNullOrWhiteSpace(runtimeGuardrailContext))
			{
				TryGetInputEmbedding(runtimeGuardrailContext, out vec);
			}
			bool flag = vec != null && vec.Length != 0;
			List<GuardrailRulePromptConfig> allEnabledRulePrompts = GetAllEnabledRulePrompts();
			if (allEnabledRulePrompts == null || allEnabledRulePrompts.Count <= 0)
			{
				return false;
			}
			GuardrailEvalSnapshot guardrailEvalSnapshot = new GuardrailEvalSnapshot
			{
				Key = text
			};
			List<GuardrailRuleEval> list3 = new List<GuardrailRuleEval>();
			Dictionary<string, GuardrailRulePromptConfig> dictionary = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < allEnabledRulePrompts.Count; i++)
			{
				GuardrailRulePromptConfig guardrailRulePromptConfig = allEnabledRulePrompts[i];
				if (guardrailRulePromptConfig == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig.Id))
				{
					continue;
				}
				string id = guardrailRulePromptConfig.Id;
				string ruleInstruction = guardrailRulePromptConfig.Instruction ?? "";
				List<string> list4 = BuildRuleSemanticSeeds(id, ruleInstruction, guardrailRulePromptConfig.TriggerKeywords);
				float num = 0f;
				float num2 = 0f;
				string matchedSeed = "";
				string matchedIntent = "";
				for (int j = 0; j < list4.Count; j++)
				{
					string text3 = list4[j];
					if (string.IsNullOrWhiteSpace(text3) || !TryGetPhraseEmbedding(text3, out var vec2) || vec2 == null || vec2.Length == 0)
					{
						continue;
					}
					for (int k = 0; k < list.Count; k++)
					{
						GuardrailIntentInput guardrailIntentInput = list[k];
						if (guardrailIntentInput?.Vector != null && guardrailIntentInput.Vector.Length != 0)
						{
							float num3 = DotProductNormalized(guardrailIntentInput.Vector, vec2) * Math.Max(0f, guardrailIntentInput.Weight);
							if (num3 > num)
							{
								num = num3;
								matchedSeed = text3;
								matchedIntent = guardrailIntentInput.Text;
							}
						}
					}
					if (flag)
					{
						float num4 = DotProductNormalized(vec, vec2);
						if (num4 > num2)
						{
							num2 = num4;
						}
					}
				}
				float num5 = 0f;
				float num6 = (flag ? (num * (1f - num5) + num2 * num5) : num);
				GuardrailRuleEval guardrailRuleEval = new GuardrailRuleEval
				{
					RuleTag = id,
					MatchedSeed = matchedSeed,
					MatchedIntent = matchedIntent,
					RawInput = num,
					RawContext = num2,
					MixedRaw = num6,
					AmpScore = num6,
					RerankScore = num6
				};
				list3.Add(guardrailRuleEval);
				dictionary[id] = guardrailRulePromptConfig;
				guardrailEvalSnapshot.Rules[id] = guardrailRuleEval;
			}
			int guardrailRuleReturnCap = GuardrailRuleReturnCap;
			int num7 = Math.Max(1, list.Count);
			int guardrailRerankBudget = GetGuardrailRerankBudget(guardrailRuleReturnCap);
			int guardrailPerIntentRerank = GetGuardrailPerIntentRerank(guardrailRerankBudget, num7);
			int guardrailPerIntentRecall = GetGuardrailPerIntentRecall(guardrailPerIntentRerank);
			guardrailEvalSnapshot.IntentCount = num7;
			guardrailEvalSnapshot.ReturnCap = guardrailRuleReturnCap;
			guardrailEvalSnapshot.RerankPerIntent = guardrailPerIntentRerank;
			guardrailEvalSnapshot.RecallPerIntent = guardrailPerIntentRecall;
			OnnxCrossEncoderReranker onnxCrossEncoderReranker = null;
			bool flag2 = false;
			try
			{
				onnxCrossEncoderReranker = OnnxCrossEncoderReranker.Instance;
				flag2 = onnxCrossEncoderReranker?.IsAvailable ?? false;
			}
			catch
			{
				flag2 = false;
			}
			string text4 = (guardrailEvalSnapshot.MatchMode = ((!flag2) ? ((list.Count > 1) ? "semantic_multi" : "semantic") : ((list.Count > 1) ? "rerank_multi" : "rerank")));
			Dictionary<string, GuardrailRuleAggregate> dictionary2 = new Dictionary<string, GuardrailRuleAggregate>(StringComparer.OrdinalIgnoreCase);
			for (int l = 0; l < list.Count; l++)
			{
				GuardrailIntentInput guardrailIntentInput2 = list[l];
				if (guardrailIntentInput2?.Vector == null || guardrailIntentInput2.Vector.Length == 0)
				{
					continue;
				}
				List<GuardrailRuleScore> list5 = new List<GuardrailRuleScore>();
				for (int m = 0; m < allEnabledRulePrompts.Count; m++)
				{
					GuardrailRulePromptConfig guardrailRulePromptConfig2 = allEnabledRulePrompts[m];
					if (guardrailRulePromptConfig2 == null || string.IsNullOrWhiteSpace(guardrailRulePromptConfig2.Id))
					{
						continue;
					}
					string id2 = guardrailRulePromptConfig2.Id;
					guardrailEvalSnapshot.Rules.TryGetValue(id2, out var value);
					List<string> list6 = BuildRuleSemanticSeeds(id2, guardrailRulePromptConfig2.Instruction ?? "", guardrailRulePromptConfig2.TriggerKeywords);
					float num8 = 0f;
					string matchedSeed2 = "";
					for (int n = 0; n < list6.Count; n++)
					{
						string text5 = list6[n];
						if (!string.IsNullOrWhiteSpace(text5) && TryGetPhraseEmbedding(text5, out var vec3) && vec3 != null && vec3.Length != 0)
						{
							float num9 = DotProductNormalized(guardrailIntentInput2.Vector, vec3) * Math.Max(0f, guardrailIntentInput2.Weight);
							if (num9 > num8)
							{
								num8 = num9;
								matchedSeed2 = text5;
							}
						}
					}
					float num10 = ((flag && value != null) ? value.RawContext : 0f);
					float num11 = 0f;
					float num12 = (flag ? (num8 * (1f - num11) + num10 * num11) : num8);
					list5.Add(new GuardrailRuleScore
					{
						Rule = guardrailRulePromptConfig2,
						RawScore = num12,
						FinalScore = num12,
						MatchedSeed = matchedSeed2,
						MatchedIntent = guardrailIntentInput2.Text
					});
				}
				list5 = list5.OrderByDescending((GuardrailRuleScore x) => x.RawScore).ThenBy((GuardrailRuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).Take(guardrailPerIntentRecall)
					.ToList();
				if (list5.Count <= 0)
				{
					continue;
				}
				int num13 = Math.Min(guardrailPerIntentRerank, list5.Count);
				List<GuardrailRuleScore> list7 = new List<GuardrailRuleScore>();
				List<string> list8 = null;
				List<float> scores = null;
				bool flag3 = false;
				if (flag2)
				{
					list8 = new List<string>(num13);
					for (int num14 = 0; num14 < num13; num14++)
					{
						GuardrailRuleScore guardrailRuleScore = list5[num14];
						list8.Add((guardrailRuleScore?.Rule == null) ? "" : BuildGuardrailRuleRerankText(guardrailRuleScore.Rule));
					}
					flag3 = onnxCrossEncoderReranker.TryScoreBatch(guardrailIntentInput2.Text, list8, out scores) && scores != null && scores.Count == num13;
				}
				for (int num15 = 0; num15 < num13; num15++)
				{
					GuardrailRuleScore guardrailRuleScore2 = list5[num15];
					if (guardrailRuleScore2?.Rule != null)
					{
						float finalScore = guardrailRuleScore2.RawScore;
						if (flag2 && flag3 && list8 != null && num15 < list8.Count && !string.IsNullOrWhiteSpace(list8[num15]) && scores != null && num15 < scores.Count)
						{
							finalScore = scores[num15] * Math.Max(0f, guardrailIntentInput2.Weight);
						}
						list7.Add(new GuardrailRuleScore
						{
							Rule = guardrailRuleScore2.Rule,
							RawScore = guardrailRuleScore2.RawScore,
							FinalScore = finalScore,
							MatchedSeed = guardrailRuleScore2.MatchedSeed,
							MatchedIntent = guardrailRuleScore2.MatchedIntent
						});
					}
				}
				List<GuardrailRuleScore> list9 = SelectGuardrailCandidateScores(list7, (flag2 && flag3) ? "cross_encoder" : "recall_fallback", guardrailIntentInput2.Text, num13);
				for (int num16 = 0; num16 < list9.Count; num16++)
				{
					GuardrailRuleScore guardrailRuleScore3 = list9[num16];
					if (guardrailRuleScore3?.Rule == null)
					{
						continue;
					}
					string text6 = (guardrailRuleScore3.Rule.Id ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6) && guardrailEvalSnapshot.Rules.TryGetValue(text6, out var value2))
					{
						if (!dictionary2.TryGetValue(text6, out var value3))
						{
							value3 = new GuardrailRuleAggregate
							{
								Eval = value2
							};
						}
						value3.ScoreSum += guardrailRuleScore3.FinalScore;
						value3.HitCount++;
						if (num16 + 1 < value3.BestRank)
						{
							value3.BestRank = num16 + 1;
						}
						if (guardrailRuleScore3.FinalScore >= value3.BestScore)
						{
							value3.BestScore = guardrailRuleScore3.FinalScore;
							value3.MatchedSeed = guardrailRuleScore3.MatchedSeed;
							value3.MatchedIntent = guardrailRuleScore3.MatchedIntent;
						}
						dictionary2[text6] = value3;
					}
				}
			}
			for (int num17 = 0; num17 < list3.Count; num17++)
			{
				GuardrailRuleEval guardrailRuleEval2 = list3[num17];
				if (guardrailRuleEval2 != null)
				{
					guardrailRuleEval2.Candidate = false;
					guardrailRuleEval2.AmpScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.RerankScore = guardrailRuleEval2.MixedRaw;
					guardrailRuleEval2.MatchMode = text4;
				}
			}
			int num18 = 0;
			if (dictionary2.Count > 0)
			{
				int num19 = Math.Max(guardrailRuleReturnCap * 2, guardrailPerIntentRerank * Math.Min(list.Count, 3));
				if (num19 < guardrailRuleReturnCap)
				{
					num19 = guardrailRuleReturnCap;
				}
				if (num19 > 24)
				{
					num19 = 24;
				}
				List<GuardrailRuleAggregate> list10 = (from x in dictionary2.Values
					orderby Math.Min(1f, x.ScoreSum / (float)Math.Max(1, x.HitCount) + (float)(x.HitCount - 1) * 0.08f) descending, x.BestRank
					select x).ThenBy((GuardrailRuleAggregate x) => x?.Eval?.RuleTag ?? "", StringComparer.OrdinalIgnoreCase).Take(num19).ToList();
				num18 = list10.Count;
				for (int num20 = 0; num20 < list10.Count; num20++)
				{
					GuardrailRuleAggregate guardrailRuleAggregate = list10[num20];
					if (guardrailRuleAggregate?.Eval != null)
					{
						float num21 = guardrailRuleAggregate.ScoreSum / (float)Math.Max(1, guardrailRuleAggregate.HitCount) + (float)(guardrailRuleAggregate.HitCount - 1) * 0.08f;
						if (num21 > 1f)
						{
							num21 = 1f;
						}
						guardrailRuleAggregate.Eval.Candidate = true;
						guardrailRuleAggregate.Eval.AmpScore = num21;
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
			}
			list3 = (from x in list3
				orderby x.Candidate ? 1 : 0 descending, x.Candidate ? x.AmpScore : x.MixedRaw descending
				select x).ThenBy((GuardrailRuleEval x) => x.RuleTag, StringComparer.OrdinalIgnoreCase).ToList();
			float mean = ((list3.Count > 0) ? list3.Average((GuardrailRuleEval x) => x.Candidate ? x.AmpScore : x.MixedRaw) : 0f);
			float topGap = ((list3.Count > 1) ? ((list3[0].Candidate ? list3[0].AmpScore : list3[0].MixedRaw) - (list3[1].Candidate ? list3[1].AmpScore : list3[1].MixedRaw)) : 1f);
			for (int num22 = 0; num22 < list3.Count; num22++)
			{
				GuardrailRuleEval guardrailRuleEval3 = list3[num22];
				guardrailRuleEval3.Mean = mean;
				guardrailRuleEval3.Rank = num22 + 1;
				float num23 = -1f;
				string maxOtherTag = "";
				for (int num24 = 0; num24 < list3.Count; num24++)
				{
					if (num24 != num22)
					{
						GuardrailRuleEval guardrailRuleEval4 = list3[num24];
						float num25 = (guardrailRuleEval4.Candidate ? guardrailRuleEval4.AmpScore : guardrailRuleEval4.MixedRaw);
						if (num25 > num23)
						{
							num23 = num25;
							maxOtherTag = guardrailRuleEval4.RuleTag;
						}
					}
				}
				float num26 = (guardrailRuleEval3.Candidate ? guardrailRuleEval3.AmpScore : guardrailRuleEval3.MixedRaw);
				float delta = ((num23 < -0.5f) ? num26 : (num26 - num23));
				float intentEvidence = 0f;
				float intentGate = 0f;
				string intentSeed = "";
				bool lexicalAnchor = false;
				bool flag4 = guardrailRuleEval3.Candidate && guardrailRuleEval3.Rank <= guardrailRuleReturnCap;
				string rejectReason = (flag4 ? (text4 + "_return(" + guardrailRuleEval3.Rank + "/" + guardrailRuleReturnCap + ")") : (guardrailRuleEval3.Candidate ? (text4 + "_return_overflow") : (text4 + "_recall_miss")));
				guardrailRuleEval3.MaxOther = num23;
				guardrailRuleEval3.MaxOtherTag = maxOtherTag;
				guardrailRuleEval3.Delta = delta;
				guardrailRuleEval3.TopGap = topGap;
				guardrailRuleEval3.IntentEvidence = intentEvidence;
				guardrailRuleEval3.IntentGate = intentGate;
				guardrailRuleEval3.IntentSeed = intentSeed;
				guardrailRuleEval3.LexicalAnchor = lexicalAnchor;
				guardrailRuleEval3.AbsHit = flag4;
				guardrailRuleEval3.RelHit = false;
				guardrailRuleEval3.HighAmpHit = false;
				guardrailRuleEval3.ForceHit = false;
				guardrailRuleEval3.RejectReason = rejectReason;
				guardrailRuleEval3.MatchMode = text4;
				guardrailRuleEval3.Hit = flag4;
			}
			try
			{
				Logger.Log("GuardrailSemantic", $"candidate_pool mode={text4} returnCap={guardrailRuleReturnCap} rerankBudget={guardrailRerankBudget} rerankPerIntent={guardrailPerIntentRerank} recallPerIntent={guardrailPerIntentRecall} intents={num7} got={num18}");
			}
			catch
			{
			}
			try
			{
				int count = list3.Count;
				int num27 = 0;
				for (int num28 = 0; num28 < list3.Count; num28++)
				{
					GuardrailRuleEval guardrailRuleEval5 = list3[num28];
					if (guardrailRuleEval5 != null)
					{
						if (guardrailRuleEval5.Hit)
						{
							num27++;
						}
						Logger.RecordHitRate("guardrail", guardrailRuleEval5.RuleTag ?? "__unknown__", guardrailRuleEval5.Hit, BuildSemanticHitRateDetail($"raw={guardrailRuleEval5.RawInput:0.000} rerank={guardrailRuleEval5.RerankScore:0.000} amp={guardrailRuleEval5.AmpScore:0.000} rank={guardrailRuleEval5.Rank} reason={guardrailRuleEval5.RejectReason}", secondaryText), userText);
					}
				}
				Logger.RecordHitRate("guardrail", "__query__", num27 > 0, BuildSemanticHitRateDetail($"hits={num27}/{count} inputLen={userText.Length}", secondaryText), userText);
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
			if (intents != null && intents.Count > 0 && !(weight <= 0f) && perSourceLimit > 0)
			{
				int num29 = 0;
				for (int num30 = 0; num30 < intents.Count; num30++)
				{
					if (list.Count >= 4)
					{
						break;
					}
					string text7 = NormalizeSemanticText(intents[num30]);
					if (!string.IsNullOrWhiteSpace(text7) && TryGetInputEmbedding(text7, out var vec4) && vec4 != null && vec4.Length != 0)
					{
						num29++;
						if (num29 > perSourceLimit)
						{
							break;
						}
						list2.Add(text7);
						list.Add(new GuardrailIntentInput
						{
							Text = text7,
							Vector = vec4,
							Weight = weight
						});
					}
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
		int num = ((intentCount <= 0) ? 1 : intentCount);
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
			for (int num8 = 0; num8 < list2.Count; num8++)
			{
				if (list.Count >= num)
				{
					break;
				}
				GuardrailRuleScore guardrailRuleScore = list2[num8];
				if (guardrailRuleScore?.Rule != null && !(guardrailRuleScore.FinalScore < num2))
				{
					string text = (guardrailRuleScore.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
					{
						list.Add(guardrailRuleScore);
						num7++;
					}
				}
			}
			if (list.Count < num)
			{
				for (int num9 = 0; num9 < list2.Count; num9++)
				{
					if (list.Count >= num)
					{
						break;
					}
					GuardrailRuleScore guardrailRuleScore2 = list2[num9];
					if (guardrailRuleScore2?.Rule != null)
					{
						string text2 = (guardrailRuleScore2.Rule.Id ?? "").Trim();
						if (string.IsNullOrWhiteSpace(text2) || hashSet.Add(text2))
						{
							list.Add(guardrailRuleScore2);
						}
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
		if (TryLexicalRuleKeywordHit(input, secondaryInput, triggerKeywords, out var matchedKeyword2))
		{
			matchedKeyword = matchedKeyword2;
			score = 1f;
			Logger.Log("GuardrailSemantic", "rule=" + ruleTag + " hit=True mode=lexical_fallback matched=" + matchedKeyword2);
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
				if (!string.IsNullOrWhiteSpace(text3) && ((!string.IsNullOrWhiteSpace(text) && text.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrWhiteSpace(text2) && text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0)))
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
				if (value != null && value.IsEnabled && !string.IsNullOrWhiteSpace(text) && (includeBuiltInRules || !IsBuiltInRuleTag(text)) && IsRuleCurrentlyEligibleForRag(text) && TryLexicalRuleKeywordHit(input, secondaryInput, value.TriggerKeywords, out var matchedKeyword))
				{
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
				Settlement currentSettlement = Settlement.CurrentSettlement;
				string text3 = (((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null) ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text3) ? ("troop:" + text2) : ("troop:" + text2 + "@" + text3);
			}
		}
		catch
		{
		}
		try
		{
			Hero val = ResolveConversationTargetHero();
			string text4 = (((val != null) ? ((MBObjectBase)val).StringId : null) ?? "").Trim();
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
			CharacterObject val2 = ResolveConversationTargetCharacter();
			string text5 = (((val2 != null) ? ((MBObjectBase)val2).StringId : null) ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text5))
			{
				Settlement currentSettlement2 = Settlement.CurrentSettlement;
				string text6 = (((currentSettlement2 != null) ? ((MBObjectBase)currentSettlement2).StringId : null) ?? "").Trim().ToLowerInvariant();
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
		string text2 = text;
		string text3 = text2;
		if (text3 == "kingdom_service" || text3 == "marriage")
		{
			return 3;
		}
		return 0;
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
		return (ruleId ?? "").Trim().ToLowerInvariant() switch
		{
			"duel" => text.IndexOf("[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase) >= 0, 
			"reward" => text.IndexOf("[ACTION:GIVE_GOLD:", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:GIVE_ITEM:", StringComparison.OrdinalIgnoreCase) >= 0, 
			"loan" => text.IndexOf("[ACTION:DEBT_", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:DEBT_PAY_", StringComparison.OrdinalIgnoreCase) >= 0, 
			"kingdom_service" => text.IndexOf("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase) >= 0, 
			"marriage" => text.IndexOf("[ACTION:MARRIAGE_", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase) >= 0, 
			"lords_hall_access" => text.IndexOf("[ACTION:OPEN_LORDS_HALL]", StringComparison.OrdinalIgnoreCase) >= 0, 
			_ => false, 
		};
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
		string value = NormalizeSemanticText(state.MatchedSeed);
		if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return IsStickyGuardrailFollowUpInput(input);
	}

	private static float ApplyStickyGuardrailScoreDecay(float score, int maxCarryTurns, int carryTurnIndex)
	{
		float num = ((score > 0f) ? score : 0.6f);
		float num2 = ((maxCarryTurns < 3) ? ((carryTurnIndex <= 1) ? 0.72f : 0.45f) : ((carryTurnIndex <= 1) ? 0.78f : ((carryTurnIndex == 2) ? 0.58f : 0.36f)));
		return Math.Max(0.18f, num * num2);
	}

	private static List<GuardrailRuleHit> MergeStickyGuardrailRuleHits(string input, string secondaryInput, List<GuardrailRuleHit> liveHits, int maxCount)
	{
		List<GuardrailRuleHit> list = (from x in liveHits ?? new List<GuardrailRuleHit>()
			where x != null && !string.IsNullOrWhiteSpace(x.RuleId)
			orderby x.Priority descending, x.Score descending
			select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
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
			for (int num2 = 0; num2 < value.Count; num2++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState = value[num2];
				if (stickyGuardrailRuleState == null || string.IsNullOrWhiteSpace(stickyGuardrailRuleState.RuleId) || hashSet.Contains(stickyGuardrailRuleState.RuleId))
				{
					continue;
				}
				string text2 = stickyGuardrailRuleState.RuleId.Trim();
				if (GetStickyGuardrailTurnLimit(text2) > 0 && dictionary.TryGetValue(text2, out var value2) && value2 != null && value2.IsEnabled && ShouldContinueStickyGuardrailRule(stickyGuardrailRuleState, value2, input, list.Count, secondaryInput))
				{
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
			}
			for (int num3 = 0; num3 < list.Count; num3++)
			{
				GuardrailRuleHit guardrailRuleHit = list[num3];
				if (ShouldStartStickyGuardrailRule(input, secondaryInput, guardrailRuleHit, num3))
				{
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
					}
					else
					{
						stickyGuardrailRuleState2.Group = guardrailRuleHit.Group ?? stickyGuardrailRuleState2.Group;
						stickyGuardrailRuleState2.Priority = guardrailRuleHit.Priority;
						stickyGuardrailRuleState2.LastScore = guardrailRuleHit.Score;
						stickyGuardrailRuleState2.MatchedSeed = guardrailRuleHit.MatchedSeed ?? stickyGuardrailRuleState2.MatchedSeed;
						stickyGuardrailRuleState2.RemainingCarryTurns = stickyGuardrailTurnLimit;
						stickyGuardrailRuleState2.MaxCarryTurns = stickyGuardrailTurnLimit;
					}
				}
			}
			list3 = (from x in list3
				orderby x.Priority descending, x.LastScore descending
				select x).ThenBy((StickyGuardrailRuleState x) => x.RuleId, StringComparer.OrdinalIgnoreCase).Take(3).ToList();
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
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				StickyGuardrailRuleState stickyGuardrailRuleState3 = list2[num4];
				if (stickyGuardrailRuleState3 != null && !hashSet.Contains(stickyGuardrailRuleState3.RuleId) && dictionary.TryGetValue(stickyGuardrailRuleState3.RuleId, out var value3) && value3 != null)
				{
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
		}
		list = (from x in list
			orderby x.Priority descending, x.Score descending
			select x).ThenBy((GuardrailRuleHit x) => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList();
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
			List<GuardrailRuleHit> list = GetGuardrailSemanticRuleHits(input, secondaryInput, maxRules);
			if (list == null || list.Count <= 0)
			{
				list = GetGuardrailLexicalRuleHits(input, secondaryInput, maxRules);
			}
			list = MergeStickyGuardrailRuleHits(input, secondaryInput, list, maxRules);
			if (list == null || list.Count <= 0)
			{
				return "";
			}
			Dictionary<string, GuardrailRulePromptConfig> dictionary = (hasAnyHero ? null : BuildRulePromptRegistry());
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				GuardrailRuleHit guardrailRuleHit = list[i];
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
					string text3 = BuildRuntimeKingdomServiceInstruction();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						value = text3;
					}
				}
				if (hasAnyHero && string.Equals(text, "marriage", StringComparison.OrdinalIgnoreCase))
				{
					string text4 = RomanceSystemBehavior.Instance?.BuildMarriageRuntimeInstruction(ResolveConversationTargetHero()) ?? "";
					if (!string.IsNullOrWhiteSpace(text4))
					{
						value = text4;
					}
				}
				if (hasAnyHero && string.Equals(text, "vanilla_issue", StringComparison.OrdinalIgnoreCase))
				{
					value = VanillaIssueOfferBridge.BuildRuntimePromptBlockForExternal(ResolveConversationTargetHero()) ?? "";
				}
				if (string.Equals(text, "meeting_taunt", StringComparison.OrdinalIgnoreCase))
				{
					string text5 = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(ResolveConversationTargetHero(), ResolveConversationTargetCharacter(), ResolveConversationTargetAgentIndex());
					if (!string.IsNullOrWhiteSpace(text5))
					{
						value = text5;
					}
				}
				if (hasAnyHero && string.Equals(text, "npc_major_actions", StringComparison.OrdinalIgnoreCase))
				{
					string text6 = MyBehavior.BuildNpcMajorActionsRuntimeInstructionForExternal(ResolveConversationTargetHero());
					if (!string.IsNullOrWhiteSpace(text6))
					{
						value = text6;
					}
				}
				if (hasAnyHero && string.Equals(text, "npc_recent_actions", StringComparison.OrdinalIgnoreCase))
				{
					string text7 = MyBehavior.BuildNpcRecentActionsRuntimeInstructionForExternal(ResolveConversationTargetHero());
					if (!string.IsNullOrWhiteSpace(text7))
					{
						value = text7;
					}
				}
				if (string.Equals(text, "lords_hall_access", StringComparison.OrdinalIgnoreCase))
				{
					string text8 = BuildRuntimeLordsHallAccessInstruction();
					if (!string.IsNullOrWhiteSpace(text8))
					{
						value = text8;
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
			return ApplyPlayerDisplayNameToGuardrailText(stringBuilder.ToString().Trim());
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
			_guardrailRuntimeTargetKingdomId.Value = (kingdomId ?? "").Trim().ToLowerInvariant() ?? "";
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
			_guardrailRuntimeTargetTroopId.Value = (troopId ?? "").Trim().ToLowerInvariant() ?? "";
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
			_guardrailRuntimeTargetUnnamedRank.Value = (unnamedRank ?? "").Trim().ToLowerInvariant() ?? "";
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

	private static string ApplyPlayerDisplayNameToGuardrailText(string text)
	{
		try
		{
			string text2 = text ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			string text3 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text3) || string.Equals(text3, "玩家", StringComparison.Ordinal))
			{
				return text2;
			}
			text2 = text2.Replace("[AFEF玩家行为补充]", "__AFEF_PLAYER_FACT__");
			text2 = text2.Replace("【玩家家族可婚配未婚成员（事实清单）】", "__PLAYER_CLAN_FACT__");
			text2 = text2.Replace("玩家家族", "__PLAYER_CLAN__");
			text2 = text2.Replace("玩家", text3);
			text2 = text2.Replace("__PLAYER_CLAN__", "玩家家族");
			text2 = text2.Replace("__AFEF_PLAYER_FACT__", "[AFEF玩家行为补充]");
			return text2.Replace("__PLAYER_CLAN_FACT__", "【玩家家族可婚配未婚成员（事实清单）】");
		}
		catch
		{
			return text ?? "";
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
		int num2 = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			num2 = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			num2 = 6;
		}
		if (1 == 0)
		{
		}
		string text = num2 switch
		{
			1 => "彻底不信", 
			2 => "极度怀疑", 
			3 => "强烈戒备", 
			4 => "不信任", 
			5 => "保留", 
			_ => "观望", 
		};
		if (1 == 0)
		{
		}
		string value = text;
		string value2 = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(value2))
		{
			value2 = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = value2,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = num2.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num),
			["trustAttitude"] = value
		};
	}

	public static string BuildRuntimeLoanInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero targetHero2 = targetHero ?? ResolveConversationTargetHero();
			CharacterObject targetCharacter2 = targetCharacter ?? ResolveConversationTargetCharacter();
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(targetHero2, targetCharacter2));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(LoanRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildLoanRuntimeTokens(targetHero2, targetCharacter2));
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
		int num2 = 6;
		try
		{
			num = ResolveRuntimeTrustValue(targetHero, targetCharacter);
			num2 = RewardSystemBehavior.GetTrustLevelIndex(num);
		}
		catch
		{
			num = 0;
			num2 = 6;
		}
		string value = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(value))
		{
			value = "玩家";
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["playerName"] = value,
			["trustCurrent"] = num.ToString(),
			["trustIndex"] = num2.ToString(),
			["trustLevel"] = RewardSystemBehavior.GetTrustLevelText(num)
		};
	}

	public static string BuildRuntimeRewardInstructionForExternal(Hero targetHero = null, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero targetHero2 = targetHero ?? ResolveConversationTargetHero();
			CharacterObject targetCharacter2 = targetCharacter ?? ResolveConversationTargetCharacter();
			int num = 6;
			try
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(ResolveRuntimeTrustValue(targetHero2, targetCharacter2));
			}
			catch
			{
				num = 6;
			}
			Dictionary<string, string> dictionary = NormalizeTemplateMap(RewardRuntimeInstructionTemplates);
			if (dictionary != null && dictionary.TryGetValue("level_" + num, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				string text = ApplyRuntimeTemplate(value, BuildRewardRuntimeTokens(targetHero2, targetCharacter2));
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
				Kingdom val = ((IEnumerable<Kingdom>)Kingdom.All)?.FirstOrDefault((Kingdom k) => k != null && string.Equals((((MBObjectBase)k).StringId ?? "").Trim().ToLowerInvariant(), text, StringComparison.OrdinalIgnoreCase));
				if (val != null)
				{
					return val;
				}
			}
		}
		catch
		{
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			object obj2;
			if (oneToOneConversationHero == null)
			{
				obj2 = null;
			}
			else
			{
				Clan clan = oneToOneConversationHero.Clan;
				obj2 = ((clan != null) ? clan.Kingdom : null);
			}
			Kingdom val2 = (Kingdom)obj2;
			if (val2 != null)
			{
				return val2;
			}
			IFaction obj3 = ((oneToOneConversationHero != null) ? oneToOneConversationHero.MapFaction : null);
			Kingdom val3 = (Kingdom)(object)((obj3 is Kingdom) ? obj3 : null);
			if (val3 != null)
			{
				return val3;
			}
		}
		catch
		{
		}
		try
		{
			Campaign current = Campaign.Current;
			object obj5;
			if (current == null)
			{
				obj5 = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj5 = ((conversationManager != null) ? conversationManager.OneToOneConversationCharacter : null);
			}
			CharacterObject val4 = (CharacterObject)obj5;
			Hero val5 = ((val4 != null) ? val4.HeroObject : null);
			object obj6;
			if (val5 == null)
			{
				obj6 = null;
			}
			else
			{
				Clan clan2 = val5.Clan;
				obj6 = ((clan2 != null) ? clan2.Kingdom : null);
			}
			Kingdom val6 = (Kingdom)obj6;
			if (val6 != null)
			{
				return val6;
			}
			IFaction obj7 = ((val5 != null) ? val5.MapFaction : null);
			Kingdom val7 = (Kingdom)(object)((obj7 is Kingdom) ? obj7 : null);
			if (val7 != null)
			{
				return val7;
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
				Hero val = Hero.Find(text);
				if (val != null)
				{
					return val;
				}
				Hero val2 = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)));
				if (val2 != null)
				{
					return val2;
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
			Campaign current = Campaign.Current;
			object obj3;
			if (current == null)
			{
				obj3 = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				if (conversationManager == null)
				{
					obj3 = null;
				}
				else
				{
					CharacterObject oneToOneConversationCharacter = conversationManager.OneToOneConversationCharacter;
					obj3 = ((oneToOneConversationCharacter != null) ? oneToOneConversationCharacter.HeroObject : null);
				}
			}
			Hero val3 = (Hero)obj3;
			if (val3 != null)
			{
				return val3;
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
				CharacterObject val = ((IEnumerable<CharacterObject>)CharacterObject.All)?.FirstOrDefault((CharacterObject x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (val != null)
				{
					return val;
				}
			}
		}
		catch
		{
		}
		try
		{
			Campaign current = Campaign.Current;
			object obj2;
			if (current == null)
			{
				obj2 = null;
			}
			else
			{
				ConversationManager conversationManager = current.ConversationManager;
				obj2 = ((conversationManager != null) ? conversationManager.OneToOneConversationCharacter : null);
			}
			CharacterObject val2 = (CharacterObject)obj2;
			if (val2 != null)
			{
				return val2;
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
			Clan playerClan;
			Kingdom playerKingdom;
			bool isMercenaryService;
			Kingdom targetKingdom;
			bool isSameKingdom;
			int playerTier;
			int mercTier;
			int vassalTier;
			int mercTrustMin;
			int vassalTrustMin;
			int currentTrust;
			Dictionary<string, string> tokens = BuildKingdomServiceRuntimeTokens(out playerClan, out playerKingdom, out isMercenaryService, out targetKingdom, out isSameKingdom, out playerTier, out mercTier, out vassalTier, out mercTrustMin, out vassalTrustMin, out currentTrust);
			if (playerClan == null)
			{
				return "";
			}
			string text = ResolveKingdomServiceRuntimeStateKey(playerKingdom, isMercenaryService, targetKingdom, isSameKingdom, playerTier, mercTier, vassalTier, mercTrustMin, vassalTrustMin, currentTrust);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			return ResolveKingdomServiceRuntimeText(text, forConstraint: false, tokens);
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
			return true;
		default:
			return false;
		}
	}

	private static Dictionary<string, string> BuildKingdomServiceRuntimeTokens(out Clan playerClan, out Kingdom playerKingdom, out bool isMercenaryService, out Kingdom targetKingdom, out bool isSameKingdom, out int playerTier, out int mercTier, out int vassalTier, out int mercTrustMin, out int vassalTrustMin, out int currentTrust)
	{
		playerClan = Clan.PlayerClan;
		Clan obj = playerClan;
		playerKingdom = ((obj != null) ? obj.Kingdom : null);
		Clan obj2 = playerClan;
		isMercenaryService = obj2 != null && obj2.IsUnderMercenaryService;
		targetKingdom = ResolveConversationTargetKingdom();
		isSameKingdom = playerKingdom != null && targetKingdom != null && playerKingdom == targetKingdom;
		Clan obj3 = playerClan;
		playerTier = ((obj3 != null) ? obj3.Tier : 0);
		mercTier = 1;
		vassalTier = 2;
		mercTrustMin = 5;
		vassalTrustMin = 20;
		currentTrust = 0;
		try
		{
			Campaign current = Campaign.Current;
			int? obj4;
			if (current == null)
			{
				obj4 = null;
			}
			else
			{
				GameModels models = current.Models;
				if (models == null)
				{
					obj4 = null;
				}
				else
				{
					ClanTierModel clanTierModel = models.ClanTierModel;
					obj4 = ((clanTierModel != null) ? new int?(clanTierModel.MercenaryEligibleTier) : ((int?)null));
				}
			}
			mercTier = obj4 ?? 1;
		}
		catch
		{
			mercTier = 1;
		}
		try
		{
			Campaign current2 = Campaign.Current;
			int? obj6;
			if (current2 == null)
			{
				obj6 = null;
			}
			else
			{
				GameModels models2 = current2.Models;
				if (models2 == null)
				{
					obj6 = null;
				}
				else
				{
					ClanTierModel clanTierModel2 = models2.ClanTierModel;
					obj6 = ((clanTierModel2 != null) ? new int?(clanTierModel2.VassalEligibleTier) : ((int?)null));
				}
			}
			vassalTier = obj6 ?? 2;
		}
		catch
		{
			vassalTier = 2;
		}
		Hero val = ResolveConversationTargetHero();
		if (val == null)
		{
			Kingdom obj8 = targetKingdom;
			val = ((obj8 != null) ? obj8.Leader : null);
		}
		if (val == null)
		{
			Kingdom obj9 = playerKingdom;
			val = ((obj9 != null) ? obj9.Leader : null);
		}
		try
		{
			currentTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(val) ?? 0;
		}
		catch
		{
			currentTrust = 0;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Kingdom obj11 = playerKingdom;
		dictionary["playerKingdom"] = ((obj11 == null) ? null : ((object)obj11.Name)?.ToString()) ?? "";
		Kingdom obj12 = playerKingdom;
		dictionary["playerKingdomId"] = ((obj12 != null) ? ((MBObjectBase)obj12).StringId : null) ?? "";
		Kingdom obj13 = targetKingdom;
		dictionary["targetKingdom"] = ((obj13 == null) ? null : ((object)obj13.Name)?.ToString()) ?? "";
		Kingdom obj14 = targetKingdom;
		dictionary["targetKingdomId"] = ((obj14 != null) ? ((MBObjectBase)obj14).StringId : null) ?? "";
		dictionary["playerTier"] = playerTier.ToString();
		dictionary["mercTier"] = mercTier.ToString();
		dictionary["vassalTier"] = vassalTier.ToString();
		dictionary["trustMerc"] = mercTrustMin.ToString();
		dictionary["trustVassal"] = vassalTrustMin.ToString();
		dictionary["trustCurrent"] = currentTrust.ToString();
		dictionary["trustMercGap"] = Math.Max(0, mercTrustMin - currentTrust).ToString();
		dictionary["trustVassalGap"] = Math.Max(0, vassalTrustMin - currentTrust).ToString();
		return dictionary;
	}

	private static bool IsRuntimeTargetCastleGuard(int agentIndex)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (agentIndex < 0 || Mission.Current == null)
			{
				return false;
			}
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			if (val != null)
			{
				BasicCharacterObject character = val.Character;
				CharacterObject val2 = (CharacterObject)(object)((character is CharacterObject) ? character : null);
				if (val2 != null && ((BasicCharacterObject)val2).IsSoldier)
				{
					CampaignAgentComponent component = val.GetComponent<CampaignAgentComponent>();
					AgentNavigator val3 = ((component != null) ? component.AgentNavigator : null);
					bool flag = false;
					if (val3 != null)
					{
						WeakGameEntity gameEntity;
						int num;
						if (val3.TargetUsableMachine != null && val.IsUsingGameObject)
						{
							gameEntity = ((ScriptComponentBehavior)val3.TargetUsableMachine).GameEntity;
							num = (((WeakGameEntity)(ref gameEntity)).HasTag("sp_guard_castle") ? 1 : 0);
						}
						else
						{
							num = 0;
						}
						flag = (byte)num != 0;
						if (!flag && (val3.SpecialTargetTag == "sp_guard_castle" || val3.SpecialTargetTag == "sp_guard"))
						{
							LocationComplex current = LocationComplex.Current;
							Location lordsHallLocation = ((current != null) ? current.GetLocationWithId("lordshall") : null);
							MissionAgentHandler missionBehavior = Mission.Current.GetMissionBehavior<MissionAgentHandler>();
							if (lordsHallLocation != null && ((missionBehavior != null) ? missionBehavior.TownPassageProps : null) != null)
							{
								UsableMachine val4 = missionBehavior.TownPassageProps.FirstOrDefault(delegate(UsableMachine x)
								{
									Passage val5 = (Passage)(object)((x is Passage) ? x : null);
									return val5 != null && val5.ToLocation == lordsHallLocation;
								});
								if (val4 != null)
								{
									gameEntity = ((ScriptComponentBehavior)val4).GameEntity;
									Vec3 globalPosition = ((WeakGameEntity)(ref gameEntity)).GlobalPosition;
									if (((Vec3)(ref globalPosition)).DistanceSquared(val.Position) < 100f)
									{
										flag = true;
									}
								}
							}
						}
					}
					return flag;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static string DescribeLordsHallAccessReason(AccessDetails accessDetails)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected I4, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Invalid comparison between Unknown and I4
		try
		{
			AccessLimitationReason accessLimitationReason = accessDetails.AccessLimitationReason;
			AccessLimitationReason val = accessLimitationReason;
			return (val - 3) switch
			{
				3 => "玩家家族等级不足", 
				0 => "玩家在当地有犯罪评级", 
				2 => "当前只能靠伪装混入", 
				4 => "领主大厅当前无人可见", 
				_ => ((int)accessDetails.AccessLevel == 2) ? "原生规则允许直接进入" : "原生规则不允许直接进入", 
			};
		}
		catch
		{
			return "";
		}
	}

	private static bool TryBuildLordsHallAccessRuntimeState(out string stateKey, out Dictionary<string, string> tokens)
	{
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Invalid comparison between Unknown and I4
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Invalid comparison between Unknown and I4
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Invalid comparison between Unknown and I4
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Invalid comparison between Unknown and I4
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Invalid comparison between Unknown and I4
		stateKey = "not_applicable";
		tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			int num = ResolveConversationTargetAgentIndex();
			string text = ResolveRuntimeTargetUnnamedRank();
			string text2 = ResolveRuntimeTargetTroopId();
			int num2 = 0;
			Hero mainHero = Hero.MainHero;
			int num3 = ((mainHero != null) ? mainHero.Gold : 0);
			bool flag = !string.IsNullOrWhiteSpace(text2) && string.Equals(text, "soldier", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace((_guardrailRuntimeTargetHeroId.Value ?? "").Trim()) && IsRuntimeTargetCastleGuard(num);
			string text3 = MyBehavior.BuildRuleTargetKeyForExternal(null, null, num);
			if (string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text2))
			{
				text3 = "troop:" + text2;
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				try
				{
					num2 = ShoutBehavior.CurrentInstance?.GetRecentNonHeroGoldForRuleTarget(text3) ?? 0;
				}
				catch
				{
					num2 = 0;
				}
			}
			tokens["settlementName"] = ((currentSettlement == null) ? null : ((object)currentSettlement.Name)?.ToString()) ?? "";
			tokens["settlementId"] = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null) ?? "";
			tokens["troopId"] = text2 ?? "";
			tokens["targetRank"] = text ?? "";
			tokens["targetKey"] = text3 ?? "";
			tokens["prepaidGold"] = num2.ToString();
			tokens["playerGold"] = num3.ToString();
			Dictionary<string, string> obj2 = tokens;
			Clan playerClan = Clan.PlayerClan;
			obj2["playerClanTier"] = ((playerClan != null) ? new int?(playerClan.Tier) : ((int?)null)).GetValueOrDefault().ToString();
			tokens["accessReason"] = "";
			tokens["guideBribeGold"] = "0";
			if (currentSettlement == null || !currentSettlement.IsTown || !flag)
			{
				stateKey = "not_applicable";
				return true;
			}
			Campaign current = Campaign.Current;
			object obj3;
			if (current == null)
			{
				obj3 = null;
			}
			else
			{
				GameModels models = current.Models;
				obj3 = ((models != null) ? models.SettlementAccessModel : null);
			}
			SettlementAccessModel val = (SettlementAccessModel)obj3;
			if (val == null)
			{
				stateKey = "denied_no_bribe";
				return true;
			}
			AccessDetails val2 = default(AccessDetails);
			val.CanMainHeroEnterLordsHall(currentSettlement, ref val2);
			bool flag2 = false;
			TextObject val3 = null;
			bool flag3 = val.CanMainHeroAccessLocation(currentSettlement, "lordshall", ref flag2, ref val3);
			Campaign current2 = Campaign.Current;
			int? obj4;
			if (current2 == null)
			{
				obj4 = null;
			}
			else
			{
				GameModels models2 = current2.Models;
				if (models2 == null)
				{
					obj4 = null;
				}
				else
				{
					BribeCalculationModel bribeCalculationModel = models2.BribeCalculationModel;
					obj4 = ((bribeCalculationModel != null) ? new int?(bribeCalculationModel.GetBribeToEnterLordsHall(currentSettlement)) : ((int?)null));
				}
			}
			int? num4 = obj4;
			int valueOrDefault = num4.GetValueOrDefault();
			tokens["accessReason"] = DescribeLordsHallAccessReason(val2);
			tokens["guideBribeGold"] = valueOrDefault.ToString();
			if (flag3)
			{
				stateKey = "allowed_directly";
				return true;
			}
			if ((int)val2.AccessLevel == 1 && (int)val2.LimitedAccessSolution == 1 && valueOrDefault > 0)
			{
				stateKey = "denied_but_bribe_available";
				return true;
			}
			if ((int)val2.AccessLevel == 1 && (int)val2.LimitedAccessSolution == 2)
			{
				stateKey = "disguise_only";
				return true;
			}
			if ((int)val2.AccessLevel == 0 && (int)val2.AccessLimitationReason == 7)
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
			switch (text)
			{
			case "marriage":
			{
				Hero speaker = ResolveConversationTargetHero();
				return RomanceSystemBehavior.Instance?.BuildMarriageRuntimeConstraintHint(speaker) ?? "";
			}
			case "npc_major_actions":
				return MyBehavior.BuildNpcActionsRuntimeConstraintHintForExternal(ResolveConversationTargetHero(), recentOnly: false);
			case "npc_recent_actions":
				return MyBehavior.BuildNpcActionsRuntimeConstraintHintForExternal(ResolveConversationTargetHero(), recentOnly: true);
			case "lords_hall_access":
			{
				if (TryBuildLordsHallAccessRuntimeState(out var stateKey, out var tokens2))
				{
					return ResolveRuleRuntimeText("lords_hall_access", stateKey, forConstraint: true, tokens2);
				}
				return "";
			}
			default:
			{
				if (text != "kingdom_service")
				{
					return "";
				}
				Clan playerClan = Clan.PlayerClan;
				if (playerClan == null)
				{
					return ResolveKingdomServiceRuntimeText("no_player_clan", forConstraint: true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
				}
				Kingdom playerKingdom;
				bool isMercenaryService;
				Kingdom targetKingdom;
				bool isSameKingdom;
				int playerTier;
				int mercTier;
				int vassalTier;
				int mercTrustMin;
				int vassalTrustMin;
				int currentTrust;
				Dictionary<string, string> tokens = BuildKingdomServiceRuntimeTokens(out playerClan, out playerKingdom, out isMercenaryService, out targetKingdom, out isSameKingdom, out playerTier, out mercTier, out vassalTier, out mercTrustMin, out vassalTrustMin, out currentTrust);
				string text2 = ResolveKingdomServiceRuntimeStateKey(playerKingdom, isMercenaryService, targetKingdom, isSameKingdom, playerTier, mercTier, vassalTier, mercTrustMin, vassalTrustMin, currentTrust);
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
				return ResolveKingdomServiceRuntimeText(text2, forConstraint: true, tokens);
			}
			}
		}
		catch
		{
			return "";
		}
	}

	private static string RuleTopicLabel(string tag)
	{
		string text = (tag ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"duel" => "决斗挑战", 
			"reward" => "交易/奖励", 
			"loan" => "借贷/赊账", 
			"surroundings" => "地理方位", 
			_ => "当前话题", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string RuleTopicAskHint(string tag)
	{
		string text = (tag ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"duel" => "向我发起决斗", 
			"reward" => "谈交易和货物", 
			"loan" => "谈借钱或还款", 
			"surroundings" => "问附近位置", 
			_ => "讨论这件事", 
		};
		if (1 == 0)
		{
		}
		return result;
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
			string path = Path.Combine(basePath, "Modules", "AnimusForge", "ModuleData", "AIConfig.json");
			if (!File.Exists(path))
			{
				Logger.Log("AIConfig", "[错误] 找不到 AIConfig.json");
				_config = new AIConfigModel();
			}
			else
			{
				string text = File.ReadAllText(path);
				_config = JsonConvert.DeserializeObject<AIConfigModel>(text) ?? new AIConfigModel();
			}
			string path2 = Path.Combine(basePath, "Modules", "AnimusForge", "ModuleData", "RuleBehaviorPrompts.json");
			if (!File.Exists(path2))
			{
				Logger.Log("AIConfig", "[错误] 找不到 RuleBehaviorPrompts.json");
				_guardrail = new GuardrailConfigModel();
			}
			else
			{
				string text2 = File.ReadAllText(path2);
				_guardrail = JsonConvert.DeserializeObject<GuardrailConfigModel>(text2) ?? new GuardrailConfigModel();
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
			string text3 = (KnowledgeRetrievalFromMcm ? "MCM" : "Guardrail");
			Logger.Log("AIConfig", string.Format("配置加载成功。触发词(决斗/奖励/借贷/地理)={0}/{1}/{2}/{3}，扩展规则={4}，启用规则总数={5}。规则返回上限={6}。知识检索({7})：{8}（语义优先={9}, returnCap={10}）。", valueOrDefault, valueOrDefault2, valueOrDefault3, valueOrDefault4, valueOrDefault5, num2, GetGuardrailReturnCapFromMcm(), text3, KnowledgeRetrievalEnabled ? "开启" : "关闭", KnowledgeSemanticFirst, KnowledgeSemanticTopK));
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
