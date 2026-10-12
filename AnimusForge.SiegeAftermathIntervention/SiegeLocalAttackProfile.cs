namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy and wording for local player attacks during active GCCZ intervention scenes.
/// A player strike against one NPC is a local conflict signal; AF adapters own live agent panic and standoff routing.
/// </summary>
public static class SiegeLocalAttackProfile
{
    public const uint MessageColor = 0xFFFFD27Fu;

    public const float AbnormalCivilianHealthLimitMultiplier = 3f;

    public const string PlayerAgentHitBridgeSource = "intervention_agent_hit";

    public const string PlayerScoreHitBridgeSource = "intervention_score_hit";

    public const string NonEnemyDamagePrefixSource = "non_enemy_damage_prefix";

    public const string LocalAttackSource = "local_player_attack";

    public const string LocalFleeSource = "local_player_attack_flee";

    public const string LocalDefiantSource = "local_player_attack_defiant";

    public const string MemoryTitle = "Local skirmish";
    public const string MemoryTitleEn = "Local Conflict";

    public static string GetMemoryTitle(bool isEnglish) => isEnglish ? MemoryTitleEn : MemoryTitle;

    public static bool ShouldNormalizeCivilianHealth(float currentHealthLimit, float normalHealthLimit)
    {
        return normalHealthLimit > 0f
            && currentHealthLimit > normalHealthLimit * AbnormalCivilianHealthLimitMultiplier;
    }

    public static string BuildPlayerHitMessage(string targetName, bool targetWillDefy)
        => BuildPlayerHitMessage(targetName, targetWillDefy, false);

    public static string BuildPlayerHitMessage(string targetName, bool targetWillDefy, bool isEnglish)
    {
        if (isEnglish)
        {
            string reactionEn = targetWillDefy ? "target calls for restraint/stands defiant without attacking soldiers" : "target flees, nearby panicked";
            return "[Local conflict] Hit " + NormalizeTargetName(targetName, "an NPC") + "; " + reactionEn + ".";
        }
        string reaction = targetWillDefy ? SiegeAftermathCompatibilityTexts.Raw("attack_defy_reaction") : SiegeAftermathCompatibilityTexts.Raw("attack_flee_reaction");
        return SiegeAftermathCompatibilityTexts.Raw("attack_hit_prefix") + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("attack_semi") + reaction + SiegeAftermathCompatibilityTexts.Raw("attack_period");
    }

    public static string BuildPlayerHitMemoryText(string targetName, bool targetWillDefy)
        => BuildPlayerHitMemoryText(targetName, targetWillDefy, false);

    public static string BuildPlayerHitMemoryText(string targetName, bool targetWillDefy, bool isEnglish)
    {
        if (isEnglish)
        {
            string reactionEn = targetWillDefy ? "target marked as local defiance/calling restraint, cannot link to massacre hostility" : "target marked as local fleeing civilian";
            return "The player attacked " + NormalizeTargetName(targetName, "an NPC") + " in the post-siege scene; this caused only local conflict. " + reactionEn + "; must not automatically escalate to full-city massacre.";
        }
        string reaction = targetWillDefy ? SiegeAftermathCompatibilityTexts.Raw("attack_memory_defy") : SiegeAftermathCompatibilityTexts.Raw("attack_memory_flee");
        return SiegeAftermathCompatibilityTexts.Raw("attack_memory_prefix") + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("attack_memory_middle") + reaction + SiegeAftermathCompatibilityTexts.Raw("attack_memory_suffix");
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
