using static AnimusForge.MyBehavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;
using DevWeeklyReportBatchPreviewEntry = AnimusForge.MyBehavior.DevWeeklyReportBatchPreviewEntry;
using static AnimusForge.EventEditorUiText;

namespace AnimusForge;

internal class WeeklyEditorDisplayPort
{
 internal Func<int,string> FormatNewsDate;
 internal Func<string> PromptProfileLabel;
 internal delegate string ResolveHeroDisplayCapability(string heroId);
 internal ResolveHeroDisplayCapability ResolveHeroDisplay;
 internal delegate string ResolveSettlementDisplayCapability(string settlementId);
 internal ResolveSettlementDisplayCapability ResolveSettlementDisplay;
 internal delegate string BuildDevSummaryPreviewCapability(string text, int maxLen);
 internal BuildDevSummaryPreviewCapability BuildDevSummaryPreview;
 internal delegate string TranslateEventMaterialTypeForDevCapability(string materialType);
 internal TranslateEventMaterialTypeForDevCapability TranslateEventMaterialTypeForDev;
 internal delegate void AppendDevNpcActionFieldCapability(StringBuilder stringBuilder, string label, string value);
 internal AppendDevNpcActionFieldCapability AppendDevNpcActionField;
 internal delegate string ResolveKingdomDisplayCapability(string kingdomId);
 internal ResolveKingdomDisplayCapability ResolveKingdomDisplay;
 internal delegate int GetEventAndRebellionApiMaxTokensCapability();
 internal GetEventAndRebellionApiMaxTokensCapability GetEventAndRebellionApiMaxTokens;
 internal delegate string BuildWeeklyReportGroupReportIdCapability(WeeklyEventMaterialPreviewGroup group);
 internal BuildWeeklyReportGroupReportIdCapability BuildWeeklyReportGroupReportId;
}

internal static class WeeklyEditorProjection
{
	internal static string MaterialTypeForDisplay(string materialType)
	{
		string kind = (materialType ?? "").Trim().ToLowerInvariant();
		if (kind.StartsWith("prompt_agg_", StringComparison.OrdinalIgnoreCase))
			return T("AF_UI_WeeklyProjection_MaterialAggregate", "Weekly-report aggregate material");
		switch (kind)
		{
			case "world_opening_summary": return T("AF_UI_WeeklyProjection_MaterialWorldOpening", "World opening summary");
			case "kingdom_opening_summary": return T("AF_UI_WeeklyProjection_MaterialKingdomOpening", "Kingdom opening summary");
			case "kingdom_current_ruler": return T("AF_UI_WeeklyProjection_MaterialRuler", "Current ruler");
			case "npc_recent_action": return T("AF_UI_WeeklyProjection_MaterialRecentAction", "Recent NPC action");
			case "npc_major_action": return T("AF_UI_WeeklyProjection_MaterialMajorAction", "Major NPC action");
			case "raw_text": return T("AF_UI_WeeklyProjection_MaterialRawText", "Raw text material");
			default: return string.IsNullOrWhiteSpace(materialType) ? T("AF_UI_WeeklyProjection_MaterialUnclassified", "Unclassified material") : materialType.Trim();
		}
	}

	internal static string ProfileForDisplay(int preset)
	{
		switch (Math.Max(1, Math.Min(4, preset)))
		{
			case 1: return T("AF_UI_WeeklyProjection_Profile1", "Tier 1 (200-400 characters)");
			case 3: return T("AF_UI_WeeklyProjection_Profile3", "Tier 3 (200-1200 characters)");
			case 4: return T("AF_UI_WeeklyProjection_Profile4", "Tier 4 (200-1500 characters)");
			default: return T("AF_UI_WeeklyProjection_Profile2", "Tier 2 (200-800 characters)");
		}
	}

