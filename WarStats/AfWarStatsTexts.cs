using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace AFWarStatsTerminal.Localization;

internal static class AfWarStatsTexts
{
    public static string Title => Get("AFWST_Title", "War Statistics");

    public static string CurrentWars => Get("AFWST_CurrentWars", "Current Wars");

    public static string HistoricalWars => Get("AFWST_HistoricalWars", "Historical Wars");

    public static string ClearAll => Get("AFWST_ClearAll", "Clear All");

    public static string HistorySortLabel => Get("AFWST_HistorySortLabel", "Sort");

    public static string CurrentSortLabel => Get("AFWST_CurrentSortLabel", "Sort");

    public static string SelectAll => Get("AFWST_SelectAll", "Select All");

    public static string DeselectAll => Get("AFWST_DeselectAll", "Deselect All");

    public static string DeleteSelected => Get("AFWST_DeleteSelected", "Delete Selected");

    public static string Previous => Get("AFWST_Previous", "Previous");

    public static string Next => Get("AFWST_Next", "Next");

    public static string Close => Get("AFWST_Close", "Close");

    public static string Versus => Get("AFWST_Versus", "VS");

    public static string AttackerLeft => Get("AFWST_AttackerLeft", "Attacker");

    public static string WarColumn => Get("AFWST_WarColumn", "War");

    public static string DefenderRight => Get("AFWST_DefenderRight", "Defender");

    public static string HistoryDetails => Get("AFWST_HistoryDetails", "War Archive");

    public static string SelectedWar => Get("AFWST_SelectedWar", "Selected War");

    public static string PlayerInvolvedBadge => Get("AFWST_PlayerInvolvedBadge", "★ Player Involved");

    public static string DurationLabel => Get("AFWST_DurationLabel", "Duration");

    public static string WarStatusLabel => Get("AFWST_WarStatusLabel", "War Status");

    public static string AttackerRole => Get("AFWST_AttackerRole", "Attacker");

    public static string DefenderRole => Get("AFWST_DefenderRole", "Defender");

    public static string Advantage => Get("AFWST_Advantage", "Advantage");

    public static string Stalemate => Get("AFWST_Stalemate", "Stalemate");

    public static string WearinessLabel => Get("AFWST_WearinessLabel", "War Weariness");

    public static string AttackerFallenLords => Get("AFWST_AttackerFallenLords", "Attacker · Lord Deaths During War");

    public static string DefenderFallenLords => Get("AFWST_DefenderFallenLords", "Defender · Lord Deaths During War");

    public static string NoFallenLords => Get("AFWST_NoFallenLords", "No lord deaths were recorded during this war.");

    public static string DeathTrackingNotice => Get("AFWST_DeathTrackingNotice", "Lord deaths during wars are recorded from v1.3.9 onward.");

    public static string UnknownHero => Get("AFWST_UnknownHero", "Unknown Hero");

    public static string UnknownCause => Get("AFWST_UnknownCause", "Unknown cause of death");

    public static string OpenFailed => Get("AFWST_OpenFailed", "Failed to open War Statistics. Details were written to rgl_log.");

    public static string NotInitializedMessage => Get("AFWST_NotInitializedMessage", "War Statistics is not initialized.");

    public static string ClearTitle => Get("AFWST_ClearTitle", "Clear All War Statistics");

    public static string ClearBody => Get("AFWST_ClearBody", "Clear all historical wars and lord-death records, and reset kills, casualties, wins, and losses for current wars? This action cannot be undone.");

    public static string ConfirmClear => Get("AFWST_ConfirmClear", "Confirm Clear");

    public static string Cancel => Get("AFWST_Cancel", "Cancel");

    public static string RecordsCleared => Get("AFWST_RecordsCleared", "War Statistics records have been cleared.");

    public static string DeleteSelectedTitle => Get("AFWST_DeleteSelectedTitle", "Delete Selected Historical Wars");

    public static string ConfirmDeleteSelected => Get("AFWST_ConfirmDeleteSelected", "Delete Selected");

    public static string RecordsDeleted(int count)
    {
        return Format("AFWST_RecordsDeleted", "{COUNT} historical war records were deleted.", "COUNT", Number(count));
    }

    public static string HistorySubtitle => Get("AFWST_HistorySubtitle", "Historical Wars · Dates, estimated advantage, war weariness, and lord deaths during the war");

    public static string TotalKills => Get("AFWST_TotalKills", "Total Kills");

    public static string ConfirmedDeaths => Get("AFWST_ConfirmedDeaths", "Confirmed Deaths");

    public static string TotalCasualties => Get("AFWST_TotalCasualties", "Total Casualties");

    public static string DeadAndWounded => Get("AFWST_DeadWounded", "Dead + Wounded");

