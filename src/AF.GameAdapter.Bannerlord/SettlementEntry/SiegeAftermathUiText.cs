using System;
using System.Globalization;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Bannerlord-facing localization for SETS scene messages and settlement memory
/// notices. The dependency-free profiles remain usable by contract tests and
/// prompt composition; only player-visible host output enters this adapter.
/// </summary>
internal static class SiegeAftermathUiText
{
    private static string Text(string id, string fallback, params (string Name, string Value)[] variables)
    {
        TextObject text = new TextObject("{=" + id + "}" + fallback);
        foreach ((string Name, string Value) variable in variables ?? Array.Empty<(string Name, string Value)>())
        {
            text.SetTextVariable(variable.Name, variable.Value ?? string.Empty);
        }
        return text.ToString();
    }

    private static string Number(long value) => Math.Max(0, value).ToString(CultureInfo.InvariantCulture);

    private static string SettlementNoun(SetsSettlementSceneKind kind)
        => kind == SetsSettlementSceneKind.Castle
            ? Text("AF_SETS_Noun_Castle", "castle")
            : kind == SetsSettlementSceneKind.Village
                ? Text("AF_SETS_Noun_Village", "village")
                : kind == SetsSettlementSceneKind.Town
                    ? Text("AF_SETS_Noun_Town", "town")
                    : Text("AF_SETS_Noun_Settlement", "settlement");

    private static string DefenderSummary(SetsSettlementSceneKind kind)
        => kind == SetsSettlementSceneKind.Castle
            ? Text("AF_SETS_Defenders_Castle", "guards, militia, garrison, and castle lord troops")
            : kind == SetsSettlementSceneKind.Village
                ? Text("AF_SETS_Defenders_Village", "village militia")
                : kind == SetsSettlementSceneKind.Town
                    ? Text("AF_SETS_Defenders_Town", "guards, militia, garrison, and town lord troops")
                    : Text("AF_SETS_Defenders_Default", "the defenders");

    private static string SpawnDescription(SetsSettlementSceneKind kind)
        => kind == SetsSettlementSceneKind.Castle
            ? Text("AF_SETS_Spawn_Castle", "the lord's hall entrance")
            : kind == SetsSettlementSceneKind.Village
                ? Text("AF_SETS_Spawn_Village", "the village map edge")
                : kind == SetsSettlementSceneKind.Town
                    ? Text("AF_SETS_Spawn_Town", "the town workshop district")
                    : Text("AF_SETS_Spawn_Default", "the edge of the scene");

    private static string DefenderPhase(SetsSettlementSceneKind kind, string phaseKind)
    {
        if (kind == SetsSettlementSceneKind.Village)
        {
            return Text("AF_SETS_Phase_VillageMilitia", "village militia");
        }
        string noun = SettlementNoun(kind);
        if (string.Equals(phaseKind, "garrison", StringComparison.OrdinalIgnoreCase))
        {
            return Text("AF_SETS_Phase_Garrison", "{NOUN} garrison", ("NOUN", noun));
        }
        if (string.Equals(phaseKind, "militia", StringComparison.OrdinalIgnoreCase))
        {
            return Text("AF_SETS_Phase_Militia", "{NOUN} militia", ("NOUN", noun));
        }
        return Text("AF_SETS_Phase_Lord", "hostile lord troops");
    }

    internal static string NoEligibleCivilianMessage
        => Text("AF_SETS_Civilians_None", "[SETS] There are no eligible adult civilians to gather.");

    internal static string BuildQueuedCivilianMessage(int civilianCount)
        => Text("AF_SETS_Civilians_Queued", "[SETS] Gathering {COUNT} adult civilians; they will join command group 3 when ready.",
            ("COUNT", Number(civilianCount)));

    internal static string BuildGatheredCivilianMessage(int civilianCount)
        => Text("AF_SETS_Civilians_Gathered", "[SETS] Gathered {COUNT} adult civilians and assigned them to command group 3.",
            ("COUNT", Number(civilianCount)));

    internal static string BuildVillageRewardMessage(int gold, int itemCount)
        => Text("AF_SETS_VillageReward", "[SETS village result] Gained {GOLD} denars; {ITEMS} village supplies were sent to the native loot screen.",
            ("GOLD", Number(gold)), ("ITEMS", Number(itemCount)));

    internal static string BuildConflictStartedMessage(SetsSettlementSceneKind kind)
        => Text("AF_SETS_ConflictStarted", "[SETS internal unrest] {DEFENDERS} are hostile (wave 0). Selected escort soldiers, companions, and family members have joined your formation and await your orders.",
            ("DEFENDERS", DefenderSummary(kind)));

