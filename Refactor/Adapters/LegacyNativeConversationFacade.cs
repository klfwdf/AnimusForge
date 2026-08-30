using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Opt-in Native Conversation sidecar. Capture and Commit are main-thread
/// operations; GenerateAsync only receives the detached envelope and runs the
/// shared three-stage coordinator. The old ShoutBehavior entry remains the
/// default until the final cutover gate.
/// </summary>
public sealed class LegacyNativeConversationFacade : IDisposable
{
    private readonly LegacyChannelInteractionFacade _inner;

    public LegacyNativeConversationFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<long> currentGeneration,
        Func<string, InteractionEnvelope> capture)
    {
        _inner = new LegacyChannelInteractionFacade(ports, gateway, currentGeneration, capture);
    }

    /// <summary>
    /// Must be called on the game main thread while the Native conversation
    /// target is valid.
    /// </summary>
    public InteractionEnvelope Capture(string playerText)
    {
        return _inner.Capture(playerText);
    }

    /// <summary>
    /// Safe to await from a background continuation because only the detached
    /// envelope crosses this boundary.
    /// </summary>
    public Task<InteractionResult> GenerateAsync(
        InteractionEnvelope envelope,
        RuntimeConfigSnapshot configuration,
        string moduleId,
        string providerId,
        CancellationToken cancellationToken)
    {
        return _inner.GenerateAsync(envelope, configuration, moduleId, providerId, cancellationToken);
    }

    /// <summary>
    /// Must be called on the game main thread. The current generation is
    /// checked again before the action executor is allowed to run.
    /// </summary>
    public InteractionCommitResult Commit(
        InteractionEnvelope envelope,
        InteractionResult result,
        IActionPlanExecutor actionExecutor,
        IInteractionMemory memory,
        bool appendPlayerInput = true)
    {
        return _inner.Commit(envelope, result, actionExecutor, memory, appendPlayerInput);
    }

    public void Cancel(InteractionEnvelope envelope)
    {
        _inner.Cancel(envelope);
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
}
