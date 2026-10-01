using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;

namespace AnimusForge;

internal sealed class WeeklyMemoryMaterialEvaluation
	{
		public bool HasMajorTag;

		public long EstimatedValueDenars;

		public string Reason = "";

		public List<string> Tags = new List<string>();

		public bool Eligible => HasMajorTag || EstimatedValueDenars > WeeklyMemoryMaterialPolicy.ValueThresholdDenars;
	}

internal static class WeeklyMemoryMaterialPolicy
{
 internal const long ValueThresholdDenars = 20000L;
    internal static WeeklyMemoryMaterialTrigger CreateTrigger(string memoryId, string npcName,
        int day, string gameDate, int sceneSessionId, int nativeDialogueSessionId, int targetAgentIndex,
        string footholdKingdomId, string footholdSettlementId, string tagText,
        WeeklyMemoryMaterialEvaluation evaluation, long utcTicks)
    {
		return new WeeklyMemoryMaterialTrigger
		{
			MemoryId = memoryId,
			NpcName = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim(),
			GameDayIndex = day,
			GameDate = gameDate,
			SceneSessionId = sceneSessionId,
			DialogueSessionId = nativeDialogueSessionId,
			TargetAgentIndex = targetAgentIndex,
			FootholdKingdomId = footholdKingdomId,
			FootholdSettlementId = footholdSettlementId,
			NormalizedTagText = tagText,
			Tags = evaluation.Tags,
			EstimatedValueDenars = evaluation.EstimatedValueDenars,
			TriggerReason = evaluation.Reason,
			StableKey = BuildWeeklyMemoryMaterialTriggerStableKey(memoryId, day, sceneSessionId, nativeDialogueSessionId, footholdKingdomId, tagText),
			CreatedUtcTicks = utcTicks
		};
    }

