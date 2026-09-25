using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        VerifyMissingShapeIsRepaired();
        VerifyExistingReferencesArePreserved();
        VerifyBehaviorDelegation();
        Console.WriteLine($"World diplomacy storage-shape smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyMissingShapeIsRepaired()
    {
        WorldDiplomacyStorage storage = WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(null);
        Test.True(storage != null, "null storage must become one empty canonical storage instance");

        foreach (PropertyInfo property in typeof(WorldDiplomacyStorage).GetProperties())
        {
            Test.True(property.GetValue(storage) != null,
                "root persistence member must be initialized: " + property.Name);
        }

        storage.NationalPrestigeByKingdom["realm"] = 1;
        storage.InternationalReputationByKingdom["realm"] = 2;
        storage.InternationalReputationNaturalChangeLastDayByKingdom["realm"] = 3;
        storage.LastOffensiveWarDayByKingdom["realm"] = 4;
        storage.LastPeaceDayByPair["realm"] = 5;
        Test.True(storage.NationalPrestigeByKingdom.ContainsKey("REALM")
                  && storage.InternationalReputationByKingdom.ContainsKey("REALM")
                  && storage.InternationalReputationNaturalChangeLastDayByKingdom.ContainsKey("REALM")
                  && storage.LastOffensiveWarDayByKingdom.ContainsKey("REALM")
                  && storage.LastPeaceDayByPair.ContainsKey("REALM"),
            "new root dictionaries must retain ordinal-ignore-case identity semantics");
    }

    private static void VerifyExistingReferencesArePreserved()
    {
        WorldDiplomacyStorage storage = WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(null);
        Dictionary<PropertyInfo, object> references = typeof(WorldDiplomacyStorage).GetProperties()
            .ToDictionary(property => property, property => property.GetValue(storage));

        WorldDiplomacyStorage normalized = WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(storage);
        Test.True(ReferenceEquals(normalized, storage),
            "normalization must preserve the existing canonical storage instance");
        foreach ((PropertyInfo property, object expected) in references)
        {
            Test.True(ReferenceEquals(property.GetValue(normalized), expected),
                "normalization must preserve a populated root member: " + property.Name);
        }
    }

    private static void VerifyBehaviorDelegation()
    {
        string behavior = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"), Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string normalizeStorage = ExtractMethod(behavior, "private void NormalizeStorage(");
        string normalizeThreats = ExtractMethod(behavior, "private void NormalizeDiplomaticThreats(");
        string normalizer = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyStorageShapeNormalizer.cs"),
            Encoding.UTF8);
        string storageRecord = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyStorageRecord.cs"),
            Encoding.UTF8);

        int shapeRepair = normalizeStorage.IndexOf(
            "WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(_storage)",
            StringComparison.Ordinal);
        int notificationMigration = normalizeStorage.IndexOf(
            "WorldDiplomacyNotificationStateMigration.Migrate(_storage)",
            StringComparison.Ordinal);
        Test.True(shapeRepair >= 0 && notificationMigration > shapeRepair,
            "root shape repair must run before migrations consume persisted collections");
        const string rootInitializationPattern = @"_storage\.[A-Za-z0-9_]+\s*\?\?=\s*new (?:List|Dictionary)<";
        Test.True(!Regex.IsMatch(normalizeStorage, rootInitializationPattern)
                  && !Regex.IsMatch(normalizeThreats, rootInitializationPattern),
            "behavior migration methods must delegate root collection initialization");
        Test.True(!normalizer.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !normalizer.Contains("Newtonsoft", StringComparison.Ordinal),
            "root storage shape repair must remain independent of game and JSON APIs");
        Test.True(!behavior.Contains("public sealed class WorldDiplomacyStorage", StringComparison.Ordinal)
                  && storageRecord.Contains("namespace AnimusForge;", StringComparison.Ordinal)
                  && storageRecord.Contains("public sealed class WorldDiplomacyStorage", StringComparison.Ordinal)
                  && !storageRecord.Contains("TaleWorlds", StringComparison.Ordinal),
            "the canonical root record must live in persistence without changing its type identity or gaining game dependencies");
        Test.True(Count(normalizer, "storage.") == typeof(WorldDiplomacyStorage).GetProperties().Length,
            "the persistence normalizer must own every root collection member exactly once");
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

    private static int Count(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
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
