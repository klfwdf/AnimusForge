using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralCancelTradeRules
{
    public static WorldDiplomacyOralCancelTradeResolution ResolveCommand(
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
            return WorldDiplomacyOralCancelTradeResolution.Rejected(
                WorldDiplomacyOralCancelTradeResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralCancelTradeResolution.Rejected(
                WorldDiplomacyOralCancelTradeResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!playerKingdomExists || playerKingdomIsEliminated)
        {
            return WorldDiplomacyOralCancelTradeResolution.Rejected(
                WorldDiplomacyOralCancelTradeResolutionStatus.PlayerKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralCancelTradeResolution.Rejected(
                WorldDiplomacyOralCancelTradeResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }

        bool playerFirst = string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase);
        bool npcFirst = string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
        if (!playerFirst && !npcFirst)
        {
            return WorldDiplomacyOralCancelTradeResolution.Rejected(
                WorldDiplomacyOralCancelTradeResolutionStatus.KingdomPairMismatch,
                firstId,
                secondId);
        }

        return WorldDiplomacyOralCancelTradeResolution.Ready(
            firstId,
            secondId,
            new WorldDiplomacyCancelTradeCommand(
                playerKingdomId,
                npcKingdomId,
                npcSpeakerHeroId));
    }
}
