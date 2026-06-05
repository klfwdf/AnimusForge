using PlayerExportsEditor.Core;

var parsed = ParseArgs(args);
var output = parsed.OutputPath;
if (string.IsNullOrWhiteSpace(output))
{
    output = ConditionCatalogStore.GetDefaultPath(Directory.GetCurrentDirectory());
}

var options = new ConditionCatalogBuildOptions
{
    IncludePackagedCatalog = false,
    IncludeLooseModuleData = false,
    OfficialModulesOnly = !parsed.IncludeInstalledMods,
    ExtraModulesRoots = parsed.ModulesRoots
};

var catalog = new ConditionCatalogBuilder().Build(null, AppContext.BaseDirectory, options);
if (catalog.XmlFileCount == 0)
{
    Console.Error.WriteLine("No Bannerlord ModuleData XML files were found. Pass --modules-root \"<Bannerlord Modules>\" if the game is not in a default Steam path.");
    return 1;
}

ConditionCatalogStore.SaveVanillaCatalog(output, catalog);
Console.WriteLine("Exported vanilla condition catalog:");
Console.WriteLine(output);
Console.WriteLine(catalog.Summary);
return 0;

static ParsedArgs ParseArgs(string[] args)
{
    var result = new ParsedArgs();
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            result.OutputPath = args[++i];
            continue;
        }

        if (arg.Equals("--modules-root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            result.ModulesRoots.Add(args[++i]);
            continue;
        }

        if (arg.Equals("--include-installed-mods", StringComparison.OrdinalIgnoreCase))
        {
            result.IncludeInstalledMods = true;
        }
    }

    return result;
}

internal sealed class ParsedArgs
{
    public string OutputPath { get; set; } = "";

    public bool IncludeInstalledMods { get; set; }

    public List<string> ModulesRoots { get; } = new();
}
