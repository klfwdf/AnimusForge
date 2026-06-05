using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

internal enum TextMappingTargetMode
{
    Auto,
    Kingdom,
    Settlement,
    Clan,
    Hero,
    Manual
}

internal sealed class TextMappingKindDefinition
{
    public required string CategoryKey { get; init; }

    public required string CategoryLabel { get; init; }

    public required string Kind { get; init; }

    public required string Label { get; init; }

    public TextMappingTargetMode TargetMode { get; init; }

    public string AutoTargetId { get; init; } = "";

    public override string ToString()
    {
        return "【" + CategoryLabel + "】" + Label + " (" + Kind + ")";
    }
}

internal sealed class TextMappingTargetRequirement
{
    public TextMappingTargetRequirement(TextMappingTargetMode mode, string autoTargetId, string label)
    {
        Mode = mode;
        AutoTargetId = autoTargetId ?? "";
        Label = label ?? "";
    }

    public TextMappingTargetMode Mode { get; }

    public string AutoTargetId { get; }

    public string Label { get; }
}

internal sealed class TextMappingTargetOption
{
    public TextMappingTargetOption(string id, string display)
    {
        Id = id ?? "";
        Display = display ?? id ?? "";
    }

    public string Id { get; }

    public string Display { get; }

    public override string ToString()
    {
        return Display;
    }
}

internal static class TextMappingCatalog
{
    public const string TargetCurrentNpc = "__current_npc__";
    public const string TargetPlayer = "__player__";
    public const string TargetBoundKingdom = "__bound_kingdom__";
    public const string TargetBoundSettlement = "__bound_settlement__";
    public const string TargetBoundHero = "__bound_hero__";

    private static readonly IReadOnlyList<TextMappingKindDefinition> _allKindDefinitions = BuildKindDefinitions();
    private static readonly Dictionary<string, TextMappingKindDefinition> _byKind =
        _allKindDefinitions
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<TextMappingKindDefinition> AllKindDefinitions => _allKindDefinitions;

    public static TextMappingKindDefinition? FindKind(string? kind)
    {
        var clean = Clean(kind);
        return string.IsNullOrWhiteSpace(clean) ? null : _byKind.GetValueOrDefault(clean);
    }

    public static string GetKindDisplayName(string? kind)
    {
        var clean = Clean(kind);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return "";
        }

        if (TryParseStatusMappingKind(clean, out var sourceKey, out var statusKey))
        {
            return "状态判断：" + GetStatusSourceLabel(sourceKey) + " - " + GetStatusConditionLabel(statusKey);
        }

