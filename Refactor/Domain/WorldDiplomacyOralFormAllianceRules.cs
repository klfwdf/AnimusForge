using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralFormAllianceRules
{
    public static WorldDiplomacyOralFormAllianceResolution ResolveCommand(
        string payload,
        bool playerKingdomExists,
        string playerKingdomId,
        bool playerKingdomIsEliminated,
        bool playerIsRuler,
        bool npcKingdomExists,
        string npcKingdomId,
        string npcSpeakerHeroId,
        bool npcSpeakerIsRuler)
    {
        string[] parts = (payload ?? "").Split(':');
        if (parts.Length < 2)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        string durationToken = parts.Length > 2 ? parts[2].Trim() : "default";
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!playerKingdomExists || playerKingdomIsEliminated)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.PlayerKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!playerIsRuler)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.PlayerNotRuler,
                firstId,
                secondId);
        }
        if (!npcSpeakerIsRuler)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.NpcSpeakerNotRuler,
                firstId,
                secondId);
        }

        bool playerFirst = string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase);
        bool npcFirst = string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
        if (!playerFirst && !npcFirst)
        {
            return WorldDiplomacyOralFormAllianceResolution.Rejected(
                WorldDiplomacyOralFormAllianceResolutionStatus.KingdomPairMismatch,
                firstId,
                secondId);
        }

        return WorldDiplomacyOralFormAllianceResolution.Ready(
            firstId,
            secondId,
            new WorldDiplomacyFormAllianceCommand(
                playerKingdomId,
                npcKingdomId,
                npcSpeakerHeroId,
                durationToken));
    }
}
