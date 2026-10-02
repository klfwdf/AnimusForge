using PlayerExportsEditor.Core;

var service = new PlayerExportsService();
var validator = new PlayerExportsValidator();

if (args.Length == 2 && args[0] == "--installed-library-contract")
{
    var installedLibraryFixture = Path.GetFullPath(args[1]);
    var workspace = Directory.GetCurrentDirectory();
    if (!installedLibraryFixture.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || Directory.Exists(installedLibraryFixture) || File.Exists(installedLibraryFixture))
        throw new InvalidOperationException("Installed library contract requires a new workspace fixture.");
    Environment.SetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT", null);
    var module = Path.Combine(installedLibraryFixture, "AnimusForge");
    var exports = Path.Combine(module, "PlayerExports");
    var package = Path.Combine(exports, "worldbook");
    Directory.CreateDirectory(package);
    File.WriteAllText(Path.Combine(module, "SubModule.xml"), "<Module />");
    var source = Path.Combine(package, "data.json");
    File.WriteAllText(source, "{\"playerModified\":true}");
    var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AnimusForge", "UserData", "PlayerExports");
    if (service.FindDefaultPlayerExportsRoot(module) != expected
        || service.ListPackages(exports).Single().FullPath != package)
        throw new InvalidOperationException("Installed library must not require a migration receipt.");
    RejectWrite(() => service.CreatePackage(exports, "new"));
    RejectWrite(() => service.SaveJsonDocument(package, source, "{}"));
    if (File.ReadAllText(source) != "{\"playerModified\":true}")
        throw new InvalidOperationException("Reading the installed library changed player files.");
    Console.WriteLine("PASS installed library contract: no migration gate, user destination, module writes rejected");
    return 0;
}

if (args.Length == 2 && args[0] == "--backup-contract")
{
    var id = args[1];
    var cwd = Directory.GetCurrentDirectory();
    if (id.Length == 0 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ||
        !File.Exists(Path.Combine(cwd, "AnimusForge.csproj")))
        throw new InvalidOperationException("Backup contract requires a repository root and safe run id.");
    var backupRoot = Path.Combine(cwd, "artifacts", "j15-content", "editor-backup-" + id);
    if (Directory.Exists(backupRoot) || File.Exists(backupRoot))
        throw new InvalidOperationException("Backup contract refuses to reuse an output root.");
    Directory.CreateDirectory(backupRoot);
    var file = Path.Combine(backupRoot, "sample.json");
    var store = new JsonFileStore();
    File.WriteAllText(file, "{\"value\":0}");
    var backups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (var value = 1; value <= 12; value++)
    {
        var previous = File.ReadAllText(file);
        var backup = store.SaveUtf8WithBackup(file, "{\"value\":" + value + "}", backupRoot);
        if (!File.Exists(backup) || File.ReadAllText(backup) != previous || !backups.Add(backup))
            throw new InvalidOperationException("Consecutive edits did not preserve distinct previous bytes.");
    }
    var final = File.ReadAllText(file);
    var invalidRejected = false;
    try { store.SaveUtf8WithBackup(file, "{bad json", backupRoot); }
    catch (System.Text.Json.JsonException) { invalidRejected = true; }
    if (!invalidRejected || File.ReadAllText(file) != final || backups.Count != 12)
        throw new InvalidOperationException("Invalid edit changed the active JSON or earlier backups.");
    Console.WriteLine("PASS editor backup contract: consecutive=12 distinct=12 invalid_preserved=1");
    return 0;
}

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

if (args.Length == 2 && args[0] == "--isolated-full")
{
    return RunIsolatedFullSmoke(service, validator, args[1]);
}

var fixtureRoot = Path.Combine(Path.GetTempPath(), "af_playerexports_editor_" + Guid.NewGuid().ToString("N"));
try
{
    RunDataTypeDeletionSmoke(service, Path.Combine(fixtureRoot, "UserData", "PlayerExports"));
}
finally
{
    if (Directory.Exists(fixtureRoot))
    {
        Directory.Delete(fixtureRoot, recursive: true);
    }
}

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

