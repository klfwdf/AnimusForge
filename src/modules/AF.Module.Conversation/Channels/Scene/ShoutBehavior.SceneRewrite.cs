using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
    // Only the latest player round is retained. These opaque references are compared
    // on the mission thread; network workers never inspect the game objects.
    private ConversationRewriteTurn _sceneRewriteTurn;
    private Mission _sceneRewriteMission;
    private Agent _sceneRewritePlayer;
    private long _sceneRewriteGeneration;
    private int _sceneRewriteSession;
    private int _sceneRewriteEpoch;

    internal void InvalidateRewriteTurn()
    {
        _sceneRewriteTurn?.Invalidate();
        _sceneRewriteTurn = null;
        _sceneRewriteMission = null;
        _sceneRewritePlayer = null;
    }

    private void BeginSceneRewriteTurn(string playerText, ScenePlayerShoutRequest request, int epoch)
    {
        InvalidateRewriteTurn();
        _sceneRewriteMission = request.Mission;
        _sceneRewritePlayer = request.Player;
        _sceneRewriteGeneration = request.RuntimeGeneration;
        _sceneRewriteSession = request.SceneSessionId;
        _sceneRewriteEpoch = epoch;
        Volatile.Write(ref _sceneRewriteTurn, new ConversationRewriteTurn(playerText));
    }

    private ConversationRewriteTurn CaptureSceneRewriteTurn(int epoch)
    {
        ConversationRewriteTurn turn = Volatile.Read(ref _sceneRewriteTurn);
        return epoch == _sceneRewriteEpoch ? turn : null;
    }

    private void FailSceneRewriteRound(SceneGroupReceipt receipt, int epoch, string reason)
    {
        receipt?.Fail(reason);
        CaptureSceneRewriteTurn(epoch)?.Invalidate(reason);
    }

    private void ObserveSceneRewritePostprocess(int epoch, Task<ScenePostprocessOutcome> task)
    {
        ConversationRewriteTurn turn = CaptureSceneRewriteTurn(epoch);
        if (turn == null || task == null) return;
        // Only detached result/receipt flags cross threads. A failed action phase
        // cannot turn an incomplete group into an apparently completed undo target.
        task.ContinueWith(completed =>
        {
            if (completed.Status != TaskStatus.RanToCompletion)
            {
                _ = completed.Exception;
                turn.Invalidate("scene_postprocess_not_completed");
            }
            else if (completed.Result?.Succeeded != true)
                turn.Invalidate("scene_postprocess_unconfirmed");
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void CompleteSceneRewriteTurn(ConversationRewriteTurn expected)
    {
        if (expected == null || !ReferenceEquals(expected, _sceneRewriteTurn)) return;
        if (IsRewriteSourceCurrent()) expected.Complete();
        else InvalidateRewriteTurn();
    }

    private bool IsRewriteSourceCurrent()
    {
        return _sceneRewriteTurn != null && _ports.IsOwnerCurrent()
            && SaveRuntimeGuard.IsCurrentGeneration(_sceneRewriteGeneration)
            && _sceneRewriteSession == _ports.SceneSessionId()
            && _sceneRewriteEpoch == VolatileEpoch()
            && _sceneRewriteMission != null && ReferenceEquals(_sceneRewriteMission, Mission.Current)
            && !_sceneRewriteMission.MissionEnded
            && _sceneRewritePlayer != null && ReferenceEquals(_sceneRewritePlayer, Agent.Main);
    }

    private int VolatileEpoch() => System.Threading.Volatile.Read(ref _sceneConversationEpoch);

    // UI polling performs identity/task-state checks only. Exact memory validation
    // belongs to TryRewrite, once per click, rather than every UI refresh.
    internal bool HasCompletedRewriteTurn => _sceneRewriteTurn?.IsCompleted == true && IsRewriteSourceCurrent();

    internal bool TryRewriteLatestSceneTurn(out string playerText, out string reason)
    {
        playerText = string.Empty;
        reason = "rewrite_unavailable";
        if (!HasCompletedRewriteTurn) return false;
        ConversationRewriteTurn turn = _sceneRewriteTurn;
        if (!turn.TryRewrite(out reason)) return false;
        playerText = turn.PlayerText;
        // Retire old reply/TTS callbacks without clearing surviving conversation history.
        BeginNewPlayerDrivenSceneConversationEpoch();
        InvalidateRewriteTurn();
        return true;
    }
}

public partial class ShoutBehavior
{
    public static bool CanRewriteSceneConversationForExternal
    {
        get
        {
            ShoutBehavior owner = CurrentInstance;
            return owner != null && IsBannerlordMainThreadForNativeActions()
                && owner._sceneConversationRuntime?.HasCompletedRewriteTurn == true
                && !owner.IsSceneRewriteBusy();
        }
    }

    private bool IsSceneRewriteBusy()
    {
        return _isProcessingShout || _isWaitingForScenePostprocessGate
            || !_sceneConversation.GetScenePostprocessGateTask().IsCompleted
            || IsPresentationRoundActive() || IsSpeechPipelineBusy();
    }

    public static bool TryRewriteSceneConversationForExternal(out string playerText, out string status)
    {
        playerText = string.Empty;
        status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Unavailable", "There is no completed round to undo in this conversation.");
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions()) return false;
        if (owner.IsSceneRewriteBusy())
        {
            status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Busy", "Wait for the current round to finish before undoing it.");
            return false;
        }
        if (owner._sceneConversationRuntime == null
            || !owner._sceneConversationRuntime.TryRewriteLatestSceneTurn(out playerText, out string reason))
        {
            status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Changed", "The conversation changed; this round can no longer be undone.");
            return false;
        }
        owner.ClearPresentationRound();
        BumpPresentation();
        status = ConversationUiLocalization.Text("AF_CONV_UI_Rewrite_Done", "The round was removed. Edit your message and send it again. Completed game actions are kept.");
        Logger.Log("ConversationRewrite", "scene round removed; confirmed game facts retained");
        return true;
    }
}
