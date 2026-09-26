using System;
using System.Collections.Generic;
using System.Linq;

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
			if (!MyBehavior.IsWeeklyPromptAggregatableMaterial(material))
			{
				preserved.Add(material);
				continue;
			}
			string category = MyBehavior.ResolveWeeklyPromptAggregateCategory(material);
			string eventKey = MyBehavior.BuildWeeklyPromptAggregateEventKey(material);
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
		foreach (string category in MyBehavior.GetWeeklyPromptAggregateCategoryOrder())
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
}