// Deletion fixture must live under a canonical <data-root>/UserData/PlayerExports; the caller owns cleanup.
static void RunDataTypeDeletionSmoke(PlayerExportsService service, string playerExportsRoot)
{
    Directory.CreateDirectory(playerExportsRoot);
    {
        var package = service.CreatePackage(playerExportsRoot, "DeletionSmoke");
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
}

// Full synthetic edit/save/backup/restore/delete smoke. Every write stays under the approved synthetic root;
// nothing is cleaned up so the root remains as evidence.
static int RunIsolatedFullSmoke(PlayerExportsService service, PlayerExportsValidator validator, string syntheticRootArg)
{
    var syntheticRoot = RequireIsolatedRoot(syntheticRootArg);
    var dataRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT")!).TrimEnd('\\', '/');
    var realExports = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnimusForge", "UserData", "PlayerExports");
    var realBefore = SnapshotDirectory(realExports);
    var store = new JsonFileStore();

    // 1. Shared locator resolves to the synthetic data root only.
    var exports = service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory);
    Check(exports == Path.Combine(dataRoot, "UserData", "PlayerExports"), "locator did not resolve the synthetic data root");
    Check(!Directory.Exists(exports) && !File.Exists(exports), "synthetic PlayerExports already existed");
    Directory.CreateDirectory(exports);

    // 2. Create package with default files; it must validate without errors.
    var package = service.CreatePackage(exports, "J15Smoke");
    Check(service.ListPackages(exports).Count == 1, "new synthetic package was not listed exactly once");
    var pkg = package.FullPath;
    var data = service.LoadPackage(pkg);
    Check(data.VoiceMapping != null && data.UnnamedPersona != null && data.EventFiles.Count == 3, "created package is missing default files");
    Check(ErrorCount(validator, data) == 0, "fresh package has validation errors");
    Console.WriteLine("step create-package: events=" + data.EventFiles.Count);

    // 3. Knowledge rule create + edit keeps a byte-exact backup.
    var rule = new LoreRule
    {
        Id = "j15_smoke_rule",
        Keywords = new List<string> { " smoke ", "smoke" },
        RagShortTexts = new List<string> { "J15 synthetic smoke rule." },
        Variants = new List<LoreVariant> { new() { Priority = 0, Content = "Generic synthetic content." } }
    };
    var rulePath = service.CreateKnowledgeRule(pkg, rule);
    Check(File.Exists(rulePath) && rulePath.StartsWith(Path.Combine(pkg, "knowledge", "rules"), StringComparison.OrdinalIgnoreCase), "knowledge rule not created in package");
    var ruleBefore = File.ReadAllText(rulePath);
    rule.Variants.Add(new LoreVariant { Priority = 1, When = new LoreWhen { Cultures = new List<string> { "empire" } }, Content = "Empire variant." });
    var ruleBackup = service.SaveKnowledgeRule(pkg, rulePath, rule);
    Check(File.Exists(ruleBackup) && File.ReadAllText(ruleBackup) == ruleBefore, "knowledge edit backup does not hold previous bytes");
    var reloadedRule = service.LoadPackage(pkg).KnowledgeRules.Single();
    Check(reloadedRule.Rule?.Variants?.Count == 2 && reloadedRule.Rule.Keywords?.Count == 1, "knowledge edit not persisted or not normalized");
    Console.WriteLine("step knowledge: created=1 edited=1 backup=1");

    // 4. Persona create + edit.
    var personaPath = Path.Combine(pkg, "personality_background", "lord_synthetic__Smoke.json");
    service.SavePersonaProfile(pkg, personaPath, new NpcPersonaProfile { Personality = " calm ", Background = "Synthetic background." });
    var personaBefore = File.ReadAllText(personaPath);
    var personaBackup = service.SavePersonaProfile(pkg, personaPath, new NpcPersonaProfile { Personality = "bold", Background = "Edited." });
    Check(File.ReadAllText(personaBackup) == personaBefore, "persona edit backup does not hold previous bytes");
    Check(service.LoadPackage(pkg).Personas.Single().Profile?.Personality == "bold", "persona edit not persisted");
    Console.WriteLine("step persona: created=1 edited=1 backup=1");

    // 5. Raw JSON edit, invalid edit rejection, and restore from backup.
    var summaryPath = Path.Combine(pkg, "event_data", "WorldOpeningSummary.json");
    var summaryOriginal = File.ReadAllText(summaryPath);
    var summaryBackup = service.SaveJsonDocument(pkg, summaryPath, "{\"Summary\":\"Synthetic world summary.\"}");
    Check(File.ReadAllText(summaryBackup) == summaryOriginal, "summary backup does not hold previous bytes");
    var summaryEdited = File.ReadAllText(summaryPath);
    var invalidRejected = false;
    try { service.SaveJsonDocument(pkg, summaryPath, "{bad json"); }
    catch (System.Text.Json.JsonException) { invalidRejected = true; }
    Check(invalidRejected && File.ReadAllText(summaryPath) == summaryEdited && !File.Exists(summaryPath + ".tmp"), "invalid JSON changed the active file");
    var restoreBackup = service.SaveJsonDocument(pkg, summaryPath, store.ReadUtf8(summaryBackup));
    Check(File.Exists(restoreBackup) && restoreBackup != summaryBackup && File.ReadAllText(restoreBackup) == summaryEdited,
        "restore did not preserve the edited version in a distinct backup");
    Check(File.ReadAllText(summaryPath) == summaryOriginal, "restore from backup did not return original bytes");
    Console.WriteLine("step json-edit: edited=1 invalid_preserved=1 restored=1");

    // 6. Package list ordering (newest first) with explicit timestamps.
    var second = service.CreatePackage(exports, "J15SmokeSecond");
    Directory.SetLastWriteTime(pkg, new DateTime(2026, 1, 1));
    Directory.SetLastWriteTime(second.FullPath, new DateTime(2026, 1, 2));
    var listed = service.ListPackages(exports);
    Check(listed.Count == 2 && listed[0].FullPath == second.FullPath, "package list is not newest-first");
    Console.WriteLine("step list-packages: count=2 newest_first=1");

    // 7. Soft deletes: single file, data type, whole package.
    var personaEdited = File.ReadAllText(personaPath);
    var movedPersona = service.MoveJsonFileToDeleted(pkg, personaPath);
    Check(!File.Exists(personaPath) && File.ReadAllText(movedPersona) == personaEdited, "persona soft delete failed");
    Check(movedPersona.StartsWith(Path.Combine(pkg, ".deleted_files"), StringComparison.OrdinalIgnoreCase), "persona moved outside package deleted root");
    RunDataTypeDeletionSmoke(service, exports);
    var movedPackage = service.MovePackageToDeleted(exports, second.FullPath);
    Check(!Directory.Exists(second.FullPath) && Directory.Exists(movedPackage)
        && movedPackage.StartsWith(Path.Combine(exports, ".deleted_packages"), StringComparison.OrdinalIgnoreCase), "package soft delete failed");
    Check(service.ListPackages(exports).All(p => p.FullPath != second.FullPath), "deleted package still listed");
    Console.WriteLine("step soft-delete: file=1 datatype=1 package=1");

    // 8. Escapes and hidden roots are rejected before writing.
    RejectWrite(() => service.SaveJsonDocument(pkg, Path.Combine(exports, "escape.json"), "{}"));
    RejectWrite(() => service.SaveJsonDocument(pkg, Path.Combine(pkg, "knowledge", "note.txt"), "{}"));
    RejectWrite(() => service.SaveJsonDocument(movedPackage, Path.Combine(movedPackage, "voice_mapping", "VoiceMapping.json"), "{}"));
    RejectWrite(() => service.CreatePackage(exports, ".hidden"));
    Check(!File.Exists(Path.Combine(exports, "escape.json")), "escape write reached disk");
    Console.WriteLine("step reject-writes: 4");

    // 9. Final reload, validation and condition catalog on the edited package.
    var final = service.LoadPackage(pkg);
    Check(ErrorCount(validator, final) == 0 && final.LoadIssues.Count == 0, "edited package has validation errors");
    var catalog = new ConditionCatalogBuilder().Build(final, AppContext.BaseDirectory);
    Check(catalog.Roles.Count >= 7, "condition catalog missing built-in roles");
    Console.WriteLine("step final-validate: knowledge=" + final.KnowledgeRules.Count + " personas=" + final.Personas.Count + " errors=0");

    Check(SnapshotDirectory(realExports) == realBefore, "real user PlayerExports root changed during smoke");
    Check(Path.GetFullPath(Path.GetTempPath()).StartsWith(syntheticRoot, StringComparison.OrdinalIgnoreCase), "TEMP drifted outside synthetic root");
    Console.WriteLine("PASS editor isolated full smoke: steps=9 real_root_unchanged=1");
    return 0;
}

