using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;
using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

internal static partial class WeeklyPromptMaterialOwner
{
	internal static void Prepare(WeeklyEventMaterialPreviewGroup group, bool shortOnly)
	{
		if (group == null)
		{
			return;
		}
		group.OutputMode = shortOnly ? WeeklyReportOutputMode.TitleShortTagsOnly : WeeklyReportOutputMode.FullReport;
		group.IncludePreviousReportInPrompt = !shortOnly;
		group.PromptMaterials = shortOnly ? BuildShort(group) : BuildFull(group);
		if (group.PromptMaterials == null)
		{
			group.PromptMaterials = new List<EventMaterialReference>();
		}
	}

	private static List<EventMaterialReference> BuildFull(WeeklyEventMaterialPreviewGroup group)
	{
		List<EventMaterialReference> source = OrderWeeklyPreviewMaterials(group?.Materials)
			.Where(x => x != null).Select(CloneEventMaterialReference).ToList();
		return ResolveVillageRaids(source);
	}

	internal static List<EventMaterialReference> ResolveVillageRaids(List<EventMaterialReference> source)
	{
		List<EventMaterialReference> materials = new List<EventMaterialReference>();
		Dictionary<string, List<EventMaterialReference>> raids = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
		int unknownIndex = 0;
		foreach (EventMaterialReference item in OrderWeeklyPreviewMaterials(source).Where(x => x != null))
		{
			if (!IsWeeklyPromptVillageRaidMaterial(item))
			{
				materials.Add(item);
				continue;
			}
			string key = ResolveVillageRaidKey(item);
			if (string.IsNullOrWhiteSpace(key))
			{
				key = "unknown_village_raid_" + unknownIndex++;
			}
			if (!raids.TryGetValue(key, out var events))
			{
				events = new List<EventMaterialReference>();
				raids[key] = events;
			}
			events.Add(item);
		}
		foreach (List<EventMaterialReference> events in raids.Values.Where(x => x != null && x.Count > 0)
			.OrderBy(x => x.Min(y => y?.ActionDay ?? int.MaxValue))
			.ThenBy(x => x.Min(y => y?.ActionSequence ?? int.MaxValue))
			.ThenBy(x => ResolveVillageRaidKey(x[0]), StringComparer.OrdinalIgnoreCase))
		{
			EventMaterialReference resolved = BuildWeeklyPromptResolvedVillageRaidMaterial(events);
			if (resolved != null)
			{
				materials.Add(resolved);
			}
		}
		return OrderWeeklyPreviewMaterials(materials).ToList();
	}

	private static List<EventMaterialReference> BuildShort(WeeklyEventMaterialPreviewGroup group)
	{
		List<EventMaterialReference> materials = new List<EventMaterialReference>();
		List<EventMaterialReference> source = OrderWeeklyPreviewMaterials(group?.Materials).Where(x => x != null).ToList();
		List<EventMaterialReference> settlementStats = source.Where(IsWeeklyPromptSettlementStatsMaterial).ToList();
		List<EventMaterialReference> villageRaids = source.Where(IsWeeklyPromptVillageRaidMaterial).ToList();
		bool addedSettlementStats = false;
		bool addedVillageRaids = false;
		foreach (EventMaterialReference item in source)
		{
			if (IsWeeklyPromptTournamentMaterial(item) || IsWeeklyPromptOpeningSummaryMaterial(item))
			{
				continue;
			}
			if (IsWeeklyPromptSettlementStatsMaterial(item))
			{
				if (!addedSettlementStats)
				{
					EventMaterialReference summary = BuildWeeklyPromptShortSettlementStatsMaterial(settlementStats, group);
					if (summary != null) materials.Add(summary);
					addedSettlementStats = true;
				}
				continue;
			}
			if (IsWeeklyPromptVillageRaidMaterial(item))
			{
				if (!addedVillageRaids)
				{
					EventMaterialReference summary = BuildWeeklyPromptShortVillageRaidMaterial(villageRaids, group);
					if (summary != null) materials.Add(summary);
					addedVillageRaids = true;
				}
				continue;
			}
			EventMaterialReference selected = BuildWeeklyPromptShortMaterial(item, group);
			if (selected != null) materials.Add(selected);
		}
		return OrderWeeklyPreviewMaterials(materials).ToList();
	}

