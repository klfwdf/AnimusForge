using System;
using System.Collections.Generic;
using System.Linq;
namespace AnimusForge;

internal sealed class WorldDiplomacyNativeDecisionSnapshot
{
    internal string HostId, TargetId, ProposerKingdomId, Action;
    internal bool HasHost, HasTarget, SameTargetHost, TargetEliminated, HostIsPlayer, HostEliminated, ProposerEliminated;
}
internal static class WorldDiplomacyEventRules
{
    internal static int RoundHardDurationDays(int days) => days <= 15 ? 18 : days >= 28 ? 32 : 24;
    internal static int NativeSignalBaseValue(string action) => action == "declare_war" ? 24 : 42;
    internal static bool IsIncomingPlayerOffer(WorldDiplomacyNativeDecisionSnapshot snapshot) => snapshot.HostIsPlayer && snapshot.Action != "declare_war";
    internal static bool TryNativeSignal(WorldDiplomacyNativeDecisionSnapshot snapshot, out string source, out string target)
    {
        source = target = null;
        if (snapshot == null || !snapshot.HasHost || !snapshot.HasTarget || snapshot.SameTargetHost || snapshot.TargetEliminated) return false;
        if (snapshot.Action != "declare_war" && snapshot.Action != "propose_peace" && snapshot.Action != "propose_alliance" && snapshot.Action != "propose_trade") return false;
        bool incoming = IsIncomingPlayerOffer(snapshot);
        source = incoming ? snapshot.TargetId : snapshot.ProposerKingdomId ?? snapshot.HostId;
        target = incoming ? snapshot.HostId : snapshot.TargetId;
        return !(incoming ? snapshot.TargetEliminated : snapshot.ProposerKingdomId != null ? snapshot.ProposerEliminated : snapshot.HostEliminated);
    }
    internal static bool ShouldCaptureBattle(bool exists, bool hasWinner, bool hideout) => exists && hasWinner && !hideout;
    internal static bool HasOpposingKingdoms(IReadOnlyList<string> attackers, IReadOnlyList<string> defenders) =>
        attackers.Count > 0 && defenders.Count > 0
        && attackers.Except(defenders, StringComparer.OrdinalIgnoreCase).Any()
        && defenders.Except(attackers, StringComparer.OrdinalIgnoreCase).Any();
}
