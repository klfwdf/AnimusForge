using AnimusForge;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Scene;
using System.Xml.Linq;

internal static class RewriteUiReplay
{
    internal static void Run(string root, Action<bool, string> check)
    {
        ShoutBehavior.NativeRewriteCalls = ShoutBehavior.SceneRewriteCalls = ShoutBehavior.SceneSubmitCalls = 0;
        ShoutBehavior.RewriteSucceeds = true;
        ShoutBehavior.CanRewriteNativeConversationForExternal = true;
        int submitted = 0;
        var host = new AnimusForgeNativeConversationOverlayVM(_ => submitted++, null, null, null, null, null);
        host.SetInputVisible(true);
        var native = new NativeOverlayVM(host);
        check(native.CanRewrite, "native latest completed round is available");
        native.InputText = "new local draft";
        native.ExecuteRewrite();
        check(ShoutBehavior.NativeRewriteCalls == 0 && native.InputText == "new local draft", "replacement native draft cannot be silently overwritten");
        native.InputText = "";
        host.SetBusy(true); native.ExecuteRewrite();
        check(ShoutBehavior.NativeRewriteCalls == 0 && !native.CanRewrite, "native busy round cannot be rewritten");
        host.SetBusy(false);
        native.ShowLogView(); native.ExecuteRewrite();
        check(ShoutBehavior.NativeRewriteCalls == 0, "native auxiliary interaction cannot rewrite underlying conversation");
        native.Auxiliary.IsOpen = false; native.AuxiliaryStateChanged();
        int focus = host.InputFocusVersion;
        native.ExecuteRewrite();
        check(ShoutBehavior.NativeRewriteCalls == 1 && native.InputText == "old player line" && host.InputFocusVersion > focus,
            "native rewrite restores the exact player line through host notifications and requests focus");
        check(submitted == 0 && !native.CanRewrite, "native rewrite never resubmits or repeats the consumed round");
        native.OnFinalize(); native.ExecuteRewrite();
        check(ShoutBehavior.NativeRewriteCalls == 1, "disposed native wrapper rejects late rewrite click");

        var scene = new SceneSessionVM();
        ShoutBehavior.CanRewriteSceneConversationForExternal = true;
        ShoutBehavior.SceneHistory = new() {
            new() { Text = "earlier line", Kind = "npc" }, new() { Text = "old player line", Kind = "player" },
            new() { Text = "old npc line", Kind = "npc" }, new() { Text = "completed gift fact", Kind = "fact" } };
        scene.Refresh(forceHistory: true);
        scene.SetBusy(true, false); scene.ExecuteRewrite(); scene.TakePending()?.Invoke();
        check(!scene.CanRewrite && ShoutBehavior.SceneRewriteCalls == 0, "scene busy completion disables rewrite");
        scene.SetBusy(false, false);
        scene.InputText = "new draft"; scene.ExecuteRewrite(); scene.TakePending()?.Invoke();
        check(ShoutBehavior.SceneRewriteCalls == 0 && scene.InputText == "new draft", "scene existing draft survives disabled rewrite");
        scene.InputText = "";
        scene.ExecuteRewrite(); scene.InputText = "draft entered after click"; scene.TakePending()?.Invoke();
        check(ShoutBehavior.SceneRewriteCalls == 0 && scene.InputText == "draft entered after click", "queued scene rewrite rechecks draft when executed");
        scene.InputText = "";
        int scroll = scene.HistoryScrollVersion;
        scene.ExecuteRewrite();
        check(ShoutBehavior.SceneRewriteCalls == 0, "scene click defers runtime mutation to the layer command queue");
        scene.TakePending()?.Invoke();
        check(ShoutBehavior.SceneRewriteCalls == 1 && scene.InputText == "old player line" && scene.InputFocusVersion == 1,
            "scene rewrite restores the player line and input focus");
        check(ShoutBehavior.SceneSubmitCalls == 0 && scene.HistoryScrollVersion > scroll
            && scene.HistoryLines.Select(x => x.Text).SequenceEqual(new[] { "earlier line", "completed gift fact" }),
            "scene visible history refresh keeps earlier words and completed facts without auto submit");
        scene.ExecuteRewrite(); scene.TakePending()?.Invoke();
        check(ShoutBehavior.SceneRewriteCalls == 1, "scene consumed round cannot be rewritten twice");
        scene.InputText = ""; ShoutBehavior.CanRewriteSceneConversationForExternal = true;
        ShoutBehavior.RewriteSucceeds = false; ShoutBehavior.RewriteStatus = "records kept";
        scene.SetBusy(false, false); scene.ExecuteRewrite(); scene.TakePending()?.Invoke();
        check(scene.InputText == "" && scene.StatusText == "records kept" && scene.HistoryLines.Count == 2, "scene rewrite failure shows status and leaves draft/history intact");
        ShoutBehavior.HasScenePresentationStagedTradeForExternal = true; scene.Refresh();
        check(!scene.CanRewrite, "scene staged gift protects the pending input context");
        ShoutBehavior.HasScenePresentationStagedTradeForExternal = false;
        scene.Refresh(); scene.ExecuteRewrite(); scene.OnFinalize(); scene.TakePending()?.Invoke();
        check(ShoutBehavior.SceneRewriteCalls == 2, "scene close discards late queued rewrite");

        ShoutBehavior.RewriteSucceeds = true;
        ShoutBehavior.RewriteStatus = "round removed";
        bool pending = false;
        bool illustrationQueued = false;
        var popup = new ShoutTextInputPopupVM("", "", "", "", _ => submitted++, null, allowIllustration: true, onIllustration: () => illustrationQueued = true,
            allowRewrite: true, onRewrite: () => pending = true);
        int calls = ShoutBehavior.SceneRewriteCalls;
        popup.RefreshRewriteAvailability(); popup.ExecuteRewrite();
        check(pending && popup.InputText == "" && ShoutBehavior.SceneRewriteCalls == calls, "fallback popup queues rewrite rather than closing or submitting");
        popup.InputText = "draft entered after click"; popup.RewriteLastRound();
        check(popup.InputText == "draft entered after click" && ShoutBehavior.SceneRewriteCalls == calls, "fallback deferred rewrite rechecks draft");
        popup.InputText = ""; popup.RewriteLastRound();
        check(popup.InputText == "old player line" && popup.InputFocusVersion == 1 && popup.RewriteVersion == 1 && submitted == 0,
            "fallback popup keeps the current input open, restores text and requests focus without submitting");
        ShoutBehavior.SceneIllustrationAvailable = true;
        popup.ExecuteIllustrate();
        check(illustrationQueued && popup.StatusText == popup.IllustrationStatusText, "new illustration request clears rewrite status so capture progress stays visible");
        ShoutBehavior.SceneIllustrationAvailable = false;
        var tagPopup = new ShoutTextInputPopupVM("", "", "", "", null, null);
        ShoutBehavior.CanRewriteSceneConversationForExternal = true; tagPopup.RefreshRewriteAvailability();
        check(!tagPopup.IsRewriteVisible && !tagPopup.CanRewrite, "unrelated tag/battle popup does not acquire a rewrite entry");

        string[] paths = {
            "content/modules/AF.Module.Conversation/GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml",
            "content/modules/AF.Module.Conversation/GUI/Prefabs/ShoutTextInputPopup.xml",
            "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml",
            "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueShout.xml",
            "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneSessionScroll.xml",
            "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneSessionFolio.xml" };
        foreach (string path in paths)
        {
            var xml = XDocument.Load(Path.Combine(root, path));
            var button = xml.Descendants("AnimusForgeConversationRewriteButtonWidget").Single(x => (string)x.Attribute("Command.Click") == "ExecuteRewrite");
            check((string)button.Attribute("IsEnabled") == "@CanRewrite" && (string)button.Attribute("DoNotPassEventsToChildren") == "true"
                && !button.Descendants("TextWidget").Any()
                && (string)button.Attribute("SuggestedWidth") == "32" && (string)button.Attribute("SuggestedHeight") == "32"
                && (string)button.Attribute("HorizontalAlignment") == "Right" && (string)button.Attribute("VerticalAlignment") == "Top", "real rewrite icon, availability and input corner routing: " + path);
            if (!path.Contains("Native")) check(xml.Descendants().Any(x => x.Attribute("FocusRequestId")?.Value == "@InputFocusVersion"), "real scene rewrite focus binding: " + path);
        }
        var english = XDocument.Load(Path.Combine(root, "content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml"));
        var chinese = XDocument.Load(Path.Combine(root, "content/modules/AF.Module.Conversation/ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml"));
        var en = english.Descendants("string").Where(x => x.Attribute("id")?.Value.StartsWith("AF_CONV_UI_Rewrite_") == true).ToDictionary(x => x.Attribute("id")!.Value, x => x.Attribute("text")!.Value);
        var cn = chinese.Descendants("string").Where(x => x.Attribute("id")?.Value.StartsWith("AF_CONV_UI_Rewrite_") == true).ToDictionary(x => x.Attribute("id")!.Value, x => x.Attribute("text")!.Value);
        check(en.Count == 7 && en.Keys.OrderBy(x => x).SequenceEqual(cn.Keys.OrderBy(x => x)) && en["AF_CONV_UI_Rewrite_Button"] == "Undo and edit" && cn["AF_CONV_UI_Rewrite_Button"] == "撤回重写",
            "both UI languages register every new rewrite key and player-facing label");
    }
}
