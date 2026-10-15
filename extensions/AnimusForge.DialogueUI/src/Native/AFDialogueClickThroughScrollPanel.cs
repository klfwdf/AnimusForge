using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI.Native;

public class AFDialogueClickThroughScrollPanel : ScrollablePanel
{
    private int _observedTextRevision = ConversationHelper.StreamingTextRevision;
    private int _remainingFollowFrames;

    // Only the reply body opts in. Answer lists keep native manual scrolling.
    public bool FollowStreamingText { get; set; }

    public AFDialogueClickThroughScrollPanel(UIContext context) : base(context)
    {
        // Vanilla 0.2 adds enough wheel inertia to skip short dialogue viewports.
        MouseScrollSpeed = 0.04f;
    }

    protected override void OnLateUpdate(float dt)
    {
        base.OnLateUpdate(dt);
        if (!FollowStreamingText || !ConversationHelper.IsStreamingTextRevealEnabled)
        {
            _remainingFollowFrames = 0;
            _observedTextRevision = ConversationHelper.StreamingTextRevision;
            return;
        }
        int revision = ConversationHelper.StreamingTextRevision;
        if (_observedTextRevision != revision)
        {
            _observedTextRevision = revision;
            _remainingFollowFrames = 3;
        }
        if (_remainingFollowFrames <= 0 || VerticalScrollbar == null) return;
        _remainingFollowFrames--;
        // The base late update has measured the wrapped content. A short tail
        // covers delayed layout, then releases the bar for manual reading.
        VerticalScrollbar.ValueFloat = VerticalScrollbar.MaxValue;
    }

    // Keep native wheel/controller scrolling; blank presses belong to ContinueButton.
    protected override bool OnPreviewMousePressed() => false;
    protected override bool OnPreviewMouseReleased() => false;
    protected override bool OnPreviewMouseAlternatePressed() => false;
    protected override bool OnPreviewMouseAlternateReleased() => false;
}
