namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free gate for castle aftermath action tags.
/// It prevents later conciliatory/stabilizing castle orders from mutating state after an irreversible
/// destructive command has already locked the military situation, while still allowing further
/// destructive orders such as executing a captive lord after slaughtering the surrendered garrison.
/// It also protects mutually exclusive military process choices.
/// </summary>
public static class SiegeCastleActionGateProfile
{
    public const string MemoryTitle = "城堡军务锁定";

    public const string DiagnosticCategory = "CastleActionGate";

    public const string AllowedReason = "allowed";

    public const string IrreversibleLockReason = "irreversible_order_locked";

    public const string UnknownActionReason = "unknown_castle_action";

    public const string HonorArmoryMutualExclusionReason = "honor_captives_armory_mutual_exclusion";

    public static SiegeCastleActionGateDecision Evaluate(
        SiegeCastleAftermathStateSnapshot current,
        SiegeCastleAftermathActionKind nextAction,
        SiegeCastleAftermathEffectProfile nextEffect)
    {
        if (nextAction == SiegeCastleAftermathActionKind.Unknown)
        {
            return new SiegeCastleActionGateDecision(false, UnknownActionReason, current.IrreversibleOrderLocked || current.DestructiveOrderIssued);
        }

        if ((nextAction == SiegeCastleAftermathActionKind.HonorCaptives && current.ArmorySeized)
            || (nextAction == SiegeCastleAftermathActionKind.SeizeArmory && current.HonorCaptivesChosen))
        {
            return new SiegeCastleActionGateDecision(false, HonorArmoryMutualExclusionReason, current.IrreversibleOrderLocked || current.DestructiveOrderIssued);
        }

        bool locked = current.IrreversibleOrderLocked || current.DestructiveOrderIssued;
        if (!locked || nextEffect.IsDestructive || nextEffect.IsIrreversible)
        {
            return new SiegeCastleActionGateDecision(true, AllowedReason, locked);
        }

        return new SiegeCastleActionGateDecision(false, IrreversibleLockReason, locked);
    }

    public static string BuildBlockedMessageText(SiegeCastleAftermathActionKind blockedAction)
    {
        if (blockedAction == SiegeCastleAftermathActionKind.HonorCaptives || blockedAction == SiegeCastleAftermathActionKind.SeizeArmory)
        {
            return "【城堡处置】“优待战俘”和“接收军械”互斥，“" + SiegeCastleAftermathProfile.GetActionLabel(blockedAction) + "”未改变城堡状态。";
        }

        return "【城堡处置】不可逆军务已锁定，“" + SiegeCastleAftermathProfile.GetActionLabel(blockedAction) + "”未改变城堡状态。";
    }

    public static string BuildBlockedMemoryText(SiegeCastleAftermathActionKind blockedAction, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        if (blockedAction == SiegeCastleAftermathActionKind.HonorCaptives || blockedAction == SiegeCastleAftermathActionKind.SeizeArmory)
        {
            return safeCastleName + "已有互斥城堡流程选择；后续“" + SiegeCastleAftermathProfile.GetActionLabel(blockedAction) + "”被视为无效后续标签，只记录拦截，不再改变俘虏信任、己方士气、军械或战利品状态。";
        }

        return safeCastleName + "已有不可逆城堡军务命令，后续“" + SiegeCastleAftermathProfile.GetActionLabel(blockedAction) + "”被视为无效后续标签，只记录拦截，不再改变领主好感、城堡忠诚/治安、村庄产出、赎金、军械或收编俘虏状态。";
    }

    public static string BuildDiagnosticText(SiegeCastleActionGateDecision decision, SiegeCastleAftermathActionKind action)
    {
        return "action=" + action
            + " allowed=" + decision.Allowed
            + " reason=" + decision.Reason
            + " locked=" + decision.LockActive;
    }
}

public readonly struct SiegeCastleActionGateDecision
{
    public SiegeCastleActionGateDecision(bool allowed, string reason, bool lockActive)
    {
        Allowed = allowed;
        Reason = reason ?? string.Empty;
        LockActive = lockActive;
    }

    public bool Allowed { get; }

    public string Reason { get; }

    public bool LockActive { get; }
}
