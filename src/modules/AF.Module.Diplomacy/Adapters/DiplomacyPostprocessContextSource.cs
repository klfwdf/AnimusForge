using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Captures current campaign values; prompt admission and layout live in Application.
internal struct DiplomacyPostprocessContextSource : IDiplomacyPostprocessContextSource, IDiplomacyOralPostprocessSource
{
    private readonly Hero _npc;
    private Kingdom _npcKingdom;
    private Kingdom _playerKingdom;

    internal DiplomacyPostprocessContextSource(Hero npc) : this() => _npc = npc;
    public bool HasSpeaker => _npc != null;
    public bool UseFormalCommitments => WorldDiplomacyBehavior.UseFormalDiplomacyForConversation;
    public string NativeActionInstruction() => AIConfigHandler.ResolveRuleRuntimeText("diplomacy", "native_action_postprocess", forConstraint: false, null);
    public string OralArrangementContext() => WorldDiplomacyBehavior.BuildOralArrangementContext(_npc);

    public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot)
    {
        snapshot = default;
        var source = new DiplomacyIndependentPeaceSource(_npc);
        if (!DiplomacyIndependentPeaceApplication.CanUse(ref source)) return false;
        Clan player = source.PlayerClan;
        Kingdom target = source.TargetKingdom;
        snapshot = new DiplomacyIndependentPeaceContextSnapshot(
            player.Name?.ToString() ?? "玩家家族", player.Settlements?.Count ?? 0,
            target.Name?.ToString() ?? "目标王国");
        return true;
    }

    public DiplomacyConversationEligibilitySnapshot CaptureEligibility() =>
        DiplomacyBehavior.CaptureEligibilitySnapshot(_npc);

    public DiplomacyPostprocessKingdomSnapshot CaptureKingdoms()
    {
        _npcKingdom = _npc.Clan?.Kingdom;
        if (_npcKingdom == null) return default;
        _playerKingdom = Clan.PlayerClan?.Kingdom;
        var kingdoms = new List<DiplomacyKingdomSummary>();
        foreach (Kingdom kingdom in Kingdom.All)
        {
            bool eliminated = kingdom.IsEliminated;
            kingdoms.Add(new DiplomacyKingdomSummary(eliminated ? "" : kingdom.StringId,
                eliminated ? "" : DisplayName(kingdom), eliminated));
        }
        bool playerEliminated = _playerKingdom?.IsEliminated == true;
        return new DiplomacyPostprocessKingdomSnapshot(
            true, _npcKingdom.StringId, DisplayName(_npcKingdom),
            _playerKingdom != null, playerEliminated,
            _playerKingdom?.StringId, _playerKingdom == null || playerEliminated ? "" : DisplayName(_playerKingdom),
            _playerKingdom != null && !playerEliminated && Hero.MainHero == _playerKingdom.RulingClan?.Leader,
            _playerKingdom != _npcKingdom, kingdoms);
    }

    public string GetAnnexationHint() =>
        KingdomAnnexationBehavior.BuildRuntimeAnnexationConstraintHintForExternal(_npc);

    public bool ArePlayerAndNpcAtWar() =>
        _playerKingdom != null && _npcKingdom != null &&
        FactionManager.IsAtWarAgainstFaction(_npcKingdom, _playerKingdom);

    public int CalculateDailyTribute(bool npcPays)
    {
        var source = npcPays
            ? new DiplomacyTributePowerSource(_npcKingdom, _playerKingdom)
            : new DiplomacyTributePowerSource(_playerKingdom, _npcKingdom);
        return DiplomacyTributePowerApplication.TryBuild(ref source, out AfTributePowerContext context)
            ? context.CalculatedTribute : 0;
    }

    public void LogFailure(string message) =>
        Logger.Log("DiplomacyBehavior", "[BuildPostprocess Error] " + message);

    private static string DisplayName(Kingdom kingdom) =>
        kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
}
