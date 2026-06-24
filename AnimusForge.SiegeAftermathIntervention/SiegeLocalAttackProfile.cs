namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy and wording for local player attacks during active GCCZ intervention scenes.
/// A player strike against one NPC is a local conflict signal; AF adapters own live agent panic/fight routing.
/// </summary>
public static class SiegeLocalAttackProfile
{
    public const uint MessageColor = 0xFFFFD27Fu;

    public const string PlayerAgentHitBridgeSource = "intervention_agent_hit";

    public const string PlayerScoreHitBridgeSource = "intervention_score_hit";

    public const string NonEnemyDamagePrefixSource = "non_enemy_damage_prefix";

    public const string LocalAttackSource = "local_player_attack";

    public const string LocalFleeSource = "local_player_attack_flee";

    public const string LocalHostileSource = "local_player_attack_resist";

    public const string MemoryTitle = "局部冲突";

    public static string BuildPlayerHitMessage(string targetName, bool targetWillResist)
    {
        string reaction = targetWillResist ? "目标会进行局部反抗" : "目标会逃跑并引发附近恐慌";
        return "【局部冲突】你击中了 " + NormalizeTargetName(targetName, "一名NPC") + "，" + reaction + "；这不会自动进入全城血洗。若要血洗全城，必须对己方士兵明确下令。";
    }

    public static string BuildPlayerHitMemoryText(string targetName, bool targetWillResist)
    {
        string reaction = targetWillResist ? "该目标被标记为局部反抗者" : "该目标被标记为局部逃散者";
        return "玩家在攻城后处置场景中攻击了 " + NormalizeTargetName(targetName, "一名NPC") + "，这只造成局部冲突；" + reaction + "，不得因此自动升级为全城血洗。";
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
