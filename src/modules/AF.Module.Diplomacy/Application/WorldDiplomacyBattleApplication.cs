using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace AnimusForge;
internal interface IWorldDiplomacyBattlePort
{
    bool Exists { get; }
    bool HasWinner { get; }
    bool IsHideout { get; }
    string EventId { get; }
    List<string> AttackerKingdomIds();
    List<string> DefenderKingdomIds();
    WorldDiplomacyBattleFact CaptureDetails(List<string> attackers, List<string> defenders);
    void Log(string message);
}
internal static class WorldDiplomacyBattleApplication
{
    internal static void Record(IWorldDiplomacyBattlePort port, IWorldDiplomacyOrchestration orchestration)
    {
        try
        {
            if (!WorldDiplomacyEventRules.ShouldCaptureBattle(port.Exists, port.HasWinner, port.IsHideout)) return;
            var attackers = port.AttackerKingdomIds(); var defenders = port.DefenderKingdomIds();
            if (!WorldDiplomacyEventRules.HasOpposingKingdoms(attackers, defenders)) return;
            var fact = port.CaptureDetails(attackers, defenders);
            fact.BattleId = "battle:" + fact.Day.ToString(CultureInfo.InvariantCulture) + ":" + (port.EventId ?? "")
                + ":" + string.Join(",", attackers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                + ":" + string.Join(",", defenders.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            orchestration.RecordBattleFact(fact);
        }
        catch (Exception ex) { port.Log("record recent battle failed: " + ex.Message); }
    }
}
