namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Localized player-facing text used by the staged GCCZ town entry presentation.
/// </summary>
public sealed class TownEntryPresentationTextCatalog
{
    public int Version { get; set; }

    public string UnknownSettlementName { get; set; }

    public string SelectionPromptTemplate { get; set; }

    public string SceneNarrationTemplate { get; set; }

    public string AutomaticSelectionNarrationTemplate { get; set; }

    public string NoFollowersArrivedNarrationTemplate { get; set; }

    public static TownEntryPresentationTextCatalog Resolve(TownEntryPresentationTextCatalog source)
    {
        TownEntryPresentationTextCatalog fallback = CreateEnglishFallback();
        if (source == null)
        {
            return fallback;
        }

        return new TownEntryPresentationTextCatalog
        {
            Version = source.Version > 0 ? source.Version : fallback.Version,
            UnknownSettlementName = Pick(source.UnknownSettlementName, fallback.UnknownSettlementName),
            SelectionPromptTemplate = Pick(source.SelectionPromptTemplate, fallback.SelectionPromptTemplate),
            SceneNarrationTemplate = Pick(source.SceneNarrationTemplate, fallback.SceneNarrationTemplate),
            AutomaticSelectionNarrationTemplate = Pick(source.AutomaticSelectionNarrationTemplate, fallback.AutomaticSelectionNarrationTemplate),
            NoFollowersArrivedNarrationTemplate = Pick(source.NoFollowersArrivedNarrationTemplate, fallback.NoFollowersArrivedNarrationTemplate),
        };
    }

    public static TownEntryPresentationTextCatalog CreateEnglishFallback()
    {
        return new TownEntryPresentationTextCatalog
        {
            Version = 1,
            UnknownSettlementName = "the occupied town",
            SelectionPromptTemplate = "[GCCZ] Choose up to {max_count} people to accompany you. Give orders through ordinary scene dialogue after entering.",
            SceneNarrationTemplate = "You enter {settlement} in battle gear with {arrived_count} selected followers. The occupied streets wait for your first words; leaving still uses the established aftermath flow.",
            AutomaticSelectionNarrationTemplate = "You enter {settlement} in battle gear with {arrived_count} available followers assembled automatically. The occupied streets wait for your first words; leaving still uses the established aftermath flow.",
            NoFollowersArrivedNarrationTemplate = "You enter {settlement} in battle gear, but no follower reaches the street with you. The occupied streets wait for your first words; leaving still uses the established aftermath flow.",
        };
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
