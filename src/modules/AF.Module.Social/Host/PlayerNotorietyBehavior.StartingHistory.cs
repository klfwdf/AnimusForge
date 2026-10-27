using System;
using System.Globalization;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior
{
    private static readonly Func<string, string, string> StartingHistoryPromptFormatter = FormatStartingHistoryForPrompt;
    private static readonly Func<string, string, string> StartingHistoryDisplayFormatter = FormatStartingHistoryForDisplay;
    // Session boundary only: no Tick work, model request or scan of game objects.
    private void OnSessionLaunchedForStartingHistory(CampaignGameStarter starter)
    {
#if BANNERLORD_1_5_OR_GREATER
        try
        {
            _state = NormalizeState(_state);
            if (_state.PlayerStartHistoryRecorded) return;
            var options = Campaign.Current?.AdvancedStartData;
            if (options == null || !options.HasValue("StartType")) return;
            if (RecordConfirmedStartingHistory(options.GetValue<string>("StartType")))
                Logger.Log("PlayerNotoriety", "Confirmed advanced-start identity recorded in player history.");
        }
        catch (Exception exception)
        {
            Logger.Log("PlayerNotoriety", "Starting history capture failed: " + exception.Message);
        }
#endif
    }

    private bool RecordConfirmedStartingHistory(string typeId)
    {
        _state = NormalizeState(_state);
        if (!PlayerStartingHistoryRecord.TrySeed(typeId, _state.PlayerStartHistoryRecorded,
            _state.MajorSummary, out string history)) return false;
        _state.MajorSummary = history;
        _state.PlayerStartHistoryRecorded = true;
        _state.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
        return true;
    }

    private static string RenderStartingHistoryForPrompt(string raw, string playerName)
        => PlayerStartingHistoryRecord.Render(raw, playerName, StartingHistoryPromptFormatter);

    private static string FormatStartingHistoryForPrompt(string id, string playerName)
        => string.Format(CultureInfo.InvariantCulture, SocialPromptTexts.Get("player_start_" + id), playerName);

    private static string FormatStartingHistoryForDisplay(string id, string playerName)
        => SocialUiLocalization.Format("AF_SOCIAL_UI_PlayerStart_" + id,
            PlayerStartingHistoryRecord.EnglishUiTemplate(id), ("PLAYER", playerName));
}
