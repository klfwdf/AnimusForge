using System;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private ConversationRewriteTurn _nativeRewriteTurn;
    private NativeConversationAdmission _nativeRewriteAdmission;
    private string _nativeRewritePreviousDisplayText;

    // One main-thread receipt per actual player turn. NPC initiated openings are not
    // editable input; admitting another channel retires this receipt without clearing history.
    private ConversationRewriteTurn BeginNativeRewriteTurn(NativeConversationAdmission admission,
        string playerText, string previousDisplayText)
    {
        InvalidateNativeRewriteTurn();
        _sceneConversationRuntime?.InvalidateRewriteTurn();
        var turn = new ConversationRewriteTurn(playerText);
        _nativeRewriteAdmission = admission;
        _nativeRewritePreviousDisplayText = previousDisplayText;
        _nativeRewriteTurn = turn;
        return turn;
    }

    private void InvalidateNativeRewriteTurn()
    {
        _nativeRewriteTurn?.Invalidate("native_rewrite_context_retired");
        _nativeRewriteTurn = null;
        _nativeRewriteAdmission = null;
        _nativeRewritePreviousDisplayText = null;
    }

    // Existing UI Tick may read this property. It observes stamps and cached busy
    // state only; the click path performs full target and exact history validation.
    public static bool CanRewriteNativeConversationForExternal
    {
        get
        {
            ShoutBehavior owner = CurrentInstance;
            return owner != null && IsBannerlordMainThreadForNativeActions()
                && owner._nativeRewriteTurn?.IsCompleted == true
                && owner._nativeRewriteAdmission != null
                && owner._nativeAdmissionOwner.IsPresentationCurrent(owner._nativeRewriteAdmission.PresentationRevision)
                && owner.IsNativeConversationContextStampCurrent(owner._nativeRewriteAdmission)
                && !owner.NativeAdmissions.IsBusyForUi()
                && !owner.IsSpeechPipelineBusy()
                && !AnimusForgeNativeConversationOverlay.IsRewritePresentationBusy
                && !_j17NativeConversationPlaybackWaitAdapter.HasPendingPlayback;
        }
    }

    public static bool TryRewriteNativeConversationForExternal(out string playerText, out string status)
    {
        playerText = "";
        status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Unavailable", "There is no completed dialogue turn to undo.");
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions()) return false;
        ConversationRewriteTurn turn = owner._nativeRewriteTurn;
        NativeConversationAdmission admission = owner._nativeRewriteAdmission;
        if (turn?.IsCompleted != true || admission == null) return false;
        if (owner.NativeAdmissions.IsBusyForUi() || owner.IsSpeechPipelineBusy()
            || AnimusForgeNativeConversationOverlay.IsRewritePresentationBusy
            || _j17NativeConversationPlaybackWaitAdapter.HasPendingPlayback)
        {
            status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Busy", "Wait until the dialogue and speech have finished before undoing this turn.");
            return false;
        }
        if (!owner._nativeAdmissionOwner.IsPresentationCurrent(admission.PresentationRevision)
            || !owner.IsNativeConversationContextCurrent(admission, out _))
        {
            owner.InvalidateNativeRewriteTurn();
            status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Changed", "The dialogue context has changed. This turn can no longer be undone.");
            return false;
        }
        if (!turn.TryRewrite(out string reason))
        {
            Logger.Log("ConversationRewrite", "native_rewrite_rejected reason=" + (reason ?? "unknown"));
            status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Failed", "This turn could not be undone because its history changed.");
            return false;
        }

        playerText = turn.PlayerText;
        string previousDisplayText = owner._nativeRewritePreviousDisplayText;
        owner.InvalidateNativeRewriteTurn();
        try { AnimusForgeNativeConversationOverlay.RestoreAfterRewrite(previousDisplayText); }
        catch (Exception ex)
        {
            // The history mutation has completed. An optional UI repaint must not
            // lose the original player input or report that those records still exist.
            Logger.Log("ConversationRewrite", "native_rewrite_display_restore_failed error=" + ex.Message);
        }
        status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Done", "Dialogue undone. Edit your line and send it again. Completed game actions are retained.");
        return true;
    }
}
