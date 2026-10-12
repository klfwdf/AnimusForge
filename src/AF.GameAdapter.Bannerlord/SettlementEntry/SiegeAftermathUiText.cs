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
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static string Text(string id, string fallback, params (string Name, string Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
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

    internal static string VillageActivated
        => Text("AF_SETS_Village_Activated", "[GCCZ village] You can carry out noble disposition in this village. Normal AF and native dialogue remain available.");

    internal static string VillageCultureAfterDialogue
        => Text("AF_SETS_Village_CultureAfterDialogue", "[GCCZ village] A culture-change confirmation panel will open after the dialogue ends.");

    internal static string VillageInsufficientGold(string action)
        => Text("AF_SETS_Village_InsufficientGold", "[GCCZ village] Not enough denars to perform {ACTION}.", ("ACTION", action));

    internal static string VillageActionApplied(string action, string oldHearth, string newHearth, string extra)
        => Text("AF_SETS_Village_ActionApplied", "[GCCZ village] Applied {ACTION}: hearths {OLD} → {NEW}{EXTRA}.",
            ("ACTION", action), ("OLD", oldHearth), ("NEW", newHearth), ("EXTRA", extra));

    internal static string VillageActionName(VillageAftermathActionKind action)
        => action switch
        {
            VillageAftermathActionKind.GatherElders => Text("AF_SETS_Village_Action_GatherElders", "gather elders"),
            VillageAftermathActionKind.RestrainTroops => Text("AF_SETS_Village_Action_RestrainTroops", "restrain troops"),
            VillageAftermathActionKind.Pacify => Text("AF_SETS_Village_Action_Pacify", "pacify the village"),
            VillageAftermathActionKind.Relief => Text("AF_SETS_Village_Action_Relief", "provide relief"),
            VillageAftermathActionKind.Fine => Text("AF_SETS_Village_Action_Fine", "fine the village"),
            VillageAftermathActionKind.RequisitionFood => Text("AF_SETS_Village_Action_RequisitionFood", "requisition food"),
            VillageAftermathActionKind.RequisitionProduce => Text("AF_SETS_Village_Action_RequisitionProduce", "requisition produce"),
            VillageAftermathActionKind.RequisitionLivestock => Text("AF_SETS_Village_Action_RequisitionLivestock", "requisition livestock"),
            VillageAftermathActionKind.LevyRecruits => Text("AF_SETS_Village_Action_LevyRecruits", "levy recruits"),
            VillageAftermathActionKind.PunishRingleader => Text("AF_SETS_Village_Action_PunishRingleader", "punish the ringleader"),
            VillageAftermathActionKind.ConfiscateProperty => Text("AF_SETS_Village_Action_ConfiscateProperty", "confiscate property"),
            VillageAftermathActionKind.DestroyLivelihood => Text("AF_SETS_Village_Action_DestroyLivelihood", "destroy livelihoods"),
            VillageAftermathActionKind.Massacre => Text("AF_SETS_Village_Action_Massacre", "massacre"),
            VillageAftermathActionKind.CulturalReform => Text("AF_SETS_Village_Action_CulturalReform", "change culture"),
            _ => action.ToString()
        };

    internal static string VillageCultureModeName(VillageCultureChangeMode mode)
        => mode switch
        {
            VillageCultureChangeMode.GradualEducation => VillageCultureModeGradual,
            VillageCultureChangeMode.MigrantResettlement => VillageCultureModeMigration,
            VillageCultureChangeMode.PurgeColonization => VillageCultureModePurge,
            _ => mode.ToString()
        };

    internal static string VillageCultureModeGradual
        => Text("AF_SETS_Village_CultureMode_Gradual", "Gradual education");

    internal static string VillageCultureModeMigration
        => Text("AF_SETS_Village_CultureMode_Migration", "Migrant resettlement");

    internal static string VillageCultureModePurge
        => Text("AF_SETS_Village_CultureMode_Purge", "Purge and colonization");

    internal static string VillageCultureModeGradualHint
        => Text("AF_SETS_Village_CultureMode_GradualHint", "Changes to the target culture after 180 days; hearths are not reduced immediately.");

    internal static string VillageCultureModeMigrationHint
        => Text("AF_SETS_Village_CultureMode_MigrationHint", "Changes to the target culture immediately; hearths are reduced by 25% and old volunteer slots are cleared.");

    internal static string VillageCultureModePurgeHint
        => Text("AF_SETS_Village_CultureMode_PurgeHint", "Changes to the target culture immediately; hearths are reduced by 60%, old rural notables are removed, and recruitment sources are rebuilt.");

    internal static string VillageCultureInquiryTitle
        => Text("AF_SETS_Village_CultureInquiryTitle", "GCCZ village: culture change");

    internal static string VillageCultureInquiryDescription
        => Text("AF_SETS_Village_CultureInquiryDescription", "Choose a change method first. This action does not choose for you.");

    internal static string NextButton => Text("AF_SETS_Next", "Next");
    internal static string CancelButton => Text("AF_SETS_Cancel", "Cancel");
    internal static string ConfirmButton => Text("AF_SETS_Confirm", "Confirm");
    internal static string BackButton => Text("AF_SETS_Back", "Back");

    internal static string VillageNoTargetCulture
        => Text("AF_SETS_Village_NoTargetCulture", "[GCCZ village] No target culture is available.");

    internal static string VillageCultureTargetTitle
        => Text("AF_SETS_Village_CultureTargetTitle", "GCCZ village: choose target culture");

    internal static string VillageCultureTargetDescription(string mode)
        => Text("AF_SETS_Village_CultureTargetDescription", "Change method: {MODE}", ("MODE", mode));

    internal static string VillageCultureGradualStarted(string culture)
        => Text("AF_SETS_Village_CultureGradualStarted", "[GCCZ village] Gradual education started; the culture will change to {CULTURE} in about 180 days.", ("CULTURE", culture));

    internal static string VillageCultureChanged(string mode, string culture, string oldHearth, string newHearth)
        => Text("AF_SETS_Village_CultureChanged", "[GCCZ village] {MODE} complete: culture changed to {CULTURE}; hearths {OLD} → {NEW}.",
            ("MODE", mode), ("CULTURE", culture), ("OLD", oldHearth), ("NEW", newHearth));

    internal static string VillageCultureFailure(bool mutationStarted)
        => mutationStarted
            ? Text("AF_SETS_Village_CultureFailureLocked", "[GCCZ village] Culture change failed after mutation; this disposition is locked to avoid duplicate costs. Export the GCCZ log for diagnosis.")
            : Text("AF_SETS_Village_CultureFailure", "[GCCZ village] Culture change failed and had no effect. Export the GCCZ log for diagnosis.");

    internal static string VillageCultureGradualCompleted(string village, string culture)
        => Text("AF_SETS_Village_CultureGradualCompleted", "[GCCZ village] Gradual education for {VILLAGE} is complete; the culture is now {CULTURE}.",
            ("VILLAGE", village), ("CULTURE", culture));

    internal static string VillageEntryUnavailable
        => Text("AF_SETS_Village_EntryUnavailable", "[GCCZ village] The village disposition scene cannot be entered right now.");

    internal static string VillageStatePrepareFailed
        => Text("AF_SETS_Village_StatePrepareFailed", "[GCCZ village] The village disposition state could not be prepared.");

    internal static string VillageSceneNotReady
        => Text("AF_SETS_Village_SceneNotReady", "[GCCZ village] The village disposition scene is not ready. Try again.");

    internal static string VillageSceneOpenFailed
        => Text("AF_SETS_Village_SceneOpenFailed", "[GCCZ village] Failed to enter the village disposition scene.");

    internal static string NativeVictoryMenu(string noun)
        => Text("AF_SETS_NativeVictoryMenu", "[SETS internal unrest] The {NOUN} defenders were defeated. The native siege victory disposition menu is now open; choose the native or GCCZ siege disposition.", ("NOUN", noun));

    internal static string SettlementCaptured(string settlement)
        => Text("AF_SETS_SettlementCaptured", "[SETS internal unrest] {SETTLEMENT} has been captured by your clan.", ("SETTLEMENT", settlement));

    internal static string IncidentEntryTooltip(bool enabled, string noun)
        => enabled
            ? Text("AF_SETS_IncidentEntryTooltip", "Enter the {NOUN} in person to decide the fate of the remaining residents.", ("NOUN", noun))
            : Text("AF_SETS_IncidentEntryUnavailableTooltip", "No owned or attached SETS settlement incident scene is available right now.");

    internal static string SettlementFallback(string noun)
        => Text("AF_SETS_SettlementFallback", "this {NOUN}", ("NOUN", noun));

    internal static string CastleInspectionPrepareFailed
        => Text("AF_SETS_CastleInspectionPrepareFailed", "Castle disposition preparation failed; party state was restored.");

    internal static string ExecutionApproach(string actor, string prisoner)
        => Text("AF_SETS_Execution_Approach", "{ACTOR} walks toward {PRISONER} to execute them.", ("ACTOR", actor), ("PRISONER", prisoner));

    internal static string ExecutionCompleted(string actor, string prisoner)
        => Text("AF_SETS_Execution_Completed", "{ACTOR} executed {PRISONER}.", ("ACTOR", actor), ("PRISONER", prisoner));

    internal static string ExecutionHeadGiven(string actor, string item)
        => Text("AF_SETS_Execution_HeadGiven", "{ACTOR} gave the player {ITEM}.", ("ACTOR", actor), ("ITEM", item));

    internal static string ExecutionHeadFailed
        => Text("AF_SETS_Execution_HeadFailed", "[Noble prisoner escort] Execution completed, but the RP head item could not be created.");

    internal static string ExecutionCancelled(bool aborted)
        => aborted
            ? Text("AF_SETS_Execution_Cancelled", "[Noble prisoner escort] Execution was cancelled.")
            : Text("AF_SETS_Execution_Aborted", "[Noble prisoner escort] Execution was aborted; no execution result was recorded.");

    internal static string DuelSlaughterStopped
        => Text("AF_SETS_Duel_SlaughterStopped", "[Castle duel] The unfinished slaughter order was stopped; surviving prisoners were restored to the unassigned pool.");

    internal static string DuelStarted(string loadout)
        => Text("AF_SETS_Duel_Started", "[Castle duel] The tag took effect: the captive lord picked up {LOADOUT} about 20 meters ahead. After a brief setup, the duel will begin; reaching the last point of health counts as defeat, not death.", ("LOADOUT", loadout));

    internal static string DuelLoadout(string weapon, int weaponTier, string shield, int shieldTier, bool oneHandedAndShield)
        => oneHandedAndShield
            ? Text("AF_SETS_Duel_LoadoutWithShield", "{WEAPON} (tier {WEAPON_TIER}) and {SHIELD} (tier {SHIELD_TIER})",
                ("WEAPON", weapon), ("WEAPON_TIER", weaponTier.ToString(CultureInfo.InvariantCulture)),
                ("SHIELD", shield), ("SHIELD_TIER", shieldTier.ToString(CultureInfo.InvariantCulture)))
            : Text("AF_SETS_Duel_LoadoutWeapon", "{WEAPON} (tier {WEAPON_TIER})",
                ("WEAPON", weapon), ("WEAPON_TIER", weaponTier.ToString(CultureInfo.InvariantCulture)));

    internal static string DuelInterrupted
        => Text("AF_SETS_Duel_Interrupted", "[Castle duel] The duel could not continue; both sides were restored to the post-siege disposition state.");

    internal static string DuelPlayerWon
        => Text("AF_SETS_Duel_PlayerWon", "[Castle duel] You defeated the captive lord. The opponent surrendered and returned to a prisoner stance.");

    internal static string DuelPlayerLost
        => Text("AF_SETS_Duel_PlayerLost", "[Castle duel] The captive lord defeated you and lowered their weapon. Both sides remain in the disposition scene.");

    internal static string DuelCombatStarted
        => Text("AF_SETS_Duel_CombatStarted", "[Castle duel] Both sides are ready. The duel begins.");

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
