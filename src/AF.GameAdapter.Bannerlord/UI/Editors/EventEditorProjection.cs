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
			return EventEditorUiText.T("AF_UI_Event_Rebellion_NoResult", "No rebellion result is available to display.");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Kingdom", "Kingdom: {KINGDOM}", ("KINGDOM", port.GetKingdomDisplayName(result.Kingdom, EventEditorUiText.T("AF_UI_Event_Kingdom_Fallback", "kingdom")))));
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Stability", "Stability: {VALUE} ({TIER})", ("VALUE", result.StabilityValue.ToString()), ("TIER", result.StabilityTierText ?? "unknown")));
		if (!result.Forced)
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Chance", "Weekly rebellion chance: {VALUE}", ("VALUE", port.FormatKingdomRebellionChance(result.TriggerChance))));
			if (result.Roll.HasValue)
			{
				stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Roll", "Roll: {VALUE}", ("VALUE", result.Roll.Value.ToString("0.000"))));
			}
		}
		else
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Forced", "Mode: forced trigger (probability gate skipped)"));
		}
		if (result.SelectedClan != null)
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_SelectedClan", "Selected clan: {CLAN}", ("CLAN", port.GetClanDisplayName(result.SelectedClan))));
		}
		if (result.SelectedFollowerClans != null && result.SelectedFollowerClans.Count > 0)
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Followers", "Expected responding clans: {CLANS}", ("CLANS", string.Join(", ", result.SelectedFollowerClans.Select(clan => port.GetClanDisplayName(clan))))));
		}
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Result", "Result: {RESULT}", ("RESULT", result.Message.Trim())));
		}
		if (result.Candidates != null && result.Candidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_Candidates", "Candidate clans:"));
			foreach (KingdomRebellionCandidateInfo item in result.Candidates.Take(8))
			{
				if (item == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item.ClanName + " | " + EventEditorUiText.T(item.Eligible ? "AF_UI_Event_Rebellion_Eligible" : "AF_UI_Event_Rebellion_Ineligible", item.Eligible ? "eligible" : "ineligible") + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_RelationKing", "relation {VALUE}", ("VALUE", item.RelationToKing.ToString())) + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_Towns", "towns {VALUE}", ("VALUE", item.TownCount.ToString())) + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_Castles", "castles {VALUE}", ("VALUE", item.CastleCount.ToString())) + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_Score", "score {VALUE}", ("VALUE", item.Score.ToString("0.0"))));
				if (!string.IsNullOrWhiteSpace(item.Note))
				{
					stringBuilder.AppendLine("  " + item.Note.Trim());
				}
				if (item.PreviewFollowerClanNames != null && item.PreviewFollowerClanNames.Count > 0)
				{
					stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_FollowerPreview", "  If it leads the rebellion, expected responses: {CLANS}", ("CLANS", string.Join(", ", item.PreviewFollowerClanNames))));
				}
				else
				{
					stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_NoFollowerPreview", "  If it leads the rebellion, no other clans are expected to respond."));
				}
			}
		}
		if (result.FollowerCandidates != null && result.FollowerCandidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Rebellion_FollowerCandidates", "Potential responding clans:"));
			foreach (KingdomRebellionFollowerInfo item2 in result.FollowerCandidates.Take(8))
			{
				if (item2 == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item2.ClanName + " | " + EventEditorUiText.T(item2.Eligible ? "AF_UI_Event_Rebellion_WillJoin" : "AF_UI_Event_Rebellion_WillNotJoin", item2.Eligible ? "will join" : "will not join") + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_RelationToKing", "king {VALUE}", ("VALUE", item2.RelationToKing.ToString())) + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_RelationToLeader", "leader {VALUE}", ("VALUE", item2.RelationToLeader.ToString())) + " | " + EventEditorUiText.T("AF_UI_Event_Rebellion_Score", "score {VALUE}", ("VALUE", item2.Score.ToString("0.0"))));
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
			return EventEditorUiText.T("AF_UI_Event_Invalid_Record", "Invalid event");
		}
		string text = port.TranslateEventKindForDev(entry.EventKind);
		string text2 = string.IsNullOrWhiteSpace(entry.CreatedDate) ? EventEditorUiText.T("AF_UI_Event_Field_Date", "Day {DAY}", ("DAY", Math.Max(0, entry.CreatedDay).ToString())) : entry.CreatedDate.Trim();
		string text3 = string.IsNullOrWhiteSpace(entry.ScopeKingdomId) ? "" : port.ResolveKingdomDisplay(entry.ScopeKingdomId);
		string text4 = string.IsNullOrWhiteSpace(text3) ? "" : (" [" + text3 + "]");
		int count = (entry.Materials != null) ? entry.Materials.Count : 0;
		return text2 + " [" + text + "] " + EventEditorUiText.T("AF_UI_Event_Field_Week", "Week {WEEK}", ("WEEK", Math.Max(0, entry.WeekIndex).ToString())) + text4 + " " + (entry.Title ?? "").Trim() + " (" + EventEditorUiText.T("AF_UI_Event_Field_MaterialCount", "Material count") + " " + count + ")";
	}

	internal static string BuildDevEventRecordMenuLabel(EventEditorDisplayPort port, EventRecordEntry entry)
	{
		if (entry == null)
		{
			return EventEditorUiText.T("AF_UI_Event_Invalid_Record", "Invalid event");
		}
		bool isWorld = string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string kingdomName = port.ResolveKingdomDisplay(entry.ScopeKingdomId);
		if (isWorld || string.IsNullOrWhiteSpace(kingdomName))
		{
			return EventEditorUiText.T("AF_UI_Event_WorldReport", "World weekly report");
		}
		return kingdomName;
	}

	internal static string BuildDevEventMaterialItemLabel(EventEditorDisplayPort port, EventMaterialReference material)
	{
		if (material == null)
		{
			return EventEditorUiText.T("AF_UI_Event_Invalid_Material", "Invalid material");
		}
		string text = port.TranslateEventMaterialTypeForDev(material.MaterialType);
		string text2 = BuildDevSummaryPreview(material.Label, 56);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = BuildDevSummaryPreview(material.SnapshotText, 56);
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = EventEditorUiText.T("AF_UI_Event_NoPreview", "No preview");
		}
		return "[" + text + "] " + text2;
	}

}
