namespace AnimusForge;

public sealed partial class AnimusForgeNativeConversationOverlay
{
    internal static bool IsRewritePresentationBusy => _activeOverlay != null
        && !_activeOverlay._isClosed && _activeOverlay._isSubmitting;

    internal static void RestoreAfterRewrite(string previousDisplayText)
    {
        AnimusForgeNativeConversationOverlay overlay = _activeOverlay;
        if (overlay == null || overlay._isClosed || overlay._isSubmitting) return;
        // Retire already queued presentation callbacks before repainting the retained
        // reply. Their old generation cannot restore the withdrawn text later.
        overlay._submitGeneration++;
        overlay._submitPresentationScope = null;
        overlay.ClearInterruptedPresentation();
        overlay.ClearPendingPostprocessNotice();
        overlay.SetSubmissionDisplayText(overlay._submitGeneration, previousDisplayText ?? "");
        overlay._dataSource.RefreshRewriteAvailability();
    }
}
