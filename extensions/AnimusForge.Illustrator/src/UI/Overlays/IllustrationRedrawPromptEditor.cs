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
                return DevTextEditorHelper.ShowOwnedLongTextEditor(basedOnImage ? "重绘（基于本图）" : "重绘（带提示词）", "",
                    basedOnImage ? "描述对当前图片的修改要求；留空不会开始重绘。" : "填写本次重绘的画面要求；留空不会开始生成。", draft ?? "", input =>
                {
                    restore();
                    if (submitted) return;
                    submitted = true;
                    if (!isCurrent()) return;
                    string prompt = (input ?? "").Trim();
                    if (prompt.Length == 0) { status("请填写本次重绘提示词，未开始生成。"); return; }
                    try
                    {
                        if (!basedOnImage) VisualDirectorEngine.RequirePlayerRedrawDirector(prompt, IllustratorRuntime.CaptureOptions());
                        redraw(prompt);
                    }
                    catch (Exception ex) { status("重绘准备失败：" + ex.Message); }
                }, () => { submitted = true; restore(); }, inputOwner, isInputOwnerAlive, "重绘", "取消");
            }
            catch (Exception ex)
            {
                submitted = true;
                restore();
                if (isCurrent()) status("重绘准备失败：" + ex.Message);
                return null;
            }
        }
    }
}
