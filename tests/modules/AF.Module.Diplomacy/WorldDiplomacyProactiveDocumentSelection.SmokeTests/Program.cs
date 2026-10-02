using System.Text;
using AnimusForge;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        VerifyEligibility();
        VerifyPriority();
        VerifyUrgency();
        VerifyRelatedDocumentSelection();
        VerifyStableKey();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy proactive document selection smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyEligibility()
    {
        WorldDiplomacyProactiveDocumentCandidate eligible = Candidate();
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.MeetsBaseEligibility(eligible, earliestDay: 10),
            "baseline candidate must pass the cheap base gates");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(eligible, earliestDay: 10),
            "baseline candidate must be eligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(Candidate(ready: false), 10),
            "unpublished document must be ineligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(Candidate(compressed: true), 10),
            "compressed document must be ineligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(Candidate(day: 9), 10),
            "document before the seven-day boundary must be ineligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(Candidate(known: false), 10),
            "unknown document must be ineligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: false, targeted: false, addressed: false, mentioned: false, major: false), 10),
            "irrelevant document must be ineligible");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: true, targeted: false), 10),
            "player-kingdom authored document must be relevant");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: false, targeted: true), 10),
            "directly targeted document must be relevant");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: false, targeted: false, addressed: true), 10),
            "addressed document must be relevant");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: false, targeted: false, mentioned: true), 10),
            "mentioned document must be relevant");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligible(
                Candidate(authored: false, targeted: false, major: true), 10),
            "major document must be relevant");
    }

    private static void VerifyPriority()
    {
        WorldDiplomacyProactiveDocumentCandidate baseline = Candidate(day: 12, ticks: 100);
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                Candidate(mechanical: true, day: 1, ticks: 1), baseline),
            "mechanical result must have first priority");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                baseline, Candidate(mechanical: true, day: 1, ticks: 1)),
            "recency must not override mechanical-result priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                Candidate(targeted: true, authored: false, day: 1, ticks: 1), baseline),
            "direct target must have second priority");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                baseline, Candidate(targeted: true, authored: false, day: 1, ticks: 1)),
            "recency must not override direct-target priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                Candidate(day: 13, ticks: 1), baseline),
            "newer day must have third priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                Candidate(day: 12, ticks: 101), baseline),
            "newer created ticks must have fourth priority");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter(
                Candidate(day: 12, ticks: 100), baseline),
            "exact tie must retain the first source document");
    }

    private static void VerifyUrgency()
    {
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency(
                hasMechanicalResult: true, isTargetingPlayerKingdom: true, isMajor: true) == 82f,
            "mechanical result must have first urgency priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency(
                hasMechanicalResult: false, isTargetingPlayerKingdom: true, isMajor: true) == 74f,
            "direct player-kingdom target must have second urgency priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency(
                hasMechanicalResult: false, isTargetingPlayerKingdom: false, isMajor: true) == 64f,
            "major diplomacy document must have third urgency priority");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency(
                hasMechanicalResult: false, isTargetingPlayerKingdom: false, isMajor: false) == 58f,
            "ordinary relevant document must use fallback urgency");
    }

    private static void VerifyRelatedDocumentSelection()
    {
        WorldDiplomacyProactiveRelatedDocumentCandidate baseline = RelatedCandidate(day: 10, ticks: 100);
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.IsEligibleRelatedDocument(baseline),
            "known uncompressed same-round document must be eligible");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligibleRelatedDocument(
                RelatedCandidate(compressed: true)),
            "compressed related document must be rejected");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligibleRelatedDocument(
                RelatedCandidate(known: false)),
            "unknown related document must be rejected");
        Test.True(!WorldDiplomacyProactiveDocumentSelectionRules.IsEligibleRelatedDocument(
                RelatedCandidate(sameRound: false)),
            "different-round document must be rejected");
        Test.True(RelatedInsertionIndex(RelatedCandidate(day: 10), count: 0) == 0,
            "first related document must fill the first slot");
        Test.True(RelatedInsertionIndex(RelatedCandidate(day: 11), count: 1, first: baseline) == 0,
            "newer day must insert before the incumbent");
        Test.True(RelatedInsertionIndex(RelatedCandidate(day: 10, ticks: 101), count: 1, first: baseline) == 0,
            "newer ticks on the same day must insert before the incumbent");
        Test.True(RelatedInsertionIndex(RelatedCandidate(day: 9), count: 1, first: baseline) == 1,
            "older document must fill the next free slot");
        Test.True(RelatedInsertionIndex(
                RelatedCandidate(day: 11),
                count: 3,
                first: RelatedCandidate(day: 12),
                second: RelatedCandidate(day: 10),
                third: RelatedCandidate(day: 8)) == 1,
            "candidate between first and second must insert into the middle slot");
        Test.True(RelatedInsertionIndex(
                RelatedCandidate(day: 9),
                count: 3,
                first: RelatedCandidate(day: 12),
                second: RelatedCandidate(day: 10),
                third: RelatedCandidate(day: 8)) == 2,
            "candidate newer only than third must insert into the final slot");
        Test.True(RelatedInsertionIndex(
                RelatedCandidate(day: 10, ticks: 100), count: 2, first: baseline, second: baseline) == 2,
            "equal documents must preserve source order while a slot remains");
        Test.True(RelatedInsertionIndex(
                RelatedCandidate(day: 10, ticks: 100), count: 3, first: baseline, second: baseline, third: baseline) == -1,
            "fourth equal document must not displace an earlier source item");
        Test.True(RelatedInsertionIndex(RelatedCandidate(day: 10), count: 4) == -1,
            "invalid retained count must be rejected");
    }

    private static void VerifyStableKey()
    {
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
                " round-1 ", " document-1 ") == "world_diplomacy:round-1: document-1 ",
            "round ID must be the trimmed scope while the document suffix remains unchanged");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
                " ", " document-1 ") == "world_diplomacy:document-1: document-1 ",
            "blank round ID must fall back to the trimmed document ID scope");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
                null!, "document-1") == "world_diplomacy:document-1:document-1",
            "missing round ID must fall back to the document ID");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
                "round-1", null!) == "world_diplomacy:round-1:",
            "missing document suffix must preserve the existing empty suffix");
        Test.True(WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey(
                null!, null!) == "world_diplomacy::",
            "missing IDs must preserve the existing empty key shape");
    }

    private static void VerifySourceBoundary()
    {
        string rules = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyProactiveDocumentSelectionRules.cs"),
            Encoding.UTF8);
        string behavior = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"),
            Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string method = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyProactiveDiscussionApplication.cs"), Encoding.UTF8);
        Test.True(!behavior.Contains("private bool TryBuildProactiveDiscussion(Hero hero", StringComparison.Ordinal),
            "legacy Behavior must not retain the complete proactive use case");
        int selectionStart = method.IndexOf("int earliestDay", StringComparison.Ordinal);
        int selectionEnd = method.IndexOf("if (selected == null)", selectionStart, StringComparison.Ordinal);
        Test.True(selectionStart >= 0 && selectionEnd > selectionStart,
            "selection source block must be present");
        string selection = method.Substring(selectionStart, selectionEnd - selectionStart);

        Test.True(!rules.Contains("using TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("WorldDiplomacyDocument", StringComparison.Ordinal),
            "pure selection rules must not reference live or persisted types");
        Test.True(selection.Contains("foreach (WorldDiplomacyDocument document", StringComparison.Ordinal)
                  && selection.Contains("WorldDiplomacyProactiveDocumentSelectionRules.MeetsBaseEligibility", StringComparison.Ordinal)
                  && selection.Contains("WorldDiplomacyProactiveDocumentSelectionRules.IsEligible", StringComparison.Ordinal)
                  && selection.Contains("WorldDiplomacyProactiveDocumentSelectionRules.IsStrictlyBetter", StringComparison.Ordinal),
            "behavior must use the streaming pure selection rules");
        Test.True(!selection.Contains("OrderByDescending", StringComparison.Ordinal)
                  && !selection.Contains("ThenByDescending", StringComparison.Ordinal)
                  && !selection.Contains("FirstOrDefault", StringComparison.Ordinal)
                  && !selection.Contains("ToList", StringComparison.Ordinal),
            "replaced selection sort and materialization must be removed");
        Test.True(selection.Contains("selected = document;", StringComparison.Ordinal)
                  && selection.Contains("selectedCandidate = candidate;", StringComparison.Ordinal),
            "strict improvement must update both selected document and snapshot");
        int baseGate = selection.IndexOf(
            "WorldDiplomacyProactiveDocumentSelectionRules.MeetsBaseEligibility",
            StringComparison.Ordinal);
        int majorCheck = selection.IndexOf("IsMajorDiplomaticDocument(document)", StringComparison.Ordinal);
        Test.True(baseGate >= 0 && majorCheck > baseGate,
            "cheap publication/age/knowledge gates must short-circuit before major-document inspection");
        Test.True(method.Contains("WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency", StringComparison.Ordinal)
                  && method.Contains("selectedCandidate.HasMechanicalResult", StringComparison.Ordinal)
                  && method.Contains("selectedCandidate.IsTargetingPlayerKingdom", StringComparison.Ordinal),
            "behavior must calculate urgency from the selected immutable snapshot");
        Test.True(!method.Contains("urgency = !string.IsNullOrWhiteSpace(selected.MechanicalResult)", StringComparison.Ordinal),
            "replaced inline urgency expression must be removed");
        int urgencyMajorGuard = method.IndexOf("bool selectedIsMajorForUrgency", StringComparison.Ordinal);
        int urgencyCall = method.IndexOf(
            "WorldDiplomacyProactiveDocumentSelectionRules.CalculateUrgency",
            StringComparison.Ordinal);
        Test.True(urgencyMajorGuard >= 0 && urgencyCall > urgencyMajorGuard,
            "major-document inspection must remain guarded before pure urgency calculation");
        Test.True(method.Contains("WorldDiplomacyProactiveDocumentSelectionRules.BuildDiscussionStableKey", StringComparison.Ordinal),
            "behavior must delegate stable-key construction to the pure rule");
        Test.True(!method.Contains(
                "\"world_diplomacy:\" + FirstNonEmpty(selected.RoundId, selected.DocumentId)",
                StringComparison.Ordinal),
            "replaced inline stable-key construction must be removed");
        int relatedStart = method.IndexOf("WorldDiplomacyDocument relatedFirst", StringComparison.Ordinal);
        int relatedEnd = method.IndexOf("StringBuilder sb", relatedStart, StringComparison.Ordinal);
        Test.True(relatedStart >= 0 && relatedEnd > relatedStart,
            "related-document streaming block must be present");
        string relatedSelection = method.Substring(relatedStart, relatedEnd - relatedStart);
        Test.True(relatedSelection.Contains("foreach (WorldDiplomacyDocument document", StringComparison.Ordinal)
                  && relatedSelection.Contains("IsEligibleRelatedDocument", StringComparison.Ordinal)
                  && relatedSelection.Contains("GetRelatedDocumentInsertionIndex", StringComparison.Ordinal),
            "behavior must delegate bounded related-document selection to pure rules");
        Test.True(!relatedSelection.Contains("OrderByDescending", StringComparison.Ordinal)
                  && !relatedSelection.Contains("ThenByDescending", StringComparison.Ordinal)
                  && !relatedSelection.Contains("Take(3)", StringComparison.Ordinal)
                  && !relatedSelection.Contains("ToList", StringComparison.Ordinal),
            "replaced related-document sorting and list materialization must be removed");
        Test.True(method.Contains("AppendProactiveDiscussionDocument(sb, relatedFirst, source.FormatDate)", StringComparison.Ordinal)
                  && method.Contains("AppendProactiveDiscussionDocument(sb, relatedSecond, source.FormatDate)", StringComparison.Ordinal)
                  && method.Contains("AppendProactiveDiscussionDocument(sb, relatedThird, source.FormatDate)", StringComparison.Ordinal),
            "retained documents must be appended in newest-first slot order");
    }

    private static WorldDiplomacyProactiveDocumentCandidate Candidate(
        bool ready = true,
        bool compressed = false,
        int day = 12,
        long ticks = 100,
        bool known = true,
        bool authored = true,
        bool targeted = false,
        bool addressed = false,
        bool mentioned = false,
        bool major = false,
        bool mechanical = false)
    {
        return new WorldDiplomacyProactiveDocumentCandidate(
            ready, compressed, day, ticks, known, authored, targeted, addressed, mentioned, major, mechanical);
    }

    private static WorldDiplomacyProactiveRelatedDocumentCandidate RelatedCandidate(
        bool compressed = false,
        bool known = true,
        bool sameRound = true,
        int day = 10,
        long ticks = 100)
    {
        return new WorldDiplomacyProactiveRelatedDocumentCandidate(compressed, known, sameRound, day, ticks);
    }

    private static int RelatedInsertionIndex(
        WorldDiplomacyProactiveRelatedDocumentCandidate candidate,
        int count,
        WorldDiplomacyProactiveRelatedDocumentCandidate first = default,
        WorldDiplomacyProactiveRelatedDocumentCandidate second = default,
        WorldDiplomacyProactiveRelatedDocumentCandidate third = default)
    {
        return WorldDiplomacyProactiveDocumentSelectionRules.GetRelatedDocumentInsertionIndex(
            candidate, count, first, second, third);
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Could not extract expected source section.");
        }
        return source.Substring(start, end - start);
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}
