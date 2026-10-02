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
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy proactive speaker eligibility smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyEligibility()
    {
        Test.True(WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate()),
            "baseline speaker must be eligible");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(heroExists: false)),
            "missing hero must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(playerKingdomExists: false)),
            "missing player kingdom must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(playerKingdomIsEliminated: true)),
            "eliminated player kingdom must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(clanExists: false)),
            "missing clan must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(clanBelongsToPlayerKingdom: false)),
            "speaker outside the player kingdom must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(isPlayerClan: true)),
            "player clan speaker must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(isUnderMercenaryService: true)),
            "clan under mercenary service must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(isClanTypeMercenary: true)),
            "mercenary clan type must be rejected");
        Test.True(!WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(Candidate(isLord: false)),
            "non-lord speaker must be rejected");
    }

    private static void VerifySourceBoundary()
    {
        string rules = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyProactiveSpeakerEligibilityRules.cs"),
            Encoding.UTF8);
        string behavior = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"),
            Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string method = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyProactiveDiscussionApplication.cs"), Encoding.UTF8);
        string adapter = File.ReadAllText(FindRepositoryFile(
            "src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs"), Encoding.UTF8);

        Test.True(!rules.Contains("using TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("System.Collections", StringComparison.Ordinal)
                  && !rules.Contains("WorldDiplomacyDocument", StringComparison.Ordinal),
            "pure speaker rule must not reference TaleWorlds, collections, or persisted documents");
        Test.True(method.Contains("WorldDiplomacyProactiveSpeakerCandidate speaker", StringComparison.Ordinal)
                  && method.Contains("WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(speaker)", StringComparison.Ordinal)
                  && behavior.Contains("internal static bool TryCaptureProactiveSpeaker(", StringComparison.Ordinal)
                  && adapter.Contains("WorldDiplomacyProactiveDiscussionApplication.TryBuild(Proactive, heroId", StringComparison.Ordinal)
                  && !behavior.Contains("private bool TryBuildProactiveDiscussion(Hero hero", StringComparison.Ordinal),
            "Behavior captures scalar facts while Application owns speaker admission");
        Test.True(!method.Contains("hero == null || playerKingdom == null", StringComparison.Ordinal)
                  && !method.Contains("clan.IsUnderMercenaryService || clan.IsClanTypeMercenary", StringComparison.Ordinal),
            "replaced inline eligibility conjunction must be removed");
        int eligibility = method.IndexOf(
            "WorldDiplomacyProactiveSpeakerEligibilityRules.IsEligible(speaker)",
            StringComparison.Ordinal);
        int knowledgeLookup = method.IndexOf("source.TryCaptureDocuments", StringComparison.Ordinal);
        Test.True(eligibility >= 0 && knowledgeLookup > eligibility,
            "speaker eligibility must short-circuit before known-document lookup");
        Test.True(!rules.Contains("new ", StringComparison.Ordinal),
            "pure scalar eligibility rule must not allocate");
    }

    private static WorldDiplomacyProactiveSpeakerCandidate Candidate(
        bool heroExists = true,
        bool playerKingdomExists = true,
        bool playerKingdomIsEliminated = false,
        bool clanExists = true,
        bool clanBelongsToPlayerKingdom = true,
        bool isPlayerClan = false,
        bool isUnderMercenaryService = false,
        bool isClanTypeMercenary = false,
        bool isLord = true)
    {
        return new WorldDiplomacyProactiveSpeakerCandidate(
            heroExists,
            playerKingdomExists,
            playerKingdomIsEliminated,
            clanExists,
            clanBelongsToPlayerKingdom,
            isPlayerClan,
            isUnderMercenaryService,
            isClanTypeMercenary,
            isLord);
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
