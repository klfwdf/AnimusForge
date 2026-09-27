using System;

namespace AnimusForge;

internal readonly struct DiplomacyConversationEligibilitySnapshot
{
    internal DiplomacyConversationEligibilitySnapshot(bool npcExists, bool npcIsPlayer, bool npcIsDead,
        bool npcKingdomExists, bool npcKingdomEliminated, bool npcIsRuler,
        bool playerKingdomExists, bool playerKingdomEliminated, bool kingdomsDiffer, bool playerIsRuler)
    {
        NpcExists = npcExists;
        NpcIsPlayer = npcIsPlayer;
        NpcIsDead = npcIsDead;
        NpcKingdomExists = npcKingdomExists;
        NpcKingdomEliminated = npcKingdomEliminated;
        NpcIsRuler = npcIsRuler;
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomEliminated = playerKingdomEliminated;
        KingdomsDiffer = kingdomsDiffer;
        PlayerIsRuler = playerIsRuler;
    }

    internal bool NpcExists { get; }
    internal bool NpcIsPlayer { get; }
    internal bool NpcIsDead { get; }
    internal bool NpcKingdomExists { get; }
    internal bool NpcKingdomEliminated { get; }
    internal bool NpcIsRuler { get; }
    internal bool PlayerKingdomExists { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal bool KingdomsDiffer { get; }
    internal bool PlayerIsRuler { get; }
}

internal interface IDiplomacyConversationEligibilitySource
{
    DiplomacyConversationEligibilitySnapshot Capture(string heroId);
}

internal static class DiplomacyConversationEligibilityApplication
{
    internal const string IndependentClanPeaceTag = "[ACTION:DIPLOMACY:INDEPENDENT_CLAN_PEACE]";

    internal static bool CanInject(IDiplomacyConversationEligibilitySource source, string heroId) =>
        Evaluate(source, heroId, CanInject);

    internal static bool CanUseAction(IDiplomacyConversationEligibilitySource source, string heroId) =>
        Evaluate(source, heroId, CanUseAction);

    internal static bool CanUseFull(IDiplomacyConversationEligibilitySource source, string heroId) =>
        Evaluate(source, heroId, CanUseFull);

    internal static bool CanUseNpcDeclareWar(IDiplomacyConversationEligibilitySource source, string heroId) =>
        Evaluate(source, heroId, CanUseNpcDeclareWar);

    internal static bool IsIndependentClanPeaceTag(string tag) =>
        string.Equals((tag ?? "").Trim(), IndependentClanPeaceTag, StringComparison.OrdinalIgnoreCase);

    private static bool Evaluate(IDiplomacyConversationEligibilitySource source, string heroId,
        Func<DiplomacyConversationEligibilitySnapshot, bool> decide)
    {
        try { return decide(source.Capture(heroId)); }
        catch { return false; }
    }

    internal static bool CanInject(DiplomacyConversationEligibilitySnapshot value) =>
        value.NpcExists && !value.NpcIsPlayer && !value.NpcIsDead &&
        value.NpcKingdomExists && !value.NpcKingdomEliminated && value.NpcIsRuler;

    // Full diplomacy historically does not reject a dead speaker here. Preserve that gate.
    internal static bool CanUseFull(DiplomacyConversationEligibilitySnapshot value) =>
        value.NpcExists && value.PlayerKingdomExists && value.NpcKingdomExists &&
        !value.PlayerKingdomEliminated && !value.NpcKingdomEliminated &&
        value.KingdomsDiffer && value.PlayerIsRuler && value.NpcIsRuler;

    internal static bool CanUseNpcDeclareWar(DiplomacyConversationEligibilitySnapshot value) => CanInject(value);

    internal static bool CanUseAction(DiplomacyConversationEligibilitySnapshot value) =>
        CanUseFull(value) || CanUseNpcDeclareWar(value);
}
