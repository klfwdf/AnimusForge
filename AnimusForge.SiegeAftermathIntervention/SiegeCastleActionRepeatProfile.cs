namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free duplicate gate for castle aftermath action tags.
/// The AF bridge owns the per-mission set of applied actions; this profile decides whether a
/// repeated model/NPC tag should be ignored so castle military state is not stacked by chatter.
/// </summary>
public static class SiegeCastleActionRepeatProfile
{
    public const string MemoryTitle = "城堡军务重复标签";

    public const string DiagnosticCategory = "CastleActionRepeat";

    public const string FirstApplicationReason = "first_application";

    public const string DuplicateReason = "duplicate_castle_action";

    public static SiegeCastleActionRepeatDecision Evaluate(SiegeCastleAftermathActionKind action, bool alreadyApplied)
    {
        if (action == SiegeCastleAftermathActionKind.Unknown)
        {
            return new SiegeCastleActionRepeatDecision(false, "unknown_castle_action");
        }

        return alreadyApplied
            ? new SiegeCastleActionRepeatDecision(false, DuplicateReason)
            : new SiegeCastleActionRepeatDecision(true, FirstApplicationReason);
    }

    public static string BuildDuplicateMessageText(SiegeCastleAftermathActionKind action)
    {
        return "【城堡处置】“" + SiegeCastleAftermathProfile.GetActionLabel(action) + "”已执行过，本次重复标签未再次叠加。";
    }

    public static string BuildDuplicateMemoryText(SiegeCastleAftermathActionKind action, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return safeCastleName + "重复出现“" + SiegeCastleAftermathProfile.GetActionLabel(action) + "”标签；为避免 NPC 群聊或后处理重复叠加，只记录一次有效军务状态。";
    }

    public static string BuildDiagnosticText(SiegeCastleActionRepeatDecision decision, SiegeCastleAftermathActionKind action)
    {
        return "action=" + action
            + " allowed=" + decision.Allowed
            + " reason=" + decision.Reason;
    }
}

public readonly struct SiegeCastleActionRepeatDecision
{
    public SiegeCastleActionRepeatDecision(bool allowed, string reason)
    {
        Allowed = allowed;
        Reason = reason ?? string.Empty;
    }

    public bool Allowed { get; }

    public string Reason { get; }
}
