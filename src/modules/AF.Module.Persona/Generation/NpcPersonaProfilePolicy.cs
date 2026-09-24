using System;

namespace AnimusForge.Refactor.Runtime;

internal static class NpcPersonaProfilePolicy
{
    internal static bool NeedsGeneration(string id, string personality, string background)
        => !string.IsNullOrWhiteSpace(id)
            && (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background));

    internal static string NormalizeGenerated(string text)
        => string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    internal static void CompleteGeneratedFields(ref string personality, ref string background)
    {
        if (string.IsNullOrWhiteSpace(personality) && !string.IsNullOrWhiteSpace(background)) personality = background;
        else if (string.IsNullOrWhiteSpace(background) && !string.IsNullOrWhiteSpace(personality)) background = personality;
    }

    // The caller already revalidated the live Hero, owner, generation and lease on
    // the main thread. This decision consumes only captured/current text values.
    internal static bool TryMerge(bool overwriteExisting, string originalPersonality, string originalBackground,
        string currentPersonality, string currentBackground, string generatedPersonality, string generatedBackground,
        out string personality, out string background)
    {
        personality = background = null;
        if (overwriteExisting && (!string.Equals(currentPersonality, originalPersonality, StringComparison.Ordinal)
            || !string.Equals(currentBackground, originalBackground, StringComparison.Ordinal))) return false;
        personality = overwriteExisting || string.IsNullOrWhiteSpace(currentPersonality) ? generatedPersonality : currentPersonality.Trim();
        background = overwriteExisting || string.IsNullOrWhiteSpace(currentBackground) ? generatedBackground : currentBackground.Trim();
        return true;
    }
}
