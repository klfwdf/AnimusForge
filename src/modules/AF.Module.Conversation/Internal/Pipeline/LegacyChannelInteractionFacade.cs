using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Channel-neutral lifecycle facade for SceneShout, NativeConversation and
/// Courier. Capture and Commit are owned by the channel and must run on its
/// main-thread boundary; only the immutable envelope crosses into Generate.
/// </summary>
public sealed class LegacyChannelInteractionFacade : IDisposable
{
    private readonly InteractionRequestCoordinator _coordinator;
    private readonly InteractionResultCommitter _committer;
    private readonly Func<string, InteractionEnvelope> _capture;
    private bool _disposed;

    public LegacyChannelInteractionFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<long> currentGeneration,
        Func<string, InteractionEnvelope> capture)
    {
        _coordinator = LegacyInteractionPipelineComposition.Create(ports, gateway, currentGeneration);
        _committer = new InteractionResultCommitter(currentGeneration);
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
    }

    public InteractionEnvelope Capture(string playerText)
    {
        ThrowIfDisposed();
        return _capture(playerText) ?? throw new InvalidOperationException("Interaction capture returned no envelope.");
    }

    public Task<InteractionResult> GenerateAsync(
        InteractionEnvelope envelope,
        RuntimeConfigSnapshot configuration,
        string moduleId,
        string providerId,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _coordinator.ExecuteAsync(envelope, configuration, moduleId, providerId, cancellationToken);
    }

    public InteractionCommitResult Commit(
        InteractionEnvelope envelope,
        InteractionResult result,
        IActionPlanExecutor actionExecutor,
        IInteractionMemory memory,
        bool appendPlayerInput = true)
    {
        ThrowIfDisposed();
        return _committer.Commit(envelope, result, actionExecutor, memory, appendPlayerInput);
    }

    public void Cancel(InteractionEnvelope envelope)
    {
        if (envelope != null)
        {
            _coordinator.Cancel(envelope.Snapshot.Identity);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _coordinator.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LegacyChannelInteractionFacade));
        }
    }
}
