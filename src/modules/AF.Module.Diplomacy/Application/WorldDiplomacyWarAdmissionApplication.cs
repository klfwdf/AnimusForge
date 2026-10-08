namespace AnimusForge;

// Lazy value reads retain short-circuit order; the kingdom scan is last.
internal interface IWorldDiplomacyWarAdmissionPort
{
    bool ValidPair { get; }
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
        { reason = "王国目标无效"; return false; }
        if (!port.HasIndependentAuthority)
        { reason = "附庸国没有独立外交权，应由宗主国处理"; return false; }
        if (port.AtWar)
        { reason = "双方已经处于战争状态"; return false; }
        if (port.Allied)
        { reason = "双方仍有同盟，必须先正式解除同盟"; return false; }
        int day = port.CurrentDay;
        int protectionDays = port.PeaceProtectionDays;
        if (protectionDays > 0 && port.TryGetLastPeaceDay(out int lastPeaceDay) && day - lastPeaceDay < protectionDays)
        { reason = "仍处于和平保护期，剩余" + (protectionDays - (day - lastPeaceDay)) + "天"; return false; }
        return true;
    }

    internal static bool CanDeclareWar<T>(ref T port, out string reason, bool enforceRejectedUltimatum = false, bool isPlayerAuthored = false)
        where T : IWorldDiplomacyWarAdmissionPort
    {
        if (!CanIssueWarThreat(ref port, out reason)) return false;
        if (port.BlocksNewOffensiveWar)
        { reason = "该国正在内战，不能新开主动战争"; return false; }
        if (isPlayerAuthored) return true;
        if (port.PendingThreatDecision)
        { reason = "已发出的谴责或最后通牒仍在等待对象国一次性决定"; return false; }
        if (enforceRejectedUltimatum) return true;
        int day = port.CurrentDay;
        int cooldownDays = port.OffensiveWarCooldownDays;
        if (port.TryGetLastOffensiveWarDay(out int lastWarDay) && day - lastWarDay < cooldownDays)
        { reason = "主动战争冷却尚未结束，剩余" + (cooldownDays - (day - lastWarDay)) + "天"; return false; }
        int activeWars = port.ActiveWars;
        int maxWars = port.MaxConcurrentOffensiveWars;
        if (activeWars >= maxWars)
        { reason = "当前同时战争数量过多（" + activeWars + "/" + maxWars + "，包含被动战争），不能新增主动战争"; return false; }
        return true;
    }
}