	internal static EventMaterialReference BuildResolvedVillageRaidMaterial(List<EventMaterialReference> source, Func<string, string> resolveSettlementDisplay)
	{
		List<EventMaterialReference> list = OrderWeeklyPreviewMaterials(source).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(list[0]);
		string text = ResolveVillageRaidKey(eventMaterialReference);
		string text2 = resolveSettlementDisplay(eventMaterialReference.SettlementId);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = string.IsNullOrWhiteSpace(text) ? "某村庄" : text;
		}
		List<EventMaterialReference> list2 = list.Where(IsWeeklyPromptVillageRaidCompletionMaterial).ToList();
		List<string> list3 = new List<string>();
		foreach (EventMaterialReference item in list2)
		{
			list3.Add(text2 + "：" + GetWeeklyPromptVillageRaidOutcomeMaterialText(ResolveVillageRaidOutcome(item)));
		}
		bool flag = IsWeeklyPromptVillageRaidStartedMaterial(list[list.Count - 1]);
		if (list2.Count == 0)
		{
			list3.Add(text2 + "：正在遭到掠夺，截止本期没有结算结果，不能写为掠夺成功。");
		}
		else if (flag)
		{
			list3.Add(text2 + "：随后又遭到掠夺，截止本期没有结算结果，不能写为掠夺成功。");
		}
		eventMaterialReference.MaterialType = "prompt_resolved_village_raid";
		eventMaterialReference.Label = "村庄掠夺结算 - " + text2;
		eventMaterialReference.SnapshotText = "[村庄掠夺结算]\n- " + string.Join("\n- ", list3);
		eventMaterialReference.ActionKind = "prompt_resolved:village_raid";
		eventMaterialReference.ActionStableKey = "prompt_resolved_village_raid:" + text;
		eventMaterialReference.SourceMaterialCount = list.Count;
		eventMaterialReference.ActionDay = list.Min((EventMaterialReference x) => x?.ActionDay ?? int.MaxValue);
		eventMaterialReference.ActionSequence = list.Min((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue);
		foreach (EventMaterialReference item2 in list.Skip(1))
		{
			WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(item2, eventMaterialReference);
		}
		return eventMaterialReference;
	}

