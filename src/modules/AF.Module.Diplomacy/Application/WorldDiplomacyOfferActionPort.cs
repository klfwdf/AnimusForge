namespace AnimusForge;

internal readonly struct WorldDiplomacyOfferActionReceipt
{
    internal readonly bool Applied;
    internal readonly string Message;
    internal readonly bool Complete;
    internal readonly bool Known;
    internal WorldDiplomacyOfferActionReceipt(bool applied, string message, bool complete = true, bool known = true)
    {
        Known = known;
        Applied = known && applied;
        Complete = known && applied && complete;
        Message = message ?? "";
    }
}

internal interface IWorldDiplomacyOfferActionPort
{
    WorldDiplomacyStorage Storage { get; }
    int CurrentDay { get; }
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    bool ResolveParties(WorldDiplomacyRoundOffer offer);
    WorldDiplomacyOfferActionReceipt ExecutePeace(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms);
    WorldDiplomacyCessionReceipt ApplyCession(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms);
    WorldDiplomacyOfferActionReceipt ExecuteAlliance(string proposerId, string targetId);
    WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId);
    WorldDiplomacyOfferActionReceipt ReadPeace(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms);
    bool HasTakenEffect(string intent, string proposerId, string targetId);
    void Log(string message);
}

