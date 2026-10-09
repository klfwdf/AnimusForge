using System;
using AnimusForge.Illustrator.Core;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.UI.Overlays
{
    internal static class IllustrationRedrawPromptEditor
    {
        internal static IDisposable Show(string draft, Func<bool> isCurrent, Action<string> redraw,
            Action<string> status, Action<bool> setEditing, bool basedOnImage = false,
            ScreenLayer inputOwner = null, Func<bool> isInputOwnerAlive = null)
        {
            if (!isCurrent()) return null;
            bool editing = false;
            bool submitted = false;
            Action restore = () => { if (editing) { editing = false; setEditing(false); } };
            try
            {
                if (!basedOnImage) VisualDirectorEngine.RequirePlayerRedrawDirector("requested", IllustratorRuntime.CaptureOptions());
                editing = true;
                setEditing(true);
                return DevTextEditorHelper.ShowOwnedLongTextEditor(IllustratorUiText.RedrawPromptTitle(basedOnImage), "",
                    IllustratorUiText.RedrawPromptHint(basedOnImage), draft ?? "", input =>
                {
                    restore();
                    if (submitted) return;
                    submitted = true;
                    if (!isCurrent()) return;
                    string prompt = (input ?? "").Trim();
                    if (prompt.Length == 0) { status(IllustratorUiText.RedrawPromptRequired); return; }
                    try
                    {
                        if (!basedOnImage) VisualDirectorEngine.RequirePlayerRedrawDirector(prompt, IllustratorRuntime.CaptureOptions());
                        redraw(prompt);
                    }
                    catch (Exception ex) { status(IllustratorUiText.CardRedrawFailed(ex.Message)); }
                }, () => { submitted = true; restore(); }, inputOwner, isInputOwnerAlive, IllustratorUiText.Redraw, IllustratorUiText.Cancel);
            }
            catch (Exception ex)
            {
                submitted = true;
                restore();
                if (isCurrent()) status(IllustratorUiText.CardRedrawFailed(ex.Message));
                return null;
            }
        }
    }
}
