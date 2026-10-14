using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;
using TaleWorlds.MountAndBlade;

static class Replay
{
    static int checks;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    static object Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    static ConversationRewriteTurn Admit(ShoutBehavior host, string input = "original player words")
    {
        var runtime = host._sceneConversationRuntime;
        runtime.Epoch++;
        Invoke(runtime, "BeginSceneRewriteTurn", input, new ScenePlayerShoutRequest
        {
            Mission = Mission.Current, Player = Agent.Main, RuntimeGeneration = 1, SceneSessionId = 4
        }, runtime.Epoch);
        var turn = (ConversationRewriteTurn)Invoke(runtime, "CaptureSceneRewriteTurn", runtime.Epoch);
        var player = new string(input.ToCharArray());
        var reply = new string("npc reply".ToCharArray());
        host.Records.Add(player); host.Records.Add(reply);
        using (ConversationRewriteTurn.CaptureScope(turn))
        {
            ConversationRewriteTurn.CaptureListWrite(() => host.Records, player, isPlayerSpeech: true);
            ConversationRewriteTurn.CaptureListWrite(() => host.Records, reply, isNpcReply: true);
        }
        Invoke(runtime, "CompleteSceneRewriteTurn", turn);
        return turn;
    }
    static ShoutBehavior Host()
    {
        Mission.Current = new Mission(); Agent.Main = new Agent();
        var host = new ShoutBehavior(); ShoutBehavior.CurrentInstance = host;
        return host;
    }
    public static void Main()
    {
        var host = Host();
        host.Records.Add("earlier dialogue"); host.Records.Add("confirmed AFEF fact");
        Admit(host);
        Check(ShoutBehavior.CanRewriteSceneConversationForExternal, "completed-current-round-available");
        host.Processing = true;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal && !ShoutBehavior.TryRewriteSceneConversationForExternal(out _, out _), "generation-busy-cannot-delete");
        host.Processing = false; host.Waiting = true;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "waiting-postprocess-busy");
        host.Waiting = false; host._sceneConversationRuntime.Postprocess = new TaskCompletionSource<bool>().Task;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "unfinished-postprocess-gate-busy");
        host._sceneConversationRuntime.Postprocess = Task.CompletedTask; host.SpeechBusy = true;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "speech-busy-cannot-delete");
        host.SpeechBusy = false; host.RoundBusy = true;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "presentation-round-busy");
        host.RoundBusy = false;
        Check(ShoutBehavior.TryRewriteSceneConversationForExternal(out var input, out _) && input == "original player words", "rewrite-returns-exact-player-input");
        Check(host.Records.Count == 2 && host.Records[0] == "earlier dialogue" && host.Records[1] == "confirmed AFEF fact", "immediate-earlier-memory-and-fact-retained");
        Check(host._sceneConversationRuntime.RetireCount == 1 && !ShoutBehavior.TryRewriteSceneConversationForExternal(out _, out _), "one-use-retire-late-callbacks");
        Admit(host, "new message");
        Check(ShoutBehavior.CanRewriteSceneConversationForExternal, "new-turn-can-rewrite-again");
        host._sceneConversationRuntime.Epoch++;
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal && !ShoutBehavior.TryRewriteSceneConversationForExternal(out _, out _), "changed-epoch-rejects-old-turn");
        host = Host(); Admit(host); Mission.Current = new Mission();
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "changed-mission-rejects-old-turn");
        host = Host(); Admit(host); Agent.Main = new Agent();
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "changed-player-object-rejects-old-turn");
        host = Host(); Admit(host); host._sceneConversationRuntime.InvalidateRewriteTurn();
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "closed-session-rejects-old-turn");
        host = Host(); var old = Admit(host); var current = Admit(host, "second message");
        Invoke(host._sceneConversationRuntime, "CompleteSceneRewriteTurn", old);
        Check(current.IsCompleted && ShoutBehavior.CanRewriteSceneConversationForExternal, "late-old-completion-cannot-replace-latest");
        Check(!Task.Run(() => ShoutBehavior.TryRewriteSceneConversationForExternal(out _, out _)).GetAwaiter().GetResult(), "off-thread-request-rejected");
        Check(host.Records.Count == 4, "off-thread-rejection-keeps-all-records");
        host = Host(); Admit(host);
        Invoke(host._sceneConversationRuntime, "FailSceneRewriteRound", null, host._sceneConversationRuntime.Epoch, "scene.group_exception");
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal && host.Records.Count == 2, "partial-group-failure-cannot-seal-or-delete");
        host = Host(); Admit(host);
        var postprocess = new TaskCompletionSource<ScenePostprocessOutcome>();
        Invoke(host._sceneConversationRuntime, "ObserveSceneRewritePostprocess", host._sceneConversationRuntime.Epoch, postprocess.Task);
        postprocess.SetResult(new ScenePostprocessOutcome { Succeeded = false });
        Check(!ShoutBehavior.CanRewriteSceneConversationForExternal, "late-postprocess-failure-invalidates-sealed-turn");
        host = Host(); Admit(host);
        var oldPostprocess = new TaskCompletionSource<ScenePostprocessOutcome>();
        Invoke(host._sceneConversationRuntime, "ObserveSceneRewritePostprocess", host._sceneConversationRuntime.Epoch, oldPostprocess.Task);
        Admit(host, "latest message");
        oldPostprocess.SetResult(new ScenePostprocessOutcome { Succeeded = false });
        Check(ShoutBehavior.CanRewriteSceneConversationForExternal, "old-postprocess-failure-cannot-invalidate-new-turn");
        Console.WriteLine("SCENE_REWRITE_LIFECYCLE_PASS " + checks);
    }
}

