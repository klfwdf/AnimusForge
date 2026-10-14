using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI;

// One small shared sprite, loaded at construction and re-registered by the existing
// sprite refresh hook. No Tick IO, layout polling or conversation-state mutation.
public sealed class AnimusForgeConversationRewriteButtonWidget : ButtonWidget
{
    private bool _hintShown;

    public AnimusForgeConversationRewriteButtonWidget(UIContext context) : base(context)
    {
        DoNotPassEventsToChildren = true;
        UpdateChildrenStates = true;
        if (DialogueUiSprites.EnsureRewriteIconLoaded())
            DialogueUiButtons.StyleSprite(this, "afdui_icon_rewrite");
    }

    protected override void OnHoverBegin()
    {
        base.OnHoverBegin();
        MBInformationManager.ShowHint(ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Hint",
            "Undo this dialogue turn and edit your original line. Completed game actions are retained."));
        _hintShown = true;
    }

    protected override void OnHoverEnd()
    {
        HideHint();
        base.OnHoverEnd();
    }

    protected override void OnDisconnectedFromRoot()
    {
        HideHint();
        base.OnDisconnectedFromRoot();
    }

    private void HideHint()
    {
        if (!_hintShown) return;
        _hintShown = false;
        MBInformationManager.HideInformations();
    }
}
