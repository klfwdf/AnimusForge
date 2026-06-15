namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for GCCZ cultural repopulation request handling.
/// AF adapters resolve Bannerlord cultures, mutate settlements, and run mission/combat side effects.
/// </summary>
public sealed class SiegeCulturalRepopulationProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const string TargetValidationMessage = "【攻城处置】屠民迁殖只能与己方士兵对话触发，不能由平民或其他NPC触发。";

    public const string PlayerHeroCultureSourceLabel = "玩家角色文化";

    public const string PlayerKingdomCultureSourceLabel = "玩家所属王国文化";

    public const string PlayerClanCultureSourceLabel = "玩家家族文化";

    public const string PlayerCultureFallbackLabel = "玩家文化";

    public const string VictoryAlreadyReachedApplySource = "victory_already_reached";

    public const string MassacreVictoryApplySource = "massacre_victory";

    public const string FinalizeAftermathApplySource = "finalize_aftermath";

    public const string DirectMassacreLootMessageApplySource = "direct_massacre_loot_message";

    public SiegeAftermathResolutionKind AftermathKind { get; } = SiegeAftermathResolutionKind.Devastate;

    public string MassacreTriggerSource { get; } = "场景对话屠民迁殖触发血洗";

    public string MassacreTriggerDetail { get; } = "玩家通过己方士兵直接下令屠民迁殖；无论这是开局直接殖民还是血洗后的继续升级，士兵都会按血洗方式屠戮原住民，离场按毁坏/迁殖结算。";

    public string MemoryTitle { get; } = "殖民";

    public uint PendingMessageColor { get; } = 0xFFFF7777u;

    public uint CompletedMessageColor { get; } = 0xFFFF7777u;

    public string BuildRequestMemoryText(string targetCultureText)
    {
        return "玩家通过己方士兵触发屠民迁殖，要求清除原住民并把定居点强行改为 " + NormalizeTargetCultureText(targetCultureText) + "；这是最高级处置，可在开局直接触发，也可由血洗继续升级而来。";
    }

    public string BuildPendingMessageText(string targetCultureText)
    {
        return "【攻城处置】屠民迁殖已列入处置：这是最高级处置，离场结算后此地将强行改为 " + NormalizeTargetCultureText(targetCultureText) + "。";
    }

    public string BuildCompletedMessageText(string settlementName, string targetCultureText, string notableResultText)
    {
        return "【攻城处置】屠民迁殖完成：" + NormalizeSettlementName(settlementName) + " 已被强行改为 " + NormalizeTargetCultureText(targetCultureText) + "。" + (notableResultText ?? string.Empty);
    }

    public string BuildCompletedNotableResultText(bool isTown, int killedNotables, int spawnedNotables)
    {
        if (!isTown)
        {
            return string.Empty;
        }

        return " 旧要人已处死 " + killedNotables + " 人，新要人已扶立 " + spawnedNotables + " 人。";
    }

    public static string BuildTargetCultureMessageText(string cultureName, string sourceLabel)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return NormalizeCultureSourceLabel(sourceLabel);
        }

        string trimmedCultureName = cultureName.Trim();
        if (string.IsNullOrWhiteSpace(sourceLabel))
        {
            return trimmedCultureName;
        }

        return trimmedCultureName + "（" + sourceLabel.Trim() + "）";
    }

    public static string NormalizeCultureSourceLabel(string sourceLabel)
    {
        return string.IsNullOrWhiteSpace(sourceLabel) ? PlayerCultureFallbackLabel : sourceLabel.Trim();
    }

    private static string NormalizeTargetCultureText(string targetCultureText)
    {
        return string.IsNullOrWhiteSpace(targetCultureText) ? "目标文化" : targetCultureText.Trim();
    }

    private static string NormalizeSettlementName(string settlementName)
    {
        return string.IsNullOrWhiteSpace(settlementName) ? "该定居点" : settlementName.Trim();
    }
}
