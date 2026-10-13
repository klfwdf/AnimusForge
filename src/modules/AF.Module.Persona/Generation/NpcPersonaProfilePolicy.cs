using System;

namespace AnimusForge.Refactor.Runtime;

internal static class NpcPersonaProfilePolicy
{
    internal static bool NeedsGeneration(string id, string personality, string background)
        => !string.IsNullOrWhiteSpace(id)
            && string.IsNullOrWhiteSpace(personality) && string.IsNullOrWhiteSpace(background);

    internal static bool NeedsGeneration(string id, string description)
        => !string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(description);

    internal static string NormalizeGenerated(string text)
        => string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    // Shared with the standalone editor. Only called at load/import/write boundaries.
    internal static string ResolveDescription(string description, int schemaVersion, string personality, string background)
    {
        if (schemaVersion >= 3 || !string.IsNullOrWhiteSpace(description)) return NormalizeGenerated(description);
        return MergeLegacy(personality, background);
    }

    internal static string MergeLegacy(string personality, string background)
    {
        string first = NormalizeGenerated(personality), second = NormalizeGenerated(background);
        if (first.Length == 0) return second;
        if (second.Length == 0 || string.Equals(first, second, StringComparison.Ordinal)) return first;
        return first + "\n\n" + second;
    }

    // The caller already revalidated the live Hero, owner, generation and lease on
    // the main thread. This decision consumes only captured/current text values.
    internal static bool TryMerge(bool overwriteExisting, string originalDescription,
        string currentDescription, string generatedDescription, out string description)
    {
        description = null;
        if (!string.Equals(currentDescription, originalDescription, StringComparison.Ordinal)) return false;
        description = overwriteExisting || string.IsNullOrWhiteSpace(currentDescription) ? generatedDescription : currentDescription;
        return true;
    }
}
