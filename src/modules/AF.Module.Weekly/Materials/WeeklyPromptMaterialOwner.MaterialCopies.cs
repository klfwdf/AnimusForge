using System;using System.Collections.Generic;using System.Linq;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal static partial class WeeklyPromptMaterialOwner {
internal static EventMaterialReference CloneEventMaterialReference(EventMaterialReference material)
	{
		if (material == null)
		{
			return null;
		}
		return new EventMaterialReference
		{
			MaterialType = (material.MaterialType ?? "").Trim(),
			Label = (material.Label ?? "").Trim(),
			SnapshotText = (material.SnapshotText ?? "").Trim(),
			HeroId = (material.HeroId ?? "").Trim(),
			KingdomId = (material.KingdomId ?? "").Trim(),
			SettlementId = (material.SettlementId ?? "").Trim(),
			RecentOnly = material.RecentOnly,
			ActionKind = (material.ActionKind ?? "").Trim(),
			ActorHeroId = (material.ActorHeroId ?? "").Trim(),
			ActorClanId = (material.ActorClanId ?? "").Trim(),
			ActorKingdomId = (material.ActorKingdomId ?? "").Trim(),
			TargetHeroId = (material.TargetHeroId ?? "").Trim(),
			TargetClanId = (material.TargetClanId ?? "").Trim(),
			TargetKingdomId = (material.TargetKingdomId ?? "").Trim(),
			SettlementOwnerHeroId = (material.SettlementOwnerHeroId ?? "").Trim(),
			SettlementOwnerClanId = (material.SettlementOwnerClanId ?? "").Trim(),
			SettlementOwnerKingdomId = (material.SettlementOwnerKingdomId ?? "").Trim(),
			PreviousSettlementOwnerHeroId = (material.PreviousSettlementOwnerHeroId ?? "").Trim(),
			PreviousSettlementOwnerClanId = (material.PreviousSettlementOwnerClanId ?? "").Trim(),
			PreviousSettlementOwnerKingdomId = (material.PreviousSettlementOwnerKingdomId ?? "").Trim(),
			LocationText = (material.LocationText ?? "").Trim(),
			Won = material.Won,
			RelatedHeroIds = new List<string>((material.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedClanIds = new List<string>((material.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedKingdomIds = new List<string>((material.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceStableKeys = new List<string>((material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceActionKinds = new List<string>((material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceMaterialCount = Math.Max(0, material.SourceMaterialCount),
			ActionStableKey = (material.ActionStableKey ?? "").Trim(),
			ActionDay = material.ActionDay,
			ActionOrder = material.ActionOrder,
			ActionSequence = material.ActionSequence
		};
	}
internal static IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterials(List<EventMaterialReference> materials)
	{
		return (materials ?? new List<EventMaterialReference>()).OrderBy((EventMaterialReference x) => GetWeeklyPreviewMaterialSortBucket(x)).ThenBy((EventMaterialReference x) => x?.ActionDay ?? int.MinValue).ThenBy((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue).ThenBy((EventMaterialReference x) => x?.ActionOrder ?? int.MinValue).ThenBy((EventMaterialReference x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase);
	}
internal static int GetWeeklyPreviewMaterialSortBucket(EventMaterialReference material)
	{
		string text = (material?.MaterialType ?? "").Trim().ToLowerInvariant();
		if (text == "world_opening_summary" || text == "kingdom_opening_summary")
		{
			return 0;
		}
		if (text == "kingdom_current_ruler")
		{
			return 1;
		}
		return 2;
	}
}
