using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
namespace AnimusForge;
internal static class DiplomacyRecentPeaceBridge
{
    internal static void RegisterPeace(IFaction first, IFaction second, string reason) =>
        DiplomacyModuleServices.Module.RecentPeace.Register(DiplomacyFactionSnapshot.Id(first), DiplomacyFactionSnapshot.Id(second), DateTime.UtcNow);
    internal static bool ShouldBlockEncounterHostility(PartyBase attacker, PartyBase defender, string source) =>
        ShouldBlockDeclareWar(attacker?.MapFaction, defender?.MapFaction, source);
    internal static bool ShouldBlockDeclareWar(IFaction first, IFaction second, DeclareWarAction.DeclareWarDetail detail, string source) =>
        ShouldBlockDeclareWar(first, second, source);
    internal static bool ShouldBlockDeclareWar(IFaction first, IFaction second, string source) =>
        DiplomacyModuleServices.Module.RecentPeace.ShouldBlock(DiplomacyFactionSnapshot.Id(first), DiplomacyFactionSnapshot.Id(second), DateTime.UtcNow);
}
