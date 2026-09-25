using System.Text;
using AnimusForge;
using AnimusForge.Refactor.Persistence;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        VerifyNormalizationAndLegacyAliases();
        VerifyNullShapeAndRetentionBound();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy offer-cooldown persistence smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyNormalizationAndLegacyAliases()
    {
        WorldDiplomacyStorage storage = new WorldDiplomacyStorage
        {
            OfferCooldownStateSchemaVersion = 9,
            OfferCooldowns = new List<WorldDiplomacyOfferCooldown>
            {
                null,
                Cooldown("", "b", "trade", 8, "blank"),
                Cooldown("same", " SAME ", "trade", 8, "same"),
                Cooldown("a", "b", "unknown", 8, "unknown"),
                Cooldown("a", "b", "trade", -1, "negative"),
                Cooldown(" A ", "B", " propose_trade ", 2, " old "),
                Cooldown("a", " b ", "TRADE", 5, " newer "),
                Cooldown("A", "B", "trade", 5, " tie wins "),
                Cooldown("B", "A", "trade", 3, " reverse "),
                Cooldown("A", "B", "propose_alliance", 4, " alliance ")
            }
        };

        WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(storage);

        Test.True(storage.OfferCooldownStateSchemaVersion == 1,
            "normalization must stamp the current schema exactly as the existing save path did");
        Test.True(storage.OfferCooldowns.Count == 3,
            "invalid records and case-insensitive directed duplicates must be removed");
        WorldDiplomacyOfferCooldown trade = storage.OfferCooldowns[0];
        Test.True(trade.ProposerKingdomId == "A" && trade.TargetKingdomId == "B",
            "normalized keys must retain trimmed IDs from the selected directed key");
        Test.True(trade.Domain == "trade" && trade.LastFailedRoundDay == 5,
            "legacy trade aliases must normalize to the canonical token and newest day");
        Test.True(trade.SourceRoundId == "tie wins",
            "a later equal-day duplicate must retain the existing last-record-wins behavior");
        Test.True(storage.OfferCooldowns[1].Domain == "alliance"
                  && storage.OfferCooldowns[1].LastFailedRoundDay == 4,
            "alliance aliases must remain independent from trade cooldowns");
        Test.True(storage.OfferCooldowns[2].ProposerKingdomId == "B"
                  && storage.OfferCooldowns[2].TargetKingdomId == "A"
                  && storage.OfferCooldowns[2].LastFailedRoundDay == 3,
            "reverse offer direction must remain an independent persisted key");
        Test.True(storage.OfferCooldowns.Select(x => x.LastFailedRoundDay).SequenceEqual(new[] { 5, 4, 3 }),
            "normalized cooldowns must retain deterministic newest-first ordering");

        Test.True(WorldDiplomacyOfferCooldownStorageNormalizer.TryCreateKey(
                      trade,
                      out WorldDiplomacyOfferCooldownKey key)
                  && key.IsValid
                  && key.Domain == WorldDiplomacyOfferDomain.Trade,
            "runtime index rebuild must share the persistence parser and key validation");
        Test.True(WorldDiplomacyOfferCooldownStorageNormalizer.DomainToken(WorldDiplomacyOfferDomain.None) == "",
            "unsupported runtime domains must not acquire a persisted token");
    }

    private static void VerifyNullShapeAndRetentionBound()
    {
        WorldDiplomacyStorage empty = new WorldDiplomacyStorage { OfferCooldowns = null };
        WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(empty);
        Test.True(empty.OfferCooldowns != null && empty.OfferCooldowns.Count == 0,
            "normalization must repair a null legacy collection");

        WorldDiplomacyStorage storage = new WorldDiplomacyStorage();
        for (int index = 0; index < 2050; index++)
        {
            storage.OfferCooldowns.Add(Cooldown("source", "target-" + index, "trade", index, "round"));
        }

        WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(storage);

        Test.True(storage.OfferCooldowns.Count == WorldDiplomacyOfferCooldownStorageNormalizer.MaxStoredCooldowns,
            "normalization must retain at most the established 2048 cooldown records");
        Test.True(storage.OfferCooldowns[0].LastFailedRoundDay == 2049
                  && storage.OfferCooldowns[^1].LastFailedRoundDay == 2,
            "retention must keep the newest records after deterministic sorting");
    }

    private static void VerifySourceBoundary()
    {
        string behavior = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"), Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string persistence = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyOfferCooldownStorageNormalizer.cs"),
            Encoding.UTF8);
        string record = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyOfferCooldownRecord.cs"),
            Encoding.UTF8);
        string lifecycleRules = File.ReadAllText(
            FindRepositoryFile("Refactor", "Domain", "WorldDiplomacyRoundLifecycleRules.cs"),
            Encoding.UTF8);
        string normalizeMethod = ExtractMethod(behavior, "private void NormalizeOfferCooldownStorage()");
        string rebuildMethod = ExtractMethod(lifecycleRules, "public static void RebuildOfferCooldownIndex(");

        Test.True(normalizeMethod.Contains(
                      "WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(_storage)",
                      StringComparison.Ordinal)
                  && normalizeMethod.Contains("WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(", StringComparison.Ordinal)
                  && !normalizeMethod.Contains("new Dictionary", StringComparison.Ordinal),
            "campaign behavior must delegate storage cleanup before rebuilding its runtime index");
        Test.True(rebuildMethod.Contains(
                      "WorldDiplomacyOfferCooldownStorageNormalizer.TryCreateKey(",
                      StringComparison.Ordinal),
            "runtime index rebuild must reuse persistence key parsing");
        Test.True(!behavior.Contains("private static bool TryParseOfferCooldownDomain", StringComparison.Ordinal)
                  && !behavior.Contains("private static string OfferCooldownDomainToken", StringComparison.Ordinal),
            "campaign behavior must not retain a second storage codec");
        Test.True(!behavior.Contains("public sealed class WorldDiplomacyOfferCooldown", StringComparison.Ordinal)
                  && record.Contains("namespace AnimusForge;", StringComparison.Ordinal)
                  && record.Contains("public sealed class WorldDiplomacyOfferCooldown", StringComparison.Ordinal),
            "the serialized cooldown record must live in persistence while retaining its AnimusForge type identity");
        foreach (string jsonField in new[]
                 {
                     "proposerKingdomId", "targetKingdomId", "domain", "lastFailedRoundDay", "sourceRoundId"
                 })
        {
            Test.True(record.Contains("[JsonProperty(\"" + jsonField + "\")]", StringComparison.Ordinal),
                "the extracted cooldown record must preserve JSON field: " + jsonField);
        }
        Test.True(!persistence.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !persistence.Contains("Newtonsoft", StringComparison.Ordinal),
            "cooldown normalization must remain isolated from game APIs and JSON transport");
    }

    private static WorldDiplomacyOfferCooldown Cooldown(
        string proposer,
        string target,
        string domain,
        int day,
        string round)
    {
        return new WorldDiplomacyOfferCooldown
        {
            ProposerKingdomId = proposer,
            TargetKingdomId = target,
            Domain = domain,
            LastFailedRoundDay = day,
            SourceRoundId = round
        };
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Could not locate method: " + signature);
        int openBrace = source.IndexOf('{', start);
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
            {
                return source.Substring(start, index - start + 1);
            }
        }
        throw new InvalidOperationException("Could not parse method: " + signature);
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}

namespace AnimusForge
{
    public sealed class WorldDiplomacyStorage
    {
        public int OfferCooldownStateSchemaVersion { get; set; }
        public List<WorldDiplomacyOfferCooldown> OfferCooldowns { get; set; } = new List<WorldDiplomacyOfferCooldown>();
    }

    public sealed class WorldDiplomacyOfferCooldown
    {
        public string ProposerKingdomId { get; set; } = "";
        public string TargetKingdomId { get; set; } = "";
        public string Domain { get; set; } = "";
        public int LastFailedRoundDay { get; set; }
        public string SourceRoundId { get; set; } = "";
    }
}
