namespace AnimusForge;

internal readonly struct DiplomacyOralPairSnapshot
{
    internal DiplomacyOralPairSnapshot(bool playerKingdomExists, string playerKingdomId,
        bool playerKingdomEliminated, bool npcKingdomExists, string npcKingdomId, string speakerHeroId)
    {
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomId = playerKingdomId;
        PlayerKingdomEliminated = playerKingdomEliminated;
        NpcKingdomExists = npcKingdomExists;
        NpcKingdomId = npcKingdomId;
        SpeakerHeroId = speakerHeroId;
    }
    internal bool PlayerKingdomExists { get; }
    internal string PlayerKingdomId { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal bool NpcKingdomExists { get; }
    internal string NpcKingdomId { get; }
    internal string SpeakerHeroId { get; }
}
