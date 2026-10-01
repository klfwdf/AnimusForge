using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge;

internal sealed class MemoryEditorDisplayPort
{
 internal Func<DailyMemoryLine, string> BuildDailyMemoryLineForPrompt;
 internal Func<IEnumerable<WeeklyMemoryMaterialTrigger>, List<WeeklyMemoryMaterialTrigger>> SanitizeWeeklyMemoryMaterialTriggers;
 internal Func<string, string> BuildWeeklyMemoryMaterialTagLabel;
 internal Func<string, int, string> BuildCompressedMemoryBlockId;
 internal Func<int, int, string> FormatMemoryHourRange;
 internal Func<string, int, string> BuildDevHistoryPreview;
 internal Func<int> GetCurrentGameDayIndexSafe, GetCurrentHourOfDaySafeForPrompt;
 internal Func<string> ResolveCurrentMemorySceneLabel;
}

internal static class MemoryEditorProjection
{
	internal static bool IsDevDailyMemoryDraftMatch(MemoryEditorDisplayPort port, DailyMemoryDraft draft, string[] terms)
	{
		if (draft == null)
		{
			return false;
		}
		if (terms == null || terms.Length <= 0)
		{
			return true;
		}
		string haystack = BuildDevDailyMemoryDraftSearchText(port, draft).ToLowerInvariant();
		foreach (string term in terms)
		{
			string text = (term ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text) && !haystack.Contains(text))
			{
				return false;
			}
		}
		return true;
	}

	internal static string BuildDevDailyMemoryDraftSearchText(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		StringBuilder sb = new StringBuilder();
		if (draft == null)
		{
			return "";
		}
		sb.AppendLine(draft.HeroName ?? "");
		sb.AppendLine(draft.GameDate ?? "");
		sb.AppendLine(draft.GameDayIndex.ToString());
		foreach (DailyMemoryLine line in draft.Lines ?? new List<DailyMemoryLine>())
		{
			sb.AppendLine(port.BuildDailyMemoryLineForPrompt(line));
		}
		sb.AppendLine(BuildDevWeeklyMemoryMaterialTriggerText(port, draft.WeeklyMaterialTriggers));
		return sb.ToString();
	}

	internal static string BuildDevDailyMemoryDraftListLabel(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		if (draft == null)
		{
			return "（空）";
		}
		string date = string.IsNullOrWhiteSpace(draft.GameDate) ? ("第" + draft.GameDayIndex + "日") : draft.GameDate.Trim();
		string flags = (draft.QueuedForSummary ? " 已入队" : "") + (string.IsNullOrWhiteSpace(draft.LastSummaryError) ? "" : " 有错误") + (((draft.WeeklyMaterialTriggers?.Count).GetValueOrDefault() > 0) ? (" 周报素材x" + draft.WeeklyMaterialTriggers.Count) : "");
		return date + " | " + ((draft.Lines?.Count).GetValueOrDefault()) + " 行" + flags + " | " + port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 64);
	}

	internal static string BuildDevDailyMemoryDraftSubtitle(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		if (draft == null)
		{
			return "";
		}
		string date = string.IsNullOrWhiteSpace(draft.GameDate) ? ("第" + draft.GameDayIndex + "日") : draft.GameDate.Trim();
		return "日期：" + date + "\n行数：" + ((draft.Lines?.Count).GetValueOrDefault()) + "\n周报素材触发器：" + ((draft.WeeklyMaterialTriggers?.Count).GetValueOrDefault()) + "\n已入总结队列：" + (draft.QueuedForSummary ? "是" : "否") + "\n最近总结错误：" + BuildDevStoredErrorReference(port, draft.LastSummaryError);
	}

	internal static string BuildDevStoredErrorReference(MemoryEditorDisplayPort port, string error)
	{
		// 开发菜单会在返回和翻页时反复重建；这里只放固定短提示，避免历史 API 原文再次挤占操作项。
		return string.IsNullOrWhiteSpace(error) ? "无" : "已记录（详情已在触发时显示于左下角）";
	}

	internal static string BuildDevDailyMemoryDraftEditorDescription(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		if (draft == null)
		{
			return "找不到未压缩记忆。";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(BuildDevDailyMemoryDraftSubtitle(port, draft));
		sb.AppendLine("普通行：" + (draft.Lines ?? new List<DailyMemoryLine>()).Count((DailyMemoryLine x) => x != null && !x.IsAfef));
		sb.AppendLine("AFEF行：" + (draft.Lines ?? new List<DailyMemoryLine>()).Count((DailyMemoryLine x) => x != null && x.IsAfef));
		string triggerText = BuildDevWeeklyMemoryMaterialTriggerText(port, draft.WeeklyMaterialTriggers);
		if (!string.IsNullOrWhiteSpace(triggerText))
		{
			sb.AppendLine("周报素材：");
			sb.AppendLine(triggerText);
		}
		sb.AppendLine("预览：" + port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 180));
		return sb.ToString().TrimEnd();
	}

	internal static string BuildDevWeeklyMemoryMaterialTriggerText(MemoryEditorDisplayPort port, IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
	{
		List<WeeklyMemoryMaterialTrigger> list = port.SanitizeWeeklyMemoryMaterialTriggers(triggers);
		if (list.Count == 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		foreach (WeeklyMemoryMaterialTrigger trigger in list.Take(8))
		{
			List<string> labels = (trigger.Tags ?? new List<string>()).Select(port.BuildWeeklyMemoryMaterialTagLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			sb.Append("- 王国=").Append(trigger.FootholdKingdomId ?? "")
				.Append(" session=").Append(trigger.SceneSessionId >= 0 ? ("scene:" + trigger.SceneSessionId) : (trigger.DialogueSessionId >= 0 ? ("dialogue:" + trigger.DialogueSessionId) : "none"))
				.Append(" value=").Append(Math.Max(0L, trigger.EstimatedValueDenars))
				.Append(" tags=").Append(labels.Count > 0 ? string.Join("、", labels) : string.Join("、", trigger.Tags ?? new List<string>()));
			if (!string.IsNullOrWhiteSpace(trigger.TriggerReason))
			{
				sb.Append(" reason=").Append(trigger.TriggerReason.Trim());
			}
			sb.AppendLine();
		}
		if (list.Count > 8)
		{
			sb.AppendLine("- ...还有 " + (list.Count - 8) + " 条");
		}
		return sb.ToString().TrimEnd();
	}

	internal static string BuildDevDailyMemoryLineListLabel(MemoryEditorDisplayPort port, DailyMemoryLine line)
	{
		if (line == null)
		{
			return "（空）";
		}
		string type = line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? "LLM" : "普通");
		string speaker = string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "手动") : line.Speaker.Trim();
		return MBMath.ClampInt(line.GameHour, 0, 23) + "时 | " + type + " | " + speaker + " | " + port.BuildDevHistoryPreview(line.Text, 90);
	}

	internal static string BuildDevDailyMemoryLineSubtitle(MemoryEditorDisplayPort port, DailyMemoryLine line)
	{
		if (line == null)
		{
			return "";
		}
		string date = string.IsNullOrWhiteSpace(line.GameDate) ? ("第" + line.GameDayIndex + "日") : line.GameDate.Trim();
		return "日期：" + date + " " + MBMath.ClampInt(line.GameHour, 0, 23) + "时\n场景：" + (string.IsNullOrWhiteSpace(line.Scene) ? "未知场景" : line.Scene.Trim()) + "\n说话人：" + (string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "手动") : line.Speaker.Trim()) + "\n类型：" + (line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? "LLM对话" : "普通"));
	}

	internal static string BuildDevDailyMemoryLineDescription(MemoryEditorDisplayPort port, DailyMemoryLine line)
	{
		if (line == null)
		{
			return "找不到未压缩记忆行。";
		}
		return BuildDevDailyMemoryLineSubtitle(port, line) + "\n\n正文：\n" + (string.IsNullOrWhiteSpace(line.Text) ? "（空）" : line.Text.Trim());
	}

	internal static int GetDefaultDevDailyMemoryLineHour(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		DailyMemoryLine latest = (draft?.Lines ?? new List<DailyMemoryLine>()).LastOrDefault((DailyMemoryLine x) => x != null);
		if (latest != null)
		{
			return MBMath.ClampInt(latest.GameHour, 0, 23);
		}
		if (draft != null && draft.GameDayIndex == port.GetCurrentGameDayIndexSafe())
		{
			return port.GetCurrentHourOfDaySafeForPrompt();
		}
		return 12;
	}

	internal static string GetDefaultDevDailyMemoryLineScene(MemoryEditorDisplayPort port, DailyMemoryDraft draft)
	{
		string text = (draft?.Lines ?? new List<DailyMemoryLine>()).LastOrDefault((DailyMemoryLine x) => x != null && !string.IsNullOrWhiteSpace(x.Scene))?.Scene;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return port.ResolveCurrentMemorySceneLabel();
	}

	internal static CompressedMemoryBlock FindDevCompressedMemoryBlock(MemoryEditorDisplayPort port, List<CompressedMemoryBlock> blocks, string blockId)
	{
		string text = (blockId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		foreach (CompressedMemoryBlock block in blocks ?? new List<CompressedMemoryBlock>())
		{
			if (string.Equals(GetDevCompressedMemoryBlockId(port, block), text, StringComparison.OrdinalIgnoreCase))
			{
				return block;
			}
		}
		return null;
	}

	internal static string GetDevCompressedMemoryBlockId(MemoryEditorDisplayPort port, CompressedMemoryBlock block)
	{
		if (block == null)
		{
			return "";
		}
		string text = (block.Id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = port.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex);
		}
		return (text ?? "").Trim();
	}

	internal static string BuildDevCompressedMemoryBlockListLabel(MemoryEditorDisplayPort port, CompressedMemoryBlock block, int displayIndex)
	{
		if (block == null)
		{
			return displayIndex + "# （空）";
		}
		string date = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
		string title = string.IsNullOrWhiteSpace(block.RichTitle) ? "（无标题）" : block.RichTitle.Trim();
		return displayIndex + "# " + date + " " + port.FormatMemoryHourRange(block.StartHour, block.EndHour) + " | " + port.BuildDevHistoryPreview(title, 44);
	}

	internal static string BuildDevCompressedMemoryBlockSubtitle(MemoryEditorDisplayPort port, CompressedMemoryBlock block)
	{
		if (block == null)
		{
			return "";
		}
		string date = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
		string title = string.IsNullOrWhiteSpace(block.RichTitle) ? "（无标题）" : block.RichTitle.Trim();
		return "ID：" + GetDevCompressedMemoryBlockId(port, block) + "\n日期：" + date + " " + port.FormatMemoryHourRange(block.StartHour, block.EndHour) + "\n标题：" + title + "\n周报素材触发器：" + ((block.WeeklyMaterialTriggers?.Count).GetValueOrDefault());
	}

	internal static string BuildDevCompressedMemoryBlockEditorBody(MemoryEditorDisplayPort port, CompressedMemoryBlock block)
	{
		if (block == null)
		{
			return "找不到记忆块。";
		}
		StringBuilder sb = new StringBuilder();
		if (block.Scenes != null && block.Scenes.Count > 0)
		{
			sb.AppendLine("场景：" + string.Join(" / ", block.Scenes));
		}
		sb.AppendLine("正文预览：" + port.BuildDevHistoryPreview(block.Summary, 220));
		sb.AppendLine("AFEF：" + (block.AfefLines?.Count ?? 0) + " 行");
		string triggerText = BuildDevWeeklyMemoryMaterialTriggerText(port, block.WeeklyMaterialTriggers);
		if (!string.IsNullOrWhiteSpace(triggerText))
		{
			sb.AppendLine("周报素材：");
			sb.AppendLine(triggerText);
		}
		if (block.AfefLines != null && block.AfefLines.Count > 0)
		{
			sb.AppendLine("AFEF预览：" + port.BuildDevHistoryPreview(string.Join(" / ", block.AfefLines.Take(4)), 180));
		}
		sb.AppendLine();
		sb.AppendLine("编辑或删除后，该NPC的记忆大总结会被清空并重新排队整理。");
		return sb.ToString().TrimEnd();
	}

	internal static string[] SplitDevCompressedMemorySearchTerms(MemoryEditorDisplayPort port, string query)
	{
		try
		{
			return (query ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
		}
		catch
		{
			return new string[0];
		}
	}

	internal static bool IsDevCompressedMemoryBlockMatch(MemoryEditorDisplayPort port, CompressedMemoryBlock block, string[] terms)
	{
		if (block == null)
		{
			return false;
		}
		if (terms == null || terms.Length <= 0)
		{
			return true;
		}
		string haystack = (GetDevCompressedMemoryBlockId(port, block) + "\n" + (block.HeroName ?? "") + "\n" + (block.GameDate ?? "") + "\n" + (block.RichTitle ?? "") + "\n" + (block.Summary ?? "") + "\n" + string.Join("\n", block.Scenes ?? new List<string>()) + "\n" + string.Join("\n", block.AfefLines ?? new List<string>()) + "\n" + BuildDevWeeklyMemoryMaterialTriggerText(port, block.WeeklyMaterialTriggers)).ToLowerInvariant();
		foreach (string term in terms)
		{
			string text = (term ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text) && !haystack.Contains(text))
			{
				return false;
			}
		}
		return true;
	}

	internal static string NormalizeDevCompressedMemoryMultilineInput(MemoryEditorDisplayPort port, string input)
	{
		return MemoryDeveloperEditOwner.NormalizeMultiline(input);
	}

	internal static List<string> ParseDevCompressedMemoryLineList(MemoryEditorDisplayPort port, string input, int maxCount, bool ignoreCase)
	{
		return MemoryDeveloperEditOwner.ParseLineList(input, maxCount, ignoreCase);
	}
}
