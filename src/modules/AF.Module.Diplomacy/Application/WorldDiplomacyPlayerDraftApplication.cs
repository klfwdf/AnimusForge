using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal static class WorldDiplomacyPlayerDraftApplication
{
    internal static bool TryPrepare(string body, long generation, out WorldDiplomacyPlayerDraftInput input,
        out Func<CancellationToken, Task<WorldDiplomacyPlayerDraftResult>> run, out string error)
    {
        input = null;
        run = null;
        error = "";
        if (string.IsNullOrWhiteSpace(body)) { error = DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftNeedBody", "Write a title or draft notes first."); return false; }
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) { error = DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftStaleWindow", "The current draft window was invalidated by loading a game; please reopen it."); return false; }
        WorldDiplomacyBehavior.GetDiplomaticDeclarationCharacterRange(out int minimum, out int maximum);
        WorldDiplomacyPlayerDraftInput snapshot = new WorldDiplomacyPlayerDraftInput(body, minimum, maximum, generation);
        string preference = DuelSettings.GetSettings()?.WorldDiplomacyPrompt ?? "";
        if (!WorldDiplomacyLlmClient.TryPrepareSingleCall(WorldDiplomacyPlayerDraftRules.BuildMessages(snapshot, preference),
            WorldDiplomacyPlayerDraftRules.GetOutputTokenBudget(snapshot), 90000, "WorldDiplomacyPlayerDraft", generation, out var call, out _))
        {
            error = DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftApiMissing", "The scribe cannot draft documents right now. Check the event/rebellion API or main API settings.");
            return false;
        }
        input = snapshot;
        run = async token =>
        {
            WorldDiplomacyApiCallResult result = await call(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (result?.IsOutputTruncated == true)
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftTruncated", "The draft was truncated by the output limit; original text was kept. Please raise the API output limit."));
            if (result?.Success != true)
                return WorldDiplomacyPlayerDraftResult.Failed(DiplomacyUiLocalization.Text("AF_DIP_UI_Compose_DraftFailed", "The scribe could not draft the document; the original text was kept. Try again."));
            return WorldDiplomacyPlayerDraftRules.Parse(result.Content);
        };
        return true;
    }
}
