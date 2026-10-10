using System;
using System.Collections.Generic;

namespace AnimusForge;

// Original fact wording for weekly Prompt material; UI projection is separate.
internal static class NpcActionMaterialPreviewProjection
{
	internal static string Build(NpcActionEditorDisplayPort port, NpcActionEntry entry)
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
}

