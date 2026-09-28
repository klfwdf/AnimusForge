using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal readonly struct DiplomacyOralIndependentPeaceSource : IDiplomacyOralIndependentPeaceSource
{
    private static readonly WorldDiplomacyIndependentClanPeaceCommandFacade CommandFacade =
        new WorldDiplomacyIndependentClanPeaceCommandFacade(
            new BannerlordWorldDiplomacyIndependentClanPeaceGameActionPort());

    private readonly Hero _npc;
    internal DiplomacyOralIndependentPeaceSource(Hero npc) => _npc = npc;

    public DiplomacyOralIndependentPeaceSnapshot Capture()
    {
        var source = new DiplomacyIndependentPeaceSource(_npc);
        bool available = DiplomacyIndependentPeaceApplication.CanUse(ref source);
        return new DiplomacyOralIndependentPeaceSnapshot(
            available, available ? source.PlayerClan?.StringId : null,
            available ? source.TargetKingdom?.StringId : null, _npc?.StringId);
    }

    public WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(
        WorldDiplomacyIndependentClanPeaceCommand command) => CommandFacade.Execute(command);

    public void Log(string message) => Logger.Log("DiplomacyBehavior", message);
}
