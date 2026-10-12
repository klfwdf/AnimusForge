using AnimusForge;
using AnimusForge.Refactor.Adapters;

using AnimusForge.Refactor.Contracts;

using AnimusForge.Refactor.Modules;

using TaleWorlds.CampaignSystem;



namespace AnimusForge;



internal struct DiplomacyOralBreakAllianceSource : IDiplomacyOralBreakAllianceSource

{

    private static readonly WorldDiplomacyBreakAllianceCommandFacade CommandFacade =

        new WorldDiplomacyBreakAllianceCommandFacade(new BannerlordWorldDiplomacyBreakAllianceGameActionPort());



    private readonly Hero _npc;

    private Kingdom _playerKingdom;

    private Kingdom _npcKingdom;

    internal DiplomacyOralBreakAllianceSource(Hero npc) : this() => _npc = npc;



    public DiplomacyOralPairSnapshot Capture()

    {

        Kingdom player = Clan.PlayerClan?.Kingdom;

        Kingdom npcKingdom = _npc?.Clan?.Kingdom;

        return new DiplomacyOralPairSnapshot(

            player != null, player?.StringId, player?.IsEliminated == true,

            npcKingdom != null, npcKingdom?.StringId, _npc?.StringId);

    }



    public WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command) =>

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

        "break_alliance", _playerKingdom, _npcKingdom, DiplomacyMiscPromptTexts.Get("OralDiplomacy_AgreedFaceToFace", "Face-to-face oral diplomacy reached"));



    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);

}

