using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class WeeklyMaterialBatchPlanner
{
	internal static HashSet<string> SelectFullReportKingdomIds(
		List<MyBehavior.WeeklyEventMaterialPreviewGroup> groups, IEnumerable<string> nearbyKingdomIds)
	{
		List<string> selected = (nearbyKingdomIds ?? Enumerable.Empty<string>())
			.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
		if (selected.Count == 0)
		{
			selected = (groups ?? new List<MyBehavior.WeeklyEventMaterialPreviewGroup>())
				.Where(group => group != null && string.Equals((group.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
				.Select(group => (group.KingdomId ?? "").Trim()).Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
		}
		return new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
	}

	internal static bool IsShortOnly(MyBehavior.WeeklyEventMaterialPreviewGroup group, HashSet<string> fullReportKingdomIds)
	{
		return group != null && string.Equals((group.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)
			&& !(fullReportKingdomIds?.Contains((group.KingdomId ?? "").Trim()) ?? false);
	}

	internal static List<MyBehavior.WeeklyEventMaterialPreviewGroup> OrderGroups(
		List<MyBehavior.WeeklyEventMaterialPreviewGroup> groups, List<string> nearbyKingdomIds)
	{
		Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < (nearbyKingdomIds?.Count ?? 0); i++)
		{
			string id = (nearbyKingdomIds[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id) && !ranks.ContainsKey(id))
			{
				ranks[id] = i;
			}
		}
		return (groups ?? new List<MyBehavior.WeeklyEventMaterialPreviewGroup>())
			.OrderBy(group =>
			{
				if (string.Equals((group.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
				{
					string id = (group.KingdomId ?? "").Trim();
					return !string.IsNullOrWhiteSpace(id) && ranks.TryGetValue(id, out int rank)
						? (rank == 0 ? 0 : rank + 1) : 1000;
				}
				return string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? 1 : 2000;
			})
			.ThenBy(group => group.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	internal static List<MyBehavior.WeeklyReportBatchRequest> BuildBatches(
		List<MyBehavior.WeeklyEventMaterialPreviewGroup> eligibleGroups,
		int weekIndex, int startDay, int endDay, int batchSize)
	{
		List<MyBehavior.WeeklyReportBatchRequest> batches = new List<MyBehavior.WeeklyReportBatchRequest>();
		if (eligibleGroups == null || eligibleGroups.Count == 0)
		{
			return batches;
		}
		MyBehavior.WeeklyEventMaterialPreviewGroup world = eligibleGroups.FirstOrDefault(
			group => string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase));
		if (world != null)
		{
			batches.Add(new MyBehavior.WeeklyReportBatchRequest
			{
				WeekIndex = weekIndex, StartDay = startDay, EndDay = endDay,
				OutputMode = MyBehavior.WeeklyReportOutputMode.FullReport,
				Groups = new List<MyBehavior.WeeklyEventMaterialPreviewGroup> { world }
			});
		}
		List<MyBehavior.WeeklyEventMaterialPreviewGroup> other = eligibleGroups.Where(
			group => !string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase)).ToList();
		int size = Math.Max(1, batchSize);
		AppendMode(other, MyBehavior.WeeklyReportOutputMode.FullReport, size, weekIndex, startDay, endDay, batches);
		AppendMode(other, MyBehavior.WeeklyReportOutputMode.TitleShortTagsOnly, size, weekIndex, startDay, endDay, batches);
		return batches;
	}

	private static void AppendMode(List<MyBehavior.WeeklyEventMaterialPreviewGroup> groups,
		MyBehavior.WeeklyReportOutputMode mode, int batchSize, int weekIndex, int startDay, int endDay,
		List<MyBehavior.WeeklyReportBatchRequest> batches)
	{
		List<MyBehavior.WeeklyEventMaterialPreviewGroup> selected = groups.Where(group =>
			(mode == MyBehavior.WeeklyReportOutputMode.TitleShortTagsOnly)
				== (group.OutputMode == MyBehavior.WeeklyReportOutputMode.TitleShortTagsOnly)).ToList();
		for (int i = 0; i < selected.Count; i += batchSize)
		{
			batches.Add(new MyBehavior.WeeklyReportBatchRequest
			{
				WeekIndex = weekIndex, StartDay = startDay, EndDay = endDay,
				OutputMode = mode,
				Groups = selected.GetRange(i, Math.Min(batchSize, selected.Count - i))
			});
		}
	}
}
