namespace PlayerExportsEditor.Core;

public static class FileNameHelper
{
    public static string SanitizeFileNamePart(string value, string fallback, int maxLength)
    {
        var result = (value ?? "").Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalid, '_');
        }

        if (result.Length > maxLength)
        {
            result = result[..maxLength];
        }

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    public static string BuildKnowledgeRuleFileName(LoreRule rule)
    {
        var id = SanitizeFileNamePart(rule.Id ?? "", "rule", 80);
        var firstKeyword = rule.Keywords?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
        var keyword = SanitizeFileNamePart(firstKeyword, "", 35);
        return string.IsNullOrWhiteSpace(keyword) ? id + ".json" : id + "__" + keyword + ".json";
    }

    public static string BuildNpcDataFileName(string entityId, string displayName)
    {
        var id = SanitizeFileNamePart(entityId, "unknown", 120);
        var name = SanitizeFileNamePart(displayName, "NPC", 80);
        return id + "__" + name + ".json";
    }

    public static string? TryParseIdFromDataFileName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath) ?? "";
        var marker = name.IndexOf("__", StringComparison.Ordinal);
        if (marker <= 0)
        {
            return null;
        }

        var id = name[..marker].Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }

    public static string? TryParseDisplayNameFromDataFileName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath) ?? "";
        var marker = name.IndexOf("__", StringComparison.Ordinal);
        if (marker < 0 || marker + 2 >= name.Length)
        {
            return null;
        }

        var displayName = name[(marker + 2)..].Trim();
        return string.IsNullOrEmpty(displayName) ? null : displayName;
    }
}
