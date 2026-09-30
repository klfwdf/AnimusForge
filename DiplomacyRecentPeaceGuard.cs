using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
namespace AnimusForge;
internal static class DiplomacyRecentPeaceGuard
{
    internal static void RegisterPeace(IFaction a, IFaction b, string reason) => DiplomacyRecentPeaceBridge.RegisterPeace(a,b,reason);
    internal static bool ShouldBlockEncounterHostility(PartyBase a, PartyBase b, string source) => DiplomacyRecentPeaceBridge.ShouldBlockEncounterHostility(a,b,source);
    internal static bool ShouldBlockDeclareWar(IFaction a, IFaction b, DeclareWarAction.DeclareWarDetail detail, string source) => DiplomacyRecentPeaceBridge.ShouldBlockDeclareWar(a,b,detail,source);
    internal static bool ShouldBlockDeclareWar(IFaction a, IFaction b, string source) => DiplomacyRecentPeaceBridge.ShouldBlockDeclareWar(a,b,source);
}
