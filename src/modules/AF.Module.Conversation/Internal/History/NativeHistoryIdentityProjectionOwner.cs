using System;
namespace AnimusForge;
internal sealed class NativeHistoryIdentitySnapshot
{
    internal bool HasHero, HasWildernessMemory, CaptureFailed;
    internal int AgentIndex;
    internal string HeroId, WildernessMemoryId, UnnamedKey, CharacterId, NpcName, FailureHeroId, FailureKey;
}
internal static class NativeHistoryIdentityProjectionOwner
{
    internal static string BuildUnnamedKey(bool hasHero, string troop, string culture, bool isSoldier, string name, string faction, string leader)
    {
        if (hasHero) return "";
        string troopId = NormalizeWildernessNonHeroMemoryKeyPart(troop);
        string factionKey = NormalizeWildernessNonHeroMemoryKeyPart(faction);
        string leaderKey = NormalizeWildernessNonHeroMemoryKeyPart(leader);
        string key;
        if (!string.IsNullOrWhiteSpace(troopId)) key = "troop:" + troopId;
        else
        {
            string cultureId = NormalizeWildernessNonHeroMemoryKeyPart(culture);
            if (string.IsNullOrWhiteSpace(cultureId)) cultureId = "neutral";
            string normalizedName = NormalizeWildernessNonHeroMemoryKeyPart(name);
            if (string.IsNullOrWhiteSpace(normalizedName)) normalizedName = "npc";
            key = "mix:" + cultureId + ":" + (isSoldier ? "soldier" : "commoner") + ":" + normalizedName;
        }
        if (!string.IsNullOrWhiteSpace(factionKey)) key += ":kingdom:" + factionKey;
        if (!string.IsNullOrWhiteSpace(leaderKey)) key += ":lord:" + leaderKey;
        return key.ToLowerInvariant();
    }
    internal static string BuildKey(NativeHistoryIdentitySnapshot input)
    {
        if (input.CaptureFailed)
        {
            string heroId = (input.FailureHeroId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(heroId)) return heroId;
            string fallback = (input.FailureKey ?? "").Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(fallback) ? "" : "native_nonhero:fallback:" + fallback;
        }
        if (input.HasHero) return (input.HeroId ?? "").Trim();
        if (input.HasWildernessMemory) return input.WildernessMemoryId;
        string key = input.UnnamedKey;
        if (!string.IsNullOrWhiteSpace(key))
        {
            key = NormalizeWildernessNonHeroMemoryKeyPart(key);
            if (input.AgentIndex >= 0 && key.IndexOf("|agent:", StringComparison.OrdinalIgnoreCase) < 0)
                key += "|agent:" + input.AgentIndex;
            return "native_nonhero:" + key;
        }
        string characterId = NormalizeWildernessNonHeroMemoryKeyPart(input.CharacterId);
        string name = NormalizeWildernessNonHeroMemoryKeyPart(input.NpcName);
        if (!string.IsNullOrWhiteSpace(characterId))
            return "native_nonhero:character:" + characterId + (string.IsNullOrWhiteSpace(name) ? "" : "|name:" + name);
        return string.IsNullOrWhiteSpace(name) ? "" : "native_nonhero:name:" + name;
    }
    internal static string NormalizeWildernessNonHeroMemoryKeyPart(string value)
    {
        string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim().ToLowerInvariant();
        while (text.Contains("  ")) text = text.Replace("  ", " ");
        return text;
    }
    internal static AnimusForgeDialogueHistoryEntry BuildEntry(long sequence, int day, string date, int hour, string scene,
        string npcName, string speaker, string text, string kind, int targetAgentIndex, int playerTargetAgentIndex, string playerTargetName)
    {
        bool isPlayerLine = string.Equals((kind ?? "").Trim(), "player", StringComparison.OrdinalIgnoreCase);
        int storedTargetAgentIndex = isPlayerLine ? (playerTargetAgentIndex >= 0 ? playerTargetAgentIndex : targetAgentIndex) : -1;
        string storedTargetName = isPlayerLine ? (playerTargetName ?? "").Trim() : "";
        if (isPlayerLine && string.IsNullOrWhiteSpace(storedTargetName)) storedTargetName = (npcName ?? "").Trim();
        return new AnimusForgeDialogueHistoryEntry {
            EventSequence = sequence, GameDayIndex = day, GameDate = date, GameHour = hour, Scene = scene,
            Speaker = string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker.Trim(),
            TargetAgentIndex = storedTargetAgentIndex, TargetName = storedTargetName, Text = text, Kind = kind ?? ""
        };
    }
}
