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

    public static string BuildLordSaleMessage(string lordName, int gold)
        => "【城堡处置】已将 " + Name(lordName)
            + " 交给赎金经纪人，严格按原版酒馆实时赎卖价获得 " + Count(gold)
            + " 金币；该领主已解除俘虏并离开现场。";

    public static string BuildLordSaleFailedMessage(string lordName, string reasonCode)
        => "【城堡处置】未能贩卖 " + Name(lordName)
            + "；未结算金币或最终处置。" + DescribeLordSaleFailure(reasonCode);

    public static string BuildLordExecutionQueuedMessage(string lordName)
        => "【城堡处置】已对 " + Name(lordName)
            + " 下达处刑命令，正在打开原版处刑确认；取消不会改变其生死或俘虏状态。";

    public static string BuildLordExecutionCancelledMessage(string lordName)
        => "【城堡处置】已取消对 " + Name(lordName)
            + " 的处刑；该领主仍存活并保持俘虏身份，可重新交涉。";

    public static string BuildLordExecutionFailedMessage(string lordName)
        => "【城堡处置】未能打开 " + Name(lordName)
            + " 的原版处刑确认，未结算死亡；可稍后重试，详情已写入日志。";

    public static string BuildLordExecutionCompletedMessage(
        string lordName,
        bool deferredByMapEvent,
        bool sceneDeathApplied)
        => "【城堡处置】" + Name(lordName)
            + " 已由玩家确认处刑"
            + (sceneDeathApplied ? "，并在城堡场景内当场倒地" : "；场景倒地表现未成功，死亡结算仍已登记")
            + (deferredByMapEvent
                ? "；原版战后处决将在当前攻城遭遇结束时完成角色状态结算。"
                : "；原版角色死亡、关系与家族后果已结算。");

    private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    private static int Count(int value) => Math.Max(0, value);
    private static string Name(string value) => string.IsNullOrWhiteSpace(value) ? "该被俘领主" : value.Trim();

    private static string DescribeLordSaleFailure(string reasonCode)
    {
        return (reasonCode ?? string.Empty).Trim() switch
        {
            "lord_sale_roster_unavailable" => "主队俘虏名册当前不可用。",
            "lord_sale_target_invalid" => "目标已死亡或不具备可赎卖身份。",
            "lord_not_selected_main_party_prisoner" => "目标不再是本次带入且由玩家主队扣押的领主。",
            "lord_prisoner_locked_from_ransom" => "该领主被原版任务或名册锁定，酒馆同样不能赎卖。",
            "ransom_value_model_unavailable" => "原版赎金计价模型当前不可用。",
            "vanilla_lord_sale_did_not_release_target" => "原版赎卖动作未能解除该领主的俘虏状态。",
            "" => "目标当前不可赎卖。",
            _ => "原版赎卖动作失败，详情已写入日志。"
        };
    }
}
