using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

public sealed class WorldDiplomacyBreakAllianceCommandFacade
{
    private readonly IWorldDiplomacyBreakAllianceGameActionPort _gameActionPort;

    public WorldDiplomacyBreakAllianceCommandFacade(IWorldDiplomacyBreakAllianceGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyBreakAllianceExecutionStatus.InvalidCommand,
                command,
                "diplomacy.break_alliance.invalid_command");
        }
        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyBreakAllianceExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.break_alliance.port_exception");
        }
    }

    private static WorldDiplomacyBreakAllianceExecutionReceipt Receipt(
        WorldDiplomacyBreakAllianceExecutionStatus status,
        WorldDiplomacyBreakAllianceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyBreakAllianceExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
