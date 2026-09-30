using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;

namespace AnimusForge;

internal static class WeeklyMaterialAggregationOwner
{
	internal static void Apply(MyBehavior.WeeklyEventMaterialPreviewGroup group,
		Func<MyBehavior.WeeklyEventMaterialPreviewGroup, string,
			Dictionary<string, List<MyBehavior.EventMaterialReference>>, MyBehavior.EventMaterialReference> renderCategory)
	{
		if (group?.Materials == null || group.Materials.Count == 0)
		{
			return;
		}
		List<MyBehavior.EventMaterialReference> ordered = MyBehavior.OrderWeeklyPreviewMaterials(group.Materials)
			.Where(material => material != null).Select(MyBehavior.CloneEventMaterialReference).ToList();
		List<MyBehavior.EventMaterialReference> preserved = new List<MyBehavior.EventMaterialReference>();
		Dictionary<string, Dictionary<string, List<MyBehavior.EventMaterialReference>>> buckets =
			new Dictionary<string, Dictionary<string, List<MyBehavior.EventMaterialReference>>>(StringComparer.OrdinalIgnoreCase);
		foreach (MyBehavior.EventMaterialReference material in ordered)
		{
			if (!IsWeeklyPromptAggregatableMaterial(material))
			{
				preserved.Add(material);
				continue;
			}
			string category = ResolveWeeklyPromptAggregateCategory(material);
			string eventKey = BuildWeeklyPromptAggregateEventKey(material);
			if (!buckets.TryGetValue(category, out var events))
			{
				events = new Dictionary<string, List<MyBehavior.EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
				buckets[category] = events;
			}
			if (!events.TryGetValue(eventKey, out var materials))
			{
				materials = new List<MyBehavior.EventMaterialReference>();
				events[eventKey] = materials;
			}
			materials.Add(material);
		}
		if (buckets.Count == 0)
		{
			group.Materials = preserved;
			return;
		}
		List<MyBehavior.EventMaterialReference> aggregated = new List<MyBehavior.EventMaterialReference>();
		foreach (string category in GetWeeklyPromptAggregateCategoryOrder())
		{
			if (buckets.TryGetValue(category, out var events))
			{
				MyBehavior.EventMaterialReference material = renderCategory(group, category, events);
				if (material != null)
				{
					aggregated.Add(material);
				}
			}
		}
		group.Materials = MyBehavior.OrderWeeklyPreviewMaterials(preserved.Concat(aggregated).ToList()).ToList();
	}

