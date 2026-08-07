using System.Text;

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
    private const string NaturalTradeProposal = "阿塞莱愿与南帝国就边地商路秩序展开协商，并派遣使节商谈具体条款。";

    private static int Main()
    {
        string sourcePath = FindRepositoryFile("WorldDiplomacyBehavior.cs");
        string source = File.ReadAllText(sourcePath, Encoding.UTF8);
        string generatedValidation = ExtractSection(
            source,
            "private bool TryGetGeneratedIntentLegalityViolation(",
            "private static bool ArePeaceTermsEquivalent(");

        Test.True(NaturalTradeProposal.Contains("商路", StringComparison.Ordinal), "fixture must describe the trade domain");
        Test.True(!new[] { "提议", "建议", "倡议", "邀请", "请求" }
            .Any(NaturalTradeProposal.Contains), "fixture must retain the natural wording that the old whitelist rejected");

        Test.True(!generatedValidation.Contains("TryGetPlayerVisibleIntentViolation", StringComparison.Ordinal),
            "AI-generated declarations must not use the player free-text intent recognizer");
        Test.True(!generatedValidation.Contains("visible_intent_mismatch", StringComparison.Ordinal),
            "AI-generated declarations must not be rejected because prose misses a keyword whitelist");
        Test.True(!generatedValidation.Contains("HasVisibleIntentDirectedAtTarget", StringComparison.Ordinal),
            "AI-generated declarations must not re-infer the structured target from literary prose");
        Test.True(!generatedValidation.Contains("LooksLikeMisaddressedThirdPartyOfferResponse", StringComparison.Ordinal),
            "AI-generated declarations must use structured offer ownership instead of prose inference");
        Test.True(!generatedValidation.Contains("LooksLikeExplicitPeaceNegotiationWithTarget", StringComparison.Ordinal),
            "AI-generated declarations must not acquire a different intent from prose keywords");

        foreach (string requiredGuard in new[]
        {
            "IsSupportedDiplomacyIntent",
            "CommitmentMatchesIntent",
            "TryGetDiplomaticStateViolation",
            "TryResolveOpenProposalFor",
            "offer_response_source_mismatch",
            "diplomatic_action_has_no_target",
            "TryGetPublicPeaceTermsDisclosureViolation"
        })
        {
            Test.True(generatedValidation.Contains(requiredGuard, StringComparison.Ordinal),
                "generated validation must retain hard guard: " + requiredGuard);
        }

        Test.True(source.Contains("TryGetPlayerVisibleIntentViolation", StringComparison.Ordinal),
            "player-authored free text must retain its separate safety boundary");
        Test.True(source.Contains("\"propose_trade\" => \"贸易申请\"", StringComparison.Ordinal),
            "structured trade intent must remain visible in the document UI");
        Test.True(source.Contains("\"declare_war\" => \"宣战告知\"", StringComparison.Ordinal),
            "structured war intent must remain visible in the document UI");
        Test.True(source.Contains("\"propose_alliance\" => \"同盟申请\"", StringComparison.Ordinal),
            "structured alliance intent must remain visible in the document UI");

        Console.WriteLine("World diplomacy intent-boundary smoke tests passed: " + Test.Assertions);
        return 0;
    }

    private static string FindRepositoryFile(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file.", fileName);
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Test.True(start >= 0, "missing start marker: " + startMarker);
        int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Test.True(end > start, "missing end marker: " + endMarker);
        return source.Substring(start, end - start);
    }
}
