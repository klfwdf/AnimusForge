using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralMakePeaceRules
{
    public static WorldDiplomacyOralMakePeaceResolution ResolveCommand(
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
        if (parts.Length < 3)
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.BadPayloadFormat);
        }

        string firstId = parts[0].Trim();
        string secondId = parts[1].Trim();
        string amountToken = parts[2].Trim();
        string durationToken = parts.Length > 3 ? parts[3].Trim() : "default";
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId))
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.EmptyKingdomId,
                firstId,
                secondId);
        }
        if (!playerKingdomExists || playerKingdomIsEliminated)
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.PlayerKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!npcKingdomExists)
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.NpcKingdomUnavailable,
                firstId,
                secondId);
        }
        if (!playerIsRuler)
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.PlayerNotRuler,
                firstId,
                secondId);
        }
        if (!npcSpeakerIsRuler)
        {
            return WorldDiplomacyOralMakePeaceResolution.Rejected(
                WorldDiplomacyOralMakePeaceResolutionStatus.NpcSpeakerNotRuler,
                firstId,
                secondId);
        }

        if (string.Equals(firstId, playerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, npcKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            return Ready(firstId, secondId, npcSpeakerHeroId, amountToken, durationToken);
        }
        if (string.Equals(firstId, npcKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(secondId, playerKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            return Ready(firstId, secondId, npcSpeakerHeroId, amountToken, durationToken);
        }

        return WorldDiplomacyOralMakePeaceResolution.Rejected(
            WorldDiplomacyOralMakePeaceResolutionStatus.KingdomPairMismatch,
            firstId,
            secondId);
    }

    private static WorldDiplomacyOralMakePeaceResolution Ready(
        string payerKingdomId,
        string receiverKingdomId,
        string speakerHeroId,
        string amountToken,
        string durationToken)
    {
        return WorldDiplomacyOralMakePeaceResolution.Ready(
            payerKingdomId,
            receiverKingdomId,
            new WorldDiplomacyMakePeaceCommand(
                payerKingdomId,
                receiverKingdomId,
                speakerHeroId,
                amountToken,
                durationToken));
    }
}
