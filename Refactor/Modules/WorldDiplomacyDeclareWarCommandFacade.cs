using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

internal sealed class WorldDiplomacyDeclareWarCommandFacade
{
    private readonly IWorldDiplomacyDeclareWarGameActionPort _gameActionPort;

    public WorldDiplomacyDeclareWarCommandFacade(IWorldDiplomacyDeclareWarGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyDeclareWarExecutionStatus.InvalidCommand,
                command,
                "diplomacy.declare_war.invalid_command");
        }

        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyDeclareWarExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.declare_war.port_exception");
        }
    }

    private static WorldDiplomacyDeclareWarExecutionReceipt Receipt(
        WorldDiplomacyDeclareWarExecutionStatus status,
        WorldDiplomacyDeclareWarCommand command,
        string errorCode)
    {
        return new WorldDiplomacyDeclareWarExecutionReceipt(
            status,
            command.DeclarerKingdomId,
            command.TargetKingdomId,
            errorCode);
    }
}
