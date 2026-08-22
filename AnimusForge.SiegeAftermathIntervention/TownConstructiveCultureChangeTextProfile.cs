namespace AnimusForge.SiegeAftermathIntervention;

public static class TownConstructiveCultureChangeTextProfile
{
    public const uint SuccessMessageColor = 0xFFB6F7A8u;

    public static string BuildPromptContext(
        string settlementName,
        string currentCultureName,
        string targetCultureName,
        bool available,
        TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(textCatalog);
        string template = available
            ? text.ConstructiveCultureChangeContextTemplate
            : text.ConstructiveCultureChangeUnavailableContextTemplate;
        return Format(template, settlementName, currentCultureName, targetCultureName);
    }

    public static string BuildSuccessMessage(
        string settlementName,
        string currentCultureName,
        string targetCultureName,
        TownPromptTextCatalog textCatalog)
    {
        return Format(
            TownPromptTextCatalog.Resolve(textCatalog).ConstructiveCultureChangeSuccessMessageTemplate,
            settlementName,
            currentCultureName,
            targetCultureName);
    }

    public static string BuildMemory(
        string settlementName,
        string currentCultureName,
        string targetCultureName,
        TownPromptTextCatalog textCatalog)
    {
        return Format(
            TownPromptTextCatalog.Resolve(textCatalog).ConstructiveCultureChangeMemoryTemplate,
            settlementName,
            currentCultureName,
            targetCultureName);
    }

    private static string Format(string template, string settlementName, string currentCultureName, string targetCultureName)
    {
        return (template ?? string.Empty)
            .Replace("{settlement}", Normalize(settlementName, "the settlement"))
            .Replace("{current_culture}", Normalize(currentCultureName, "the current culture"))
            .Replace("{target_culture}", Normalize(targetCultureName, "the target culture"));
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
