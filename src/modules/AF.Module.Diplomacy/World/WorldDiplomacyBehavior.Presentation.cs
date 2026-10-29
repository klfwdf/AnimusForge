using AnimusForge;
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
    private static double CurrentDiplomacyHours()
    {
        if (Campaign.Current == null || !Campaign.Current.GameStarted) return 0;
        try { return Math.Max(0, CampaignTime.Now.ToHours); }
        catch { return CurrentHour(); }
    }
    private sealed class NotificationWorld : IWorldDiplomacyNotificationSink, IWorldDiplomacyPlayerLocationSink
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal NotificationWorld(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool MapNotificationsEnabled => AreMapNotificationsEnabled();
        public bool CanPublishMapNotification() => TaleWorlds.MountAndBlade.Mission.Current == null
            && Game.Current?.GameStateManager?.ActiveState is TaleWorlds.CampaignSystem.GameState.MapState;
        public bool EnsureMapNotificationRegistered() => _owner.TryEnsureMapNotificationRegistered();
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdom(id));
        public string FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public double CurrentHour => CurrentDiplomacyHours();
        public void RecordShownTime(WorldDiplomacyDocument document, double hour) => _owner._orchestration.RecordActualPlayerNotice(document, hour);
        public string PlayerKingdomId => Clan.PlayerClan?.Kingdom?.StringId ?? "";
        public string PlayerSettlementId => TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement?.StringId ?? "";
        public void ShowRumor(string text) => InformationManager.DisplayMessage(new InformationMessage(text));
        public void ShowNotice(WorldDiplomacyNotice notice)
        {
            if (notice.ShowOnMap)
                MBInformationManager.AddNotice(new WorldDiplomacyMapNotification(notice.DocumentId, notice.Title, notice.Description));
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    notice.Description ?? "", Colors.Yellow));
            }
        }
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
                if (!_owner.IsLiveCampaign) return default;
                Kingdom player = Clan.PlayerClan?.Kingdom;
                return new WorldDiplomacyPlayerContext(_owner._runtimeGeneration, player?.StringId,
                    player != null && !player.IsEliminated && player.RulingClan?.Leader == Hero.MainHero,
                    HasIndependentWorldDiplomacyAuthority(player), KingdomName(ResolveWorldDiplomacyRepresentative(player)));
            }
        }
        public System.Collections.Generic.IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() =>
            _owner.IsLiveCampaign ? WorldDiplomacyPresentationQueries.Archive(_owner._storage, FormatCampaignDate, _owner.ResolveRound, _owner.ResolveDocument, Player) : Array.Empty<WorldDiplomacyArchiveRecord>();
        public string ArchiveSubtitle() => !_owner.IsLiveCampaign ? _owner.UnavailableReason : WorldDiplomacyPresentationQueries.ArchiveSubtitle(
            _owner._storage, _owner.ResolveDocument, id =>
            {
                Kingdom representative = ResolveWorldDiplomacyRepresentative(ResolveKingdom(id));
                return HasIndependentWorldDiplomacyAuthority(representative) ? KingdomName(representative) : null;
            }, id =>
            {
                Kingdom kingdom = ResolveKingdom(id);
                return kingdom == null ? null : KingdomName(kingdom);
            });
        public string Standing(string kingdomId) => _owner.IsLiveCampaign ? WorldDiplomacyPresentationQueries.Standing(_owner._storage, kingdomId) : "";
        public string Submit(WorldDiplomacyPlayerDocumentCommand command) => _owner.IsLiveCampaign ? WorldDiplomacyPlayerApplication.Execute(this, command, _owner._orchestration) : _owner.UnavailableReason;
        public bool MarkRead(string id) => _owner.IsLiveCampaign && _owner._orchestration.MarkDocumentRead(id);
        public string RetryAnalysis(string id, long generation)
        {
            if (!_owner.IsLiveCampaign) return _owner.UnavailableReason;
            if (generation != _owner._runtimeGeneration) return DiplomacyMiscPromptTexts.Get("Presentation_DocumentPageExpired", "This document page has expired; please reopen it.");
            var document = _owner.ResolveDocument(id);
            if (!WorldDiplomacyPlayerApplication.CanRetryAnalysis(document, Player)) return DiplomacyMiscPromptTexts.Get("Presentation_CannotReparse", "This document cannot be reparsed currently.");
            document.AnalysisStatus = "pending_analysis";
            document.MechanicalResult = DiplomacyMiscPromptTexts.Get("Presentation_ReparsingPending", "Declaration published; reparsing original text, diplomatic action not yet executed.");
            _owner._orchestration.EnqueueAnalysisJob(document, 100);
            return DiplomacyMiscPromptTexts.Get("Presentation_ReparsedResubmitted", "Original text resubmitted for parsing; declaration will not be published again.");
        }
        public bool CanOpenReply(string documentId, string roundId, long generation)
        {
            return _owner.IsLiveCampaign && generation == _owner._runtimeGeneration && _owner.ResolveDocument(documentId) != null
                && Detail(documentId)?.CanReply == true;
        }
        public WorldDiplomacyDocumentDetail Detail(string id)
        {
            if (!_owner.IsLiveCampaign) return null;
            WorldDiplomacyDocument document = _owner.ResolveDocument(id);
            if (document == null) return null;
            WorldDiplomacyRound round = _owner.ResolveRound(document.RoundId);
            return WorldDiplomacyPresentationQueries.Detail(document, round, Player, FormatCampaignDate);
        }
        public bool KingdomExists(string id) => _owner.IsLiveCampaign && ResolveKingdom(id) != null;
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.IsLiveCampaign ? _owner.ResolveDocument(id) : null;
        public WorldDiplomacyRound ResolveRound(string id) => _owner.IsLiveCampaign ? _owner.ResolveRound(id) : null;
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
    }
}
