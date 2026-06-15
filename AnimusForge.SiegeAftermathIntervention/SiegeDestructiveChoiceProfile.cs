using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for destructive GCCZ choices.
/// AF adapters apply Bannerlord aftermath, troops, mission, UI, settlement, and memory side effects.
/// </summary>
public sealed class SiegeDestructiveChoiceProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const uint DirectMassacreTriggerMessageColor = 0xFFFF7777u;

    public const string PlayerHitTriggerSource = "玩家主动攻击NPC触发血洗";

    public const string PlayerAgentHitBridgeSource = "intervention_agent_hit";

    public const string PlayerScoreHitBridgeSource = "intervention_score_hit";

    public const string NonEnemyDamagePrefixSource = "non_enemy_damage_prefix";

    private SiegeDestructiveChoiceProfile(
        SiegeAftermathResolutionKind aftermathKind,
        string assemblySource,
        string messageKey,
        string messageText,
        uint messageColor,
        string memoryTitle,
        string firstMemoryText,
        string repeatMemoryText)
    {
        AftermathKind = aftermathKind;
        AssemblySource = assemblySource;
        MessageKey = messageKey;
        MessageText = messageText;
        MessageColor = messageColor;
        MemoryTitle = memoryTitle;
        FirstMemoryText = firstMemoryText;
        RepeatMemoryText = repeatMemoryText;
    }

    public SiegeAftermathResolutionKind AftermathKind { get; }

    public string AssemblySource { get; }

    public string MessageKey { get; }

    public string MessageText { get; }

    public uint MessageColor { get; }

    public string MemoryTitle { get; }

    public string FirstMemoryText { get; }

    public string RepeatMemoryText { get; }

    public static SiegeDestructiveChoiceProfile BuildPlunder()
    {
        return new SiegeDestructiveChoiceProfile(
            aftermathKind: SiegeAftermathResolutionKind.Pillage,
            assemblySource: "plunder_started",
            messageKey: "plunder",
            messageText: "【士兵搜掠】搜掠开始：一部分士兵会分散盘问民众并索取财物。若你后续明确宽恕或宣抚，可回退为正向处置；也可升级为血洗。市场金库和物资会在离场后结算。",
            messageColor: 0xFFFFC46Bu,
            memoryTitle: "搜掠",
            firstMemoryText: "玩家已下令搜掠或收缴战利品，士兵开始向民众盘问并索取财物；该处置尚可被后续宽恕/救济/宣抚覆盖。",
            repeatMemoryText: "玩家继续维持搜掠/收缴财物处置，后续NPC应承认士兵正在执行搜掠。");
    }

    public static SiegeDestructiveChoiceProfile BuildMassacre()
    {
        return new SiegeDestructiveChoiceProfile(
            aftermathKind: SiegeAftermathResolutionKind.Devastate,
            assemblySource: string.Empty,
            messageKey: "massacre",
            messageText: "【攻城处置】血洗已触发：士兵将主动追击城内民众；多数民众会逃向预设藏身点/城门方向，少数民众、头人或携械者会反抗。本次处置不能再降回宽恕、救济或普通搜掠，但仍可继续升级为屠民迁殖；可随时按 TAB 离场，市场战利品在离场后结算。",
            messageColor: 0xFFFF7777u,
            memoryTitle: "血洗",
            firstMemoryText: string.Empty,
            repeatMemoryText: string.Empty);
    }

    public static string DescribeMassacreMemorySource(string triggerSource)
    {
        string source = triggerSource ?? string.Empty;
        if (source.IndexOf("直接攻击", StringComparison.OrdinalIgnoreCase) >= 0
            || source.IndexOf("主动攻击", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "由玩家亲自攻击NPC或平民触发";
        }

        if (source.IndexOf("屠民迁殖", StringComparison.OrdinalIgnoreCase) >= 0
            || source.IndexOf("迁殖", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "由玩家通过己方士兵下达屠民迁殖命令触发";
        }

        return "由玩家通过场景对话命令或谈崩触发";
    }

    public string BuildMassacreMemoryText(string triggerSource)
    {
        return DescribeMassacreMemorySource(triggerSource) + "，本次处置已升级为毁坏/血洗，不能再降回宽恕、救济或普通搜掠；若玩家后续明确命令清除原住民并迁入己方人口，仍可继续升级为屠民迁殖。";
    }

    public static string BuildPlayerHitMessage(string targetName)
    {
        return "【攻城处置】你击中了 " + NormalizeTargetName(targetName, "一名NPC") + "，本次入城处置进入血洗。";
    }

    public static string BuildPlayerHitTriggerDetail(string targetName)
    {
        return "玩家在攻城后亲自进城期间主动攻击了" + NormalizeTargetName(targetName, "一名NPC") + "，本次处置按血洗处理。";
    }

    private static string NormalizeTargetName(string targetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();
    }
}