    internal static string BuildReserveWaveMessage(SetsSettlementSceneKind kind, string phaseKind, int waveNumber, int maxActiveWaves)
        => Text("AF_SETS_ReserveWave", "[SETS internal unrest] {PHASE} have joined the fight from {SPAWN} (wave {WAVE}; up to {MAX} active waves).",
            ("PHASE", DefenderPhase(kind, phaseKind)), ("SPAWN", SpawnDescription(kind)),
            ("WAVE", Number(waveNumber)), ("MAX", Number(maxActiveWaves)));

    internal static string BuildExitBlockedMessage(SetsSettlementSceneKind kind)
        => Text("AF_SETS_ExitBlocked", "The internal unrest is not over. Defeat {DEFENDERS} before leaving.",
            ("DEFENDERS", DefenderSummary(kind)));

    internal static string BuildVictoryMessage(SetsSettlementSceneKind kind)
        => kind == SetsSettlementSceneKind.Village
            ? Text("AF_SETS_Victory_Village", "[SETS internal unrest] The village militia has been defeated. Press TAB to leave, collect the payment, and open the native loot screen.")
            : Text("AF_SETS_Victory", "[SETS internal unrest] {DEFENDERS} have been defeated. Press TAB to leave and open the native siege victory disposition menu; you may choose the native or GCCZ siege disposition.",
                ("DEFENDERS", DefenderSummary(kind)));

    internal static string BuildOwnedIncidentMessage(SetsSettlementSceneKind kind)
        => kind == SetsSettlementSceneKind.Village
            ? Text("AF_SETS_OwnedIncident_Village", "[SETS] An owned or attached village incident has started; villagers are fleeing. Press TAB at any time to leave and open the GCCZ village disposition menu.")
            : Text("AF_SETS_OwnedIncident", "[SETS] An owned or attached {NOUN} incident has started; residents are fleeing. Press TAB at any time to leave and open the SETS disposition menu.",
                ("NOUN", SettlementNoun(kind)));

    internal static string Aftermath(string actor, string outcome)
        => Text("AF_SETS_Memory_Aftermath", "Commander {ACTOR}'s post-siege disposition at this town was settled as {OUTCOME}.",
            ("ACTOR", string.IsNullOrWhiteSpace(actor) ? Text("AF_SETS_Memory_UnknownCommander", "unknown commander") : actor),
            ("OUTCOME", Action(outcome)));

    internal static string CompletedIntervention(string actor, string outcome, long lootValue, long gold,
        int civilianDeaths, int notableDeaths, int reliefGold, int reliefFood)
        => Text("AF_SETS_Memory_Completed", "{AFTERMATH} Settled loot valuation: {LOOT}; denars taken: {GOLD}; civilian deaths: {CIVILIANS}; notable deaths: {NOTABLES}; relief delivered: {RELIEF_GOLD} denars and {RELIEF_FOOD} food. This records the actual settlement and does not claim that later promises were fulfilled.",
            ("AFTERMATH", Aftermath(actor, outcome)), ("LOOT", Number(lootValue)), ("GOLD", Number(gold)),
            ("CIVILIANS", Number(civilianDeaths)), ("NOTABLES", Number(notableDeaths)),
            ("RELIEF_GOLD", Number(reliefGold)), ("RELIEF_FOOD", Number(reliefFood)));

    internal static string Policy(string eventKind, string title)
    {
        string status = eventKind switch
        {
            "published" => Text("AF_SETS_Policy_Published", "published"),
            "renewed" => Text("AF_SETS_Policy_Renewed", "renewed"),
            "abolished" => Text("AF_SETS_Policy_Abolished", "abolished"),
            "expired" => Text("AF_SETS_Policy_Expired", "expired"),
            _ => string.Empty
        };
        return status.Length == 0
            ? string.Empty
            : Text("AF_SETS_Memory_Policy", "Local policy \"{TITLE}\" was {STATUS} in this town. This only confirms the policy state; expected economic or public-order effects are not implied.",
                ("TITLE", title ?? string.Empty), ("STATUS", status));
    }

    private static string Action(string outcome)
        => outcome == "Pillage"
            ? Text("AF_SETS_Action_Pillage", "pillage")
            : outcome == "Devastate"
                ? Text("AF_SETS_Action_Devastate", "devastation")
                : Text("AF_SETS_Action_Mercy", "mercy");
}
