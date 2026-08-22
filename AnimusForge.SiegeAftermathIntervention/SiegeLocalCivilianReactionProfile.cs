using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free policy for local civilian witness reactions when the player hurts a civilian
/// during the active GCCZ scene. AF adapters own live agent lookup, movement, and speech dispatch; combat escalation stays outside this local panic policy.
/// </summary>
public static class SiegeLocalCivilianReactionProfile
{
    public const float WitnessRadius = 24f;

    public const int MaxWitnessesPerIncident = 18;

    public const int MaxDefiantWitnessesPerIncident = 3;

    public const float LocalFleeMinDistance = 28f;

    public const float LocalFleeFallbackDistance = 36f;

    public const float LocalFleeArrivalRadius = 5f;

    public const float LocalFleeRefreshSeconds = 3.5f;

    public const float WitnessRepeatCooldownSeconds = 18f;

    public const uint SoldierWitnessFallbackMessageColor = 0xFFFFD27Fu;

    public const string WitnessFleeSource = "local_player_attack_witness_flee";

    public const string WitnessDefiantSource = "local_player_attack_witness_defiant";

    public const string NativeFleeBridgeSource = "local_player_attack_native_flee";

    public const string PlayerDownSource = "local_player_attack_down";

    public const string SoldierWitnessInquirySource = "local_player_attack_soldier_witness_inquiry";

    public const string WitnessMemoryTitle = "局部恐慌";

    public const string SoldierWitnessMemoryTitle = "士兵请示";

    public static bool IsInsideWitnessRadiusSquared(float distanceSquared)
    {
        return distanceSquared <= WitnessRadius * WitnessRadius;
    }

    public static int CalculateMaxDefiantWitnesses(int witnessCount, int defiantEligibleCount)
    {
        if (witnessCount <= 0 || defiantEligibleCount <= 0)
        {
            return 0;
        }
        int proportionalLimit = (int)Math.Round(witnessCount * 0.18d, MidpointRounding.AwayFromZero);
        int safeLimit = Math.Max(1, proportionalLimit);
        return Math.Min(MaxDefiantWitnessesPerIncident, Math.Min(defiantEligibleCount, safeLimit));
    }

    public static bool ShouldCivilianDefy(int deterministicSeed, bool isNotable, bool carriesWeapon, bool isGuardOrSoldier)
    {
        if (isGuardOrSoldier)
        {
            return false;
        }
        int normalizedSeed = deterministicSeed == int.MinValue ? int.MaxValue : Math.Abs(deterministicSeed);
        int bucket = normalizedSeed % 12;
        if (isNotable)
        {
            return bucket < 4;
        }
        if (carriesWeapon)
        {
            return bucket < 3;
        }
        return bucket == 0;
    }

    public static bool IsLocalFleeTargetFarEnough(float distanceSquared)
    {
        return distanceSquared >= LocalFleeMinDistance * LocalFleeMinDistance;
    }

    public static bool IsLocalFleeTargetReached(float distanceSquared)
    {
        return distanceSquared <= LocalFleeArrivalRadius * LocalFleeArrivalRadius;
    }

    public static bool ShouldRefreshLocalFleeOrder(bool force, bool hasTarget, bool hasLastOrder, bool reachedTarget, float elapsedSeconds)
    {
        return force
            || !hasTarget
            || !hasLastOrder
            || (!reachedTarget && elapsedSeconds >= LocalFleeRefreshSeconds);
    }

    public static string BuildPlayerDownMessage(string targetName)
    {
        return "【局部冲突】打倒" + NormalizeTargetName(targetName, "一名NPC") + "，附近逃散。";
    }

    public static string BuildPlayerDownMemoryText(string targetName)
    {
        return "玩家在攻城后处置场景中打倒了 " + NormalizeTargetName(targetName, "一名NPC") + "；这只触发局部区域恐慌和少量喝止/对峙，不得自动升级为全城血洗或士兵参战。";
    }

    public static string BuildWitnessMemoryText(string targetName, int fleeingCount, int defiantCount)
    {
        return "玩家攻击 " + NormalizeTargetName(targetName, "一名NPC") + " 后，附近约 " + Math.Max(0, fleeingCount) + " 名民众开始逃散，约 " + Math.Max(0, defiantCount) + " 名民众尝试局部喝止/对峙；该反应只代表街巷区域性冲突，不接入血洗式敌对。";
    }

    public static string BuildWitnessFact(string targetName, bool victimDown, bool witnessWillDefy, string settlementName)
    {
        string scene = string.IsNullOrWhiteSpace(settlementName) ? SiegeAmbientReactionProfile.DefaultSettlementName : settlementName.Trim();
        string target = NormalizeTargetName(targetName, "附近一名民众");
        string incident = victimDown ? "玩家刚打倒了" : "玩家刚攻击了";
        string role = witnessWillDefy
            ? "你是附近少数胆大、带武器或有身份的人，会惊恐但只做短促喝止、退让或对峙，不要说自己正主动攻击玩家或士兵。"
            : "你是附近目击的战败平民，会惊恐求生、喊人快跑、求饶或提醒家人躲开。";
        return "【攻城处置环境发言】当前地点是" + scene + "。" + incident + target + "，这只是局部街巷冲突，不是全城血洗命令。"
            + role
            + "必须承认玩家是刚攻下此地的胜利方首领/处置者，不要把玩家叫成陌生人、路人、本地人或库赛特人。"
            + "请只说一句12到32字的现场话，不要写旁白、动作描写或方括号标签。";
    }

