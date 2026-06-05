namespace PlayerExportsEditor.Core;

public sealed class PlayerExportsValidator
{
    public const int RagShortTextMaxLength = 100;

    private static readonly string[] VoiceGroupKeys =
    {
        "male_young",
        "male_middle",
        "male_old",
        "female_young",
        "female_middle",
        "female_old"
    };

    public IReadOnlyList<ValidationIssue> Validate(PlayerExportsPackageData package)
    {
        var issues = new List<ValidationIssue>();
        issues.AddRange(package.LoadIssues);
        ValidateKnowledge(package, issues);
        ValidatePersonas(package, issues);
        ValidateVoiceMapping(package, issues);
        ValidateEventData(package, issues);
        return issues;
    }

    private static void ValidateKnowledge(PlayerExportsPackageData package, List<ValidationIssue> issues)
    {
        var ids = new Dictionary<string, KnowledgeRuleDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in package.KnowledgeRules)
        {
            if (!string.IsNullOrWhiteSpace(doc.Error))
            {
                continue;
            }

            var rule = doc.Rule;
            if (rule == null)
            {
                issues.Add(Error("Knowledge", doc.FilePath, "Knowledge rule is empty."));
                continue;
            }

            var id = (rule.Id ?? "").Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(Error("Knowledge", doc.FilePath, "RuleId is empty."));
            }
            else if (ids.TryGetValue(id, out var existing))
            {
                issues.Add(Error("Knowledge", doc.FilePath, "Duplicate RuleId: " + id + " also exists in " + existing.FileName + "."));
            }
            else
            {
                ids[id] = doc;
            }

            var ragShortTexts = rule.RagShortTexts?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>();
            if (ragShortTexts.Count == 0)
            {
                issues.Add(Error("Knowledge", doc.FilePath, "RagShortTexts is empty. Game export/import validation requires at least one short RAG text."));
            }

            foreach (var text in ragShortTexts)
            {
                if (text.Length > RagShortTextMaxLength)
                {
                    issues.Add(Error("Knowledge", doc.FilePath, "RagShortText exceeds " + RagShortTextMaxLength + " characters: " + text));
                }
            }

            ValidateDuplicateVariantConditions(rule, doc, issues);

