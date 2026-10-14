using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal static class Program
{
    private static int _checks;
    private static void Check(bool result, string name)
    {
        _checks++;
        if (!result) throw new InvalidOperationException(name);
    }

    private static (ShoutBehavior Owner, ConversationRewriteTurn Turn, List<object> Rows) Completed(string input = "original player line")
    {
        var owner = new ShoutBehavior();
        ShoutBehavior.CurrentInstance = owner;
        var rows = new List<object> { new object(), new object() }; // earlier speech and game fact
        var turn = owner.BeginTest(input, "previous visible reply");
        using (ConversationRewriteTurn.CaptureScope(turn))
        {
            object player = new object(), npc = new object();
            rows.Add(player);
            ConversationRewriteTurn.CaptureListWrite(() => rows, player, isPlayerSpeech: true);
            rows.Add(npc);
            ConversationRewriteTurn.CaptureListWrite(() => rows, npc, isNpcReply: true);
        }
        turn.Complete();
        return (owner, turn, rows);
    }

    private static void Main()
    {
        var initial = Completed();
        Check(ShoutBehavior.CanRewriteNativeConversationForExternal, "completed current turn available");
        Check(initial.Owner.SceneInvalidations == 1, "native start retires scene receipt");
        var pending = initial.Owner.BeginTest("pending", "earlier");
        Check(!initial.Turn.IsCompleted && !ShoutBehavior.CanRewriteNativeConversationForExternal, "new actual input invalidates old receipt");
        Check(initial.Rows.Count == 4, "retiring receipt retains all committed history");
        pending.Invalidate("test_failure");
        Check(!ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out _), "failure cannot rewrite");

        foreach (string busy in new[] { "backend", "scene-speech", "native-speech" })
        {
            var current = Completed();
            current.Owner.SetBusy(busy);
            Check(!ShoutBehavior.CanRewriteNativeConversationForExternal, busy + " availability");
            Check(!ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out string status) && status.Contains("Busy"), busy + " click rejects");
            Check(current.Rows.Count == 4 && current.Turn.IsCompleted, busy + " rejects without mutating receipt");
        }

        foreach (string changed in new[] { "presentation", "stamp", "target" })
        {
            var current = Completed();
            current.Owner.ChangeContext(changed);
            if (changed != "target") Check(!ShoutBehavior.CanRewriteNativeConversationForExternal, changed + " cheap stamp rejects");
            else Check(ShoutBehavior.CanRewriteNativeConversationForExternal, "UI availability does not scan target");
            Check(!ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out string status) && status.Contains("Changed"), changed + " final click guard");
            Check(current.Rows.Count == 4, changed + " history retained");
        }

        var edited = Completed();
        edited.Rows.RemoveAt(2);
        Check(!ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out string editedStatus) && editedStatus.Contains("Failed"), "edited source rejects receipt");
        Check(edited.Rows.Count == 3, "source mismatch does not delete remaining reply");

        var pendingUi = Completed();
        var waitingOverlay = AnimusForgeNativeConversationOverlay.OpenForTest();
        waitingOverlay.SetSubmitting(true);
        Check(!ShoutBehavior.CanRewriteNativeConversationForExternal
            && !ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out string waitingStatus)
            && waitingStatus.Contains("Busy"), "queued UI completion blocks rewrite after backend finished");
        Check(pendingUi.Rows.Count == 4, "pending presentation leaves history intact");
        waitingOverlay.SetSubmitting(false);

        var success = Completed("same text can be resent");
        var overlay = AnimusForgeNativeConversationOverlay.OpenForTest();
        int uiGeneration = overlay.Generation;
        Check(ShoutBehavior.TryRewriteNativeConversationForExternal(out string playerText, out string done), "current sealed turn rewritten");
        Check(playerText == "same text can be resent" && done.Contains("Done"), "original input returned");
        Check(success.Rows.Count == 2, "only current two speech rows removed");
        Check(overlay.Display == "previous visible reply", "visible reply restored");
        Check(overlay.Generation == uiGeneration + 1 && overlay.Cleared == 2 && overlay.Refreshes == 1, "queued old UI callbacks retired");
        Check(!ShoutBehavior.CanRewriteNativeConversationForExternal && !ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out _), "one shot receipt");

        var failedUi = Completed("input survives UI repaint failure");
        AnimusForgeNativeConversationOverlay.OpenForTest().ThrowOnPaint = true;
        Check(ShoutBehavior.TryRewriteNativeConversationForExternal(out string recoveredInput, out _)
            && recoveredInput == "input survives UI repaint failure" && failedUi.Rows.Count == 2,
            "UI repaint failure preserves completed receipt result and input");

        var offThread = Completed();
        bool workerRejected = Task.Run(() =>
        {
            ShoutBehavior.IsMain = false;
            return !ShoutBehavior.CanRewriteNativeConversationForExternal && !ShoutBehavior.TryRewriteNativeConversationForExternal(out _, out _);
        }).GetAwaiter().GetResult();
        ShoutBehavior.IsMain = true;
        Check(workerRejected && offThread.Rows.Count == 4, "background caller cannot mutate game-owned history");
        ShoutBehavior.CurrentInstance = null;
        Check(!ShoutBehavior.CanRewriteNativeConversationForExternal, "retired owner unavailable");
        Console.WriteLine("NATIVE_REWRITE_INTEGRATION_PASS checks=" + _checks);
    }
}

