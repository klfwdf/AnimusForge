using System;

namespace AnimusForge.CoupSystem;

// Final confirmed facts only. One pass over the session records, once per outcome receipt.
internal static class CoupOutcomeReport
{
    internal static bool CanReport(CoupSession session, bool success) => session != null && session.CasualtiesCommitted
        && (success ? session.KingSubdued && session.RulingClanCommitted && session.TownCommitted && session.CustodyCommitted
            : session.DefectionCommitted && session.WithdrawalCommitted);

    internal static string BuildPlayerVictory(CoupSession session, string kingdom, string ruler,
        string town, string ownerClan, string king, bool kingStillHeld)
    {
        if (session?.HasConfirmedVictory != true) throw new InvalidOperationException("Coup settlement is not fully confirmed; the coronation report cannot be shown.");
        int alliedKilled = 0, alliedWounded = 0, defenderKilled = 0, defenderWounded = 0;
        foreach (CoupTroopRecord troop in session.Troops)
        {
            if (troop.Role == CoupTroopRole.Ally)
            {
                if (troop.Killed) alliedKilled++;
                if (troop.Wounded) alliedWounded++;
            }
            else
            {
                if (troop.Killed) defenderKilled++;
                if (troop.Wounded) defenderWounded++;
            }
        }
        string custody = session.Disposition == CoupKingDisposition.Capture
            ? (kingStillHeld ? CoupUiText.FormerRulerHeld : CoupUiText.FormerRulerNoLongerHeld)
            : CoupUiText.FormerRulerReleased;
        return CoupUiText.VictoryReport(kingdom, ruler, town, ownerClan, king, custody,
            alliedKilled, alliedWounded, defenderKilled, defenderWounded);
    }

    internal static string Build(CoupSession session, string player, string king, string town, string kingdom,
        bool success, bool captured, bool atWarWithOriginalKingdom)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        int allies = 0, hallAllies = 0, alliedKilled = 0, alliedWounded = 0, defenderKilled = 0, defenderWounded = 0;
        foreach (CoupTroopRecord troop in session.Troops)
        {
            if (troop.Role == CoupTroopRole.Ally)
            {
                allies++;
                if (troop.HallSelected) hallAllies++;
                if (troop.Killed) alliedKilled++;
                if (troop.Wounded) alliedWounded++;
            }
            else
            {
                if (troop.Killed) defenderKilled++;
                if (troop.Wounded) defenderWounded++;
            }
        }
        string process;
        bool hallReached = session.HallEntered || session.KingSubdued;
        bool gateReached = session.GateBreached || hallReached;
        if (!session.SceneEntered && !gateReached) process = CoupSourceTexts.Get("ReportNoEntry");
        else
        {
            var parts = new System.Collections.Generic.List<string> { CoupSourceTexts.Get("ReportStreet") };
            if (gateReached) parts.Add(CoupSourceTexts.Get("ReportGate"));
            if (hallReached) parts.Add(CoupSourceTexts.Format("ReportHall", hallAllies));
            if (session.KingSubdued) parts.Add(CoupSourceTexts.Get("ReportSubdued"));
            process = string.Join(CoupSourceTexts.Get("ReportListSeparator"), parts) + CoupSourceTexts.Get("ReportSentenceEnd");
        }
        string result = success ? CoupSourceTexts.Format(captured ? "ReportSuccessCaptured" : "ReportSuccessReleased", player, kingdom, town)
            : CoupSourceTexts.Get(atWarWithOriginalKingdom ? "ReportFailureWar" : "ReportFailureNoWar");
        return CoupSourceTexts.Format("ReportIntro", player, kingdom, town, king, allies) + "\n"
            + CoupSourceTexts.Get("ReportProcessLabel") + process + "\n"
            + CoupSourceTexts.Get("ReportResultLabel") + result + "\n"
            + CoupSourceTexts.Format("ReportCasualties", alliedKilled, alliedWounded, defenderKilled, defenderWounded);
    }
}
