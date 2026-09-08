using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
    // Weak context keys keep each generation isolated without a static Hero/work-item cache.
    private sealed class CourierDetachedPostprocessOwner
    {
        internal CourierDeliveryBehavior Behavior;
        internal InteractionEnvelope Envelope;
        internal Hero Recipient;
        internal ShoutBehavior.CourierActionPostprocessWorkItem WorkItem;
    }

    private static string NormalizeCourierDetachedVisibleReply(string rawText)
    {
        string reply = PrepareNpcReplyForActionPostprocess(rawText);
        if (string.IsNullOrWhiteSpace(reply) || LooksLikeApiError(reply))
            throw new InvalidOperationException("Courier generated no usable reply.");
        string visibleReply = CleanNpcReply(LegacyActionTagParser.RemoveProtocolTags(reply, _ => true));
        if (string.IsNullOrWhiteSpace(visibleReply))
            throw new InvalidOperationException("Courier generated no visible letter text.");
        return visibleReply;
    }

    private static async Task<PromptPackage> PrepareCourierDetachedPostprocessAsync(
        InteractionEnvelope envelope, string rawReply, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, CourierDetachedPostprocessOwner> owners,
        CancellationToken cancellationToken)
    {
        CourierDeliveryBehavior behavior = Instance;
        if (behavior == null || envelope == null || context == null) return null;
        if (!envelope.Snapshot.DetachedFacts.ContainsKey("courier_request_extras")) return null;
        long generation = envelope.Snapshot.Trace.RuntimeGeneration;
        CourierDetachedPostprocessOwner owner = await behavior.RunCourierOwnerPhaseAsync(
            generation, "detached_postprocess_prepare", () =>
            {
                Hero recipient = behavior.RequireCurrentCourierPostprocessRecipient(envelope);
                var facts = envelope.Snapshot.DetachedFacts;
                var request = new CourierReplyGenerationRequest
                {
                    SessionId = envelope.Snapshot.Identity.SessionId,
                    RuntimeGeneration = generation,
                    RecipientHeroId = envelope.Snapshot.Identity.SubjectId,
                    RecipientName = recipient.Name?.ToString() ?? "NPC",
                    LetterText = envelope.Snapshot.PlayerText,
                    Extras = ReadCourierFact(facts, "courier_request_extras"),
                    HistoryText = ReadCourierFact(facts, "courier_request_history"),
                    EntityPostprocessContext = ReadCourierFact(facts, "courier_request_entities"),
                    SelectedRuleHits = ReadCourierCsvFact(envelope.Snapshot, "courier_selected_rule_ids").ToList()
                };
                string reply = PrepareNpcReplyForActionPostprocess(rawReply);
                if (string.IsNullOrWhiteSpace(reply) || LooksLikeApiError(reply)) return null;
                var workItem = behavior.PrepareCourierDetachedPostprocessWorkItem(recipient, request,
                    request.LetterText, request.HistoryText, reply);
                return workItem == null ? null : new CourierDetachedPostprocessOwner
                {
                    Behavior = behavior, Envelope = envelope, Recipient = recipient, WorkItem = workItem
                };
            }, cancellationToken).ConfigureAwait(false);
        if (owner == null) return null;
        cancellationToken.ThrowIfCancellationRequested();
        owners.Add(context, owner);
        return new PromptPackage(new[]
        {
            new PromptMessage("system", owner.WorkItem.SystemPrompt),
            new PromptMessage("user", owner.WorkItem.UserPrompt)
        }, 5000, "legacy-courier-postprocess");
    }

    private static async Task<ActionPlan> CompleteCourierDetachedPostprocessAsync(
        string rawText, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, CourierDetachedPostprocessOwner> owners,
        LegacyActionTagParser parser, CancellationToken cancellationToken)
    {
        if (context == null || !owners.TryGetValue(context, out var owner) || !owners.Remove(context))
            return new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        return await owner.Behavior.RunCourierOwnerPhaseAsync(
            owner.Envelope.Snapshot.Trace.RuntimeGeneration, "detached_postprocess_complete", () =>
            {
                if (!ReferenceEquals(owner.Recipient, owner.Behavior.RequireCurrentCourierPostprocessRecipient(owner.Envelope)))
                    throw new OperationCanceledException("Courier postprocess recipient was replaced.");
                // The full existing normalizer owns qualification, indices, exclusions and weekly material.
                // Parse only its canonical output, never the model's unqualified raw tag stream.
                string normalized = owner.WorkItem.CompleteOnMainThread(rawText);
                return parser.Parse(normalized, context);
            }, cancellationToken).ConfigureAwait(false);
    }

    private Hero RequireCurrentCourierPostprocessRecipient(InteractionEnvelope envelope)
    {
        if (!ReferenceEquals(Instance, this) || envelope?.Snapshot?.Identity?.Channel != InteractionChannel.Courier
            || !SaveRuntimeGuard.IsCurrentGeneration(envelope.Snapshot.Trace.RuntimeGeneration))
            throw new OperationCanceledException("Courier postprocess belongs to an expired owner.");
        var session = GetSessionById(envelope.Snapshot.Identity.SessionId);
        Hero recipient = session == null ? null : ResolveRecipient(session);
        if (session == null || IsTerminalStage(session) || !session.ReplyGenerationStarted || session.ReplyGenerated
            || recipient == null || recipient.IsDead
            || !string.Equals(SafeHeroId(recipient), envelope.Snapshot.Identity.SubjectId, StringComparison.Ordinal))
            throw new OperationCanceledException("Courier postprocess target is no longer current.");
        return recipient;
    }

    private static string ReadCourierFact(IReadOnlyDictionary<string, string> facts, string key)
        => facts.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;

    private async Task<T> RunCourierOwnerPhaseAsync<T>(long generation, string source, Func<T> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        int state = 0;
        void Invoke()
        {
            if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
            if (cancellationToken.IsCancellationRequested || !ReferenceEquals(Instance, this)
                || !SaveRuntimeGuard.IsCurrentGeneration(generation))
            {
                completion.TrySetCanceled();
                return;
            }
            try { completion.TrySetResult(action()); }
            catch (OperationCanceledException) { completion.TrySetCanceled(); }
            catch (Exception error) { completion.TrySetException(error); }
        }
        bool mainThread = false;
        try { mainThread = TWParallel.IsMainThread(); } catch { }
        if (mainThread) Invoke(); else MainThreadActions.Enqueue(Invoke);
        using (cancellationToken.Register(() =>
        {
            Interlocked.CompareExchange(ref state, 2, 0);
            completion.TrySetCanceled();
        }))
        {
            Task winner = await Task.WhenAny(completion.Task, Task.Delay(30000)).ConfigureAwait(false);
            if (winner != completion.Task)
            {
                Interlocked.CompareExchange(ref state, 2, 0);
                Log("courier owner phase timeout source=" + source);
                throw new OperationCanceledException("Courier owner phase timed out.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) throw new OperationCanceledException("Courier owner phase expired.");
            return await completion.Task.ConfigureAwait(false);
        }
    }
}
