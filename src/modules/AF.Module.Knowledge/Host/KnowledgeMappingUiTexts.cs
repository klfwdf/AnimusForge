using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Localized labels for the Knowledge text-mapping editor. Internal mapping tokens remain unchanged.</summary>
internal static class KnowledgeMappingUiTexts
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

	public static string StatusSource(string sourceKey)
	{
		return (sourceKey ?? "").Trim().ToLowerInvariant() switch
		{
			"hero" => Text("AF_KNOWLEDGE_MappingSource_hero", "Hero"),
			"current_npc_hero" => Text("AF_KNOWLEDGE_MappingSource_current_npc_hero", "Current NPC"),
			"player_hero" => Text("AF_KNOWLEDGE_MappingSource_player_hero", "Player"),
			"bound_hero" => Text("AF_KNOWLEDGE_MappingSource_bound_hero", "Bound hero"),
			"clan" => Text("AF_KNOWLEDGE_MappingSource_clan", "Clan"),
			"current_npc_clan" => Text("AF_KNOWLEDGE_MappingSource_current_npc_clan", "Current NPC clan"),
			"player_clan" => Text("AF_KNOWLEDGE_MappingSource_player_clan", "Player clan"),
			"bound_settlement_owner_clan" => Text("AF_KNOWLEDGE_MappingSource_bound_settlement_owner_clan", "Bound settlement owner clan"),
			"bound_hero_clan" => Text("AF_KNOWLEDGE_MappingSource_bound_hero_clan", "Bound hero clan"),
			"kingdom" => Text("AF_KNOWLEDGE_MappingSource_kingdom", "Kingdom"),
			"current_npc_kingdom" => Text("AF_KNOWLEDGE_MappingSource_current_npc_kingdom", "Current NPC kingdom"),
			"player_kingdom" => Text("AF_KNOWLEDGE_MappingSource_player_kingdom", "Player kingdom"),
			"bound_kingdom" => Text("AF_KNOWLEDGE_MappingSource_bound_kingdom", "Bound kingdom"),
			"settlement" => Text("AF_KNOWLEDGE_MappingSource_settlement", "Settlement"),
			"current_npc_settlement" => Text("AF_KNOWLEDGE_MappingSource_current_npc_settlement", "Current NPC settlement"),
			"player_settlement" => Text("AF_KNOWLEDGE_MappingSource_player_settlement", "Player settlement"),
			"bound_settlement" => Text("AF_KNOWLEDGE_MappingSource_bound_settlement", "Bound settlement"),
			_ => Text("AF_KNOWLEDGE_MappingSource_Unknown", "Status object"),
		};
	}

	public static string StatusCondition(string statusKey)
	{
		return (statusKey ?? "").Trim().ToLowerInvariant() switch
		{
			"is_alive" => Text("AF_KNOWLEDGE_MappingCondition_is_alive", "Is alive"),
			"is_dead" => Text("AF_KNOWLEDGE_MappingCondition_is_dead", "Is dead"),
			"is_disabled" => Text("AF_KNOWLEDGE_MappingCondition_is_disabled", "Is disabled"),
			"is_missing" => Text("AF_KNOWLEDGE_MappingCondition_is_missing", "Is missing"),
			"is_married" => Text("AF_KNOWLEDGE_MappingCondition_is_married", "Is married"),
			"is_widowed" => Text("AF_KNOWLEDGE_MappingCondition_is_widowed", "Is widowed"),
			"is_female" => Text("AF_KNOWLEDGE_MappingCondition_is_female", "Is female"),
			"is_male" => Text("AF_KNOWLEDGE_MappingCondition_is_male", "Is male"),
			"is_child" => Text("AF_KNOWLEDGE_MappingCondition_is_child", "Is child"),
			"is_adult" => Text("AF_KNOWLEDGE_MappingCondition_is_adult", "Is adult"),
			"is_in_age_range" => Text("AF_KNOWLEDGE_MappingCondition_is_in_age_range", "Is in age range"),
			"is_clan_leader" => Text("AF_KNOWLEDGE_MappingCondition_is_clan_leader", "Is clan leader"),
			"is_kingdom_leader" => Text("AF_KNOWLEDGE_MappingCondition_is_kingdom_leader", "Is kingdom leader"),
			"is_governor" => Text("AF_KNOWLEDGE_MappingCondition_is_governor", "Is governor"),
			"is_prisoner" => Text("AF_KNOWLEDGE_MappingCondition_is_prisoner", "Is prisoner"),
			"is_in_settlement" => Text("AF_KNOWLEDGE_MappingCondition_is_in_settlement", "Is in settlement"),
			"is_in_field" => Text("AF_KNOWLEDGE_MappingCondition_is_in_field", "Is in field"),
			"is_wanderer" => Text("AF_KNOWLEDGE_MappingCondition_is_wanderer", "Is wanderer"),
			"is_notable" => Text("AF_KNOWLEDGE_MappingCondition_is_notable", "Is notable"),
			"is_lord" => Text("AF_KNOWLEDGE_MappingCondition_is_lord", "Is lord"),
			"is_merchant" => Text("AF_KNOWLEDGE_MappingCondition_is_merchant", "Is merchant"),
			"is_gang_leader" => Text("AF_KNOWLEDGE_MappingCondition_is_gang_leader", "Is gang leader"),
			"is_artisan" => Text("AF_KNOWLEDGE_MappingCondition_is_artisan", "Is artisan"),
			"is_preacher" => Text("AF_KNOWLEDGE_MappingCondition_is_preacher", "Is preacher"),
			"is_headman" => Text("AF_KNOWLEDGE_MappingCondition_is_headman", "Is headman"),
			"is_minor_faction_hero" => Text("AF_KNOWLEDGE_MappingCondition_is_minor_faction_hero", "Is minor faction hero"),
			"is_party_leader" => Text("AF_KNOWLEDGE_MappingCondition_is_party_leader", "Is party leader"),
			"is_player_companion" => Text("AF_KNOWLEDGE_MappingCondition_is_player_companion", "Is player companion"),
			"is_rebel" => Text("AF_KNOWLEDGE_MappingCondition_is_rebel", "Is rebel"),
			"is_wounded" => Text("AF_KNOWLEDGE_MappingCondition_is_wounded", "Is wounded"),
			"is_known_to_player" => Text("AF_KNOWLEDGE_MappingCondition_is_known_to_player", "Is known to player"),
			"has_children" => Text("AF_KNOWLEDGE_MappingCondition_has_children", "Has children"),
			"has_father" => Text("AF_KNOWLEDGE_MappingCondition_has_father", "Has father"),
			"has_mother" => Text("AF_KNOWLEDGE_MappingCondition_has_mother", "Has mother"),
			"has_home_settlement" => Text("AF_KNOWLEDGE_MappingCondition_has_home_settlement", "Has home settlement"),
			"is_eliminated" => Text("AF_KNOWLEDGE_MappingCondition_is_eliminated", "Is eliminated"),
			"has_kingdom" => Text("AF_KNOWLEDGE_MappingCondition_has_kingdom", "Has kingdom"),
			"has_leader" => Text("AF_KNOWLEDGE_MappingCondition_has_leader", "Has leader"),
			"has_ruling_clan" => Text("AF_KNOWLEDGE_MappingCondition_has_ruling_clan", "Has ruling clan"),
			"has_any_settlement" => Text("AF_KNOWLEDGE_MappingCondition_has_any_settlement", "Has any settlement"),
			"has_any_town" => Text("AF_KNOWLEDGE_MappingCondition_has_any_town", "Has any town"),
			"has_any_castle" => Text("AF_KNOWLEDGE_MappingCondition_has_any_castle", "Has any castle"),
			"has_any_village" => Text("AF_KNOWLEDGE_MappingCondition_has_any_village", "Has any village"),
			"has_members" => Text("AF_KNOWLEDGE_MappingCondition_has_members", "Has members"),
			"has_male_members" => Text("AF_KNOWLEDGE_MappingCondition_has_male_members", "Has male members"),
			"has_female_members" => Text("AF_KNOWLEDGE_MappingCondition_has_female_members", "Has female members"),
			"has_age_range_members" => Text("AF_KNOWLEDGE_MappingCondition_has_age_range_members", "Has age range members"),
			"is_mercenary" => Text("AF_KNOWLEDGE_MappingCondition_is_mercenary", "Is mercenary"),
			"is_minor_faction" => Text("AF_KNOWLEDGE_MappingCondition_is_minor_faction", "Is minor faction"),
			"is_rebel_clan" => Text("AF_KNOWLEDGE_MappingCondition_is_rebel_clan", "Is rebel clan"),
			"is_noble" => Text("AF_KNOWLEDGE_MappingCondition_is_noble", "Is noble"),
			"is_bandit_faction" => Text("AF_KNOWLEDGE_MappingCondition_is_bandit_faction", "Is bandit faction"),
			"is_outlaw" => Text("AF_KNOWLEDGE_MappingCondition_is_outlaw", "Is outlaw"),
			"has_any_clan" => Text("AF_KNOWLEDGE_MappingCondition_has_any_clan", "Has any clan"),
			"has_any_lord" => Text("AF_KNOWLEDGE_MappingCondition_has_any_lord", "Has any lord"),
			"has_active_policies" => Text("AF_KNOWLEDGE_MappingCondition_has_active_policies", "Has active policies"),
			"has_any_war" => Text("AF_KNOWLEDGE_MappingCondition_has_any_war", "Has any war"),
			"has_any_allies" => Text("AF_KNOWLEDGE_MappingCondition_has_any_allies", "Has any allies"),
			"is_active" => Text("AF_KNOWLEDGE_MappingCondition_is_active", "Is active"),
			"is_town" => Text("AF_KNOWLEDGE_MappingCondition_is_town", "Is town"),
			"is_castle" => Text("AF_KNOWLEDGE_MappingCondition_is_castle", "Is castle"),
			"is_village" => Text("AF_KNOWLEDGE_MappingCondition_is_village", "Is village"),
			"is_fortification" => Text("AF_KNOWLEDGE_MappingCondition_is_fortification", "Is fortification"),
			"is_hideout" => Text("AF_KNOWLEDGE_MappingCondition_is_hideout", "Is hideout"),
			"is_under_siege" => Text("AF_KNOWLEDGE_MappingCondition_is_under_siege", "Is under siege"),
			"is_under_raid" => Text("AF_KNOWLEDGE_MappingCondition_is_under_raid", "Is under raid"),
			"is_raided" => Text("AF_KNOWLEDGE_MappingCondition_is_raided", "Is raided"),
			"is_starving" => Text("AF_KNOWLEDGE_MappingCondition_is_starving", "Is starving"),
			"is_rebellious" => Text("AF_KNOWLEDGE_MappingCondition_is_rebellious", "Is rebellious"),
			"has_port" => Text("AF_KNOWLEDGE_MappingCondition_has_port", "Has port"),
			"has_owner" => Text("AF_KNOWLEDGE_MappingCondition_has_owner", "Has owner"),
			"has_owner_clan" => Text("AF_KNOWLEDGE_MappingCondition_has_owner_clan", "Has owner clan"),
			"has_notables" => Text("AF_KNOWLEDGE_MappingCondition_has_notables", "Has notables"),
			"has_parties" => Text("AF_KNOWLEDGE_MappingCondition_has_parties", "Has parties"),
			_ => Text("AF_KNOWLEDGE_MappingCondition_Unknown", "Unknown status"),
		};
	}

	public static string StatusLabel(string sourceKey, string statusKey)
	{
		return Text("AF_KNOWLEDGE_MappingStatusLabel", "Status check: {SOURCE}: {CONDITION}", ("SOURCE", StatusSource(sourceKey)), ("CONDITION", StatusCondition(statusKey)));
	}

	public static string Kind(string kind)
	{
		string normalized = (kind ?? "").Trim();
		if (normalized.Length == 0)
		{
			return Text("AF_KNOWLEDGE_MappingKind_Unknown", "Unknown mapping");
		}
		return Text("AF_KNOWLEDGE_MappingKind_" + normalized, Humanize(normalized));
	}

	public static string TargetTypeForSource(string sourceKey)
	{
		return (sourceKey ?? "").Trim().ToLowerInvariant() switch
		{
			"hero" or "current_npc_hero" or "player_hero" or "bound_hero" => Text("AF_KNOWLEDGE_MappingTarget_Hero", "Hero"),
			"clan" or "current_npc_clan" or "player_clan" or "bound_settlement_owner_clan" or "bound_hero_clan" => Text("AF_KNOWLEDGE_MappingTarget_Clan", "Clan"),
			"kingdom" or "current_npc_kingdom" or "player_kingdom" or "bound_kingdom" => Text("AF_KNOWLEDGE_MappingTarget_Kingdom", "Kingdom"),
			"settlement" or "current_npc_settlement" or "player_settlement" or "bound_settlement" => Text("AF_KNOWLEDGE_MappingTarget_Settlement", "Settlement"),
			_ => Text("AF_KNOWLEDGE_MappingTarget_Automatic", "Automatic target"),
		};
	}

	public static string TargetTypeForKind(string kind)
	{
		string normalized = (kind ?? "").Trim().ToLowerInvariant();
		if (normalized.StartsWith("current_npc_", StringComparison.Ordinal) || normalized.StartsWith("player_", StringComparison.Ordinal) || normalized.StartsWith("bound_", StringComparison.Ordinal))
		{
			return Text("AF_KNOWLEDGE_MappingTarget_Automatic", "Automatic target");
		}
		if (normalized.StartsWith("kingdom_", StringComparison.Ordinal)) return Text("AF_KNOWLEDGE_MappingTarget_Kingdom", "Kingdom");
		if (normalized.StartsWith("settlement_", StringComparison.Ordinal)) return Text("AF_KNOWLEDGE_MappingTarget_Settlement", "Settlement");
		if (normalized.StartsWith("clan_", StringComparison.Ordinal)) return Text("AF_KNOWLEDGE_MappingTarget_Clan", "Clan");
		if (normalized.StartsWith("hero_", StringComparison.Ordinal)) return Text("AF_KNOWLEDGE_MappingTarget_Hero", "Hero");
		return Text("AF_KNOWLEDGE_MappingTarget_Generic", "Target");
	}

	private static string Humanize(string value)
	{
		string[] words = (value ?? "").Replace("_", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < words.Length; i++)
		{
			if (string.Equals(words[i], "npc", StringComparison.OrdinalIgnoreCase)) words[i] = "NPC";
			else if (words[i].Length > 0) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
		}
		return string.Join(" ", words);
	}

	private static string Text(string id, string fallback, params (string Name, string Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }
}
