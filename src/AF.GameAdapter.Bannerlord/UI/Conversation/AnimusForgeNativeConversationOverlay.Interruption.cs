using System;
using System.Collections.Generic;

namespace AnimusForge;

public sealed partial class AnimusForgeNativeConversationOverlay
{
    private readonly Queue<Action> _deferredVisibleUiActions = new Queue<Action>();
    private int _displayTextGeneration = -1;
    private string _submissionDisplayText;

    // ApplicationTick still runs with a native pause/options layer. Drain the same
    // bounded UI queue without waiting for mission simulation or stealing its input.
    private void ProcessInterruptedPresentation()
    {
        ProcessMainThreadActions();
        ValidatePendingSubmissionPresentation();
    }

    private void SetSubmissionDisplayText(int generation, string text)
    {
        if (!IsSubmitGenerationCurrent(generation)) return;
        _displayTextGeneration = generation;
        _submissionDisplayText = text ?? string.Empty;
        ConversationHelper.UpdateDialogText(_submissionDisplayText);
    }

    // Only visible notifications/focus/retry dialogs wait for resume. State completion
    // and scoped reply text are allowed on the game thread while the overlay is hidden.
    private void RunVisibleUiAction(int generation, Action action)
    {
        if (!IsSubmitGenerationCurrent(generation) || action == null) return;
        if (!_temporarySystemUiActive)
        {
            action();
            return;
        }
        _deferredVisibleUiActions.Enqueue(() =>
        {
            if (IsSubmitGenerationCurrent(generation)
                && _modeTextScope?.HasCurrentConversationContext() == true)
                action();
        });
    }

    private void RestoreInterruptedPresentation()
    {
        if (_isClosed || _temporarySystemUiActive) return;
        // Native pause/options may refresh/rebind the dialogue VM after EndStreaming.
        // Replay one cached UI-only text, never an action or memory commit. A new NPC,
        // native token, save, mode or request generation makes this text ineligible.
        ReapplyInterruptedDisplayText();
        int processed = 0;
        while (processed++ < 128 && _deferredVisibleUiActions.Count > 0)
        {
            Action action = _deferredVisibleUiActions.Dequeue();
            try { action(); }
            catch (Exception ex) { Logger.Log("NativeConversationOverlay", "[WARN] deferred visible UI action failed: " + ex.Message); }
        }
    }

    // Reuse the existing eight-frame restore window: a native VM may rebind one
    // frame AFTER the pause closes. Equal text is O(1) for the same string reference;
    // only an actual overwrite triggers the cold NPC/context check and one repaint.
    private void ReapplyInterruptedDisplayText()
    {
        if (_isClosed || _temporarySystemUiActive || !_dataSource.IsCustomAnswerVisible
            || _displayTextGeneration != _submitGeneration || _submissionDisplayText == null
            || !ConversationHelper.HasActiveVM || ConversationHelper.IsTypewriterActive
            || string.Equals(ConversationHelper.GetCurrentDialogText(), _submissionDisplayText, StringComparison.Ordinal))
            return;
        if (_modeTextScope?.HasCurrentConversationContext() == true)
            ConversationHelper.UpdateDialogText(_submissionDisplayText);
    }

    private void ClearInterruptedPresentation()
    {
        _submissionDisplayText = null;
        _displayTextGeneration = -1;
        _deferredVisibleUiActions.Clear();
    }
}
