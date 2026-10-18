namespace AnimusForge;

// Evaluated once when a main-response request is admitted. Never writes the user's preference.
internal static class TtsSpeechPolicy
{
    internal static bool ForceMainStreaming(bool enabled, bool local, bool cloud)
        => enabled && (local || cloud);
    internal static bool IncludeLocalTone(bool enabled, bool local, bool battleBlocked)
        => enabled && local && !battleBlocked;
    internal static bool UseMainStreaming(DuelSettings settings)
        => settings != null && (settings.MainApiStreamingEnabled ||
            settings.IsSelectedTtsEnabled);
}
