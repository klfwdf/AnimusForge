using System;

namespace AnimusForge;

public sealed partial class AnimusForgeNativeConversationOverlay
{
    private ShoutBehavior.NativeConversationPresentationScope _submitPresentationScope;

    // UI generation owns local controls; the request scope separately owns dialogue presentation.
    private bool IsSubmissionPresentationCurrent(int generation)
    {
        return IsSubmitGenerationCurrent(generation) && _submitPresentationScope?.IsCurrent() == true;
    }

    private void RunNativePresentationCallback(int generation, Action callback)
    {
        RunOnMainThread(() =>
        {
            if (!IsSubmitGenerationCurrent(generation))
                return;
            if (!IsSubmissionPresentationCurrent(generation))
            {
                RetireStaleSubmissionPresentation(generation);
                return;
            }
            callback?.Invoke();
        });
    }

    // Called from the existing Tick. This only compares captured identity stamps, never scans agents.
    private void ValidatePendingSubmissionPresentation()
    {
        if (_isSubmitting && _submitPresentationScope != null && !_submitPresentationScope.HasCurrentContext())
            RetireStaleSubmissionPresentation(_submitGeneration);
    }

    private void RetireStaleSubmissionPresentation(int generation)
    {
        if (!IsSubmitGenerationCurrent(generation))
            return;
        _submitGeneration++;
        _submitPresentationScope = null;
        _isSubmitting = false;
        _npcOpeningAutoStarted = false;
        StopWaitingDotsAnimation();
        ClearPendingPostprocessNotice();
        _dataSource.SetBusy(false);
        // Never restore the old NPC text, end another request's stream, or emit a stale ready popup.
        if (_dataSource.IsCustomAnswerVisible)
            FocusInputIfVisible();
        Logger.Log("NativeConversationOverlay", "Retired stale presentation generation=" + generation);
    }

    private bool CompleteNativeSubmissionPresentation(int generation)
    {
        if (!IsSubmitGenerationCurrent(generation))
            return false;
        if (!IsSubmissionPresentationCurrent(generation))
        {
            RetireStaleSubmissionPresentation(generation);
            return false;
        }
        StopWaitingDotsAnimation(generation);
        _isSubmitting = false;
        _dataSource.SetBusy(false);
        ConversationHelper.EndStreaming();
        _submitPresentationScope = null;
        return true;
    }
}
