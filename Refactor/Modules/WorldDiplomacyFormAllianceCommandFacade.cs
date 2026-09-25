using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

public sealed class WorldDiplomacyFormAllianceCommandFacade
{
    private readonly IWorldDiplomacyFormAllianceGameActionPort _gameActionPort;

    public WorldDiplomacyFormAllianceCommandFacade(IWorldDiplomacyFormAllianceGameActionPort gameActionPort)
    {
        _gameActionPort = gameActionPort ?? throw new ArgumentNullException(nameof(gameActionPort));
    }

    public WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command)
    {
        if (!command.IsValid)
        {
            return Receipt(
                WorldDiplomacyFormAllianceExecutionStatus.InvalidCommand,
                command,
                "diplomacy.form_alliance.invalid_command");
        }
        try
        {
            return _gameActionPort.Execute(command);
        }
        catch
        {
            return Receipt(
                WorldDiplomacyFormAllianceExecutionStatus.UnknownAfterStart,
                command,
                "diplomacy.form_alliance.port_exception");
        }
    }

    private static WorldDiplomacyFormAllianceExecutionReceipt Receipt(
        WorldDiplomacyFormAllianceExecutionStatus status,
        WorldDiplomacyFormAllianceCommand command,
        string errorCode)
    {
        return new WorldDiplomacyFormAllianceExecutionReceipt(
            status,
            command.PlayerKingdomId,
            command.NpcKingdomId,
            errorCode);
    }
}
