using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks++;
    }

    private static void Main()
    {
        var owner = typeof(WeeklyMaterialAggregationOwner);
        Check(owner != null, "owner loaded");
        Check(!WeeklyMaterialAggregationOwner.IsWeeklyPromptAggregatableMaterial(null), "null excluded");
        Check(WeeklyMaterialAggregationOwner.IsWeeklyPromptAggregatableMaterial(new MyBehavior.EventMaterialReference { MaterialType = "NPC_RECENT_ACTION" }), "recent accepted");
        Check(WeeklyMaterialAggregationOwner.IsWeeklyPromptAggregatableMaterial(new MyBehavior.EventMaterialReference { MaterialType = "npc_major_action" }), "major accepted");
        Check(!WeeklyMaterialAggregationOwner.IsWeeklyPromptAggregatableMaterial(new MyBehavior.EventMaterialReference { MaterialType = "raw_text" }), "raw preserved");
        Check(WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(new MyBehavior.EventMaterialReference { ActionKind = "clan_defected" }) == "strategic_shift", "strategic category");
        Check(WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(new MyBehavior.EventMaterialReference { ActionKind = "map_event_aftermath" }) == "battle", "battle category");
        Check(WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(new MyBehavior.EventMaterialReference { ActionKind = "unknown" }) == "other", "unknown category");
        Check(WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryOrder().First() == "strategic_shift", "priority first");
        Check(WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryOrder().Last() == "other", "priority last");
        Check(WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryLabel("death") == "其他事件", "death label preserves legacy default");
        Check(WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryLabel("siege") == "围城与守城", "siege label");

        var first = new MyBehavior.EventMaterialReference { MaterialType = "npc_recent_action", ActionKind = "prisoner_taken_captor", ActionStableKey = "capture:one:captor", ActionDay = 4, ActionSequence = 2 };
        var second = new MyBehavior.EventMaterialReference { MaterialType = "npc_major_action", ActionKind = "prisoner_taken_prisoner", ActionStableKey = "capture:one:prisoner", ActionDay = 4, ActionSequence = 3 };
        Check(WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(first) == "capture:one", "captor suffix normalized");
        Check(WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(second) == "capture:one", "prisoner suffix normalized");
        var battle = new MyBehavior.EventMaterialReference { MaterialType = "npc_major_action", ActionKind = "map_event_aftermath", ActionStableKey = "mapevent_aftermath:77:side:enemy" };
        Check(WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(battle) == "mapevent:77", "battle sides coalesce");

        var group = new MyBehavior.WeeklyEventMaterialPreviewGroup { Materials = new List<MyBehavior.EventMaterialReference> { first, second, new() { MaterialType = "raw_text", Label = "preserved" } } };
        int callbacks = 0;
        WeeklyMaterialAggregationOwner.Apply(group, (g, category, buckets) =>
        {
            callbacks++;
            Check(category == "captivity", "render category");
            Check(buckets.Count == 1 && buckets.Single().Value.Count == 2, "single event bucket preserves both sources");
            return new MyBehavior.EventMaterialReference { MaterialType = "prompt_agg_captivity", SourceMaterialCount = 2 };
        });
        Check(callbacks == 1, "render once");
        Check(group.Materials.Count == 2 && group.Materials.Any(x => x.Label == "preserved"), "non-aggregate material preserved");
        Check(group.Materials.Single(x => x.MaterialType == "prompt_agg_captivity").SourceMaterialCount == 2, "source count preserved");
        first.ActorHeroId = "hero_a";
        second.TargetHeroId = "hero_b";
        var categoryMaterial = WeeklyMaterialAggregationOwner.BuildCategoryMaterial(
            group, "captivity", new Dictionary<string, List<MyBehavior.EventMaterialReference>>
            {
                ["capture:one"] = new() { first, second }
            }, "人物被俘", _ => "俘虏事件");
        Check(categoryMaterial.MaterialType == "prompt_agg_captivity", "category material type");
        Check(categoryMaterial.SnapshotText.Replace("\r\n", "\n") == "[人物被俘]\n- 俘虏事件", "category text");
        Check(categoryMaterial.SourceMaterialCount == 2, "category source count");
        Check(categoryMaterial.RelatedHeroIds.SequenceEqual(new[] { "hero_a", "hero_b" }), "category related heroes");
        Check(categoryMaterial.SourceStableKeys.Contains("capture:one:captor") && categoryMaterial.SourceStableKeys.Contains("capture:one:prisoner"), "category stable keys");
        var raidSource = new List<MyBehavior.EventMaterialReference>
        {
            new() { MaterialType = "raw_text", ActionStableKey = "village_raid_started:village_a", SettlementId = "village_a", Label = "掠夺开始", ActionDay = 1 },
            new() { MaterialType = "raw_text", ActionStableKey = "raid_completed:village_a:Defender", SettlementId = "village_a", Label = "掠夺结果", ActionDay = 2 },
            new() { MaterialType = "raw_text", ActionStableKey = "village_raid_started:village_a", SettlementId = "village_a", Label = "掠夺开始", ActionDay = 3 }
        };
        var raid = WeeklyPromptMaterialOwner.BuildResolvedVillageRaidMaterial(raidSource, id => id == "village_a" ? "A 村" : "");
        Check(raid.MaterialType == "prompt_resolved_village_raid", "raid material type");
        Check(raid.SourceMaterialCount == 3, "raid source count");
        Check(raid.SnapshotText.Contains("掠夺被击退") && raid.SnapshotText.Contains("随后又遭到掠夺"), "raid defended then restarted");
        Check(!raid.SnapshotText.Contains("：掠夺成功，"), "raid no false success");
        Check(WeeklyPromptMaterialOwner.ResolveVillageRaidOutcome(new() { ActionStableKey = "raid_completed:village_a:Attacker" }) == "success", "attacker success");
        Check(WeeklyPromptMaterialOwner.ResolveVillageRaidOutcome(new() { ActionStableKey = "raid_completed:village_a:None" }) == "aborted", "aborted outcome");
        var shortGroup = new MyBehavior.WeeklyEventMaterialPreviewGroup
        {
            KingdomId = "kingdom_a",
            Materials = new()
            {
                new() { MaterialType = "prompt_agg_movement", ActionKind = "prompt_aggregate:movement", SnapshotText = "事件=近期行军动向；前往 A 村袭扰", SourceMaterialCount = 2 },
                new() { MaterialType = "raw_text", ActionStableKey = "tournament_finished:one", Label = "竞技大会结算" },
                new() { MaterialType = "raw_text", ActionStableKey = "other:one", Label = "保留" }
            }
        };
        WeeklyPromptMaterialOwner.Prepare(shortGroup, true);
        Check(shortGroup.OutputMode == MyBehavior.WeeklyReportOutputMode.TitleShortTagsOnly, "short output mode");
        Check(!shortGroup.IncludePreviousReportInPrompt, "short omits previous report");
        Check(shortGroup.PromptMaterials.Any(x => x.MaterialType == "prompt_short_movement" && x.SourceMaterialCount == 2), "movement converter executes in owner");
        Check(!shortGroup.PromptMaterials.Any(x => x.ActionStableKey == "tournament_finished:one"), "tournament excluded");
        Check(shortGroup.PromptMaterials.Any(x => x.Label == "保留"), "unclassified material cloned");
        var stats = WeeklyPromptMaterialOwner.BuildWeeklyPromptShortSettlementStatsMaterial(
            new()
            {
                new() { MaterialType = "raw_text", ActionStableKey = "settlement_stats:a", SettlementId = "a", SnapshotText = "繁荣改善；粮食吃紧", ActionDay = 2 },
                new() { MaterialType = "raw_text", ActionStableKey = "settlement_stats:b", SettlementId = "b", SnapshotText = "治安改善", ActionDay = 3 }
            }, shortGroup);
        Check(stats.MaterialType == "prompt_short_settlement_stats" && stats.SourceMaterialCount == 2, "settlement stats owned summary");
        Check(stats.SnapshotText.Contains("繁荣") && stats.SnapshotText.Contains("粮食"), "settlement trends preserved");
        raidSource[1].ActorKingdomId = "kingdom_a";
        var shortRaid = WeeklyPromptMaterialOwner.BuildWeeklyPromptShortVillageRaidMaterial(raidSource, shortGroup);
        Check(shortRaid.MaterialType == "prompt_short_village_raid" && shortRaid.SourceMaterialCount == 3, "raid short owned summary");
        Check(shortRaid.SnapshotText.Contains("被击退") && !shortRaid.SnapshotText.Contains("掠夺成功：1"), "raid short outcome not inflated");
        var factsRead = 0;
        var eventOwner = new WeeklyAggregateEventLineOwner(new WeeklyAggregateGameFacts(
            id => "英雄:" + id, id => "家族:" + id, id => "王国:" + id,
            id => "据点:" + id, id => "据点名称:" + id, id => "村庄:" + id,
            id => { factsRead++; return id == "victim" ? new WeeklyHeroFact { Id = id, IsFemale = true, FatherId = "father" } : id == "father" ? new WeeklyHeroFact { Id = id } : null; }));
        Check(eventOwner.Render(null) == "", "empty event render");
        Check(eventOwner.Render(new() { new() { ActionKind = "unknown", ActionStableKey = "unknown:one" } }).Contains("未知") == false, "generic event renders without game object");
        var deathLine = eventOwner.Render(new()
        {
            new() { ActionKind = "hero_killed", HeroId = "victim", ActorHeroId = "father", ActionStableKey = "hero_killed:victim:Murdered" },
            new() { ActionKind = "clan_member_killed", HeroId = "victim", ActorHeroId = "father", ActionStableKey = "hero_killed:victim:Murdered" }
        });
        Check(deathLine.Contains("英雄:father的女儿") && factsRead > 0, "death renderer reads captured kinship facts");
        Check(WeeklyAggregateEventLineOwner.TranslateNpcActionKindForPrompt("clan_rebellion") == "家族叛乱事件", "action kind text owned");
        Check(WeeklyAggregateEventLineOwner.TryParseWeeklyPromptPrisonerPair("prisoner_taken:captor:prisoner:captor", out var captorId, out var prisonerId) && captorId == "captor" && prisonerId == "prisoner", "prisoner pair parser reused");
        Check(!WeeklyAggregateEventLineOwner.TryParseWeeklyPromptPrisonerPair("other:captor:prisoner", out _, out _), "unrelated pair excluded");
        Console.WriteLine($"PASS J17WeeklyAggregationRules {_checks} checks");
    }
}

public partial class MyBehavior
{
    internal static IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterials(IEnumerable<EventMaterialReference> source) => source ?? Enumerable.Empty<EventMaterialReference>();
    internal static EventMaterialReference CloneEventMaterialReference(EventMaterialReference source) => (EventMaterialReference)typeof(EventMaterialReference).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(source, null)!;
    internal static EventMaterialReference BuildWeeklyPromptResolvedVillageRaidMaterial(List<EventMaterialReference> source) => WeeklyPromptMaterialOwner.BuildResolvedVillageRaidMaterial(source, id => id == "village_a" ? "A 村" : "");
    internal static string ResolveHeroKingdomIdForPrompt(string heroId) => heroId == "hero_a" ? "kingdom_a" : "kingdom_b";
    internal static string ResolveClanDisplay(string clanId) => clanId;
    internal static string ResolveKingdomDisplay(string kingdomId) => kingdomId;
}
