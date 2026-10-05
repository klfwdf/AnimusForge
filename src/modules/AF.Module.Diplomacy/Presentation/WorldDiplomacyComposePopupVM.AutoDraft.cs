using System;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class WorldDiplomacyComposePopupVM
{
    private readonly long _draftGeneration = SaveRuntimeGuard.CaptureGeneration();
    private long _bodyRevision;
    private long _draftRequestId;
    private bool _isDrafting;
    private bool _draftFinalized;
    private bool _canAutoDraft;
    private string _autoDraftText = "书记官代笔";
    private CancellationTokenSource _draftCancellation;
    private DraftCompletion _draftCompletion;

    private sealed class DraftCompletion
    {
        internal readonly long RequestId;
        internal readonly long Revision;
        internal readonly WorldDiplomacyPlayerDraftInput Input;
        internal readonly WorldDiplomacyPlayerDraftResult Result;
        internal DraftCompletion(long requestId, long revision, WorldDiplomacyPlayerDraftInput input,
            WorldDiplomacyPlayerDraftResult result)
        { RequestId = requestId; Revision = revision; Input = input; Result = result; }
    }

    internal bool IsCurrentDraftWindow => !_draftFinalized && SaveRuntimeGuard.IsCurrentGeneration(_draftGeneration);

    [DataSourceProperty]
    public bool CanAutoDraft
    {
        get => _canAutoDraft;
        private set
        {
            if (_canAutoDraft == value) return;
            _canAutoDraft = value;
            OnPropertyChangedWithValue(value, nameof(CanAutoDraft));
        }
    }

    [DataSourceProperty]
    public string AutoDraftText
    {
        get => _autoDraftText;
        private set
        {
            if (_autoDraftText == value) return;
            _autoDraftText = value;
            OnPropertyChangedWithValue(value, nameof(AutoDraftText));
        }
    }

    private void RefreshDraftAvailability()
    {
        bool ready = IsCurrentDraftWindow && !_isDrafting && !string.IsNullOrWhiteSpace(BodyText);
        CanPublish = ready;
        CanAutoDraft = ready;
    }

    public void ExecuteAutoDraft()
    {
        if (!IsCurrentDraftWindow || _isDrafting) return;
        if (string.IsNullOrWhiteSpace(BodyText)) { HintText = "请先写下标题或拟文要点。"; return; }
        try
        {
            if (!WorldDiplomacyPlayerDraftApplication.TryPrepare(BodyText, _draftGeneration,
                out WorldDiplomacyPlayerDraftInput input, out var run, out string error))
            { HintText = error; return; }
            long id = ++_draftRequestId;
            long revision = _bodyRevision;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            CancellationToken token = cancellation.Token;
            _draftCancellation = cancellation;
            _isDrafting = true;
            AutoDraftText = "书记官拟稿中…";
            HintText = "书记官正在拟稿，请稍候。";
            RefreshDraftAvailability();
            // One bounded mailbox per live window, drained by its existing UI tick.
            Task.Run(async () =>
            {
                WorldDiplomacyPlayerDraftResult result;
                try { result = await run(token).ConfigureAwait(false); }
                catch (OperationCanceledException)
                { result = WorldDiplomacyPlayerDraftResult.Failed("拟稿已取消，原文已保留。"); }
                catch (Exception ex)
                {
                    Logger.Log("WorldDiplomacyPlayerDraft", "request failed type=" + ex.GetType().Name);
                    result = WorldDiplomacyPlayerDraftResult.Failed("书记官拟稿失败，原文已保留。请重试。");
                }
                finally { cancellation.Dispose(); }
                Interlocked.Exchange(ref _draftCompletion, new DraftCompletion(id, revision, input, result));
            });
        }
        catch (Exception ex)
        {
            Logger.Log("WorldDiplomacyPlayerDraft", "start failed type=" + ex.GetType().Name);
            CancelDraftRequest();
            _isDrafting = false;
            AutoDraftText = "书记官代笔";
            HintText = "书记官暂时无法拟稿，原文已保留。请重试。";
            RefreshDraftAvailability();
        }
    }

    internal void ProcessAutoDraftCompletion()
    {
        if (Volatile.Read(ref _draftCompletion) == null) return;
        DraftCompletion completion = Interlocked.Exchange(ref _draftCompletion, null);
        if (completion == null || !IsCurrentDraftWindow || completion.RequestId != _draftRequestId
            || completion.Input.Generation != _draftGeneration) return;
        _draftCancellation = null; // The worker disposes its source after completion.
        _isDrafting = false;
        AutoDraftText = "书记官代笔";
        if (completion.Revision != _bodyRevision || !string.Equals(BodyText, completion.Input.OriginalBody, StringComparison.Ordinal))
            HintText = "拟稿期间内容已修改，保留您当前的文字。";
        else if (completion.Result?.Success != true)
            HintText = completion.Result?.Error ?? "书记官拟稿失败，原文已保留。请重试。";
        else
        {
            BodyText = completion.Result.Body;
            HintText = "稿件已回填，可修改后发布；再次代笔将重新拟稿。";
        }
        RefreshDraftAvailability();
    }

    private void CancelDraftRequest()
    {
        CancellationTokenSource cancellation = _draftCancellation;
        _draftCancellation = null;
        if (cancellation == null) return;
        try { cancellation.Cancel(); }
        catch (ObjectDisposedException) { /* Worker already finished and released its source. */ }
    }

    public override void OnFinalize()
    {
        if (_draftFinalized) return;
        _draftFinalized = true;
        ++_draftRequestId;
        CancelDraftRequest();
        Interlocked.Exchange(ref _draftCompletion, null);
        RefreshDraftAvailability();
        base.OnFinalize();
    }
}
