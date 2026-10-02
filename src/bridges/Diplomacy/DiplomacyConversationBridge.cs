using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// AF channel edge: map the existing live target to its stable identity, then route.
// No eligibility, tag grammar, game action, scan, cache or fallback belongs here.
internal static class DiplomacyConversationBridge
{
    internal static void ApplyExternalPrestigeDelta(string kingdomId, int delta, string reason) =>
        DiplomacyModuleServices.World.ApplyExternalPrestigeDelta(kingdomId, delta, reason);
    internal static bool ApplyVassalageRewardTags(Hero giver, Hero receiver, ref string text, System.Collections.Generic.List<string> giverFacts, System.Collections.Generic.List<string> receiverFacts) =>
        DiplomacyModuleServices.Conversation.ApplyVassalageRewardTags(giver?.StringId, receiver?.StringId, ref text, giverFacts, receiverFacts);
    internal static bool ApplyKingdomAnnexationRewardTags(Hero giver, Hero receiver, ref string text, System.Collections.Generic.List<string> giverFacts, System.Collections.Generic.List<string> receiverFacts) =>
        DiplomacyModuleServices.Conversation.ApplyKingdomAnnexationRewardTags(giver?.StringId, receiver?.StringId, ref text, giverFacts, receiverFacts);
    internal static string BuildDiplomacyMemory(Hero hero, string kingdomOverride, string input,
        System.Collections.Generic.IReadOnlyList<string> ruleIds, bool proactive) =>
        DiplomacyModuleServices.World.BuildMemory(hero?.StringId, kingdomOverride, input, ruleIds, proactive);
    internal static string BuildDiplomacyPrompt(Hero hero, string extras) => DiplomacyModuleServices.Conversation.BuildPrompt(hero?.StringId, extras);
    internal static bool CanInjectDiplomacyRuleForExternal(Hero hero, CharacterObject character = null) =>
        DiplomacyModuleServices.Conversation.CanInjectDiplomacyRule((hero ?? character?.HeroObject)?.StringId);
    internal static bool CanUseDiplomacyActionPostprocessForExternal(Hero hero, CharacterObject character = null) =>
        DiplomacyModuleServices.Conversation.CanUseDiplomacyActionPostprocess((hero ?? character?.HeroObject)?.StringId);
    internal static bool CanUseFullDiplomacyActionPostprocessForExternal(Hero hero, CharacterObject character = null) =>
        DiplomacyModuleServices.Conversation.CanUseFullDiplomacyActionPostprocess((hero ?? character?.HeroObject)?.StringId);
    internal static bool CanUseNpcSovereignDeclareWarPostprocessForExternal(Hero hero, CharacterObject character = null) =>
        DiplomacyModuleServices.Conversation.CanUseNpcSovereignDeclareWarPostprocess((hero ?? character?.HeroObject)?.StringId);
    internal static bool CanUseIndependentClanPeaceForExternal(Hero hero, CharacterObject character = null) =>
        DiplomacyModuleServices.Conversation.CanUseIndependentClanPeace((hero ?? character?.HeroObject)?.StringId);
    internal static bool IsIndependentClanPeacePostprocessTag(string tag) => DiplomacyModuleServices.Conversation.IsIndependentClanPeacePostprocessTag(tag);
    internal static string BuildDiplomacyPostprocessContext(Hero hero) => DiplomacyModuleServices.Conversation.BuildDiplomacyPostprocessContext(hero?.StringId);
    internal static void ProcessDiplomacyTagsDispatch(Hero hero, ref string text) => DiplomacyModuleServices.Conversation.ProcessDiplomacyTags(hero?.StringId, ref text);
    internal static bool CanDiscussWorldDiplomacyForExternal(Hero hero) => DiplomacyModuleServices.World.CanDiscuss(hero?.StringId);
    internal static bool TryBuildProactiveDiscussionForExternal(Hero hero, out string key, out string fact, out float urgency) =>
        DiplomacyModuleServices.World.TryBuildProactiveDiscussion(hero?.StringId, out key, out fact, out urgency);
    internal static bool TryBuildTributePowerContext(Kingdom payer, Kingdom receiver, out AfTributePowerContext context) =>
        DiplomacyModuleServices.Conversation.TryBuildTributePowerContext(payer?.StringId, receiver?.StringId, out context);
}
