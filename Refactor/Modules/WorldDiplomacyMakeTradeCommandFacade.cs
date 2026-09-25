using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

public sealed class WorldDiplomacyMakeTradeCommandFacade
{
    private readonly IWorldDiplomacyMakeTradeGameActionPort _gameActionPort;

    public WorldDiplomacyMakeTradeCommandFacade(IWorldDiplomacyMakeTradeGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyMakeTradeExecutionStatus.InvalidCommand,
                command,
                0,
                "diplomacy.make_trade.invalid_command");
        }
        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyMakeTradeExecutionStatus.UnknownAfterStart,
                command,
                0,
                "diplomacy.make_trade.port_exception");
        }
    }

    private static WorldDiplomacyMakeTradeExecutionReceipt Receipt(
        WorldDiplomacyMakeTradeExecutionStatus status,
        WorldDiplomacyMakeTradeCommand command,
        int durationDays,
        string errorCode)
    {
        return new WorldDiplomacyMakeTradeExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            durationDays,
            errorCode);
    }
}
