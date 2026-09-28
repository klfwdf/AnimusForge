using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public sealed partial class WorldDiplomacyBehavior
{
    private readonly struct NoActionPort : IWorldDiplomacyNoActionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        private readonly Kingdom _author, _target;
        internal NoActionPort(WorldDiplomacyBehavior owner, Kingdom author, Kingdom target)
        { _owner = owner; _author = author; _target = target; }
        public bool AuthorResolved => _author != null;
        public bool TargetResolved => _target != null;
        public bool SameParty => _author == _target;
        public bool AuthorIsPlayer => IsPlayerKingdom(_author);
        public bool AuthorEliminated => _author?.IsEliminated == true;
        public bool TargetEliminated => _target?.IsEliminated == true;
        public bool AuthorHasAuthority => HasIndependentWorldDiplomacyAuthority(_author);
        public bool TargetHasAuthority => HasIndependentWorldDiplomacyAuthority(_target);
        public string AuthorId => _author?.StringId;
        public string TargetId => _target?.StringId;
        public int MaxParticipants => MaxRelayParticipants;
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public bool IsRepresentativeFor(WorldDiplomacyDocument document) => _owner.IsDiplomaticRepresentativeForAddressedVassal(_author, document);
    }
}
