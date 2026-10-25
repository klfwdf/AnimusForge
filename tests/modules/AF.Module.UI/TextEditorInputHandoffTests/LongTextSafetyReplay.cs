using System;
using System.Linq;
using AnimusForge;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

static partial class Program
{
    static void RunLongTextSafetyCases()
    {
        const int limit = AnimusForgeTextInputSanitizer.MaxLongEditorChars;
        string maximum = new string('规', limit);
        foreach (var spec in new[] {
            ("", "", false), ("规则：保持事实。", "规则：保持事实。", false),
            (maximum, maximum, false), (maximum + "末", maximum, true),
            (new string('规', 1000000), maximum, true),
            ("甲\r\n乙\t丙\0\u200B丁\uD800末", "甲\n乙\t丙丁末", false),
            ("{\"rule\":\"{PLAYER.NAME}\",\"next\":\"[END]\"}", "{\"rule\":\"{PLAYER.NAME}\",\"next\":\"[END]\"}", false),
            (new string('\n', limit), new string('\n', limit), false)
        })
        {
            var h = Host("MCM", 4000); int saves = 0; string result = null;
            DevTextEditorHelper.ShowLongTextEditor("title", "", "hint", spec.Item1, text => { saves++; result = text; }, null);
            var vm = Vm(Editor(h.Screen));
            C(vm.EditedText == spec.Item2, "initial input sanitation matches established output");
            C(vm.LengthStatusText.Contains("truncated") == spec.Item3, "initial truncation is visible, including helper input");
            C(vm.LengthStatusText.Contains(spec.Item2.Length + " / " + limit), "length status uses retained text");
            vm.ExecuteSave(); vm.ExecuteSave();
            C(saves == 1 && result == spec.Item2 && !DevHistoryEditPopup.IsOpen, "bounded text saved exactly once");
        }
        {
            var h = Host("owned", 4020);
            using (DevTextEditorHelper.ShowOwnedLongTextEditor("title", "", "hint", maximum + "末", _ => { }, null, h.Parent, () => true))
                C(Vm(Editor(h.Screen)).LengthStatusText.Contains("truncated"), "owned editor retains initial truncation signal");
        }
        {
            var h = Host("paste", 4000);
            DevTextEditorHelper.ShowLongTextEditor("title", "", "hint", maximum, _ => { }, null);
            var vm = Vm(Editor(h.Screen));
            var probe = new WidgetInputProbe(maximum, limit, limit) { Truncated = vm.NotifyInputTruncated };
            probe.Paste("tail");
            C(probe.Text == maximum && probe.TruncationEvents == 1, "full field rejects extra text without altering content");
            C(vm.LengthStatusText.Contains("truncated"), "widget signals truncation even if RealText does not change");
            string warning = vm.LengthStatusText; vm.NotifyInputTruncated();
            C(vm.LengthStatusText == warning, "repeat overflow signal is idempotent");
            vm.EditedText = "shortened";
            C(vm.LengthStatusText.Contains("9 / 60000") && vm.LengthStatusText.Contains("truncated"), "warning survives edits to an already clipped draft");
            vm.ExecuteCancel();
        }
        {
            var probe = new WidgetInputProbe(maximum, 10, 15);
            probe.Paste("12345678");
            C(probe.Text == maximum.Substring(0, 10) + "12345" + maximum.Substring(15), "selection replacement respects remaining capacity");
            C(probe.TruncationEvents == 1, "selection capacity overflow is signaled");
            var exact = new WidgetInputProbe(maximum, 10, 15); exact.Paste("12345");
            C(exact.TruncationEvents == 0 && exact.Text == probe.Text, "exact capacity needs no warning");
            var empty = new WidgetInputProbe("", 0, 0); empty.Paste(maximum + "末");
            C(empty.Text == maximum && empty.TruncationEvents == 1, "clipboard sanitizer reports discarded tail before binding");
            var normalized = new WidgetInputProbe("", 0, 0) { MaxLength = 3 }; normalized.Paste("甲\r\n乙");
            C(normalized.Text == "甲\n乙" && normalized.TruncationEvents == 0, "line ending normalization alone is not overflow");
            normalized = new WidgetInputProbe("", 0, 0) { MaxLength = 1 }; normalized.Paste("e\u0301");
            C(normalized.Text == "é" && normalized.TruncationEvents == 0, "NFC reduction alone is not overflow");
        }
        foreach (bool owned in new[] { false, true })
        {
            var h = Host("failure", 4000); int saves = 0; bool restoredBeforeCallback = false;
            InformationManager.Messages.Clear(); Logger.Messages.Clear();
            Action<string> save = text => { saves++; restoredBeforeCallback = h.Parent.IsActive; throw new InvalidOperationException("PRIVATE_DRAFT_MARKER"); };
            IDisposable session = null;
            if (owned) session = DevTextEditorHelper.ShowOwnedLongTextEditor("title", "", "hint", "draft", save, null, h.Parent, () => true);
            else DevHistoryEditPopup.Show("direct", "", "draft", "draft", save, null);
            var vm = Vm(Editor(h.Screen)); vm.ExecuteSave(); vm.ExecuteSave(); session?.Dispose();
            C(saves == 1 && restoredBeforeCallback, "failed save is not retried and still restores parent before callback");
            C(!DevHistoryEditPopup.IsOpen && ScreenManager.FocusedLayer == h.Parent && ScreenManager.PushSubscribers == 0, "failed save releases popup and subscriptions");
            C(InformationManager.Messages.Count == 1 && InformationManager.Messages[0].Contains("Could not complete saving"), "failure shown instead of success");
            C(Logger.Messages.Count == 1 && Logger.Messages[0].Contains("InvalidOperationException") && !Logger.Messages[0].Contains("PRIVATE_DRAFT_MARKER"), "failure diagnostic excludes draft-bearing exception message");
            DevTextEditorHelper.ShowLongTextEditor("reopen", "", "hint", "recovered", _ => saves++, null);
            Vm(Editor(h.Screen)).ExecuteSave();
            C(saves == 2 && !DevHistoryEditPopup.IsOpen && ScreenManager.FocusedLayer == h.Parent, "same host can reopen and save after callback failure");
        }
        {
            var h = Host("nested", 4000); int saves = 0; bool restored = false;
            DevTextEditorHelper.ShowLongTextEditor("first", "", "hint", "first", _ => {
                saves++; restored = h.Parent.IsActive;
                DevTextEditorHelper.ShowLongTextEditor("second", "", "hint", "second", null, null);
            }, null);
            var old = Vm(Editor(h.Screen)); old.ExecuteSave(); old.ExecuteSave();
            C(restored && saves == 1 && DevHistoryEditPopup.IsOpen && !h.Parent.IsActive, "save callback can open successor without old close undoing focus");
            var next = Editor(h.Screen);
            C(Vm(next).EditedText == "second" && ScreenManager.FocusedLayer == next, "successor owns input");
            Vm(next).ExecuteCancel(); C(h.Parent.IsActive && ScreenManager.FocusedLayer == h.Parent, "successor cancel restores original host");
        }
        {
            var h = Host("native fallback", 4000); InformationManager.Messages.Clear(); int saves = 0;
            GauntletLayer.FailLoad = true;
            DevTextEditorHelper.ShowLongTextEditor("title", "", "hint", maximum + "末", _ => { saves++; throw new InvalidOperationException("failure"); }, null);
            GauntletLayer.FailLoad = false;
            C(!DevHistoryEditPopup.IsOpen && h.Parent.IsActive && InformationManager.Messages.Single().Contains("truncated"), "fallback initial clipping is reported after failed custom open");
            InformationManager.Inquiry.Save("draft");
            C(saves == 1 && InformationManager.Messages.Last().Contains("Could not complete saving"), "fallback save callback has the same failure boundary");
            InformationManager.Messages.Clear(); string saved = null;
            DevTextEditorHelper.InvokeSaveSafely(text => saved = text, maximum + "末");
            C(saved == maximum && InformationManager.Messages.Single().Contains("truncated"), "fallback save-time truncation is reported");
        }
        {
            Host("failed diagnostics", 4000); int saves = 0;
            DevTextEditorHelper.ShowLongTextEditor("title", "", "hint", "draft", _ => { saves++; throw new InvalidOperationException(); }, null);
            var vm = Vm(Editor(ScreenManager.TopScreen)); Logger.FailLog = InformationManager.FailNotify = true;
            try { vm.ExecuteSave(); vm.ExecuteSave(); } finally { Logger.FailLog = InformationManager.FailNotify = false; }
            C(saves == 1 && !DevHistoryEditPopup.IsOpen, "diagnostic or notification failure does not escape or retry save");
        }
    }
}