// Only unavailable Bannerlord identity, audio, localization and UI leaves are faked.
// The native rewrite adapter, exact receipt and repaint partial are linked unchanged.
public partial class ShoutBehavior
{
    internal static ShoutBehavior CurrentInstance;
    internal static bool IsMain = true;
    internal static bool IsBannerlordMainThreadForNativeActions() => IsMain;
    private readonly FakeScene _sceneConversationRuntime = new();
    private readonly FakeAdmissions NativeAdmissions = new();
    private readonly FakeAdmissionOwner _nativeAdmissionOwner = new();
    private static readonly FakePlayback _j17NativeConversationPlaybackWaitAdapter = new();
    private bool _speechBusy, _stamp = true, _target = true;
    internal sealed class NativeConversationAdmission { internal long PresentationRevision = 1; }
    internal ShoutBehavior() { _j17NativeConversationPlaybackWaitAdapter.HasPendingPlayback = false; }
    private bool IsNativeConversationContextStampCurrent(NativeConversationAdmission a) => _stamp;
    private bool IsNativeConversationContextCurrent(NativeConversationAdmission a, out string reason) { reason = ""; return _stamp && _target; }
    private bool IsSpeechPipelineBusy() => _speechBusy;
    internal ConversationRewriteTurn BeginTest(string input, string display) => BeginNativeRewriteTurn(new NativeConversationAdmission(), input, display);
    internal int SceneInvalidations => _sceneConversationRuntime.Invalidations;
    internal void SetBusy(string type)
    {
        if (type == "backend") NativeAdmissions.Busy = true;
        if (type == "scene-speech") _speechBusy = true;
        if (type == "native-speech") _j17NativeConversationPlaybackWaitAdapter.HasPendingPlayback = true;
    }
    internal void ChangeContext(string type)
    {
        if (type == "presentation") _nativeAdmissionOwner.Revision = 2;
        if (type == "stamp") _stamp = false;
        if (type == "target") _target = false;
    }
}
internal sealed class FakeScene { internal int Invalidations; internal void InvalidateRewriteTurn() => Invalidations++; }
internal sealed class FakeAdmissions { internal bool Busy; internal bool IsBusyForUi() => Busy; }
internal sealed class FakeAdmissionOwner { internal long Revision = 1; internal bool IsPresentationCurrent(long revision) => Revision == revision; }
internal sealed class FakePlayback { internal bool HasPendingPlayback; }
internal static class ConversationUiLocalization { internal static string Text(string key, string fallback) => key; }
internal static class Logger { internal static void Log(string category, string line) { } }

public sealed partial class AnimusForgeNativeConversationOverlay
{
    private static AnimusForgeNativeConversationOverlay _activeOverlay;
    private bool _isClosed, _isSubmitting;
    private int _submitGeneration = 7;
    private object _submitPresentationScope;
    private readonly FakeUi _dataSource = new();
    internal int Generation => _submitGeneration;
    internal string Display;
    internal int Cleared;
    internal bool ThrowOnPaint;
    internal int Refreshes => _dataSource.Refreshes;
    internal static AnimusForgeNativeConversationOverlay OpenForTest() => _activeOverlay = new();
    internal void SetSubmitting(bool value) => _isSubmitting = value;
    private void ClearInterruptedPresentation() => Cleared++;
    private void ClearPendingPostprocessNotice() => Cleared++;
    private void SetSubmissionDisplayText(int generation, string text)
    {
        if (ThrowOnPaint) throw new InvalidOperationException("fake UI failure");
        Display = text;
    }
}
internal sealed class FakeUi { internal int Refreshes; internal void RefreshRewriteAvailability() => Refreshes++; }
