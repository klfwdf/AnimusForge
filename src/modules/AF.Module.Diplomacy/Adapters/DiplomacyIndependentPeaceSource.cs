using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// One invocation's live game objects. Application owns the ordered admission decision.
internal struct DiplomacyIndependentPeaceSource : IDiplomacyIndependentPeaceSource
{
    private readonly Hero _npc;
    private Clan _targetClan;
    internal Clan PlayerClan { get; private set; }
    internal Kingdom TargetKingdom { get; private set; }

    internal DiplomacyIndependentPeaceSource(Hero npc) : this() => _npc = npc;

    public DiplomacyIndependentPeacePlayerSnapshot CapturePlayer()
    {
        Hero main = Hero.MainHero;
        PlayerClan = Clan.PlayerClan ?? main?.Clan;
        Clan clan = PlayerClan;
        return new DiplomacyIndependentPeacePlayerSnapshot(
            Campaign.Current != null, main != null, clan != null,
            clan?.IsEliminated == true, clan?.Kingdom != null,
            clan?.IsUnderMercenaryService == true,
            clan?.Leader == null || clan.Leader == main);
    }

    public DiplomacyIndependentPeaceSpeakerSnapshot CaptureSpeaker()
    {
        Hero npc = _npc;
        _targetClan = npc?.Clan;
        Clan targetClan = _targetClan;
        return new DiplomacyIndependentPeaceSpeakerSnapshot(
            npc != null, npc != null && npc == Hero.MainHero, npc?.IsDead == true,
            targetClan != null, targetClan != null && targetClan == PlayerClan,
            targetClan?.IsEliminated == true, targetClan?.IsBanditFaction == true,
            targetClan?.IsOutlaw == true);
    }

    public DiplomacyIndependentPeaceTargetSnapshot CaptureTarget()
    {
        TargetKingdom = _targetClan?.Kingdom ?? _npc?.MapFaction as Kingdom;
        return new DiplomacyIndependentPeaceTargetSnapshot(
            TargetKingdom != null, _npc != null && TargetKingdom?.RulingClan?.Leader == _npc);
    }

    public DiplomacyIndependentPeaceWarSnapshot CaptureWar()
    {
        Clan player = PlayerClan;
        Kingdom target = TargetKingdom;
        bool distinct = player != null && target != null && (IFaction)player != target;
        bool playerEliminated = player?.IsEliminated == true;
        bool targetEliminated = target?.IsEliminated == true;
        bool canQueryWar = distinct && !playerEliminated && !targetEliminated;
        bool atWar = canQueryWar && FactionManager.IsAtWarAgainstFaction(player, target);
        return new DiplomacyIndependentPeaceWarSnapshot(
            distinct, playerEliminated, targetEliminated,
            atWar, atWar && FactionManager.IsAtConstantWarAgainstFaction(player, target));
    }
}
