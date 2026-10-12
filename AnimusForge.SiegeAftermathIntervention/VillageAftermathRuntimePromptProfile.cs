using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

public static class VillageAftermathRuntimePromptProfile
{
    public const string RuleId = "gccz_village_administration";

    public const string InjectedRuleBlockMarker = "[RULE:village_aftermath]";
    public const string InjectedRuleBlockMarkerEn = "[RULE:village_aftermath]";

    public static string BuildPrompt(string villageName, string boundSettlementName, VillageAftermathAuthorityKind authorityKind)
        => BuildPrompt(villageName, boundSettlementName, authorityKind, false);

    public static string BuildPrompt(string villageName, string boundSettlementName, VillageAftermathAuthorityKind authorityKind, bool isEnglish)
        => BuildPrompt(villageName, boundSettlementName, authorityKind, TownPromptCatalogResources.Get(isEnglish));

    public static string BuildPrompt(string villageName, string boundSettlementName, VillageAftermathAuthorityKind authorityKind, TownPromptTextCatalog textCatalog)
    {
        TownPromptTextCatalog text = textCatalog ?? TownPromptCatalogResources.Get(true);
        string authority = text.GetRuntimeText(authorityKind == VillageAftermathAuthorityKind.DirectOwner ? "VillageDirectOwnerAuthority" : "VillageRulerAuthority");
        return TownPromptTextCatalog.FormatTemplate(text.VillagePromptTemplate, ("village", villageName ?? text.GetRuntimeText("VillageNameDefault")), ("bound", boundSettlementName ?? text.GetRuntimeText("VillageBoundDefault")), ("authority", authority)).Replace("\n", System.Environment.NewLine);
    }
}
