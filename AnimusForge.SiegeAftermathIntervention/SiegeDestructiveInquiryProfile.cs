namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free wording and throttling constants for soldier questions after a bad civilian-side signal.
/// AF adapters still choose the live soldier and trigger the immediate scene reaction.
/// </summary>
public static class SiegeDestructiveInquiryProfile
{
    public const float InquiryCooldownSeconds = 18f;

    public const string InvalidSoldierMediatedTagReason = "invalid_soldier_mediated_destructive_tag";

    public const string CivilianRobberyReason = "civilian_robbery";

    public const string FactPrefix = "【攻城处置士兵请示】";

    public static string BuildInquiryFact(string sourceSpeakerName, string reason)
    {
        string speaker = string.IsNullOrWhiteSpace(sourceSpeakerName) ? "附近民众" : sourceSpeakerName.Trim();
        string reasonText = NormalizeReason(reason);
        return FactPrefix + speaker + " 的对话已经出现" + reasonText + "。你作为玩家己方入城士兵，听到了这个苗头；请主动向玩家请示是否只做局部抢取、是否扩大为全城搜掠，或是否升级为血洗/迁殖。现在只是请示，不得宣布已经执行，不得输出任何方括号动作标签。";
    }

    private static string NormalizeReason(string reason)
    {
        string value = (reason ?? string.Empty).Trim();
        if (value == CivilianRobberyReason)
        {
            return "索取财物/抢取物资";
        }

        if (value == InvalidSoldierMediatedTagReason)
        {
            return "搜掠、血洗或迁殖相关危险信号";
        }

        return string.IsNullOrWhiteSpace(value) ? "危险苗头" : value;
    }
}
