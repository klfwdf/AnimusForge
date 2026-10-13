using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
    // A present result with empty Text is prepared-empty, never a request to read live history again.
    private sealed class CourierPreparedHistory
    {
        internal CourierPreparedHistory(string extraFact, string text, List<ConversationMessage> messages)
        {
            ExtraFact = extraFact ?? string.Empty;
            Text = (text ?? string.Empty).Trim();
            Messages = messages;
        }
        internal string ExtraFact { get; }
        internal string Text { get; }
        internal List<ConversationMessage> Messages { get; }
    }

    private sealed class CourierHistoryWork
    {
        internal CourierHistoryWork(string extraFact, Func<string> resolve, MyBehavior.HistoryPromptSnapshot snapshot)
        {
            ExtraFact = extraFact;
            Resolve = resolve;
            Snapshot = snapshot;
        }
        internal string ExtraFact { get; }
        internal Func<string> Resolve { get; }
        internal MyBehavior.HistoryPromptSnapshot Snapshot { get; }
    }

    // The two existing synchronous public envelope-capture APIs promise a synchronous result.
    // Keep their main-thread history semantics here; never route normal async preparation back here.
    private static CourierPreparedHistory CaptureCourierHistoryForLegacyEnvelope(CourierSession session,
        Hero participant, bool inbound)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Courier envelope capture requires the game thread.");
        string extraFact = inbound
            ? BuildInboundDeliveryFactText(session, delivered: false, participant)
            : BuildDeliveryFactText(session, delivered: true, participant);
        MyBehavior.HistoryPromptSnapshot snapshot = null;
        Func<string> resolve = MyBehavior.CaptureHistoryContextWorkForHero(participant,
            inbound ? null : session.LetterText, extraFact, false, SaveRuntimeGuard.CaptureGeneration(),
            captured => snapshot = captured);
        string history = resolve?.Invoke() ?? string.Empty;
        return new CourierPreparedHistory(extraFact, history, snapshot?.UncompressedMessages);
    }

    private bool IsCourierHistoryOwnerCurrent(string sessionId, CourierSession expectedSession,
        Hero expectedParticipant, bool inbound)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Courier history validation requires the game thread.");
        CourierSession current = GetSessionById(sessionId);
        return ReferenceEquals(current, expectedSession) && current != null && !IsTerminalStage(current)
            && IsInboundToPlayer(current) == inbound
            && expectedParticipant != null
            && ReferenceEquals(inbound ? ResolveSender(current) : ResolveRecipient(current), expectedParticipant);
    }

    private async Task<CourierPreparedHistory> PrepareCourierHistoryAsync(string sessionId,
        CourierSession expectedSession, Hero expectedParticipant, bool inbound, long generation)
    {
        string source = inbound ? "courier_inbound_history" : "courier_reply_history";
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            CourierHistoryWork work = await RunCourierOwnerPhaseAsync(generation, source + "_capture", () =>
            {
                if (!IsCourierHistoryOwnerCurrent(sessionId, expectedSession, expectedParticipant, inbound)) return null;
                Log("[MemoryPerf] history_start reason=" + source + " session=" + sessionId + " mode=main_capture");
                // Capture live delivery facts once, alongside the history snapshot. No worker reads Hero here.
                string extraFact = inbound
                    ? BuildInboundDeliveryFactText(expectedSession, delivered: false, expectedParticipant)
                    : BuildDeliveryFactText(expectedSession, delivered: true, expectedParticipant);
                Func<string> resolve;
                MyBehavior.HistoryPromptSnapshot snapshot = null;
                try
                {
                    resolve = MyBehavior.CaptureHistoryContextWorkForHero(expectedParticipant,
                        inbound ? null : expectedSession.LetterText, extraFact,
                        includeCurrentActiveSceneSession: false, generation: generation, captured: value => snapshot = value);
                }
                catch (Exception error)
                {
                    // Legacy BuildHistoryContextForExternal treated unavailable history as empty.
                    // Preserve that optional-context fallback, not a whole-request retry.
                    Log(source + " capture failed: " + error.GetType().Name);
                    resolve = () => string.Empty;
                }
                return resolve == null ? null : new CourierHistoryWork(extraFact, resolve, snapshot);
            }, CancellationToken.None).ConfigureAwait(false);
            if (work == null) return null;

            string history = await Task.Run(() =>
            {
                try { return work.Resolve() ?? string.Empty; }
                catch (Exception error)
                {
                    Log(source + " resolve failed: " + error.GetType().Name);
                    return string.Empty;
                }
            }).ConfigureAwait(false);

            bool current = await RunCourierOwnerPhaseAsync(generation, source + "_accept",
                () => IsCourierHistoryOwnerCurrent(sessionId, expectedSession, expectedParticipant, inbound)
                    && (work.Snapshot == null || work.Snapshot.IsCurrent()),
                CancellationToken.None).ConfigureAwait(false);
            Log("[MemoryPerf] history_done reason=" + source + " session=" + sessionId
                + " chars=" + history.Length + " mode=captured_snapshot accepted=" + current
                + " totalMs=" + Math.Round(elapsed.Elapsed.TotalMilliseconds, 2));
            return current ? new CourierPreparedHistory(work.ExtraFact, history, work.Snapshot?.UncompressedMessages) : null;
        }
        catch (OperationCanceledException) when (!ReferenceEquals(Instance, this)
            || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        {
            // Retire old owner/save work; a same-owner timeout still reaches the existing failure handler.
            return null;
        }
    }
}
