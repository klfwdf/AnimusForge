using AnimusForge;
using Newtonsoft.Json.Linq;

internal static class RoundBoundaryReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage
        {
            ActiveRound = new() { RoundId = "r", State = "active" },
            ActiveExchange = new() { State = "active" }, ForcedWarToggleWasEnabled = true
        };
        storage.SuspendedExchanges.Add(new()); storage.Jobs.Add(new());
        storage.WarPressure.Add(new() { IsEscalationArmed = true });
        storage.WarPressure.Add(null);
        bool disabled = false, sanitized = true;
        var exchange = storage.ActiveExchange;
        var events = new List<string>();
        WorldDiplomacyRoundApplication.Disable(storage, ref disabled, ref sanitized, () => 42,
            reason => { Test.True(disabled && storage.Jobs.Count == 1, "disable flag precedes close and queue cleanup"); events.Add(reason); storage.ActiveRound = null; },
            day => { Test.True(storage.Jobs.Count == 0 && storage.ActiveExchange == null && day == 42 && sanitized, "queue cleanup precedes scheduling and native reset"); events.Add("schedule"); });
        Test.True(disabled && !sanitized && events.SequenceEqual(new[] { "closed_disabled", "schedule" })
            && exchange.State == "closed_disabled" && exchange.CompletedDay == 42
            && storage.SuspendedExchanges.Count == 0 && !storage.WarPressure[0].IsEscalationArmed && !storage.ForcedWarToggleWasEnabled,
            "disabled state closes canonical exchange and disarms pressure in original order");

        var round = new WorldDiplomacyRound { RoundId = "r", State = "active" };
        storage.ActiveRound = round;
        var root = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", Title = "title", PlannedKingdomIds = new() { "b", "a" } };
        foreach (string variant in new[] { "missing", "stale", "closed", "planned", "active" })
        {
            round.State = variant == "closed" ? "closed" : "active";
            round.RelayPlanned = variant == "planned";
            events.Clear();
            WorldDiplomacyRoundApplication.CommitEmbeddedPlan(storage,
                variant == "missing" ? null : variant == "stale" ? new WorldDiplomacyRound { State = "active" } : round, root,
                (id, r) => { events.Add("candidates"); return new() { "a", "b" }; },
                (job, json) =>
                {
                    events.Add("commit"); var parsed = JObject.Parse(json);
                    Test.True(job.RoundId == "r" && job.DocumentId == "d" && job.AuthorKingdomId == "a"
                        && job.CandidateKingdomIds.SequenceEqual(new[] { "a", "b" })
                        && parsed["topic"]?.ToString() == "title"
                        && parsed["selected_kingdom_ids"].Values<string>().SequenceEqual(new[] { "b", "a" }), "embedded plan preserves candidate and selected order with exact source");
                }, _ => events.Add("log"));
            Test.True(variant == "active" ? events.SequenceEqual(new[] { "candidates", "commit", "log" }) : events.Count == 0,
                "invalid embedded plan never captures candidates or commits: " + variant);
        }
    }
}
