using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI.Native;

public class AFDialogueClickThroughScrollPanel : ScrollablePanel
{
    public AFDialogueClickThroughScrollPanel(UIContext context) : base(context) { }

    // Keep native wheel/controller scrolling; blank presses belong to ContinueButton.
    protected override bool OnPreviewMousePressed() => false;
    protected override bool OnPreviewMouseReleased() => false;
    protected override bool OnPreviewMouseAlternatePressed() => false;
    protected override bool OnPreviewMouseAlternateReleased() => false;
}
