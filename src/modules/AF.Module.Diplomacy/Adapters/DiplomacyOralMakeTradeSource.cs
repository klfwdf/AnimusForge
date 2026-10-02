using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal struct DiplomacyOralMakeTradeSource : IDiplomacyOralMakeTradeSource
{
    private static readonly WorldDiplomacyMakeTradeCommandFacade CommandFacade =
        new WorldDiplomacyMakeTradeCommandFacade(new BannerlordWorldDiplomacyMakeTradeGameActionPort());

    private readonly Hero _npc;
    private Kingdom _playerKingdom;
    private Kingdom _npcKingdom;
    internal DiplomacyOralMakeTradeSource(Hero npc) : this() => _npc = npc;

    public DiplomacyOralRoyalSnapshot Capture()
    {
        Kingdom player = Clan.PlayerClan?.Kingdom;
        Kingdom npcKingdom = _npc?.Clan?.Kingdom;
        return new DiplomacyOralRoyalSnapshot(
            player != null, player?.StringId, player?.IsEliminated == true,
            DiplomacyBehavior.IsPlayerKing(), npcKingdom != null, npcKingdom?.StringId,
            _npc?.StringId, DiplomacyBehavior.IsNpcKing(_npc, npcKingdom));
    }

    public WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command) =>
        CommandFacade.Execute(command);

    public bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId)
    {
        _playerKingdom = DiplomacyBehavior.ResolveKingdom(playerId);
        _npcKingdom = DiplomacyBehavior.ResolveKingdom(npcId);
        resolvedPlayerId = _playerKingdom?.StringId;
        resolvedNpcId = _npcKingdom?.StringId;
        return _playerKingdom != null && _npcKingdom != null;
    }

    public void NotifyResolved() => WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved(
        "accept_trade", _playerKingdom, _npcKingdom, "面对面口头外交达成");

    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
