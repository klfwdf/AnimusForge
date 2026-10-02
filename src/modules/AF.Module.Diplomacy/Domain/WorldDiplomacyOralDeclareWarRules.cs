using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralDeclareWarRules
{
    public static WorldDiplomacyOralDeclareWarResolution ResolveCommand(
        string payload,
        bool npcKingdomExists,
        string npcKingdomId,
        string npcSpeakerHeroId,
        bool playerKingdomExists,
        string playerKingdomId,
        bool playerKingdomIsEliminated,
        bool npcSpeakerIsRuler)
    {
        string[] parts = (payload ?? "").Split(':');
        if (parts.Length < 2)
        {
            return WorldDiplomacyOralDeclareWarResolution.Rejected(
                WorldDiplomacyOralDeclareWarResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralDeclareWarResolution.Rejected(
                WorldDiplomacyOralDeclareWarResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralDeclareWarResolution.Rejected(
                WorldDiplomacyOralDeclareWarResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }

        if (string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            if (!playerKingdomExists || playerKingdomIsEliminated)
            {
                return WorldDiplomacyOralDeclareWarResolution.Rejected(
                    WorldDiplomacyOralDeclareWarResolutionStatus.PlayerKingdomUnavailable,
                    firstId,
                    secondId);
            }
            if (!string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase))
            {
                return WorldDiplomacyOralDeclareWarResolution.Rejected(
                    WorldDiplomacyOralDeclareWarResolutionStatus.PlayerDeclarerMismatch,
                    firstId,
                    secondId);
            }
            return WorldDiplomacyOralDeclareWarResolution.Ready(
                firstId,
                secondId,
                new WorldDiplomacyDeclareWarCommand(
                    firstId,
                    secondId,
                    npcSpeakerHeroId,
                    WorldDiplomacyDeclareWarDeclarerKind.PlayerKingdom));
        }

        if (string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            if (!npcSpeakerIsRuler)
            {
                return WorldDiplomacyOralDeclareWarResolution.Rejected(
                    WorldDiplomacyOralDeclareWarResolutionStatus.NpcSpeakerNotRuler,
                    firstId,
                    secondId);
            }
            return WorldDiplomacyOralDeclareWarResolution.Ready(
                firstId,
                secondId,
                new WorldDiplomacyDeclareWarCommand(
                    firstId,
                    secondId,
                    npcSpeakerHeroId,
                    WorldDiplomacyDeclareWarDeclarerKind.NpcKingdom));
        }

        return WorldDiplomacyOralDeclareWarResolution.Rejected(
            WorldDiplomacyOralDeclareWarResolutionStatus.NpcKingdomMismatch,
            firstId,
            secondId);
    }
}
