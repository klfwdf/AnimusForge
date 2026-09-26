using System;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class NotificationWorld : IWorldDiplomacyNotificationSink
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal NotificationWorld(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool MapNotificationsEnabled => AreMapNotificationsEnabled();
        public bool CanPublishMapNotification() => WorldDiplomacyBehavior.CanPublishMapNotification();
        public bool EnsureMapNotificationRegistered() => _owner.TryEnsureMapNotificationRegistered();
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdom(id));
        public string FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public string PlayerKingdomId => Clan.PlayerClan?.Kingdom?.StringId ?? "";
        public void ShowRumor(string text) => InformationManager.DisplayMessage(new InformationMessage(text));
        public void ShowNotice(WorldDiplomacyNotice notice) => MBInformationManager.AddNotice(
            new WorldDiplomacyMapNotification(notice.DocumentId, notice.Title, notice.Description));
        public void Log(string text) => WorldDiplomacyBehavior.Log(text);
    }

    internal static IWorldDiplomacyPresentationPort ResolvePresentationPort()
    {
        WorldDiplomacyBehavior owner = ResolveInstance();
        return owner == null ? null : owner.PresentationPort;
    }
    private PresentationWorld _presentationPort;
    private PresentationWorld PresentationPort => _presentationPort ?? (_presentationPort = new PresentationWorld(this));

    private sealed class PresentationWorld : IWorldDiplomacyPresentationPort, IWorldDiplomacyPlayerWorld
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal PresentationWorld(WorldDiplomacyBehavior owner) { _owner = owner; }
        public WorldDiplomacyPlayerContext Player
        {
            get
            {
                Kingdom player = Clan.PlayerClan?.Kingdom;
                return new WorldDiplomacyPlayerContext(_owner._runtimeGeneration, player?.StringId,
                    player != null && !player.IsEliminated && player.RulingClan?.Leader == Hero.MainHero,
                    HasIndependentWorldDiplomacyAuthority(player), KingdomName(ResolveWorldDiplomacyRepresentative(player)));
            }
        }
        public System.Collections.Generic.IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() =>
            WorldDiplomacyPresentationQueries.Archive(_owner._storage, FormatCampaignDate, _owner.ResolveRound, _owner.ResolveDocument);
        public string ArchiveSubtitle() => WorldDiplomacyPresentationQueries.ArchiveSubtitle(
            _owner._storage, _owner.ResolveDocument, id =>
            {
                Kingdom representative = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
                return HasIndependentWorldDiplomacyAuthority(representative) ? KingdomName(representative) : null;
            }, id =>
            {
                Kingdom kingdom = ResolveKingdom(id);
                return kingdom == null ? null : KingdomName(kingdom);
            });
        public string Standing(string kingdomId) => WorldDiplomacyPresentationQueries.Standing(_owner._storage, kingdomId);
        public string Submit(WorldDiplomacyPlayerDocumentCommand command) => WorldDiplomacyPlayerApplication.Execute(this, command);
        public bool MarkRead(string id) => WorldDiplomacyPresentationQueries.MarkRead(_owner.ResolveDocument(id));
        public bool CanOpenReply(string documentId, string roundId, long generation)
        {
            return generation == _owner._runtimeGeneration && _owner.ResolveDocument(documentId) != null
                && _owner.ResolveRound(roundId) != null;
        }
        public WorldDiplomacyDocumentDetail Detail(string id)
        {
            WorldDiplomacyDocument document = _owner.ResolveDocument(id);
            if (document == null) return null;
            WorldDiplomacyRound round = _owner.ResolveRound(document.RoundId);
            WorldDiplomacyRoundParticipant participant = round?.Participants?.FirstOrDefault(x => x != null && IsPlayerKingdom(ResolveKingdom(x.KingdomId)));
            bool canReply = round != null && participant?.MandatoryReplyPending == true
                && HasIndependentWorldDiplomacyAuthority(Clan.PlayerClan?.Kingdom);
            return WorldDiplomacyPresentationQueries.Detail(document, _owner._runtimeGeneration, canReply, FormatCampaignDate);
        }
        public bool KingdomExists(string id) => ResolveKingdom(id) != null;
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyRound EnsureActiveRound(string author, string target, bool isPlayerInsertion) =>
            _owner.EnsureActiveRound(ResolveKingdom(author), ResolveKingdom(target), isPlayerInsertion);
        public WorldDiplomacyDocument CreateDocument(string author, string target, string title, string body, string origin,
            bool isPlayerAuthored, bool isResponse, string exchangeId) => _owner.CreateDocument(
                ResolveKingdom(author), ResolveKingdom(target), title, body, origin, isPlayerAuthored, isResponse, exchangeId);
        public void AddDocument(WorldDiplomacyDocument document) => _owner.AddDocument(document);
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document) => _owner.PublishPlayerAuthoredDocumentImmediately(document);
        public void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority) => _owner.EnqueueAnalysisJob(document, priority);
    }
}