    public static string NoActiveWars => Get("AFWST_NoActiveWars", "There are no active kingdom wars.");

    public static string EndedWars => Get("AFWST_EndedWars", "Ended Wars");

    public static string NoHistoricalWars => Get("AFWST_NoHistoricalWars", "There are no completed war records yet.");

    public static string Status => Get("AFWST_Status", "Status");

    public static string NotInitialized => Get("AFWST_NotInitialized", "Not Initialized");

    public static string WaitingForCampaign => Get("AFWST_WaitingForCampaign", "Waiting for Campaign");

    public static string BehaviorNotInitialized => Get("AFWST_BehaviorNotInitialized", "The War Statistics campaign behavior is not initialized.");

    public static string RecentlyDeclared => Get("AFWST_RecentlyDeclared", "Recently Declared");

    public static string Legacy => Get("AFWST_Legacy", "Legacy");

    public static string LegacyRecord => Get("AFWST_LegacyRecord", "Legacy Record");

    public static string UnknownKingdom => Get("AFWST_UnknownKingdom", "Unknown Kingdom");

    public static string CurrentSubtitle(int rowsPerPage)
    {
        return CurrentSubtitle(rowsPerPage, false);
    }

    public static string CurrentSubtitle(int rowsPerPage, bool scrollable)
    {
        if (scrollable)
        {
            return Get(
                "AFWST_CurrentSubtitleScrollable",
                "Current Wars · Single-page scrolling · War weariness is estimated from duration, casualties, defeats, and territorial losses");
        }

        return Format(
            "AFWST_CurrentSubtitle",
            "Current Wars · {ROWS} per page · War weariness is estimated from duration, casualties, defeats, and territorial losses",
            "ROWS", Number(rowsPerPage));
    }

    public static string PlayerInvolvedCount(int count)
    {
        return Format("AFWST_PlayerInvolvedCount", "Player Involved  {COUNT}", "COUNT", Number(count));
    }

    public static string HistorySelectionCount(int count)
    {
        return Format("AFWST_HistorySelectionCount", "Selected {COUNT}", "COUNT", Number(count));
    }

    public static string DeleteSelectedBody(int count)
    {
        return Format(
            "AFWST_DeleteSelectedBody",
            "Delete {COUNT} selected historical wars and their lord-death records? This action cannot be undone.",
            "COUNT", Number(count));
    }

    public static TextObject[] HistorySortOptions()
    {
        return SortOptions();
    }

    public static TextObject[] CurrentSortOptions()
    {
        return SortOptions();
    }

    private static TextObject[] SortOptions()
    {
        return new[]
        {
            new TextObject("{=AFWST_SortLatest}Latest Wars First"),
            new TextObject("{=AFWST_SortEarliest}Earliest Wars First"),
            new TextObject("{=AFWST_SortDuration}Longest Duration"),
            new TextObject("{=AFWST_SortCasualties}Most Casualties"),
            new TextObject("{=AFWST_SortWeariness}Highest War Weariness"),
            new TextObject("{=AFWST_SortPlayer}Player-Related First")
        };
    }

    public static string Page(int currentPage, int totalPages, int rowsPerPage)
    {
        return Format(
            "AFWST_Page",
            "Page {CURRENT} / {TOTAL} · {ROWS} wars per page",
            "CURRENT", Number(currentPage),
            "TOTAL", Number(totalPages),
            "ROWS", Number(rowsPerPage));
    }

    public static string Kills(int count)
    {
        return Format("AFWST_Kills", "Kills  {COUNT}", "COUNT", Number(count));
    }

    public static string Casualties(int count)
    {
        return Format("AFWST_Casualties", "Casualties  {COUNT}", "COUNT", Number(count));
    }

    public static string Record(int wins, int losses)
    {
        return Format(
            "AFWST_Record",
            "Record  {WINS} W / {LOSSES} L",
            "WINS", Number(wins),
            "LOSSES", Number(losses));
    }

    public static string Weariness(int percent)
    {
        return Format("AFWST_Weariness", "War Weariness  {PERCENT}%", "PERCENT", Number(percent));
    }

    public static string FinalWeariness(int percent)
    {
        return Format("AFWST_FinalWeariness", "Final War Weariness  {PERCENT}%", "PERCENT", Number(percent));
    }

    public static string Percent(int percent)
    {
        return Format("AFWST_Percent", "{PERCENT}%", "PERCENT", Number(percent));
    }

    public static string DateRange(string startDate, string endDate)
    {
        return Format(
            "AFWST_DateRange",
            "{START} — {END}",
            "START", startDate ?? string.Empty,
            "END", endDate ?? string.Empty);
    }

    public static string DeathCount(int count)
    {
        return Format("AFWST_DeathCount", "{COUNT}", "COUNT", Number(count));
    }

