using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal struct DiplomacyOralMakePeaceSource : IDiplomacyOralMakePeaceSource
{
    private static readonly WorldDiplomacyMakePeaceCommandFacade CommandFacade =
        new WorldDiplomacyMakePeaceCommandFacade(new BannerlordWorldDiplomacyMakePeaceGameActionPort());

    private readonly Hero _npc;
    private Kingdom _payer;
    private Kingdom _receiver;
    internal DiplomacyOralMakePeaceSource(Hero npc) : this() => _npc = npc;

    public DiplomacyOralRoyalSnapshot Capture()
    {
        Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
        Kingdom npcKingdom = _npc?.Clan?.Kingdom;
        return new DiplomacyOralRoyalSnapshot(
            playerKingdom != null, playerKingdom?.StringId,
            playerKingdom?.IsEliminated == true, DiplomacyBehavior.IsPlayerKing(),
            npcKingdom != null, npcKingdom?.StringId, _npc?.StringId,
            DiplomacyBehavior.IsNpcKing(_npc, npcKingdom));
    }

    public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command) =>
        CommandFacade.Execute(command);

    public bool TryResolveAppliedEndpoints(string payerId, string receiverId,
        out string resolvedPayerId, out string resolvedReceiverId)
    {
        _payer = DiplomacyBehavior.ResolveKingdom(payerId);
        _receiver = DiplomacyBehavior.ResolveKingdom(receiverId);
        resolvedPayerId = _payer?.StringId;
        resolvedReceiverId = _receiver?.StringId;
        return _payer != null && _receiver != null;
    }

    public void NotifyResolved() => WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved(
        "accept_peace", _payer, _receiver, "面对面口头外交达成");

    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
