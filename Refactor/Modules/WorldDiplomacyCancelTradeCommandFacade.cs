using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

public sealed class WorldDiplomacyCancelTradeCommandFacade
{
    private readonly IWorldDiplomacyCancelTradeGameActionPort _gameActionPort;

    public WorldDiplomacyCancelTradeCommandFacade(IWorldDiplomacyCancelTradeGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyCancelTradeExecutionStatus.InvalidCommand,
                command,
                "diplomacy.cancel_trade.invalid_command");
        }
        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyCancelTradeExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.cancel_trade.port_exception");
        }
    }

    private static WorldDiplomacyCancelTradeExecutionReceipt Receipt(
        WorldDiplomacyCancelTradeExecutionStatus status,
        WorldDiplomacyCancelTradeCommand command,
        string errorCode)
    {
        return new WorldDiplomacyCancelTradeExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