            if (rule.Variants == null || rule.Variants.Count == 0)
            {
                issues.Add(Warning("Knowledge", doc.FilePath, "Rule has no variants."));
            }
            else
            {
                for (var i = 0; i < rule.Variants.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(rule.Variants[i]?.Content))
                    {
                        issues.Add(Warning("Knowledge", doc.FilePath, "Variant #" + (i + 1) + " has empty content."));
                    }
                }
            }

            foreach (var mapping in rule.TextMappings ?? new List<LoreTextMapping>())
            {
                if (string.IsNullOrWhiteSpace(mapping.SourceText) || string.IsNullOrWhiteSpace(mapping.Kind))
                {
                    issues.Add(Warning("Knowledge", doc.FilePath, "TextMapping has empty SourceText or Kind."));
                }
            }
        }
    }

    private static void ValidateDuplicateVariantConditions(LoreRule rule, KnowledgeRuleDocument doc, List<ValidationIssue> issues)
    {
        var signatures = new Dictionary<string, int>(StringComparer.Ordinal);
        var variants = rule.Variants ?? new List<LoreVariant>();
        for (var i = 0; i < variants.Count; i++)
        {
            var signature = BuildWhenSignature(variants[i]?.When);
            if (signatures.TryGetValue(signature, out var previous))
            {
                issues.Add(Error("Knowledge", doc.FilePath, "Duplicate variant condition: #" + (previous + 1) + " and #" + (i + 1) + "."));
                continue;
            }

            signatures[signature] = i;
        }
    }

    private static string BuildWhenSignature(LoreWhen? when)
    {
        var normalized = NormalizeWhen(when);
        if (normalized == null)
        {
            return "__generic__";
        }

        var hero = string.Join("|", NormalizeStringList(normalized.HeroIds));
        var culture = string.Join("|", NormalizeStringList(normalized.Cultures));
        var kingdom = string.Join("|", NormalizeStringList(normalized.KingdomIds));
        var settlement = string.Join("|", NormalizeStringList(normalized.SettlementIds));
        var role = string.Join("|", NormalizeStringList(normalized.Roles));
        var identity = string.Join("|", NormalizeStringList(normalized.IdentityIds));
        var gender = normalized.IsFemale.HasValue ? normalized.IsFemale.Value ? "female" : "male" : "any";
        var clan = normalized.IsClanLeader.HasValue ? normalized.IsClanLeader.Value ? "leader" : "not_leader" : "any";
        var skill = string.Join("|", NormalizeSkillMin(normalized.SkillMin).Select(kv => kv.Key + ":" + kv.Value));
        return $"hero={hero};culture={culture};kingdom={kingdom};settlement={settlement};role={role};identity={identity};gender={gender};clan={clan};skill={skill}";
    }

    private static LoreWhen? NormalizeWhen(LoreWhen? when)
    {
        if (when == null)
        {
            return null;
        }

        var normalized = new LoreWhen
        {
            HeroIds = NormalizeStringList(when.HeroIds),
            Cultures = NormalizeStringList(when.Cultures),
            KingdomIds = NormalizeStringList(when.KingdomIds),
            SettlementIds = NormalizeStringList(when.SettlementIds),
            Roles = NormalizeStringList(when.Roles),
            IdentityIds = NormalizeStringList(when.IdentityIds),
            IsFemale = when.IsFemale,
            IsClanLeader = when.IsClanLeader,
            SkillMin = NormalizeSkillMin(when.SkillMin).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
        };

        if (normalized.HeroIds.Count == 0)
        {
            normalized.HeroIds = null;
        }

        if (normalized.Cultures.Count == 0)
        {
            normalized.Cultures = null;
        }

        if (normalized.KingdomIds.Count == 0)
        {
            normalized.KingdomIds = null;
        }

        if (normalized.SettlementIds.Count == 0)
        {
            normalized.SettlementIds = null;
        }

        if (normalized.Roles.Count == 0)
        {
            normalized.Roles = null;
        }

        if (normalized.IdentityIds.Count == 0)
        {
            normalized.IdentityIds = null;
        }

        if (normalized.SkillMin.Count == 0)
        {
            normalized.SkillMin = null;
        }

        if (normalized.HeroIds == null &&
            normalized.Cultures == null &&
            normalized.KingdomIds == null &&
            normalized.SettlementIds == null &&
            normalized.Roles == null &&
            normalized.IdentityIds == null &&
            !normalized.IsFemale.HasValue &&
            !normalized.IsClanLeader.HasValue &&
            normalized.SkillMin == null)
        {
            return null;
        }

        return normalized;
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values)
    {
        return values?
            .Select(x => (x ?? "").Trim().ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList() ?? new List<string>();
    }

    private static List<KeyValuePair<string, int>> NormalizeSkillMin(Dictionary<string, int>? skillMin)
    {
        return skillMin?
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value >= 0)
            .Select(kv => new KeyValuePair<string, int>((kv.Key ?? "").Trim().ToLowerInvariant(), kv.Value))
            .GroupBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Max(x => x.Value)))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList() ?? new List<KeyValuePair<string, int>>();
    }

    private static void ValidatePersonas(PlayerExportsPackageData package, List<ValidationIssue> issues)
    {
        foreach (var doc in package.Personas)
        {
            if (!string.IsNullOrWhiteSpace(doc.Error))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(doc.EntityId))
            {
                issues.Add(Warning("Persona", doc.FilePath, "File name does not use the expected Id__Name.json pattern."));
            }

            var profile = doc.Profile;
            if (profile == null)
            {
                issues.Add(Error("Persona", doc.FilePath, "Persona profile is empty."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(profile.Personality) && string.IsNullOrWhiteSpace(profile.Background))
            {
                issues.Add(Warning("Persona", doc.FilePath, "Both Personality and Background are empty."));
            }
        }
    }

    private static void ValidateVoiceMapping(PlayerExportsPackageData package, List<ValidationIssue> issues)
    {
        var doc = package.VoiceMapping;
        if (doc == null)
        {
            issues.Add(Warning("VoiceMapping", null, "voice_mapping/VoiceMapping.json is missing."));
            return;
        }

        if (doc.Root is not System.Text.Json.Nodes.JsonObject obj)
        {
            issues.Add(Error("VoiceMapping", doc.FilePath, "VoiceMapping root must be a JSON object."));
            return;
        }

        var allVoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in VoiceGroupKeys)
        {
            if (obj[key] is not System.Text.Json.Nodes.JsonArray array)
            {
                issues.Add(Warning("VoiceMapping", doc.FilePath, key + " is missing or not an array."));
                continue;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in array)
            {
                var voice = node?.GetValue<string>()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(voice))
                {
                    issues.Add(Warning("VoiceMapping", doc.FilePath, key + " contains an empty voice id."));
                }
                else if (!seen.Add(voice))
                {
                    issues.Add(Warning("VoiceMapping", doc.FilePath, key + " contains duplicated voice id: " + voice));
                }
                else
                {
                    allVoices.Add(voice);
                }
            }
        }

        var fallback = obj["fallback"]?.GetValue<string>()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(fallback))
        {
            issues.Add(Warning("VoiceMapping", doc.FilePath, "fallback is empty."));
        }
        else if (!allVoices.Contains(fallback))
        {
            issues.Add(Warning("VoiceMapping", doc.FilePath, "fallback is not present in any voice group: " + fallback));
        }
    }

    private static void ValidateEventData(PlayerExportsPackageData package, List<ValidationIssue> issues)
    {
        if (package.EventFiles.Count == 0)
        {
            issues.Add(Warning("EventData", null, "event_data/*.json is missing."));
            return;
        }

        foreach (var doc in package.EventFiles)
        {
            if (!string.IsNullOrWhiteSpace(doc.Error))
            {
                continue;
            }

            if (doc.FileName.Equals("WorldOpeningSummary.json", StringComparison.OrdinalIgnoreCase) &&
                doc.Root is System.Text.Json.Nodes.JsonObject obj &&
                string.IsNullOrWhiteSpace(obj["Summary"]?.GetValue<string>()))
            {
                issues.Add(Warning("EventData", doc.FilePath, "WorldOpeningSummary.Summary is empty."));
            }
        }
    }

    private static ValidationIssue Error(string area, string? filePath, string message)
    {
        return new ValidationIssue { Severity = ValidationSeverity.Error, Area = area, FilePath = filePath, Message = message };
    }

    private static ValidationIssue Warning(string area, string? filePath, string message)
    {
        return new ValidationIssue { Severity = ValidationSeverity.Warning, Area = area, FilePath = filePath, Message = message };
    }
}
