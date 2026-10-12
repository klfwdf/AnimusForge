namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free wording and throttling constants for soldier questions after a bad civilian-side signal.
/// AF adapters still choose the live soldier and trigger the immediate scene reaction.
/// </summary>
public static class SiegeDestructiveInquiryProfile
{
    public const float InquiryCooldownSeconds = 18f;

    public const string InvalidSoldierMediatedTagReason = "invalid_soldier_mediated_destructive_tag";

    public const string CivilianRobberyReason = "civilian_robbery";

    public static string FactPrefix => TownPromptCatalogResources.Get(false).GetRuntimeText("DestructiveInquiryPrefix");
    public const string FactPrefixEn = "[SIEGE SOLDIER INQUIRY] ";

    public static string BuildInquiryFact(string sourceSpeakerName, string reason)
        => BuildInquiryFact(sourceSpeakerName, reason, false);

    public static string BuildInquiryFact(string sourceSpeakerName, string reason, bool isEnglish)
        => BuildInquiryFact(sourceSpeakerName, reason, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildInquiryFact(string sourceSpeakerName, string reason, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string speaker = string.IsNullOrWhiteSpace(sourceSpeakerName) ? text.GetRuntimeText("DestructiveInquirySpeakerDefault") : sourceSpeakerName.Trim();
        string value = (reason ?? string.Empty).Trim();
        string normalizedReason = value == CivilianRobberyReason ? text.GetRuntimeText("DestructiveInquiryRobberyReason") : (value == InvalidSoldierMediatedTagReason ? text.GetRuntimeText("DestructiveInquiryInvalidReason") : (value.Length == 0 ? text.GetRuntimeText("DestructiveInquiryDefaultReason") : value));
        return TownPromptTextCatalog.FormatTemplate(text.DestructiveInquiryFactTemplate, ("speaker", speaker), ("reason", normalizedReason));
    }

}
