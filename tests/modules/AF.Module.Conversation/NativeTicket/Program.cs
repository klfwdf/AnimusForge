using System;
using System.Threading.Tasks;
using AnimusForge;

internal static class Program
{
    private sealed class Ticket
    {
        internal readonly int Value;
        internal Ticket(int value) { Value = value; }
        public override bool Equals(object other) => other is Ticket ticket && ticket.Value == Value;
        public override int GetHashCode() => Value;
    }

    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }

    private static async Task Main()
    {
        var owner = new NativeConversationAdmissionOwner<Ticket>();
        var first = new Ticket(7);
        var sameValue = new Ticket(7);
        Check(owner.Current == null && !owner.Owns(null), "empty_slot_has_no_owner");
        Check(owner.ConversationEpoch == 0 && owner.PresentationRevision == 0, "process_local_zero_stamps");
        owner.ReserveCaptured(first);
        Check(owner.Owns(first) && ReferenceEquals(owner.Current, first), "reservation_owns_exact_ticket");
        Check(!owner.Owns(sameValue), "equal_value_is_not_ticket_identity");
        owner.Release(sameValue);
        Check(owner.Owns(first), "equal_ticket_cannot_release_slot");
        Check(owner.ConversationEpoch == 0, "foreign_release_does_not_end_conversation");
        Check(owner.PresentationRevision == 0, "reservation_does_not_consume_presentation");
        long revision = owner.BeginPresentation();
        Check(revision == 1 && owner.IsPresentationCurrent(revision), "successful_admission_advances_revision");
        Check(owner.ConversationEpoch == 0, "presentation_does_not_end_conversation");
        owner.Release(first);
        Check(owner.Current == null, "backend_releases_own_slot");
        Check(owner.IsPresentationCurrent(revision), "backend_completion_keeps_captured_presentation");
        Check(owner.ConversationEpoch == 0, "backend_completion_does_not_end_conversation");
        Check(!owner.IsConversationEpochCurrent(1), "different_epoch_rejected");

        // Real asynchronous boundary, released by a deterministic gate, not sleep/timing luck.
        owner.ReserveCaptured(first);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task oldRequest = Task.Run(async () =>
        {
            started.SetResult(true);
            await finish.Task.ConfigureAwait(false);
            owner.Release(first);
        });
        await started.Task.ConfigureAwait(false);
        Check(!oldRequest.IsCompleted, "old_request_is_really_suspended");
        long epoch = owner.ConversationEpoch;
        owner.EndConversation();
        Check(!owner.IsConversationEpochCurrent(epoch) && owner.ConversationEpoch == epoch + 1,
            "conversation_end_retires_queued_request_even_if_game_token_reused");
        Check(owner.Current == null && owner.PresentationRevision == revision,
            "end_clears_reservation_not_presentation_clock");
        var next = new Ticket(7);
        owner.ReserveCaptured(next);
        long nextRevision = owner.BeginPresentation();
        Check(!owner.IsPresentationCurrent(revision) && owner.IsPresentationCurrent(nextRevision),
            "new_admission_retires_old_presentation");
        finish.SetResult(true);
        await oldRequest.ConfigureAwait(false);
        Check(owner.Owns(next), "late_finally_cannot_release_new_ticket");
        owner.Release(first);
        owner.Release(null);
        Check(owner.Owns(next), "duplicate_old_release_and_null_leave_new_ticket");
        var independent = new NativeConversationAdmissionOwner<Ticket>();
        independent.EndConversation();
        independent.BeginPresentation();
        independent.Release(next);
        Check(owner.Owns(next), "other_host_cannot_release_ticket");
        owner.Release(next);
        Check(owner.Current == null && owner.IsPresentationCurrent(nextRevision), "new_request_can_release_itself");
        Console.WriteLine($"NativeTicket PASS={_checks} FAIL=0 realOwner=true forcedYield=true gameObjects=NONE LIVE=NOT_RUN");
    }
}
