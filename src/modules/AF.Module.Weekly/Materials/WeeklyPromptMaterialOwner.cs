using System;
using System.Collections.Generic;
using System.Linq;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;
using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

internal static class WeeklyPromptMaterialOwner
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
			string key = ResolveWeeklyPromptVillageRaidKey(item);
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
			.ThenBy(x => ResolveWeeklyPromptVillageRaidKey(x[0]), StringComparer.OrdinalIgnoreCase))
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
}
