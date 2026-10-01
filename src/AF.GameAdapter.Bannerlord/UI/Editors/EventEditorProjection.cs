using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal static class EventEditorProjection
{
	internal static string BuildKingdomRebellionResolutionText(EventEditorDisplayPort port, KingdomRebellionResolutionResult result)
	{
		if (result == null)
		{
			return "当前没有可显示的叛乱结果。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("王国：" + port.GetKingdomDisplayName(result.Kingdom, "某王国"));
		stringBuilder.AppendLine("稳定度：" + result.StabilityValue + "（" + (result.StabilityTierText ?? "未知") + "）");
		if (!result.Forced)
		{
			stringBuilder.AppendLine("本周叛乱概率：" + port.FormatKingdomRebellionChance(result.TriggerChance));
			if (result.Roll.HasValue)
			{
				stringBuilder.AppendLine("本次掷值：" + result.Roll.Value.ToString("0.000"));
			}
		}
		else
		{
			stringBuilder.AppendLine("本次模式：强制触发（跳过概率门槛）");
		}
		if (result.SelectedClan != null)
		{
			stringBuilder.AppendLine("选中的家族：" + port.GetClanDisplayName(result.SelectedClan));
		}
		if (result.SelectedFollowerClans != null && result.SelectedFollowerClans.Count > 0)
		{
			stringBuilder.AppendLine("预计联合响应家族：" + string.Join("、", result.SelectedFollowerClans.Select(clan => port.GetClanDisplayName(clan))));
		}
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			stringBuilder.AppendLine("结果：" + result.Message.Trim());
		}
		if (result.Candidates != null && result.Candidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("候选家族：");
			foreach (KingdomRebellionCandidateInfo item in result.Candidates.Take(8))
			{
				if (item == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item.ClanName + " | " + (item.Eligible ? "可触发" : "不可触发") + " | 关系 " + item.RelationToKing + " | 城镇 " + item.TownCount + " | 城堡 " + item.CastleCount + " | 评分 " + item.Score.ToString("0.0"));
				if (!string.IsNullOrWhiteSpace(item.Note))
				{
					stringBuilder.AppendLine("  " + item.Note.Trim());
				}
				if (item.PreviewFollowerClanNames != null && item.PreviewFollowerClanNames.Count > 0)
				{
					stringBuilder.AppendLine("  若由其主导叛乱，预计联合响应：" + string.Join("、", item.PreviewFollowerClanNames));
				}
				else
				{
					stringBuilder.AppendLine("  若由其主导叛乱，预计没有其他家族联合响应。");
				}
			}
		}
		if (result.FollowerCandidates != null && result.FollowerCandidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("联合响应候选：");
			foreach (KingdomRebellionFollowerInfo item2 in result.FollowerCandidates.Take(8))
			{
				if (item2 == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item2.ClanName + " | " + (item2.Eligible ? "会加入" : "不会加入") + " | 对国王 " + item2.RelationToKing + " | 对领袖 " + item2.RelationToLeader + " | 评分 " + item2.Score.ToString("0.0"));
				if (!string.IsNullOrWhiteSpace(item2.Note))
				{
					stringBuilder.AppendLine("  " + item2.Note.Trim());
				}
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal static List<Kingdom> GetDevEditableKingdoms()
	{
		try
		{
			return Kingdom.All.Where((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(x.StringId)).OrderBy((Kingdom x) => x.Name?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Kingdom x) => x.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return new List<Kingdom>();
		}
	}

	internal static string BuildDevSummaryPreview(string text, int maxLen)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		bool flag = false;
		foreach (char c in text)
		{
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		string text2 = stringBuilder.ToString().Trim();
		if (text2.Length <= maxLen)
		{
			return text2;
		}
		return text2.Substring(0, Math.Max(1, maxLen)) + "...";
	}

	internal static string BuildDevEventRecordItemLabel(EventEditorDisplayPort port, EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "无效事件";
		}
		string text = port.TranslateEventKindForDev(entry.EventKind);
		string text2 = string.IsNullOrWhiteSpace(entry.CreatedDate) ? ("第 " + Math.Max(0, entry.CreatedDay) + " 日") : entry.CreatedDate.Trim();
		string text3 = string.IsNullOrWhiteSpace(entry.ScopeKingdomId) ? "" : port.ResolveKingdomDisplay(entry.ScopeKingdomId);
		string text4 = string.IsNullOrWhiteSpace(text3) ? "" : (" [" + text3 + "]");
		int count = (entry.Materials != null) ? entry.Materials.Count : 0;
		return text2 + " [" + text + "] 第" + Math.Max(0, entry.WeekIndex) + "周" + text4 + " " + (entry.Title ?? "").Trim() + " (素材 " + count + " 条)";
	}

	internal static string BuildDevEventRecordMenuLabel(EventEditorDisplayPort port, EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "无效事件";
		}
		bool isWorld = string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string kingdomName = port.ResolveKingdomDisplay(entry.ScopeKingdomId);
		if (isWorld || string.IsNullOrWhiteSpace(kingdomName))
		{
			return "世界周报";
		}
		return kingdomName;
	}

	internal static string BuildDevEventMaterialItemLabel(EventEditorDisplayPort port, EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材";
		}
		string text = port.TranslateEventMaterialTypeForDev(material.MaterialType);
		string text2 = BuildDevSummaryPreview(material.Label, 56);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = BuildDevSummaryPreview(material.SnapshotText, 56);
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "无预览";
		}
		return "[" + text + "] " + text2;
	}

}
