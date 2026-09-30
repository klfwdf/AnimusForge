using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    // One synchronous prompt/repair invocation. Never stored on the behavior or
    // captured by a worker. Index lazily once instead of re-scanning per fact;
    // system-only prompts and rejected empty inputs do not scan kingdoms.
    // Leaf facts only: section ordering, eligibility branching, and prompt policy
    // live in WorldDiplomacyPromptComposer.
    private sealed class PromptWorld : IWorldDiplomacyDraftRepairWorld
    {
        private readonly WorldDiplomacyBehavior _owner;
        private readonly List<Kingdom> _kingdoms = new List<Kingdom>();
        private readonly Dictionary<string, Kingdom> _active = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Kingdom> _all = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
        private bool _kingdomIndexInitialized;
        internal PromptWorld(WorldDiplomacyBehavior owner) => _owner = owner;
        private void EnsureKingdomIndex()
        {
            if (_kingdomIndexInitialized) return;
            _kingdomIndexInitialized = true;
            if (Campaign.Current == null) return;
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null) continue;
                _kingdoms.Add(kingdom);
                Index(_all, kingdom.StringId, kingdom);
                Index(_all, kingdom.Name?.ToString(), kingdom);
                if (kingdom.IsEliminated) continue;
                Index(_active, kingdom.StringId, kingdom);
                Index(_active, kingdom.Name?.ToString(), kingdom);
            }
        }
        private static void Index(Dictionary<string, Kingdom> index, string key, Kingdom kingdom)
        {
            if (!string.IsNullOrWhiteSpace(key) && !index.ContainsKey(key)) index.Add(key, kingdom);
        }
        private Kingdom Resolve(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            EnsureKingdomIndex();
            return Lookup(_active, id);
        }
        private Kingdom ResolveAny(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            EnsureKingdomIndex();
            return Lookup(_all, id);
        }
        private static Kingdom Lookup(Dictionary<string, Kingdom> index, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return index.TryGetValue(id.Trim(), out Kingdom kingdom) ? kingdom : null;
        }
        string IWorldDiplomacyPromptWorld.ResolveKingdom(string id) => Resolve(id)?.StringId;
        bool IWorldDiplomacyPromptWorld.IsEliminated(string id) => ResolveAny(id)?.IsEliminated == true;
        bool IWorldDiplomacyPromptWorld.HasIndependentWorldDiplomacyAuthority(string id) => WorldDiplomacyBehavior.HasIndependentWorldDiplomacyAuthority(Resolve(id));
        IEnumerable<string> IWorldDiplomacyPromptWorld.KingdomIds()
        {
            EnsureKingdomIndex();
            return _kingdoms.Select(x => x.StringId);
        }
        IReadOnlyList<string> IWorldDiplomacyPromptWorld.CurrentWarKingdomIds(string authorId)
        {
            Kingdom author = Resolve(authorId);
            if (author == null) return new List<string>();
            EnsureKingdomIndex();
            return _kingdoms
                .Where(x => x != null && x != author && !x.IsEliminated
                    && FactionManager.IsAtWarAgainstFaction(author, x))
                .Select(x => x.StringId)
                .ToList();
        }
        IReadOnlyList<string> IWorldDiplomacyPromptWorld.IndependentKingdomIds()
        {
            EnsureKingdomIndex();
            return _kingdoms
                .Where(x => x != null && !x.IsEliminated
                    && WorldDiplomacyBehavior.HasIndependentWorldDiplomacyAuthority(x))
                .Select(x => x.StringId)
                .ToList();
        }
        bool IWorldDiplomacyPromptWorld.IsAtWar(string author, string target) => FactionManager.IsAtWarAgainstFaction(Resolve(author), Resolve(target));
        bool IWorldDiplomacyPromptWorld.IsAlly(string author, string target)
        {
            Kingdom a = Resolve(author);
            Kingdom b = Resolve(target);
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            return a != null && b != null && alliance != null && alliance.IsAllyWithKingdom(a, b);
        }
        bool IWorldDiplomacyPromptWorld.HasTradeAgreement(string author, string target)
        {
            Kingdom a = Resolve(author);
            Kingdom b = Resolve(target);
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            return a != null && b != null && trade != null && BannerlordApiCompat.HasTradeAgreement(trade, a, b);
        }
        WorldDiplomacyRound IWorldDiplomacyPromptWorld.ResolveRound(string id) => _owner.ResolveRound(id);
        WorldDiplomacyDocument IWorldDiplomacyPromptWorld.ResolveDocument(string id) => _owner.ResolveDocument(id);
        IReadOnlyList<WorldDiplomacyDocument> IWorldDiplomacyPromptWorld.Documents() => _owner._storage?.Documents;
        IReadOnlyList<WorldDiplomacyThreat> IWorldDiplomacyPromptWorld.DiplomaticThreats() => _owner._storage?.DiplomaticThreats;
        string IWorldDiplomacyPromptWorld.KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(Resolve(id) ?? ResolveAny(id));
        string IWorldDiplomacyPromptWorld.RulerName(string id) => WorldDiplomacyBehavior.RulerName(Resolve(id) ?? ResolveAny(id));
        int IWorldDiplomacyPromptWorld.GetRoundParticipantLimit() => WorldDiplomacyBehavior.GetRoundParticipantLimit();
        int IWorldDiplomacyPromptWorld.GetActivityLevel() => WorldDiplomacyBehavior.GetActivityLevel();
        int IWorldDiplomacyPromptWorld.CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        int IWorldDiplomacyPromptWorld.DaysPerYear() => WorldDiplomacyBehavior.DaysPerYear;
        int IWorldDiplomacyPromptWorld.RecentBattleRetentionDays() => WorldDiplomacyBehavior.RecentBattleRetentionDays;
        int IWorldDiplomacyPromptWorld.NegativeReputationFactRetentionDays() => WorldDiplomacyBehavior.RecentNegativeReputationFactRetentionDays;
        string IWorldDiplomacyPromptWorld.FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        string IWorldDiplomacyPromptWorld.GetCommonDiplomacyContract(WorldDiplomacyRound round) => _owner.GetCommonDiplomacyContract(round);
        string IWorldDiplomacyPromptWorld.BuildWorldDiplomacyVassalageSnapshot() => WorldDiplomacyBehavior.BuildWorldDiplomacyVassalageSnapshot();
        string IWorldDiplomacyPromptWorld.BuildPolicySnapshot(string id) => WorldDiplomacyPolicyContext.BuildSnapshot(id);
        string IWorldDiplomacyPromptWorld.BuildGatheringSnapshot(IEnumerable<string> ids, int count) => NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(ids, count);
        WorldDiplomacyRoundOffer IWorldDiplomacyPromptWorld.FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string resultSettlementSlotId, bool isExternalResponseOnly, string sourceDocumentId, bool requireAnyOpenPeaceOffer) => WorldDiplomacyBehavior.FindRequiredPeaceOfferResponse(round, Resolve(author), resultSettlementSlotId, isExternalResponseOnly, sourceDocumentId, requireAnyOpenPeaceOffer);
        IReadOnlyList<string> IWorldDiplomacyPromptWorld.CessionCandidates(string cedingId, string receivingId, float cessionScore)
            => WorldDiplomacyBehavior.ProjectCessionCandidates(_owner.BuildCessionCandidates(Resolve(cedingId), Resolve(receivingId), cessionScore)).ToList();
        WarSituationSnapshot IWorldDiplomacyPromptWorld.WarSituation(string authorId, string targetId) => _owner.GetWarSituation(Resolve(authorId), Resolve(targetId));
        WorldDiplomacyRealmRelationProfile IWorldDiplomacyPromptWorld.RelationProfile(string authorId, string targetId) => _owner.GetRealmRelationProfile(Resolve(authorId), Resolve(targetId));
        WorldDiplomacyBorderRelation IWorldDiplomacyPromptWorld.BorderRelation(string authorId, string targetId) => _owner.GetKingdomBorderRelation(Resolve(authorId), Resolve(targetId));
        int IWorldDiplomacyPromptWorld.RulerRelation(string authorId, string targetId) => WorldDiplomacyBehavior.GetRulerRelation(Resolve(authorId), Resolve(targetId));
        int IWorldDiplomacyPromptWorld.CulturalClaimCount(string authorId, string targetId) => WorldDiplomacyBehavior.CountCulturalClaims(Resolve(authorId), Resolve(targetId));
        int IWorldDiplomacyPromptWorld.NationalPrestige(string kingdomId) => WorldDiplomacyReputationRules.GetNationalPrestige(_owner._storage?.NationalPrestigeByKingdom, kingdomId);
        int IWorldDiplomacyPromptWorld.InternationalReputation(string kingdomId) => WorldDiplomacyReputationRules.GetInternationalReputation(_owner._storage?.InternationalReputationByKingdom, kingdomId);
        int IWorldDiplomacyPromptWorld.WarPressure(string authorId, string targetId) => WorldDiplomacyWarPressureRules.GetWarPressure(_owner._storage?.WarPressure, authorId, targetId);
        string IWorldDiplomacyPromptWorld.RulerVoiceContext(string kingdomId) => WorldDiplomacyBehavior.BuildRulerVoiceContext(Resolve(kingdomId) ?? ResolveAny(kingdomId));
        string IWorldDiplomacyPromptWorld.RealmInstitutionalVoiceContext(string kingdomId) => _owner.BuildRealmInstitutionalVoiceContext(Resolve(kingdomId) ?? ResolveAny(kingdomId));
        string IWorldDiplomacyPromptWorld.AuthorRulerFamilyContext(string kingdomId) => WorldDiplomacyBehavior.BuildAuthorRulerFamilyContext(Resolve(kingdomId) ?? ResolveAny(kingdomId));
        string IWorldDiplomacyPromptWorld.BilateralRulerFamilyContext(string authorId, string targetId) => WorldDiplomacyBehavior.BuildBilateralRulerFamilyContext(Resolve(authorId), Resolve(targetId));
        string IWorldDiplomacyPromptWorld.RecentNativeSignalContext(string authorId, string targetId) => WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext(_owner._storage?.NativeSignals, authorId, targetId);
        string IWorldDiplomacyPromptWorld.RecentBilateralBattleContext(string authorId, string targetId) => WorldDiplomacyTextRules.BuildRecentBilateralBattleContext(
            _owner._storage?.RecentBattles, authorId, targetId, WorldDiplomacyBehavior.CurrentDay(),
            WorldDiplomacyBehavior.RecentBattleRetentionDays,
            id => WorldDiplomacyBehavior.KingdomName(Resolve(id) ?? ResolveAny(id)),
            WorldDiplomacyBehavior.FormatCampaignDate);
        WorldDiplomacyRulerCaptivity IWorldDiplomacyPromptWorld.AuthorRulerCaptivity(string authorId) => WorldDiplomacyBehavior.ResolveAuthorRulerCaptivity(Resolve(authorId));
        string IWorldDiplomacyDraftRepairWorld.BuildGovernmentHardFact(string author) => WorldDiplomacyBehavior.BuildCanonicalRealmGovernmentHardFact(Resolve(author), WorldDiplomacyBehavior.ResolveRealmRulerTitle(Resolve(author), Resolve(author)?.Leader ?? Resolve(author)?.RulingClan?.Leader));
        string IWorldDiplomacyDraftRepairWorld.NewId(string prefix) => WorldDiplomacyBehavior.NewId(prefix);
        void IWorldDiplomacyDraftRepairWorld.Log(string text) => WorldDiplomacyBehavior.Log(text);
    }
}
