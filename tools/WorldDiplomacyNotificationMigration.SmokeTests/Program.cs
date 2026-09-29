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
        VerifyLegacyMigration();
        VerifyVersionGate();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy notification-migration smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyLegacyMigration()
    {
        WorldDiplomacyDocument player = new WorldDiplomacyDocument { IsPlayerAuthored = true };
        WorldDiplomacyDocument readyAi = new WorldDiplomacyDocument { IsReadyForPublication = true };
        WorldDiplomacyDocument notifiedAi = new WorldDiplomacyDocument { IsNotified = true };
        WorldDiplomacyDocument readAi = new WorldDiplomacyDocument { IsRead = true };
        WorldDiplomacyDocument compressedAi = new WorldDiplomacyDocument { IsCompressed = true };
        WorldDiplomacyDocument untouchedAi = new WorldDiplomacyDocument();
        WorldDiplomacyStorage storage = new WorldDiplomacyStorage
        {
            DiplomacyNotificationStateSchemaVersion = 0,
            Documents = new List<WorldDiplomacyDocument>
            {
                null,
                player,
                readyAi,
                notifiedAi,
                readAi,
                compressedAi,
                untouchedAi
            }
        };

        WorldDiplomacyNotificationStateMigration.Migrate(storage);

        Test.True(storage.DiplomacyNotificationStateSchemaVersion == 1,
            "legacy notification state must stamp schema version 1");
        Test.True(player.RumorNotified && player.FormalNoticeShown,
            "player-authored legacy documents must suppress both notification paths");
        Test.True(readyAi.RumorNotified && !readyAi.FormalNoticeShown,
            "published AI documents must retain the legacy rumor-only migration");
        Test.True(!notifiedAi.RumorNotified && notifiedAi.FormalNoticeShown,
            "previously notified AI documents must retain formal notice state");
        Test.True(readAi.FormalNoticeShown && compressedAi.FormalNoticeShown,
            "read or compressed AI documents must retain formal notice state");
        Test.True(!untouchedAi.RumorNotified && !untouchedAi.FormalNoticeShown,
            "unpublished untouched AI documents must remain unnotified");
    }

    private static void VerifyVersionGate()
    {
        WorldDiplomacyDocument document = new WorldDiplomacyDocument
        {
            IsPlayerAuthored = true,
            IsReadyForPublication = true
        };
        WorldDiplomacyStorage storage = new WorldDiplomacyStorage
        {
            DiplomacyNotificationStateSchemaVersion = 1,
            Documents = new List<WorldDiplomacyDocument> { document }
        };

        WorldDiplomacyNotificationStateMigration.Migrate(storage);

        Test.True(!document.RumorNotified && !document.FormalNoticeShown,
            "current-schema storage must not replay the migration");
        storage.DiplomacyNotificationStateSchemaVersion = 2;
        WorldDiplomacyNotificationStateMigration.Migrate(storage);
        Test.True(storage.DiplomacyNotificationStateSchemaVersion == 2,
            "newer storage must not be downgraded");
    }

    private static void VerifySourceBoundary()
    {
        string behavior = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs"), Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.JobRuntime.cs"));
        string normalizeStorage = ExtractMethod(
            File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStorageNormalizationApplication.cs"), Encoding.UTF8),
            "internal static void Normalize<TSource, TMigration>(");
        Test.True(behavior.Contains("WorldDiplomacyStorageNormalizationApplication.Normalize(ref _storage, allowWorldValidation, ref source, ref migration)", StringComparison.Ordinal),
            "behavior normalization must call the real storage-normalization Application owner");
        string newGame = ExtractMethod(behavior, "private void OnNewGameCreated(");
        string migration = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyNotificationStateMigration.cs"),
            Encoding.UTF8);
        string documentRecords = File.ReadAllText(
            FindRepositoryFile("Refactor", "Persistence", "WorldDiplomacyDocumentRecords.cs"),
            Encoding.UTF8);

        Test.True(normalizeStorage.Contains(
                      "WorldDiplomacyNotificationStateMigration.Migrate(storage)",
                      StringComparison.Ordinal)
                  && !normalizeStorage.Contains("document.RumorNotified", StringComparison.Ordinal)
                  && !normalizeStorage.Contains("document.FormalNoticeShown", StringComparison.Ordinal),
            "behavior normalization must delegate the notification migration algorithm");
        Test.True(newGame.Contains(
                      "WorldDiplomacyNotificationStateMigration.CurrentSchemaVersion",
                      StringComparison.Ordinal)
                  && !behavior.Contains("private const int DiplomacyNotificationStateSchemaVersion", StringComparison.Ordinal),
            "new-game storage and migration must share one schema-version owner");
        Test.True(!migration.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !migration.Contains("Newtonsoft", StringComparison.Ordinal)
                  && !migration.Contains("System.Linq", StringComparison.Ordinal),
            "notification migration must remain persistence-only and allocation-bounded");
        Test.True(!behavior.Contains("public sealed class WorldDiplomacyDocument", StringComparison.Ordinal)
                  && documentRecords.Contains("namespace AnimusForge;", StringComparison.Ordinal)
                  && documentRecords.Contains("public sealed class WorldDiplomacyDocumentAction", StringComparison.Ordinal)
                  && documentRecords.Contains("public sealed class WorldDiplomacyDocument", StringComparison.Ordinal)
                  && !documentRecords.Contains("TaleWorlds", StringComparison.Ordinal),
            "document persistence records must retain their AnimusForge identities without game dependencies");
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
