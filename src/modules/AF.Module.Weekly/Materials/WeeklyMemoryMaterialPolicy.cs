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
					reasons.Add(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0345") + label);
				}
			}
			long value = estimateTagValue(tag);
			if (value > 0L)
			{
				evaluation.EstimatedValueDenars = AddWeeklyMemoryMaterialValue(evaluation.EstimatedValueDenars, value);
				reasons.Add(BuildWeeklyMemoryMaterialTagLabel(tag) + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0346") + value + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0347"));
			}
		}
		if (evaluation.EstimatedValueDenars > ValueThresholdDenars)
		{
			reasons.Add(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0348") + ValueThresholdDenars + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0349"));
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
		string date = string.IsNullOrWhiteSpace(block?.GameDate) ? (WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0350") + Math.Max(0, block?.GameDayIndex ?? 0) + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0351")) : block.GameDate.Trim();
		sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0352")).Append(date).Append(" ").Append(hourRange).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0353")).Append(npcName).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0354"));
		List<string> scenes = (block?.Scenes ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace((x ?? "").Trim())).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
		if (scenes.Count > 0)
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0355")).Append(string.Join("、", scenes)).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.RichTitle))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0356")).Append(block.RichTitle.Trim()).Append("。");
		}
		sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0357")).Append((material ?? "").Trim()).Append("。");
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicityReason))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0358")).Append(block.PlayerPublicityReason.Trim()).Append("。");
		}
		return sb.ToString().Trim();
	}

    internal static List<WeeklyDialogueMaterial> BuildDialogueMaterials(CompressedMemoryBlock block,
        List<WeeklyMemoryMaterialTrigger> triggers, string blockId)
    {
        var result = new List<WeeklyDialogueMaterial>();
        foreach (var group in triggers.GroupBy(x => (x.FootholdKingdomId ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
        {
            string kingdomId = (group.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(kingdomId)) continue;
            var items = group.ToList();
            string hash = ComputeWeeklyMemoryMaterialHash(string.Join("|", items.Select(x => (x.StableKey ?? "").Trim()).Where(x => !string.IsNullOrWhiteSpace(x))));
            result.Add(new WeeklyDialogueMaterial {
                KingdomId = kingdomId,
                StableKey = "player_dialogue_memory:" + kingdomId + ":" + blockId + ":" + hash,
                Label = WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0359") + (string.IsNullOrWhiteSpace(block.HeroName) ? "NPC" : block.HeroName.Trim()),
                Snapshot = BuildWeeklyMemoryMaterialSnapshotText(block, items),
                SettlementId = items.Select(x => (x.FootholdSettlementId ?? "").Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "",
                TriggerCount = items.Count,
                Value = items.Sum(x => Math.Max(0L, x.EstimatedValueDenars))
            });
        }
        return result;
    }

	internal static string BuildWeeklyMemoryMaterialSnapshotText(CompressedMemoryBlock block, List<WeeklyMemoryMaterialTrigger> triggers)
	{
		StringBuilder sb = new StringBuilder();
		string npcName = (block?.HeroName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = triggers?.Select((WeeklyMemoryMaterialTrigger x) => x?.NpcName).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "NPC";
		}
		sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0360")).Append(npcName).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0361"));
		if (!string.IsNullOrWhiteSpace(block?.RichTitle))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0362")).Append(block.RichTitle.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.Summary))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0363")).Append(block.Summary.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicity))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0364")).Append(block.PlayerPublicity.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(block?.PlayerPublicityReason))
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0365")).Append(block.PlayerPublicityReason.Trim()).Append("。");
		}
		List<string> scenes = (block?.Scenes ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace((x ?? "").Trim())).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
		if (scenes.Count > 0)
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0366")).Append(string.Join("、", scenes)).Append("。");
		}
		List<string> labels = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).SelectMany((WeeklyMemoryMaterialTrigger x) => x?.Tags ?? new List<string>()).Select(BuildWeeklyMemoryMaterialTagLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList();
		if (labels.Count > 0)
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0367")).Append(string.Join("、", labels)).Append("。");
		}
		long value = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).Sum((WeeklyMemoryMaterialTrigger x) => Math.Max(0L, x?.EstimatedValueDenars ?? 0L));
		if (value > 0L)
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0368")).Append(value).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0369"));
		}
		List<string> reasons = (triggers ?? new List<WeeklyMemoryMaterialTrigger>()).Select((WeeklyMemoryMaterialTrigger x) => (x?.TriggerReason ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();
		if (reasons.Count > 0)
		{
			sb.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0370")).Append(string.Join("；", reasons)).Append("。");
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
		reasons.Add(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0371") + value + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0372"));
		if (evaluation.EstimatedValueDenars > ValueThresholdDenars)
		{
			reasons.Add(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0373") + ValueThresholdDenars + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0374"));
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
		if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0375"), StringComparison.Ordinal))
		{
			return false;
		}
		if (value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0376"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0377"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0378"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0379"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0380"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0381"), StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0382"), StringComparison.Ordinal) >= 0 && (value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0383"), StringComparison.Ordinal) >= 0 || value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0384"), StringComparison.Ordinal) >= 0))
		{
			return true;
		}
		if (value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0385"), StringComparison.Ordinal) >= 0 && value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0386"), StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		return value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0387"), StringComparison.Ordinal) >= 0 && value.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0388"), StringComparison.Ordinal) >= 0;
	}

	internal static long ExtractPlayerTransferredValueFromWeeklyMemoryFact(string text)
	{
		long value = 0L;
		foreach (Match match in Regex.Matches(text ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0389"), RegexOptions.IgnoreCase))
		{
			if (TryParsePositiveLong(match.Groups[1].Value, out var parsed))
			{
				value = Math.Max(value, parsed);
			}
		}
		foreach (Match match in Regex.Matches(text ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0390"), RegexOptions.IgnoreCase))
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
		if (Regex.IsMatch(text, "^\\[ACTION:(?:MARRIAGE_FORMAL|MARRIAGE_CLAN|MARRIAGE_ELOPE|DIVORCE):[^\\]]+\\]$", RegexOptions.IgnoreCase))
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
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0391");
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_GIVE_ASSET]", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0392");
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_DEBT_CREATE]", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0393");
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_DEBT_RESOLVE]", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0394");
		}
		if (string.Equals(text, "[WEEKLY:ECONOMY_SETTLEMENT_TRANSFER]", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0395");
		}
		if (string.Equals(text, "[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0396");
		}
		if (text.StartsWith("[ACTION:GIVE_ASSET:GOLD:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0397");
		}
		if (text.StartsWith("[AD:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0398");
		}
		if (text.StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0399");
		}
		if (text.StartsWith("[ATT:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0400");
		}
		if (text.StartsWith("[ATP:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0401");
		}
		if (text.StartsWith("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0402");
		}
		if (text.StartsWith("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0403");
		}
		if (text.StartsWith("[ACTION:MARRIAGE_FORMAL:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[ACTION:MARRIAGE_CLAN:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0404");
		}
		if (text.StartsWith("[ACTION:MARRIAGE_ELOPE:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0405");
		}
		if (text.StartsWith("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0406");
		}
		if (text.StartsWith("[ACTION:AGENDA:", StringComparison.OrdinalIgnoreCase))
		{
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0407");
		}
		if (IsWeeklyMemoryMaterialSiegeTag(text))
		{
			return Regex.Replace(text, "^\\[ACTION:|\\]$", "", RegexOptions.IgnoreCase).Replace("SIEGE_", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0408"));
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

internal sealed class WeeklyDialogueMaterial
{
    internal string KingdomId;
    internal string StableKey;
    internal string Label;
    internal string Snapshot;
    internal string SettlementId;
    internal int TriggerCount;
    internal long Value;
}
