using System.Text.Json.Nodes;
using System.Text.Json;
using AnimusForge;

namespace PlayerExportsEditor.Core;

public sealed class PlayerExportsService
{
    private readonly JsonFileStore _json = new();

    public string FindDefaultPlayerExportsRoot(string startDirectory)
    {
        // Kept for editor callers; the current directory is never a data-root fallback.
        var dataRoot = AnimusForgeDataPaths.GetCurrentRoot();
        var exports = AnimusForgeDataPaths.GetPlayerExportsDirectory(dataRoot);
        return exports;
    }

    public IReadOnlyList<PlayerExportsPackageInfo> ListPackages(string playerExportsRoot)
    {
        if (string.IsNullOrWhiteSpace(playerExportsRoot) || !Directory.Exists(playerExportsRoot))
        {
            return Array.Empty<PlayerExportsPackageInfo>();
        }

        return Directory.EnumerateDirectories(playerExportsRoot)
            .Where(dir => !Path.GetFileName(dir).StartsWith(".", StringComparison.Ordinal))
            .Select(BuildPackageInfo)
            .OrderByDescending(info => info.LastWriteTime)
            .ToList();
    }

    public PlayerExportsPackageInfo CreatePackage(string playerExportsRoot, string packageName)
    {
        var root = ValidateEditablePlayerExportsRoot(playerExportsRoot);
        var safeName = FileNameHelper.SanitizeFileNamePart(packageName, "NewPackage", 80);
        if (safeName.StartsWith(".", StringComparison.Ordinal))
            throw new InvalidOperationException("Hidden package names are reserved for recovery.");
        var packagePath = Path.Combine(root, safeName);
        if (Directory.Exists(packagePath))
        {
            throw new InvalidOperationException("Package already exists: " + safeName);
        }

        Directory.CreateDirectory(Path.Combine(packagePath, "knowledge", "rules"));
        Directory.CreateDirectory(Path.Combine(packagePath, "personality_background"));
        Directory.CreateDirectory(Path.Combine(packagePath, "unnamed_persona"));
        Directory.CreateDirectory(Path.Combine(packagePath, "voice_mapping"));
        Directory.CreateDirectory(Path.Combine(packagePath, "event_data"));

        _json.SaveUtf8WithBackup(Path.Combine(packagePath, "voice_mapping", "VoiceMapping.json"), BuildDefaultVoiceMappingJson(), packagePath);
        _json.SaveUtf8WithBackup(Path.Combine(packagePath, "unnamed_persona", "UnnamedNpcProfiles.json"), "{\r\n  \"Version\": 1,\r\n  \"Profiles\": {}\r\n}\r\n", packagePath);
        _json.SaveUtf8WithBackup(Path.Combine(packagePath, "event_data", "WorldOpeningSummary.json"), "{\r\n  \"Summary\": \"\"\r\n}\r\n", packagePath);
        _json.SaveUtf8WithBackup(Path.Combine(packagePath, "event_data", "KingdomOpeningSummaries.json"), "{}\r\n", packagePath);
        _json.SaveUtf8WithBackup(Path.Combine(packagePath, "event_data", "EventRecords.json"), "[]\r\n", packagePath);

        return BuildPackageInfo(packagePath);
    }