// Engine leaf ports are simulated; the rewrite runtime and public API are linked
// verbatim from production. No network, game installation or real save is used.
namespace TaleWorlds.MountAndBlade
{
    public class Mission { public static Mission Current; public bool MissionEnded; }
    public class Agent { public static Agent Main; }
}
namespace AnimusForge.Refactor.Runtime
{
    internal static class SaveRuntimeGuard { internal static bool IsCurrentGeneration(long value) => value == 1; }
}
namespace AnimusForge
{
    internal static class Logger { internal static void Log(string category, string text) { } }
    internal static class ConversationUiLocalization { internal static string Text(string key, string fallback) => fallback; }
    internal class ScenePlayerShoutRequest { internal Mission Mission; internal Agent Player; internal long RuntimeGeneration; internal int SceneSessionId; }
    internal class SceneGroupReceipt { internal void Fail(string reason) { } }
    internal class ScenePostprocessOutcome { internal bool Succeeded; }
    internal class SceneConversationSessionPorts { internal Func<bool> IsOwnerCurrent; internal Func<int> SceneSessionId; }
    internal sealed partial class SceneConversationSessionRuntime
    {
        internal readonly SceneConversationSessionPorts _ports;
        internal int _sceneConversationEpoch;
        internal int Epoch { get => _sceneConversationEpoch; set => _sceneConversationEpoch = value; }
        internal int RetireCount;
        internal Task Postprocess = Task.CompletedTask;
        internal SceneConversationSessionRuntime(ShoutBehavior owner) { _ports = new SceneConversationSessionPorts { IsOwnerCurrent = () => ReferenceEquals(ShoutBehavior.CurrentInstance, owner), SceneSessionId = () => 4 }; }
        internal Task GetScenePostprocessGateTask() => Postprocess;
        internal int BeginNewPlayerDrivenSceneConversationEpoch() { InvalidateRewriteTurn(); RetireCount++; return ++_sceneConversationEpoch; }
    }
    internal class ScenePresentationStub { internal readonly PresentationStub Presentation = new PresentationStub(); }
    internal class PresentationStub { internal void ClearRound() { } }
    public partial class ShoutBehavior
    {
        internal static ShoutBehavior CurrentInstance;
        internal readonly SceneConversationSessionRuntime _sceneConversationRuntime;
        internal SceneConversationSessionRuntime _sceneConversation => _sceneConversationRuntime;
        internal readonly ScenePresentationStub _j17ScenePresentationBannerlordAdapter = new ScenePresentationStub();
        internal readonly List<string> Records = new List<string>();
        internal bool _isProcessingShout, _isWaitingForScenePostprocessGate, SpeechBusy, RoundBusy;
        internal bool Processing { get => _isProcessingShout; set => _isProcessingShout = value; }
        internal bool Waiting { get => _isWaitingForScenePostprocessGate; set => _isWaitingForScenePostprocessGate = value; }
        static readonly int MainThread = Thread.CurrentThread.ManagedThreadId;
        internal ShoutBehavior() { _sceneConversationRuntime = new SceneConversationSessionRuntime(this); }
        internal static bool IsBannerlordMainThreadForNativeActions() => Thread.CurrentThread.ManagedThreadId == MainThread;
        private bool IsPresentationRoundActive() => RoundBusy;
        private bool IsSpeechPipelineBusy() => SpeechBusy;
        private void ClearPresentationRound() => _j17ScenePresentationBannerlordAdapter.Presentation.ClearRound();
        internal static void BumpPresentation() { }
    }
}
