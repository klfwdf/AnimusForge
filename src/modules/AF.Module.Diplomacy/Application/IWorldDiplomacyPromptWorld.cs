using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Main-thread fact projection and current legality, used only during prompt assembly.
// Members are leaf facts or bounded snapshots resolved from stable IDs; section
// ordering, eligibility branching, and prompt policy live in the composer.
internal interface IWorldDiplomacyPromptWorld
{
    string ResolveKingdom(string id);
    bool IsEliminated(string id);
    bool HasIndependentWorldDiplomacyAuthority(string id);
    IEnumerable<string> KingdomIds();
    IReadOnlyList<string> CurrentWarKingdomIds(string authorId);
    IReadOnlyList<string> IndependentKingdomIds();
    bool IsAtWar(string author, string target);
    bool IsAlly(string author, string target);
    bool HasTradeAgreement(string author, string target);
    WorldDiplomacyRound ResolveRound(string id);
    WorldDiplomacyDocument ResolveDocument(string id);
    IReadOnlyList<WorldDiplomacyDocument> Documents();
    IReadOnlyList<WorldDiplomacyThreat> DiplomaticThreats();
    string KingdomName(string id);
    string RulerName(string id);
    int GetRoundParticipantLimit();
    int GetActivityLevel();
    int CurrentDay();
    int DaysPerYear();
    int RecentBattleRetentionDays();
    int NegativeReputationFactRetentionDays();
    string FormatCampaignDate(int day);
    string GetCommonDiplomacyContract(WorldDiplomacyRound round);
    string BuildWorldDiplomacyVassalageSnapshot();
    string BuildPolicySnapshot(string id);
    string BuildGatheringSnapshot(IEnumerable<string> ids, int count);
    WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string resultSettlementSlotId, bool isExternalResponseOnly, string sourceDocumentId, bool requireAnyOpenPeaceOffer);
    IReadOnlyList<string> CessionCandidates(string cedingId, string receivingId, float cessionScore);
    WarSituationSnapshot WarSituation(string authorId, string targetId);
    WorldDiplomacyRealmRelationProfile RelationProfile(string authorId, string targetId);
    WorldDiplomacyBorderRelation BorderRelation(string authorId, string targetId);
    int RulerRelation(string authorId, string targetId);
    int CulturalClaimCount(string authorId, string targetId);
    int NationalPrestige(string kingdomId);
    int InternationalReputation(string kingdomId);
    int WarPressure(string authorId, string targetId);
    // Bilateral state label precedence (at-war > ally > trade > peace) is decided
    // by WorldDiplomacyTextRules.BuildBilateralStateLabel over the leaf facts above.
    string RulerVoiceContext(string kingdomId);
    string RealmInstitutionalVoiceContext(string kingdomId);
    string AuthorRulerFamilyContext(string kingdomId);
    string BilateralRulerFamilyContext(string authorId, string targetId);
    string RecentNativeSignalContext(string authorId, string targetId);
    string RecentBilateralBattleContext(string authorId, string targetId);
    WorldDiplomacyRulerCaptivity AuthorRulerCaptivity(string authorId);
}

// Bounded ruler-captivity fact; the composer decides how it shapes the prompt.
internal sealed class WorldDiplomacyRulerCaptivity
{
    internal bool IsPrisoner;
    internal string HolderKingdomId = "";
    internal string HolderKingdomName = "";
}