	internal static string BuildWeeklyEventMaterialPreviewGroupLabel(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return T("AF_UI_WeeklyProjection_InvalidGroup", "Invalid group");
		}
		string text = string.IsNullOrWhiteSpace(group.Summary) ? "" : port.BuildDevSummaryPreview(group.Summary, 44);
		int count = (group.Materials != null) ? group.Materials.Count : 0;
		int count2 = (group.PromptMaterials != null) ? group.PromptMaterials.Count : 0;
		string text2 = T("AF_UI_WeeklyProjection_MaterialCounts", "{SOURCE} / {PROMPT} materials", ("SOURCE", count.ToString()), ("PROMPT", count2.ToString()));
		string text3 = string.Equals((group.OutputMode).ToString(), WeeklyReportOutputMode.TitleShortTagsOnly.ToString(), StringComparison.OrdinalIgnoreCase) ? T("AF_WEEKLY_OutputModeShort", "Short") : T("AF_WEEKLY_OutputModeFull", "Full");
		return (group.Title ?? T("AF_WEEKLY_UnnamedGroup", "Unnamed group")) + " [" + text3 + " | " + text2 + "]" + (string.IsNullOrWhiteSpace(text) ? "" : (" " + text));
	}

	internal static WeeklyReportBrowserCountryData BuildWeeklyReportBrowserCountryData(WeeklyEditorDisplayPort port, string eventKind, string scopeKingdomId, string displayName, bool isWorld, List<EventRecordEntry> source, IReadOnlyDictionary<string, List<string>> bulletinAssociations = null)
	{
		string text = (scopeKingdomId ?? "").Trim();
		string text3 = (isWorld ? "world" : text);
		string text2 = (displayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = (isWorld ? T("AF_WEEKLY_WorldWeeklyReport", "World weekly report") : text);
		}
		return new WeeklyReportBrowserCountryData
		{
			CountryId = text3,
			DisplayName = text2,
			IsWorld = isWorld,
			Reports = BuildWeeklyReportBrowserEntries(port, source, eventKind, text, bulletinAssociations)
		};
	}

	internal static List<WeeklyReportBrowserEntryData> BuildWeeklyReportBrowserEntries(WeeklyEditorDisplayPort port, List<EventRecordEntry> source, string eventKind, string scopeKingdomId, IReadOnlyDictionary<string, List<string>> bulletinAssociations = null)
	{
		string text = (eventKind ?? "").Trim();
		string text2 = (scopeKingdomId ?? "").Trim();
		return (source ?? new List<EventRecordEntry>()).Where(x => WeeklyReportArchivePolicy.Matches(x, text, text2, bulletinAssociations))
            .OrderByDescending(x => x.CreatedDay).ThenByDescending(x => x.WeekIndex)
            .ThenByDescending(x => WeeklyReportArchivePolicy.IssueNumber(x.EventId))
            .ThenByDescending(x => x.Title ?? "", StringComparer.OrdinalIgnoreCase).Select(delegate(EventRecordEntry x)
		{
			string text3 = (x.Title ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = BuildWeeklyReportBrowserDefaultTitle(port, text, text2, x.WeekIndex);
			}
			bool recent = WeeklyReportArchivePolicy.IsRecent(x.EventId);
			string date = recent ? BuildRecentDateForDisplay(port, x) : port.FormatNewsDate != null && x.CreatedDay >= 0 ? port.FormatNewsDate(x.CreatedDay) : (!string.IsNullOrWhiteSpace(x.CreatedDate) ? x.CreatedDate.Trim() : T("AF_UI_Timeline_UnknownDate", "Unknown date"));
			if (recent)
				text3 = (x.ScopeKingdomId == WeeklyReportArchivePolicy.OtherKingdomId ? T("AF_UI_WeeklyProjection_OtherRecent", "Other recent news") : T("AF_UI_WeeklyProjection_KingdomRecent", "{KINGDOM} recent news", ("KINGDOM", port.ResolveKingdomDisplay(x.ScopeKingdomId)))) + " · " + date;
			string body = WeeklyReportArchivePolicy.BodyWithRegionalNews(x).Trim();
			return new WeeklyReportBrowserEntryData
			{
				EventId = (x.EventId ?? "").Trim(),
				ArchiveKind = WeeklyReportArchivePolicy.EntryKind(x.EventId),
				OpenTargetId = (x.EventId ?? "").Trim(),
				WeekIndex = Math.Max(0, x.WeekIndex),
				Title = text3,
				BodyText = body.Length == 0 ? T("AF_UI_WeeklyProjection_EmptyBody", "This weekly-report issue has no body text yet.") : body,
				CreatedDate = date,
				CreatedDay = Math.Max(0, x.CreatedDay),
				TagText = (x.TagText ?? "").Trim(),
				HasFullReport = !string.IsNullOrWhiteSpace(x.Summary)
			};
		}).ToList();
	}

	internal static string BuildRecentDateForDisplay(WeeklyEditorDisplayPort port, EventRecordEntry entry)
	{
		// A recent archive may span several days. Missing provenance keeps its stored range intact.
		var materials = entry?.Materials?.Where(material => material != null).ToList();
		if (port.FormatNewsDate != null && materials != null && materials.Count > 0
			&& materials.All(material => material.ActionDay.HasValue && material.ActionDay.Value >= 0
				&& (material.Label ?? "").IndexOf(" — ", StringComparison.Ordinal) < 0))
		{
			int firstDay = materials.Min(material => material.ActionDay.Value);
			int lastDay = materials.Max(material => material.ActionDay.Value);
			string first = port.FormatNewsDate(firstDay);
			string last = firstDay == lastDay ? first : port.FormatNewsDate(lastDay);
			return firstDay == lastDay ? first : first + " — " + last;
		}
		return !string.IsNullOrWhiteSpace(entry?.CreatedDate) ? entry.CreatedDate.Trim() : T("AF_UI_Timeline_UnknownDate", "Unknown date");
	}

	internal static string BuildWeeklyReportBrowserDefaultTitle(WeeklyEditorDisplayPort port, string eventKind, string scopeKingdomId, int weekIndex)
	{
		if (string.Equals((eventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return T("AF_WEEKLY_WorldWeeklyReport", "World weekly report") + " · " + (port.FormatNewsDate?.Invoke(Math.Max(0, weekIndex * 7 - 1)) ?? T("AF_UI_Timeline_UnknownDate", "Unknown date"));
		}
		string text = port.ResolveKingdomDisplay(scopeKingdomId);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = T("AF_UI_WeeklyProjection_Kingdom", "Kingdom");
		}
		return T("AF_UI_WeeklyProjection_KingdomWeekly", "{KINGDOM} weekly report", ("KINGDOM", text)) + " · " + (port.FormatNewsDate?.Invoke(Math.Max(0, weekIndex * 7 - 1)) ?? T("AF_UI_Timeline_UnknownDate", "Unknown date"));
	}

	internal static string BuildWeeklyReportPromptPreviewText(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
		=> WeeklyDiagnosticPreviewProjection.BuildSingle(port, group, systemPrompt, userPrompt);

	internal static string BuildWeeklyBatchPromptPreviewText(WeeklyEditorDisplayPort port, WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
		=> WeeklyDiagnosticPreviewProjection.BuildBatch(port, batch, systemPrompt, userPrompt);

	internal static string BuildWeeklyReportPromptPreviewTextForDisplay(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_GenerationTarget", "Generation target"), string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? T("AF_WEEKLY_WorldWeeklyReport", "World weekly report") : T("AF_WEEKLY_KingdomWeeklyReport", "Kingdom weekly report"));
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_AssociatedKingdom", "Associated kingdom"), port.ResolveKingdomDisplay(group?.KingdomId));
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_OutputMode", "Output mode"), ((group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_LengthProfile", "Length profile"), port.PromptProfileLabel());
		port.AppendDevNpcActionField(stringBuilder, "MaxTokens", port.GetEventAndRebellionApiMaxTokens().ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(T("AF_UI_WeeklyProjection_SystemPrompt", "[System Prompt]"));
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(T("AF_UI_WeeklyProjection_UserPrompt", "[User Prompt]"));
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildWeeklyBatchPromptPreviewTextForDisplay(WeeklyEditorDisplayPort port, WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<WeeklyEventMaterialPreviewGroup> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_BatchWeek", "Batch week"), ((batch != null) ? batch.WeekIndex : 0).ToString());
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_MaterialRange", "Material day range"), ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_BatchBlocks", "Batch block count"), list.Count.ToString());
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_BatchMode", "Batch mode"), ((batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report");
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_BatchTargets", "Batch targets"), string.Join(" | ", list.Select(group => port.BuildWeeklyReportGroupReportId(group)).Where((string x) => !string.IsNullOrWhiteSpace(x))));
		port.AppendDevNpcActionField(stringBuilder, T("AF_UI_WeeklyProjection_OutputMode", "Output mode"), string.Join(" | ", list.Select((WeeklyEventMaterialPreviewGroup x) => ((x?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "title_short_tags_only" : "full_report")));
		port.AppendDevNpcActionField(stringBuilder, "MaxTokens", port.GetEventAndRebellionApiMaxTokens().ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(T("AF_UI_WeeklyProjection_SystemPrompt", "[System Prompt]"));
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(T("AF_UI_WeeklyProjection_UserPrompt", "[User Prompt]"));
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildWeeklyPreviewMaterialLabel(WeeklyEditorDisplayPort port, EventMaterialReference material)
	{
		if (material == null)
		{
			return T("AF_UI_WeeklyProjection_InvalidMaterial", "Invalid material");
		}
		string text = port.TranslateEventMaterialTypeForDev(material.MaterialType);
		string text6 = material.ActionDay.HasValue ? T("AF_UI_WeeklyProjection_ActionDay", "[Day {DAY}] ", ("DAY", material.ActionDay.Value.ToString())) : "";
		if ((material.MaterialType ?? "").Trim().StartsWith("npc_", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = port.ResolveHeroDisplay(material.HeroId);
			string text3 = port.ResolveSettlementDisplay(material.SettlementId);
			string text4 = port.BuildDevSummaryPreview(material.SnapshotText, 18);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? T("AF_UI_WeeklyProjection_UnknownLord", "A lord") : text2) + " - " + text3;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? T("AF_UI_WeeklyProjection_UnknownLord", "A lord") : text2) + " - " + text4;
			}
			return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? T("AF_UI_WeeklyProjection_UnknownLord", "A lord") : text2);
		}
		string text5 = port.BuildDevSummaryPreview(!string.IsNullOrWhiteSpace(material.Label) ? material.Label : material.SnapshotText, 24);
		return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text5) ? T("AF_UI_WeeklyProjection_NoPreview", "No preview") : text5);
	}
}
