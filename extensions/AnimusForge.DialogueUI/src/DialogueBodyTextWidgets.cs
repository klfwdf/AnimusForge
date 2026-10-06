using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI;

// XML creates these only for body text in the redesigned prefabs. Capture once
// after XML attributes have been applied (constructor defaults would be replaced
// by Brush.FontSize/EditorFontSize). No per-frame MCM reads or tree traversal.
public class AFDialogueBodyTextWidget : TextWidget
{
    private bool _fontCaptured;
    public AFDialogueBodyTextWidget(UIContext context) : base(context) { }
    protected override void OnUpdate(float dt)
    {
        if (!_fontCaptured) { Brush.FontSize = DialogueUiOptions.BodyFontSize; _fontCaptured = true; }
        base.OnUpdate(dt);
    }
}

public class AFDialogueBodyRichTextWidget : RichTextWidget
{
    private bool _fontCaptured;
    public AFDialogueBodyRichTextWidget(UIContext context) : base(context) { }
    protected override void OnUpdate(float dt)
    {
        if (!_fontCaptured) { Brush.FontSize = DialogueUiOptions.BodyFontSize; _fontCaptured = true; }
        base.OnUpdate(dt);
    }
}

public class AFDialogueBodyEditorWidget : DevMultilineEditableTextWidget
{
    private bool _fontCaptured;
    public AFDialogueBodyEditorWidget(UIContext context) : base(context) { }
    protected override void OnUpdate(float dt)
    {
        if (!_fontCaptured)
        {
            EditorFontSize = DialogueUiOptions.BodyFontSize;
            Brush.FontSize = EditorFontSize;
            _fontCaptured = true;
        }
        base.OnUpdate(dt);
    }
}
