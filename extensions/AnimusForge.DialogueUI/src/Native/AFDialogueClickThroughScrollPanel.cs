using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI.Native;

public class AFDialogueClickThroughScrollPanel : ScrollablePanel
{
    public AFDialogueClickThroughScrollPanel(UIContext context) : base(context)
    {
        // Vanilla 0.2 adds enough wheel inertia to skip short dialogue viewports.
        MouseScrollSpeed = 0.04f;
    }

    // Keep native wheel/controller scrolling; blank presses belong to ContinueButton.
    protected override bool OnPreviewMousePressed() => false;
    protected override bool OnPreviewMouseReleased() => false;
    protected override bool OnPreviewMouseAlternatePressed() => false;
    protected override bool OnPreviewMouseAlternateReleased() => false;
}
