using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

public sealed class WorldDiplomacyMakePeaceCommandFacade
{
    private readonly IWorldDiplomacyMakePeaceGameActionPort _gameActionPort;

    public WorldDiplomacyMakePeaceCommandFacade(IWorldDiplomacyMakePeaceGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyMakePeaceExecutionStatus.InvalidCommand,
                command,
                "diplomacy.make_peace.invalid_command");
        }

        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyMakePeaceExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.make_peace.port_exception");
        }
    }

    private static WorldDiplomacyMakePeaceExecutionReceipt Receipt(
        WorldDiplomacyMakePeaceExecutionStatus status,
        WorldDiplomacyMakePeaceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyMakePeaceExecutionReceipt(
            status,
            command.PayerKingdomId,
            command.ReceiverKingdomId,
            0,
            0,
            errorCode);
    }
}
