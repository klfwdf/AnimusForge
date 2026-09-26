using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Coordinates an immutable interaction request without owning game state.
/// One active request is retained per channel/session identity. A newer request
/// cancels the older one, and the runtime generation is checked both before and
/// after the LLM pipeline so a loaded-save result cannot reach a facade.
/// </summary>
public sealed class InteractionRequestCoordinator : IDisposable
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, InteractionRequestLease> _inFlight =
        new Dictionary<string, InteractionRequestLease>(StringComparer.OrdinalIgnoreCase);
    private readonly IInteractionPipeline _pipeline;
    private readonly Func<long> _currentGeneration;
    private bool _disposed;

    public InteractionRequestCoordinator(IInteractionPipeline pipeline, Func<long> currentGeneration)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _currentGeneration = currentGeneration ?? throw new ArgumentNullException(nameof(currentGeneration));
    }

    public async Task<InteractionResult> ExecuteAsync(
        InteractionEnvelope envelope,
        RuntimeConfigSnapshot configuration,
        string moduleId,
        string providerId,
        CancellationToken cancellationToken)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }
        if (configuration == null)
        {
            return Failure(InteractionStatus.NonRetryableFailure, "missing_runtime_config");
        }
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return Failure(InteractionStatus.NonRetryableFailure, "missing_module_id");
        }
        if (!configuration.IsModuleEnabled(moduleId))
        {
            return Failure(InteractionStatus.SkippedByEligibility, "module_disabled");
        }
        if (!configuration.TryGetProvider(providerId, out LlmProviderSnapshot provider))
        {
            return Failure(InteractionStatus.DegradedWithoutProvider, "provider_unavailable");
        }
        if (!IsCurrentGeneration(envelope))
        {
            return Failure(InteractionStatus.CancelledAsStale, "stale_before_start");
        }

        string requestKey = BuildRequestKey(envelope.Snapshot.Identity);
        InteractionRequestLease linked;
        InteractionRequestLease previous;
        lock (_gate)
        {
            ThrowIfDisposed();
            linked = new InteractionRequestLease(cancellationToken);
            _inFlight.TryGetValue(requestKey, out previous);
            _inFlight[requestKey] = linked;
        }
        try
        {
            previous?.Cancel();
            linked.Token.ThrowIfCancellationRequested();
            InteractionResult result = await _pipeline.GenerateAsync(
                envelope,
                provider,
                linked.Token).ConfigureAwait(false);
            if (linked.IsCancellationRequested || !IsCurrentGeneration(envelope))
            {
                return Failure(InteractionStatus.CancelledAsStale, "stale_after_generation");
            }
            return result ?? Failure(InteractionStatus.NonRetryableFailure, "null_pipeline_result");
        }
        catch (OperationCanceledException)
        {
            return Failure(InteractionStatus.CancelledAsStale, "cancelled");
        }
        catch (Exception)
        {
            // A failing module must not take down the host or other channels.
            return Failure(InteractionStatus.NonRetryableFailure, "pipeline_exception");
        }
        finally
        {
            lock (_gate)
            {
                if (_inFlight.TryGetValue(requestKey, out InteractionRequestLease current)
                    && ReferenceEquals(current, linked))
                {
                    _inFlight.Remove(requestKey);
                }
            }
            linked.Complete();
        }
    }

    public void Cancel(InteractionIdentity identity)
    {
        if (identity == null)
        {
            return;
        }
        InteractionRequestLease source = null;
        lock (_gate)
        {
            if (_inFlight.TryGetValue(BuildRequestKey(identity), out source))
            {
                _inFlight.Remove(BuildRequestKey(identity));
            }
        }
        source?.Cancel();
    }

    public void Dispose()
    {
        List<InteractionRequestLease> sources;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            sources = new List<InteractionRequestLease>(_inFlight.Values);
            _inFlight.Clear();
        }
        foreach (InteractionRequestLease source in sources)
        {
            source.Cancel();
        }
    }

    private bool IsCurrentGeneration(InteractionEnvelope envelope)
    {
        try
        {
            return envelope.Snapshot.Trace.RuntimeGeneration > 0
                && envelope.Snapshot.Trace.RuntimeGeneration == _currentGeneration();
        }
        catch
        {
            return false;
        }
    }

    private static string BuildRequestKey(InteractionIdentity identity)
    {
        return identity.Channel + ":" + identity.SessionId;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InteractionRequestCoordinator));
        }
    }

    private static InteractionResult Failure(InteractionStatus status, string errorCode)
    {
        return new InteractionResult(
            status,
            string.Empty,
            new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
            Array.Empty<FactRecord>(),
            errorCode);
    }
}
