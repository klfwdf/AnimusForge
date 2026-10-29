using AnimusForge;
namespace AnimusForge;

// Current pair authority and state only; no retired score, timer or world scan.
internal interface IWorldDiplomacyWarAdmissionPort
{
    bool ValidPair { get; }
    bool HasIndependentAuthority { get; }
    bool AtWar { get; }
    bool Allied { get; }
    bool BlocksNewOffensiveWar { get; }
}

internal static class WorldDiplomacyWarAdmissionApplication
{
    internal static bool CanIssueWarThreat<T>(ref T port, out string reason)
        where T : IWorldDiplomacyWarAdmissionPort
    {
        reason = "";
        if (!port.ValidPair)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_InvalidTarget", "Invalid kingdom target"); return false; }
        if (!port.HasIndependentAuthority)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_SubjectNoIndependentDiplomacy", "Subject kingdoms lack independent diplomacy; handle through suzerain"); return false; }
        if (port.AtWar)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_AlreadyAtWar", "Both sides are already at war"); return false; }
        if (port.Allied)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_AlliedMustBreakAlliance", "Both sides remain allied; alliance must be formally broken first"); return false; }
        return true;
    }

    internal static bool CanDeclareWar<T>(ref T port, out string reason, bool enforceRejectedUltimatum = false, bool isPlayerAuthored = false)
        where T : IWorldDiplomacyWarAdmissionPort
    {
        if (!CanIssueWarThreat(ref port, out reason)) return false;
        if (port.BlocksNewOffensiveWar)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_CivilWarNoNewWars", "This kingdom is in civil war and cannot initiate new wars"); return false; }
        return true;
    }
}
