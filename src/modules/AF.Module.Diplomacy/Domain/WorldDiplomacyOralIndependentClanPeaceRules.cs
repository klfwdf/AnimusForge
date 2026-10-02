using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyOralIndependentClanPeaceRules
{
    public static WorldDiplomacyOralIndependentClanPeaceResolution ResolveCommand(
        string payload,
        bool contextAvailable,
        string playerClanId,
        string targetKingdomId,
        string speakerHeroId)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            return WorldDiplomacyOralIndependentClanPeaceResolution.Rejected(
                WorldDiplomacyOralIndependentClanPeaceResolutionStatus.UnexpectedPayload);
        }
        if (!contextAvailable)
        {
            return WorldDiplomacyOralIndependentClanPeaceResolution.Rejected(
                WorldDiplomacyOralIndependentClanPeaceResolutionStatus.ContextUnavailable);
        }

        WorldDiplomacyIndependentClanPeaceCommand command =
            new WorldDiplomacyIndependentClanPeaceCommand(
                playerClanId,
                targetKingdomId,
                speakerHeroId);
        if (!command.IsValid)
        {
            return WorldDiplomacyOralIndependentClanPeaceResolution.Rejected(
                WorldDiplomacyOralIndependentClanPeaceResolutionStatus.MissingIdentity);
        }
        return WorldDiplomacyOralIndependentClanPeaceResolution.Ready(command);
    }
}
