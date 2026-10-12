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
		foreach (EventMaterialReference material in group.PromptMaterials)
		{
			if (material == null || !PolicyWeeklyFactPromptProjection.IsPolicyKind(material.MaterialType)) continue;
			material.Label = PolicyWeeklyFactPromptProjection.Label(material.MaterialType, material.Label);
			material.SnapshotText = PolicyWeeklyFactPromptProjection.Snapshot(material.MaterialType, material.SnapshotText);
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
			text2 = string.IsNullOrWhiteSpace(text) ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0489") : text;
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
			list3.Add(text2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0490"));
		}
		else if (flag)
		{
			list3.Add(text2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0491"));
		}
		eventMaterialReference.MaterialType = "prompt_resolved_village_raid";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0492") + text2;
		eventMaterialReference.SnapshotText = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0493") + string.Join("\n- ", list3);
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
		return text.StartsWith("village_raid_started:", StringComparison.OrdinalIgnoreCase) || (material?.Label ?? "").Trim().StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0494"), StringComparison.OrdinalIgnoreCase);
	}

	internal static bool IsWeeklyPromptVillageRaidCompletionMaterial(EventMaterialReference material)
	{
		string text = (material?.ActionStableKey ?? "").Trim();
		return text.StartsWith("raid_completed:", StringComparison.OrdinalIgnoreCase) || (material?.Label ?? "").Trim().StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0495"), StringComparison.OrdinalIgnoreCase);
	}

	internal static string GetWeeklyPromptVillageRaidOutcomeMaterialText(string outcome)
	{
		switch ((outcome ?? "").Trim())
		{
		case "success":
			return WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0496");
		case "defended":
			return WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0497");
		case "aborted":
			return WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0498");
		default:
			return WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0499");
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
		if (text2.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0500"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "success";
		}
		if (text2.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0501"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "defended";
		}
		if (text2.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0502"), StringComparison.OrdinalIgnoreCase) >= 0)
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
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("village_raid_started:", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("raid_completed:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0503"), StringComparison.OrdinalIgnoreCase) || text3.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0504"), StringComparison.OrdinalIgnoreCase));
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
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("settlement_stats:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0505"), StringComparison.OrdinalIgnoreCase));
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
		return string.Equals(text, "raw_text", StringComparison.OrdinalIgnoreCase) && (text2.StartsWith("tournament_finished:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0506"), StringComparison.OrdinalIgnoreCase));
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
		return string.Equals(text, "prompt_agg_movement", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:movement", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0507"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptCaptivityAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_captivity", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:captivity", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0508"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptReleaseAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_release", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:release", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0509"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptClanChangeAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_strategic_shift", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:strategic_shift", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0510"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptArmyAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_army", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:army", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0511"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptSiegeAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_siege", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:siege", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0512"), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWeeklyPromptBattleAggregateMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		string text2 = (material.ActionKind ?? "").Trim();
		return string.Equals(text, "prompt_agg_battle", StringComparison.OrdinalIgnoreCase) || string.Equals(text2, "prompt_aggregate:battle", StringComparison.OrdinalIgnoreCase) || string.Equals((material.Label ?? "").Trim(), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0513"), StringComparison.OrdinalIgnoreCase);
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
			num = Regex.Matches(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0514"), RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		List<string> list = ExtractWeeklyPromptShortMovementLocations(text).Take(5).ToList();
		List<string> list2 = new List<string>();
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0515"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0516"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0517"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0518"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0519"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0520"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0521"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0522"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0523"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0524"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0525"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0526"));
		AddWeeklyPromptShortMovementAction(list2, text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0527"), WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0528"));
		if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0529"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0530"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0531"), StringComparison.OrdinalIgnoreCase) >= 0)
		{
			AddUniqueId(list2, WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0532"));
		}
		if (list2.Count == 0)
		{
			list2.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0533"));
		}
		string text2 = (list.Count > 0) ? string.Join("、", list) : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0534");
		string text3 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0535") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0536") + text2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0537") + string.Join("、", list2) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_movement";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0538");
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
			num = Regex.Matches(source.SnapshotText ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0539"), RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		string text5 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0540") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0541");
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 += WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0542") + num2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0543") + num3 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0544");
		}
		text5 += WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_captivity";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0545");
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
			num = Regex.Matches(source.SnapshotText ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0546"), RegexOptions.IgnoreCase).Count;
		}
		if (num <= 0)
		{
			num = 1;
		}
		string text5 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0547") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0548");
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 += WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0549") + num2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0550") + num3 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0551");
		}
		text5 += WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_release";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0552");
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
			if (string.Equals(text4, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0553"), StringComparison.OrdinalIgnoreCase))
			{
				AddUniqueId(list, string.IsNullOrWhiteSpace(text2) ? (text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0554")) : (text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0555") + text2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0556")));
			}
			else if (string.Equals(text4, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0557"), StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0558") + text2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0559") + text3);
				}
				else if (!string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0560") + text3);
				}
			}
			else if (string.Equals(text4, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0561"), StringComparison.OrdinalIgnoreCase))
			{
				AddUniqueId(list, string.IsNullOrWhiteSpace(text3) ? (text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0562")) : (text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0563") + text3));
			}
			else if (!string.IsNullOrWhiteSpace(text3))
			{
				AddUniqueId(list, text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0564") + text3);
			}
			else if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text4))
			{
				AddUniqueId(list, text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_WordSeparator") + WeeklyMaterialTexts.ProjectRaw(text4) + "：" + text2);
			}
		}
		string text5;
		if (list.Count > 0)
		{
			bool flag = list.Any((string x) => (x ?? "").IndexOf(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0554"), StringComparison.OrdinalIgnoreCase) >= 0);
			text5 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0566") + (flag ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0567") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0568")) + "：" + string.Join("；", list.Take(6)) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
		}
		else
		{
			int num = Math.Max(1, Math.Max(0, source.SourceMaterialCount));
			text5 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0569") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0570");
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_strategic_shift";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0571");
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
			string actor = ExtractWeeklyPromptShortArmyActor(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0572"));
			if (string.IsNullOrWhiteSpace(actor))
			{
				actor = ExtractWeeklyPromptShortArmyActor(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0573"));
			}
			if (!string.IsNullOrWhiteSpace(actor))
			{
				AddUniqueId(list, actor);
				continue;
			}
			actor = ExtractWeeklyPromptShortArmyActor(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0574"));
			if (string.IsNullOrWhiteSpace(actor))
			{
				actor = ExtractWeeklyPromptShortArmyActor(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0575"));
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
				list3.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0576") + string.Join("、", list.Take(8)));
			}
			if (list2.Count > 0)
			{
				list3.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0577") + string.Join("、", list2.Take(8)));
			}
			text2 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0578") + string.Join("；", list3) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
		}
		else
		{
			int num = Math.Max(1, Math.Max(0, source.SourceMaterialCount));
			text2 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0579") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0580");
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_army";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0581");
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
			if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0582"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0583"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0584"), StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0585"), StringComparison.OrdinalIgnoreCase) >= 0)
			{
				continue;
			}
			if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0586"), StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text2 = ExtractWeeklyPromptShortSiegeTarget(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0587"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0588"));
				if (!string.IsNullOrWhiteSpace(text2))
				{
					AddUniqueId(list, WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0589") + text2);
				}
				continue;
			}
			if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0590"), StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text3 = ExtractWeeklyPromptShortSiegeTarget(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0591"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0592"));
				if (!string.IsNullOrWhiteSpace(text3))
				{
					AddUniqueId(list, WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0593") + text3);
				}
				continue;
			}
			if (text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0594"), StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text4 = ExtractWeeklyPromptShortSiegeTarget(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0595"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0596"));
				string text5 = ExtractWeeklyPromptShortSiegeResult(text);
				if (!string.IsNullOrWhiteSpace(text4) || !string.IsNullOrWhiteSpace(text5))
				{
					AddUniqueId(list2, (string.IsNullOrWhiteSpace(text4) ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0597") : text4) + (string.IsNullOrWhiteSpace(text5) ? "" : ("：" + text5)));
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
			list3.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0598") + string.Join("、", list.Take(6)));
		}
		if (list2.Count > 0)
		{
			list3.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0599") + string.Join("；", list2.Take(6)));
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_siege";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0600");
		eventMaterialReference.SnapshotText = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0601") + string.Join("；", list3) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
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
			if (string.IsNullOrWhiteSpace(text) || text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0602"), StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			bool flag = WeeklyPromptBattleLineSideContainsKingdom(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0603"), kingdomName);
			bool flag2 = WeeklyPromptBattleLineSideContainsKingdom(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0604"), kingdomName);
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
			if (TryParseWeeklyPromptShortBattleCasualty(text, flag ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0605") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0606"), out var ownKilled, out var ownInjured))
			{
				ownDied += ownKilled;
				ownWounded += ownInjured;
			}
			if (TryParseWeeklyPromptShortBattleCasualty(text, flag ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0607") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0608"), out var enemyKilled, out var enemyInjured))
			{
				enemyDied += enemyKilled;
				enemyWounded += enemyInjured;
			}
		}
		if (winCount == 0 && lossCount == 0)
		{
			return null;
		}
		string text2 = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0609") + winCount + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0610") + lossCount + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0611") + (ownDied + ownWounded) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0612") + ownDied + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0613") + ownWounded + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0614") + (enemyDied + enemyWounded) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0615") + enemyDied + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0616") + enemyWounded + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_ParenthesisSentenceEnd");
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(source);
		eventMaterialReference.MaterialType = "prompt_short_battle";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0617");
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
		int num3 = text.LastIndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0618"), StringComparison.OrdinalIgnoreCase);
		if (num3 >= 0)
		{
			text = text.Substring(num3 + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0619").Length).Trim();
		}
		int num4 = text.LastIndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0620"), StringComparison.OrdinalIgnoreCase);
		if (num4 >= 0)
		{
			text = text.Substring(num4 + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0621").Length).Trim();
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
		foreach (Match item in Regex.Matches(text ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0622"), RegexOptions.IgnoreCase))
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
		int num = text.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0623"), StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string result = text.Substring(num + WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0624").Length).Trim(' ', '。', '，', ',', ';', '；');
		// Aggregate records retain their original labels; project only the derived prompt.
		return string.Join(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_ListSeparator"),
			result.Split('、').Select(WeeklyMaterialTexts.ProjectRaw));
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
		string text = ExtractWeeklyPromptBattlePipeField(line, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0625"));
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
		Match match2 = Regex.Match(value, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0626"), RegexOptions.IgnoreCase);
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
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0627");
		case "JoinKingdom":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0628");
		case "JoinKingdomByDefection":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0629");
		case "LeaveKingdom":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0630");
		case "LeaveWithRebellion":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0631");
		case "LeaveAsMercenary":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0632");
		case "CreateKingdom":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0633");
		case "LeaveByKingdomDestruction":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0634");
		case "LeaveByClanDestruction":
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0635");
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
			foreach (Match item2 in Regex.Matches(item.SnapshotText ?? "", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0636"), RegexOptions.IgnoreCase))
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
		list2.Add(Math.Max(hashSet.Count, list.Count) > 2 ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0637") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0638"));
		List<string> trends = GetWeeklyPromptSettlementStatsOrder().Where((string orderKey) => dictionary.TryGetValue(orderKey, out var value3) && value3.Count > 0).Take(6).ToList();
		if (trends.Count > 0)
		{
			list2.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0639") + string.Join("、", trends.Select(WeeklyMaterialTexts.ProjectRaw)));
		}
		List<string> list3 = reasonTagsBySettlement.OrderByDescending((KeyValuePair<string, HashSet<string>> x) => x.Value.Count).ThenBy((KeyValuePair<string, HashSet<string>> x) => x.Key, StringComparer.OrdinalIgnoreCase).Select((KeyValuePair<string, HashSet<string>> x) => x.Key).Take(5).ToList();
		if (list3.Count > 0)
		{
			list2.Add(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0640") + string.Join("、", list3.Select(WeeklyMaterialTexts.ProjectRaw)));
		}
		EventMaterialReference eventMaterialReference = CloneEventMaterialReference(list[0]);
		eventMaterialReference.MaterialType = "prompt_short_settlement_stats";
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0641");
		eventMaterialReference.SnapshotText = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0642") + string.Join("；", list2) + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_SentenceEnd");
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
		eventMaterialReference.Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0643");
		eventMaterialReference.SnapshotText = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0644") + num + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0645") + num2 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0646") + num3 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0647") + num4 + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Material_0648");
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
		Match match = Regex.Match(text, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0649"), RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return new List<string>();
		}
		string reasonText = match.Groups["reason"]?.Value ?? "";
		List<string> tags = new List<string>();
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0650"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0651"), "Food", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0652"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0653"), "Starv", "Surplus", "Food Stores", "Food Shortage");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0654"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0655"), "Garrison", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0656"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0657"), "wage", "Morale", "Payment", "Unpaid");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0658"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0659"), "Village", "Raided", "Looted", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0660"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0661"), "Hearth");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0662"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0663"), "Siege", "Under Siege");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0664"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0665"), "Security", "Hideout", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0666"), "Bandit", "Patrol", "Guard", "Corruption", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0667"));
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0668"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0669"), "Loyalty", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0670"), "Culture", "Governor", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0671"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0672"));
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0673"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0674"), "Policy", "Rights", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0675"), "Building", "Project", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0676"), "Aqueduct", "Fairground", "Irrigation");
		AddWeeklyPromptSettlementReasonTag(tags, reasonText, WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0677"), WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0678"), "Market", "Goods", WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0679"), "Prosperity", "Housing");
		return tags;
	}

	private static List<string> GetWeeklyPromptSettlementStatsOrder()
	{
		return new List<string>
		{
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0680"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0681"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0682"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0683"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0684"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0685"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0686"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0687"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0688"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0689"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0690"),
			WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0691")
		};
	}

	private static string NormalizeWeeklyPromptSettlementStatsLabel(string label)
	{
		switch ((label ?? "").Trim())
		{
		case var _ when ((label ?? "").Trim()) == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0692"):
			return WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0693");
		default:
			return (label ?? "").Trim();
		}
	}

	private static string NormalizeWeeklyPromptSettlementStatsTrend(string label, string direction)
	{
		string text = NormalizeWeeklyPromptSettlementStatsLabel(label);
		string text2 = (direction ?? "").Trim();
		bool rise = text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0694") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0695") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0696") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0697") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0698");
		bool fall = text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0699") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0700") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0701") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0702") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0703") || text2 == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0704");
		if (!rise && !fall)
		{
			return "";
		}
		switch (text)
		{
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0705"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0706") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0707");
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0708"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0709") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0710");
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0711"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0712") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0713");
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0714"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0715") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0716");
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0717"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0718") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0719");
		case var _ when text == WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0720"):
			return rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0721") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0722");
		default:
			return text + (rise ? WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0723") : WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0724"));
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
		int num = text3.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0725"), StringComparison.OrdinalIgnoreCase);
		if (num <= 0)
		{
			num = text3.IndexOf(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0726"), StringComparison.OrdinalIgnoreCase);
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