	internal static WeeklyMemoryMaterialEvaluation EvaluateWeeklyMemoryMaterialTags(List<string> tags, Func<string, long> estimateTagValue)
	{
		WeeklyMemoryMaterialEvaluation evaluation = new WeeklyMemoryMaterialEvaluation();
		evaluation.Tags = NormalizeWeeklyMemoryMaterialTags(tags);
		List<string> reasons = new List<string>();
		foreach (string tag in evaluation.Tags)
		{
			if (IsWeeklyMemoryMaterialMajorTag(tag))
			{
				evaluation.HasMajorTag = true;
				string label = BuildWeeklyMemoryMaterialTagLabel(tag);
				if (!string.IsNullOrWhiteSpace(label))
				{
					reasons.Add("重大标签：" + label);
				}
			}
			long value = estimateTagValue(tag);
			if (value > 0L)
			{
				evaluation.EstimatedValueDenars = AddWeeklyMemoryMaterialValue(evaluation.EstimatedValueDenars, value);
				reasons.Add(BuildWeeklyMemoryMaterialTagLabel(tag) + "估值 " + value + " 第纳尔");
			}
		}
		if (evaluation.EstimatedValueDenars > ValueThresholdDenars)
		{
			reasons.Add("本轮金额/估值合计严格大于 " + ValueThresholdDenars + " 第纳尔");
		}
		evaluation.Reason = string.Join("；", reasons.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
		return evaluation;
	}
	internal static string BuildPublicDailyMemoryWeeklyMaterialSnapshotText(CompressedMemoryBlock block, string material, string hourRange)
	{
		StringBuilder sb = new StringBuilder();
		string npcName = (block?.HeroName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = "NPC";
		}
		string date = string.IsNullOrWhiteSpace(block?.GameDate) ? ("第" + Math.Max(0, block?.GameDayIndex ?? 0) + "日") : block.GameDate.Trim();
		sb.Append("公开聊天日结素材：").Append(date).Append(" ").Append(hourRange).Append("，玩家与 ").Append(npcName).Append(" 的交流被日结标记为公开。");
		List<string> scenes = (block?.Scenes ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace((x ?? "").Trim())).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
		if (scenes.Count > 0)
		{
			sb.Append(" 场景：").Append(string.Join("、", scenes)).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.RichTitle))
		{
			sb.Append(" 日结标题：").Append(block.RichTitle.Trim()).Append("。");
		}
		sb.Append(" 公开素材：").Append((material ?? "").Trim()).Append("。");
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicityReason))
		{
			sb.Append(" 公开原因：").Append(block.PlayerPublicityReason.Trim()).Append("。");
		}
		return sb.ToString().Trim();
	}

	internal static string BuildWeeklyMemoryMaterialSnapshotText(CompressedMemoryBlock block, List<WeeklyMemoryMaterialTrigger> triggers)
	{
		StringBuilder sb = new StringBuilder();
		string npcName = (block?.HeroName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = triggers?.Select((WeeklyMemoryMaterialTrigger x) => x?.NpcName).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "NPC";
		}
		sb.Append("玩家与 ").Append(npcName).Append(" 的交流在后处理阶段被识别为本周王国周报素材。");
		if (!string.IsNullOrWhiteSpace(block?.RichTitle))
		{
			sb.Append(" 记忆标题：").Append(block.RichTitle.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.Summary))
		{
			sb.Append(" 记忆摘要：").Append(block.Summary.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicity))
		{
			sb.Append(" 日结公开判定：").Append(block.PlayerPublicity.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicityReason))
		{
			sb.Append(" 公开判定原因：").Append(block.PlayerPublicityReason.Trim()).Append("。");
		}
		List<string> scenes = (block?.Scenes ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace((x ?? "").Trim())).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
		if (scenes.Count > 0)
		{
			sb.Append(" 场景：").Append(string.Join("、", scenes)).Append("。");
		}
		List<string> labels = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).SelectMany((WeeklyMemoryMaterialTrigger x) => x?.Tags ?? new List<string>()).Select(BuildWeeklyMemoryMaterialTagLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
		if (labels.Count > 0)
		{
			sb.Append(" 触发机制：").Append(string.Join("、", labels)).Append("。");
		}
		long value = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).Sum((WeeklyMemoryMaterialTrigger x) => Math.Max(0L, x?.EstimatedValueDenars ?? 0L));
		if (value > 0L)
		{
			sb.Append(" 本轮标签估值合计约 ").Append(value).Append(" 第纳尔。");
		}
		List<string> reasons = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).Select((WeeklyMemoryMaterialTrigger x) => (x?.TriggerReason ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
		if (reasons.Count > 0)
		{
			sb.Append(" 触发依据：").Append(string.Join("；", reasons)).Append("。");
		}
		return sb.ToString().Trim();
	}

	internal static void TryApplyPlayerTransferredValueToWeeklyMemoryMaterialEvaluation(WeeklyMemoryMaterialEvaluation evaluation, List<string> tags, DailyMemoryDraft draft, string npcName, int sceneSessionId, int nativeDialogueSessionId)
	{
		if (evaluation == null || evaluation.Eligible || !ShouldAugmentWeeklyMemoryMaterialWithPlayerTransferredValue(tags))
		{
			return;
		}
		if (!TryEstimatePlayerTransferredValueForWeeklyMemoryMaterial(draft, npcName, sceneSessionId, nativeDialogueSessionId, out var value))
		{
			return;
		}
		evaluation.EstimatedValueDenars = AddWeeklyMemoryMaterialValue(evaluation.EstimatedValueDenars, value);
		List<string> reasons = (evaluation.Reason ?? "").Split(new char[1] { '；' }, StringSplitOptions.RemoveEmptyEntries).Select((string x) => x.Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		reasons.Add("玩家转移价值估值 " + value + " 第纳尔");
		if (evaluation.EstimatedValueDenars > ValueThresholdDenars)
		{
			reasons.Add("本轮金额/估值合计严格大于 " + ValueThresholdDenars + " 第纳尔");
		}
		evaluation.Reason = string.Join("；", reasons.Distinct(StringComparer.OrdinalIgnoreCase));
	}

	internal static bool ShouldAugmentWeeklyMemoryMaterialWithPlayerTransferredValue(IEnumerable<string> tags)
	{
		foreach (string tag in tags ?? Enumerable.Empty<string>())
		{
			string text = (tag ?? "").Trim();
			if (GiveAssetTagCodec.TryParseWhole(text, out _) ||
				Regex.IsMatch(text, "^\\[AD:\\d+:\\d+:P:[^\\]]*\\]$", RegexOptions.IgnoreCase) ||
				Regex.IsMatch(text, "^\\[ADP:[^\\]\\r\\n:;]+\\]$", RegexOptions.IgnoreCase) ||
				Regex.IsMatch(text, "^\\[ATT:(?:ALL|\\d+):(?:ALL|\\d+)\\]$", RegexOptions.IgnoreCase) ||
				Regex.IsMatch(text, "^\\[ATP:(?:ALL|\\d+):(?:ALL|\\d+)\\]$", RegexOptions.IgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool TryEstimatePlayerTransferredValueForWeeklyMemoryMaterial(DailyMemoryDraft draft, string npcName, int sceneSessionId, int nativeDialogueSessionId, out long value)
	{
		value = 0L;
		List<DailyMemoryLine> lines = (draft?.Lines ?? new List<DailyMemoryLine>()).Where((DailyMemoryLine x) => x != null && x.IsAfef && !string.IsNullOrWhiteSpace(x.Text)).ToList();
		if (lines.Count == 0)
		{
			return false;
		}
		List<DailyMemoryLine> matched = lines.Where((DailyMemoryLine x) => (sceneSessionId >= 0 && x.SceneSessionId == sceneSessionId) || (nativeDialogueSessionId >= 0 && x.DialogueSessionId == nativeDialogueSessionId)).ToList();
		if (matched.Count > 0)
		{
			lines = matched;
		}
		else
		{
			lines = lines.Skip(Math.Max(0, lines.Count - 12)).ToList();
		}
		foreach (DailyMemoryLine line in lines)
		{
			long lineValue = EstimatePlayerTransferredValueFromWeeklyMemoryText(line?.Text, npcName);
			if (lineValue > 0L)
			{
				value = AddWeeklyMemoryMaterialValue(value, lineValue);
			}
		}
		return value > 0L;
	}

	internal static long EstimatePlayerTransferredValueFromWeeklyMemoryText(string text, string npcName)
	{
		long total = 0L;
		foreach (string rawPart in Regex.Split(text ?? "", "\\r?\\n"))
		{
			string part = (rawPart ?? "").Trim();
			if (!IsPlayerTransferredValueWeeklyMemoryFact(part, npcName))
			{
				continue;
			}
			long partValue = ExtractPlayerTransferredValueFromWeeklyMemoryFact(part);
			if (partValue > 0L)
			{
				total = AddWeeklyMemoryMaterialValue(total, partValue);
			}
		}
		return total;
	}

	internal static bool IsPlayerTransferredValueWeeklyMemoryFact(string text, string npcName)
	{
		string value = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			return false;
		}
		if (value.IndexOf("展示", StringComparison.Ordinal) >= 0 || value.IndexOf("看了看", StringComparison.Ordinal) >= 0 || value.IndexOf("暂未交付", StringComparison.Ordinal) >= 0 || value.IndexOf("没有转移所有权", StringComparison.Ordinal) >= 0 || value.IndexOf("未能确认送达", StringComparison.Ordinal) >= 0 || value.IndexOf("准备通过信使", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (value.IndexOf("已经将", StringComparison.Ordinal) >= 0 && (value.IndexOf("交给", StringComparison.Ordinal) >= 0 || value.IndexOf("转交给", StringComparison.Ordinal) >= 0))
		{
			return true;
		}
		if (value.IndexOf("通过信使", StringComparison.Ordinal) >= 0 && value.IndexOf("转移了", StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		return value.IndexOf("转移了", StringComparison.Ordinal) >= 0 && value.IndexOf("估值约", StringComparison.Ordinal) >= 0;
	}

	internal static long ExtractPlayerTransferredValueFromWeeklyMemoryFact(string text)
	{
		long value = 0L;
		foreach (Match match in Regex.Matches(text ?? "", "(?:合计总值约|总值约|估值约|价值约)\\s*(\\d+)\\s*第纳尔", RegexOptions.IgnoreCase))
		{
			if (TryParsePositiveLong(match.Groups[1].Value, out var parsed))
			{
				value = Math.Max(value, parsed);
			}
		}
		foreach (Match match in Regex.Matches(text ?? "", "(?:已经将|转移了)\\s*(\\d+)\\s*第纳尔", RegexOptions.IgnoreCase))
		{
			if (TryParsePositiveLong(match.Groups[1].Value, out var parsed))
			{
				value = Math.Max(value, parsed);
			}
		}
		return value;
	}

	internal static bool IsWeeklyMemoryMaterialMajorTag(string tag)
	{
		string text = (tag ?? "").Trim();
		if (string.Equals(text, "[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (string.Equals(text, "[A:C_J_P_K]", StringComparison.OrdinalIgnoreCase)
			|| Regex.IsMatch(text, "^\\[A:C_J_K:[a-zA-Z0-9_.\\-]+\\]$", RegexOptions.IgnoreCase)
			|| Regex.IsMatch(text, "^\\[A:P_J_K_[MV]\\]$", RegexOptions.IgnoreCase)
			|| string.Equals(text, "[A:P_L_K]", StringComparison.OrdinalIgnoreCase)
			|| Regex.IsMatch(text, "^\\[ACTION:KINGDOM_SERVICE:(?:LEAVE(?::current)?|MERCENARY:[^\\]]+|VASSAL:[^\\]]+|CLAN_JOIN_(?:PLAYER_KINGDOM|KINGDOM):[^\\]]+)\\]$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (Regex.IsMatch(text, "^\\[ACTION:(?:MARRIAGE_FORMAL|MARRIAGE_ELOPE|DIVORCE):[^\\]]+\\]$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (Regex.IsMatch(text, "^\\[ACTION:AGENDA:[^\\]]+\\]$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		return IsWeeklyMemoryMaterialSiegeTag(text);
	}

	internal static bool IsWeeklyMemoryMaterialSiegeTag(string tag)
	{
		IReadOnlyList<SiegeInterventionActionKind> actions = SiegeActionTagCatalog.ExtractKinds(tag);
		return actions.Count == 1
			&& actions[0] != SiegeInterventionActionKind.StopMassacre
			&& actions[0] != SiegeInterventionActionKind.ConstructiveCultureChange;
	}

	internal static string BuildWeeklyMemoryMaterialTagLabel(string tag)
	{
		string text = (tag ?? "").Trim();
		if (string.Equals(text, "[WEEKLY:ECONOMY_GIVE_GOLD]", StringComparison.OrdinalIgnoreCase))
		{
			return "已确认的金币给付";
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_GIVE_ASSET]", StringComparison.OrdinalIgnoreCase))
		{
			return "已确认的资产给付";
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_DEBT_CREATE]", StringComparison.OrdinalIgnoreCase))
		{
			return "已确认的债务建立";
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_DEBT_RESOLVE]", StringComparison.OrdinalIgnoreCase))
		{
			return "已确认的债务清偿";
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_SETTLEMENT_TRANSFER]", StringComparison.OrdinalIgnoreCase))
		{
			return "已确认的定居点资产转移";
		}
		if (string.Equals(text, "[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase))
		{
			return "决斗";
		}
		if (text.StartsWith("[ACTION:GIVE_ASSET:GOLD:", StringComparison.OrdinalIgnoreCase))
		{
			return "给付金币";
		}
		if (text.StartsWith("[AD:", StringComparison.OrdinalIgnoreCase))
		{
			return "新增债务";
		}
		if (text.StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase))
		{
			return "债务清偿";
		}
		if (text.StartsWith("[ATT:", StringComparison.OrdinalIgnoreCase))
		{
			return "部队转移";
		}
		if (text.StartsWith("[ATP:", StringComparison.OrdinalIgnoreCase))
		{
			return "俘虏转移";
		}
		if (text.StartsWith("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase))
		{
			return "给付资产";
		}
		if (text.StartsWith("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase))
		{
			return "王国服役变更";
		}
		if (text.StartsWith("[ACTION:MARRIAGE_FORMAL:", StringComparison.OrdinalIgnoreCase))
		{
			return "正式婚姻";
		}
		if (text.StartsWith("[ACTION:MARRIAGE_ELOPE:", StringComparison.OrdinalIgnoreCase))
		{
			return "私奔婚姻";
		}
		if (text.StartsWith("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase))
		{
			return "离婚";
		}
		if (text.StartsWith("[ACTION:AGENDA:", StringComparison.OrdinalIgnoreCase))
		{
			return "王国议程承诺";
		}
		if (IsWeeklyMemoryMaterialSiegeTag(text))
		{
			return Regex.Replace(text, "^\\[ACTION:|\\]$", "", RegexOptions.IgnoreCase).Replace("SIEGE_", "攻城处置:");
		}
		return "";
	}

	internal static string NormalizeWeeklyMemoryMaterialTagText(string text)
	{
		return MemoryRecordRules.NormalizeWeeklyMemoryMaterialTagText(text);
	}

	internal static List<string> ExtractWeeklyMemoryMaterialTags(string text)
	{
		return MemoryRecordRules.ExtractWeeklyMemoryMaterialTags(text);
	}

	internal static List<string> NormalizeWeeklyMemoryMaterialTags(IEnumerable<string> tags)
	{
		return MemoryRecordRules.NormalizeWeeklyMemoryMaterialTags(tags);
	}

	internal static bool TryParsePositiveLong(string value, out long result)
	{
		if (long.TryParse((value ?? "").Trim(), out result) && result > 0L)
		{
			return true;
		}
		result = 0L;
		return false;
	}

	internal static long AddWeeklyMemoryMaterialValue(long current, long addition)
	{
		if (addition <= 0L)
		{
			return current;
		}
		if (current > long.MaxValue - addition)
		{
			return long.MaxValue;
		}
		return current + addition;
	}

	internal static string BuildWeeklyMemoryMaterialTriggerStableKey(string memoryId, int day, int sceneSessionId, int dialogueSessionId, string kingdomId, string tagText)
	{
		return MemoryRecordRules.BuildWeeklyMemoryMaterialTriggerStableKey(memoryId, day, sceneSessionId, dialogueSessionId, kingdomId, tagText);
	}

	internal static string ComputeWeeklyMemoryMaterialHash(string sourceText)
	{
		return MemoryRecordRules.ComputeWeeklyMemoryMaterialHash(sourceText);
	}
}