    public static string DeathCause(KillCharacterAction.KillCharacterActionDetail cause, string killerName)
    {
        bool hasKiller = !string.IsNullOrWhiteSpace(killerName);
        return cause switch
        {
            KillCharacterAction.KillCharacterActionDetail.DiedInBattle => hasKiller
                ? Format("AFWST_DeathBattleBy", "Killed in battle · Slain by {KILLER}", "KILLER", killerName)
                : Get("AFWST_DeathBattle", "Killed in battle"),
            KillCharacterAction.KillCharacterActionDetail.WoundedInBattle => hasKiller
                ? Format("AFWST_DeathWoundsBy", "Died of battle wounds · Fatally wounded by {KILLER}", "KILLER", killerName)
                : Get("AFWST_DeathWounds", "Died of battle wounds"),
            KillCharacterAction.KillCharacterActionDetail.Executed => hasKiller
                ? Format("AFWST_DeathExecutedBy", "Executed · Executed by {KILLER}", "KILLER", killerName)
                : Get("AFWST_DeathExecuted", "Executed"),
            KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent => hasKiller
                ? Format("AFWST_DeathExecutedAfterBattleBy", "Executed after battle · Executed by {KILLER}", "KILLER", killerName)
                : Get("AFWST_DeathExecutedAfterBattle", "Executed after battle"),
            KillCharacterAction.KillCharacterActionDetail.Murdered => hasKiller
                ? Format("AFWST_DeathMurderedBy", "Murdered · Killed by {KILLER}", "KILLER", killerName)
                : Get("AFWST_DeathMurdered", "Murdered"),
            KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge => Get("AFWST_DeathOldAge", "Died of old age"),
            KillCharacterAction.KillCharacterActionDetail.DiedInLabor => Get("AFWST_DeathInLabor", "Died in childbirth"),
            KillCharacterAction.KillCharacterActionDetail.Lost => Get("AFWST_DeathLost", "Disappeared"),
            _ => hasKiller
                ? Format("AFWST_DeathUnknownBy", "Died · Killed by {KILLER}", "KILLER", killerName)
                : UnknownCause
        };
    }

    public static string DeathMeta(string dateText, string battleName)
    {
        string safeDate = string.IsNullOrWhiteSpace(dateText) ? LegacyRecord : dateText;
        return string.IsNullOrWhiteSpace(battleName)
            ? Format("AFWST_DeathMetaNoBattle", "{DATE} · No associated battle", "DATE", safeDate)
            : Format("AFWST_DeathMeta", "{DATE} · {BATTLE}", "DATE", safeDate, "BATTLE", battleName);
    }

    public static string DurationDays(int days)
    {
        string id = days == 1 ? "AFWST_Day" : "AFWST_Days";
        string fallback = days == 1 ? "{DAYS} day" : "{DAYS} days";
        return Format(id, fallback, "DAYS", Number(days));
    }

    public static string Fiefs(int sideA, int sideB)
    {
        return Format(
            "AFWST_Fiefs",
            "Fiefs {A} : {B}",
            "A", Number(sideA),
            "B", Number(sideB));
    }

    public static string CompactHistory(int winsA, int lossesA, int winsB, int lossesB, int casualties)
    {
        return Format(
            "AFWST_CompactHistory",
            "Attacker {A_WINS}W·{A_LOSSES}L    Defender {B_WINS}W·{B_LOSSES}L    Casualties {CASUALTIES}",
            "A_WINS", Number(winsA),
            "A_LOSSES", Number(lossesA),
            "B_WINS", Number(winsB),
            "B_LOSSES", Number(lossesB),
            "CASUALTIES", Number(casualties));
    }

    public static string WarEnded(string endDate)
    {
        return Format("AFWST_WarEnded", "Truce · {DATE}", "DATE", endDate ?? string.Empty);
    }

    public static string TerritoryOf(string kingdomName)
    {
        return Format("AFWST_TerritoryOf", "Territory of {NAME}", "NAME", kingdomName ?? string.Empty);
    }

    private static string Get(string id, string fallback)
    {
        return Create(id, fallback).ToString();
    }

    private static string Format(string id, string fallback, params string[] variables)
    {
        if (variables == null || variables.Length % 2 != 0)
        {
            throw new ArgumentException("Localization variables must be name/value pairs.", nameof(variables));
        }

        TextObject text = Create(id, fallback);
        for (int i = 0; i < variables.Length; i += 2)
        {
            text.SetTextVariable(variables[i], variables[i + 1] ?? string.Empty);
        }

        return text.ToString();
    }

    private static TextObject Create(string id, string fallback)
    {
        return new TextObject("{=" + id + "}" + fallback);
    }

    private static string Number(int value)
    {
        return value.ToString("N0");
    }
}
