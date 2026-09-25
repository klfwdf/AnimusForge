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
        VerifyRepresentativeEligibility();
        VerifyKnownDocumentEligibility();
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
        string method = ExtractSection(
            behavior,
            "private bool CanDiscussWorldDiplomacy(Hero hero)",
            "private bool TryBuildProactiveDiscussion(");

        Test.True(!rules.Contains("using TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds.CampaignSystem", StringComparison.Ordinal),
            "pure eligibility rules must not reference live TaleWorlds types");
        Test.True(method.Contains("new WorldDiplomacyDiscussionCandidate(", StringComparison.Ordinal)
                  && method.Contains("WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(candidate)", StringComparison.Ordinal)
                  && method.Contains("WorldDiplomacyDiscussionEligibilityRules.CanDiscuss(candidate, hasKnownDocument)", StringComparison.Ordinal),
            "behavior must map live state and delegate both eligibility decisions");
        Test.True(!method.Contains("if (hero == null || hero.Clan?.Kingdom == null", StringComparison.Ordinal)
                  && !method.Contains("if (!hero.IsLord && hero != hero.Clan.Kingdom.RulingClan?.Leader)", StringComparison.Ordinal),
            "replaced inline eligibility algorithm must be removed");
        int representativeCheck = method.IndexOf(
            "WorldDiplomacyDiscussionEligibilityRules.IsEligibleRepresentative(candidate)",
            StringComparison.Ordinal);
        int knowledgeLookup = method.IndexOf("GetKnownDocumentIdsForHero(", StringComparison.Ordinal);
        Test.True(representativeCheck >= 0 && knowledgeLookup > representativeCheck,
            "representative eligibility must short-circuit before knowledge lookup");
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
