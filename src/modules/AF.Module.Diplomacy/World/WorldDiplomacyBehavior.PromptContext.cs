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
        bool IWorldDiplomacyPromptWorld.IsAtWar(string author, string target) => FactionManager.IsAtWarAgainstFaction(Resolve(author), Resolve(target));
        WorldDiplomacyRound IWorldDiplomacyPromptWorld.ResolveRound(string id) => _owner.ResolveRound(id);
        WorldDiplomacyDocument IWorldDiplomacyPromptWorld.ResolveDocument(string id) => _owner.ResolveDocument(id);
        string IWorldDiplomacyPromptWorld.KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(Resolve(id));
        string IWorldDiplomacyPromptWorld.RulerName(string id) => WorldDiplomacyBehavior.RulerName(Resolve(id));
        int IWorldDiplomacyPromptWorld.GetRoundParticipantLimit() => WorldDiplomacyBehavior.GetRoundParticipantLimit();
        int IWorldDiplomacyPromptWorld.GetActivityLevel() => WorldDiplomacyBehavior.GetActivityLevel();
        int IWorldDiplomacyPromptWorld.CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        string IWorldDiplomacyPromptWorld.GetCommonDiplomacyContract(WorldDiplomacyRound round) => _owner.GetCommonDiplomacyContract(round);
        string IWorldDiplomacyPromptWorld.BuildWorldDiplomacyVassalageSnapshot() => WorldDiplomacyBehavior.BuildWorldDiplomacyVassalageSnapshot();
        string IWorldDiplomacyPromptWorld.BuildPolicySnapshot(string id) => WorldDiplomacyPolicyContext.BuildSnapshot(id);
        string IWorldDiplomacyPromptWorld.BuildGatheringSnapshot(IEnumerable<string> ids, int count) => NobleGatheringBehavior.BuildRecentDiplomacyMaterialForExternal(ids, count);
        string IWorldDiplomacyPromptWorld.BuildCompactRoundPlanCandidateLine(string author, string target, WorldDiplomacyRound round, IReadOnlyList<string> legalActions) => _owner.BuildCompactRoundPlanCandidateLine(Resolve(author), Resolve(target), round, legalActions);
        string IWorldDiplomacyPromptWorld.BuildWarDecisionContext(string author, string target, bool includePeaceNegotiationTerms) => _owner.BuildWarDecisionContext(Resolve(author), Resolve(target), includePeaceNegotiationTerms);
        WorldDiplomacyRoundOffer IWorldDiplomacyPromptWorld.FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string resultSettlementSlotId, bool isExternalResponseOnly, string sourceDocumentId, bool requireAnyOpenPeaceOffer) => WorldDiplomacyBehavior.FindRequiredPeaceOfferResponse(round, Resolve(author), resultSettlementSlotId, isExternalResponseOnly, sourceDocumentId, requireAnyOpenPeaceOffer);
        void IWorldDiplomacyPromptWorld.AppendDiplomaticAuthorDecisionContext(StringBuilder sb, string author, string roundId) => _owner.AppendDiplomaticAuthorDecisionContext(sb, Resolve(author), roundId);
        void IWorldDiplomacyPromptWorld.AppendDiplomaticTargetDecisionContext(StringBuilder sb, WorldDiplomacyRound round, string author, string target, bool includePeaceNegotiationTerms, IReadOnlyCollection<string> legalActions) => _owner.AppendDiplomaticTargetDecisionContext(sb, round, Resolve(author), Resolve(target), includePeaceNegotiationTerms, legalActions);
        void IWorldDiplomacyPromptWorld.AppendRulerCaptivityDecisionContext(StringBuilder sb, string author, string target) => WorldDiplomacyBehavior.AppendRulerCaptivityDecisionContext(sb, Resolve(author), Resolve(target));
        void IWorldDiplomacyPromptWorld.AppendOtherKingdomRelationshipContext(StringBuilder sb, string author, IEnumerable<string> ids) => _owner.AppendOtherKingdomRelationshipContext(sb, Resolve(author), ids);
        void IWorldDiplomacyPromptWorld.AppendRelayResponseSourceContext(StringBuilder sb, WorldDiplomacyRound round, string author, WorldDiplomacyDocument source, string requiredId) => _owner.AppendRelayResponseSourceContext(sb, round, Resolve(author), source, requiredId);
        void IWorldDiplomacyPromptWorld.AppendDiplomaticThreatAnalysisContext(StringBuilder sb, string author) => _owner.AppendDiplomaticThreatAnalysisContext(sb, Resolve(author));
        string IWorldDiplomacyDraftRepairWorld.BuildBilateralState(string author, string target) => WorldDiplomacyBehavior.BuildBilateralState(Resolve(author), Resolve(target));
        string IWorldDiplomacyDraftRepairWorld.BuildGovernmentHardFact(string author) => WorldDiplomacyBehavior.BuildCanonicalRealmGovernmentHardFact(Resolve(author), WorldDiplomacyBehavior.ResolveRealmRulerTitle(Resolve(author), Resolve(author)?.Leader ?? Resolve(author)?.RulingClan?.Leader));
        string IWorldDiplomacyDraftRepairWorld.NewId(string prefix) => WorldDiplomacyBehavior.NewId(prefix);
        void IWorldDiplomacyDraftRepairWorld.Log(string text) => WorldDiplomacyBehavior.Log(text);
    }
}
