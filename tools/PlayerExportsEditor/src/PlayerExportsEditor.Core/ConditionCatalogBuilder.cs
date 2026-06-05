using System.Xml.Linq;

namespace PlayerExportsEditor.Core;

public sealed class ConditionCatalogBuilder
{
    private static readonly HashSet<string> OfficialModuleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Native",
        "SandBox",
        "SandBoxCore",
        "StoryMode",
        "CustomBattle",
        "BirthAndDeath",
        "Multiplayer",
        "NavalDLC"
    };

    private readonly CandidateSet _heroes = new();
    private readonly CandidateSet _cultures = new();
    private readonly CandidateSet _kingdoms = new();
    private readonly CandidateSet _clans = new();
    private readonly CandidateSet _settlements = new();
    private readonly CandidateSet _roles = new();
    private readonly CandidateSet _identities = new();
    private readonly CandidateSet _skills = new();
    private readonly HashSet<string> _sourceRoots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);
    private int _xmlFileCount;

    public ConditionCatalog Build(
        PlayerExportsPackageData? package,
        string? appBaseDirectory = null,
        ConditionCatalogBuildOptions? options = null)
    {
        options ??= new ConditionCatalogBuildOptions();

        AddBuiltInRoles();
        AddBuiltInSkills();
        AddPackageValues(package);

        if (options.IncludePackagedCatalog)
        {
            AddPackagedCatalog(appBaseDirectory);
        }

        var seeds = new[]
        {
            package?.Info.FullPath,
            appBaseDirectory,
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        if (options.IncludeLooseModuleData)
        {
            foreach (var moduleData in FindLooseModuleDataDirectories(seeds))
            {
                ScanModuleDataDirectory(moduleData);
            }
        }

        foreach (var modulesRoot in options.ExtraModulesRoots ?? Array.Empty<string>())
        {
            ScanModulesRoot(modulesRoot, options);
        }

        foreach (var modulesRoot in FindBannerlordModulesDirectories(seeds))
        {
            ScanModulesRoot(modulesRoot, options);
        }

        return new ConditionCatalog
        {
            Heroes = _heroes.ToList(),
            Cultures = _cultures.ToList(),
            Kingdoms = _kingdoms.ToList(),
            Clans = _clans.ToList(),
            Settlements = _settlements.ToList(),
            Roles = _roles.ToList(),
            Identities = _identities.ToList(),
            Skills = _skills.ToList(),
            SourceRoots = _sourceRoots.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            XmlFileCount = _xmlFileCount
        };
    }

    private void AddPackagedCatalog(string? appBaseDirectory)
    {
        var loaded = ConditionCatalogStore.LoadDefault(appBaseDirectory);
        if (loaded == null)
        {
            return;
        }

        AddCatalogValues(loaded.Value.Catalog, "offline");
        _sourceRoots.Add(loaded.Value.Path);
    }

    private void AddBuiltInRoles()
    {
        AddRole("lord", "\\u9886\\u4e3b");
        AddRole("notable", "\\u8981\\u4eba");
        AddRole("wanderer", "\\u6d41\\u6d6a\\u8005");
        AddRole("soldier", "\\u58eb\\u5175");
        AddRole("villager", "\\u6751\\u6c11");
        AddRole("townsfolk", "\\u9547\\u6c11");
        AddRole("commoner", "\\u672a\\u5206\\u7c7b\\u5bf9\\u8c61");
    }

    private void AddRole(string id, string escapedLabel)
    {
        _roles.Add(id, DecodeEscapedLabel(escapedLabel), "knowledge");
    }

    private void AddBuiltInSkills()
    {
        AddSkill("OneHanded", "\\u5355\\u624b");
        AddSkill("TwoHanded", "\\u53cc\\u624b");
        AddSkill("Polearm", "\\u957f\\u6746");
        AddSkill("Bow", "\\u5f13");
        AddSkill("Crossbow", "\\u5f29");
        AddSkill("Throwing", "\\u6295\\u63b7");
        AddSkill("Riding", "\\u9a91\\u672f");
        AddSkill("Athletics", "\\u8dd1\\u52a8");
        AddSkill("Crafting", "\\u953b\\u9020");
        AddSkill("Tactics", "\\u6218\\u672f");
        AddSkill("Scouting", "\\u4fa6\\u5bdf");
        AddSkill("Roguery", "\\u6d41\\u6c13\\u4e60\\u6c14");
        AddSkill("Leadership", "\\u7edf\\u5fa1");
        AddSkill("Charm", "\\u9b45\\u529b");
        AddSkill("Trade", "\\u4ea4\\u6613");
        AddSkill("Steward", "\\u7ba1\\u7406");
        AddSkill("Medicine", "\\u533b\\u672f");
        AddSkill("Engineering", "\\u5de5\\u7a0b");
    }

    private void AddSkill(string id, string escapedLabel)
    {
        _skills.Add(id, DecodeEscapedLabel(escapedLabel), "knowledge");
    }

    private void AddPackageValues(PlayerExportsPackageData? package)
    {
        if (package == null)
        {
            return;
        }

        foreach (var rule in package.KnowledgeRules.Select(x => x.Rule).Where(x => x != null))
        {
            foreach (var variant in rule?.Variants ?? new List<LoreVariant>())
            {
                var when = variant?.When;
                if (when == null)
                {
                    continue;
                }

                AddAll(_heroes, when.HeroIds, "package");
                AddAll(_cultures, when.Cultures, "package");
                AddAll(_kingdoms, when.KingdomIds, "package");
                AddAll(_settlements, when.SettlementIds, "package");
                AddAll(_roles, when.Roles, "package");
                AddAll(_identities, when.IdentityIds, "package");
                if (when.SkillMin != null)
                {
                    AddAll(_skills, when.SkillMin.Keys, "package");
                }
            }
        }

        foreach (var persona in package.Personas)
        {
            var id = (persona.EntityId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var label = string.IsNullOrWhiteSpace(persona.DisplayName) ? id : persona.DisplayName.Trim();
            _heroes.Add(id, label, "package");
            _identities.Add("hero:" + id.ToLowerInvariant(), label, "package", "hero");
        }
    }

    private static void AddAll(CandidateSet set, IEnumerable<string>? values, string source)
    {
        if (values == null)
        {
            return;
        }

        foreach (var value in values)
        {
            set.Add(value, "", source);
        }
    }

    private void AddCatalogValues(ConditionCatalog catalog, string source)
    {
        if (catalog == null)
        {
            return;
        }

        AddAll(_heroes, catalog.Heroes?.Select(x => x.Id), source);
        AddAll(_cultures, catalog.Cultures?.Select(x => x.Id), source);
        AddAll(_kingdoms, catalog.Kingdoms?.Select(x => x.Id), source);
        AddAll(_clans, catalog.Clans?.Select(x => x.Id), source);
        AddAll(_settlements, catalog.Settlements?.Select(x => x.Id), source);
        AddAll(_roles, catalog.Roles?.Select(x => x.Id), source);
        AddAll(_identities, catalog.Identities?.Select(x => x.Id), source);
        AddAll(_skills, catalog.Skills?.Select(x => x.Id), source);

        AddCandidates(_heroes, catalog.Heroes, source);
        AddCandidates(_cultures, catalog.Cultures, source);
        AddCandidates(_kingdoms, catalog.Kingdoms, source);
        AddCandidates(_clans, catalog.Clans, source);
        AddCandidates(_settlements, catalog.Settlements, source);
        AddCandidates(_roles, catalog.Roles, source);
        AddCandidates(_identities, catalog.Identities, source);
        AddCandidates(_skills, catalog.Skills, source);
    }

    private static void AddCandidates(CandidateSet set, IEnumerable<ConditionCandidate>? candidates, string source)
    {
        if (candidates == null)
        {
            return;
        }

        foreach (var candidate in candidates)
        {
            set.Add(candidate.Id, candidate.Label, source, candidate.Role);
        }
    }

    private void ScanModulesRoot(string modulesRoot, ConditionCatalogBuildOptions options)
    {
        if (!Directory.Exists(modulesRoot))
        {
            return;
        }

        _sourceRoots.Add(modulesRoot);
        foreach (var moduleData in Directory.EnumerateDirectories(modulesRoot)
                     .Where(x => ShouldScanModuleDirectory(x, options))
                     .Select(x => Path.Combine(x, "ModuleData"))
                     .Where(Directory.Exists)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            ScanModuleDataDirectory(moduleData);
        }
    }

    private static bool ShouldScanModuleDirectory(string moduleDirectory, ConditionCatalogBuildOptions options)
    {
        if (!options.OfficialModulesOnly)
        {
            return true;
        }

        var name = Path.GetFileName(moduleDirectory);
        return OfficialModuleNames.Contains(name);
    }

    private void ScanModuleDataDirectory(string moduleData)
    {
        if (!Directory.Exists(moduleData))
        {
            return;
        }

        _sourceRoots.Add(moduleData);
        LoadTranslations(moduleData);
        foreach (var file in Directory.EnumerateFiles(moduleData, "*.xml", SearchOption.AllDirectories)
                     .Where(ShouldScanXml)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            ScanXmlFile(file);
        }
    }

    private void ScanXmlFile(string file)
    {
        try
        {
            var doc = XDocument.Load(file, LoadOptions.None);
            var source = GuessSourceName(file);
            foreach (var element in doc.Descendants())
            {
                ScanElement(element, source, file);
            }

            _xmlFileCount++;
        }
        catch
        {
        }
    }

    private void ScanElement(XElement element, string source, string file)
    {
        var name = element.Name.LocalName;
        if (name.Equals("Hero", StringComparison.OrdinalIgnoreCase))
        {
            var id = CleanId(ReadAttribute(element, "id"));
            _heroes.Add(id, CleanText(ReadAttribute(element, "name")), source);
            if (!string.IsNullOrWhiteSpace(id))
            {
                _identities.Add("hero:" + id.ToLowerInvariant(), CleanText(ReadAttribute(element, "name")), source, "hero");
            }
            return;
        }

        if (name.Equals("NPCCharacter", StringComparison.OrdinalIgnoreCase))
        {
            AddCharacter(element, source);
            return;
        }

        if (name.Equals("Culture", StringComparison.OrdinalIgnoreCase))
        {
            _cultures.Add(CleanId(ReadAttribute(element, "id")), CleanText(ReadAttribute(element, "name")), source);
            return;
        }

        if (name.Equals("Kingdom", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Faction", StringComparison.OrdinalIgnoreCase))
        {
            var id = CleanId(ReadAttribute(element, "id"));
            var label = CleanText(ReadAttribute(element, "name"));
            _kingdoms.Add(id, label, source);
            if (IsClanDataFile(file))
            {
                _clans.Add(id, label, source);
            }

            return;
        }

        if (name.Equals("Clan", StringComparison.OrdinalIgnoreCase))
        {
            var id = CleanId(ReadAttribute(element, "id"));
            var label = CleanText(ReadAttribute(element, "name"));
            _kingdoms.Add(id, label, source);
            _clans.Add(id, label, source);
            return;
        }

        if (name.Equals("Settlement", StringComparison.OrdinalIgnoreCase))
        {
            _settlements.Add(CleanId(ReadAttribute(element, "id")), CleanText(ReadAttribute(element, "name")), source);
            return;
        }

        if (name.Equals("Skill", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("skill", StringComparison.Ordinal))
        {
            _skills.Add(CleanId(ReadAttribute(element, "id")), CleanText(ReadAttribute(element, "name")), source);
        }
    }

    private void AddCharacter(XElement element, string source)
    {
        var id = CleanId(ReadAttribute(element, "id"));
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var label = CleanText(ReadAttribute(element, "name"));
        var role = RoleFromCharacter(element);
        var isHero = IsTrue(ReadAttribute(element, "is_hero"));
        if (isHero)
        {
            _heroes.Add(id, label, source);
            _identities.Add("hero:" + id.ToLowerInvariant(), label, source, role);
        }
        else
        {
            _identities.Add("char:" + id.ToLowerInvariant(), label, source, role);
        }

        var culture = CleanId(ReadAttribute(element, "culture"));
        _cultures.Add(culture, "", source);

        foreach (var skill in element.Descendants().Where(x => x.Name.LocalName.Equals("skill", StringComparison.OrdinalIgnoreCase)))
        {
            _skills.Add(CleanId(ReadAttribute(skill, "id")), "", source);
        }
    }

    private static string RoleFromCharacter(XElement element)
    {
        var occupation = (ReadAttribute(element, "occupation") ?? "").Trim();
        if (occupation.Equals("Lord", StringComparison.OrdinalIgnoreCase))
        {
            return "lord";
        }

        if (occupation.Equals("Wanderer", StringComparison.OrdinalIgnoreCase))
        {
            return "wanderer";
        }

        if (occupation.Equals("Soldier", StringComparison.OrdinalIgnoreCase))
        {
            return "soldier";
        }

        if (occupation.Equals("Villager", StringComparison.OrdinalIgnoreCase))
        {
            return "villager";
        }

        if (occupation.Equals("Townsfolk", StringComparison.OrdinalIgnoreCase))
        {
            return "townsfolk";
        }

        if (occupation.Contains("Notable", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("Merchant", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("Artisan", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("GangLeader", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("Preacher", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("RuralNotable", StringComparison.OrdinalIgnoreCase) ||
            occupation.Equals("Headman", StringComparison.OrdinalIgnoreCase))
        {
            return "notable";
        }

        return "commoner";
    }

    private static IEnumerable<string> FindLooseModuleDataDirectories(IEnumerable<string?> seeds)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ancestor in EnumerateAncestors(seeds))
        {
            var candidate = Path.Combine(ancestor, "AnimusForge", "ModuleData");
            if (Directory.Exists(candidate) && seen.Add(Path.GetFullPath(candidate)))
            {
                yield return Path.GetFullPath(candidate);
            }
        }
    }

    private static IEnumerable<string> FindBannerlordModulesDirectories(IEnumerable<string?> seeds)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ancestor in EnumerateAncestors(seeds))
        {
            foreach (var candidate in new[]
                     {
                         ancestor,
                         Path.Combine(ancestor, "Modules"),
                         Path.Combine(ancestor, "Mount & Blade II Bannerlord", "Modules")
                     })
            {
                if (IsBannerlordModulesDirectory(candidate) && seen.Add(Path.GetFullPath(candidate)))
                {
                    yield return Path.GetFullPath(candidate);
                }
            }
        }

        foreach (var candidate in KnownSteamModulePaths())
        {
            if (IsBannerlordModulesDirectory(candidate) && seen.Add(Path.GetFullPath(candidate)))
            {
                yield return Path.GetFullPath(candidate);
            }
        }
    }

    private static IEnumerable<string> EnumerateAncestors(IEnumerable<string?> seeds)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in seeds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var current = File.Exists(seed) ? Path.GetDirectoryName(seed) : seed;
            while (!string.IsNullOrWhiteSpace(current))
            {
                string full;
                try
                {
                    full = Path.GetFullPath(current);
                }
                catch
                {
                    break;
                }

                if (seen.Add(full))
                {
                    yield return full;
                }

                var parent = Directory.GetParent(full);
                if (parent == null || string.Equals(parent.FullName, full, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent.FullName;
            }
        }
    }

    private static IEnumerable<string> KnownSteamModulePaths()
    {
        var game = Path.Combine("steamapps", "common", "Mount & Blade II Bannerlord", "Modules");
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady))
        {
            yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", game);
            yield return Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", game);
            yield return Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", game);
        }
    }

    private static bool IsBannerlordModulesDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        return Directory.Exists(Path.Combine(path, "Native")) &&
               Directory.Exists(Path.Combine(path, "SandBox")) &&
               Directory.Exists(Path.Combine(path, "SandBoxCore"));
    }

    private static bool ShouldScanXml(string file)
    {
        var normalized = file.Replace('/', Path.DirectorySeparatorChar);
        var lang = Path.DirectorySeparatorChar + "Languages" + Path.DirectorySeparatorChar;
        if (normalized.IndexOf(lang, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        var name = Path.GetFileName(file);
        return !name.Equals("SubModule.xml", StringComparison.OrdinalIgnoreCase) &&
               !name.Equals("language_data.xml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClanDataFile(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file ?? "");
        return name.Contains("clan", StringComparison.OrdinalIgnoreCase);
    }

    private static string GuessSourceName(string file)
    {
        try
        {
            var dir = new DirectoryInfo(file);
            while (dir.Parent != null)
            {
                if (dir.Parent.Name.Equals("Modules", StringComparison.OrdinalIgnoreCase))
                {
                    return dir.Name;
                }

                dir = dir.Parent;
            }
        }
        catch
        {
        }

        return Path.GetFileNameWithoutExtension(file);
    }

    private void LoadTranslations(string moduleData)
    {
        var languages = Path.Combine(moduleData, "Languages");
        if (!Directory.Exists(languages))
        {
            return;
        }

        foreach (var languageDir in new[] { "CNs", "CNt" })
        {
            var dir = Path.Combine(languages, languageDir);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                LoadTranslationFile(file);
            }
        }
    }

    private void LoadTranslationFile(string file)
    {
        try
        {
            var doc = XDocument.Load(file, LoadOptions.None);
            foreach (var element in doc.Descendants().Where(x => x.Name.LocalName.Equals("string", StringComparison.OrdinalIgnoreCase)))
            {
                var id = ReadAttribute(element, "id").Trim();
                var text = ReadAttribute(element, "text").Trim();
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(text) && !_translations.ContainsKey(id))
                {
                    _translations[id] = text.Replace("\r", " ").Replace("\n", " ").Trim();
                }
            }
        }
        catch
        {
        }
    }

    private static string ReadAttribute(XElement element, string name)
    {
        return element.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
    }

    private static bool IsTrue(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanId(string? value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        foreach (var prefix in new[] { "Hero.", "NPCCharacter.", "Culture.", "Kingdom.", "Settlement.", "Faction.", "Clan.", "Skill.", "SkillObject." })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return text[prefix.Length..].Trim();
            }
        }

        return text;
    }

    private string CleanText(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.StartsWith("{=", StringComparison.Ordinal))
        {
            var end = text.IndexOf('}');
            if (end >= 3)
            {
                var key = text[2..end].Trim();
                if (_translations.TryGetValue(key, out var translated) && !string.IsNullOrWhiteSpace(translated))
                {
                    return translated.Replace("\r", " ").Replace("\n", " ").Trim();
                }

                text = end < text.Length - 1 ? text[(end + 1)..].Trim() : "";
            }
        }

        return text.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string DecodeEscapedLabel(string value)
    {
        return System.Text.RegularExpressions.Regex.Unescape(value);
    }

    private sealed class CandidateSet
    {
        private readonly Dictionary<string, ConditionCandidate> _values = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string? id, string? label, string source, string role = "")
        {
            var cleanId = (id ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cleanId))
            {
                return;
            }

            var cleanLabel = (label ?? "").Trim();
            if (_values.TryGetValue(cleanId, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.Label) && !string.IsNullOrWhiteSpace(cleanLabel))
                {
                    _values[cleanId] = new ConditionCandidate
                    {
                        Id = existing.Id,
                        Label = cleanLabel,
                        Source = existing.Source,
                        Role = string.IsNullOrWhiteSpace(existing.Role) ? role : existing.Role
                    };
                }

                return;
            }

            _values[cleanId] = new ConditionCandidate
            {
                Id = cleanId,
                Label = cleanLabel,
                Source = source,
                Role = role
            };
        }

        public IReadOnlyList<ConditionCandidate> ToList()
        {
            return _values.Values
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
