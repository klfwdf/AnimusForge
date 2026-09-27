using AnimusForge;

internal static class ProactiveDiscussionApplicationReplay
{
    private sealed class Source : IWorldDiplomacyProactiveDiscussionSource
    {
        internal bool Available = true;
        internal bool Throw;
        internal int DocumentReads;
        internal WorldDiplomacyProactiveSpeakerCandidate Speaker = EligibleSpeaker();
        internal readonly List<WorldDiplomacyDocument> Documents = new();
        internal readonly HashSet<string> KnownIds = new(StringComparer.OrdinalIgnoreCase);

        public bool TryCaptureSpeaker(string heroId, out WorldDiplomacyProactiveSpeakerCandidate candidate,
            out string playerKingdomId)
        {
            candidate = Speaker;
            playerKingdomId = "player";
            return Available;
        }

        public bool TryCaptureDocuments(string heroId, string playerKingdomId,
            out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay)
        {
            DocumentReads++;
            if (Throw) throw new InvalidOperationException("snapshot failure");
            documents = Documents;
            knownIds = KnownIds;
            currentDay = 12;
            return true;
        }

        public string GetPlayerKingdomName(string playerKingdomId) => "玩家国";
        public string FormatDate(int day) => "第" + day + "天";
    }

    private static WorldDiplomacyProactiveSpeakerCandidate EligibleSpeaker() => new(
        true, true, false, true, true, false, false, false, true);

    private static WorldDiplomacyDocument Document(string id, int day, string result = "") => new()
    {
        DocumentId = id, RoundId = "round", Day = day, CreatedUtcTicks = day,
        AuthorKingdomId = "other", TargetKingdomId = "player", Title = id,
        Body = "正文" + id, IsReadyForPublication = true, MechanicalResult = result
    };

    internal static void Run()
    {
        Source source = new();
        source.Documents.Add(Document("older", 10));
        source.Documents.Add(Document("selected", 9, "确认结果"));
        source.Documents.Add(Document("unknown", 12));
        source.KnownIds.Add("older");
        source.KnownIds.Add("selected");
        Test.True(WorldDiplomacyProactiveDiscussionApplication.TryBuild(source, "speaker",
            out string key, out string fact, out float urgency), "eligible speaker selects known recent document");
        Test.True(key == "world_diplomacy:round:selected" && urgency == 82f,
            "mechanical result outranks recency and controls urgency");
        Test.True(fact.Contains("玩家国") && fact.Contains("正文older")
            && fact.Contains("正文selected") && !fact.Contains("正文unknown"),
            "fact includes only newest known documents in the selected round");
        int reads = source.DocumentReads;
        source.Speaker = default;
        Test.True(!WorldDiplomacyProactiveDiscussionApplication.TryBuild(source, "speaker",
            out _, out _, out _) && source.DocumentReads == reads,
            "invalid speaker skips document collection");
        source.Speaker = EligibleSpeaker();
        source.Throw = true;
        Test.True(!WorldDiplomacyProactiveDiscussionApplication.TryBuild(source, "speaker",
            out key, out fact, out urgency) && key == "" && fact == "" && urgency == 0f,
            "snapshot failure clears all outputs");
        source.Throw = false;
        source.Available = false;
        reads = source.DocumentReads;
        Test.True(!WorldDiplomacyProactiveDiscussionApplication.TryBuild(source, "speaker",
            out _, out _, out _) && source.DocumentReads == reads,
            "missing owner skips document collection");
    }
}
