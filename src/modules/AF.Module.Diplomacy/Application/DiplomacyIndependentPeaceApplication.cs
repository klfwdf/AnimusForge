namespace AnimusForge;

internal readonly struct DiplomacyIndependentPeacePlayerSnapshot
{
    internal DiplomacyIndependentPeacePlayerSnapshot(bool campaignAvailable, bool mainHeroAvailable,
        bool clanAvailable, bool clanEliminated, bool clanHasKingdom, bool isMercenary, bool leaderAllowed)
    {
        CampaignAvailable = campaignAvailable;
        MainHeroAvailable = mainHeroAvailable;
        ClanAvailable = clanAvailable;
        ClanEliminated = clanEliminated;
        ClanHasKingdom = clanHasKingdom;
        IsMercenary = isMercenary;
        LeaderAllowed = leaderAllowed;
    }
    internal bool CampaignAvailable { get; }
    internal bool MainHeroAvailable { get; }
    internal bool ClanAvailable { get; }
    internal bool ClanEliminated { get; }
    internal bool ClanHasKingdom { get; }
    internal bool IsMercenary { get; }
    internal bool LeaderAllowed { get; }
}

internal readonly struct DiplomacyIndependentPeaceSpeakerSnapshot
{
    internal DiplomacyIndependentPeaceSpeakerSnapshot(bool npcAvailable, bool npcIsPlayer, bool npcDead,
        bool clanAvailable, bool sameClan, bool clanEliminated, bool bandit, bool outlaw)
    {
        NpcAvailable = npcAvailable;
        NpcIsPlayer = npcIsPlayer;
        NpcDead = npcDead;
        ClanAvailable = clanAvailable;
        SameClan = sameClan;
        ClanEliminated = clanEliminated;
        Bandit = bandit;
        Outlaw = outlaw;
    }
    internal bool NpcAvailable { get; }
    internal bool NpcIsPlayer { get; }
    internal bool NpcDead { get; }
    internal bool ClanAvailable { get; }
    internal bool SameClan { get; }
    internal bool ClanEliminated { get; }
    internal bool Bandit { get; }
    internal bool Outlaw { get; }
}

internal readonly struct DiplomacyIndependentPeaceTargetSnapshot
{
    internal DiplomacyIndependentPeaceTargetSnapshot(bool targetAvailable, bool npcIsRuler)
    {
        TargetAvailable = targetAvailable;
        NpcIsRuler = npcIsRuler;
    }
    internal bool TargetAvailable { get; }
    internal bool NpcIsRuler { get; }
}

internal readonly struct DiplomacyIndependentPeaceWarSnapshot
{
    internal DiplomacyIndependentPeaceWarSnapshot(bool distinctFactions, bool playerEliminated,
        bool targetEliminated, bool atWar, bool constantWar)
    {
        DistinctFactions = distinctFactions;
        PlayerEliminated = playerEliminated;
        TargetEliminated = targetEliminated;
        AtWar = atWar;
        ConstantWar = constantWar;
    }
    internal bool DistinctFactions { get; }
    internal bool PlayerEliminated { get; }
    internal bool TargetEliminated { get; }
    internal bool AtWar { get; }
    internal bool ConstantWar { get; }
}

internal interface IDiplomacyIndependentPeaceSource
{
    DiplomacyIndependentPeacePlayerSnapshot CapturePlayer();
    DiplomacyIndependentPeaceSpeakerSnapshot CaptureSpeaker();
    DiplomacyIndependentPeaceTargetSnapshot CaptureTarget();
    DiplomacyIndependentPeaceWarSnapshot CaptureWar();
}

internal static class DiplomacyIndependentPeaceApplication
{
    internal static bool CanUse<TSource>(ref TSource source) where TSource : struct, IDiplomacyIndependentPeaceSource
    {
        try
        {
            if (!IsEligible(source.CapturePlayer()) || !IsEligible(source.CaptureSpeaker()) ||
                !IsEligible(source.CaptureTarget())) return false;
            return IsEligible(source.CaptureWar());
        }
        catch { return false; }
    }

    internal static bool IsEligible(DiplomacyIndependentPeacePlayerSnapshot value) =>
        value.CampaignAvailable && value.MainHeroAvailable && value.ClanAvailable &&
        !value.ClanEliminated && !value.ClanHasKingdom && !value.IsMercenary && value.LeaderAllowed;

    internal static bool IsEligible(DiplomacyIndependentPeaceSpeakerSnapshot value) =>
        value.NpcAvailable && !value.NpcIsPlayer && !value.NpcDead && value.ClanAvailable &&
        !value.SameClan && !value.ClanEliminated && !value.Bandit && !value.Outlaw;

    internal static bool IsEligible(DiplomacyIndependentPeaceTargetSnapshot value) =>
        value.TargetAvailable && value.NpcIsRuler;

    internal static bool IsEligible(DiplomacyIndependentPeaceWarSnapshot value) =>
        value.DistinctFactions && !value.PlayerEliminated && !value.TargetEliminated &&
        value.AtWar && !value.ConstantWar;
}
