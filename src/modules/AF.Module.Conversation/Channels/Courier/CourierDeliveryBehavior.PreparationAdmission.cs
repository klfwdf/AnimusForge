using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class CourierDeliveryBehavior
{
    private sealed class CourierPreparationAdmission
    {
        // Identity handles only on a worker; live access belongs to the existing owner phase.
        internal CourierSession Session;
        internal Hero Participant;
        internal string FallbackLetter;
    }

    private CourierPreparationAdmission CaptureCourierPreparationAdmission(string sessionId, bool inbound, long generation)
    {
        CourierSession session = GetSessionById(sessionId);
        if (session == null || IsTerminalStage(session) || IsInboundToPlayer(session) != inbound) return null;
        Hero participant = inbound ? ResolveSender(session) : ResolveRecipient(session);
        string fallback = inbound ? NormalizeInboundLetterText(string.IsNullOrWhiteSpace(session.InboundFallbackLetter)
            ? session.LetterText : session.InboundFallbackLetter, session, participant) : null;
        if (participant == null || participant.IsDead)
        {
            string reason = inbound ? "inbound_letter_generated_sender_invalid" : "reply_generated_recipient_invalid";
            EnqueueMainThreadActionForGeneration(generation, () =>
            {
                CourierSession current = GetSessionById(sessionId);
                if (!ReferenceEquals(Instance, this) || !ReferenceEquals(current, session) || IsTerminalStage(current)
                    || IsInboundToPlayer(current) != inbound
                    || !ReferenceEquals(inbound ? ResolveSender(current) : ResolveRecipient(current), participant)) return;
                if (inbound) current.LetterText = fallback;
                current.ReplyGenerated = true;
                current.ReplyGenerationStarted = false;
                ProcessSessionById(sessionId, reason);
            }, reason);
            return null;
        }
        return new CourierPreparationAdmission { Session = session, Participant = participant, FallbackLetter = fallback };
    }

    private async Task<bool> EnsureCourierPersonaContextReadyAsync(Hero hero, string chain, string sessionId, CourierSession session, long generation)
    {
        MyBehavior personaOwner = MyBehavior.Instance;
        Task<NpcPersonaReadinessSnapshot> Observe() => RunCourierOwnerPhaseAsync(generation, "courier_persona_" + chain, () =>
        {
            bool inbound = string.Equals(chain, "inbound", StringComparison.Ordinal);
            if (!IsCourierHistoryOwnerCurrent(sessionId, session, hero, inbound) || hero.IsDead) return null;
            return MyBehavior.CaptureNpcPersonaReadiness(hero, personaOwner);
        }, CancellationToken.None);
        try
        {
            NpcPersonaReadinessSnapshot state = await Observe().ConfigureAwait(false);
            if (state == null) return false;
            if (!state.Available || !state.NeedsGeneration) return true;
            const int waitTimeoutMs = 180000;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Log("[ContextAlignment] persona_start chain=courier_" + chain + " hero=" + state.HeroId);
            Task generationTask = MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync(hero, ignoreRetryCooldown: true);
            if (!await PersonaGenerationWaiter.WaitAsync(generationTask,
                async () => await Observe().ConfigureAwait(false) != null,
                () => watch.ElapsedMilliseconds < waitTimeoutMs).ConfigureAwait(false))
                return await Observe().ConfigureAwait(false) != null;
            while (watch.ElapsedMilliseconds < waitTimeoutMs)
            {
                state = await Observe().ConfigureAwait(false);
                if (state == null) return false;
                if (!state.Available || !state.NeedsGeneration) return true;
                if (!state.Active)
                {
                    Log("[ContextAlignment] persona_fallback chain=courier_" + chain + " hero=" + state.HeroId
                        + " coolingDown=" + state.CoolingDown + " waitMs=" + watch.ElapsedMilliseconds);
                    return true;
                }
                await Task.Delay(500).ConfigureAwait(false);
            }
            // Observe once more before accepting the legacy timeout fallback.
            state = await Observe().ConfigureAwait(false);
            return state != null;
        }
        catch (OperationCanceledException) when (!ReferenceEquals(Instance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation))
        { return false; }
    }
}
