using System;using System.Collections.Generic;using System.Linq;using System.Text;
namespace AnimusForge;
internal sealed class NpcActionEditorDisplayPort
{
 internal Func<NpcActionEntry,string> ResolveDisplayNameBySettlementEntry;
 internal Func<string,string> ResolveHeroDisplay;
 internal Func<string,string> ResolveClanDisplay;
 internal Func<string,string> ResolveKingdomDisplay;
 internal Func<NpcActionEntry,bool> HasStructuredNpcActionMetadata;
 internal Func<NpcActionEntry,string> BuildNpcActionActorNarrativeText;
 internal Func<string,string> TranslateNpcActionKindForPrompt;
}
// UI-only read projection; actual records and sanitization stay with the NpcAction domain owner.
internal static class NpcActionEditorProjection
{
	internal static string BuildDevNpcActionItemLabel(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "无效行动";
		}
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text2 = BuildDevNpcActionPreviewText(port, entry);
		if (text2.Length > 108)
		{
			text2 = text2.Substring(0, 108) + "...";
		}
		return text + " " + text2;
	}

	internal static string BuildDevNpcActionDetailSubtitle(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "查看行动记录（结构化）";
		}
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text2 = GetDevNpcActionKindDisplay(port, entry.ActionKind);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "查看行动记录（结构化） - " + text;
		}
		return "查看行动记录（结构化） - " + text + " / " + text2;
	}

	internal static string BuildDevNpcActionDetailText(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = BuildDevNpcActionNarrative(port, entry);
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.AppendLine("【易读说明】");
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("【原始字段】");
		AppendDevNpcActionField(stringBuilder, "日期", !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日"));
		AppendDevNpcActionField(stringBuilder, "日序", entry.Day.ToString());
		AppendDevNpcActionField(stringBuilder, "显示文本", (entry.Text ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "StableKey", (entry.StableKey ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "行动类型", GetDevNpcActionKindDisplay(port, entry.ActionKind));
		AppendDevNpcActionField(stringBuilder, "是否重大行动", entry.IsMajor ? "是" : "否");
		AppendDevNpcActionField(stringBuilder, "结果", !entry.Won.HasValue ? "" : (entry.Won.Value ? "获胜" : "失利"));
		AppendDevNpcActionField(stringBuilder, "地点文本", (entry.LocationText ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "定居点", port.ResolveDisplayNameBySettlementEntry(entry));
		AppendDevNpcActionField(stringBuilder, "定居点ID", (entry.SettlementId ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "定居点所属领主", port.ResolveHeroDisplay(entry.SettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, "定居点所属家族", port.ResolveClanDisplay(entry.SettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, "定居点所属王国", port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, "定居点前任领主", port.ResolveHeroDisplay(entry.PreviousSettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, "定居点前任家族", port.ResolveClanDisplay(entry.PreviousSettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, "定居点前任王国", port.ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, "行动人物", port.ResolveHeroDisplay(entry.ActorHeroId));
		AppendDevNpcActionField(stringBuilder, "行动家族", port.ResolveClanDisplay(entry.ActorClanId));
		AppendDevNpcActionField(stringBuilder, "行动王国", port.ResolveKingdomDisplay(entry.ActorKingdomId));
		AppendDevNpcActionField(stringBuilder, "目标人物", port.ResolveHeroDisplay(entry.TargetHeroId));
		AppendDevNpcActionField(stringBuilder, "目标家族", port.ResolveClanDisplay(entry.TargetClanId));
		AppendDevNpcActionField(stringBuilder, "目标王国", port.ResolveKingdomDisplay(entry.TargetKingdomId));
		AppendDevNpcActionField(stringBuilder, "相关人物ID", JoinDevActionIds(entry.RelatedHeroIds));
		AppendDevNpcActionField(stringBuilder, "相关家族ID", JoinDevActionIds(entry.RelatedClanIds));
		AppendDevNpcActionField(stringBuilder, "相关王国ID", JoinDevActionIds(entry.RelatedKingdomIds));
		if (!port.HasStructuredNpcActionMetadata(entry))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("提示：这条行动缺少较完整的结构化元数据，可能是旧版本记录，或当时游戏未提供足够目标信息。");
			stringBuilder.AppendLine("建议让游戏继续跑 1-2 天，再观察新生成的行动记录。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildDevNpcActionPreviewText(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "无效行动";
		}
		List<string> list = new List<string>();
		string text = port.BuildNpcActionActorNarrativeText(entry);
		string text2 = port.TranslateNpcActionKindForPrompt(entry.ActionKind);
		string text3 = port.ResolveDisplayNameBySettlementEntry(entry);
		string text4 = port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text5 = port.ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text6 = port.ResolveKingdomDisplay(entry.TargetKingdomId);
		string text7 = port.ResolveClanDisplay(entry.TargetClanId);
		string text8 = port.ResolveHeroDisplay(entry.TargetHeroId);
		string text9 = port.ResolveClanDisplay(entry.ActorClanId);
		string text10 = port.ResolveKingdomDisplay(entry.ActorKingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		else if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("这是一条" + text2);
		}
		if (!string.IsNullOrWhiteSpace(text3) && string.IsNullOrWhiteSpace(entry.SettlementId) && !text.Contains(text3))
		{
			list.Add("地点在" + text3);
		}
		if (!string.IsNullOrWhiteSpace(text9) && !string.IsNullOrWhiteSpace(text10))
		{
			list.Add("其所属家族是" + text9 + "，隶属于" + text10);
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("其所属家族是" + text9);
		}
		else if (!string.IsNullOrWhiteSpace(text10))
		{
			list.Add("其隶属于" + text10);
		}
		if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text4))
		{
			list.Add("当时该地由" + text5 + "掌控，隶属于" + text4);
		}
		else if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("当时该地隶属于" + text4);
		}
		else if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("当时该地由" + text5 + "掌控");
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("涉及人物是" + text8);
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("涉及家族是" + text7);
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("涉及王国是" + text6);
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利"));
		}
		if (entry.IsMajor)
		{
			list.Add("属于重大行动");
		}
		if (list.Count == 0)
		{
			return "这条记录暂时没有可读摘要。";
		}
		if (!port.HasStructuredNpcActionMetadata(entry))
		{
			list.Add("这条记录的结构化信息较少");
		}
		return string.Join("；", list) + "。";
	}

	internal static string BuildDevNpcActionNarrative(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text2 = port.TranslateNpcActionKindForPrompt(entry.ActionKind);
		string text3 = port.ResolveHeroDisplay(entry.ActorHeroId);
		string text15 = string.IsNullOrWhiteSpace(text3) ? "该人物" : text3;
		string text4 = port.ResolveDisplayNameBySettlementEntry(entry);
		string text5 = port.ResolveHeroDisplay(entry.TargetHeroId);
		string text6 = port.ResolveClanDisplay(entry.TargetClanId);
		string text7 = port.ResolveKingdomDisplay(entry.TargetKingdomId);
		string text8 = port.ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text9 = port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text10 = port.ResolveClanDisplay(entry.PreviousSettlementOwnerClanId);
		string text11 = port.ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId);
		string text12 = port.ResolveClanDisplay(entry.ActorClanId);
		string text13 = port.ResolveKingdomDisplay(entry.ActorKingdomId);
		list.Add(text + "。");
		string text14 = port.BuildNpcActionActorNarrativeText(entry);
		if (!string.IsNullOrWhiteSpace(text14))
		{
			list.Add(text14 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("这属于" + text2 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text12) && !string.IsNullOrWhiteSpace(text13))
		{
			list.Add(text15 + "所属家族是" + text12 + "，隶属于" + text13 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text12))
		{
			list.Add(text15 + "所属家族是" + text12 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text13))
		{
			list.Add(text15 + "隶属于" + text13 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("地点是" + text4 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text8) && !string.IsNullOrWhiteSpace(text9))
		{
			list.Add("当时该定居点由" + text8 + "掌控，隶属于" + text9 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("当时该定居点由" + text8 + "掌控。");
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("当时该定居点隶属于" + text9 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text10) && !string.IsNullOrWhiteSpace(text11))
		{
			list.Add("在此之前，这里由" + text10 + "掌控，归属" + text11 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("直接涉及的人物是" + text5 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("涉及的家族是" + text6 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("涉及的王国是" + text7 + "。");
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利") + "。");
		}
		if (entry.IsMajor)
		{
			list.Add("这被归类为重大行动。");
		}
		return string.Join("", list);
	}

	internal static string GetDevNpcActionKindDisplay(NpcActionEditorDisplayPort port, string actionKind)
	{
		string text = (actionKind ?? "").Trim();
		string text2 = port.TranslateNpcActionKindForPrompt(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return string.IsNullOrWhiteSpace(text) ? text2 : (text2 + " (" + text + ")");
	}

	internal static void AppendDevNpcActionField(StringBuilder stringBuilder, string label, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.AppendLine(label + "：" + value.Trim());
	}

	internal static string JoinDevActionIds(List<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return "";
		}
		List<string> list = ids.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return (list.Count == 0) ? "" : string.Join(", ", list);
	}

}
