using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal struct DiplomacyOralCancelTradeSource : IDiplomacyOralCancelTradeSource
{
    private static readonly WorldDiplomacyCancelTradeCommandFacade CommandFacade =
        new WorldDiplomacyCancelTradeCommandFacade(new BannerlordWorldDiplomacyCancelTradeGameActionPort());

    private readonly Hero _npc;
    private Kingdom _playerKingdom;
    private Kingdom _npcKingdom;
    internal DiplomacyOralCancelTradeSource(Hero npc) : this() => _npc = npc;

    public DiplomacyOralPairSnapshot Capture()
    {
        Kingdom player = Clan.PlayerClan?.Kingdom;
        Kingdom npcKingdom = _npc?.Clan?.Kingdom;
        return new DiplomacyOralPairSnapshot(
            player != null, player?.StringId, player?.IsEliminated == true,
            npcKingdom != null, npcKingdom?.StringId, _npc?.StringId);
    }

    public WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command) =>
        CommandFacade.Execute(command);

    public bool TryResolveAppliedEndpoints(string playerId, string npcId,
        out string resolvedPlayerId, out string resolvedNpcId)
    {
        _playerKingdom = DiplomacyBehavior.ResolveKingdom(playerId, includeEliminated: true);
        _npcKingdom = DiplomacyBehavior.ResolveKingdom(npcId, includeEliminated: true);
        resolvedPlayerId = _playerKingdom?.StringId;
        resolvedNpcId = _npcKingdom?.StringId;
        return _playerKingdom != null && _npcKingdom != null;
    }

    public void NotifyResolved() => WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved(
        "cancel_trade", _playerKingdom, _npcKingdom, "面对面口头外交达成");

    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
