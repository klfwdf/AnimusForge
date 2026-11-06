using AnimusForge;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json;

// Real save round-trip, Loaded/Session lifecycle and silence scheduler;
// game world and transport leaves are controlled by the existing host.
internal static partial class EventConvergenceReplay
{
    private struct SilenceLoadSource : IWorldDiplomacyLifecycleSource
    {
        internal WorldDiplomacyOrchestration Owner;
        public bool StartAtPeace => false;
        public void ResetTransientRuntime(string reason) => Owner.ResetRuntimeState();
    }

    private static void ReloadSilence(WorldDiplomacyOrchestration owner)
    {
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(owner.CurrentStorage))!;
        owner.ReplaceStorage(saved);
        var source = new SilenceLoadSource { Owner = owner };
        WorldDiplomacyLifecycleApplication.Run(WorldDiplomacyLifecycleEvent.Loaded, owner, ref source);
        WorldDiplomacyLifecycleApplication.Run(WorldDiplomacyLifecycleEvent.Session, owner, ref source);
    }

    internal static void SilenceLoad()
    {
        foreach (string scenario in new[] { "noticed", "notice_after_load", "missing_wait_time", "adjusted", "unnoticed", "answered", "closed", "withdrawn", "suppressed", "paused" })
        {
            var (host, owner, round) = Fixture("p", "b");
            owner.CurrentStorage.DiplomacyNotificationStateSchemaVersion = WorldDiplomacyNotificationStateMigration.CurrentSchemaVersion;
            var document = Publish(owner, round, "pending-player-reply", "b", "p", response: true);
            document.PlayerLifecycleClassified = true;
            var wait = round.PlayerNegotiationWaits.Single();
            double noticeHour = host.CurrentCampaignHours();
            if (scenario is not ("unnoticed" or "notice_after_load"))
            {
                document.ActualPlayerNoticeHour = noticeHour;
                document.FormalNoticeShown = true;
                if (scenario != "missing_wait_time") owner.RecordActualPlayerNotice(document, noticeHour);
            }
            if (scenario == "answered") wait.AnswerDocumentId = "published-player-answer";
            if (scenario == "closed") wait.ClosureDocumentId = "published-closure";
            if (scenario == "withdrawn") wait.RequiresAnswer = false;
            if (scenario == "suppressed") document.LegacyNoticeSuppressed = true;
            if (scenario == "paused") owner.CurrentStorage.EventsPausedAtHour = noticeHour;
            if (scenario == "adjusted") wait.NotifiedHour += 24;
            Call(owner, "BeginEventEnding", round, "");
            string roundId = round.RoundId;
            ReloadSilence(owner);
            round = owner.ResolveRound(roundId);
            document = owner.ResolveDocument(document.DocumentId);
            wait = round.PlayerNegotiationWaits.Single();
            if (scenario == "notice_after_load")
            {
                document.ActualPlayerNoticeHour = noticeHour;
                document.FormalNoticeShown = true;
                owner.RecordActualPlayerNotice(document, noticeHour);
            }
            bool pending = scenario is "noticed" or "notice_after_load" or "missing_wait_time" or "adjusted" or "paused";
            if (pending) Test.True(wait.NotifiedHour == noticeHour + (scenario == "adjusted" ? 24 : 0),
                scenario + ": saved deadline survives load, actual notice fills only missing times");
            if (scenario == "unnoticed") Test.True(wait.NotifiedHour < 0, "unshown notice never starts a retrospective deadline");
            host.CurrentDayValue += 2;
            Call(owner, "ProcessDuePlayerSilence");
            Test.True(!owner.CurrentStorage.Jobs.Any(x => !string.IsNullOrEmpty(x.SilenceSourceDocumentId)), scenario + ": no premature or completed-work timeout");
            host.CurrentDayValue += 3;
            if (scenario == "paused")
            {
                Call(owner, "ResumeDiplomacyEvents");
                Test.True(wait.NotifiedHour == host.CurrentCampaignHours(), "disabled time does not consume reply window");
                Call(owner, "ProcessDuePlayerSilence");
                Test.True(!owner.CurrentStorage.Jobs.Any(x => !string.IsNullOrEmpty(x.SilenceSourceDocumentId)), "resume preserves full remaining reply window");
                host.CurrentDayValue += 3;
            }
            Call(owner, "ProcessDuePlayerSilence");
            Test.True(owner.CurrentStorage.Jobs.Count(x => x.SilenceSourceDocumentId == document.DocumentId) == (pending ? 1 : 0),
                scenario + ": real scheduler creates exactly one due closure, never resurrects completed waits; "
                + JsonConvert.SerializeObject(new { wait, round.State, round.TechnicalHoldReason, round.Participants,
                    jobs = owner.CurrentStorage.Jobs.Select(x => new { x.Kind, x.SourceDocumentId, x.SilenceSourceDocumentId }), host.Logs }));
            if (pending)
            {
                ReloadSilence(owner);
                Call(owner, "ProcessDuePlayerSilence");
                Call(owner, "ProcessDuePlayerSilence");
                Test.True(owner.CurrentStorage.Jobs.Count(x => x.SilenceSourceDocumentId == document.DocumentId) == 1,
                    scenario + ": repeated load keeps the existing closure job without duplication");
            }
        }
    }
}
