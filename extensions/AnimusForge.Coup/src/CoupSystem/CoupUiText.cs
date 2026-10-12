using TaleWorlds.Localization;

namespace AnimusForge.CoupSystem;

/// <summary>
/// Player-facing coup UI text. Dynamic diagnostics, prompt payloads, protocol
/// values, logs and save data remain owned by their callers and are not
/// localized here.
/// </summary>
internal static class CoupUiText
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            if (variables[i].Value is TextObject nested)
                text.SetTextVariable(variables[i].Name, nested);
            else
                text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }

    internal static string Prefix => Text("AF_COUP_UI_PREFIX", "[Coup] ");

    internal static string MinimumTroopsConflict(int minimumTroops, int streetLimit)
        => Text("AF_COUPX_MinimumTroopsConflict", "Coup configuration conflict: minimum assault party size {MIN} exceeds street limit {LIMIT}; please adjust in MCM.",
            ("MIN", minimumTroops), ("LIMIT", streetLimit));

    internal static string InsufficientTier(int current, int required)
        => Text("AF_COUPX_InsufficientTier", "Clan tier insufficient: {CURRENT}/{REQUIRED} tier.",
            ("CURRENT", current), ("REQUIRED", required));

    internal static string InsufficientInfluence(string current, int required)
        => Text("AF_COUPX_InsufficientInfluence", "Influence insufficient: {CURRENT}/{REQUIRED} (checked only, not spent).",
            ("CURRENT", current), ("REQUIRED", required));

    internal static string InsufficientRegulars(int required, int current)
        => Text("AF_COUPX_InsufficientTroops", "At least {REQUIRED} healthy regular soldiers needed, with 1 person left as reserve; currently {CURRENT} people.",
            ("REQUIRED", required), ("CURRENT", current));

    internal static string NamingProgressTitle => Text("AF_COUP_UI_NAMING_PROGRESS_TITLE", "Generating post-coup rebellion name");

    internal static string NamingProgressBody => Text("AF_COUP_UI_NAMING_PROGRESS_BODY",
        "AF's existing naming service is generating a kingdom name and description. Clan defection and kingdom creation will run only after it completes.");

    internal static string NamingFailureTitle => Text("AF_COUP_UI_NAMING_FAILURE_TITLE", "Post-coup rebellion naming failed");

    internal static string NamingFailureBody(string detail)
        => Text("AF_COUP_UI_NAMING_FAILURE_BODY",
            "AF did not return a usable kingdom name and description. No clans have moved or founded a kingdom yet. You can retry the existing naming service or skip this rebellion attempt.{DETAIL}",
            ("DETAIL", string.IsNullOrEmpty(detail) ? string.Empty : "\n" + LocalizeReason(detail)));

    internal static string NamingRetry => Text("AF_COUP_UI_NAMING_RETRY", "Generate Again");

    internal static string NamingSkip => Text("AF_COUP_UI_NAMING_SKIP", "Skip This Attempt");

    internal static string NamingSkipped => Text("AF_COUP_UI_NAMING_SKIPPED",
        "Skipped this post-coup rebellion attempt; clans were not reselected.");

    internal static string PassageBlocked => Text("AF_COUP_UI_PASSAGE_BLOCKED",
        "[Coup] The other passage cannot be used during the battle; withdraw through the battle exit.");

    internal static string GateNotCleared => Text("AF_COUP_UI_GATE_NOT_CLEARED",
        "[Coup] The hall guards at the entrance have not been cleared; the hall cannot be entered yet.");

    internal static string HallObjective => Text("AF_COUP_UI_HALL_OBJECTIVE",
        "[Coup] Subdue the ruler and defeat all hall guards. Falling or withdrawing counts as a defeat.");

    internal static string StreetObjective => Text("AF_COUP_UI_STREET_OBJECTIVE",
        "[Coup] Defeat the guards at the hall entrance, then press F at the entrance to attack the hall. Falling or withdrawing counts as a defeat.");

    internal static string HallBreakthrough => Text("AF_COUP_UI_HALL_BREAKTHROUGH",
        "[Coup] The hall entrance has been breached. Street reinforcements have stopped; enemies already present will continue fighting. Press F at the entrance to attack the hall.");

    internal static string HallSelectionPrompt => Text("AF_COUP_UI_HALL_SELECTION_PROMPT",
        "[Coup] The hall entrance has been breached. Press F to select soldiers for the hall assault.");

    internal static string KingSubdued => Text("AF_COUP_UI_KING_SUBDUED",
        "[Coup] The ruler has been subdued and the hall is under control. Confirm the former ruler's disposition before leaving and settling the coup.");

    internal static string SaveInvalid => Text("AF_COUP_UI_SAVE_INVALID",
        "The coup save state is invalid; the original record was retained and new coups are disabled. Check the log.");
    internal static string MenuArmedCoup => Text("AF_COUP_UI_MENU_ARMED_COUP", "Armed Coup");
    internal static string MenuRetryWarRegistration => Text("AF_COUP_UI_MENU_RETRY_WAR", "Retry Post-Coup Civil War Registration");
    internal static string MenuEnterStreet => Text("AF_COUP_UI_MENU_ENTER_STREET", "Lead the Coup Assault into the Town");
    internal static string MenuEnterHall => Text("AF_COUP_UI_MENU_ENTER_HALL", "Lead the Coup Assault into the Lord's Hall");
    internal static string MenuVictoryReport => Text("AF_COUP_UI_MENU_VICTORY_REPORT", "View Coup Coronation Report");
    internal static string MenuResumeSettlement => Text("AF_COUP_UI_MENU_RESUME", "Retry Coup Settlement");
    internal static string BeginTooltip => Text("AF_COUP_UI_BEGIN_TOOLTIP",
        "Select an assault party using the coup MCM troop requirements, take the hall, and seize the throne; leave one healthy soldier as a reserve. Failure causes defection with land and war.");
    internal static string EnterTooltip => Text("AF_COUP_UI_ENTER_TOOLTIP",
        "Enter with the selected roster; falling or withdrawing counts as a coup defeat.");
    internal static string NoPendingEntry => Text("AF_COUP_UI_NO_PENDING_ENTRY", "There is no coup assault waiting to enter a scene.");
    internal static string SceneInterfaceUnavailable => Text("AF_COUP_UI_SCENE_INTERFACE_UNAVAILABLE", "The coup scene interface is not ready. Check the log.");
    internal static string EncounterChanged => Text("AF_COUP_UI_ENCOUNTER_CHANGED", "The current town encounter changed; the coup scene cannot be entered.");
    internal static string BlockingTownFlow => Text("AF_COUP_UI_BLOCKING_TOWN_FLOW", "A town conflict or post-battle resolution is still pending.");
    internal static string SceneMissing => Text("AF_COUP_UI_SCENE_MISSING", "The coup target scene does not exist.");
    internal static string SceneOpenFailed => Text("AF_COUP_UI_SCENE_OPEN_FAILED", "The coup scene could not be opened.");
    internal static string CoupDisabled => Text("AF_COUP_UI_DISABLED", "Armed coups are disabled.");
    internal static string SaveRecordInvalid => Text("AF_COUP_UI_SAVE_RECORD_INVALID", "The coup save record is invalid. Resolve the logged error first.");
    internal static string MissionProtectionUnavailable => Text("AF_COUP_UI_MISSION_PROTECTION_UNAVAILABLE", "Coup scene compatibility protection was not registered. Check the log and game version.");
    internal static string SeamUnavailable => Text("AF_COUP_UI_SEAM_UNAVAILABLE", "The coup integration seam is unavailable in this AF version. Check the log.");
    internal static string PendingCoupFeedback => Text("AF_COUP_UI_PENDING_FEEDBACK", "Another coup or coronation report is still awaiting confirmation.");
    internal static string EligibleLeaderOnly => Text("AF_COUP_UI_ELIGIBLE_LEADER_ONLY", "Only the leader of a formal vassal clan in this kingdom can launch a coup.");
    internal static string OwnTownRequired => Text("AF_COUP_UI_OWN_TOWN_REQUIRED", "You must be in a town belonging to your kingdom.");
    internal static string PlayerCannotFight => Text("AF_COUP_UI_PLAYER_CANNOT_FIGHT", "The player cannot join a battle right now.");
    internal static string RulerMustBeHere => Text("AF_COUP_UI_RULER_MUST_BE_HERE", "The current ruler of your kingdom must be in this town and not imprisoned.");
    internal static string BlockingBattle => Text("AF_COUP_UI_BLOCKING_BATTLE", "A siege, battle, or special mission is in progress; a coup cannot be launched.");
    internal static string PendingConflict => Text("AF_COUP_UI_PENDING_CONFLICT", "A town conflict or post-battle resolution is still pending.");
    internal static string LeaveArmy => Text("AF_COUP_UI_LEAVE_ARMY", "Leave the army before launching a coup with your own party.");
    internal static string BeginTitle => Text("AF_COUP_UI_BEGIN_TITLE", "Armed Coup");
    internal static string BeginBody(string ruler, string town, int tier, float influence, int minimumTroops,
        int streetLimit, int hallLimit, int gateLimit, int hallGuardLimit, int waveSize, float waveInterval, int maxWaves)
        => Text("AF_COUP_UI_BEGIN_BODY",
            "Target: {RULER}\nLocation: {TOWN}\nEntry requirements: clan tier at least {TIER}; influence at least {INFLUENCE} (checked but not spent; 0 means unrestricted).\nSelect at least {MIN_TROOPS} healthy regular soldiers and leave one additional reserve.\nAssault party: up to {STREET_LIMIT} in the street and {HALL_LIMIT} in the hall (excluding the player); at least one soldier must remain as reserve.\nGuards: up to {GATE_LIMIT} at the entrance and {HALL_GUARD_LIMIT} in the hall (excluding the ruler), based on available troops.\nReinforcements: up to {WAVE_SIZE} per wave, every {WAVE_INTERVAL} seconds, with at most {MAX_WAVES} active waves.\nThese parameters are fixed for this coup. Once you enter, falling or withdrawing makes the clan defect with its land and declare war on the former kingdom.",
            ("RULER", ruler), ("TOWN", town), ("TIER", tier), ("INFLUENCE", influence), ("MIN_TROOPS", minimumTroops),
            ("STREET_LIMIT", streetLimit), ("HALL_LIMIT", hallLimit), ("GATE_LIMIT", gateLimit),
            ("HALL_GUARD_LIMIT", hallGuardLimit), ("WAVE_SIZE", waveSize), ("WAVE_INTERVAL", waveInterval), ("MAX_WAVES", maxWaves));
    internal static string BeginConfirm => Text("AF_COUP_UI_BEGIN_CONFIRM", "Select Assault Party");
    internal static string Cancel => Text("AF_COUP_UI_CANCEL", "Cancel");
    internal static string InsufficientTroops => Text("AF_COUP_UI_INSUFFICIENT_TROOPS", "There are not enough healthy soldiers for the assault party and reserve. The coup was cancelled.");
    internal static string StreetSelectionTitle => Text("AF_COUP_UI_STREET_SELECTION_TITLE", "Coup Assault Party");
    internal static string SelectionPartyRequired => Text("AF_COUP_UI_SELECTION_PARTY_REQUIRED", "Troop selection requires a valid player party.");
    internal static string SelectionDetachedRosterRequired => Text("AF_COUP_UI_SELECTION_DETACHED_ROSTER_REQUIRED", "Troop selection requires a separate temporary roster.");
    internal static TextObject AvailableSoldiers => new TextObject("{=AF_COUP_UI_AVAILABLE_SOLDIERS}Available soldiers");
    internal static TextObject SelectionPartyName(string title, int minimum, int limit)
    {
        var text = new TextObject("{=AF_COUP_UI_SELECTION_PARTY_NAME}{TITLE} ({MINIMUM} to {LIMIT} soldiers)");
        text.SetTextVariable("TITLE", title ?? string.Empty);
        text.SetTextVariable("MINIMUM", minimum);
        text.SetTextVariable("LIMIT", limit);
        return text;
    }
    internal static TextObject SelectionCountHint(int minimum, int limit)
    {
        var text = new TextObject("{=AF_COUP_UI_SELECTION_COUNT_HINT}Select {MINIMUM} to {LIMIT} soldiers.");
        text.SetTextVariable("MINIMUM", minimum);
        text.SetTextVariable("LIMIT", limit);
        return text;
    }
    internal static string SelectionInvalid(int minimum) => Text("AF_COUP_UI_SELECTION_INVALID",
        "The troop selection is no longer valid. Select at least {MINIMUM} healthy regular soldiers and leave one healthy soldier as reserve.", ("MINIMUM", minimum));
    internal static string RosterChanged => Text("AF_COUP_UI_ROSTER_CHANGED", "The party roster changed; select again.");
    internal static string StreetRegistered => Text("AF_COUP_UI_STREET_REGISTERED", "The assault party is registered. Use the town menu to lead it into the town.");
    internal static string ContextChanged => Text("AF_COUP_UI_CONTEXT_CHANGED", "The ruler, player, or town state changed; the coup cannot continue.");
    internal static string AdmissionUnavailable => Text("AF_COUP_UI_ADMISSION_UNAVAILABLE", "The coup admission record is unavailable.");
    internal static string NoAssaultTroops => Text("AF_COUP_UI_NO_ASSAULT_TROOPS", "No assault soldiers remain; withdraw from the coup.");
    internal static string CombatInterrupted => Text("AF_COUP_UI_COMBAT_INTERRUPTED", "The assault was withdrawn or the battle was interrupted.");
    internal static string ResolutionPendingPrefix => Text("AF_COUP_UI_RESOLUTION_PENDING_PREFIX", "Coup settlement is incomplete: ");
    internal static string SuspendedPrefix => Text("AF_COUP_UI_SUSPENDED_PREFIX", "Coup temporarily suspended: ");
    internal static string PoliticalChangesRetained => Text("AF_COUP_UI_POLITICAL_CHANGES_RETAINED", " Political changes already made were retained; retry the remaining settlement from the town menu.");
    internal static string CasualtiesRetained => Text("AF_COUP_UI_CASUALTIES_RETAINED", " Casualties were retained; no victory or defection penalty was granted.");
    internal static string ReportNotOpened => Text("AF_COUP_UI_REPORT_NOT_OPENED", "The coronation result was settled, but the report did not open. Retry from the target town's coup report menu.");
    internal static string HallSelectionTitle => Text("AF_COUP_UI_HALL_SELECTION_TITLE", "Assault the Lord's Hall");
    internal static string HallSelectionLimitExceeded => Text("AF_COUP_UI_HALL_SELECTION_LIMIT", "The hall selection exceeds this coup's party limit.");
    internal static string HallRosterMismatch => Text("AF_COUP_UI_HALL_ROSTER_MISMATCH", "The hall selection roster does not match.");
    internal static string HallSelectionAbandoned => Text("AF_COUP_UI_HALL_SELECTION_ABANDONED", "The hall assault was abandoned.");
    internal static string HallRegistered => Text("AF_COUP_UI_HALL_REGISTERED", "The hall assault party is selected. Use the town menu to lead it into the lord's hall.");
    internal static string HallSelectionCancelled => Text("AF_COUP_UI_HALL_SELECTION_CANCELLED", "The hall assault was cancelled; withdraw from the coup.");
    internal static string ReleaseFormerRuler => Text("AF_COUP_UI_RELEASE_RULER", "Force the former ruler to abdicate and release him");
    internal static string CaptureFormerRuler => Text("AF_COUP_UI_CAPTURE_RULER", "Capture the former ruler");
    internal static string CaptureExcludesFamily => Text("AF_COUP_UI_CAPTURE_EXCLUDES_FAMILY", "Capturing the former ruler excludes his clan from this rebellion.");
    internal static string CaptureUnavailable => Text("AF_COUP_UI_CAPTURE_UNAVAILABLE", "Captivity protection is unavailable in this version; the former ruler can only be released.");
    internal static string FormerRulerFallback => Text("AF_COUP_UI_FORMER_RULER_FALLBACK", "former ruler");
    internal static string DispositionTitle => Text("AF_COUP_UI_DISPOSITION_TITLE", "Coup Victory: Former Ruler's Disposition");
    internal static string DispositionBody(string name) => Text("AF_COUP_UI_DISPOSITION_BODY",
        "Ruler {NAME} has been subdued and the hall is under control.\nChoose the former ruler's disposition; the assault will end and the throne and town will be settled afterward.\nReleasing him may allow his clan to join the rebellion; capturing him excludes that clan from this rebellion.", ("NAME", name));
    internal static string DispositionConfirm => Text("AF_COUP_UI_DISPOSITION_CONFIRM", "Confirm Disposition and Leave");
    internal static string DecideLater => Text("AF_COUP_UI_DECIDE_LATER", "Decide Later");
    internal static string DispositionNotConfirmed => Text("AF_COUP_UI_DISPOSITION_NOT_CONFIRMED", "The former ruler's disposition is not confirmed; remain in the hall and choose again with the battle exit key or an exit.");
    internal static string LeaveNotReady => Text("AF_COUP_UI_LEAVE_NOT_READY", "The victory exit is not ready.");
    internal static string DispositionLeaveFailed(string detail) => Text("AF_COUP_UI_DISPOSITION_LEAVE_FAILED", "The disposition was confirmed, but leaving the scene has not completed: {DETAIL}", ("DETAIL", detail));
    internal static string PoliticalSettlementComplete => Text("AF_COUP_UI_POLITICAL_SETTLEMENT_COMPLETE", "Political settlement is complete. The coronation scene and report will be shown next.");
    internal static string CoronationUnavailable => Text("AF_COUP_UI_CORONATION_UNAVAILABLE", "The native coronation scene is unavailable; the coronation result was retained and the report will be shown.");
    internal static string VictoryTitle => Text("AF_COUP_UI_VICTORY_TITLE", "Armed Coup Successful");
    internal static string ConfirmReturnTown => Text("AF_COUP_UI_CONFIRM_RETURN_TOWN", "Confirm and Return to Town");
    internal static string FailureCommitted => Text("AF_COUP_UI_FAILURE_COMMITTED", "The coup failed. The reserve party withdrew and recovered. Your clan kept its land, left the former kingdom, and is now at war with it.");

    internal static string VictoryReport(string kingdom, string ruler, string town, string ownerClan, string king,
        string custody, int alliedKilled, int alliedWounded, int defenderKilled, int defenderWounded)
        => Text("AF_COUP_UI_VICTORY_REPORT",
            "The coup is complete.\n\nKingdom: {KINGDOM}\nCurrent ruler: {RULER}\nTown taken: {TOWN}\nTown owner: {OWNER}\nFormer ruler: {KING} {CUSTODY}\n\nRecorded casualties:\nAssault party: {ALLY_KILLED} killed, {ALLY_WOUNDED} wounded\nDefenders: {DEF_KILLED} killed, {DEF_WOUNDED} wounded\n\nAftermath: rebellion eligibility was assessed and the request was registered; whether a rebellion actually begins depends on later campaign results.",
            ("KINGDOM", kingdom), ("RULER", ruler), ("TOWN", town), ("OWNER", ownerClan), ("KING", king), ("CUSTODY", custody),
            ("ALLY_KILLED", alliedKilled), ("ALLY_WOUNDED", alliedWounded), ("DEF_KILLED", defenderKilled), ("DEF_WOUNDED", defenderWounded));
    internal static string FormerRulerHeld => Text("AF_COUP_UI_FORMER_RULER_HELD", "is captured and still held by your party.");
    internal static string FormerRulerNoLongerHeld => Text("AF_COUP_UI_FORMER_RULER_NOT_HELD", "was captured but is no longer held by your party.");
    internal static string FormerRulerReleased => Text("AF_COUP_UI_FORMER_RULER_RELEASED", "was forced to abdicate; you chose not to capture him.");
    internal static string ReportIntro(string player, string kingdom, string town, string king, int allies)
        => Text("AF_COUP_UI_REPORT_INTRO", "{PLAYER} launched an armed coup against {KING} in {TOWN} of {KINGDOM} with {ALLIES} assault troops.",
            ("PLAYER", player), ("KINGDOM", kingdom), ("TOWN", town), ("KING", king), ("ALLIES", allies));
    internal static string ReportProcessLabel => Text("AF_COUP_UI_REPORT_PROCESS_LABEL", "Process: ");
    internal static string ReportNoEntry => Text("AF_COUP_UI_REPORT_NO_ENTRY", "The assault party was assembled, but no actual scene entry was recorded.");
    internal static string ReportStreet => Text("AF_COUP_UI_REPORT_STREET", "entered the town street");
    internal static string ReportGate => Text("AF_COUP_UI_REPORT_GATE", "breached the hall entrance");
    internal static string ReportHall(int allies) => Text("AF_COUP_UI_REPORT_HALL", "led {ALLIES} assault troops into the lord's hall", ("ALLIES", allies));
    internal static string ReportSubdued => Text("AF_COUP_UI_REPORT_SUBDUED", "subdued the ruler");
    internal static string ReportResultLabel => Text("AF_COUP_UI_REPORT_RESULT_LABEL", "Result: ");
    internal static string ReportSuccess(string player, string kingdom, string town, bool captured)
        => Text(captured ? "AF_COUP_UI_REPORT_SUCCESS_CAPTURED" : "AF_COUP_UI_REPORT_SUCCESS_RELEASED", captured
            ? "The coup succeeded: {PLAYER} took the throne of {KINGDOM} and seized {TOWN}; the former ruler was captured."
            : "The coup succeeded: {PLAYER} took the throne of {KINGDOM} and seized {TOWN}; you chose not to capture the former ruler.",
            ("PLAYER", player), ("KINGDOM", kingdom), ("TOWN", town));
    internal static string ReportFailure(bool atWar)
        => Text(atWar ? "AF_COUP_UI_REPORT_FAILURE" : "AF_COUP_UI_REPORT_FAILURE_NO_WAR", atWar
            ? "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land and went to war with it."
            : "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land.");
    internal static string ReportCasualties(int alliedKilled, int alliedWounded, int defenderKilled, int defenderWounded)
        => Text("AF_COUP_UI_REPORT_CASUALTIES", "Recorded casualties: assault party {ALLY_KILLED} killed, {ALLY_WOUNDED} wounded; defenders {DEF_KILLED} killed, {DEF_WOUNDED} wounded.",
            ("ALLY_KILLED", alliedKilled), ("ALLY_WOUNDED", alliedWounded), ("DEF_KILLED", defenderKilled), ("DEF_WOUNDED", defenderWounded));


    internal static string WarRetryHint => Text("AF_COUPX_WarRetryHint", " You can select 'Retry Post-Coup Civil War Registration' from the town menu; the rebel kingdom will not be created again.");
    internal static string NamingContext => Text("AF_COUPX_NamingContext", "\nRebellion background: the player just seized the throne through armed coup; the rebelling clan is the former ruler's clan or clans more loyal to the former ruler; this is a rebellion against usurpation and does not require negative relations with the new ruler. Use this as the background for kingdom naming and description.");
    internal static string RestorationContext => Text("AF_COUPX_RestorationContext", "\nFormer ruler has been released; this war's goal is restoration of the old dynasty; only rebel victory can restore, peace negotiations do not restore.");
    internal static string CapturedContext => Text("AF_COUPX_CapturedContext", "\nFormer ruler is captured; the former ruler and their clan do not participate in this civil war; do not claim the former ruler personally raised rebellion.");
    internal static string NamingLogTarget(string clan) => Text("AF_COUPX_NamingLogTarget", "Post-coup rebellion kingdom naming - {CLAN}", ("CLAN", clan ?? string.Empty));
    internal static string NpcFactSuccess(string player, string place) => Text("AF_COUPX_NpcFactSuccess", "{PLAYER} launched an armed coup in {PLACE} and defeated you; this coup has succeeded.", ("PLAYER", player), ("PLACE", place));
    internal static string NpcFactFailure(string player, string place) => Text("AF_COUPX_NpcFactFailure", "{PLAYER} launched an armed coup against you in {PLACE}, but the assault failed and then withdrew the reserve forces.", ("PLAYER", player), ("PLACE", place));
    internal static string PlayerFactSuccess(string king, string place) => Text("AF_COUPX_PlayerFactSuccess", "You launched an armed coup against {KING} in {PLACE} and succeeded.", ("KING", king), ("PLACE", place));
    internal static string PlayerFactFailure(string king, string place) => Text("AF_COUPX_PlayerFactFailure", "You launched an armed coup against {KING} in {PLACE} and failed; reserve forces withdrew.", ("KING", king), ("PLACE", place));
    internal static string WeeklyTitle(string settlement) => Text("AF_COUPX_WeeklyTitle", "Armed Coup - {SETTLEMENT}", ("SETTLEMENT", settlement));
    internal static string BulletinTimeLabel(string date) => Text("AF_COUPX_BulletinTimeLabel", "Occurrence time: {DATE}", ("DATE", date));
    internal static string DefaultOriginalKingdom => Text("AF_COUPX_DefaultOriginalKingdom", "the original kingdom");

        private static readonly (string SourceKey, string UiKey, string Fallback)[] ReasonDefinitions = new[]
    {
        ("PoliticalIncomplete", "AF_COUPX_PoliticalIncomplete", "Coup victory status or former ruler's disposition incomplete; political settlement forbidden."),
        ("CampaignIdentityChanged", "AF_COUPX_CampaignIdentityChanged", "Campaign identity changed; stopping automatic political settlement."),
        ("ThroneTransferFailed", "AF_COUPX_ThroneTransferFailed", "Throne transfer not effective."),
        ("TownTransferFailed", "AF_COUPX_TownTransferFailed", "Town transfer not effective."),
        ("CaptivityUnavailable", "AF_COUPX_CaptivityUnavailable", "Captivity protection unavailable; please restore compatibility components before settlement."),
        ("KingCaptivityFailed", "AF_COUPX_KingCaptivityFailed", "Former ruler's captivity not effective."),
        ("EventSeamUnavailable", "AF_COUPX_EventSeamUnavailable", "AF event and rebellion system unavailable."),
        ("NewBattleStarted", "AF_COUPX_NewBattleStarted", "New campaign battle has started; cannot automatically settle old coup."),
        ("DefectionInconsistent", "AF_COUPX_DefectionInconsistent", "Defection or war declaration results inconsistent; stopping subsequent settlement."),
        ("FailureFactUnavailable", "AF_COUPX_FailureFactUnavailable", "Coup failure facts temporarily cannot be written."),
        ("ReceptionPartyUnavailable", "AF_COUPX_ReceptionPartyUnavailable", "Reception party unavailable; cannot confirm withdrawal."),
        ("EncounterChanged", "AF_COUPX_EncounterChanged", "Current encounter has changed; cannot execute withdrawal for another encounter."),
        ("PartyNotExited", "AF_COUPX_PartyNotExited", "Reception party has not completely exited town encounter."),
        ("BattleEndedEarly", "AF_COUPX_BattleEndedEarly", "Coup battle ended prematurely."),
        ("PlayerDown", "AF_COUPX_PlayerDown", "Player has fallen."),
        ("VanillaNoPlayer", "AF_COUPX_VanillaNoPlayer", "Vanilla scene did not spawn player."),
        ("ExitDefeat", "AF_COUPX_ExitDefeat", "Withdrew from coup battle through scene exit."),
        ("MissionContextChanged", "AF_COUPX_MissionContextChanged", "Coup mission context has changed."),
        ("NoHallEntrance", "AF_COUPX_NoHallEntrance", "Street has no usable real lord's hall entrance."),
        ("NoKingOrThrone", "AF_COUPX_NoKingOrThrone", "Lord's hall has no king or valid throne/guard spawn point."),
        ("KingSpawnFailed", "AF_COUPX_KingSpawnFailed", "King cannot spawn in hall."),
        ("KingLeftHall", "AF_COUPX_KingLeftHall", "Target king abnormally left hall; coup result not yet established."),
        ("CaptivityProtectionUnavailable", "AF_COUPX_CaptivityProtectionUnavailable", "Coup captivity protection unavailable, or target not yet captured by player."),
        ("SeamNotReady", "AF_COUPX_SeamNotReady", "AF rebellion seam, save, or kingdom not ready."),
        ("CivilWarDisabled", "AF_COUPX_CivilWarDisabled", "Civil war system is disabled; skipping post-coup civil war."),
        ("NoEligibleLoyalist", "AF_COUPX_NoEligibleLoyalist", "Among the former ruler's clan and clans more loyal to the former ruler, no candidates currently meet the conditions to raise an armed rebellion with land."),
        ("StabilityDisabled", "AF_COUPX_StabilityDisabled", "AF kingdom stability and rebellion system is disabled; skipping post-coup rebellion."),
        ("ImmunityEnabled", "AF_COUPX_ImmunityEnabled", "Player kingdom rebellion immunity is enabled; skipping post-coup rebellion."),
        ("IdentityMissing", "AF_COUPX_IdentityMissing", "Ruler identity before or after the coup is missing; cannot determine support relationships."),
        ("FormerClanExcluded", "AF_COUPX_FormerClanExcluded", "Former ruler is captured; their clan will not participate in this civil war."),
        ("SupportInsufficient", "AF_COUPX_SupportInsufficient", "Support for the former ruler does not exceed support for the new ruler."),
        ("StabilityEndSession", "AF_COUPX_StabilityEndSession", "AF kingdom stability and rebellion system is disabled; this session ends."),
        ("ImmunityEndSession", "AF_COUPX_ImmunityEndSession", "Player kingdom rebellion immunity is enabled; this session ends."),
        ("ContextChanged", "AF_COUPX_ContextChanged", "Kingdom, ruler, or original candidate clan leader has changed; this session ends without re-selecting clans."),
        ("SupportChanged", "AF_COUPX_SupportChanged", "Original candidate clan has changed support for the new and old rulers; will not raise rebellion this time."),
        ("CivilWarEndSession", "AF_COUPX_CivilWarEndSession", "Civil war system is disabled; will not create rebels without settlement attribution this time."),
        ("FormerClanIneligible", "AF_COUPX_FormerClanIneligible", "Former ruler's clan can no longer participate in restoration civil war; will not raise rebellion this time."),
        ("ParticipantsUnavailable", "AF_COUPX_ParticipantsUnavailable", "Kingdom already founded, but civil war participant records are temporarily unavailable."),
        ("DynastyNotJoined", "AF_COUPX_DynastyNotJoined", "Former ruler's clan has not yet joined the restoration rebels."),
        ("NamingEmpty", "AF_COUPX_NamingEmpty", "AF naming result is empty."),
        ("ExecutionConsumed", "AF_COUPX_ExecutionConsumed", "Post-coup rebellion execution starting; this assessment has been consumed."),
        ("OutcomeSeamNotReady", "AF_COUPX_OutcomeSeamNotReady", "AF coup fact seam or save not ready."),
        ("OutcomeIncomplete", "AF_COUPX_OutcomeIncomplete", "Coup result has not completed actual settlement; cannot register final facts."),
        ("OutcomeMismatch", "AF_COUPX_OutcomeMismatch", "Same coup ID has different facts; refusing to overwrite repeatedly."),
        ("MemoryIdentityChanged", "AF_COUPX_MemoryIdentityChanged", "Coup memory recovery identity changed; refusing to write repeatedly."),
        ("BulletinRejected", "AF_COUPX_BulletinRejected", "Coup bulletin fact not yet accepted."),
        ("FactsRegistered", "AF_COUPX_FactsRegistered", "Coup facts have been registered to action, memory, weekly, and bulletin entries according to current settings."),
        ("SetsSeamNotReady", "AF_COUPX_SetsSeamNotReady", "AF SETS compatibility bridge not ready."),
        ("CoupOptionsCorrupted", "AF_COUPX_CoupOptionsCorrupted", "Coup battle parameters corrupted."),
        ("SetsLogicNotInScene", "AF_COUPX_SetsLogicNotInScene", "SETS town entry logic not in current scene.")
    };

    private static readonly System.Lazy<System.Collections.Generic.Dictionary<string, (string UiKey, string Fallback)>> ReasonLookup =
        new System.Lazy<System.Collections.Generic.Dictionary<string, (string UiKey, string Fallback)>>(() =>
        {
            var map = new System.Collections.Generic.Dictionary<string, (string UiKey, string Fallback)>(System.StringComparer.Ordinal);
            foreach (var def in ReasonDefinitions)
            {
                string raw = CoupSourceTexts.Get(def.SourceKey);
                map[raw] = (def.UiKey, def.Fallback);
                map[def.Fallback] = (def.UiKey, def.Fallback);
            }
            return map;
        });

    private static readonly System.Lazy<string[]> WarRetryHints = new System.Lazy<string[]>(() => new[] {
            CoupSourceTexts.Get("WarRetryHintCurly"),
            CoupSourceTexts.Get("WarRetryHintCurlySpaced"),
            CoupSourceTexts.Get("WarRetryHintAscii"),
            CoupSourceTexts.Get("WarRetryHintHtml"),
            "You can select 'Retry Post-Coup Civil War Registration' from the town menu; the rebel kingdom will not be created again.",
            " You can select 'Retry Post-Coup Civil War Registration' from the town menu; the rebel kingdom will not be created again."
        });

    private static string StripWarRetryHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        string t = text.TrimEnd();
        foreach (string hint in WarRetryHints.Value)
        {
            if (t.EndsWith(hint, System.StringComparison.Ordinal))
            {
                t = t.Substring(0, t.Length - hint.Length).TrimEnd();
                break;
            }
        }
        return t;
    }

    internal static string LocalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return reason ?? string.Empty;
        string r = reason.Trim();

        if (ReasonLookup.Value.TryGetValue(r, out var matched))
            return Text(matched.UiKey, matched.Fallback);

        string pScene = CoupSourceTexts.Get("Prefix_SceneHandlingFailed");
        if (r.StartsWith(pScene, System.StringComparison.Ordinal))
            return Text("AF_COUPX_SceneHandlingFailed", "Coup scene handling failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pScene.Length))));
        if (r.StartsWith("Coup scene handling failed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_SceneHandlingFailed", "Coup scene handling failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(28))));

        string pHall = CoupSourceTexts.Get("Prefix_HallTransitionFailed");
        if (r.StartsWith(pHall, System.StringComparison.Ordinal))
            return Text("AF_COUPX_HallTransitionFailed", "Hall transition failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pHall.Length))));
        if (r.StartsWith("Hall transition failed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_HallTransitionFailed", "Hall transition failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(24))));

        string pReg = CoupSourceTexts.Get("Prefix_RegistrationFailed");
        if (r.StartsWith(pReg, System.StringComparison.Ordinal))
            return Text("AF_COUPX_RegistrationFailed", "Post-coup rebellion registration failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pReg.Length))));
        if (r.StartsWith("Post-coup rebellion registration failed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_RegistrationFailed", "Post-coup rebellion registration failed: {ERROR}", ("ERROR", LocalizeReason(r.Substring("Post-coup rebellion registration failed: ".Length))));

        string pVal = CoupSourceTexts.Get("Prefix_ValidationFailed");
        string sVal = CoupSourceTexts.Get("Suffix_ValidationFailed");
        if (r.StartsWith(pVal, System.StringComparison.Ordinal))
        {
            string sub = r.Substring(pVal.Length);
            int endIdx = sub.IndexOf(sVal, System.StringComparison.Ordinal);
            string reasonPart = endIdx >= 0 ? sub.Substring(0, endIdx) : sub;
            return Text("AF_COUPX_ValidationFailed", "Original candidate clan no longer meets AF rules: {REASON}. This session ends.", ("REASON", LocalizeReason(reasonPart)));
        }
        if (r.StartsWith("Original candidate clan no longer meets AF rules: ", System.StringComparison.Ordinal))
        {
            string sub = r.Substring(50);
            int endIdx = sub.IndexOf(" This session ends.", System.StringComparison.Ordinal);
            string reasonPart = endIdx >= 0 ? sub.Substring(0, endIdx) : sub;
            return Text("AF_COUPX_ValidationFailed", "Original candidate clan no longer meets AF rules: {REASON}. This session ends.", ("REASON", LocalizeReason(reasonPart)));
        }

        string pWar = CoupSourceTexts.Get("Prefix_WarRegistrationPending");
        if (r.StartsWith(pWar, System.StringComparison.Ordinal))
        {
            string error = StripWarRetryHint(r.Substring(pWar.Length));
            return Text("AF_COUPX_WarRegistrationPending", "Rebel kingdom established; civil war registration awaiting retry: {ERROR}", ("ERROR", LocalizeReason(error)));
        }
        if (r.StartsWith("Rebel kingdom established; civil war registration awaiting retry: ", System.StringComparison.Ordinal))
        {
            string error = StripWarRetryHint(r.Substring(66));
            return Text("AF_COUPX_WarRegistrationPending", "Rebel kingdom established; civil war registration awaiting retry: {ERROR}", ("ERROR", LocalizeReason(error)));
        }

        string pReb = CoupSourceTexts.Get("Prefix_RebellionIncomplete");
        if (r.StartsWith(pReb, System.StringComparison.Ordinal))
            return Text("AF_COUPX_RebellionIncomplete", "Rebellion not completed: {MESSAGE}", ("MESSAGE", LocalizeReason(r.Substring(pReb.Length))));
        if (r.StartsWith("Rebellion not completed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_RebellionIncomplete", "Rebellion not completed: {MESSAGE}", ("MESSAGE", LocalizeReason(r.Substring(25))));

        string pSet = CoupSourceTexts.Get("Prefix_SettlementException");
        if (r.StartsWith(pSet, System.StringComparison.Ordinal))
            return Text("AF_COUPX_SettlementException", "Rebellion settlement exception; changes made were retained and founding will not be repeated: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pSet.Length))));
        if (r.StartsWith("Rebellion settlement exception; changes made were retained and founding will not be repeated: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_SettlementException", "Rebellion settlement exception; changes made were retained and founding will not be repeated: {ERROR}", ("ERROR", LocalizeReason(r.Substring("Rebellion settlement exception; changes made were retained and founding will not be repeated: ".Length))));

        string pSeam = CoupSourceTexts.Get("Prefix_SeamException");
        if (r.StartsWith(pSeam, System.StringComparison.Ordinal))
            return Text("AF_COUPX_SeamException", "Post-coup rebellion seam exception; this session ends: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pSeam.Length))));
        if (r.StartsWith("Post-coup rebellion seam exception; this session ends: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_SeamException", "Post-coup rebellion seam exception; this session ends: {ERROR}", ("ERROR", LocalizeReason(r.Substring(55))));

        string pMemRej = CoupSourceTexts.Get("Prefix_MemoryRejected");
        if (r.StartsWith(pMemRej, System.StringComparison.Ordinal))
            return Text("AF_COUPX_MemoryRejected", "Coup memory not accepted: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pMemRej.Length))));
        if (r.StartsWith("Coup memory not accepted: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_MemoryRejected", "Coup memory not accepted: {ERROR}", ("ERROR", LocalizeReason(r.Substring(26))));

        string pMemInc = CoupSourceTexts.Get("Prefix_MemoryIncomplete");
        if (r.StartsWith(pMemInc, System.StringComparison.Ordinal))
            return Text("AF_COUPX_MemoryIncomplete", "Coup memory not completed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pMemInc.Length))));
        if (r.StartsWith("Coup memory not completed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_MemoryIncomplete", "Coup memory not completed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(27))));

        string pFact = CoupSourceTexts.Get("Prefix_FactRegistrationFailed");
        if (r.StartsWith(pFact, System.StringComparison.Ordinal))
            return Text("AF_COUPX_FactRegistrationFailed", "Coup fact registration not completed: {ERROR}", ("ERROR", LocalizeReason(r.Substring(pFact.Length))));
        if (r.StartsWith("Coup fact registration not completed: ", System.StringComparison.Ordinal))
            return Text("AF_COUPX_FactRegistrationFailed", "Coup fact registration not completed: {ERROR}", ("ERROR", LocalizeReason(r.Substring("Coup fact registration not completed: ".Length))));

        return reason;
    }
}