static string RequireIsolatedRoot(string arg)
{
    if (string.IsNullOrWhiteSpace(arg) || !Path.IsPathFullyQualified(arg) || arg.StartsWith(@"\\", StringComparison.Ordinal))
        throw new InvalidOperationException("Isolated smoke requires an absolute local synthetic root.");
    var root = Path.GetFullPath(arg).TrimEnd('\\', '/');
    if (!Directory.Exists(root))
        throw new InvalidOperationException("Isolated smoke root must be pre-created by the runner.");
    for (DirectoryInfo? d = new(root); d != null; d = d.Parent)
    {
        if ((d.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Isolated smoke root crosses a reparse point.");
    }
    var prefix = root + Path.DirectorySeparatorChar;
    foreach (var name in new[] { "ANIMUSFORGE_DATA_ROOT", "TEMP", "TMP" })
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value) || !Path.GetFullPath(value).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(name + " must point inside the isolated smoke root.");
    }
    if (!Path.GetFullPath(Path.GetTempPath()).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Process temp path is outside the isolated smoke root.");
    var dataRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.GetEnvironmentVariable("ANIMUSFORGE_DATA_ROOT")!));
    var tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
    if (!string.Equals(dataRoot, Path.Combine(root, "data"), StringComparison.OrdinalIgnoreCase) ||
        !Directory.Exists(tempRoot) ||
        !string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.GetEnvironmentVariable("TEMP")!)), tempRoot, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.GetEnvironmentVariable("TMP")!)), tempRoot, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Isolated smoke requires canonical data and one existing temp directory under its root.");
    return root;
}

static string SnapshotDirectory(string path)
{
    if (!Directory.Exists(path)) return "absent";
    var entries = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)
        .Select(p => p + "|" + File.GetLastWriteTimeUtc(p).Ticks)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
    return string.Join("\n", entries);
}

static int ErrorCount(PlayerExportsValidator validator, PlayerExportsPackageData data)
{
    return validator.Validate(data).Count(x => x.Severity == ValidationSeverity.Error);
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("FAIL editor isolated full smoke: " + message);
}
