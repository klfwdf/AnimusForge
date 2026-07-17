namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only policy for one allied witness reaction after a meaningful prisoner action.
/// Ordinary actions create unrest only when the witness actually voices discontent;
/// prisoner recruitment always carries the explicit stacked recruitment penalty.
/// </summary>
public static class SiegeCastleSoldierReactionProfile
{
    public const int ExpressedDiscontentPenalty = 30;
    public const int VoluntaryRecruitmentPenalty = 30;
    public const int ForcedRecruitmentPenalty = 60;
    public const int VoluntaryRecruitmentTotalPenalty = ExpressedDiscontentPenalty + VoluntaryRecruitmentPenalty;
    public const int ForcedRecruitmentTotalPenalty = ExpressedDiscontentPenalty + ForcedRecruitmentPenalty;

    public const uint NeedMessageColor = 0xFFFFD27Fu;
    public const uint AppeasedMessageColor = 0xFFB6F7A8u;

    public const string NeedMemoryTitle = "城堡战俘处置后的军心不满";
    public const string AppeasementMemoryTitle = "城堡安兵";
    public const string PenaltyMemoryTitle = "城堡战俘处置军心受损";

    public static bool CanReactTo(SiegeCastleActionKind action)
    {
        return SiegeCastleActionKindProfile.IsProcess(action)
            || SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action)
            || SiegeCastleActionKindProfile.IsLordTerminal(action);
    }

    public static bool AlwaysCreatesConcern(SiegeCastleActionKind action)
        => SiegeCastleActionKindProfile.IsRecruitment(action);

    public static int ResolvePendingMoralePenalty(
        SiegeCastleActionKind action,
        bool witnessExpressedDiscontent)
    {
        if (action == SiegeCastleActionKind.RecruitPrisonersVoluntary)
        {
            return VoluntaryRecruitmentTotalPenalty;
        }
        if (action == SiegeCastleActionKind.RecruitPrisonersForced)
        {
            return ForcedRecruitmentTotalPenalty;
        }
        return witnessExpressedDiscontent && CanReactTo(action)
            ? ExpressedDiscontentPenalty
            : 0;
    }

    public static string BuildWitnessFact(
        string playerName,
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        bool sameCulture)
    {
        string commander = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
        return "【城堡处置见证反应】" + commander + "刚刚决定对 "
            + Clamp(affectedRegularPrisoners) + " 名战俘执行“" + DescribeConcernAction(action)
            + "”。请只按你自己的性格、军纪观、双方文化和现场经历自由评论；可以赞同、中立、疑虑或不满，但不得抗命。"
            + (sameCulture ? "你与受处置战俘中至少一部分同文化，应真实考虑这种身份冲突。" : string.Empty)
            + (SiegeCastleActionKindProfile.IsRecruitment(action)
                ? "把战败敌兵编入本军必然使随军士兵产生现实军心压力，你的发言应明确表现至少一种担忧或不满。"
                : "不要为了触发机制而强行不满；若你认可该决定，可以直接赞同。")
            + "这只是一次自由发言，不得提出或结算新的战俘处置。";
    }

    public static string BuildNeedMessage(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty)
    {
        return "【城堡处置】随军士兵对“" + DescribeConcernAction(action) + "”"
            + Clamp(affectedRegularPrisoners) + " 名战俘表达了不满；离场前可直接安抚，否则部队士气 -"
            + Clamp(pendingPenalty) + "。";
    }

    public static string BuildNeedMemoryText(
        SiegeCastleActionKind action,
        int affectedRegularPrisoners,
        int pendingPenalty)
    {
        return "玩家在城堡处置现场决定以“" + DescribeConcernAction(action) + "”处理 "
            + Clamp(affectedRegularPrisoners) + " 名战败守军；一名随军士兵明确表达不满，当前待安抚士气惩罚为 "
            + Clamp(pendingPenalty) + "。";
    }

    public static string BuildAppeasementMessage(int avoidedPenalty)
    {
        return "【城堡处置】己方士兵接受了玩家的解释、补偿与军令，已免除本次待结算的 -"
            + Clamp(avoidedPenalty) + " 士气。";
    }

    public static string BuildAppeasementMemoryText(int avoidedPenalty)
    {
        return "玩家在城堡处置现场直接安抚了表达不满的随军士兵，避免部队士气降低 "
            + Clamp(avoidedPenalty) + "。";
    }

    public static string BuildPenaltyMemoryText(int penalty)
    {
        return "玩家离开城堡前没有安抚对战俘处置表达不满的随军士兵，部队士气降低 "
            + Clamp(penalty) + "。";
    }

    public static string DescribeConcernAction(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.TreatPrisoners => "善待俘虏",
            SiegeCastleActionKind.ReceiveArmaments => "收缴俘虏军械",
            SiegeCastleActionKind.ReleasePrisoners => "释放战俘",
            SiegeCastleActionKind.SlaughterPrisoners => "屠戮战俘",
            SiegeCastleActionKind.SellPrisoners => "贩卖战俘",
            SiegeCastleActionKind.LaborPrisonersVoluntary => "自愿劳役",
            SiegeCastleActionKind.LaborPrisonersForced => "强制劳役",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => "自愿担任教官",
            SiegeCastleActionKind.InstructorPrisonersForced => "强迫担任教官",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "自愿收编",
            SiegeCastleActionKind.RecruitPrisonersForced => "强制收编",
            SiegeCastleActionKind.RecruitLord => "招揽被俘领主",
            SiegeCastleActionKind.SellLord => "贩卖被俘领主",
            SiegeCastleActionKind.ExecuteLord => "处决被俘领主",
            _ => "处置战俘"
        };
    }

    private static int Clamp(int value) => value < 0 ? 0 : value;
}