    public static bool ResolveSoldierWitnessBloodthirstyFromPersona(string personalityText, string backgroundText = null)
    {
        string text = ((personalityText ?? "") + " " + (backgroundText ?? "")).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        return ContainsAny(text,
            "嗜血", "残酷", "残忍", "冷血", "好杀", "屠戮", "屠城", "杀戮", "杀人如麻", "渴望战利品", "贪战利", "贪婪好战", "劫掠成性", "掠夺成性",
            "bloodthirsty", "blood-thirsty", "cruel", "ruthless", "merciless", "sadistic", "brutal", "savage", "slaughter", "massacre", "plunder-hungry", "loot-hungry");
    }

    public static string BuildSoldierWitnessInquiryFact(string targetName, bool victimDown, string settlementName, bool soldierIsBloodthirsty = false, string soldierPersonaText = null)
    {
        string scene = string.IsNullOrWhiteSpace(settlementName) ? SiegeAmbientReactionProfile.DefaultSettlementName : settlementName.Trim();
        string target = NormalizeTargetName(targetName, "附近一名民众");
        string incident = victimDown ? "玩家刚打倒了" : "玩家刚攻击了";
        string personaLine = BuildSoldierPersonaLine(soldierPersonaText);
        string responsePolicy = BuildSoldierWitnessResponsePolicy(victimDown, soldierIsBloodthirsty);
        return "【攻城处置士兵请示】当前地点是" + scene + "。" + incident + target + "，你在24米内亲眼看到这个局部暴力信号。"
            + "你是玩家己方入城士兵，必须称玩家为统帅/大人/长官。" + personaLine + responsePolicy
            + "只说一句自然短句，不要写成三选一菜单，不要宣布已经执行处置，不得自行攻击平民，不得输出任何方括号动作标签。";
    }

    public static string BuildSoldierWitnessMemoryText(string targetName, bool victimDown, string soldierName, bool soldierIsBloodthirsty = false)
    {
        string actor = NormalizeTargetName(soldierName, "附近己方士兵");
        string incident = victimDown ? "打倒" : "攻击";
        string inquiry;
        if (soldierIsBloodthirsty)
        {
            inquiry = "以嗜血/残酷口吻向玩家请示是否下令屠城/血洗";
        }
        else if (victimDown)
        {
            inquiry = "已向玩家请示是否控住现场，或由玩家明令扩大为搜掠/血洗";
        }
        else
        {
            inquiry = "已向玩家请示是否压住附近人群并控制街口";
        }
        return actor + " 在24米内目击玩家" + incident + " " + NormalizeTargetName(targetName, "一名NPC") + "，" + inquiry + "；该请示本身不代表已经执行破坏性处置。";
    }

    public static string BuildSoldierWitnessFallbackMessage(string soldierName, string targetName, bool victimDown, bool soldierIsBloodthirsty = false)
    {
        string actor = NormalizeTargetName(soldierName, "附近己方士兵");
        if (soldierIsBloodthirsty)
        {
            return "【士兵请示】" + actor + "：统帅，要不要趁这股乱劲下屠城令？您一句话，我们就动手。";
        }
        if (victimDown)
        {
            return "【士兵请示】" + actor + "：统帅，人已经倒了。要我先控住这片？若要搜掠或血洗，请您明令。";
        }
        return "【士兵请示】" + actor + "：大人，这边乱起来了。要不要我带人压住街口，把围观的赶开？";
    }

    private static string BuildSoldierWitnessResponsePolicy(bool victimDown, bool soldierIsBloodthirsty)
    {
        if (soldierIsBloodthirsty)
        {
            return "你有嗜血/残酷倾向，允许更主动、更兴奋地请示是否趁乱下屠城令、血洗这座城或让士兵开杀；仍必须是在向玩家请令，不得说成已经执行。";
        }
        return victimDown
            ? "人已经倒下，局势可能扩大；请先请示是否控住这一片街口，只有在话语自然时才可提醒：若玩家要扩大为全城搜掠或血洗，必须由玩家明令。"
            : "这只是一次近处打击，局势还停留在街巷局部；请请示是否压住附近人、驱散围观、护住玩家或控住街口，不要主动提全城搜掠或血洗。";
    }

    private static string BuildSoldierPersonaLine(string soldierPersonaText)
    {
        string persona = NormalizeOptionalText(soldierPersonaText);
        return string.IsNullOrWhiteSpace(persona)
            ? ""
            : "你的已知个性/背景是：" + persona + "。请按这个性格组织语气，但仍服从玩家命令。";
    }

    private static string NormalizeOptionalText(string text)
    {
        return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(text) || needles == null)
        {
            return false;
        }
        foreach (string needle in needles)
        {
            if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
