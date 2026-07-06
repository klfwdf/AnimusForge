namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free authority gate for castle aftermath tags.
/// Player-issued commands are always allowed; NPC-generated tags are checked against the current
/// castle role so ordinary soldiers do not accidentally trigger lord-only decisions.
/// </summary>
public static class SiegeCastleActionAuthorityProfile
{
    public const string MemoryTitle = "城堡军务权限拦截";

    public const string DiagnosticCategory = "CastleActionAuthority";

    public const string PlayerCommandReason = "player_command_context";

    public const string AllowedRoleReason = "role_allowed";

    public const string BlockedRoleReason = "role_not_allowed";

    public static SiegeCastleActionAuthorityDecision Evaluate(
        SiegeCastleAftermathActionKind action,
        SiegeCastleAgentRoleKind role,
        bool playerCommandContext)
    {
        if (playerCommandContext)
        {
            return new SiegeCastleActionAuthorityDecision(true, PlayerCommandReason);
        }

        return IsRoleAllowed(action, role)
            ? new SiegeCastleActionAuthorityDecision(true, AllowedRoleReason)
            : new SiegeCastleActionAuthorityDecision(false, BlockedRoleReason);
    }

    public static bool IsRoleAllowed(SiegeCastleAftermathActionKind action, SiegeCastleAgentRoleKind role)
    {
        if (role == SiegeCastleAgentRoleKind.Unknown)
        {
            return true;
        }

        switch (action)
        {
            case SiegeCastleAftermathActionKind.DemandRansom:
            case SiegeCastleAftermathActionKind.RecruitLord:
            case SiegeCastleAftermathActionKind.ExecuteLord:
                return role == SiegeCastleAgentRoleKind.CaptiveLordOrCommander;
            case SiegeCastleAftermathActionKind.HonorCaptives:
            case SiegeCastleAftermathActionKind.RecruitGarrison:
            case SiegeCastleAftermathActionKind.LaborPrisoners:
            case SiegeCastleAftermathActionKind.SellPrisoners:
                return role == SiegeCastleAgentRoleKind.CaptiveLordOrCommander
                    || role == SiegeCastleAgentRoleKind.CaptivePrisoner
                    || role == SiegeCastleAgentRoleKind.AlliedSoldierRepresentative
                    || role == SiegeCastleAgentRoleKind.PrisonerGuard;
            case SiegeCastleAftermathActionKind.SlaughterGarrison:
                return role == SiegeCastleAgentRoleKind.CaptivePrisoner
                    || role == SiegeCastleAgentRoleKind.AlliedSoldierRepresentative
                    || role == SiegeCastleAgentRoleKind.PrisonerGuard;
            case SiegeCastleAftermathActionKind.SeizeArmory:
                return role == SiegeCastleAgentRoleKind.CaptiveLordOrCommander
                    || role == SiegeCastleAgentRoleKind.CaptivePrisoner
                    || role == SiegeCastleAgentRoleKind.AlliedSoldierRepresentative
                    || role == SiegeCastleAgentRoleKind.PrisonerGuard
                    || role == SiegeCastleAgentRoleKind.CastleStaffOrWitness;
            default:
                return false;
        }
    }

    public static string BuildBlockedMessageText(SiegeCastleAftermathActionKind action)
    {
        return "【城堡处置】当前对象无权触发“" + SiegeCastleAftermathProfile.GetActionLabel(action) + "”，已忽略该标签。";
    }

    public static string BuildBlockedMemoryText(SiegeCastleAftermathActionKind action, SiegeCastleAgentRoleKind role, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return safeCastleName + "出现越权城堡军务标签：“" + SiegeCastleAftermathProfile.GetActionLabel(action) + "”；当前对象身份为“"
            + SiegeCastleAgentRoleProfile.GetRoleLabel(role) + "”，该标签未改变城堡军务状态。";
    }

    public static string BuildDiagnosticText(SiegeCastleActionAuthorityDecision decision, SiegeCastleAftermathActionKind action, SiegeCastleAgentRoleKind role, bool playerCommandContext)
    {
        return "action=" + action
            + " role=" + role
            + " allowed=" + decision.Allowed
            + " reason=" + decision.Reason
            + " playerCommand=" + playerCommandContext;
    }
}

public readonly struct SiegeCastleActionAuthorityDecision
{
    public SiegeCastleActionAuthorityDecision(bool allowed, string reason)
    {
        Allowed = allowed;
        Reason = reason ?? string.Empty;
    }

    public bool Allowed { get; }

    public string Reason { get; }
}
