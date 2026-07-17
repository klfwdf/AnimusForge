using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only policy for revisable ordinary-prisoner dispositions.
/// Roster mutation is deferred until mission exit; live slaughter deaths remain irreversible.
/// </summary>
public static class SiegeCastleRegularDispositionStagingProfile
{
    public static bool IsStagedAction(SiegeCastleActionKind action)
        => SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action);

    public static bool IsDeferredRosterAction(SiegeCastleActionKind action)
        => IsStagedAction(action) && action != SiegeCastleActionKind.SlaughterPrisoners;

    public static string Describe(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => "释放普通战俘",
            SiegeCastleActionKind.SellPrisoners => "按原版酒馆价格贩卖普通战俘",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "自愿收编普通战俘",
            SiegeCastleActionKind.RecruitPrisonersForced => "强制收编普通战俘",
            SiegeCastleActionKind.LaborPrisonersVoluntary => "自愿劳役服刑",
            SiegeCastleActionKind.LaborPrisonersForced => "强制劳役服刑",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => "自愿担任教官",
            SiegeCastleActionKind.InstructorPrisonersForced => "强制担任教官",
            SiegeCastleActionKind.SlaughterPrisoners => "现场屠戮普通战俘",
            _ => "未指定处置"
        };
    }

    public static string BuildStagedMessage(
        SiegeCastleActionKind action,
        SiegeCastleActionKind previousAction,
        int survivingPrisoners,
        int revisionCount,
        int actuallyKilled = 0)
    {
        string change = previousAction == SiegeCastleActionKind.Unknown
            ? "已暂定“" + Describe(action) + "”"
            : "已改判：“" + Describe(previousAction) + "”→“" + Describe(action) + "”";
        return "【城堡处置】" + change + "；" + Count(survivingPrisoners)
            + " 名尚存普通战俘仍留在场景和主队俘虏名册，当前不会转队、释放、贩卖或淡出。"
            + "离场时才按最后一条有效命令结算"
            + (revisionCount > 0 ? "；本场已改判 " + Count(revisionCount) + " 次" : string.Empty)
            + (actuallyKilled > 0 ? "；此前已有 " + Count(actuallyKilled) + " 人实际死亡且不可复活" : string.Empty)
            + "。";
    }

    public static string BuildMemoryText(
        SiegeCastleActionKind action,
        SiegeCastleActionKind previousAction,
        int survivingPrisoners,
        int actuallyKilled,
        int revisionCount)
    {
        string order = previousAction == SiegeCastleActionKind.Unknown
            ? "玩家暂定将尚存普通战俘处置为“" + Describe(action) + "”"
            : "玩家推翻了先前的“" + Describe(previousAction) + "”命令，改判为“" + Describe(action) + "”";
        return order + "；当前仍有 " + Count(survivingPrisoners)
            + " 名普通战俘留在现场和俘虏名册，离场前不执行普通名册副作用。"
            + (revisionCount > 0 ? "所有在场者应记住玩家本场已经改判 " + Count(revisionCount) + " 次，并据此表现不安、疑虑、庆幸或敌意，但仍服从当前最新命令。" : string.Empty)
            + (actuallyKilled > 0 ? "此前已有 " + Count(actuallyKilled) + " 名战俘实际死亡，死亡不可撤销，新命令只适用于幸存者。" : string.Empty);
    }

    public static string BuildPromptState(
        SiegeCastleActionKind action,
        int revisionCount,
        int actuallyKilled)
    {
        if (!IsStagedAction(action))
        {
            return string.Empty;
        }
        return "【普通战俘当前暂定处置】" + Describe(action)
            + "。这不是已经完成的最终结算：尚存战俘仍在场景和俘虏名册中，NPC不得声称他们已经消失、转队、释放、售出或完成劳役/教官效果。"
            + "玩家可在离场前改口，最新一条通过授权门槛的普通战俘处置命令覆盖旧命令；所有在场NPC必须结合本场记忆承认前后变化，并可对玩家反复无常作出符合身份的情绪反应。"
            + (revisionCount > 0 ? "本场已经改判 " + Count(revisionCount) + " 次。" : string.Empty)
            + (actuallyKilled > 0 ? "已有 " + Count(actuallyKilled) + " 名普通战俘在现场实际死亡；他们不可复活，后续改判只作用于幸存者。" : string.Empty);
    }

    private static int Count(int value) => Math.Max(0, value);
}
