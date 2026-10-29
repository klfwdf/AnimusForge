using AnimusForge.DiplomacyDialogue;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal static bool CaptureConversationCommitmentReview(Hero npc, string tags)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        return owner?.IsLiveCampaign == true && owner._orchestration.CaptureSpeechReview(npc?.StringId,
            npc?.Clan?.Kingdom?.StringId, DiplomacyDialogueSourceScope.Current, tags);
    }
    internal static bool UseFormalDiplomacyForConversation => IsWorldDiplomacyEnabled();
    internal static string SubmitLegacyOralCommitment(Hero npc, string action, string payload) =>
        Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration.SubmitLegacyOralCommitment(
            npc?.StringId, npc?.Clan?.Kingdom?.StringId, action, payload, DiplomacyDialogueSourceScope.Current) ?? "";
    public static string SubmitOralDiplomaticCommitment(Hero npc, string payload)
        => SubmitOralDiplomaticCommitment(npc, payload, out _);

    public static string SubmitOralDiplomaticCommitment(Hero npc, string payload, out bool registered)
    {
        registered = false;
        var owner = Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        if (owner == null) return DiplomacyMiscPromptTexts.Get("OralDiplomacy_NotReady", "Diplomatic commitment not submitted: world diplomacy system is not ready.");
        if (!owner.IsLiveCampaign) return owner.UnavailableReason;
        if (owner._orchestration == null) return DiplomacyMiscPromptTexts.Get("OralDiplomacy_Uncommitted", "Diplomatic commitment not submitted: world diplomacy system is not enabled.");
        return owner._orchestration.SubmitOralDiplomaticCommitment(npc?.StringId, npc?.Clan?.Kingdom?.StringId,
            payload, DiplomacyDialogueSourceScope.Current, out registered);
    }
    public static string ControlOralDiplomaticCommitment(Hero npc, string payload)
    {
        return Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
            .ControlOralDiplomaticCommitment(npc?.StringId, npc?.Clan?.Kingdom?.StringId, payload, DiplomacyDialogueSourceScope.Current) ?? "";
    }
    public static string BuildOralArrangementContext(Hero npc)
    {
        return Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>()?._orchestration
            .BuildOralArrangementContext(npc?.StringId, npc?.Clan?.Kingdom?.StringId) ?? "";
    }
}
