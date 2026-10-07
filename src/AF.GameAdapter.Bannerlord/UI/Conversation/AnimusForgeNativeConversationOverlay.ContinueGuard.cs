using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

namespace AnimusForge;

public sealed partial class AnimusForgeNativeConversationOverlay
{
    // Bound once when the overlay is created. Map conversations do not require a Mission.
    private readonly ConversationManager _continueGuardManager = Campaign.Current?.ConversationManager;
    private readonly long _continueGuardSaveGeneration = SaveRuntimeGuard.CaptureGeneration();

    internal static bool IsAiModeBlockingNativeContinue()
    {
        AnimusForgeNativeConversationOverlay overlay = _activeOverlay;
        return overlay != null && !overlay._isClosed
            && overlay._dataSource?.IsCustomAnswerVisible == true
            && SaveRuntimeGuard.IsCurrentGeneration(overlay._continueGuardSaveGeneration)
            && overlay._continueGuardManager != null
            && ReferenceEquals(overlay._continueGuardManager, Campaign.Current?.ConversationManager)
            && overlay._continueGuardManager.IsConversationInProgress;
    }
}
