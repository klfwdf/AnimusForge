using System;
using System.Globalization;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the one-shot presentation state for one ordinary GCCZ town entry.
/// </summary>
public sealed class TownEntryPresentationSession
{
    private bool _sceneNarrationShown;

    public bool SceneNarrationShown => _sceneNarrationShown;

    public void Reset()
    {
        _sceneNarrationShown = false;
    }

    public string BuildSelectionPrompt(TownEntryPresentationTextCatalog source, int maximumCount)
    {
        TownEntryPresentationTextCatalog text = TownEntryPresentationTextCatalog.Resolve(source);
        return Replace(
            text.SelectionPromptTemplate,
            "{max_count}",
            Math.Max(0, maximumCount).ToString(CultureInfo.InvariantCulture));
    }

    public bool TryBuildSceneNarration(
        TownEntryPresentationTextCatalog source,
        string settlementName,
        int arrivedCount,
        bool usedAutomaticSelection,
        out string narration)
    {
        narration = string.Empty;
        if (_sceneNarrationShown)
        {
            return false;
        }

        TownEntryPresentationTextCatalog text = TownEntryPresentationTextCatalog.Resolve(source);
        string resolvedSettlementName = string.IsNullOrWhiteSpace(settlementName)
            ? text.UnknownSettlementName
            : settlementName.Trim();
        int resolvedArrivedCount = Math.Max(0, arrivedCount);
        string template = resolvedArrivedCount == 0
            ? text.NoFollowersArrivedNarrationTemplate
            : usedAutomaticSelection
                ? text.AutomaticSelectionNarrationTemplate
                : text.SceneNarrationTemplate;

        narration = Replace(template, "{settlement}", resolvedSettlementName);
        narration = Replace(
            narration,
            "{arrived_count}",
            resolvedArrivedCount.ToString(CultureInfo.InvariantCulture));
        if (string.IsNullOrWhiteSpace(narration))
        {
            return false;
        }

        _sceneNarrationShown = true;
        return true;
    }

    private static string Replace(string value, string token, string replacement)
    {
        return (value ?? string.Empty).Replace(token, replacement ?? string.Empty);
    }
}
