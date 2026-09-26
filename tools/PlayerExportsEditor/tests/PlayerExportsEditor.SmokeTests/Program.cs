using PlayerExportsEditor.Core;

var service = new PlayerExportsService();
var validator = new PlayerExportsValidator();

if (args.Contains("--path-contract", StringComparer.Ordinal))
{
    var isolated = Path.Combine(Path.GetTempPath(), "af-editor-path-" + Guid.NewGuid().ToString("N"));
    Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", isolated);
    var expected = Path.Combine(isolated, "UserData", "PlayerExports");
    if (service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory) != expected ||
        service.FindDefaultPlayerExportsRoot(Directory.GetCurrentDirectory()) != expected ||
        Directory.Exists(expected))
    {
        Console.Error.WriteLine("FAIL editor path contract");
        return 1;
    }
    var legacy = Path.Combine(Directory.GetCurrentDirectory(), "AnimusForge", "PlayerExports");
    var legacyPackage = Path.Combine(legacy, "synthetic");
    RejectWrite(() => service.CreatePackage(legacy, "synthetic"));
    RejectWrite(() => service.SaveJsonDocument(legacyPackage, Path.Combine(legacyPackage, "data.json"), "{}"));
    RejectWrite(() => service.SaveKnowledgeRule(legacyPackage, Path.Combine(legacyPackage, "knowledge", "rules", "rule.json"), new LoreRule()));
    RejectWrite(() => service.CreateKnowledgeRule(legacyPackage, new LoreRule()));
    RejectWrite(() => service.SavePersonaProfile(legacyPackage, Path.Combine(legacyPackage, "personality_background", "npc.json"), new NpcPersonaProfile()));
    RejectWrite(() => service.MovePackageToDeleted(legacy, legacyPackage));
    RejectWrite(() => service.MoveJsonFileToDeleted(legacyPackage, Path.Combine(legacyPackage, "data.json")));
    RejectWrite(() => service.MoveDataTypeToDeleted(legacyPackage, PlayerExportsDataType.EventData));
    var validPackage = Path.Combine(expected, "synthetic");
    RejectWrite(() => service.SaveJsonDocument(validPackage, Path.Combine(isolated, "escape.json"), "{}"));
    Console.WriteLine("PASS editor path contract: shared override, no cwd fallback, no legacy or escaping writes");
    return 0;
}

if (args.Contains("--path-contract-invalid", StringComparer.Ordinal))
{
    Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", "relative-root");
    RejectWrite(() => service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory));
    Console.WriteLine("PASS editor path contract: invalid override fails closed");
    return 0;
}

RunDataTypeDeletionSmoke(service);

var root = service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory);

if (string.IsNullOrWhiteSpace(root))
{
    Console.Error.WriteLine("Could not find AnimusForge/PlayerExports from the current directory.");
    return 1;
}

Console.WriteLine("PlayerExports root resolved");
var packages = service.ListPackages(root);
if (packages.Count == 0)
{
    Console.Error.WriteLine("No PlayerExports packages found.");
    return 1;
}

ConditionCatalog? conditionCatalog = null;
var packageIndex = 0;
foreach (var package in packages)
{
    packageIndex++;
    var data = service.LoadPackage(package.FullPath);
    var issues = validator.Validate(data);
    var errors = issues.Count(x => x.Severity == ValidationSeverity.Error);
    var warnings = issues.Count(x => x.Severity == ValidationSeverity.Warning);

    Console.WriteLine(
        $"package-index={packageIndex}: knowledge={data.KnowledgeRules.Count}, personas={data.Personas.Count}, events={data.EventFiles.Count}, " +
        $"voice={(data.VoiceMapping == null ? "missing" : "ok")}, unnamed={(data.UnnamedPersona == null ? "missing" : "ok")}, " +
        $"errors={errors}, warnings={warnings}");

    if (conditionCatalog == null)
    {
        conditionCatalog = new ConditionCatalogBuilder().Build(data, AppContext.BaseDirectory);
        Console.WriteLine("condition-catalog built");
        if (conditionCatalog.Roles.Count < 7)
        {
            Console.Error.WriteLine("Condition catalog did not include the built-in role conditions.");
            return 1;
        }

        Console.WriteLine(
            "localized-counts: heroes=" + CountLocalized(conditionCatalog.Heroes) +
            ", cultures=" + CountLocalized(conditionCatalog.Cultures) +
            ", kingdoms=" + CountLocalized(conditionCatalog.Kingdoms) +
            ", clans=" + CountLocalized(conditionCatalog.Clans) +
            ", settlements=" + CountLocalized(conditionCatalog.Settlements) +
            ", identities=" + CountLocalized(conditionCatalog.Identities) +
            ", skills=" + CountLocalized(conditionCatalog.Skills));
    }

    foreach (var issue in issues.Where(x => x.Severity == ValidationSeverity.Error).Take(10))
    {
        Console.Error.WriteLine("  ERROR " + issue.Area);
    }

    if (data.LoadIssues.Any(x => x.Severity == ValidationSeverity.Error))
    {
        return 1;
    }
}

return 0;

static void RejectWrite(Action action)
{
    try { action(); }
    catch (ArgumentException) { return; }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException("Expected path rejection before any write.");
}

static int CountLocalized(IEnumerable<ConditionCandidate> candidates)
{
    return candidates.Count(x => (x.Label ?? "").Any(c => c > 127));
}

static void RunDataTypeDeletionSmoke(PlayerExportsService service)
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "af_playerexports_editor_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        var package = service.CreatePackage(tempRoot, "DeletionSmoke");
        var eventFiles = service.ListDataTypeJsonFiles(package.FullPath, PlayerExportsDataType.EventData);
        if (eventFiles.Count < 3)
        {
            throw new InvalidOperationException("Deletion smoke package did not create the expected event JSON files.");
        }

        var deleted = service.MoveDataTypeToDeleted(package.FullPath, PlayerExportsDataType.EventData);
        if (deleted.MovedFiles.Count != eventFiles.Count)
        {
            throw new InvalidOperationException("Data type deletion moved " + deleted.MovedFiles.Count + " files; expected " + eventFiles.Count + ".");
        }

        if (service.ListDataTypeJsonFiles(package.FullPath, PlayerExportsDataType.EventData).Count != 0)
        {
            throw new InvalidOperationException("Event data files still exist after data type deletion.");
        }

        foreach (var movedFile in deleted.MovedFiles)
        {
            if (!File.Exists(movedFile))
            {
                throw new InvalidOperationException("Moved file was not found in deleted root: " + movedFile);
            }
        }

        Console.WriteLine("data-type-delete-smoke: moved=" + deleted.MovedFiles.Count);
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
