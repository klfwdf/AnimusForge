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
    public bool ApplyVassalageRewardTags(string giverId, string receiverId, ref string text, System.Collections.Generic.List<string> giverFacts, System.Collections.Generic.List<string> receiverFacts) =>
        DiplomacyPoliticalRewardApplication.Apply(new DiplomacyPoliticalRewardPort(giverId, receiverId), DiplomacyPoliticalRewardKind.Vassalage, ref text, giverFacts, receiverFacts);
    public bool ApplyKingdomAnnexationRewardTags(string giverId, string receiverId, ref string text, System.Collections.Generic.List<string> giverFacts, System.Collections.Generic.List<string> receiverFacts) =>
        DiplomacyPoliticalRewardApplication.Apply(new DiplomacyPoliticalRewardPort(giverId, receiverId), DiplomacyPoliticalRewardKind.Annexation, ref text, giverFacts, receiverFacts);
    public string BuildPrompt(string heroId, string extras) => DiplomacyPromptApplication.Build(new DiplomacyPromptSource(ResolveHero(heroId)), extras);
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
    public string BuildDiplomacyPostprocessContext(string heroId)
    {
        var source = new DiplomacyPostprocessContextSource(ResolveHero(heroId));
        return DiplomacyPostprocessContextApplication.Build(ref source);
    }
    public void ProcessDiplomacyTags(string heroId, ref string text)
    {
        var source = new DiplomacyOralTagSource(ResolveHero(heroId));
        DiplomacyOralTagApplication.Process(source, ref text);
    }
    public bool TryBuildTributePowerContext(string payerId, string receiverId, out AfTributePowerContext context)
    {
        var source = new DiplomacyTributePowerSource(ResolveKingdom(payerId), ResolveKingdom(receiverId));
        return DiplomacyTributePowerApplication.TryBuild(ref source, out context);
    }
}
