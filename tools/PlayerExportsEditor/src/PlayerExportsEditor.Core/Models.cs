using System.Text.Json.Nodes;

namespace PlayerExportsEditor.Core;

public sealed class PlayerExportsPackageInfo
{
    public required string Name { get; init; }

    public required string FullPath { get; init; }

    public DateTime LastWriteTime { get; init; }

    public int KnowledgeRuleCount { get; init; }

    public int PersonaCount { get; init; }

    public override string ToString() => Name;
}

public sealed class PlayerExportsPackageData
{
    public required PlayerExportsPackageInfo Info { get; init; }

    public List<KnowledgeRuleDocument> KnowledgeRules { get; } = new();

    public List<NpcPersonaDocument> Personas { get; } = new();

    public List<JsonFileDocument> EventFiles { get; } = new();

    public JsonFileDocument? VoiceMapping { get; set; }

    public JsonFileDocument? UnnamedPersona { get; set; }

    public List<ValidationIssue> LoadIssues { get; } = new();
}

public sealed class KnowledgeRuleDocument
{
    public required string FilePath { get; init; }

    public required string RawJson { get; init; }

    public LoreRule? Rule { get; init; }

    public string? Error { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    public string RuleId => Rule?.Id?.Trim() ?? "";

    public string FirstKeyword => Rule?.Keywords?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";

    public int RagShortTextCount => Rule?.RagShortTexts?.Count(x => !string.IsNullOrWhiteSpace(x)) ?? 0;

    public int VariantCount => Rule?.Variants?.Count ?? 0;

    public int TextMappingCount => Rule?.TextMappings?.Count ?? 0;
}

public sealed class NpcPersonaDocument
{
    public required string FilePath { get; init; }

    public required string RawJson { get; init; }

    public NpcPersonaProfile? Profile { get; init; }

    public string? Error { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    public string EntityId => FileNameHelper.TryParseIdFromDataFileName(FilePath) ?? "";

    public string DisplayName => FileNameHelper.TryParseDisplayNameFromDataFileName(FilePath) ?? "";
}

public sealed class JsonFileDocument
{
    public required string FilePath { get; init; }

    public required string RawJson { get; init; }

    public JsonNode? Root { get; init; }

    public string? Error { get; init; }

    public string FileName => Path.GetFileName(FilePath);
}

public sealed class ValidationIssue
{
    public required ValidationSeverity Severity { get; init; }

    public required string Area { get; init; }

    public required string Message { get; init; }

    public string? FilePath { get; init; }

    public string FileName => string.IsNullOrWhiteSpace(FilePath) ? "" : Path.GetFileName(FilePath);
}

public sealed class DeletedFilesMoveResult
{
    public required string DeletedRoot { get; init; }

    public required IReadOnlyList<string> MovedFiles { get; init; }
}

public enum PlayerExportsDataType
{
    Knowledge,
    PersonalityBackground,
    VoiceMapping,
    EventData,
    UnnamedPersona
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed class KnowledgeFile
{
    public int Version { get; set; } = 1;

    public string? PlayerAppearance { get; set; } = "";

    public List<LoreRule>? Rules { get; set; } = new();
}

public sealed class LoreRule
{
    public string? Id { get; set; }

    public List<string>? Keywords { get; set; } = new();

    public List<string>? RagShortTexts { get; set; } = new();

    public List<string>? SemanticPrototypes { get; set; } = new();

    public List<LoreVariant>? Variants { get; set; } = new();

    public List<LoreTextMapping>? TextMappings { get; set; } = new();
}

public sealed class LoreVariant
{
    public int Priority { get; set; }

    public LoreWhen? When { get; set; }

    public string? Content { get; set; }
}

public sealed class LoreWhen
{
    public List<string>? HeroIds { get; set; }

    public List<string>? Cultures { get; set; }

    public List<string>? KingdomIds { get; set; }

    public List<string>? SettlementIds { get; set; }

    public List<string>? Roles { get; set; }

    public List<string>? IdentityIds { get; set; }

    public bool? IsFemale { get; set; }

    public bool? IsClanLeader { get; set; }

    public Dictionary<string, int>? SkillMin { get; set; }
}

public sealed class LoreTextMapping
{
    public string? SourceText { get; set; }

    public string? Kind { get; set; }

    public string? TargetId { get; set; }

    public int? AgeMin { get; set; }

    public int? AgeMax { get; set; }

    public string? EmptyValueText { get; set; }

    public string? TrueText { get; set; }

    public string? FalseText { get; set; }
}

public sealed class NpcPersonaProfile
{
    public string? Personality { get; set; }

    public string? Background { get; set; }

    public string? VoiceId { get; set; }
}
