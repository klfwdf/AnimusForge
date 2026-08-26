using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Provides machine context for an NPC reaction generated after a town action.
/// These markers are internal runtime facts, never player-dialogue keyword triggers.
/// </summary>
public static class TownAmbientReactionContextProfile
{
    public const string Marker = "[GCCZ_TOWN_AMBIENT_REACTION]";

    public static string BuildMarker(
        SiegeInterventionActionKind action,
        TownAmbientReactionAudience audience)
    {
        return Marker + " action=" + action + "; audience=" + audience + ";";
    }

    public static bool TryParse(
        string text,
        out SiegeInterventionActionKind action,
        out TownAmbientReactionAudience audience)
    {
        action = SiegeInterventionActionKind.Unknown;
        audience = TownAmbientReactionAudience.None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string firstLine = text.Replace("\r", string.Empty).Split('\n')[0].Trim();
        if (!firstLine.StartsWith(Marker + " ", StringComparison.Ordinal))
        {
            return false;
        }

        string payload = firstLine.Substring(Marker.Length).Trim();
        foreach (string segment in payload.Split(';'))
        {
            string value = (segment ?? string.Empty).Trim();
            int separator = value.IndexOf('=');
            if (separator <= 0 || separator >= value.Length - 1)
            {
                continue;
            }

            string key = value.Substring(0, separator).Trim();
            string rawValue = value.Substring(separator + 1).Trim();
            if (key.Equals("action", StringComparison.OrdinalIgnoreCase))
            {
                Enum.TryParse(rawValue, ignoreCase: true, out action);
            }
            else if (key.Equals("audience", StringComparison.OrdinalIgnoreCase))
            {
                Enum.TryParse(rawValue, ignoreCase: true, out audience);
            }
        }

        return action != SiegeInterventionActionKind.Unknown
            && audience != TownAmbientReactionAudience.None;
    }

    public static string BuildAudienceEventId(
        string baseEventId,
        TownAmbientReactionAudience audience)
    {
        string normalizedBase = string.IsNullOrWhiteSpace(baseEventId)
            ? "town_ambient"
            : baseEventId.Trim();
        return normalizedBase + ":audience=" + audience.ToString().ToLowerInvariant();
    }
}
