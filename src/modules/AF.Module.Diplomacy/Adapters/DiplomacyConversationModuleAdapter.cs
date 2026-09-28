using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Module-owned game adapter. Hero/kingdom lookup uses the campaign object index, never a world scan.
internal sealed class DiplomacyConversationModuleAdapter : IDiplomacyConversationPort
{
    private sealed class EligibilitySource : IDiplomacyConversationEligibilitySource
    {
        public DiplomacyConversationEligibilitySnapshot Capture(string heroId) =>
            DiplomacyBehavior.CaptureEligibilitySnapshot(ResolveHero(heroId));
    }

    private static readonly IDiplomacyConversationEligibilitySource Eligibility = new EligibilitySource();
    private static Hero ResolveHero(string id) => DiplomacyIdentityResolver.Hero(id);
    private static Kingdom ResolveKingdom(string id) => DiplomacyIdentityResolver.Kingdom(id);
    public bool CanInjectDiplomacyRule(string heroId) => DiplomacyConversationEligibilityApplication.CanInject(Eligibility, heroId);
    public bool CanUseDiplomacyActionPostprocess(string heroId) => DiplomacyConversationEligibilityApplication.CanUseAction(Eligibility, heroId);
    public bool CanUseFullDiplomacyActionPostprocess(string heroId) => DiplomacyConversationEligibilityApplication.CanUseFull(Eligibility, heroId);
    public bool CanUseNpcSovereignDeclareWarPostprocess(string heroId) => DiplomacyConversationEligibilityApplication.CanUseNpcDeclareWar(Eligibility, heroId);
    public bool CanUseIndependentClanPeace(string heroId)
    {
        var source = new DiplomacyIndependentPeaceSource(ResolveHero(heroId));
        return DiplomacyIndependentPeaceApplication.CanUse(ref source);
    }
    public bool IsIndependentClanPeacePostprocessTag(string tag) => DiplomacyConversationEligibilityApplication.IsIndependentClanPeaceTag(tag);
    public string BuildDiplomacyPostprocessContext(string heroId) => DiplomacyBehavior.BuildDiplomacyPostprocessContext(ResolveHero(heroId));
    public void ProcessDiplomacyTags(string heroId, ref string text) => DiplomacyBehavior.ProcessDiplomacyTagsDispatch(ResolveHero(heroId), ref text);
    public bool TryBuildTributePowerContext(string payerId, string receiverId, out AfTributePowerContext context) =>
        DiplomacyBehavior.TryBuildTributePowerContext(ResolveKingdom(payerId), ResolveKingdom(receiverId), out context);
}
