using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Saved diagnostic previews retain their original format; the editor has a separate display projection.
internal static class WeeklyDiagnosticPreviewProjection
{
	internal static void AppendField(StringBuilder stringBuilder, string label, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.AppendLine(label + "：" + value.Trim());
	}

	internal static string BuildSingle(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		port.AppendDevNpcActionField(stringBuilder, "生成对象", string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界周报" : "王国周报");
		port.AppendDevNpcActionField(stringBuilder, "关联王国", port.ResolveKingdomDisplay(group?.KingdomId));
		port.AppendDevNpcActionField(stringBuilder, "输出模式", ((group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, "篇幅档位", port.PromptProfileLabel());
		port.AppendDevNpcActionField(stringBuilder, "MaxTokens", port.GetEventAndRebellionApiMaxTokens().ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【System Prompt】");
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【User Prompt】");
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildBatch(WeeklyEditorDisplayPort port, WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<WeeklyEventMaterialPreviewGroup> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		port.AppendDevNpcActionField(stringBuilder, "批次周数", ((batch != null) ? batch.WeekIndex : 0).ToString());
		port.AppendDevNpcActionField(stringBuilder, "取材区间", ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));
		port.AppendDevNpcActionField(stringBuilder, "批次块数", list.Count.ToString());
		port.AppendDevNpcActionField(stringBuilder, "批次模式", ((batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, "批次对象", string.Join(" | ", list.Select(group => port.BuildWeeklyReportGroupReportId(group)).Where((string x) => !string.IsNullOrWhiteSpace(x))));
		port.AppendDevNpcActionField(stringBuilder, "输出模式", string.Join(" | ", list.Select((WeeklyEventMaterialPreviewGroup x) => ((x?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report")));
		port.AppendDevNpcActionField(stringBuilder, "MaxTokens", port.GetEventAndRebellionApiMaxTokens().ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【System Prompt】");
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【User Prompt】");
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}
}
