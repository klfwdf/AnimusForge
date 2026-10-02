using System;
namespace AnimusForge.Refactor.Domain;

// Captured synchronously; no live campaign object can escape through this value.
internal readonly struct WorldDiplomacyAuthoritySnapshot
{
    internal WorldDiplomacyAuthoritySnapshot(string id, bool exists, bool eliminated, bool controlledVassal,
        string suzerainId, bool playerControlled, bool rulerAlive)
    { Id = id; Exists = exists; Eliminated = eliminated; ControlledVassal = controlledVassal;
      SuzerainId = suzerainId; PlayerControlled = playerControlled; RulerAlive = rulerAlive; }
    internal string Id { get; }
    internal bool Exists { get; }
    internal bool Eliminated { get; }
    internal bool ControlledVassal { get; }
    internal string SuzerainId { get; }
    internal bool PlayerControlled { get; }
    internal bool RulerAlive { get; }
}

internal static class WorldDiplomacyAuthorityRules
{
    internal static bool HasIndependentAuthority(WorldDiplomacyAuthoritySnapshot party)
        => party.Exists && !party.Eliminated && !party.ControlledVassal;
    internal static string Representative(WorldDiplomacyAuthoritySnapshot party)
        => party.ControlledVassal ? party.SuzerainId : party.Id;
    internal static bool CanAiAuthor(WorldDiplomacyAuthoritySnapshot party, out string reason)
    {
        reason = !party.Exists || party.Eliminated ? "author_kingdom_missing"
            : party.PlayerControlled ? "player_controlled_realm_requires_player_authorization"
            : !party.RulerAlive ? "ruler_unavailable" : "";
        return reason.Length == 0;
    }
}
