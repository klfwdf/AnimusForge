namespace PlayerExportsEditor.Core;

public sealed class ConditionCandidate
{
    public required string Id { get; init; }

    public string Label { get; init; } = "";

    public string Source { get; init; } = "";

    public string Role { get; init; } = "";

    public override string ToString()
    {
        var label = (Label ?? "").Trim();
        var role = RoleLabel(Role);
        var roleText = string.IsNullOrWhiteSpace(role) ? "" : " [" + role + "]";
        if (!string.IsNullOrWhiteSpace(label) && !string.Equals(label, Id, StringComparison.OrdinalIgnoreCase))
        {
            return label + " (" + Id + ")" + roleText;
        }

        return Id + roleText;
    }

    private static string RoleLabel(string role)
    {
        return (role ?? "").Trim().ToLowerInvariant() switch
        {
            "hero" => "\u82f1\u96c4",
            "char" => "\u89d2\u8272",
            "lord" => "\u9886\u4e3b",
            "notable" => "\u8981\u4eba",
            "wanderer" => "\u6d41\u6d6a\u8005",
            "soldier" => "\u58eb\u5175",
            "villager" => "\u6751\u6c11",
            "townsfolk" => "\u9547\u6c11",
            "commoner" => "\u672a\u5206\u7c7b",
            _ => (role ?? "").Trim()
        };
    }
}

public sealed class ConditionCatalog
{
    public static ConditionCatalog Empty { get; } = new();

    public IReadOnlyList<ConditionCandidate> Heroes { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Cultures { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Kingdoms { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Clans { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Settlements { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Roles { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Identities { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<ConditionCandidate> Skills { get; init; } = Array.Empty<ConditionCandidate>();

    public IReadOnlyList<string> SourceRoots { get; init; } = Array.Empty<string>();

    public int XmlFileCount { get; init; }

    public string Summary =>
        "Hero " + Heroes.Count +
        " / Culture " + Cultures.Count +
        " / Kingdom " + Kingdoms.Count +
        " / Clan " + Clans.Count +
        " / Settlement " + Settlements.Count +
        " / Identity " + Identities.Count +
        " / Skill " + Skills.Count +
        " / XML " + XmlFileCount;
}
