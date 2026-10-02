using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralMakeTradeRules
{
    public static WorldDiplomacyOralMakeTradeResolution ResolveCommand(
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
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        string durationToken = parts.Length > 2 ? parts[2].Trim() : "default";
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!playerKingdomExists || playerKingdomIsEliminated)
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.PlayerKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!playerIsRuler)
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.PlayerNotRuler,
                firstId,
                secondId);
        }
        if (!npcSpeakerIsRuler)
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.NpcSpeakerNotRuler,
                firstId,
                secondId);
        }

        bool playerFirst = string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase);
        bool npcFirst = string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, playerKingdomId, StringComparison.OrdinalIgnoreCase);
        if (!playerFirst && !npcFirst)
        {
            return WorldDiplomacyOralMakeTradeResolution.Rejected(
                WorldDiplomacyOralMakeTradeResolutionStatus.KingdomPairMismatch,
                firstId,
                secondId);
        }

        return WorldDiplomacyOralMakeTradeResolution.Ready(
            firstId,
            secondId,
            new WorldDiplomacyMakeTradeCommand(
                playerKingdomId,
                npcKingdomId,
                npcSpeakerHeroId,
                durationToken));
    }
}
