using System;
using System.Diagnostics;
using System.Threading;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Owns one request's linked cancellation source until execution AND active cancellation
/// callbacks have finished. Never holds the coordinator lock while calling user code.
/// Cancellation is cooperative: retiring a lease does not claim a network/game rollback.
/// </summary>
internal sealed class InteractionRequestLease
{
    private readonly object _gate = new object();
    private readonly CancellationToken _token;
    private CancellationTokenSource _source;
    private int _activeCancellations;
    private bool _completed;

    internal InteractionRequestLease(CancellationToken callerToken)
    {
        _source = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        _token = _source.Token;
    }

    internal CancellationToken Token => _token;
    internal bool IsCancellationRequested => _token.IsCancellationRequested;

    internal void Cancel()
    {
        CancellationTokenSource source;
        lock (_gate)
        {
            if (_completed) return;
            source = _source;
            _activeCancellations++;
        }
        try
        {
            source.Cancel();
        }
        catch (AggregateException error)
        {
            // A plugin/provider callback must not abort replacement or leave other channels live.
            // Cancellation has already been requested; preserve diagnostics without exposing payloads.
            try { Trace.TraceWarning("AF interaction cancellation callback failed ({0}).", error.GetType().Name); }
            catch (Exception) { /* A failing trace listener must not interrupt cancellation cleanup. */ }
        }
        finally
        {
            CancellationTokenSource retired;
            lock (_gate)
            {
                _activeCancellations--;
                retired = TakeCompletedSource();
            }
            retired?.Dispose();
        }
    }

    internal void Complete()
    {
        CancellationTokenSource retired;
        lock (_gate)
        {
            _completed = true;
            retired = TakeCompletedSource();
        }
        retired?.Dispose();
    }

    // Called only under _gate. The executing request, not Cancel/Dispose of its facade,
    // ends source ownership; this also prevents Dispose racing a captured Cancel handle.
    private CancellationTokenSource TakeCompletedSource()
    {
        if (!_completed || _activeCancellations != 0) return null;
        CancellationTokenSource retired = _source;
        _source = null;
        return retired;
    }
}