    public string MovePackageToDeleted(string playerExportsRoot, string packagePath)
    {
        var root = ValidateEditablePlayerExportsRoot(playerExportsRoot);
        var source = Path.GetFullPath(packagePath);
        if (!string.Equals(Path.GetDirectoryName(source), root, StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(source).StartsWith(".", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only an active package directly under PlayerExports may be moved.");
        }
        AssertNoReparse(source);

        var deletedRoot = Path.Combine(root, ".deleted_packages");
        Directory.CreateDirectory(deletedRoot);
        var target = Path.Combine(deletedRoot, Path.GetFileName(source) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.Move(source, target);
        return target;
    }

    public PlayerExportsPackageData LoadPackage(string packagePath)
    {
        var info = BuildPackageInfo(packagePath);
        var data = new PlayerExportsPackageData { Info = info };
        LoadKnowledgeRules(data);
        LoadPersonas(data);
        LoadVoiceMapping(data);
        LoadEventFiles(data);
        LoadUnnamedPersona(data);
        return data;
    }

    public string SaveJsonDocument(string packageRoot, string filePath, string contents)
    {
        ValidateEditableFile(packageRoot, filePath);
        return _json.SaveUtf8WithBackup(filePath, contents, packageRoot);
    }

    public string SaveKnowledgeRule(string packageRoot, string filePath, LoreRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        ValidateEditableFile(packageRoot, filePath);

        NormalizeRule(rule);
        return _json.SaveUtf8WithBackup(filePath, _json.ToIndentedJson(rule), packageRoot);
    }

    public string CreateKnowledgeRule(string packageRoot, LoreRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        ValidateEditablePackageRoot(packageRoot);

        NormalizeRule(rule);
        if (string.IsNullOrWhiteSpace(rule.Id))
        {
            rule.Id = "rule_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        var rulesDir = Path.Combine(packageRoot, "knowledge", "rules");
        Directory.CreateDirectory(rulesDir);
        var fileName = FileNameHelper.BuildKnowledgeRuleFileName(rule);
        var filePath = Path.Combine(rulesDir, fileName);
        if (File.Exists(filePath))
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            for (var i = 2; i <= 999; i++)
            {
                var candidate = Path.Combine(rulesDir, name + "__" + i + ".json");
                if (!File.Exists(candidate))
                {
                    filePath = candidate;
                    break;
                }
            }
        }

        _json.SaveUtf8WithBackup(filePath, _json.ToIndentedJson(rule), packageRoot);
        return filePath;
    }

    public string SavePersonaProfile(string packageRoot, string filePath, NpcPersonaProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        ValidateEditableFile(packageRoot, filePath);

        profile.Personality = (profile.Personality ?? "").Trim();
        profile.Background = (profile.Background ?? "").Trim();
        profile.VoiceId = (profile.VoiceId ?? "").Trim();
        return _json.SaveUtf8WithBackup(filePath, _json.ToIndentedJson(profile), packageRoot);
    }

    public string MoveJsonFileToDeleted(string packageRoot, string filePath)
    {
        ValidateEditableFile(packageRoot, filePath);
        var deletedRoot = CreateUniqueDeletedRoot(packageRoot);
        return MoveJsonFileToDeletedRoot(packageRoot, filePath, deletedRoot);
    }

    public IReadOnlyList<string> ListDataTypeJsonFiles(string packageRoot, PlayerExportsDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
        {
            return Array.Empty<string>();
        }

        var root = Path.GetFullPath(packageRoot);
        return GetDataTypeCandidateFiles(root, dataType)
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(file => Path.GetRelativePath(root, file), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public DeletedFilesMoveResult MoveDataTypeToDeleted(string packageRoot, PlayerExportsDataType dataType)
    {
        ValidateEditablePackageRoot(packageRoot);
        var files = ListDataTypeJsonFiles(packageRoot, dataType);
        foreach (var file in files) ValidateEditableFile(packageRoot, file);
        if (files.Count == 0)
        {
            return new DeletedFilesMoveResult { DeletedRoot = "", MovedFiles = Array.Empty<string>() };
        }

        var deletedRoot = CreateUniqueDeletedRoot(packageRoot);
        var movedFiles = new List<string>();
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                movedFiles.Add(MoveJsonFileToDeletedRoot(packageRoot, file, deletedRoot));
            }
        }

        return new DeletedFilesMoveResult { DeletedRoot = deletedRoot, MovedFiles = movedFiles };
    }

    public string FormatJson(string json)
    {
        return _json.ToIndentedJson(_json.ParseNode(json));
    }

    private static string ValidateEditablePlayerExportsRoot(string playerExportsRoot)
    {
        if (string.IsNullOrWhiteSpace(playerExportsRoot) || !Path.IsPathRooted(playerExportsRoot))
            throw new ArgumentException("PlayerExports root must be absolute.", nameof(playerExportsRoot));

        var full = Path.GetFullPath(playerExportsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Directory.GetParent(full);
        if (!string.Equals(Path.GetFileName(full), "PlayerExports", StringComparison.OrdinalIgnoreCase)
            || parent == null || !string.Equals(parent.Name, "UserData", StringComparison.OrdinalIgnoreCase)
            || parent.Parent == null)
            throw new InvalidOperationException("Editable PlayerExports must be under an AF user-data root.");

        var expected = AnimusForgeDataPaths.GetPlayerExportsDirectory(parent.Parent.FullName);
        if (!string.Equals(full, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Editable PlayerExports root is not canonical.");
        return expected;
    }

    private static string ValidateEditablePackageRoot(string packageRoot)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Path.IsPathRooted(packageRoot))
            throw new ArgumentException("Package root must be absolute.", nameof(packageRoot));
        var full = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var exportRoot = ValidateEditablePlayerExportsRoot(Path.GetDirectoryName(full) ?? "");
        if (!string.Equals(Path.GetDirectoryName(full), exportRoot, StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(full).StartsWith(".", StringComparison.Ordinal))
            throw new InvalidOperationException("Only an active package is editable.");
        AssertNoReparse(full);
        return full;
    }

    private static void ValidateEditableFile(string packageRoot, string filePath)
    {
        var root = ValidateEditablePackageRoot(packageRoot);
        if (string.IsNullOrWhiteSpace(filePath) || !Path.IsPathRooted(filePath))
            throw new ArgumentException("Editable file path must be absolute.", nameof(filePath));
        var full = Path.GetFullPath(filePath);
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Editable file is outside the active package.");
        if (!string.Equals(Path.GetExtension(full), ".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only JSON files in an active package are editable.");
        AssertNoReparse(full);
    }

    private static void AssertNoReparse(string path)
    {
        for (DirectoryInfo? directory = new DirectoryInfo(path); directory != null; directory = directory.Parent)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Editor path crosses a reparse point.");
        }
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Editor file is a reparse point.");
    }

    private static void NormalizeRule(LoreRule rule)
    {
        rule.Id = (rule.Id ?? "").Trim();
        rule.Keywords = NormalizeList(rule.Keywords);
        rule.RagShortTexts = NormalizeList(rule.RagShortTexts);
        rule.SemanticPrototypes = NormalizeList(rule.SemanticPrototypes);
        rule.Variants ??= new List<LoreVariant>();
        rule.TextMappings ??= new List<LoreTextMapping>();

        foreach (var variant in rule.Variants)
        {
            if (variant == null)
            {
                continue;
            }

            variant.Content = (variant.Content ?? "").Trim();
            variant.When = NormalizeWhen(variant.When);
        }

        foreach (var mapping in rule.TextMappings)
        {
            if (mapping == null)
            {
                continue;
            }

            mapping.SourceText = (mapping.SourceText ?? "").Trim();
            mapping.Kind = (mapping.Kind ?? "").Trim();
            mapping.TargetId = (mapping.TargetId ?? "").Trim();
            mapping.EmptyValueText = string.IsNullOrWhiteSpace(mapping.EmptyValueText) ? null : mapping.EmptyValueText.Trim();
            mapping.TrueText = string.IsNullOrWhiteSpace(mapping.TrueText) ? null : mapping.TrueText.Trim();
            mapping.FalseText = string.IsNullOrWhiteSpace(mapping.FalseText) ? null : mapping.FalseText.Trim();
        }
    }

    private static List<string> NormalizeList(IEnumerable<string>? values)
    {
        return values?
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();
    }

    private static LoreWhen? NormalizeWhen(LoreWhen? when)
    {
        if (when == null)
        {
            return null;
        }

        when.HeroIds = NormalizeList(when.HeroIds);
        when.Cultures = NormalizeList(when.Cultures);
        when.KingdomIds = NormalizeList(when.KingdomIds);
        when.SettlementIds = NormalizeList(when.SettlementIds);
        when.Roles = NormalizeList(when.Roles);
        when.IdentityIds = NormalizeList(when.IdentityIds);
        when.SkillMin = when.SkillMin?
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value >= 0)
            .GroupBy(kv => kv.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Value), StringComparer.OrdinalIgnoreCase);

        if (when.HeroIds.Count == 0)
        {
            when.HeroIds = null;
        }

        if (when.Cultures.Count == 0)
        {
            when.Cultures = null;
        }

        if (when.KingdomIds.Count == 0)
        {
            when.KingdomIds = null;
        }

        if (when.SettlementIds.Count == 0)
        {
            when.SettlementIds = null;
        }

        if (when.Roles.Count == 0)
        {
            when.Roles = null;
        }

        if (when.IdentityIds.Count == 0)
        {
            when.IdentityIds = null;
        }

        if (when.SkillMin == null || when.SkillMin.Count == 0)
        {
            when.SkillMin = null;
        }

        if (when.HeroIds == null &&
            when.Cultures == null &&
            when.KingdomIds == null &&
            when.SettlementIds == null &&
            when.Roles == null &&
            when.IdentityIds == null &&
            !when.IsFemale.HasValue &&
            !when.IsClanLeader.HasValue &&
            when.SkillMin == null)
        {
            return null;
        }

        return when;
    }

    private static IEnumerable<string> GetDataTypeCandidateFiles(string packageRoot, PlayerExportsDataType dataType)
    {
        return dataType switch
        {
            PlayerExportsDataType.Knowledge => EnumerateJsonFiles(Path.Combine(packageRoot, "knowledge", "rules"))
                .Concat(new[] { Path.Combine(packageRoot, "knowledge", "KnowledgeRules.json") }),
            PlayerExportsDataType.PersonalityBackground => EnumerateJsonFiles(Path.Combine(packageRoot, "personality_background")),
            PlayerExportsDataType.VoiceMapping => new[] { Path.Combine(packageRoot, "voice_mapping", "VoiceMapping.json") },
            PlayerExportsDataType.EventData => EnumerateJsonFiles(Path.Combine(packageRoot, "event_data")),
            PlayerExportsDataType.UnnamedPersona => new[] { Path.Combine(packageRoot, "unnamed_persona", "UnnamedNpcProfiles.json") },
            _ => Array.Empty<string>()
        };
    }

    private static IEnumerable<string> EnumerateJsonFiles(string directory)
    {
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.json")
            : Array.Empty<string>();
    }

    private static string CreateUniqueDeletedRoot(string packageRoot)
    {
        var root = Path.GetFullPath(packageRoot);
        var basePath = Path.Combine(root, ".deleted_files", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var candidate = basePath;
        for (var i = 2; Directory.Exists(candidate); i++)
        {
            candidate = basePath + "_" + i;
        }

        Directory.CreateDirectory(candidate);
        return candidate;
    }

    private static string MoveJsonFileToDeletedRoot(string packageRoot, string filePath, string deletedRoot)
    {
        var root = Path.GetFullPath(packageRoot);
        var source = Path.GetFullPath(filePath);
        var relative = Path.GetRelativePath(root, source);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("File is outside the package root.");
        }

        if (!File.Exists(source))
        {
            throw new FileNotFoundException("JSON file was not found.", source);
        }

        var target = Path.Combine(Path.GetFullPath(deletedRoot), relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? deletedRoot);
        File.Move(source, target);
        return target;
    }

    private PlayerExportsPackageInfo BuildPackageInfo(string packagePath)
    {
        var dir = new DirectoryInfo(packagePath);
        var knowledgeDir = Path.Combine(packagePath, "knowledge", "rules");
        var personaDir = Path.Combine(packagePath, "personality_background");
        return new PlayerExportsPackageInfo
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            LastWriteTime = dir.Exists ? dir.LastWriteTime : DateTime.MinValue,
            KnowledgeRuleCount = Directory.Exists(knowledgeDir) ? Directory.EnumerateFiles(knowledgeDir, "*.json").Count() : 0,
            PersonaCount = Directory.Exists(personaDir) ? Directory.EnumerateFiles(personaDir, "*.json").Count() : 0
        };
    }

    private void LoadKnowledgeRules(PlayerExportsPackageData data)
    {
        var rulesDir = Path.Combine(data.Info.FullPath, "knowledge", "rules");
        if (Directory.Exists(rulesDir))
        {
            foreach (var file in Directory.EnumerateFiles(rulesDir, "*.json").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                data.KnowledgeRules.Add(LoadKnowledgeRule(file, data.LoadIssues));
            }
        }

        if (data.KnowledgeRules.Count == 0)
        {
            var legacyFile = Path.Combine(data.Info.FullPath, "knowledge", "KnowledgeRules.json");
            if (File.Exists(legacyFile))
            {
                LoadLegacyKnowledgeFile(legacyFile, data);
            }
        }
    }

    private KnowledgeRuleDocument LoadKnowledgeRule(string file, List<ValidationIssue> issues)
    {
        try
        {
            var raw = _json.ReadUtf8(file);
            var rule = _json.Deserialize<LoreRule>(raw);
            if (rule == null)
            {
                throw new InvalidDataException("Knowledge rule JSON did not produce a rule.");
            }

            return new KnowledgeRuleDocument { FilePath = file, RawJson = raw, Rule = rule };
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Area = "Knowledge",
                FilePath = file,
                Message = ex.Message
            });
            return new KnowledgeRuleDocument { FilePath = file, RawJson = TryReadRaw(file), Error = ex.Message };
        }
    }

