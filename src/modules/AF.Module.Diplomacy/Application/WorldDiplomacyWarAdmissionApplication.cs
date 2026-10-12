using AnimusForge;
namespace AnimusForge;

// Lazy value reads retain short-circuit order; the kingdom scan is last.
internal interface IWorldDiplomacyWarAdmissionPort
{
    bool ValidPair { get; }
    bool InvolvesPlayer { get; }
    bool HasIndependentAuthority { get; }
    bool AtWar { get; }
    bool Allied { get; }
    bool PendingThreatDecision { get; }
    bool BlocksNewOffensiveWar { get; }
    int CurrentDay { get; }
    int PeaceProtectionDays { get; }
    bool TryGetLastPeaceDay(out int day);
    int OffensiveWarCooldownDays { get; }
    bool TryGetLastOffensiveWarDay(out int day);
    int ActiveWars { get; }
    int MaxConcurrentOffensiveWars { get; }
}

internal static class WorldDiplomacyWarAdmissionApplication
{
    internal static bool CanIssueWarThreat<T>(ref T port, out string reason)
        where T : IWorldDiplomacyWarAdmissionPort
    {
        reason = "";
        if (!port.ValidPair)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_InvalidTarget", "Invalid kingdom target"); return false; }
        // Player diplomacy is not subject to autonomous AI authority or pacing policy.
        if (port.InvolvesPlayer) return true;
        if (!port.HasIndependentAuthority)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_SubjectNoIndependentDiplomacy", "Subject kingdoms lack independent diplomacy; handle through suzerain"); return false; }
        if (port.AtWar)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_AlreadyAtWar", "Both sides are already at war"); return false; }
        if (port.Allied)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_AlliedMustBreakAlliance", "Both sides remain allied; alliance must be formally broken first"); return false; }
        int day = port.CurrentDay;
        int protectionDays = port.PeaceProtectionDays;
        if (protectionDays > 0 && port.TryGetLastPeaceDay(out int lastPeaceDay) && day - lastPeaceDay < protectionDays)
        { reason = string.Format(DiplomacyMiscPromptTexts.Get("WarAdmission_PeaceProtectionDays", "Peace protection still active, {0} days remaining"), (protectionDays - (day - lastPeaceDay))); return false; }
        return true;
    }

    internal static bool CanDeclareWar<T>(ref T port, out string reason, bool enforceRejectedUltimatum = false, bool isPlayerAuthored = false)
        where T : IWorldDiplomacyWarAdmissionPort
    {
        if (isPlayerAuthored)
        {
            bool valid = port.ValidPair;
            reason = valid ? "" : DiplomacyMiscPromptTexts.Get("WarAdmission_InvalidTarget", "Invalid kingdom target");
            return valid;
        }
        if (!CanIssueWarThreat(ref port, out reason)) return false;
        if (port.InvolvesPlayer) return true;
        if (port.BlocksNewOffensiveWar)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_CivilWarNoNewWars", "This kingdom is in civil war and cannot initiate new wars"); return false; }
        if (port.PendingThreatDecision)
        { reason = DiplomacyMiscPromptTexts.Get("WarAdmission_ThreatPendingDecision", "Issued denunciation or ultimatum is still awaiting target kingdom's one-off decision"); return false; }
        if (enforceRejectedUltimatum) return true;
        int day = port.CurrentDay;
        int cooldownDays = port.OffensiveWarCooldownDays;
        if (port.TryGetLastOffensiveWarDay(out int lastWarDay) && day - lastWarDay < cooldownDays)
        { reason = string.Format(DiplomacyMiscPromptTexts.Get("WarAdmission_CooldownDays", "Offensive war cooldown has not ended, {0} days remaining"), (cooldownDays - (day - lastWarDay))); return false; }
        int activeWars = port.ActiveWars;
        int maxWars = port.MaxConcurrentOffensiveWars;
        if (activeWars >= maxWars)
        { reason = string.Format(DiplomacyMiscPromptTexts.Get("WarAdmission_TooManyActiveWars", "Too many simultaneous active wars ({0}, including defensive wars); cannot initiate a new offensive war"), (activeWars + "/" + maxWars)); return false; }
        return true;
    }
}
