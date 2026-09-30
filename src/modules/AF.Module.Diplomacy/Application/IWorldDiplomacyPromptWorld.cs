using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Main-thread fact projection and current legality, used only during prompt assembly.
internal interface IWorldDiplomacyPromptWorld
{
    string ResolveKingdom(string id);
    bool IsEliminated(string id);
    bool HasIndependentWorldDiplomacyAuthority(string id);
    IEnumerable<string> KingdomIds();
    bool IsAtWar(string author, string target);
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    string KingdomName(string id);
    string RulerName(string id);
    int GetRoundParticipantLimit();
    int GetActivityLevel();
    int CurrentDay();
    string GetCommonDiplomacyContract(WorldDiplomacyRound round);
    string BuildWorldDiplomacyVassalageSnapshot();
    string BuildPolicySnapshot(string id);
    string BuildGatheringSnapshot(IEnumerable<string> ids, int count);
    string BuildCompactRoundPlanCandidateLine(string author, string target, WorldDiplomacyRound round, IReadOnlyList<string> legalActions);
    string BuildWarDecisionContext(string author, string target, bool includePeaceNegotiationTerms);
    WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string resultSettlementSlotId, bool isExternalResponseOnly, string sourceDocumentId, bool requireAnyOpenPeaceOffer);
    void AppendDiplomaticAuthorDecisionContext(StringBuilder sb, string author, string roundId);
    void AppendDiplomaticTargetDecisionContext(StringBuilder sb, WorldDiplomacyRound round, string author, string target, bool includePeaceNegotiationTerms, IReadOnlyCollection<string> legalActions);
    void AppendRulerCaptivityDecisionContext(StringBuilder sb, string author, string target);
    void AppendOtherKingdomRelationshipContext(StringBuilder sb, string author, IEnumerable<string> ids);
    void AppendRelayResponseSourceContext(StringBuilder sb, WorldDiplomacyRound round, string author, WorldDiplomacyDocument source, string requiredId);
    void AppendDiplomaticThreatAnalysisContext(StringBuilder sb, string author);
}
