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
        public bool CanPublishMapNotification() => TaleWorlds.MountAndBlade.Mission.Current == null
            && Game.Current?.GameStateManager?.ActiveState is TaleWorlds.CampaignSystem.GameState.MapState;
        public bool EnsureMapNotificationRegistered() => _owner.TryEnsureMapNotificationRegistered();
        public string KingdomName(string id) => WorldDiplomacyBehavior.KingdomName(ResolveKingdom(id));
        public string FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public string PlayerKingdomId => Clan.PlayerClan?.Kingdom?.StringId ?? "";
        public void ShowRumor(string text) => InformationManager.DisplayMessage(new InformationMessage(text));
        public void ShowNotice(WorldDiplomacyNotice notice)
        {
            if (notice.ShowOnMap)
                MBInformationManager.AddNotice(new WorldDiplomacyMapNotification(notice.DocumentId, notice.Title, notice.Description));
            else
                InformationManager.DisplayMessage(new InformationMessage(
                    "【第三方外交消息】" + notice.Title + "。消息已传至本国，可在王国公告中查看全文。", Colors.Yellow));
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
            if (generation != _owner._runtimeGeneration) return "该公文页面已失效，请重新打开。";
            var document = _owner.ResolveDocument(id);
            if (!WorldDiplomacyPlayerApplication.CanRetryAnalysis(document, Player)) return "该公文当前不能重新解析。";
            document.AnalysisStatus = "pending_analysis";
            document.MechanicalResult = "宣言已公开；正在重新解析原文，尚未执行外交动作。";
            _owner._orchestration.EnqueueAnalysisJob(document, 100);
            return "已重新提交原文解析；不会再次发布宣言。";
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
