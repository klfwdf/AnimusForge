using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Module-owned game adapter. Hero/kingdom lookup uses the campaign object index, never a world scan.
internal sealed class DiplomacyConversationModuleAdapter : IDiplomacyConversationPort
{
    private static Hero ResolveHero(string id) => DiplomacyIdentityResolver.Hero(id);
    private static Kingdom ResolveKingdom(string id) => DiplomacyIdentityResolver.Kingdom(id);
    public bool CanInjectDiplomacyRule(string heroId) => DiplomacyBehavior.CanInjectDiplomacyRuleForExternal(ResolveHero(heroId));
    public bool CanUseDiplomacyActionPostprocess(string heroId) => DiplomacyBehavior.CanUseDiplomacyActionPostprocessForExternal(ResolveHero(heroId));
    public bool CanUseFullDiplomacyActionPostprocess(string heroId) => DiplomacyBehavior.CanUseFullDiplomacyActionPostprocessForExternal(ResolveHero(heroId));
    public bool CanUseNpcSovereignDeclareWarPostprocess(string heroId) => DiplomacyBehavior.CanUseNpcSovereignDeclareWarPostprocessForExternal(ResolveHero(heroId));
    public bool CanUseIndependentClanPeace(string heroId) => DiplomacyBehavior.CanUseIndependentClanPeaceForExternal(ResolveHero(heroId));
    public bool IsIndependentClanPeacePostprocessTag(string tag) => DiplomacyBehavior.IsIndependentClanPeacePostprocessTag(tag);
    public string BuildDiplomacyPostprocessContext(string heroId) => DiplomacyBehavior.BuildDiplomacyPostprocessContext(ResolveHero(heroId));
    public void ProcessDiplomacyTags(string heroId, ref string text) => DiplomacyBehavior.ProcessDiplomacyTagsDispatch(ResolveHero(heroId), ref text);
    public bool TryBuildTributePowerContext(string payerId, string receiverId, out AfTributePowerContext context) =>
        DiplomacyBehavior.TryBuildTributePowerContext(ResolveKingdom(payerId), ResolveKingdom(receiverId), out context);
}
