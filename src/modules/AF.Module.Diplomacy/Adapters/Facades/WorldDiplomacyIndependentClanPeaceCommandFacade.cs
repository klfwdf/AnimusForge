using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

internal sealed class WorldDiplomacyIndependentClanPeaceCommandFacade
{
    private readonly IWorldDiplomacyIndependentClanPeaceGameActionPort _gameActionPort;

    public WorldDiplomacyIndependentClanPeaceCommandFacade(
        IWorldDiplomacyIndependentClanPeaceGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(
        WorldDiplomacyIndependentClanPeaceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyIndependentClanPeaceExecutionStatus.InvalidCommand,
                command,
                "diplomacy.independent_clan_peace.invalid_command");
        }

        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyIndependentClanPeaceExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.independent_clan_peace.port_exception");
        }
    }

    private static WorldDiplomacyIndependentClanPeaceExecutionReceipt Receipt(
        WorldDiplomacyIndependentClanPeaceExecutionStatus status,
        WorldDiplomacyIndependentClanPeaceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyIndependentClanPeaceExecutionReceipt(
            status,
            command.PlayerClanId,
            command.TargetKingdomId,
            command.SpeakerHeroId,
            errorCode);
    }
}
