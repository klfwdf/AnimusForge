using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Player-facing castle-only action result wording.</summary>
public static class SiegeCastleActionOutcomeTextProfile
{
    public const uint SuccessColor = 0xFFB6F7A8u;
    public const uint WarningColor = 0xFFFFD27Fu;
    public const uint DangerColor = 0xFFFF7A7Au;

    public static string BuildCareMessage(int affected, int trustDelta)
        => "【城堡处置】已按兵种等级向 " + Count(affected)
            + " 名普通战俘发放物资并禁止虐待；俘虏信任 " + Signed(trustDelta) + "。";

    public static string BuildCareInsufficientMessage()
        => "【城堡处置】随军食物不足，善待俘虏流程未结算，也未扣除物资。";

    public static string BuildArmamentMessage(int affected, int items, int kinds, int gold, bool lord)
        => "【城堡处置】已收缴" + (lord ? "该领主" : Count(affected) + " 名普通战俘")
            + "的军械与随身财物，直接送入背包：物品 " + Count(items)
            + " 件 / " + Count(kinds) + " 类，金币 " + Count(gold) + "；不打开战利品界面。";

    public static string BuildTerminalMessage(SiegeCastleActionKind action, int affected, int remaining, int gold = 0)
    {
        string detail = action switch
        {
            SiegeCastleActionKind.ReleasePrisoners => "释放了 " + Count(affected) + " 名普通战俘",
            SiegeCastleActionKind.SellPrisoners => "贩卖了 " + Count(affected) + " 名普通战俘，获得 " + Count(gold) + " 金币",
            SiegeCastleActionKind.RecruitPrisonersVoluntary => "自愿收编了 " + Count(affected) + " 名普通战俘",
            SiegeCastleActionKind.RecruitPrisonersForced => "强制收编了 " + Count(affected) + " 名普通战俘",
            SiegeCastleActionKind.LaborPrisonersVoluntary => "按自愿劳役方案处置 " + Count(affected) + " 名普通战俘，地方效果将在离场时直接结算",
            SiegeCastleActionKind.LaborPrisonersForced => "按强制劳役方案处置 " + Count(affected) + " 名普通战俘，地方效果将在离场时直接结算",
            SiegeCastleActionKind.InstructorPrisonersVoluntary => "按自愿教官方案处置 " + Count(affected) + " 名普通战俘，训练效果将在离场时直接结算",
            SiegeCastleActionKind.InstructorPrisonersForced => "按强制教官方案处置 " + Count(affected) + " 名普通战俘，训练效果将在离场时直接结算",
            _ => "处理了 " + Count(affected) + " 名普通战俘"
        };
        return "【城堡处置】已" + detail + "；本次带入者中仍有 " + Count(remaining)
            + " 名普通战俘保持俘虏身份。退出时仍且只走一次原版宽恕。";
    }

    public static string BuildSlaughterStartedMessage(int targets)
        => "【城堡处置】屠戮命令已下达：编队1的己方士兵必须在场景内实际杀死 "
            + Count(targets) + " 名普通战俘；尚未死亡者不会从名册扣除。军械已自动收缴一次。";

    public static string BuildSlaughterKillMessage(int killedTotal, int remaining)
        => "【城堡处置】普通战俘实际死亡累计 " + Count(killedTotal)
            + " 人；仍有 " + Count(remaining) + " 人待实际击杀。";

    public static string BuildLordProcessMessage(SiegeCastleActionKind action, string lordName, int items = 0)
        => action == SiegeCastleActionKind.ReceiveArmaments
            ? "【城堡处置】已收缴 " + Name(lordName) + " 的武器与盔甲，共 " + Count(items) + " 件，直接送入背包。"
            : "【城堡处置】已向 " + Name(lordName) + " 提供物资并禁止虐待；该流程只针对这名领主。";

    public static string BuildLordRecruitmentMessage(
        SiegeCastleLordRecruitmentBranch branch,
        string lordName,
        string statusText)
        => "【城堡处置】" + Name(lordName) + "："
            + SiegeCastleLordRecruitmentBranchProfile.Describe(branch)
            + (string.IsNullOrWhiteSpace(statusText) ? "。" : "；" + statusText.Trim());

    public static string BuildDeferredLordExecutionMessage(string lordName)
        => "【城堡处置】" + Name(lordName)
            + " 的处决动画与死亡结算接口按设计暂未启用，本次未改变其生死或俘虏状态。";

    private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    private static int Count(int value) => Math.Max(0, value);
    private static string Name(string value) => string.IsNullOrWhiteSpace(value) ? "该被俘领主" : value.Trim();
}
