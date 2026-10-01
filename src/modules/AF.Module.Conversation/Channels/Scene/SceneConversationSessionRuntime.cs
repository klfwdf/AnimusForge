using System;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.Refactor.Runtime;
namespace AnimusForge;
internal sealed partial class SceneConversationSessionRuntime
{
    private readonly SceneConversationSessionPorts _ports;
    private readonly ConversationGameThreadDispatcher _dispatcher;
    private readonly SceneMovementController _sceneMovement;
    private readonly PendingOperationRegistry _pendingMainThreadFunctions;
    private ConversationRequestLifetime _sceneRequestLifetime = new ConversationRequestLifetime();
    private int _sceneConversationEpoch;
    private bool IsProcessingShout { get => _ports.GetProcessing(); set => _ports.SetProcessing(value); }
    internal int ConversationEpoch { get => Volatile.Read(ref _sceneConversationEpoch); set => Interlocked.Exchange(ref _sceneConversationEpoch, value); }
    internal bool IsWaitingForPostprocess { get => _isWaitingForScenePostprocessGate; set => _isWaitingForScenePostprocessGate = value; }
    internal ConversationRequestLifetime RequestLifetime { get => _sceneRequestLifetime; set => _sceneRequestLifetime=value; }
    internal ScenePlayerShoutRequestOwner PlayerRequests => _scenePlayerShoutRequestOwner;
    internal SceneConversationSessionRuntime(SceneConversationSessionPorts ports, ConversationGameThreadDispatcher dispatcher, SceneMovementController movement, PendingOperationRegistry pending)
    { _ports=ports??throw new ArgumentNullException(nameof(ports));_dispatcher=dispatcher??throw new ArgumentNullException(nameof(dispatcher));_sceneMovement=movement??throw new ArgumentNullException(nameof(movement));_pendingMainThreadFunctions=pending??throw new ArgumentNullException(nameof(pending)); }
	private readonly object _immediateSceneReactionGateLock = new object();

	private readonly Dictionary<int, float> _immediateSceneReactionLastStartedMissionTime = new Dictionary<int, float>();

	private readonly Dictionary<int, long> _immediateSceneReactionActiveRequestIds = new Dictionary<int, long>();

	private readonly Dictionary<long, ImmediateSceneReactionRequest> _pendingImmediateSceneReactionRequests = new Dictionary<long, ImmediateSceneReactionRequest>();

	private long _immediateSceneReactionRequestSequence;

	private readonly ScenePlayerShoutRequestOwner _scenePlayerShoutRequestOwner = new ScenePlayerShoutRequestOwner();


    internal int AdvanceConversationEpoch() => Interlocked.Increment(ref _sceneConversationEpoch);
    internal int PendingImmediateCount { get { lock (_immediateSceneReactionGateLock) return _pendingImmediateSceneReactionRequests.Count; } }
    internal bool HasActiveImmediateReactions { get { lock (_immediateSceneReactionGateLock) return _immediateSceneReactionActiveRequestIds.Count > 0; } }
    internal void ResetImmediateReactions(bool clearCooldown = true)
    {
        if (clearCooldown) SceneActionDirectiveController.Reset();
        List<ImmediateSceneReactionRequest> pending;
        lock (_immediateSceneReactionGateLock)
        {
            pending = new List<ImmediateSceneReactionRequest>(_pendingImmediateSceneReactionRequests.Values);
            if (clearCooldown) _immediateSceneReactionLastStartedMissionTime.Clear();
            _immediateSceneReactionActiveRequestIds.Clear();
            _pendingImmediateSceneReactionRequests.Clear();
        }
        // Retired old-scene work completes without launching its old no-speech movement fallback.
        foreach (ImmediateSceneReactionRequest request in pending)
            SettleImmediateSceneReaction(request, false, allowNoSpeech: false);
    }

    private void SettleImmediateSceneReaction(ImmediateSceneReactionRequest request, bool generated, bool allowNoSpeech)
    {
        if (request == null || Interlocked.CompareExchange(ref request.CompletionClaimed, 1, 0) != 0) return;
        FinishImmediateSceneReactionGeneration(request.TargetAgentIndex, request.RequestId);
        if (!generated && allowNoSpeech) InvokeImmediateSceneReactionNoSpeechFallback(request.OnNoSpeech);
        try { request.OnCompleted?.Invoke(generated); }
        catch (Exception ex) { Logger.Log("ShoutBehavior", "[WARN] Immediate reaction completion failed: " + ex.Message); }
    }
}
