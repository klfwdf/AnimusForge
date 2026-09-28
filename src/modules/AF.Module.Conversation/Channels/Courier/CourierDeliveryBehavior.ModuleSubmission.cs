using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
    private const int MaximumModuleCourierOperations = 128;
    private sealed class ModuleCourierRequest
    {
        internal CoreDialogueOperation Operation;
        internal long Generation;
        internal CourierSession Session;
        internal bool AdmissionReleased;
        internal bool Finished;
        internal IDisposable QueueRegistration;
        internal CoreCourierAcceptedSteps Accepted;
        internal CoreCourierTransportOutcome Transport = CoreCourierTransportOutcome.NotStarted;
        internal Dictionary<Tuple<string, string, string>, int> ExpectedPayload;
    }
    private readonly object _moduleCourierGate = new object();
    private readonly Dictionary<CoreDialogueOperation, ModuleCourierRequest> _moduleCourierRequests =
        new Dictionary<CoreDialogueOperation, ModuleCourierRequest>();
    private readonly Dictionary<string, ModuleCourierRequest> _moduleCourierSessions =
        new Dictionary<string, ModuleCourierRequest>(StringComparer.Ordinal);

    // Still an internal entry: public CourierSubmit remains unavailable until all transport
    // receipts are wired and verified. This queue reuses the real Courier owner/retirement path.
    internal static void SubmitModuleCourierDialogue(CoreDialogueOperation operation)
    {
        if (operation == null) return;
        CourierDeliveryBehavior owner = Instance;
        if (owner == null || !owner._pendingOwnerPhases.Accepting)
        {
            operation.Finish("courier.owner_unavailable");
            return;
        }
        if (operation.Channel != CoreDialogueChannel.Courier)
        {
            operation.Finish("dialogue.invalid_request");
            return;
        }
        var request = new ModuleCourierRequest { Operation = operation, Generation = SaveRuntimeGuard.CaptureGeneration() };
        bool full;
        lock (owner._moduleCourierGate)
        {
            if (owner._moduleCourierRequests.ContainsKey(operation)) return;
            full = owner._moduleCourierRequests.Count >= MaximumModuleCourierOperations;
            if (!full) owner._moduleCourierRequests.Add(operation, request);
        }
        if (full) { operation.Finish("courier.owner_capacity"); return; }
        _ = owner.RunModuleCourierDialogueAsync(request);
    }

    private async Task RunModuleCourierDialogueAsync(ModuleCourierRequest request)
    {
        CoreDialogueOperation operation = request.Operation;
        try
        {
            long version = _pendingOwnerPhases.Version;
            // Cancellation settles the operation immediately, but cannot make its unpumped
            // physical callback disappear. Keep that capacity slot until dequeue or queue reset.
            IDisposable queued = _pendingOwnerPhases.Register(version, () => ReleaseModuleCourierAdmission(request));
            bool released;
            lock (_moduleCourierGate)
            {
                released = request.AdmissionReleased;
                if (!released) request.QueueRegistration = queued;
            }
            if (released) queued?.Dispose();
            using (IDisposable lifetime = _pendingOwnerPhases.Register(version, () => operation.Finish("courier.owner_retired")))
            using (var waiting = new CancellationTokenSource())
            {
                try
                {
                    Task<bool> admission = RunCourierOwnerPhaseAsync(request.Generation, "module_courier_claim", () =>
                    {
                        if (operation.Snapshot.State != CoreDialogueState.Queued) return false;
                        if (!TryTakeModuleCourierTicket(operation.ClientId, operation.ContextIdentity, out PendingCourierFlow flow, out long revision))
                        {
                            operation.Finish("courier.context_unavailable");
                            return false;
                        }
                        CourierSession session = DispatchCourierDraft(flow, revision, operation.PlayerText, out string reason, operation);
                        if (session == null) operation.Finish(reason ?? "courier.dispatch_unconfirmed");
                        return session != null;
                    }, waiting.Token, () => ReleaseModuleCourierAdmission(request));
                    // Queued cancellation/disposal releases the physical owner wait without a tick.
                    if (await Task.WhenAny(admission, operation.Completion).ConfigureAwait(false) != admission)
                        waiting.Cancel();
                    await admission.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { operation.Finish("courier.context_unavailable"); }
                catch (Exception) { operation.Finish("courier.dispatch_unconfirmed"); }
                // Dispatch is NOT completion. The bound transport owner supplies later receipts.
                await operation.Completion.ConfigureAwait(false);
            }
        }
        catch (Exception) { operation.Finish("courier.execution_failed"); }
        finally
        {
            lock (_moduleCourierGate)
            {
                request.Finished = true;
                if (request.AdmissionReleased) _moduleCourierRequests.Remove(operation);
                if (request.Session != null && _moduleCourierSessions.TryGetValue(request.Session.Id, out ModuleCourierRequest current)
                    && ReferenceEquals(current, request)) _moduleCourierSessions.Remove(request.Session.Id);
                request.Session = null;
            }
        }
    }

    private void ReleaseModuleCourierAdmission(ModuleCourierRequest request)
    {
        IDisposable queued;
        lock (_moduleCourierGate)
        {
            if (request.AdmissionReleased) return;
            request.AdmissionReleased = true;
            queued = request.QueueRegistration;
            request.QueueRegistration = null;
            if (request.Finished) _moduleCourierRequests.Remove(request.Operation);
        }
        queued?.Dispose();
    }

    private void BindModuleCourierSession(CoreDialogueOperation operation, CourierSession session, PendingCourierFlow flow)
    {
        if (operation == null) return;
        lock (_moduleCourierGate)
        {
            if (!_moduleCourierRequests.TryGetValue(operation, out ModuleCourierRequest request)
                || !ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(request.Generation)
                || session == null || IsInboundToPlayer(session) || _moduleCourierSessions.ContainsKey(session.Id))
                throw new InvalidOperationException("Courier module source expired before binding.");
            request.Session = session;
            request.ExpectedPayload = new Dictionary<Tuple<string, string, string>, int>();
            foreach (CourierCargoEntry entry in flow.SelectedEntries)
            {
                var key = CourierCargoIdentity(entry.Kind, entry.Id, entry.SourceSettlementId);
                request.ExpectedPayload.TryGetValue(key, out int count);
                request.ExpectedPayload[key] = checked(count + entry.Amount);
            }
            request.Accepted = CoreCourierAcceptedSteps.Dispatched;
            request.Transport = CoreCourierTransportOutcome.InTransit;
            _moduleCourierSessions.Add(session.Id, request);
            operation.RecordCourierProgress(new CoreCourierReceipt(request.Accepted, request.Transport));
        }
    }

    private ModuleCourierRequest FindModuleCourierRequest(CourierSession session, bool allowRemoved = false)
    {
        if (!TWParallel.IsMainThread() || session == null) return null;
        ModuleCourierRequest request;
        lock (_moduleCourierGate)
        {
            if (!_moduleCourierSessions.TryGetValue(session.Id, out request)
                || !ReferenceEquals(request.Session, session)) return null;
        }
        CourierSession current = GetSessionById(session.Id);
        if (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(request.Generation))
        {
            request.Operation.Finish("courier.context_expired");
            return null;
        }
        if (!ReferenceEquals(current, session) && !(allowRemoved && current == null))
        {
            request.Operation.Finish("courier.session_replaced");
            return null;
        }
        return request;
    }

    private void RecordModuleCourierStep(CourierSession session, CoreCourierAcceptedSteps step, string deliveredReply = null)
    {
        ModuleCourierRequest request = FindModuleCourierRequest(session);
        if (request == null) return;
        lock (_moduleCourierGate)
        {
            request.Accepted |= step;
            request.Operation.RecordCourierProgress(new CoreCourierReceipt(request.Accepted, request.Transport), deliveredReply);
        }
    }

    private void FailModuleCourierSession(CourierSession session, string reason,
        CoreCourierTransportOutcome? transport = null)
    {
        ModuleCourierRequest request = FindModuleCourierRequest(session, allowRemoved: true);
        if (request == null) return;
        lock (_moduleCourierGate)
        {
            if (transport.HasValue) request.Transport = transport.Value;
            request.Operation.RecordCourierProgress(new CoreCourierReceipt(request.Accepted, request.Transport));
            request.Operation.Finish(reason);
        }
    }

    private void CompleteModuleCourierTransport(CourierSession session, bool cleanupConfirmed)
    {
        ModuleCourierRequest request = FindModuleCourierRequest(session, allowRemoved: true);
        if (request == null) return;
        lock (_moduleCourierGate)
        {
            request.Transport = cleanupConfirmed ? CoreCourierTransportOutcome.Returned : CoreCourierTransportOutcome.Unconfirmed;
            var receipt = new CoreCourierReceipt(request.Accepted, request.Transport);
            request.Operation.RecordCourierProgress(receipt);
            if (receipt.Complete) request.Operation.RecordOwnerCompletion(request.Operation.Snapshot.Reply);
            request.Operation.Finish(cleanupConfirmed ? "courier.completion_receipt_missing" : "courier.transport_cleanup_unconfirmed");
        }
    }

    private void CheckModuleCourierSessionIdentity(CourierSession current)
    {
        if (current == null) return;
        ModuleCourierRequest request;
        lock (_moduleCourierGate) _moduleCourierSessions.TryGetValue(current.Id, out request);
        if (request != null) FindModuleCourierRequest(request.Session);
    }

    private bool ConfirmModuleCourierPayload(CourierSession session)
    {
        ModuleCourierRequest request = FindModuleCourierRequest(session);
        if (request == null) return true;
        var delivered = new Dictionary<Tuple<string, string, string>, int>();
        foreach (CourierCargoEntry entry in session.Entries)
        {
            if (entry == null || !entry.Delivered || entry.Amount <= 0) return false;
            var key = CourierCargoIdentity(entry.Kind, entry.Id, entry.SourceSettlementId);
            delivered.TryGetValue(key, out int count);
            delivered[key] = checked(count + entry.Amount);
        }
        if (delivered.Count != request.ExpectedPayload.Count) return false;
        foreach (var expected in request.ExpectedPayload)
            if (!delivered.TryGetValue(expected.Key, out int count) || count != expected.Value) return false;
        RecordModuleCourierStep(session, CoreCourierAcceptedSteps.Payload);
        return true;
    }
}