        var definition = FindKind(clean);
        return definition == null ? clean : definition.CategoryLabel + " - " + definition.Label;
    }

    public static TextMappingTargetRequirement GetTargetRequirement(string? kind)
    {
        var clean = Clean(kind);
        if (TryParseStatusMappingKind(clean, out var sourceKey, out _))
        {
            var auto = GetAutomaticTargetIdForStatusSource(sourceKey);
            if (!string.IsNullOrWhiteSpace(auto))
            {
                return new TextMappingTargetRequirement(TextMappingTargetMode.Auto, auto, "自动目标");
            }

            return GetStatusSourceObjectKind(sourceKey) switch
            {
                "kingdom" => new TextMappingTargetRequirement(TextMappingTargetMode.Kingdom, "", "王国"),
                "settlement" => new TextMappingTargetRequirement(TextMappingTargetMode.Settlement, "", "定居点"),
                "clan" => new TextMappingTargetRequirement(TextMappingTargetMode.Clan, "", "家族"),
                "hero" => new TextMappingTargetRequirement(TextMappingTargetMode.Hero, "", "英雄"),
                _ => new TextMappingTargetRequirement(TextMappingTargetMode.Manual, "", "目标")
            };
        }

        var definition = FindKind(clean);
        if (definition == null)
        {
            return new TextMappingTargetRequirement(TextMappingTargetMode.Manual, "", "目标");
        }

        if (definition.TargetMode == TextMappingTargetMode.Auto)
        {
            return new TextMappingTargetRequirement(TextMappingTargetMode.Auto, definition.AutoTargetId, "自动目标");
        }

        return new TextMappingTargetRequirement(definition.TargetMode, "", definition.TargetMode switch
        {
            TextMappingTargetMode.Kingdom => "王国",
            TextMappingTargetMode.Settlement => "定居点",
            TextMappingTargetMode.Clan => "家族",
            TextMappingTargetMode.Hero => "英雄",
            _ => "目标"
        });
    }

    public static bool IsStatusKind(string? kind)
    {
        return Clean(kind).StartsWith("status|", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAgeRangeKind(string? kind)
    {
        var clean = Clean(kind);
        if (TryParseStatusMappingKind(clean, out _, out var statusKey))
        {
            return statusKey.Equals("is_in_age_range", StringComparison.OrdinalIgnoreCase) ||
                   statusKey.Equals("has_age_range_members", StringComparison.OrdinalIgnoreCase);
        }

        return clean.Equals("clan_age_range_members", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("current_npc_clan_age_range_members", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("player_clan_age_range_members", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("bound_settlement_owner_clan_age_range_members", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("bound_hero_clan_age_range_members", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetTargetDisplayName(string? targetId, ConditionCatalog? catalog)
    {
        var id = Clean(targetId);
        if (string.IsNullOrWhiteSpace(id))
        {
            return "";
        }

        var auto = GetAutomaticTargetDisplayName(id);
        if (!string.IsNullOrWhiteSpace(auto))
        {
            return auto + " (" + id + ")";
        }

        var candidate = FindCandidate(id, catalog);
        return candidate?.ToString() ?? id;
    }

    public static string GetAutomaticTargetDisplayName(string? targetId)
    {
        return Clean(targetId) switch
        {
            TargetCurrentNpc => "当前对话NPC",
            TargetPlayer => "玩家",
            TargetBoundKingdom => "本知识绑定王国",
            TargetBoundSettlement => "本知识绑定定居点",
            TargetBoundHero => "本知识绑定英雄",
            _ => ""
        };
    }

    public static string ExtractIdFromDisplayText(string? text)
    {
        var clean = Clean(text);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return "";
        }

        var bracketStart = clean.LastIndexOf('(');
        var bracketEnd = clean.LastIndexOf(')');
        if (bracketStart >= 0 && bracketEnd > bracketStart)
        {
            var inside = clean[(bracketStart + 1)..bracketEnd].Trim();
            if (!string.IsNullOrWhiteSpace(inside) && !inside.Contains(' ', StringComparison.Ordinal))
            {
                return inside;
            }
        }

        return clean;
    }

    private static IReadOnlyList<TextMappingKindDefinition> BuildKindDefinitions()
    {
        var list = new List<TextMappingKindDefinition>();
        AddStatusKinds(list);

        Add(list, "current_npc", "当前NPC", "current_npc_name", "名字", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_name", "所属家族", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_kingdom_name", "所属家族的所属王国", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_kingdom_leader_name", "所属家族的所属王国领袖", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_members", "所属家族的成员", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_male_members", "所属家族的男性成员", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_female_members", "所属家族的女性成员", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_age_range_members", "所属家族的年龄段成员", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_all_towns", "所属家族的所有城镇", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_all_villages", "所属家族的所有村子", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_clan_all_settlements", "所属家族的所有定居点", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_name", "所属王国", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_leader_name", "效忠君主", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_ruling_clan_name", "所属王国执政家族", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_culture_name", "所属王国文化", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_initial_home_settlement_name", "所属王国初始都城", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_clans", "所属王国全部家族", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_lords", "所属王国全部领主", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_towns", "所属王国拥有的所有城镇", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_castles", "所属王国拥有的所有城堡", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_villages", "所属王国拥有的所有村庄", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_all_settlements", "所属王国拥有的所有定居点", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_active_policies", "所属王国生效政策", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_allied_kingdoms", "所属王国盟友", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_kingdom_war_factions", "所属王国交战势力", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_spouse_name", "配偶", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_father_name", "父亲", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_mother_name", "母亲", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_name", "所在定居点", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_owner_clan_name", "所在定居点统治家族", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_owner_leader_name", "所在定居点统治者", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_owner_kingdom_name", "所在定居点所属王国", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_owner_kingdom_leader_name", "所在定居点所属王国领袖", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_culture_name", "所在定居点文化", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_notables", "所在定居点要人", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_parties", "所在定居点驻留队伍", TextMappingTargetMode.Auto, TargetCurrentNpc);
        Add(list, "current_npc", "当前NPC", "current_npc_current_settlement_bound_villages", "所在定居点绑定村庄", TextMappingTargetMode.Auto, TargetCurrentNpc);

        Add(list, "player", "玩家", "player_name", "名字", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_name", "家族", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_kingdom_name", "家族所属王国", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_kingdom_leader_name", "家族所属王国领袖", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_members", "家族成员", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_male_members", "家族男性成员", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_female_members", "家族女性成员", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_age_range_members", "家族年龄段成员", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_all_towns", "家族的所有城镇", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_all_villages", "家族的所有村子", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_clan_all_settlements", "家族的所有定居点", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_kingdom_name", "所属王国", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_kingdom_leader_name", "效忠君主", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_spouse_name", "配偶", TextMappingTargetMode.Auto, TargetPlayer);
        Add(list, "player", "玩家", "player_current_settlement_name", "所在定居点", TextMappingTargetMode.Auto, TargetPlayer);

        AddBoundKinds(list);
        AddKingdomKinds(list);
        AddSettlementKinds(list);
        AddClanKinds(list);
        AddHeroKinds(list);

        return list
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static void AddBoundKinds(List<TextMappingKindDefinition> list)
    {
        Add(list, "bound", "绑定对象", "bound_kingdom_name", "王国名称", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_leader_name", "王国领袖", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_ruling_clan_name", "王国执政家族", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_culture_name", "王国文化", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_initial_home_settlement_name", "王国初始都城", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_clans", "王国全部家族", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_lords", "王国全部领主", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_towns", "王国拥有的所有城镇", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_castles", "王国拥有的所有城堡", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_villages", "王国拥有的所有村庄", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_all_settlements", "王国拥有的所有定居点", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_active_policies", "王国生效政策", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_allied_kingdoms", "王国盟友", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_kingdom_war_factions", "王国交战势力", TextMappingTargetMode.Auto, TargetBoundKingdom);
        Add(list, "bound", "绑定对象", "bound_settlement_name", "定居点名称", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_name", "定居点统治家族", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_kingdom_name", "定居点统治家族的所属王国", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_kingdom_leader_name", "定居点统治家族的所属王国领袖", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_members", "定居点统治家族的成员", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_male_members", "定居点统治家族的男性成员", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_female_members", "定居点统治家族的女性成员", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_age_range_members", "定居点统治家族的年龄段成员", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_all_towns", "定居点统治家族的所有城镇", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_all_villages", "定居点统治家族的所有村子", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_clan_all_settlements", "定居点统治家族的所有定居点", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_leader_name", "定居点统治者", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_kingdom_name", "定居点所属王国", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_owner_kingdom_leader_name", "定居点所属王国领袖", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_culture_name", "定居点文化", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_notables", "定居点要人", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_parties", "定居点驻留队伍", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_settlement_bound_villages", "定居点绑定村庄", TextMappingTargetMode.Auto, TargetBoundSettlement);
        Add(list, "bound", "绑定对象", "bound_hero_name", "英雄名字", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_name", "英雄所属家族", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_kingdom_name", "英雄所属家族的所属王国", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_kingdom_leader_name", "英雄所属家族的所属王国领袖", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_members", "英雄所属家族的成员", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_male_members", "英雄所属家族的男性成员", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_female_members", "英雄所属家族的女性成员", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_age_range_members", "英雄所属家族的年龄段成员", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_all_towns", "英雄所属家族的所有城镇", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_all_villages", "英雄所属家族的所有村子", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_clan_all_settlements", "英雄所属家族的所有定居点", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_kingdom_name", "英雄所属王国", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_kingdom_leader_name", "英雄效忠君主", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_spouse_name", "英雄配偶", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_father_name", "英雄父亲", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_mother_name", "英雄母亲", TextMappingTargetMode.Auto, TargetBoundHero);
        Add(list, "bound", "绑定对象", "bound_hero_current_settlement_name", "英雄所在定居点", TextMappingTargetMode.Auto, TargetBoundHero);
    }

    private static void AddKingdomKinds(List<TextMappingKindDefinition> list)
    {
        Add(list, "kingdom", "指定王国", "kingdom_name", "名称", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_leader_name", "当前领袖", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_ruling_clan_name", "执政家族", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_culture_name", "文化", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_initial_home_settlement_name", "初始都城", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_clans", "全部家族", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_lords", "全部领主", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_towns", "拥有的所有城镇", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_castles", "拥有的所有城堡", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_villages", "拥有的所有村庄", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_all_settlements", "拥有的所有定居点", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_active_policies", "生效政策", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_allied_kingdoms", "盟友", TextMappingTargetMode.Kingdom);
        Add(list, "kingdom", "指定王国", "kingdom_war_factions", "交战势力", TextMappingTargetMode.Kingdom);
    }

    private static void AddSettlementKinds(List<TextMappingKindDefinition> list)
    {
        Add(list, "settlement", "指定定居点", "settlement_name", "名称", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_owner_clan_name", "统治家族", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_owner_leader_name", "统治者", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_owner_kingdom_name", "所属王国", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_owner_kingdom_leader_name", "所属王国领袖", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_culture_name", "文化", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_notables", "要人", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_parties", "驻留队伍", TextMappingTargetMode.Settlement);
        Add(list, "settlement", "指定定居点", "settlement_bound_villages", "绑定村庄", TextMappingTargetMode.Settlement);
    }

    private static void AddClanKinds(List<TextMappingKindDefinition> list)
    {
        Add(list, "clan", "指定家族", "clan_name", "名称", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_leader_name", "当前族长", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_kingdom_name", "所属王国", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_kingdom_leader_name", "所属王国领袖", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_members", "成员", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_male_members", "男性成员", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_female_members", "女性成员", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_age_range_members", "年龄段成员", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_all_towns", "统治的所有城镇", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_all_villages", "统治的所有村子", TextMappingTargetMode.Clan);
        Add(list, "clan", "指定家族", "clan_all_settlements", "统治的所有定居点", TextMappingTargetMode.Clan);
    }

    private static void AddHeroKinds(List<TextMappingKindDefinition> list)
    {
        Add(list, "hero", "指定英雄", "hero_name", "当前名字", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_clan_name", "所属家族", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_kingdom_name", "所属王国", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_kingdom_leader_name", "效忠君主", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_spouse_name", "配偶", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_father_name", "父亲", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_mother_name", "母亲", TextMappingTargetMode.Hero);
        Add(list, "hero", "指定英雄", "hero_current_settlement_name", "当前定居点", TextMappingTargetMode.Hero);
    }

    private static void AddStatusKinds(List<TextMappingKindDefinition> list)
    {
        AddHeroStatusKinds(list, "current_npc_hero", "当前NPC", TextMappingTargetMode.Auto, TargetCurrentNpc);
        AddHeroStatusKinds(list, "player_hero", "玩家", TextMappingTargetMode.Auto, TargetPlayer);
        AddHeroStatusKinds(list, "bound_hero", "本知识绑定英雄", TextMappingTargetMode.Auto, TargetBoundHero);
        AddHeroStatusKinds(list, "hero", "指定英雄", TextMappingTargetMode.Hero);
        AddClanStatusKinds(list, "current_npc_clan", "当前NPC所属家族", TextMappingTargetMode.Auto, TargetCurrentNpc);
        AddClanStatusKinds(list, "player_clan", "玩家家族", TextMappingTargetMode.Auto, TargetPlayer);
        AddClanStatusKinds(list, "bound_settlement_owner_clan", "本知识绑定定居点统治家族", TextMappingTargetMode.Auto, TargetBoundSettlement);
        AddClanStatusKinds(list, "bound_hero_clan", "本知识绑定英雄所属家族", TextMappingTargetMode.Auto, TargetBoundHero);
        AddClanStatusKinds(list, "clan", "指定家族", TextMappingTargetMode.Clan);
        AddKingdomStatusKinds(list, "current_npc_kingdom", "当前NPC所属王国", TextMappingTargetMode.Auto, TargetCurrentNpc);
        AddKingdomStatusKinds(list, "player_kingdom", "玩家所属王国", TextMappingTargetMode.Auto, TargetPlayer);
        AddKingdomStatusKinds(list, "bound_kingdom", "本知识绑定王国", TextMappingTargetMode.Auto, TargetBoundKingdom);
        AddKingdomStatusKinds(list, "kingdom", "指定王国", TextMappingTargetMode.Kingdom);
        AddSettlementStatusKinds(list, "current_npc_settlement", "当前NPC所在定居点", TextMappingTargetMode.Auto, TargetCurrentNpc);
        AddSettlementStatusKinds(list, "player_settlement", "玩家所在定居点", TextMappingTargetMode.Auto, TargetPlayer);
        AddSettlementStatusKinds(list, "bound_settlement", "本知识绑定定居点", TextMappingTargetMode.Auto, TargetBoundSettlement);
        AddSettlementStatusKinds(list, "settlement", "指定定居点", TextMappingTargetMode.Settlement);
    }

    private static void AddHeroStatusKinds(List<TextMappingKindDefinition> list, string sourceKey, string prefix, TextMappingTargetMode mode, string autoTargetId = "")
    {
        foreach (var item in new (string Status, string Label)[]
                 {
                     ("is_alive", "是否存活"),
                     ("is_dead", "是否死亡"),
                     ("is_disabled", "是否失能"),
                     ("is_missing", "是否失踪/逃亡"),
                     ("is_married", "是否已婚"),
                     ("is_widowed", "是否丧偶"),
                     ("is_female", "是否女性"),
                     ("is_male", "是否男性"),
                     ("is_child", "是否未成年"),
                     ("is_adult", "是否成年"),
                     ("is_in_age_range", "是否在年龄段内"),
                     ("is_clan_leader", "是否家族族长"),
                     ("is_kingdom_leader", "是否王国领袖"),
                     ("is_governor", "是否总督"),
                     ("is_prisoner", "是否被俘"),
                     ("is_in_settlement", "是否在定居点内"),
                     ("is_in_field", "是否在野外"),
                     ("is_wanderer", "是否流浪者"),
                     ("is_notable", "是否要人"),
                     ("is_lord", "是否领主"),
                     ("is_merchant", "是否商人"),
                     ("is_gang_leader", "是否帮派头目"),
                     ("is_artisan", "是否工匠"),
                     ("is_preacher", "是否传教士"),
                     ("is_headman", "是否村长"),
                     ("is_minor_faction_hero", "是否小势力英雄"),
                     ("is_party_leader", "是否带队"),
                     ("is_player_companion", "是否玩家同伴"),
                     ("is_rebel", "是否叛军"),
                     ("is_wounded", "是否受伤"),
                     ("is_known_to_player", "是否被玩家认识"),
                     ("has_children", "是否有子女"),
                     ("has_father", "是否有父亲"),
                     ("has_mother", "是否有母亲"),
                     ("has_home_settlement", "是否有家乡定居点")
                 })
        {
            AddStatus(list, sourceKey, item.Status, prefix + "：" + item.Label, mode, autoTargetId);
        }
    }

    private static void AddClanStatusKinds(List<TextMappingKindDefinition> list, string sourceKey, string prefix, TextMappingTargetMode mode, string autoTargetId = "")
    {
        foreach (var item in new (string Status, string Label)[]
                 {
                     ("is_eliminated", "是否已灭绝"),
                     ("has_kingdom", "是否有所属王国"),
                     ("has_leader", "是否有族长"),
                     ("has_any_settlement", "是否拥有任何定居点"),
                     ("has_any_town", "是否拥有任何城镇"),
                     ("has_any_castle", "是否拥有任何城堡"),
                     ("has_any_village", "是否拥有任何村庄"),
                     ("has_members", "是否有成员"),
                     ("has_male_members", "是否有男性成员"),
                     ("has_female_members", "是否有女性成员"),
                     ("has_age_range_members", "是否有年龄段成员"),
                     ("is_mercenary", "是否雇佣兵家族"),
                     ("is_minor_faction", "是否小势力"),
                     ("is_rebel_clan", "是否叛军家族"),
                     ("is_noble", "是否贵族家族"),
                     ("is_bandit_faction", "是否匪帮势力"),
                     ("is_outlaw", "是否法外势力")
                 })
        {
            AddStatus(list, sourceKey, item.Status, prefix + "：" + item.Label, mode, autoTargetId);
        }
    }

    private static void AddKingdomStatusKinds(List<TextMappingKindDefinition> list, string sourceKey, string prefix, TextMappingTargetMode mode, string autoTargetId = "")
    {
        foreach (var item in new (string Status, string Label)[]
                 {
                     ("is_eliminated", "是否已灭亡"),
                     ("has_leader", "是否有领袖"),
                     ("has_ruling_clan", "是否有执政家族"),
                     ("has_any_settlement", "是否拥有任何定居点"),
                     ("has_any_town", "是否拥有任何城镇"),
                     ("has_any_castle", "是否拥有任何城堡"),
                     ("has_any_village", "是否拥有任何村庄"),
                     ("has_any_clan", "是否有任何家族"),
                     ("has_any_lord", "是否有任何领主"),
                     ("has_active_policies", "是否有生效政策"),
                     ("has_any_war", "是否处于战争中"),
                     ("has_any_allies", "是否有盟友")
                 })
        {
            AddStatus(list, sourceKey, item.Status, prefix + "：" + item.Label, mode, autoTargetId);
        }
    }

    private static void AddSettlementStatusKinds(List<TextMappingKindDefinition> list, string sourceKey, string prefix, TextMappingTargetMode mode, string autoTargetId = "")
    {
        foreach (var item in new (string Status, string Label)[]
                 {
                     ("is_active", "是否活跃"),
                     ("is_town", "是否城镇"),
                     ("is_castle", "是否城堡"),
                     ("is_village", "是否村庄"),
                     ("is_fortification", "是否堡垒"),
                     ("is_hideout", "是否藏身处"),
                     ("is_under_siege", "是否被围攻"),
                     ("is_under_raid", "是否正在被劫掠"),
                     ("is_raided", "是否已被劫掠"),
                     ("is_starving", "是否饥荒"),
                     ("is_rebellious", "是否处于叛乱状态"),
                     ("has_port", "是否有港口"),
                     ("has_owner", "是否有统治者"),
                     ("has_owner_clan", "是否有统治家族"),
                     ("has_notables", "是否有要人"),
                     ("has_parties", "是否有驻留队伍")
                 })
        {
            AddStatus(list, sourceKey, item.Status, prefix + "：" + item.Label, mode, autoTargetId);
        }
    }

    private static void AddStatus(List<TextMappingKindDefinition> list, string sourceKey, string statusKey, string label, TextMappingTargetMode mode, string autoTargetId)
    {
        Add(list, "status", "状态判断", "status|" + sourceKey + "|" + statusKey, label, mode, autoTargetId);
    }

    private static void Add(List<TextMappingKindDefinition> list, string categoryKey, string categoryLabel, string kind, string label, TextMappingTargetMode mode, string autoTargetId = "")
    {
        list.Add(new TextMappingKindDefinition
        {
            CategoryKey = categoryKey,
            CategoryLabel = categoryLabel,
            Kind = kind,
            Label = label,
            TargetMode = mode,
            AutoTargetId = autoTargetId
        });
    }

    private static bool TryParseStatusMappingKind(string kind, out string sourceKey, out string statusKey)
    {
        sourceKey = "";
        statusKey = "";
        var parts = Clean(kind).Split('|', StringSplitOptions.None);
        if (parts.Length != 3 || !parts[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        sourceKey = Clean(parts[1]).ToLowerInvariant();
        statusKey = Clean(parts[2]).ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(sourceKey) && !string.IsNullOrWhiteSpace(statusKey);
    }

    private static string GetStatusSourceLabel(string sourceKey)
    {
        return Clean(sourceKey).ToLowerInvariant() switch
        {
            "hero" => "指定英雄",
            "current_npc_hero" => "当前NPC",
            "player_hero" => "玩家",
            "bound_hero" => "本知识绑定英雄",
            "clan" => "指定家族",
            "current_npc_clan" => "当前NPC所属家族",
            "player_clan" => "玩家家族",
            "bound_settlement_owner_clan" => "本知识绑定定居点统治家族",
            "bound_hero_clan" => "本知识绑定英雄所属家族",
            "kingdom" => "指定王国",
            "current_npc_kingdom" => "当前NPC所属王国",
            "player_kingdom" => "玩家所属王国",
            "bound_kingdom" => "本知识绑定王国",
            "settlement" => "指定定居点",
            "current_npc_settlement" => "当前NPC所在定居点",
            "player_settlement" => "玩家所在定居点",
            "bound_settlement" => "本知识绑定定居点",
            _ => "状态对象"
        };
    }

    private static string GetStatusConditionLabel(string statusKey)
    {
        return Clean(statusKey).ToLowerInvariant() switch
        {
            "is_alive" => "是否存活",
            "is_dead" => "是否死亡",
            "is_disabled" => "是否失能",
            "is_missing" => "是否失踪/逃亡",
            "is_married" => "是否已婚",
            "is_widowed" => "是否丧偶",
            "is_female" => "是否女性",
            "is_male" => "是否男性",
            "is_child" => "是否未成年",
            "is_adult" => "是否成年",
            "is_in_age_range" => "是否在年龄段内",
            "is_clan_leader" => "是否家族族长",
            "is_kingdom_leader" => "是否王国领袖",
            "is_governor" => "是否总督",
            "is_prisoner" => "是否被俘",
            "is_in_settlement" => "是否在定居点内",
            "is_in_field" => "是否在野外",
            "is_wanderer" => "是否流浪者",
            "is_notable" => "是否要人",
            "is_lord" => "是否领主",
            "is_merchant" => "是否商人",
            "is_gang_leader" => "是否帮派头目",
            "is_artisan" => "是否工匠",
            "is_preacher" => "是否传教士",
            "is_headman" => "是否村长",
            "is_minor_faction_hero" => "是否小势力英雄",
            "is_party_leader" => "是否带队",
            "is_player_companion" => "是否玩家同伴",
            "is_rebel" => "是否叛军",
            "is_wounded" => "是否受伤",
            "is_known_to_player" => "是否被玩家认识",
            "has_children" => "是否有子女",
            "has_father" => "是否有父亲",
            "has_mother" => "是否有母亲",
            "has_home_settlement" => "是否有家乡定居点",
            "is_eliminated" => "是否已灭亡/被消灭",
            "has_kingdom" => "是否有所属王国",
            "has_leader" => "是否有领袖/族长",
            "has_ruling_clan" => "是否有执政家族",
            "has_any_settlement" => "是否拥有任何定居点",
            "has_any_town" => "是否拥有任何城镇",
            "has_any_castle" => "是否拥有任何城堡",
            "has_any_village" => "是否拥有任何村庄",
            "has_members" => "是否有成员",
            "has_male_members" => "是否有男性成员",
            "has_female_members" => "是否有女性成员",
            "has_age_range_members" => "是否有年龄段成员",
            "is_mercenary" => "是否雇佣兵家族",
            "is_minor_faction" => "是否小势力",
            "is_rebel_clan" => "是否叛军家族",
            "is_noble" => "是否贵族家族",
            "is_bandit_faction" => "是否匪帮势力",
            "is_outlaw" => "是否法外势力",
            "has_any_clan" => "是否有任何家族",
            "has_any_lord" => "是否有任何领主",
            "has_active_policies" => "是否有生效政策",
            "has_any_war" => "是否处于战争中",
            "has_any_allies" => "是否有盟友",
            "is_active" => "是否活跃",
            "is_town" => "是否城镇",
            "is_castle" => "是否城堡",
            "is_village" => "是否村庄",
            "is_fortification" => "是否堡垒",
            "is_hideout" => "是否藏身处",
            "is_under_siege" => "是否被围攻",
            "is_under_raid" => "是否正在被劫掠",
            "is_raided" => "是否已被劫掠",
            "is_starving" => "是否饥荒",
            "is_rebellious" => "是否处于叛乱状态",
            "has_port" => "是否有港口",
            "has_owner" => "是否有统治者",
            "has_owner_clan" => "是否有统治家族",
            "has_notables" => "是否有要人",
            "has_parties" => "是否有驻留队伍",
            _ => statusKey
        };
    }

    private static string GetStatusSourceObjectKind(string sourceKey)
    {
        return Clean(sourceKey).ToLowerInvariant() switch
        {
            "hero" or "current_npc_hero" or "player_hero" or "bound_hero" => "hero",
            "clan" or "current_npc_clan" or "player_clan" or "bound_settlement_owner_clan" or "bound_hero_clan" => "clan",
            "kingdom" or "current_npc_kingdom" or "player_kingdom" or "bound_kingdom" => "kingdom",
            "settlement" or "current_npc_settlement" or "player_settlement" or "bound_settlement" => "settlement",
            _ => ""
        };
    }

    private static string GetAutomaticTargetIdForStatusSource(string sourceKey)
    {
        return Clean(sourceKey).ToLowerInvariant() switch
        {
            "current_npc_hero" or "current_npc_clan" or "current_npc_kingdom" or "current_npc_settlement" => TargetCurrentNpc,
            "player_hero" or "player_clan" or "player_kingdom" or "player_settlement" => TargetPlayer,
            "bound_hero" or "bound_hero_clan" => TargetBoundHero,
            "bound_kingdom" => TargetBoundKingdom,
            "bound_settlement" or "bound_settlement_owner_clan" => TargetBoundSettlement,
            _ => ""
        };
    }

    private static ConditionCandidate? FindCandidate(string id, ConditionCatalog? catalog)
    {
        if (catalog == null || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return catalog.Heroes
                   .Concat(catalog.Clans)
                   .Concat(catalog.Kingdoms)
                   .Concat(catalog.Settlements)
                   .FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private static string Clean(string? value)
    {
        return (value ?? "").Trim();
    }
}
