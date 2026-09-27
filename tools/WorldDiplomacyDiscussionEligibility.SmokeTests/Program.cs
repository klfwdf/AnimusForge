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
    private sealed class Source : IWorldDiplomacyDiscussionSource
    {
        internal WorldDiplomacyDiscussionCandidate Candidate;
        internal bool Available = true;
        internal bool Known;
        internal bool Throw;
        internal int KnowledgeReads;
        public bool TryCaptureRepresentative(string heroId, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId)
        {
            if (Throw) throw new InvalidOperationException("capture failed");
            candidate = Candidate;
            kingdomId = "kingdom";
            return Available;
        }
        public bool HasKnownDocument(string heroId, string kingdomId)
        {
            KnowledgeReads++;
            return Known;
        }
    }

    private static int Main()
    {
        VerifyRepresentativeEligibility();
        VerifyKnownDocumentEligibility();
        VerifyApplicationAdmission();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy discussion eligibility smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRepresentativeEligibility()
    {
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(default),
            "default candidate must be ineligible");
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: false, kingdom: true, eliminated: false, lord: true, rulingLeader: false)),
            "missing hero must be ineligible");
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: true, kingdom: false, eliminated: false, lord: true, rulingLeader: false)),
            "missing kingdom must be ineligible");
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: true, kingdom: true, eliminated: true, lord: true, rulingLeader: false)),
            "eliminated kingdom must be ineligible");
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: true, kingdom: true, eliminated: false, lord: false, rulingLeader: false)),
            "ordinary non-lord must be ineligible");
        Test.True(WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: true, kingdom: true, eliminated: false, lord: true, rulingLeader: false)),
            "lord must be eligible");
        Test.True(WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(
            Candidate(hero: true, kingdom: true, eliminated: false, lord: false, rulingLeader: true)),
            "ruling leader must be eligible even when the live lord flag is false");
    }

    private static void VerifyKnownDocumentEligibility()
    {
        WorldDiplomacyDiscussionCandidate lord =
            Candidate(hero: true, kingdom: true, eliminated: false, lord: true, rulingLeader: false);
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.CanDiscuss(lord, hasKnownDocument: false),
            "eligible lord without known documents must not discuss diplomacy");
        Test.True(WorldDiplomacyDiscussionEligibilityRules.CanDiscuss(lord, hasKnownDocument: true),
            "eligible lord with a known document may discuss diplomacy");
        Test.True(!WorldDiplomacyDiscussionEligibilityRules.CanDiscuss(
                Candidate(hero: true, kingdom: true, eliminated: true, lord: true, rulingLeader: false),
                hasKnownDocument: true),
            "known documents must not override representative eligibility");
    }

    private static void VerifySourceBoundary()
    {
        string rules = File.ReadAllText(
            FindRepositoryFile("WorldDiplomacyDiscussionEligibilityRules.cs"),
            Encoding.UTF8);
        string behavior = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"),
            Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string snapshot = ExtractSection(
            behavior,
            "internal static bool TryCaptureDiscussionCandidate(",
            "private bool TryBuildProactiveDiscussion(");
        string application = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDiscussionApplication.cs"), Encoding.UTF8);
        string adapter = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs"), Encoding.UTF8);

        Test.True(!rules.Contains("using TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds.CampaignSystem", StringComparison.Ordinal),
            "pure eligibility rules must not reference live TaleWorlds types");
        Test.True(snapshot.Contains("new WorldDiplomacyDiscussionCandidate(", StringComparison.Ordinal)
                  && !snapshot.Contains("WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(", StringComparison.Ordinal)
                  && adapter.Contains("WorldDiplomacyDiscussionApplication.CanDiscuss(Discussion, heroId)", StringComparison.Ordinal)
                  && !adapter.Contains("WorldDiplomacyBehavior.CanDiscussWorldDiplomacyForExternal(", StringComparison.Ordinal)
                  && behavior.Contains("DiplomacyModuleServices.World.CanDiscuss(hero?.StringId)", StringComparison.Ordinal)
                  && !behavior.Contains("private bool CanDiscussWorldDiplomacy(Hero hero)", StringComparison.Ordinal),
            "Behavior captures facts while the real module caller enters Application");
        int representativeCheck = application.IndexOf(
            "WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(candidate)",
            StringComparison.Ordinal);
        int knowledgeLookup = application.IndexOf("source.HasKnownDocument(heroId, kingdomId)", StringComparison.Ordinal);
        Test.True(representativeCheck >= 0 && knowledgeLookup > representativeCheck,
            "Application must short-circuit before the knowledge snapshot");
    }

    private static void VerifyApplicationAdmission()
    {
        Source source = new() { Candidate = Candidate(true, true, false, true, false), Known = true };
        Test.True(WorldDiplomacyDiscussionApplication.CanDiscuss(source, "hero"), "known eligible lord can discuss");
        Test.True(source.KnowledgeReads == 1, "eligible representative reads knowledge once");
        source.Candidate = Candidate(true, true, false, false, false);
        Test.True(!WorldDiplomacyDiscussionApplication.CanDiscuss(source, "hero") && source.KnowledgeReads == 1,
            "ineligible representative skips knowledge query");
        source.Available = false;
        Test.True(!WorldDiplomacyDiscussionApplication.CanDiscuss(source, "hero") && source.KnowledgeReads == 1,
            "missing campaign owner skips knowledge query");
        source.Available = true;
        source.Throw = true;
        Test.True(!WorldDiplomacyDiscussionApplication.CanDiscuss(source, "hero"),
            "capture failure retains false result");
    }

    private static WorldDiplomacyDiscussionCandidate Candidate(
        bool hero,
        bool kingdom,
        bool eliminated,
        bool lord,
        bool rulingLeader)
    {
        return new WorldDiplomacyDiscussionCandidate(hero, kingdom, eliminated, lord, rulingLeader);
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
