using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyDocumentExecutionPort
{
    string ResolveKingdomId(string id);
    bool IsEliminated(string id);
    WorldDiplomacyAuthoritySnapshot CaptureAuthority(string id);
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer);
    bool IsAtWar(string author, string target);
    bool IsPlayerKingdom(string id);
    string NewId(string prefix);
    WarPressureEntry FindWarPressure(string source, string target);
    void AddWarPressure(string source, string target, int delta, string reason, string intent);
    List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId);
    void Log(string message);
    void Notify(string message);
    int MaxDiplomaticActionsPerDocument { get; }
    int MaxRelayParticipants { get; }
    int CurrentDay { get; }
    IReadOnlyList<WorldDiplomacyThreat> Threats { get; }
}