    private void LoadLegacyKnowledgeFile(string file, PlayerExportsPackageData data)
    {
        try
        {
            var raw = _json.ReadUtf8(file);
            var knowledgeFile = _json.Deserialize<KnowledgeFile>(raw);
            foreach (var rule in knowledgeFile?.Rules ?? new List<LoreRule>())
            {
                var ruleJson = _json.ToIndentedJson(rule);
                data.KnowledgeRules.Add(new KnowledgeRuleDocument
                {
                    FilePath = file,
                    RawJson = ruleJson,
                    Rule = rule
                });
            }
        }
        catch (Exception ex)
        {
            data.LoadIssues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Area = "Knowledge",
                FilePath = file,
                Message = ex.Message
            });
        }
    }

    private void LoadPersonas(PlayerExportsPackageData data)
    {
        var personaDir = Path.Combine(data.Info.FullPath, "personality_background");
        if (!Directory.Exists(personaDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(personaDir, "*.json").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var raw = _json.ReadUtf8(file);
                var profile = _json.Deserialize<NpcPersonaProfile>(raw);
                data.Personas.Add(new NpcPersonaDocument { FilePath = file, RawJson = raw, Profile = profile });
            }
            catch (Exception ex)
            {
                data.LoadIssues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Area = "Persona",
                    FilePath = file,
                    Message = ex.Message
                });
                data.Personas.Add(new NpcPersonaDocument { FilePath = file, RawJson = TryReadRaw(file), Error = ex.Message });
            }
        }
    }

    private void LoadVoiceMapping(PlayerExportsPackageData data)
    {
        var file = Path.Combine(data.Info.FullPath, "voice_mapping", "VoiceMapping.json");
        if (File.Exists(file))
        {
            data.VoiceMapping = LoadJsonFile(file, "VoiceMapping", data.LoadIssues);
        }
    }

    private void LoadEventFiles(PlayerExportsPackageData data)
    {
        var eventDir = Path.Combine(data.Info.FullPath, "event_data");
        if (!Directory.Exists(eventDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(eventDir, "*.json").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            data.EventFiles.Add(LoadJsonFile(file, "EventData", data.LoadIssues));
        }
    }

    private void LoadUnnamedPersona(PlayerExportsPackageData data)
    {
        var file = Path.Combine(data.Info.FullPath, "unnamed_persona", "UnnamedNpcProfiles.json");
        if (File.Exists(file))
        {
            data.UnnamedPersona = LoadJsonFile(file, "UnnamedPersona", data.LoadIssues);
        }
    }

    private JsonFileDocument LoadJsonFile(string file, string area, List<ValidationIssue> issues)
    {
        try
        {
            var raw = _json.ReadUtf8(file);
            return new JsonFileDocument { FilePath = file, RawJson = raw, Root = _json.ParseNode(raw) };
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Area = area,
                FilePath = file,
                Message = ex.Message
            });
            return new JsonFileDocument { FilePath = file, RawJson = TryReadRaw(file), Error = ex.Message };
        }
    }

    private string TryReadRaw(string file)
    {
        try
        {
            return File.Exists(file) ? _json.ReadUtf8(file) : "";
        }
        catch
        {
            return "";
        }
    }

    private static string BuildDefaultVoiceMappingJson()
    {
        return """
               {
                 "male_young": [],
                 "male_middle": [],
                 "male_old": [],
                 "female_young": [],
                 "female_middle": [],
                 "female_old": [],
                 "fallback": ""
               }
               
               """;
    }
}
