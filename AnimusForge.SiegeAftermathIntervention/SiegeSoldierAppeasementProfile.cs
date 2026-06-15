namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for GCCZ soldier appeasement and its fallback morale penalty.
/// AF adapters apply Bannerlord party morale, UI, and memory side effects.
/// </summary>
public sealed class SiegeSoldierAppeasementProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const string TargetValidationMessage = "【攻城处置】安抚军心必须对己方入城士兵进行。";

    public int MoralePenalty { get; } = 20;

    public string NeedMemoryTitle { get; } = "军心";

    public string NeedMessageText { get; } = "【攻城处置】部分士兵对放弃战利品有些不满；若离场前安抚己方士兵，可避免主队士气 -20。";

    public uint NeedMessageColor { get; } = 0xFFFFD27Fu;

    public string AppeasementMemoryTitle { get; } = "安兵";

    public string AppeasementMemoryText { get; } = "玩家已经安抚己方士兵，承诺军纪、补偿或日后战利安排，避免因宽恕/救济/宣抚/盟誓路线扣除士气。";

    public string AppeasementMessageText { get; } = "【攻城处置】你安抚了士兵，军中对放弃搜掠的怨气被压下，本次不会扣除士气。";

    public uint AppeasementMessageColor { get; } = 0xFFB6F7A8u;

    public string PenaltyMemoryTitle { get; } = "军心";

    public string PenaltyMemoryText { get; } = "玩家未在离场结算前安抚因放弃战利品而不满的士兵，主队士气扣除20。";

    public string PenaltyMessageText { get; } = "【攻城处置】士兵对放弃战利品仍有怨气，主队士气 -20。";

    public uint PenaltyMessageColor { get; } = 0xFFFF7777u;

    public string BuildNeedMemoryText(string outcomeName)
    {
        string label = string.IsNullOrWhiteSpace(outcomeName) ? "宽恕处置" : outcomeName.Trim();
        return "玩家选择" + label + "触发部分士兵对放弃战利品的含蓄不满；若离场前对己方士兵安抚并触发安兵标签，可避免士气惩罚。";
    }
}
