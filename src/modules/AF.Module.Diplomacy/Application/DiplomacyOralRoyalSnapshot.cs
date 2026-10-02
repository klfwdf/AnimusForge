namespace AnimusForge;

internal readonly struct DiplomacyOralRoyalSnapshot
{
    internal DiplomacyOralRoyalSnapshot(bool playerKingdomExists, string playerKingdomId,
        bool playerKingdomEliminated, bool playerIsRuler, bool npcKingdomExists,
        string npcKingdomId, string speakerHeroId, bool npcIsRuler)
    {
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomId = playerKingdomId;
        PlayerKingdomEliminated = playerKingdomEliminated;
        PlayerIsRuler = playerIsRuler;
        NpcKingdomExists = npcKingdomExists;
        NpcKingdomId = npcKingdomId;
        SpeakerHeroId = speakerHeroId;
        NpcIsRuler = npcIsRuler;
    }
    internal bool PlayerKingdomExists { get; }
    internal string PlayerKingdomId { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal bool PlayerIsRuler { get; }
    internal bool NpcKingdomExists { get; }
    internal string NpcKingdomId { get; }
    internal string SpeakerHeroId { get; }
    internal bool NpcIsRuler { get; }
}
