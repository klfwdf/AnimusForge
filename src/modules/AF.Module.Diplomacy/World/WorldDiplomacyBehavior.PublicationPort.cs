using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal sealed class PublicationPort : IWorldDiplomacyPublicationPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal PublicationPort(WorldDiplomacyBehavior owner) => _owner = owner;
        public WorldDiplomacyStorage Storage => _owner._storage;
        public string ResolveKingdomId(string id) => ResolveKingdom(id)?.StringId;
        public bool CanAiAuthor(string id, out string reason) => CanAiAuthorDiplomaticDocument(ResolveKingdom(id), out reason);
        public bool HasAuthority(string id) => HasIndependentWorldDiplomacyAuthority(ResolveKingdom(id));
        public bool IsPlayerAffiliated(string id) => IsPlayerAffiliatedKingdom(ResolveKingdom(id));
        public bool IsPlayerKingdom(string id) => WorldDiplomacyBehavior.IsPlayerKingdom(ResolveKingdom(id));
        public bool RepresentsAddressedVassal(string id, WorldDiplomacyDocument document) => _owner.IsDiplomaticRepresentativeForAddressedVassal(ResolveKingdom(id), document);
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public string ResolveOriginSettlementId(string author) => _owner.ResolveCourtSettlement(ResolveKingdom(author))?.StringId;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public int ParticipantLimit => GetRoundParticipantLimit();
        public int CivilianSpreadDays => GetCivilianSpreadDays();
        public int CourtDeliveryDays => GetCourtMaxDeliveryDays();
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public WorldDiplomacyPublicationSnapshot CaptureDestinations(string authorId, string originId)
        {
            return WorldDiplomacyGeographyApplication.Publication(new GeographyPort(_owner, authorId, originId));
        }
    }
}
