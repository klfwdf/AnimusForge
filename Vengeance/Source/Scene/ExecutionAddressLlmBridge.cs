using System;

namespace RichExecutions.Scene;

/// <summary>
/// The shared ceremony only knows that a host may supply lines. AnimusForge
/// assigns the delegates; a standalone module leaves them empty.
/// </summary>
internal static class ExecutionAddressLlmBridge
{
    internal static Action<Guid> OnTick { get; set; }
    internal static Action<Guid> OnCancel { get; set; }
    internal static Action<Guid, ExecutionSpeechPhase> OnPlay { get; set; }

    internal static void Tick(Guid sessionId) => OnTick?.Invoke(sessionId);
    internal static void Cancel(Guid sessionId) => OnCancel?.Invoke(sessionId);
    internal static void Play(Guid sessionId, ExecutionSpeechPhase phase) => OnPlay?.Invoke(sessionId, phase);
}
