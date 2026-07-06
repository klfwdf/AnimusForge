namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free exit policy for castle GCCZ.
/// Castle aftermath must not inherit the town default "leave = plunder" behavior: non-destructive
/// military administration exits as stable occupation, while destructive/irreversible military orders
/// exit as devastation. Dedicated castle loot/recruit/prisoner side effects can be layered separately.
/// </summary>
public static class SiegeCastleMissionExitOutcomeProfile
{
    public const string NoOrderExitTriggerSource = "城堡军务离场：未下达额外命令";

    public const string NoOrderExitTriggerDetail = "玩家进入城堡战后军务场景后未下达额外军务命令便离场；按稳定占领结算，不触发城镇默认搜掠。";

    public const string AdministrativeExitTriggerSource = "城堡军务离场：非毁灭军务";

    public const string AdministrativeExitTriggerDetail = "玩家已下达城堡军务处置；非毁灭性军务按稳定占领结算，资源、赎金、收编和修墙由城堡专用状态记录，不借用城镇平民搜掠。";

    public const string DestructiveExitTriggerSource = "城堡军务离场：毁灭军务";

    public const string DestructiveExitTriggerDetail = "玩家已下达屠戮守军、处决领主或等同不可逆军务命令；城堡离场按毁灭性战后处置结算。";

    public static SiegeMissionExitOutcomeDecision Resolve(SiegeCastleAftermathStateSnapshot state, bool hasPendingAftermath)
    {
        if (hasPendingAftermath)
        {
            return SiegeMissionExitOutcomeDecision.None;
        }

        if (state.IrreversibleOrderLocked || state.DestructiveOrderIssued)
        {
            return Mark(SiegeAftermathResolutionKind.Devastate, DestructiveExitTriggerSource, DestructiveExitTriggerDetail);
        }

        if (state.ActionCount <= 0)
        {
            return Mark(SiegeAftermathResolutionKind.ShowMercy, NoOrderExitTriggerSource, NoOrderExitTriggerDetail);
        }

        return Mark(SiegeAftermathResolutionKind.ShowMercy, AdministrativeExitTriggerSource, AdministrativeExitTriggerDetail);
    }

    public static string BuildDiagnosticText(SiegeMissionExitOutcomeDecision decision)
    {
        if (!decision.HasDecision)
        {
            return "decision=none";
        }

        return "decision=" + decision.AftermathKind
            + " source=" + decision.TriggerSource
            + " startsPlunder=" + decision.ShouldStartPlunder;
    }

    private static SiegeMissionExitOutcomeDecision Mark(SiegeAftermathResolutionKind aftermathKind, string triggerSource, string triggerDetail)
    {
        return new SiegeMissionExitOutcomeDecision(
            hasDecision: true,
            shouldStartPlunder: false,
            aftermathKind: aftermathKind,
            triggerSource: triggerSource,
            triggerDetail: triggerDetail);
    }
}