	internal static bool IsWeeklyPromptVillageRaidStartedMaterial(EventMaterialReference material)
	{
		string text = (material?.ActionStableKey ?? "").Trim();
		return text.StartsWith("village_raid_started:", StringComparison.OrdinalIgnoreCase) || (material?.Label ?? "").Trim().StartsWith("掠夺开始", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsWeeklyPromptVillageRaidCompletionMaterial(EventMaterialReference material)
	{
		string text = (material?.ActionStableKey ?? "").Trim();
		return text.StartsWith("raid_completed:", StringComparison.OrdinalIgnoreCase) || (material?.Label ?? "").Trim().StartsWith("掠夺结果", StringComparison.OrdinalIgnoreCase);
	}

	internal static string GetWeeklyPromptVillageRaidOutcomeMaterialText(string outcome)
	{
		switch ((outcome ?? "").Trim())
		{
		case "success":
			return "掠夺成功，村庄确已被成功掠夺。";
		case "defended":
			return "掠夺被击退，入侵者未能成功掠夺村庄。";
		case "aborted":
			return "掠夺中止，入侵者未能成功掠夺村庄。";
		default:
			return "掠夺已结束但结果未确认，不能写为掠夺成功。";
		}
	}

	internal static string ResolveVillageRaidKey(EventMaterialReference material)
	{
		string text = (material?.SettlementId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (material?.ActionStableKey ?? "").Trim();
		string[] array = text2.Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length >= 2)
		{
			return (array[1] ?? "").Trim();
		}
		return text2;
	}

	internal static string ResolveVillageRaidOutcome(EventMaterialReference material)
	{
		string text = (material?.ActionStableKey ?? "").Trim();
		if (text.EndsWith(":Attacker", StringComparison.OrdinalIgnoreCase))
		{
			return "success";
		}
		if (text.EndsWith(":Defender", StringComparison.OrdinalIgnoreCase))
		{
			return "defended";
		}
		if (text.EndsWith(":None", StringComparison.OrdinalIgnoreCase))
		{
			return "aborted";
		}
		string text2 = (material?.SnapshotText ?? "").Trim();
		if (text2.IndexOf("掠夺成功", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "success";
		}
		if (text2.IndexOf("掠夺被击退", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "defended";
		}
		if (text2.IndexOf("掠夺中止", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "aborted";
		}
		return "";
	}

	internal static bool IsWeeklyPromptVillageRaidMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionStableKey ?? "").Trim();
		string text3 = (material.Label ?? "").Trim();
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("village_raid_started:", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("raid_completed:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("掠夺开始", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("掠夺结果", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool IsWeeklyPromptSettlementStatsMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionStableKey ?? "").Trim();
		string text3 = (material.Label ?? "").Trim();
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("settlement_stats:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("定居点状态变化", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool IsWeeklyPromptTournamentMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionStableKey ?? "").Trim();
		string text3 = (material.Label ?? "").Trim();
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("tournament_finished:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("竞技大会结算", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool IsWeeklyPromptOpeningSummaryMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		return string.Equals(text, "world_opening_summary", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "kingdom_opening_summary", StringComparison.OrdinalIgnoreCase);
	}

	private static EventMaterialReference BuildWeeklyPromptShortMaterial(EventMaterialReference item, WeeklyEventMaterialPreviewGroup group)
	{
		if (IsWeeklyPromptMovementAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortMovementMaterial(item);
		}
		if (IsWeeklyPromptCaptivityAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortCaptivityMaterial(item, group);
		}
		if (IsWeeklyPromptReleaseAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortReleaseMaterial(item, group);
		}
		if (IsWeeklyPromptClanChangeAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortClanChangeMaterial(item);
		}
		if (IsWeeklyPromptArmyAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortArmyMaterial(item);
		}
		if (IsWeeklyPromptSiegeAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortSiegeMaterial(item);
		}
		if (IsWeeklyPromptBattleAggregateMaterial(item))
		{
			return BuildWeeklyPromptShortBattleMaterial(item, group);
		}
		return CloneEventMaterialReference(item);
	}

	private static bool IsWeeklyPromptMovementAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_movement", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:movement", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "行军与袭扰", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptCaptivityAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_captivity", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:captivity", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "人物被俘", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptReleaseAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_release", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:release", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "人物获释", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptClanChangeAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_strategic_shift", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:strategic_shift", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "家族与王国归属", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptArmyAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_army", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:army", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "军团行动", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptSiegeAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_siege", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:siege", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "围城与守城", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptBattleAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_battle", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:battle", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), "战场交锋", StringComparison.OrdinalIgnoreCase);
	}

	private static EventMaterialReference BuildWeeklyPromptShortMovementMaterial(EventMaterialReference source)
	{
		if (source == null)
		{
			return null;
		}
		string text = (source.SnapshotText ?? source.Label ?? "").Trim();
		int num = Math.Max(0, source.SourceMaterialCount);
		if (num <= 0)
		{
			num = Regex.Matches(text, "事件=近期行军动向", RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		List<string> list = ExtractWeeklyPromptShortMovementLocations(text).Take(5).ToList();
		List<string> list2 = new List<string>();
		AddWeeklyPromptShortMovementAction(list2, text, "守备", "守备");
		AddWeeklyPromptShortMovementAction(list2, text, "保卫", "守备");
		AddWeeklyPromptShortMovementAction(list2, text, "袭扰", "袭扰");
		AddWeeklyPromptShortMovementAction(list2, text, "劫掠", "袭扰");
		AddWeeklyPromptShortMovementAction(list2, text, "掠夺", "袭扰");
		AddWeeklyPromptShortMovementAction(list2, text, "围攻", "围攻");
		AddWeeklyPromptShortMovementAction(list2, text, "强攻", "围攻");
		if (text.IndexOf("一带行动", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("前往", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("行军", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			AddUniqueId(list2, "普通行军");
		}
		if (list2.Count == 0)
		{
			list2.Add("普通行军");
		}
		string text2 = (list.Count > 0) ? string.Join("、", list) : "未列明";
		string text3 = "[行军与袭扰]\n- 近期行军动向事件共：" + num + "条；地点：" + text2 + "；主要行动：" + string.Join("、", list2) + "。";
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_movement";
		eventMaterialReference.Label = "行军与袭扰";
		eventMaterialReference.SnapshotText = text3;
		eventMaterialReference.ActionKind = "prompt_short:movement";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortCaptivityMaterial(EventMaterialReference source, WeeklyEventMaterialPreviewGroup group)
	{
		if (source == null)
		{
			return null;
		}
		string text = (group?.KingdomId ?? "").Trim();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in source.SourceStableKeys ?? new List<string>())
		{
			string captorHeroId;
			string prisonerHeroId;
			if (!TryParseWeeklyPromptPrisonerPair(item, out captorHeroId, out prisonerHeroId))
			{
				continue;
			}
			string text2 = (captorHeroId ?? "").Trim() + "->" + (prisonerHeroId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
			{
				continue;
			}
			num++;
			string text3 = MyBehavior.ResolveHeroKingdomIdForPrompt(captorHeroId);
			string text4 = MyBehavior.ResolveHeroKingdomIdForPrompt(prisonerHeroId);
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase))
			{
				num2++;
			}
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text4, text, StringComparison.OrdinalIgnoreCase))
			{
				num3++;
			}
		}
		if (num <= 0)
		{
			num = Math.Max(0, source.SourceMaterialCount);
		}
		if (num <= 0)
		{
			num = Regex.Matches(source.SnapshotText ?? "", "被.+?俘虏", RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		string text5 = "[人物被俘]\n- 人物被俘事件共：" + num + "条";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 += "；俘虏敌人：" + num2 + "人；己方被俘：" + num3 + "人";
		}
		text5 += "。";
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_captivity";
		eventMaterialReference.Label = "人物被俘";
		eventMaterialReference.SnapshotText = text5;
		eventMaterialReference.ActionKind = "prompt_short:captivity";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortReleaseMaterial(EventMaterialReference source, WeeklyEventMaterialPreviewGroup group)
	{
		if (source == null)
		{
			return null;
		}
		string text = (group?.KingdomId ?? "").Trim();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in source.SourceStableKeys ?? new List<string>())
		{
			string captorHeroId;
			string prisonerHeroId;
			if (!TryParseWeeklyPromptPrisonerPair(item, out captorHeroId, out prisonerHeroId))
			{
				continue;
			}
			string text2 = (captorHeroId ?? "").Trim() + "->" + (prisonerHeroId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
			{
				continue;
			}
			num++;
			string text3 = MyBehavior.ResolveHeroKingdomIdForPrompt(captorHeroId);
			string text4 = MyBehavior.ResolveHeroKingdomIdForPrompt(prisonerHeroId);
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase) && !string.Equals(text4, text, StringComparison.OrdinalIgnoreCase))
			{
				num2++;
			}
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text4, text, StringComparison.OrdinalIgnoreCase))
			{
				num3++;
			}
		}
		if (num <= 0)
		{
			num = Math.Max(0, source.SourceMaterialCount);
		}
		if (num <= 0)
		{
			num = Regex.Matches(source.SnapshotText ?? "", "释放|获释|结束囚禁", RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		string text5 = "[人物获释]\n- 人物获释事件共：" + num + "条";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 += "；释放敌方领主：" + num2 + "人；己方领主获释：" + num3 + "人";
		}
		text5 += "。";
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_release";
		eventMaterialReference.Label = "人物获释";
		eventMaterialReference.SnapshotText = text5;
		eventMaterialReference.ActionKind = "prompt_short:release";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortClanChangeMaterial(EventMaterialReference source)
	{
		if (source == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		foreach (string item in source.SourceStableKeys ?? new List<string>())
		{
			WeeklyPromptClanChangeParseResult weeklyPromptClanChangeParseResult = ParseWeeklyPromptClanChangeDetail(item);
			if (weeklyPromptClanChangeParseResult == null)
			{
				continue;
			}
			string[] array = (item ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
			if (array.Length < 4)
			{
				continue;
			}
			string text = MyBehavior.ResolveClanDisplay((array[1] ?? "").Trim());
			string text2 = MyBehavior.ResolveKingdomDisplay(weeklyPromptClanChangeParseResult.oldKingdomId);
			string text3 = MyBehavior.ResolveKingdomDisplay(weeklyPromptClanChangeParseResult.newKingdomId);
			string text4 = weeklyPromptClanChangeParseResult.detailLabel ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (string.Equals(text4, "脱离并叛乱", StringComparison.OrdinalIgnoreCase))
			{
				AddUniqueId(list, string.IsNullOrWhiteSpace(text2) ? (text + "发动叛乱") : (text + "脱离" + text2 + "并发动叛乱"));
			}
			else if (string.Equals(text4, "叛逃改投", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, text + "背离" + text2 + "并改投" + text3);
				}
				else if (!string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, text + "改投" + text3);
				}
			}
			else if (string.Equals(text4, "建立新王国", StringComparison.OrdinalIgnoreCase))
			{
				AddUniqueId(list, string.IsNullOrWhiteSpace(text3) ? (text + "建立新王国") : (text + "建立" + text3));
			}
			else if (!string.IsNullOrWhiteSpace(text3))
			{
				AddUniqueId(list, text + "加入" + text3);
			}
			else if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text4))
			{
				AddUniqueId(list, text + text4 + "：" + text2);
			}
		}
		string text5;
		if (list.Count > 0)
		{
			bool flag = list.Any((string x) => (x ?? "").IndexOf("发动叛乱", StringComparison.OrdinalIgnoreCase) >= 0);
			text5 = "[家族与王国归属]\n- " + (flag ? "家族叛乱" : "家族归属变更") + "：" + string.Join("；", list.Take(6)) + "。";
		}
		else
		{
			int num = Math.Max(1, Math.Max(0, source.SourceMaterialCount));
			text5 = "[家族与王国归属]\n- 家族归属变更事件共：" + num + "条。";
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_strategic_shift";
		eventMaterialReference.Label = "家族与王国归属";
		eventMaterialReference.SnapshotText = text5;
		eventMaterialReference.ActionKind = "prompt_short:strategic_shift";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortArmyMaterial(EventMaterialReference source)
	{
		if (source == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string item in SplitWeeklyPromptShortArmyLines(source.SnapshotText))
		{
			string text = (item ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string actor = ExtractWeeklyPromptShortArmyActor(text, "组建了新的军团");
			if (string.IsNullOrWhiteSpace(actor))
			{
				actor = ExtractWeeklyPromptShortArmyActor(text, "开始集结军团");
			}
			if (!string.IsNullOrWhiteSpace(actor))
			{
				AddUniqueId(list, actor);
				continue;
			}
			actor = ExtractWeeklyPromptShortArmyActor(text, "解散了军团");
			if (string.IsNullOrWhiteSpace(actor))
			{
				actor = ExtractWeeklyPromptShortArmyActor(text, "解散了他的军团");
			}
			if (!string.IsNullOrWhiteSpace(actor))
			{
				AddUniqueId(list2, actor);
			}
		}
		string text2;
		if (list.Count > 0 || list2.Count > 0)
		{
			List<string> list3 = new List<string>();
			if (list.Count > 0)
			{
				list3.Add("集结/组建：" + string.Join("、", list.Take(8)));
			}
			if (list2.Count > 0)
			{
				list3.Add("解散：" + string.Join("、", list2.Take(8)));
			}
			text2 = "[军团行动]\n- " + string.Join("；", list3) + "。";
		}
		else
		{
			int num = Math.Max(1, Math.Max(0, source.SourceMaterialCount));
			text2 = "[军团行动]\n- 军团行动事件共：" + num + "条；加入/离开已略。";
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_army";
		eventMaterialReference.Label = "军团行动";
		eventMaterialReference.SnapshotText = text2;
		eventMaterialReference.ActionKind = "prompt_short:army";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortSiegeMaterial(EventMaterialReference source)
	{
		if (source == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string item in SplitWeeklyPromptShortArmyLines(source.SnapshotText))
		{
			string text = (item ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.IndexOf("加入围城的行动", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("离开围城的行动", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("围城结束后的攻方行动", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("围城结束后的守方行动", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				continue;
			}
			if (text.IndexOf("参与围攻的行动", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text2 = ExtractWeeklyPromptShortSiegeTarget(text, "对", "发起了围攻");
				if (!string.IsNullOrWhiteSpace(text2))
				{
					AddUniqueId(list, "围攻" + text2);
				}
				continue;
			}
			if (text.IndexOf("参与守城的行动", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text3 = ExtractWeeklyPromptShortSiegeTarget(text, "加入了", "的守城");
				if (!string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, "守城" + text3);
				}
				continue;
			}
			if (text.IndexOf("围城结果事件", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text4 = ExtractWeeklyPromptShortSiegeTarget(text, "围绕", "进行了围城行动");
				string text5 = ExtractWeeklyPromptShortSiegeResult(text);
				if (!string.IsNullOrWhiteSpace(text4) || !string.IsNullOrWhiteSpace(text5))
				{
					AddUniqueId(list2, (string.IsNullOrWhiteSpace(text4) ? "目标据点" : text4) + (string.IsNullOrWhiteSpace(text5) ? "" : ("：" + text5)));
				}
			}
		}
		if (list.Count == 0 && list2.Count == 0)
		{
			return null;
		}
		List<string> list3 = new List<string>();
		if (list.Count > 0)
		{
			list3.Add("开始：" + string.Join("、", list.Take(6)));
		}
		if (list2.Count > 0)
		{
			list3.Add("结果：" + string.Join("；", list2.Take(6)));
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_siege";
		eventMaterialReference.Label = "围城与守城";
		eventMaterialReference.SnapshotText = "[围城与守城]\n- " + string.Join("；", list3) + "。";
		eventMaterialReference.ActionKind = "prompt_short:siege";
		return eventMaterialReference;
	}

	private static EventMaterialReference BuildWeeklyPromptShortBattleMaterial(EventMaterialReference source, WeeklyEventMaterialPreviewGroup group)
	{
		if (source == null)
		{
			return null;
		}
		string kingdomName = MyBehavior.ResolveKingdomDisplay(group?.KingdomId);
		int winCount = 0;
		int lossCount = 0;
		int ownDied = 0;
		int ownWounded = 0;
		int enemyDied = 0;
		int enemyWounded = 0;
		foreach (string line in SplitWeeklyPromptShortArmyLines(source.SnapshotText))
		{
			string text = (line ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || text.IndexOf("事件=战场交锋", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			bool flag = WeeklyPromptBattleLineSideContainsKingdom(text, "胜方", kingdomName);
			bool flag2 = WeeklyPromptBattleLineSideContainsKingdom(text, "败方", kingdomName);
			if (!flag && !flag2)
			{
				continue;
			}
			if (flag)
			{
				winCount++;
			}
			if (flag2)
			{
				lossCount++;
			}
			if (TryParseWeeklyPromptShortBattleCasualty(text, flag ? "胜方" : "败方", out var ownKilled, out var ownInjured))
			{
				ownDied += ownKilled;
				ownWounded += ownInjured;
			}
			if (TryParseWeeklyPromptShortBattleCasualty(text, flag ? "败方" : "胜方", out var enemyKilled, out var enemyInjured))
			{
				enemyDied += enemyKilled;
				enemyWounded += enemyInjured;
			}
		}
		if (winCount == 0 && lossCount == 0)
		{
			return null;
		}
		string text2 = "[战场交锋]\n- 本周战场交锋：胜利" + winCount + "场，失败" + lossCount + "场；己方伤亡" + (ownDied + ownWounded) + "人（阵亡" + ownDied + "、负伤" + ownWounded + "）；造成敌方杀伤" + (enemyDied + enemyWounded) + "人（阵亡" + enemyDied + "、负伤" + enemyWounded + "）。";
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_battle";
		eventMaterialReference.Label = "战场交锋";
		eventMaterialReference.SnapshotText = text2;
		eventMaterialReference.ActionKind = "prompt_short:battle";
		eventMaterialReference.KingdomId = (group?.KingdomId ?? eventMaterialReference.KingdomId ?? "").Trim();
		return eventMaterialReference;
	}

	private static void AddWeeklyPromptShortMovementAction(List<string> actions, string text, string needle, string label)
	{
		if (actions == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(needle))
		{
			return;
		}
		if ((text ?? "").IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			AddUniqueId(actions, label);
		}
	}

	private static string ExtractWeeklyPromptShortArmyActor(string line, string marker)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(marker))
		{
			return "";
		}
		string text = (line ?? "").Trim();
		if (text.StartsWith("- ", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(2).Trim();
		}
		int num = text.IndexOf('：');
		if (num >= 0 && num + 1 < text.Length)
		{
			text = text.Substring(num + 1).Trim();
		}
		int num2 = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (num2 < 0)
		{
			return "";
		}
		text = text.Substring(0, num2).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num3 = text.LastIndexOf("家族的", StringComparison.OrdinalIgnoreCase);
		if (num3 >= 0)
		{
			text = text.Substring(num3 + "家族的".Length).Trim();
		}
		int num4 = text.LastIndexOf("所属的", StringComparison.OrdinalIgnoreCase);
		if (num4 >= 0)
		{
			text = text.Substring(num4 + "所属的".Length).Trim();
		}
		int num5 = text.IndexOf('(');
		if (num5 > 0)
		{
			text = text.Substring(0, num5).Trim();
		}
		return text.Trim(' ', '。', '，', ',', ';', '；');
	}

	private static List<string> ExtractWeeklyPromptShortMovementLocations(string text)
	{
		List<string> list = new List<string>();
		foreach (Match item in Regex.Matches(text ?? "", "涉及地点=(?<value>[^|\\r\\n]+)", RegexOptions.IgnoreCase))
		{
			string value = item.Groups["value"]?.Value ?? "";
			foreach (string item2 in value.Split(new char[4] { '、', '，', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				AddUniqueId(list, item2.Trim());
			}
		}
		return list;
	}

	private static string ExtractWeeklyPromptShortSiegeResult(string line)
	{
		string text = ExtractWeeklyPromptShortLineBody(line);
		int num = text.IndexOf("结果为", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		return text.Substring(num + "结果为".Length).Trim(' ', '。', '，', ',', ';', '；');
	}

	private static string ExtractWeeklyPromptShortSiegeTarget(string line, string beforeMarker, string afterMarker)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(beforeMarker) || string.IsNullOrWhiteSpace(afterMarker))
		{
			return "";
		}
		string text = ExtractWeeklyPromptShortLineBody(line);
		int num2 = text.IndexOf(afterMarker, StringComparison.OrdinalIgnoreCase);
		if (num2 < 0)
		{
			return "";
		}
		string text2 = text.Substring(0, num2);
		int num3 = text2.LastIndexOf(beforeMarker, StringComparison.OrdinalIgnoreCase);
		if (num3 >= 0)
		{
			text2 = text2.Substring(num3 + beforeMarker.Length);
		}
		return text2.Trim(' ', '。', '，', ',', ';', '；');
	}

	internal static WeeklyPromptClanChangeParseResult ParseWeeklyPromptClanChangeDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 5)
		{
			return null;
		}
		return new WeeklyPromptClanChangeParseResult
		{
			oldKingdomId = (array[2] ?? "").Trim(),
			newKingdomId = (array[3] ?? "").Trim(),
			detailLabel = TranslateWeeklyPromptClanChangeDetail((array[4] ?? "").Trim())
		};
	}

	private static IEnumerable<string> SplitWeeklyPromptShortArmyLines(string text)
	{
		foreach (string item in (text ?? "").Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			yield return item;
		}
	}

	private static bool TryParseWeeklyPromptPrisonerPair(string stableKey, out string captorHeroId, out string prisonerHeroId)
	{
		captorHeroId = "";
		prisonerHeroId = "";
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 3)
		{
			return false;
		}
		if (!string.Equals((array[0] ?? "").Trim(), "prisoner_released", StringComparison.OrdinalIgnoreCase) && !string.Equals((array[0] ?? "").Trim(), "prisoner_taken", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		captorHeroId = (array[1] ?? "").Trim();
		prisonerHeroId = (array[2] ?? "").Trim();
		return !string.IsNullOrWhiteSpace(captorHeroId) || !string.IsNullOrWhiteSpace(prisonerHeroId);
	}

	private static bool TryParseWeeklyPromptShortBattleCasualty(string line, string sideLabel, out int died, out int wounded)
	{
		died = 0;
		wounded = 0;
		string text = ExtractWeeklyPromptBattlePipeField(line, "伤亡");
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = (sideLabel ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		Match match = Regex.Match(text, Regex.Escape(text2) + "(?<value>[^；|]*)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}
		string value = match.Groups["value"]?.Value ?? "";
		Match match2 = Regex.Match(value, "阵亡(?<died>\\d+)、负伤(?<wounded>\\d+)", RegexOptions.IgnoreCase);
		if (!match2.Success)
		{
			return false;
		}
		int.TryParse(match2.Groups["died"]?.Value ?? "0", out died);
		int.TryParse(match2.Groups["wounded"]?.Value ?? "0", out wounded);
		return true;
	}

	private static bool WeeklyPromptBattleLineSideContainsKingdom(string line, string sideLabel, string kingdomName)
	{
		string text = (kingdomName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(sideLabel) || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = ExtractWeeklyPromptBattlePipeField(line, sideLabel);
		return text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static void AddUniqueId(List<string> list, string id)
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

	internal sealed class WeeklyPromptClanChangeParseResult
	{
		public string oldKingdomId;

		public string newKingdomId;

		public string detailLabel;
	}

	private static string ExtractWeeklyPromptBattlePipeField(string line, string fieldName)
	{
		string text = (line ?? "").Trim();
		string text2 = (fieldName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		foreach (string item in text.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string text3 = item.Trim().TrimStart('-', ' ');
			if (text3.StartsWith(text2 + "=", StringComparison.OrdinalIgnoreCase))
			{
				return text3.Substring(text2.Length + 1).Trim();
			}
			if (text3.StartsWith(text2 + "：", StringComparison.OrdinalIgnoreCase))
			{
				return text3.Substring(text2.Length + 1).Trim();
			}
			if (text3.StartsWith(text2 + ":", StringComparison.OrdinalIgnoreCase))
			{
				return text3.Substring(text2.Length + 1).Trim();
			}
		}
		return "";
	}

	private static string ExtractWeeklyPromptShortLineBody(string line)
	{
		string text = (line ?? "").Trim();
		if (text.StartsWith("- ", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(2).Trim();
		}
		int num = text.IndexOf('：');
		if (num >= 0 && num + 1 < text.Length)
		{
			text = text.Substring(num + 1).Trim();
		}
		return text;
	}

	private static string TranslateWeeklyPromptClanChangeDetail(string detail)
	{
		switch ((detail ?? "").Trim())
		{
		case "JoinAsMercenary":
			return "佣兵加入";
		case "JoinKingdom":
			return "正式加入";
		case "JoinKingdomByDefection":
			return "叛逃改投";
		case "LeaveKingdom":
			return "脱离王国";
		case "LeaveWithRebellion":
			return "脱离并叛乱";
		case "LeaveAsMercenary":
			return "结束佣兵服务";
		case "CreateKingdom":
			return "建立新王国";
		case "LeaveByKingdomDestruction":
			return "原王国覆灭";
		case "LeaveByClanDestruction":
			return "家族覆灭";
		default:
			return (detail ?? "").Trim();
		}
	}

	internal static EventMaterialReference BuildWeeklyPromptShortSettlementStatsMaterial(List<EventMaterialReference> source, WeeklyEventMaterialPreviewGroup group)
	{
		List<EventMaterialReference> list = (source ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HashSet<string>> reasonTagsBySettlement = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list)
		{
			string text = ResolveWeeklyPromptSettlementStatsKey(item);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "settlement_stats_" + hashSet.Count;
			}
			hashSet.Add(text);
			foreach (Match item2 in Regex.Matches(item.SnapshotText ?? "", "(繁荣|忠诚度|忠诚|治安|粮食|民兵|驻军)(?:小幅|明显)?(扩张|回落|改善|走低|恶化|恢复|吃紧|增加|减少|上升|下降)", RegexOptions.IgnoreCase))
			{
				string text2 = NormalizeWeeklyPromptSettlementStatsLabel(item2.Groups[1]?.Value);
				string text3 = (item2.Groups[2]?.Value ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				string text4 = NormalizeWeeklyPromptSettlementStatsTrend(text2, text3);
				if (string.IsNullOrWhiteSpace(text4))
				{
					continue;
				}
				if (!dictionary.TryGetValue(text4, out var value))
				{
					value = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					dictionary[text4] = value;
				}
				value.Add(text);
			}
			foreach (string item3 in ExtractWeeklyPromptSettlementStatsReasonTags(item.SnapshotText ?? ""))
			{
				if (!reasonTagsBySettlement.TryGetValue(item3, out var value2))
				{
					value2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					reasonTagsBySettlement[item3] = value2;
				}
				value2.Add(text);
			}
		}
		List<string> list2 = new List<string>();
		list2.Add(Math.Max(hashSet.Count, list.Count) > 2 ? "多处据点出现治理波动" : "个别据点出现治理波动");
		List<string> trends = GetWeeklyPromptSettlementStatsOrder().Where((string orderKey) => dictionary.TryGetValue(orderKey, out var value3) && value3.Count > 0).Take(6).ToList();
		if (trends.Count > 0)
		{
			list2.Add("主要表现为" + string.Join("、", trends));
		}
		List<string> list3 = reasonTagsBySettlement.OrderByDescending((KeyValuePair<string, HashSet<string>> x) => x.Value.Count).ThenBy((KeyValuePair<string, HashSet<string>> x) => x.Key, StringComparer.OrdinalIgnoreCase).Select((KeyValuePair<string, HashSet<string>> x) => x.Key).Take(5).ToList();
		if (list3.Count > 0)
		{
			list2.Add("常见变化原因：" + string.Join("、", list3));
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(list[0]);
		eventMaterialReference.MaterialType = "prompt_short_settlement_stats";
		eventMaterialReference.Label = "定居点状态";
		eventMaterialReference.SnapshotText = "[定居点状态]\n- " + string.Join("；", list2) + "。";
		eventMaterialReference.KingdomId = (group?.KingdomId ?? eventMaterialReference.KingdomId ?? "").Trim();
		eventMaterialReference.ActionKind = "prompt_short:settlement_stats";
		eventMaterialReference.SourceMaterialCount = list.Count;
		eventMaterialReference.ActionDay = list.Min((EventMaterialReference x) => x?.ActionDay ?? int.MaxValue);
		eventMaterialReference.ActionSequence = list.Min((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue);
		foreach (EventMaterialReference additionalSource in list.Skip(1))
		{
			WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(additionalSource, eventMaterialReference);
		}
		return eventMaterialReference;
	}

	internal static EventMaterialReference BuildWeeklyPromptShortVillageRaidMaterial(List<EventMaterialReference> source, WeeklyEventMaterialPreviewGroup group)
	{
		List<EventMaterialReference> list = (source ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		string text = (group?.KingdomId ?? "").Trim();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list)
		{
			string text2 = (item.ActionStableKey ?? "").Trim();
			if (!text2.StartsWith("raid_completed:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string text3 = ResolveVillageRaidKey(item);
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = text2;
			}
			string text4 = ResolveVillageRaidOutcome(item);
			if (string.IsNullOrWhiteSpace(text4))
			{
				continue;
			}
			bool flag = !string.IsNullOrWhiteSpace(text) && string.Equals((item.KingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase);
			bool flag2 = !string.IsNullOrWhiteSpace(text) && string.Equals((item.ActorKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase);
			if (flag && string.Equals(text4, "success", StringComparison.OrdinalIgnoreCase) && hashSet.Add("victim_success:" + text3))
			{
				num++;
			}
			if (flag2 && hashSet.Add("attacker_" + text4 + ":" + text3))
			{
				switch (text4)
				{
				case "success":
					num2++;
					break;
				case "defended":
					num3++;
					break;
				case "aborted":
					num4++;
					break;
				}
			}
		}
		if (num == 0 && num2 == 0 && num3 == 0 && num4 == 0)
		{
			return null;
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(list[0]);
		eventMaterialReference.MaterialType = "prompt_short_village_raid";
		eventMaterialReference.Label = "村庄掠夺";
		eventMaterialReference.SnapshotText = "[村庄掠夺]\n- 我方被掠夺成功：" + num + "处；我方掠夺成功：" + num2 + "处；我方掠夺被击退：" + num3 + "处；我方掠夺中止：" + num4 + "处。";
		eventMaterialReference.KingdomId = text;
		eventMaterialReference.ActionKind = "prompt_short:village_raid";
		eventMaterialReference.SourceMaterialCount = list.Count;
		eventMaterialReference.ActionDay = list.Min((EventMaterialReference x) => x?.ActionDay ?? int.MaxValue);
		eventMaterialReference.ActionSequence = list.Min((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue);
		foreach (EventMaterialReference item2 in list.Skip(1))
		{
			WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(item2, eventMaterialReference);
		}
		return eventMaterialReference;
	}

	private static List<string> ExtractWeeklyPromptSettlementStatsReasonTags(string snapshotText)
	{
		string text = NormalizePoliticalReasonText(snapshotText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new List<string>();
		}
		Match match = Regex.Match(text, "变化原因：(?<reason>.*?)(?:。|$)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return new List<string>();
		}
		string reasonText = match.Groups["reason"]?.Value ?? "";
		List<string> tags = new List<string>();
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "粮食供需", "粮食", "Food", "食物", "饥", "Starv", "Surplus", "Food Stores", "Food Shortage");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "驻军与军费", "驻军", "Garrison", "军费", "工资", "wage", "Morale", "Payment", "Unpaid");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "村庄状态", "村庄", "Village", "Raided", "Looted", "劫掠", "袭扰", "Hearth");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "围城压力", "围城", "Siege", "Under Siege");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "治安与匪患", "治安", "Security", "Hideout", "藏身处", "Bandit", "Patrol", "Guard", "Corruption", "腐败");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "忠诚与文化", "忠诚", "Loyalty", "文化", "Culture", "Governor", "总督", "王国稳定度");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "政策与建设", "政策", "Policy", "Rights", "建筑", "Building", "Project", "工程", "Aqueduct", "Fairground", "Irrigation");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, "市场与繁荣", "市场", "Market", "Goods", "繁荣", "Prosperity", "Housing");
		return tags;
	}

	private static List<string> GetWeeklyPromptSettlementStatsOrder()
	{
		return new List<string>
		{
			"繁荣扩张",
			"繁荣回落",
			"粮食恢复",
			"粮食吃紧",
			"忠诚改善",
			"忠诚走低",
			"治安改善",
			"治安恶化",
			"民兵增加",
			"民兵减少",
			"驻军增加",
			"驻军减少"
		};
	}

	private static string NormalizeWeeklyPromptSettlementStatsLabel(string label)
	{
		switch ((label ?? "").Trim())
		{
		case "忠诚度":
			return "忠诚";
		default:
			return (label ?? "").Trim();
		}
	}

	private static string NormalizeWeeklyPromptSettlementStatsTrend(string label, string direction)
	{
		string text = NormalizeWeeklyPromptSettlementStatsLabel(label);
		string text2 = (direction ?? "").Trim();
		bool rise = text2 == "上升" || text2 == "扩张" || text2 == "改善" || text2 == "恢复" || text2 == "增加";
		bool fall = text2 == "下降" || text2 == "回落" || text2 == "走低" || text2 == "恶化" || text2 == "吃紧" || text2 == "减少";
		if (!rise && !fall)
		{
			return "";
		}
		switch (text)
		{
		case "繁荣":
			return rise ? "繁荣扩张" : "繁荣回落";
		case "粮食":
			return rise ? "粮食恢复" : "粮食吃紧";
		case "忠诚":
			return rise ? "忠诚改善" : "忠诚走低";
		case "治安":
			return rise ? "治安改善" : "治安恶化";
		case "民兵":
			return rise ? "民兵增加" : "民兵减少";
		case "驻军":
			return rise ? "驻军增加" : "驻军减少";
		default:
			return text + (rise ? "改善" : "走低");
		}
	}

	private static string ResolveWeeklyPromptSettlementStatsKey(EventMaterialReference material)
	{
		string text = (material?.SettlementId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (material?.ActionStableKey ?? "").Trim();
		if (text2.StartsWith("settlement_stats:", StringComparison.OrdinalIgnoreCase))
		{
			string[] array = text2.Split(new char[1] { ':' }, StringSplitOptions.None);
			if (array.Length >= 2)
			{
				return (array[1] ?? "").Trim();
			}
		}
		string text3 = (material?.SnapshotText ?? "").Trim();
		int num = text3.IndexOf("本周治理状态发生波动", StringComparison.OrdinalIgnoreCase);
		if (num <= 0)
		{
			num = text3.IndexOf("本周出现定居点状态波动", StringComparison.OrdinalIgnoreCase);
		}
		if (num > 0)
		{
			return text3.Substring(0, num).Trim();
		}
		return "";
	}

	private static void AddWeeklyPromptSettlementReasonTag(List<string> tags, string text, string tag, params string[] keywords)
	{
		if (tags == null || string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(tag) || tags.Any((string x) => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		foreach (string keyword in keywords ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(keyword) && text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				tags.Add(tag);
				return;
			}
		}
	}

	private static string NormalizePoliticalReasonText(string text)
	{
		return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}

}
