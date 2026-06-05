using PlayerExportsEditor.Core;

var service = new PlayerExportsService();
var validator = new PlayerExportsValidator();
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
