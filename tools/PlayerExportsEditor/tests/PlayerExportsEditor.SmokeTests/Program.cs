using PlayerExportsEditor.Core;

var service = new PlayerExportsService();
var validator = new PlayerExportsValidator();

RunDataTypeDeletionSmoke(service);

var root = service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory) ??
           service.FindDefaultPlayerExportsRoot(Directory.GetCurrentDirectory());

if (string.IsNullOrWhiteSpace(root))
{
    Console.Error.WriteLine("Could not find AnimusForge/PlayerExports from the current directory.");
    return 1;
}

Console.WriteLine("PlayerExports root: " + root);
var packages = service.ListPackages(root);
if (packages.Count == 0)
{
    Console.Error.WriteLine("No PlayerExports packages found.");
    return 1;
}

ConditionCatalog? conditionCatalog = null;
foreach (var package in packages)
{
    var data = service.LoadPackage(package.FullPath);
    var issues = validator.Validate(data);
    var errors = issues.Count(x => x.Severity == ValidationSeverity.Error);
    var warnings = issues.Count(x => x.Severity == ValidationSeverity.Warning);

    Console.WriteLine(
        $"{package.Name}: knowledge={data.KnowledgeRules.Count}, personas={data.Personas.Count}, events={data.EventFiles.Count}, " +
        $"voice={(data.VoiceMapping == null ? "missing" : "ok")}, unnamed={(data.UnnamedPersona == null ? "missing" : "ok")}, " +
        $"errors={errors}, warnings={warnings}");

    if (conditionCatalog == null)
    {
        conditionCatalog = new ConditionCatalogBuilder().Build(data, AppContext.BaseDirectory);
        Console.WriteLine("condition-catalog: " + conditionCatalog.Summary);
        if (conditionCatalog.Roles.Count < 7)
        {
            Console.Error.WriteLine("Condition catalog did not include the built-in role conditions.");
            return 1;
        }

        var localizedSamples = conditionCatalog.Heroes
            .Concat(conditionCatalog.Kingdoms)
            .Concat(conditionCatalog.Settlements)
            .Concat(conditionCatalog.Cultures)
            .Where(x => (x.Label ?? "").Any(c => c > 127))
            .Take(3)
            .Select(x => x.ToString())
            .ToList();
        Console.WriteLine(
            "localized-counts: heroes=" + CountLocalized(conditionCatalog.Heroes) +
            ", cultures=" + CountLocalized(conditionCatalog.Cultures) +
            ", kingdoms=" + CountLocalized(conditionCatalog.Kingdoms) +
            ", clans=" + CountLocalized(conditionCatalog.Clans) +
            ", settlements=" + CountLocalized(conditionCatalog.Settlements) +
            ", identities=" + CountLocalized(conditionCatalog.Identities) +
            ", skills=" + CountLocalized(conditionCatalog.Skills));
        Console.WriteLine("localized-samples: " + (localizedSamples.Count == 0 ? "none" : string.Join(" | ", localizedSamples)));
    }

    foreach (var issue in issues.Where(x => x.Severity == ValidationSeverity.Error).Take(10))
    {
        Console.Error.WriteLine("  ERROR " + issue.Area + " " + issue.FileName + ": " + issue.Message);
    }

    if (data.LoadIssues.Any(x => x.Severity == ValidationSeverity.Error))
    {
        return 1;
    }
}

return 0;

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
