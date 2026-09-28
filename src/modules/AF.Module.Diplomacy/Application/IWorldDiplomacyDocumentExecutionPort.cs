using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyDocumentExecutionPort
{
    string ResolveKingdomId(string id);
    bool IsEliminated(string id);
    bool HasIndependentWorldDiplomacyAuthority(string id);
    bool CanAiAuthorDiplomaticDocument(string id, out string reason);
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    void PruneInvalidOffers(WorldDiplomacyRound round);
    bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source);
    List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target);
    List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source);
    bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason);
    bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason);
    WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer);
    bool IsAtWar(string author, string target);
    bool IsPlayerKingdom(string id);
    string NewId(string prefix);
    void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason);
    void ExecuteImmediateIntent(string author, string target, string intent, WorldDiplomacyDocument doc);
    void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument doc, string author, string target, bool recordTargetDecisions = true);
    void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument doc, string author, string target, string intent);
    void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument doc, string author);
    bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument doc, string author, string target, string intent);
    void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument doc);
    void TrySettleRelayOffer(WorldDiplomacyDocument doc);
    void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument doc);
    void SettleInternationalReputationForDocument(WorldDiplomacyDocument doc);
    void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc);
    void HandleRoundDocumentProcessed(WorldDiplomacyDocument doc);
    void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc);
    void ReconcileAnalyzedPlayerDeclarationWithReachedCourts(WorldDiplomacyDocument doc);
    void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent item);
    void ScheduleDeferredCanonicalHistoryRetry(string id);
    void StartDocumentPropagation(WorldDiplomacyDocument doc, string author);
    WarPressureEntry FindWarPressure(string source, string target);
    void AddWarPressure(string source, string target, int delta, string reason, string intent);
    List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId);
    void Log(string message);
    void Notify(string message);
    int MaxDiplomaticActionsPerDocument { get; }
    int MaxRelayParticipants { get; }
    int CurrentDay { get; }
    List<WorldDiplomacyThreat> Threats { get; }
}
