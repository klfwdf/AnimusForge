using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

// Actual submission and orchestration; campaign effects use the existing detached host.
internal static class ReplyShortcutReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        private readonly PromptWorldFixture _prompt = new();
        public override IWorldDiplomacyPromptWorld PromptWorld() => _prompt;
    }
    private sealed class World : IWorldDiplomacyPlayerWorld
    {
        internal WorldDiplomacyOrchestration Owner = null!;
        public WorldDiplomacyPlayerContext Player { get; set; } = new(7, "p", true, false, "p");
        public bool KingdomExists(string id) => true;
        public WorldDiplomacyDocument ResolveDocument(string id) => Owner.ResolveDocument(id);
        public WorldDiplomacyRound ResolveRound(string id) => Owner.ResolveRound(id);
        public int CurrentDay() => 10;
    }

    internal static void Run()
    {
        foreach (string state in new[] { "active", "closed", "missing" })
        foreach (bool shortcut in new[] { false, true })
        {
            var host = new Host();
            var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
            host.Owner = owner;
            host.PublishEnabled = true;
            var old = owner.EnsureActiveRound("a", "p", false);
            old.State = state;
            WorldDiplomacyStructureRules.EnsureRoundParticipant(old, "p", "active", mandatoryReply: true);
            var source = new WorldDiplomacyDocument
            {
                DocumentId = "source", RoundId = state == "missing" ? "missing-round" : old.RoundId,
                AuthorKingdomId = "a", TargetKingdomId = "p", IsReadyForPublication = true,
                HasReachedPlayerCourt = true, Intent = "propose_alliance", Body = "向玩家提议结盟。",
                AutomaticReplyDepth = 5
            };
            owner.CurrentStorage.Documents.Add(source);
            var world = new World { Owner = owner };
            var command = shortcut
                ? new WorldDiplomacyPlayerDocumentCommand("我想向b国提出贸易。", 7, "source", "stale-ui-round")
                : new WorldDiplomacyPlayerDocumentCommand("我想向b国提出贸易。", 7);
            int jobs = owner.CurrentStorage.Jobs.Count;
            Test.True(WorldDiplomacyPlayerApplication.Execute(world, command, owner).Contains("已经公开发布"),
                "both compose entries accept a ruler regardless of old round or independence");
            var published = owner.CurrentStorage.Documents.Single(x => x.IsPlayerAuthored);
            Test.True(owner.CurrentStorage.Documents.Count == 2 && published.IsReadyForPublication
                && owner.CurrentStorage.Jobs.Count == jobs + 1
                && owner.CurrentStorage.Jobs.Count(x => x.DocumentId == published.DocumentId && x.Kind == "analyze") == 1,
                "one submitted document produces one publication and one analysis");
            var provisional = owner.ResolveRound(published.RoundId);
            Test.True(provisional.RoundId != old.RoundId && provisional.DialogueArrangementId == "player_manual_declaration"
                && provisional.RootDocumentId == published.DocumentId && provisional.PlayerResponses.Count == 0,
                "button does not preselect the old event or create response work before analysis");
            Test.True(!published.IsResponse && published.AutomaticReplyDepth == 0
                && string.IsNullOrEmpty(published.TargetKingdomId) && published.Origin == "player"
                && published.SourceDocumentId == (shortcut ? "source" : ""),
                "source is the only entry-specific context, without forced target or response depth");
            Test.True(old.Participants.Single(x => x.KingdomId == "p").MandatoryReplyPending && old.State == state,
                "submission alone does not settle the old event or revive closed state");
            world.Player = new(7, "p", false, true, "p");
            Test.True(WorldDiplomacyPlayerApplication.Execute(world, command, owner).Contains("不再是王国统治者")
                && owner.CurrentStorage.Documents.Count == 2 && owner.CurrentStorage.Jobs.Count == jobs + 1,
                "vassal cannot submit from either entry after losing ruler authority");
        }

        var promptWorld = new PromptWorldFixture();
        var promptOrch = new PromptOrch(promptWorld);
        var draft = new WorldDiplomacyDocument
        {
            DocumentId = "draft", AuthorKingdomId = "p", SourceDocumentId = "source",
            IsPlayerAuthored = true, Body = "我想向b国提出贸易。"
        };
        string prompt = WorldDiplomacyPromptComposer.BuildAnalysisPrompt(promptWorld, promptOrch, draft);
        Test.True(prompt.Contains("背景公文ID：source") && prompt.Contains(promptWorld.Document.Body)
            && prompt.Contains("是否回应、回应对象与动作均以玩家正文为准")
            && prompt.Contains("不因入口恢复旧提案或强制加入原交涉") && prompt.Contains(draft.Body),
            "real analysis prompt carries original text as advisory context and keeps player prose authoritative");
    }
}
