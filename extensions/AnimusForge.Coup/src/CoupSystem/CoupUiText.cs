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

}
