using TaleWorlds.Localization;

namespace AnimusForge.CoupSystem;

/// <summary>
/// Player-facing coup UI text. Dynamic diagnostics, prompt payloads, protocol
/// values, logs and save data remain owned by their callers and are not
/// localized here.
/// </summary>
internal static class CoupUiText
{
    private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        if (variables != null)
        {
            foreach ((string name, object value) in variables)
                text.SetTextVariable(name, value?.ToString() ?? string.Empty);
        }
        return text.ToString();
    }

    internal static string Prefix => Text("AF_COUP_UI_PREFIX", "[Coup] ");

    internal static string NamingProgressTitle => Text("AF_COUP_UI_NAMING_PROGRESS_TITLE", "Generating post-coup rebellion name");

    internal static string NamingProgressBody => Text("AF_COUP_UI_NAMING_PROGRESS_BODY",
        "AF's existing naming service is generating a kingdom name and description. Clan defection and kingdom creation will run only after it completes.");

    internal static string NamingFailureTitle => Text("AF_COUP_UI_NAMING_FAILURE_TITLE", "Post-coup rebellion naming failed");

    internal static string NamingFailureBody(string detail)
        => Text("AF_COUP_UI_NAMING_FAILURE_BODY",
            "AF did not return a usable kingdom name and description. No clans have moved or founded a kingdom yet. You can retry the existing naming service or skip this rebellion attempt.{DETAIL}",
            ("DETAIL", string.IsNullOrEmpty(detail) ? string.Empty : "\n" + detail));

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
        => Text("AF_COUP_UI_REPORT_FAILURE", atWar
            ? "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land and went to war with it."
            : "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land.");
    internal static string ReportCasualties(int alliedKilled, int alliedWounded, int defenderKilled, int defenderWounded)
        => Text("AF_COUP_UI_REPORT_CASUALTIES", "Recorded casualties: assault party {ALLY_KILLED} killed, {ALLY_WOUNDED} wounded; defenders {DEF_KILLED} killed, {DEF_WOUNDED} wounded.",
            ("ALLY_KILLED", alliedKilled), ("ALLY_WOUNDED", alliedWounded), ("DEF_KILLED", defenderKilled), ("DEF_WOUNDED", defenderWounded));

}
