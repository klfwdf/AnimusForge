using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public static class PolicySystemUi
{
	private const int EventInboxDisplayLimit = 160;
	private const int DetailBodyCharacterLimit = 1200;
	private const int DetailBodyLineLimit = 40;

	public static void OnApplicationTick()
	{
		try
		{
			AnimusForgeWorldEventInboxPopup.OnApplicationTick();
			WorldMessageTimelineUi.OnApplicationTick();
			ScreenBase top = ScreenManager.TopScreen;
			if (Campaign.Current == null || !(top is MapScreen))
			{
				AnimusForgeWorldEventInboxPopup.CloseActive(silent: true);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "world-policy-tick-failed", ex.Message, ex.ToString());
		}
	}

	public static bool ShowWorldPolicies(Action onClose = null)
	{
		try
		{
			if (Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
			{
				return false;
			}
			return AnimusForgeWorldEventInboxPopup.Show(BuildInboxPopupData(), onClose);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "world-policy-open-failed", ex.Message, ex.ToString());
			return false;
		}
	}

	public static void CloseWorldPolicies()
	{
		AnimusForgeWorldEventInboxPopup.CloseActive(silent: true);
	}

	private static WorldEventInboxPopupData BuildInboxPopupData()
	{
		List<AnimusForgeWorldEventInboxEntry> events = AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(EventInboxDisplayLimit);
		List<WorldEventCountryGroup> countries = BuildCountryGroups(events);
		WorldEventInboxPopupData data = new WorldEventInboxPopupData
		{
			TitleText = "世界政策",
			SubtitleText = "只读查看各国已经发布的玩家与非玩家统治者政策及政策衍生事件。",
			EmptyStateText = "暂无世界事件。统治者政策与政策衍生事件会出现在这里。",
			CloseText = "关闭"
		};

		foreach (WorldEventCountryGroup country in countries)
		{
			WorldEventCountryData countryData = new WorldEventCountryData
			{
				KingdomId = country.KingdomId ?? "",
				KingdomName = FirstNonEmpty(country.KingdomName, "未知国家"),
				UnreadCount = country.Events.Count(e => e != null && !e.IsRead)
			};
			foreach (AnimusForgeWorldEventInboxEntry entry in country.Events)
			{
				WorldEventRecordData record = BuildRecordData(entry);
				if (record != null)
				{
					countryData.Records.Add(record);
				}
			}
			data.Countries.Add(countryData);
		}

		int selected = data.Countries.FindIndex(x => x != null && x.Records.Count > 0);
		data.SelectedCountryIndex = selected >= 0 ? selected : 0;
		return data;
	}

	private static WorldEventRecordData BuildRecordData(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null)
		{
			return null;
		}
		string kind = FirstNonEmpty(entry.KindLabel, "世界事件");
		string date = FirstNonEmpty(entry.GameDate, entry.Day > 0 ? ("第" + entry.Day.ToString(CultureInfo.InvariantCulture) + "天") : "未知日期");
		string title = FirstNonEmpty(entry.Title, kind);
		string body = FirstNonEmpty(entry.DetailText, entry.Summary);
		body = (body ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		string meta = BuildRecordMetaText(entry, kind, date);
		string impact = entry.ImpactText ?? "";
		return new WorldEventRecordData
		{
			EventId = entry.EventId ?? "",
			KindLabel = kind,
			HeaderRightText = entry.HeaderRightText ?? "",
			DateText = date,
			TitleText = title,
			MetaText = meta,
			PolicyNameText = "",
			BodyText = string.IsNullOrWhiteSpace(body) ? "（无详情）" : body,
			BodySectionTitleText = FirstNonEmpty(entry.BodySectionTitleText, "事件详情"),
			ImpactSectionTitleText = entry.ImpactSectionTitleText ?? "",
			ImpactText = impact,
			IndexMetaText = date + "  ·  " + kind,
			UnreadMarkerText = entry.IsRead ? "" : "新",
			IsUnread = !entry.IsRead,
			HasPolicyName = false,
			HasImpact = !string.IsNullOrWhiteSpace(impact)
		};
	}

	private static string ExtractDetailLineValue(string detail, string prefix)
	{
		foreach (string line in (detail ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
		{
			string clean = (line ?? "").Trim();
			if (clean.StartsWith(prefix ?? "", StringComparison.Ordinal))
			{
				return clean.Substring((prefix ?? "").Length).Trim();
			}
		}
		return "";
	}

	private static string BuildRecordMetaText(AnimusForgeWorldEventInboxEntry entry, string kind, string date)
	{
		List<string> parts = new List<string>();
		parts.Add(date);
		parts.Add(kind);
		string kingdom = FirstNonEmpty(entry?.KingdomName);
		if (!string.IsNullOrWhiteSpace(kingdom))
		{
			parts.Add(kingdom);
		}
		string actor = FirstNonEmpty(entry?.ActorHeroName);
		if (!string.IsNullOrWhiteSpace(actor))
		{
			parts.Add("相关人物：" + actor);
		}
		return string.Join("  ·  ", parts);
	}

	private static List<WorldEventCountryGroup> BuildCountryGroups(List<AnimusForgeWorldEventInboxEntry> events)
	{
		Dictionary<string, WorldEventCountryGroup> byId = new Dictionary<string, WorldEventCountryGroup>(StringComparer.OrdinalIgnoreCase);
		List<WorldEventCountryGroup> groups = new List<WorldEventCountryGroup>();
		try
		{
			IEnumerable<Kingdom> kingdoms = Kingdom.All ?? Enumerable.Empty<Kingdom>();
			foreach (Kingdom kingdom in kingdoms.Where(k => k != null && !k.IsEliminated).OrderBy(GetKingdomNameSafe, StringComparer.OrdinalIgnoreCase))
			{
				string id = (kingdom.StringId ?? "").Trim();
				string name = GetKingdomNameSafe(kingdom);
				if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
				{
					continue;
				}
				AddOrGetCountryGroup(byId, groups, id, name);
			}
		}
		catch
		{
		}

		foreach (AnimusForgeWorldEventInboxEntry entry in events ?? new List<AnimusForgeWorldEventInboxEntry>())
		{
			if (entry == null)
			{
				continue;
			}
			string id = (entry.KingdomId ?? "").Trim();
			string name = FirstNonEmpty(entry.KingdomName, "未知国家");
			WorldEventCountryGroup group = AddOrGetCountryGroup(byId, groups, id, name);
			group.Events.Add(entry);
		}

		foreach (WorldEventCountryGroup group in groups)
		{
			group.Events = group.Events
				.Where(e => e != null)
				.OrderByDescending(e => e.Day)
				.ThenByDescending(e => e.CreatedUtcTicks)
				.ThenBy(e => e.EventKind ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		return groups
			.Where(g => g.Events.Count > 0)
			.OrderBy(g => g.KingdomName ?? g.KingdomId ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static WorldEventCountryGroup AddOrGetCountryGroup(Dictionary<string, WorldEventCountryGroup> byId, List<WorldEventCountryGroup> groups, string kingdomId, string kingdomName)
	{
		string id = (kingdomId ?? "").Trim();
		string name = FirstNonEmpty(kingdomName, "未知国家");
		string key = string.IsNullOrWhiteSpace(id) ? ("name:" + name.Trim()) : id;
		if (!byId.TryGetValue(key, out WorldEventCountryGroup group))
		{
			group = new WorldEventCountryGroup
			{
				KingdomId = id,
				KingdomName = name
			};
			byId[key] = group;
			groups.Add(group);
		}
		else if (string.IsNullOrWhiteSpace(group.KingdomName) || string.Equals(group.KingdomName, group.KingdomId, StringComparison.OrdinalIgnoreCase))
		{
			group.KingdomName = name;
		}
		return group;
	}

	private static string GetKingdomNameSafe(Kingdom kingdom)
	{
		try
		{
			return (kingdom?.Name?.ToString() ?? "未知国家").Trim();
		}
		catch
		{
			return "未知国家";
		}
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}

	private static string Limit(string text, int max)
	{
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)).TrimEnd() + "…";
	}

	private static string LimitMultiline(string text, int maxCharacters, int maxLines)
	{
		text = (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		bool truncated = false;
		string[] lines = text.Split('\n');
		if (maxLines > 0 && lines.Length > maxLines)
		{
			text = string.Join("\n", lines.Take(maxLines));
			truncated = true;
		}
		if (maxCharacters > 0 && text.Length > maxCharacters)
		{
			text = text.Substring(0, Math.Max(1, maxCharacters - 1)).TrimEnd();
			truncated = true;
		}
		return truncated ? text.TrimEnd('…') + "…" : text;
	}

	private sealed class WorldEventCountryGroup
	{
		public string KingdomId = "";
		public string KingdomName = "";
		public List<AnimusForgeWorldEventInboxEntry> Events = new List<AnimusForgeWorldEventInboxEntry>();
	}
}
