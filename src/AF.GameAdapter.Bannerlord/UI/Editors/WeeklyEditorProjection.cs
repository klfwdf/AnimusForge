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

namespace AnimusForge;

internal class WeeklyEditorDisplayPort
{
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
	internal static string BuildWeeklyEventMaterialPreviewGroupLabel(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "无效分组";
		}
		string text = string.IsNullOrWhiteSpace(group.Summary) ? "" : port.BuildDevSummaryPreview(group.Summary, 44);
		int count = (group.Materials != null) ? group.Materials.Count : 0;
		int count2 = (group.PromptMaterials != null) ? group.PromptMaterials.Count : 0;
		string text2 = count + " / " + count2 + " 条素材";
		string text3 = string.Equals((group.OutputMode).ToString(), WeeklyReportOutputMode.TitleShortTagsOnly.ToString(), StringComparison.OrdinalIgnoreCase) ? "短" : "全";
		return (group.Title ?? "未命名分组") + " [" + text3 + " | " + text2 + "]" + (string.IsNullOrWhiteSpace(text) ? "" : (" " + text));
	}

	internal static WeeklyReportBrowserCountryData BuildWeeklyReportBrowserCountryData(WeeklyEditorDisplayPort port, string eventKind, string scopeKingdomId, string displayName, bool isWorld, List<EventRecordEntry> source, IReadOnlyDictionary<string, List<string>> bulletinAssociations = null)
	{
		string text = (scopeKingdomId ?? "").Trim();
		string text3 = (isWorld ? "world" : text);
		string text2 = (displayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = (isWorld ? "\u4e16\u754c\u5468\u62a5" : text);
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
			if (WeeklyReportArchivePolicy.IsRecent(x.EventId))
				text3 = (x.ScopeKingdomId == WeeklyReportArchivePolicy.OtherKingdomId ? "其他近况" : port.ResolveKingdomDisplay(x.ScopeKingdomId) + "近况") + " · " + x.CreatedDate;
			string body = WeeklyReportArchivePolicy.BodyWithRegionalNews(x).Trim();
			return new WeeklyReportBrowserEntryData
			{
				EventId = (x.EventId ?? "").Trim(),
				ArchiveKind = WeeklyReportArchivePolicy.EntryKind(x.EventId),
				OpenTargetId = (x.EventId ?? "").Trim(),
				WeekIndex = Math.Max(0, x.WeekIndex),
				Title = text3,
				BodyText = body.Length == 0 ? "当前这期周报还没有正文。" : body,
				CreatedDate = (!string.IsNullOrWhiteSpace(x.CreatedDate) ? x.CreatedDate.Trim() : ("\u7b2c " + Math.Max(0, x.CreatedDay) + " \u65e5")),
				CreatedDay = Math.Max(0, x.CreatedDay),
				TagText = (x.TagText ?? "").Trim(),
				HasFullReport = !string.IsNullOrWhiteSpace(x.Summary)
			};
		}).ToList();
	}

	internal static string BuildWeeklyReportBrowserDefaultTitle(WeeklyEditorDisplayPort port, string eventKind, string scopeKingdomId, int weekIndex)
	{
		if (string.Equals((eventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return "\u4e16\u754c\u7b2c" + Math.Max(0, weekIndex) + "\u5468\u5468\u62a5";
		}
		string text = port.ResolveKingdomDisplay(scopeKingdomId);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "\u738b\u56fd";
		}
		return text + "\u7b2c" + Math.Max(0, weekIndex) + "\u5468\u5468\u62a5";
	}

	internal static string BuildWeeklyReportPromptPreviewText(WeeklyEditorDisplayPort port, WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
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

	internal static string BuildWeeklyBatchPromptPreviewText(WeeklyEditorDisplayPort port, WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
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

	internal static string BuildWeeklyPreviewMaterialLabel(WeeklyEditorDisplayPort port, EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材";
		}
		string text = port.TranslateEventMaterialTypeForDev(material.MaterialType);
		string text6 = material.ActionDay.HasValue ? ("[第" + material.ActionDay.Value + "日] ") : "";
		if ((material.MaterialType ?? "").Trim().StartsWith("npc_", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = port.ResolveHeroDisplay(material.HeroId);
			string text3 = port.ResolveSettlementDisplay(material.SettlementId);
			string text4 = port.BuildDevSummaryPreview(material.SnapshotText, 18);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2) + " - " + text3;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2) + " - " + text4;
			}
			return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2);
		}
		string text5 = port.BuildDevSummaryPreview(!string.IsNullOrWhiteSpace(material.Label) ? material.Label : material.SnapshotText, 24);
		return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text5) ? "无预览" : text5);
	}
}
