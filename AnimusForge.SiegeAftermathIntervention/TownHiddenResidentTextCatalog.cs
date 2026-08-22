using System;
using System.Globalization;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Localized feedback and scene-memory text for hidden residents brought into a GCCZ town scene.
/// </summary>
public sealed class TownHiddenResidentTextCatalog
{
    public int Version { get; set; }

    public string SpawnedMessageTemplate { get; set; }

    public string ResidentsAlreadyVisibleMessage { get; set; }

    public string SceneSpawnLimitReachedMessage { get; set; }

    public string SceneAgentLimitReachedMessage { get; set; }

    public string OperationSnapshotLockedMessage { get; set; }

    public string DestructiveCombatActiveMessage { get; set; }

    public string RuntimeUnavailableMessage { get; set; }

    public string NoSafeCornerMessage { get; set; }

    public string SpawnFailedMessage { get; set; }

    public string MemoryTitle { get; set; }

    public string MemoryTemplate { get; set; }

    public string BuildMessage(TownHiddenResidentSpawnOutcome outcome)
    {
        string template = outcome.Status switch
        {
            TownHiddenResidentSpawnStatus.Spawned => SpawnedMessageTemplate,
            TownHiddenResidentSpawnStatus.ResidentsAlreadyVisible => ResidentsAlreadyVisibleMessage,
            TownHiddenResidentSpawnStatus.SceneSpawnLimitReached => SceneSpawnLimitReachedMessage,
            TownHiddenResidentSpawnStatus.SceneAgentLimitReached => SceneAgentLimitReachedMessage,
            TownHiddenResidentSpawnStatus.OperationSnapshotLocked => OperationSnapshotLockedMessage,
            TownHiddenResidentSpawnStatus.DestructiveCombatActive => DestructiveCombatActiveMessage,
            TownHiddenResidentSpawnStatus.NoSafeCorner => NoSafeCornerMessage,
            TownHiddenResidentSpawnStatus.SpawnFailed => SpawnFailedMessage,
            _ => RuntimeUnavailableMessage,
        };
        return ReplaceCount(template, outcome.SpawnedCount);
    }

    public string BuildMemory(int spawnedCount)
    {
        return ReplaceCount(MemoryTemplate, spawnedCount);
    }

    public static TownHiddenResidentTextCatalog Resolve(TownHiddenResidentTextCatalog source)
    {
        TownHiddenResidentTextCatalog fallback = CreateEnglishFallback();
        if (source == null)
        {
            return fallback;
        }

        return new TownHiddenResidentTextCatalog
        {
            Version = source.Version > 0 ? source.Version : fallback.Version,
            SpawnedMessageTemplate = Pick(source.SpawnedMessageTemplate, fallback.SpawnedMessageTemplate),
            ResidentsAlreadyVisibleMessage = Pick(source.ResidentsAlreadyVisibleMessage, fallback.ResidentsAlreadyVisibleMessage),
            SceneSpawnLimitReachedMessage = Pick(source.SceneSpawnLimitReachedMessage, fallback.SceneSpawnLimitReachedMessage),
            SceneAgentLimitReachedMessage = Pick(source.SceneAgentLimitReachedMessage, fallback.SceneAgentLimitReachedMessage),
            OperationSnapshotLockedMessage = Pick(source.OperationSnapshotLockedMessage, fallback.OperationSnapshotLockedMessage),
            DestructiveCombatActiveMessage = Pick(source.DestructiveCombatActiveMessage, fallback.DestructiveCombatActiveMessage),
            RuntimeUnavailableMessage = Pick(source.RuntimeUnavailableMessage, fallback.RuntimeUnavailableMessage),
            NoSafeCornerMessage = Pick(source.NoSafeCornerMessage, fallback.NoSafeCornerMessage),
            SpawnFailedMessage = Pick(source.SpawnFailedMessage, fallback.SpawnFailedMessage),
            MemoryTitle = Pick(source.MemoryTitle, fallback.MemoryTitle),
            MemoryTemplate = Pick(source.MemoryTemplate, fallback.MemoryTemplate),
        };
    }

    public static TownHiddenResidentTextCatalog CreateEnglishFallback()
    {
        return new TownHiddenResidentTextCatalog
        {
            Version = 1,
            SpawnedMessageTemplate = "Movement stirs in the side streets. {count} frightened residents emerge from hiding and wait at a distance.",
            ResidentsAlreadyVisibleMessage = "Enough residents are already visible in the streets; no one else is brought out.",
            SceneSpawnLimitReachedMessage = "No more hidden residents answer this scene's summons.",
            SceneAgentLimitReachedMessage = "The streets are too crowded to bring out another group safely.",
            OperationSnapshotLockedMessage = "The current operation has already fixed its targets; no additional residents are exposed.",
            DestructiveCombatActiveMessage = "No hidden resident will emerge while the killing continues.",
            RuntimeUnavailableMessage = "The order spreads into the side streets, but no resident can be brought out now.",
            NoSafeCornerMessage = "The search finds no safe side street from which residents can emerge.",
            SpawnFailedMessage = "The search reaches the hiding places, but no resident comes into the street.",
            MemoryTitle = "Hidden residents",
            MemoryTemplate = "The player ordered hidden residents brought out. {count} ordinary civilians entered the current scene from safe side streets; this memory lasts only for this scene.",
        };
    }

    private static string ReplaceCount(string value, int count)
    {
        return (value ?? string.Empty).Replace(
            "{count}",
            Math.Max(0, count).ToString(CultureInfo.InvariantCulture));
    }

    private static string Pick(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
