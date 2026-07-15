namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Player-facing wording for a non-decisive soldier proposal awaiting player approval.
/// </summary>
public static class SiegeCastleSoldierProposalProfile
{
    public const uint PendingMessageColor = 0xFFFFD27Fu;

    public static string BuildPendingMessage(SiegeCastlePrisonerDispositionKind disposition)
    {
        return "【城堡处置】士兵提出“"
            + SiegeCastlePrisonerDispositionKindProfile.Describe(disposition)
            + "”，尚未执行；需要玩家明确同意。";
    }

    public static string BuildPendingContext(SiegeCastlePrisonerDispositionKind disposition)
    {
        return disposition == SiegeCastlePrisonerDispositionKind.None
            ? "当前没有等待玩家确认的士兵提议。"
            : "当前说话者此前提出了“"
                + SiegeCastlePrisonerDispositionKindProfile.Describe(disposition)
                + "”，尚未执行；只有玩家本轮明确同意后才能结算。";
    }
}
