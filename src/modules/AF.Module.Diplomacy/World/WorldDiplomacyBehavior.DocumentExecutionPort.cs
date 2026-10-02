using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal sealed class DocumentExecutionPort : IWorldDiplomacyDocumentExecutionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        // Per-command identity cache: at most the author and bounded action targets.
        // Discarded synchronously with this port; never retained by background work.
        private readonly Dictionary<string, TaleWorlds.CampaignSystem.Kingdom> _parties =
            new Dictionary<string, TaleWorlds.CampaignSystem.Kingdom>(StringComparer.OrdinalIgnoreCase);
        private TaleWorlds.CampaignSystem.Kingdom ResolveParty(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (!_parties.TryGetValue(id, out var party))
            {
                party = WorldDiplomacyBehavior.ResolveKingdom(id);
                _parties.Add(id, party);
            }
            return party;
        }
        internal DocumentExecutionPort(WorldDiplomacyBehavior owner) => _owner = owner;
        public string ResolveKingdomId(string id) => ResolveParty(id)?.StringId;
        public bool IsEliminated(string id) => ResolveParty(id)?.IsEliminated == true;
        public WorldDiplomacyAuthoritySnapshot CaptureAuthority(string id) => CaptureDiplomacyAuthority(ResolveParty(id));
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer) => WorldDiplomacyBehavior.FindRequiredPeaceOfferResponse(round, ResolveParty(author), slot, external, sourceId, requireAnyOpenPeaceOffer);
        public bool IsAtWar(string author, string target) => TaleWorlds.CampaignSystem.FactionManager.IsAtWarAgainstFaction(ResolveParty(author), ResolveParty(target));
        public bool IsPlayerKingdom(string id) => WorldDiplomacyBehavior.IsPlayerKingdom(ResolveParty(id));
        public string NewId(string prefix) => WorldDiplomacyBehavior.NewId(prefix);
        public WarPressureEntry FindWarPressure(string source, string target) => _owner._orchestration.FindWarPressure(source, target);
        public void AddWarPressure(string source, string target, int delta, string reason, string intent) => _owner._orchestration.AddWarPressure(source, target, delta, reason, intent);
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) => WorldDiplomacyBehavior.NormalizeKingdomIdList(values, excludedId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public void Notify(string message) => TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(message));
        public int MaxDiplomaticActionsPerDocument => WorldDiplomacyBehavior.MaxDiplomaticActionsPerDocument;
        public int MaxRelayParticipants => WorldDiplomacyBehavior.MaxRelayParticipants;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public IReadOnlyList<WorldDiplomacyThreat> Threats => _owner._storage?.DiplomaticThreats;
    }
}
