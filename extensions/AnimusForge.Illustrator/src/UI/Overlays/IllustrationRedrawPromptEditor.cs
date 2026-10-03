using System;
using AnimusForge.Illustrator.Core;

namespace AnimusForge.Illustrator.UI.Overlays
{
    internal static class IllustrationRedrawPromptEditor
    {
        internal static void Show(string draft, Func<bool> isCurrent, Action<string> redraw,
            Action<string> status, Action<bool> setEditing)
        {
            if (!isCurrent()) return;
            bool editing = false;
            bool submitted = false;
            Action restore = () => { if (editing) { editing = false; setEditing(false); } };
            try
            {
                VisualDirectorEngine.RequirePlayerRedrawDirector("requested", IllustratorRuntime.CaptureOptions());
                editing = true;
                setEditing(true);
                DevTextEditorHelper.ShowLongTextEditor("重绘（带提示词）", "", "", draft ?? "", input =>
                {
                    restore();
                    if (submitted) return;
                    submitted = true;
                    if (!isCurrent()) return;
                    string prompt = (input ?? "").Trim();
                    if (prompt.Length == 0) { status("请填写本次重绘提示词，未开始生成。"); return; }
                    try
                    {
                        VisualDirectorEngine.RequirePlayerRedrawDirector(prompt, IllustratorRuntime.CaptureOptions());
                        redraw(prompt);
                    }
                    catch (Exception ex) { status("重绘准备失败：" + ex.Message); }
                }, () => { submitted = true; restore(); }, "重绘", "取消");
            }
            catch (Exception ex)
            {
                submitted = true;
                restore();
                if (isCurrent()) status("重绘准备失败：" + ex.Message);
            }
        }
    }
}
