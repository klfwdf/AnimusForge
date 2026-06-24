using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for local civilian witness reactions when the player hurts a civilian
/// during the active GCCZ scene. AF adapters own live agent lookup, movement, combat, and speech dispatch.
/// </summary>
public static class SiegeLocalCivilianReactionProfile
{
    public const float WitnessRadius = 24f;

    public const int MaxWitnessesPerIncident = 18;

    public const int MaxSpeakersPerIncident = 4;

    public const int MaxResistersPerIncident = 3;

    public const float WitnessRepeatCooldownSeconds = 18f;

    public const string WitnessFleeSource = "local_player_attack_witness_flee";

    public const string WitnessResistSource = "local_player_attack_witness_resist";

    public const string PlayerDownSource = "local_player_attack_down";

    public const string WitnessMemoryTitle = "局部恐慌";

    public static bool IsInsideWitnessRadiusSquared(float distanceSquared)
    {
        return distanceSquared <= WitnessRadius * WitnessRadius;
    }

    public static int CalculateMaxResisters(int witnessCount, int resistantEligibleCount)
    {
        if (witnessCount <= 0 || resistantEligibleCount <= 0)
        {
            return 0;
        }
        int proportionalLimit = (int)Math.Round(witnessCount * 0.18d, MidpointRounding.AwayFromZero);
        int safeLimit = Math.Max(1, proportionalLimit);
        return Math.Min(MaxResistersPerIncident, Math.Min(resistantEligibleCount, safeLimit));
    }

    public static bool ShouldAssignWitnessSpeech(int currentSpeakerCount)
    {
        return currentSpeakerCount >= 0 && currentSpeakerCount < MaxSpeakersPerIncident;
    }

    public static string BuildPlayerDownMessage(string targetName)
    {
        return "【局部冲突】你打倒了 " + NormalizeTargetName(targetName, "一名NPC") + "；附近民众会按区域恐慌逃散，少数胆大或带武器者可能反抗，但不会自动升级为全城血洗。";
    }

    public static string BuildPlayerDownMemoryText(string targetName)
    {
        return "玩家在攻城后处置场景中打倒了 " + NormalizeTargetName(targetName, "一名NPC") + "；这只触发局部区域恐慌和少量自卫反抗，不得自动升级为全城血洗。";
    }

    public static string BuildWitnessMemoryText(string targetName, int fleeingCount, int resistingCount)
    {
        return "玩家攻击 " + NormalizeTargetName(targetName, "一名NPC") + " 后，附近约 " + Math.Max(0, fleeingCount) + " 名民众开始逃散，约 " + Math.Max(0, resistingCount) + " 名民众尝试局部反抗；该反应只代表街巷区域性冲突。";
    }

    public static string BuildWitnessFact(string targetName, bool victimDown, bool witnessWillResist, string settlementName)
    {
        string scene = string.IsNullOrWhiteSpace(settlementName) ? SiegeAmbientReactionProfile.DefaultSettlementName : settlementName.Trim();
        string target = NormalizeTargetName(targetName, "附近一名民众");
        string incident = victimDown ? "玩家刚打倒了" : "玩家刚攻击了";
        string role = witnessWillResist
            ? "你是附近少数胆大、带武器或有身份的人，会惊恐但试图短促反抗或喝止。"
            : "你是附近目击的战败平民，会惊恐求生、喊人快跑、求饶或提醒家人躲开。";
        return "【攻城处置环境发言】当前地点是" + scene + "。" + incident + target + "，这只是局部街巷冲突，不是全城血洗命令。"
            + role
            + "必须承认玩家是刚攻下此地的胜利方首领/处置者，不要把玩家叫成陌生人、路人、本地人或库赛特人。"
            + "请只说一句12到32字的现场话，不要写旁白、动作描写或方括号标签。";
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
