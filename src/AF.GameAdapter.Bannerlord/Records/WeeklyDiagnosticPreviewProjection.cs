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
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0001"), string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? AdapterRecordTexts.Raw("AF_REC_Diagnostic_0002") : AdapterRecordTexts.Raw("AF_REC_Diagnostic_0003"));
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0004"), port.ResolveKingdomDisplay(group?.KingdomId));
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0005"), ((group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0006"), port.PromptProfileLabel());
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
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0007"), ((batch != null) ? batch.WeekIndex : 0).ToString());
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0008"), ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0009"), list.Count.ToString());
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0010"), ((batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0011"), string.Join(" | ", list.Select(group => port.BuildWeeklyReportGroupReportId(group)).Where((string x) => !string.IsNullOrWhiteSpace(x))));
		port.AppendDevNpcActionField(stringBuilder, AdapterRecordTexts.Raw("AF_REC_Diagnostic_0005"), string.Join(" | ", list.Select((WeeklyEventMaterialPreviewGroup x) => ((x?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report")));
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
