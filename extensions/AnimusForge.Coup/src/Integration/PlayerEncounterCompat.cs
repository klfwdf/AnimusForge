using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge.CoupSystem;

// Only the read-side battle gate needed by this sub-MOD. No AF internal type dependency.
internal static class PlayerEncounterCompat
{
    private static readonly FieldInfo BattleField = AccessTools.Field(typeof(PlayerEncounter), "_mapEvent");
    private static readonly FieldInfo EncounteredPartyField = AccessTools.Field(typeof(PlayerEncounter), "_encounteredParty");

    internal static bool HasEncounterBattleContext()
    {
        try
        {
            if (MapEvent.PlayerMapEvent != null || MobileParty.MainParty?.MapEvent != null) return true;
            PlayerEncounter encounter = Campaign.Current?.PlayerEncounter;
            if (encounter == null) return false;
            // An unknown API layout is not proof that opening a location combat is safe.
            if (BattleField == null || EncounteredPartyField == null) return true;
            if (BattleField.GetValue(encounter) != null) return true;
            PartyBase party = EncounteredPartyField.GetValue(encounter) as PartyBase;
            return party?.MapEvent != null || (party?.IsSettlement == true && party.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapEvent != null);
        }
        catch { return true; }
    }
}
