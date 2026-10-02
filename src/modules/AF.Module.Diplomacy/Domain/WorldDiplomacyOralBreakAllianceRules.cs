using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralBreakAllianceRules
{
    public static WorldDiplomacyOralBreakAllianceResolution ResolveCommand(
        string payload,
        bool playerKingdomExists,
        string playerKingdomId,
        bool playerKingdomIsEliminated,
        bool npcKingdomExists,
        string npcKingdomId,
        string npcSpeakerHeroId)
    {
        string[] parts = (payload ?? "").Split(':');
        if (parts.Length < 2)
        {
            return WorldDiplomacyOralBreakAllianceResolution.Rejected(
                WorldDiplomacyOralBreakAllianceResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralBreakAllianceResolution.Rejected(
                WorldDiplomacyOralBreakAllianceResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!playerKingdomExists || playerKingdomIsEliminated)
        {
            return WorldDiplomacyOralBreakAllianceResolution.Rejected(
                WorldDiplomacyOralBreakAllianceResolutionStatus.PlayerKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralBreakAllianceResolution.Rejected(
                WorldDiplomacyOralBreakAllianceResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }

        bool playerFirst = string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase);
        bool npcFirst = string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
        if (!playerFirst && !npcFirst)
        {
            return WorldDiplomacyOralBreakAllianceResolution.Rejected(
                WorldDiplomacyOralBreakAllianceResolutionStatus.KingdomPairMismatch,
                firstId,
                secondId);
        }

        return WorldDiplomacyOralBreakAllianceResolution.Ready(
            firstId,
            secondId,
            new WorldDiplomacyBreakAllianceCommand(
                playerKingdomId,
                npcKingdomId,
                npcSpeakerHeroId));
    }
}
