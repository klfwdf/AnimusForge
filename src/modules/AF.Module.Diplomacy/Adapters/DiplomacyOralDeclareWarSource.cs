using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Live identity and single game-action adapter for an oral declaration.
internal struct DiplomacyOralDeclareWarSource : IDiplomacyOralDeclareWarSource
{
    private static readonly WorldDiplomacyDeclareWarCommandFacade CommandFacade =
        new WorldDiplomacyDeclareWarCommandFacade(new BannerlordWorldDiplomacyDeclareWarGameActionPort());

    private readonly Hero _npc;
    private Kingdom _declarer;
    private Kingdom _target;

    internal DiplomacyOralDeclareWarSource(Hero npc) : this() => _npc = npc;

    public DiplomacyOralDeclareWarSnapshot Capture()
    {
        Kingdom npcKingdom = _npc?.Clan?.Kingdom;
        Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
        return new DiplomacyOralDeclareWarSnapshot(
            npcKingdom != null, npcKingdom?.StringId, _npc?.StringId,
            playerKingdom != null, playerKingdom?.StringId, playerKingdom?.IsEliminated == true,
            _npc != null && _npc == npcKingdom?.RulingClan?.Leader);
    }

    public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command) =>
        CommandFacade.Execute(command);

    public bool TryResolveAppliedEndpoints(string declarerId, string targetId,
        out string resolvedDeclarerId, out string resolvedTargetId)
    {
        _declarer = DiplomacyBehavior.ResolveKingdom(declarerId);
        _target = DiplomacyBehavior.ResolveKingdom(targetId);
        resolvedDeclarerId = _declarer?.StringId;
        resolvedTargetId = _target?.StringId;
        return _declarer != null && _target != null;
    }

    public void NotifyResolved() => WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved(
        "declare_war", _declarer, _target, "面对面口头外交达成");

    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