	internal static bool IsWeeklyPromptAggregatableMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		return string.Equals(text, "npc_recent_action", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "npc_major_action", StringComparison.OrdinalIgnoreCase);
	}

	internal static List<string> GetWeeklyPromptAggregateCategoryOrder()
	{
		return new List<string>
		{
			"strategic_shift",
			"decision",
			"siege",
			"settlement_change",
			"death",
			"captivity",
			"release",
			"army",
			"battle",
			"movement",
			"other"
		};
	}

	internal static string ResolveWeeklyPromptAggregateCategory(EventMaterialReference material)
	{
		string text = (material?.ActionKind ?? "").Trim().ToLowerInvariant();
		switch (text)
		{
		case "clan_changed_kingdom":
		case "clan_defected":
		case "clan_rebellion":
			return "strategic_shift";
		case "kingdom_decision_concluded":
		case "bilateral_diplomacy_pending":
		case "bilateral_diplomacy_outcome":
			return "decision";
		case "siege_start_attack":
		case "siege_start_defend":
		case "siege_join":
		case "siege_leave":
		case "siege_end_attack":
		case "siege_end_defend":
		case "siege_complete":
			return "siege";
		case "settlement_owner_changed_gain":
		case "settlement_owner_changed_loss":
		case "settlement_owner_changed_capture":
			return "settlement_change";
		case "hero_killed":
		case "hero_executed_victim":
		case "clan_member_killed":
			return "death";
		case "prisoner_taken_captor":
		case "prisoner_taken_prisoner":
			return "captivity";
		case "prisoner_released_captor":
		case "prisoner_released_prisoner":
			return "release";
		case "army_create":
		case "army_gather":
		case "army_disperse":
		case "army_join":
		case "army_leave":
			return "army";
		case "map_event":
		case "map_event_aftermath":
			return "battle";
		case "daily_behavior":
			return "movement";
		default:
			return "other";
		}
	}

	internal static string BuildWeeklyPromptAggregateEventKey(EventMaterialReference material)
	{
		string text3 = BuildWeeklyPromptBattleEventKey(material);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			return text3;
		}
		string text = NormalizeWeeklyPromptAggregateStableKey(material?.ActionStableKey);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = ((material?.ActionKind ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.SettlementId ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.KingdomId ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.HeroId ?? "").Trim().ToLowerInvariant());
		return text.Trim();
	}

	private static string BuildWeeklyPromptBattleEventKey(EventMaterialReference material)
	{
		if (material == null)
		{
			return "";
		}
		string text = (material.ActionKind ?? "").Trim();
		if (!string.Equals(text, "map_event", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "map_event_aftermath", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		string text2 = (material.ActionStableKey ?? "").Trim();
		if (text2.IndexOf(":side:", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			text2 = NormalizeWeeklyPromptAggregateStableKey(text2);
			if (!string.IsNullOrWhiteSpace(text2) && text2.StartsWith("mapevent:", StringComparison.OrdinalIgnoreCase) && text2.Length > "mapevent:".Length)
			{
				return text2;
			}
		}
		List<string> list = new List<string>();
		AddUniqueId(list, (material.ActorKingdomId ?? "").Trim());
		AddUniqueId(list, (material.TargetKingdomId ?? "").Trim());
		list.Sort(StringComparer.OrdinalIgnoreCase);
		List<string> list2 = new List<string>
		{
			ExtractWeeklyPromptBattleTroopText(material.SnapshotText, ownSide: true),
			ExtractWeeklyPromptBattleTroopText(material.SnapshotText, ownSide: false)
		};
		list2 = list2.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select(NormalizeWeeklyPromptKeyPart).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
		List<string> list3 = new List<string>
		{
			ExtractWeeklyPromptBattleMarkedText(material.SnapshotText, "我方死伤"),
			ExtractWeeklyPromptBattleMarkedText(material.SnapshotText, "敌方死伤")
		};
		list3 = list3.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select(NormalizeWeeklyPromptKeyPart).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
		string text3 = string.Join("~", new string[5]
		{
			(material.ActionDay ?? -1).ToString(),
			NormalizeWeeklyPromptKeyPart(material.LocationText),
			NormalizeWeeklyPromptKeyPart(string.Join("_", list)),
			string.Join("_", list2),
			string.Join("_", list3)
		});
		text3 = text3.Trim('~', '_');
		return string.IsNullOrWhiteSpace(text3) ? "" : ("battle:" + text3);
	}

	internal static string NormalizeWeeklyPromptAggregateStableKey(string stableKey)
	{
		string text = (stableKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = NormalizeWeeklyPromptMapEventStableKey(text);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		string[] array = new string[9] { ":captor", ":prisoner", ":gain", ":loss", ":capture", ":chooser", ":killer", ":victim", ":clan" };
		foreach (string value in array)
		{
			if (text.EndsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return text.Substring(0, text.Length - value.Length);
			}
		}
		if (text.EndsWith(":proposer", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - ":proposer".Length);
		}
		return text;
	}

	private static string NormalizeWeeklyPromptMapEventStableKey(string stableKey)
	{
		string text = (stableKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = "";
		if (text.StartsWith("mapevent_aftermath:", StringComparison.OrdinalIgnoreCase))
		{
			text2 = text.Substring("mapevent_aftermath:".Length);
		}
		else if (text.StartsWith("mapevent:", StringComparison.OrdinalIgnoreCase))
		{
			text2 = text.Substring("mapevent:".Length);
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		int num = text2.IndexOf(":side:", StringComparison.OrdinalIgnoreCase);
		if (num > 0)
		{
			return "mapevent:" + text2.Substring(0, num).Trim();
		}
		string[] array = text2.Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length <= 0 || string.IsNullOrWhiteSpace(array[0]))
		{
			return "";
		}
		return "mapevent:" + array[0].Trim();
	}

	private static string ExtractWeeklyPromptBattleTroopText(string text, bool ownSide)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		Match match = Regex.Match(text2, "战前投入兵力：我方(?<own>[^；。]+)；敌方(?<enemy>[^。；]+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return "";
		}
		string value = ownSide ? (match.Groups["own"]?.Value ?? "") : (match.Groups["enemy"]?.Value ?? "");
		return (value ?? "").Trim();
	}

	private static string ExtractWeeklyPromptBattleMarkedText(string text, string marker)
	{
		string text2 = (text ?? "").Trim();
		string text3 = (marker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
		{
			return "";
		}
		int num = text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		num += text3.Length;
		while (num < text2.Length && (text2[num] == '：' || text2[num] == ':' || text2[num] == ' '))
		{
			num++;
		}
		int num2 = text2.Length;
		foreach (char value in new char[4] { '。', '；', ';', '\n' })
		{
			int num3 = text2.IndexOf(value, num);
			if (num3 >= num && num3 < num2)
			{
				num2 = num3;
			}
		}
		if (num2 <= num)
		{
			return "";
		}
		return text2.Substring(num, num2 - num).Trim();
	}

	private static string NormalizeWeeklyPromptKeyPart(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return Regex.Replace(text, "[\\s:|]+", "_");
	}

	internal static void AddUniqueId(List<string> list, string id)
	{
		string text = (id ?? "").Trim();
		if (list == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!list.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			list.Add(text);
		}
	}

	internal static EventMaterialReference BuildCategoryMaterial(MyBehavior.WeeklyEventMaterialPreviewGroup group, string category, Dictionary<string, List<EventMaterialReference>> eventBuckets, string categoryLabel, Func<List<EventMaterialReference>, string> renderEventLine)
	{
		if (eventBuckets == null || eventBuckets.Count == 0)
		{
			return null;
		}
		List<List<EventMaterialReference>> list = eventBuckets.Values.Where((List<EventMaterialReference> x) => x != null && x.Count > 0).OrderBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionDay ?? int.MaxValue)).ThenBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionSequence ?? int.MaxValue)).ThenBy((List<EventMaterialReference> x) => x.FirstOrDefault((EventMaterialReference y) => y != null)?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		string text = categoryLabel;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[" + text + "]");
		foreach (List<EventMaterialReference> item in list)
		{
			string weeklyPromptAggregateEventLine = renderEventLine(item);
			if (!string.IsNullOrWhiteSpace(weeklyPromptAggregateEventLine))
			{
				stringBuilder.AppendLine("- " + weeklyPromptAggregateEventLine.Trim());
			}
		}
		string text2 = stringBuilder.ToString().TrimEnd();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return null;
		}
		EventMaterialReference eventMaterialReference = new EventMaterialReference
		{
			MaterialType = "prompt_agg_" + (category ?? "").Trim().ToLowerInvariant(),
			Label = text,
			SnapshotText = text2,
			KingdomId = group?.KingdomId ?? "",
			ActionKind = "prompt_aggregate:" + category,
			SourceMaterialCount = list.Sum((List<EventMaterialReference> x) => x.Count),
			ActionDay = list.Min((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionDay ?? int.MaxValue)),
			ActionSequence = list.Min((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionSequence ?? int.MaxValue))
		};
		foreach (List<EventMaterialReference> item2 in list)
		{
			foreach (EventMaterialReference item3 in item2)
			{
				AppendMaterialReferenceIds(item3, eventMaterialReference);
			}
		}
		return eventMaterialReference;
	}

	internal static void AppendMaterialReferenceIds(EventMaterialReference source, EventMaterialReference destination)
	{
		if (source == null || destination == null)
		{
			return;
		}
		AddUniqueId(destination.RelatedHeroIds, source.ActorHeroId);
		AddUniqueId(destination.RelatedHeroIds, source.TargetHeroId);
		AddUniqueId(destination.RelatedHeroIds, source.HeroId);
		AddUniqueId(destination.RelatedHeroIds, source.SettlementOwnerHeroId);
		AddUniqueId(destination.RelatedHeroIds, source.PreviousSettlementOwnerHeroId);
		CopyFactIds(source.RelatedHeroIds, destination.RelatedHeroIds);
		AddUniqueId(destination.RelatedClanIds, source.ActorClanId);
		AddUniqueId(destination.RelatedClanIds, source.TargetClanId);
		AddUniqueId(destination.RelatedClanIds, source.SettlementOwnerClanId);
		AddUniqueId(destination.RelatedClanIds, source.PreviousSettlementOwnerClanId);
		CopyFactIds(source.RelatedClanIds, destination.RelatedClanIds);
		AddUniqueId(destination.RelatedKingdomIds, source.ActorKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.TargetKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.KingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.SettlementOwnerKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.PreviousSettlementOwnerKingdomId);
		CopyFactIds(source.RelatedKingdomIds, destination.RelatedKingdomIds);
		CopyFactIds(source.SourceStableKeys, destination.SourceStableKeys);
		CopyFactIds(source.SourceActionKinds, destination.SourceActionKinds);
		AddUniqueId(destination.SourceStableKeys, source.ActionStableKey);
		AddUniqueId(destination.SourceActionKinds, source.ActionKind);
	}

	private static void CopyFactIds(List<string> source, List<string> destination)
	{
		if (source == null || destination == null)
		{
			return;
		}
		foreach (string item in source)
		{
			AddUniqueId(destination, item);
		}
	}

	internal static string GetWeeklyPromptAggregateCategoryLabel(string category)
	{
		switch ((category ?? "").Trim().ToLowerInvariant())
		{
		case "strategic_shift":
			return "家族与王国归属";
		case "decision":
			return "王国决议";
		case "siege":
			return "围城与守城";
		case "settlement_change":
			return "定居点易主";
		case "captivity":
			return "人物被俘";
		case "release":
			return "人物获释";
		case "army":
			return "军团行动";
		case "battle":
			return "战场交锋";
		case "movement":
			return "行军与袭扰";
		default:
			return "其他事件";
		}
	}
}
