using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
// Compatibility only; channel commits enter through the AF bridge.
internal static class DiplomacyCrossDomainActionOwner
{
    internal static bool ApplyVassalageRewardTags(Hero giver, Hero receiver, ref string text, List<string> giverFacts, List<string> receiverFacts) =>
        DiplomacyConversationBridge.ApplyVassalageRewardTags(giver, receiver, ref text, giverFacts, receiverFacts);
    internal static bool ApplyKingdomAnnexationRewardTags(Hero giver, Hero receiver, ref string text, List<string> giverFacts, List<string> receiverFacts) =>
        DiplomacyConversationBridge.ApplyKingdomAnnexationRewardTags(giver, receiver, ref text, giverFacts, receiverFacts);
}
