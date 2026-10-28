using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyNotificationSink
{
    bool MapNotificationsEnabled { get; }
    bool CanPublishMapNotification();
    bool EnsureMapNotificationRegistered();
    string KingdomName(string id);
    string FormatCampaignDate(int day);
    int CurrentDay { get; }
    double CurrentHour { get; }
    void RecordShownTime(WorldDiplomacyDocument document, double hour);
    string PlayerKingdomId { get; }
    void ShowRumor(string text);
    void ShowNotice(WorldDiplomacyNotice notice);
    void Log(string text);
}

internal interface IWorldDiplomacyPlayerLocationSink
{
    string PlayerSettlementId { get; }
}

// One instance per campaign owner; Poll allocates nothing between the existing one-second deadlines.
internal sealed class WorldDiplomacyNotificationApplication
{
    private readonly Dictionary<string, string> _eventTitles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _notifiedDocumentIdsThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private List<WorldDiplomacyDocument> _pendingRumors = new List<WorldDiplomacyDocument>();
    private Queue<WorldDiplomacyDocument> _pendingFormal = new Queue<WorldDiplomacyDocument>();
    private int _nextRumorIndex;
    private List<WorldDiplomacyDocument> _knownDocuments;
    private int _knownDocumentCount = -1;
    private bool _viewDirty = true;
    private bool _updatingOwnFlags;
    private string _knownPlayerKingdomId;
    private string _knownPlayerSettlementId;
    private int _knownLocalDocumentCount = -1;
    private List<WorldDiplomacySettlementKnowledge> _knownSettlements;
    private readonly Dictionary<string, WorldDiplomacySettlementKnowledge> _settlements = new(StringComparer.OrdinalIgnoreCase);
    private int _knownSettlementCount = -1;
    private DateTime _nextNotificationPollUtc = DateTime.MinValue;
    internal int RebuildCount { get; private set; }

    private void InvalidateView()
    {
        if (!_updatingOwnFlags) _viewDirty = true;
    }

    private void EnsureView(WorldDiplomacyStorage storage, string playerKingdomId, string settlementId)
    {
        if (!ReferenceEquals(_knownSettlements, storage.SettlementKnowledge) || _knownSettlementCount != storage.SettlementKnowledge.Count)
        {
            _knownSettlements = storage.SettlementKnowledge; _knownSettlementCount = _knownSettlements.Count;
            _settlements.Clear();
            foreach (var item in _knownSettlements) if (item != null) _settlements[item.SettlementId] = item;
        }
        _settlements.TryGetValue(settlementId ?? "", out var local);
        int localCount = local?.DocumentIds?.Count ?? 0;
        if (_knownPlayerSettlementId != settlementId || _knownLocalDocumentCount != localCount) _viewDirty = true;
        _knownPlayerSettlementId = settlementId; _knownLocalDocumentCount = localCount;
        List<WorldDiplomacyDocument> documents = storage?.Documents;
        int count = documents?.Count ?? 0;
        if (!_viewDirty && ReferenceEquals(_knownDocuments, documents) && _knownDocumentCount == count
            && _knownPlayerKingdomId == playerKingdomId) return;
        _knownPlayerKingdomId = playerKingdomId;
        _knownDocuments = documents;
        _knownDocumentCount = count;
        _viewDirty = false;
        RebuildCount++;
        _eventTitles.Clear();
        foreach (var boundary in storage.EventAnnouncements ?? Enumerable.Empty<DiplomacyEventAnnouncement>())
            if (boundary?.State == "public") _eventTitles[boundary.EventId] = boundary.Title;
        foreach (var round in WorldDiplomacyLiveRoundRules.Live(storage).Concat(storage.CompletedRounds))
            if (round != null && !string.IsNullOrWhiteSpace(round.RoundTopic)) _eventTitles[round.RoundId] = round.RoundTopic;
        _pendingRumors.Clear();
        _pendingFormal.Clear();
        _nextRumorIndex = 0;
        if (documents == null) return;
        var locallyKnown = new HashSet<string>(local?.DocumentIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (WorldDiplomacyDocument document in documents)
        {
            if (document == null) continue;
            document.NotificationSelectionChanged = InvalidateView;
            if (document.IsPlayerAuthored || document.LegacyNoticeSuppressed || !document.IsReadyForPublication) continue;
            if (!document.RumorNotified) _pendingRumors.Add(document);
            if ((!document.HasReachedPlayerCourt && !locallyKnown.Contains(document.DocumentId)) || document.FormalNoticeShown) continue;
            // Lifecycle recovery explicitly resets both the shown flag and
            // timestamp to request a fresh notice without retrospective silence.
            if (document.ActualPlayerNoticeHour < 0)
                _notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
            if (!_notifiedDocumentIdsThisSession.Contains(document.DocumentId ?? ""))
                _pendingFormal.Enqueue(document);
        }
        // LINQ ordering is stable for equal dates, matching the original polling queries.
        _pendingRumors = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(_pendingRumors).ToList();
        _pendingFormal = new Queue<WorldDiplomacyDocument>(WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(_pendingFormal));
    }
    internal void Reset()
    {
        ResetView();
        _nextNotificationPollUtc = DateTime.MinValue;
    }
    internal void ResetView()
    {
        _notifiedDocumentIdsThisSession.Clear();
        _viewDirty = true;
    }
    internal void Poll(WorldDiplomacyStorage storage, DateTime nowUtc, IWorldDiplomacyNotificationSink sink)
    {
        if (nowUtc < _nextNotificationPollUtc) return;
        _nextNotificationPollUtc = nowUtc.AddSeconds(1d);
        EnsureView(storage, sink.PlayerKingdomId, (sink as IWorldDiplomacyPlayerLocationSink)?.PlayerSettlementId);
        for (int i = 0; i < 3 && _nextRumorIndex < _pendingRumors.Count; i++)
        {
            WorldDiplomacyDocument rumor = _pendingRumors[_nextRumorIndex++];
            _updatingOwnFlags = true;
            WorldDiplomacyPropagationApplication.MarkRumorNotified(rumor);
            _updatingOwnFlags = false;
            // Consume legacy rumor flags without displaying the removed player hint.
            sink.Log("diplomacy-rumor.consumed document=" + rumor.DocumentId + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
        }
        bool enabled = sink.MapNotificationsEnabled;
        if (!enabled || !sink.CanPublishMapNotification())
        {
            return;
        }
        string playerKingdomId = sink.PlayerKingdomId;
        // A fixed initial budget prevents reprocessing a failed display in the same poll.
        int budget = Math.Min(3, _pendingFormal.Count);
        for (int i = 0; i < budget; i++)
        {
            WorldDiplomacyDocument document = _pendingFormal.Dequeue();
            // A queued document can be reclassified before its turn in this batch.
            // Player-written declarations never produce an incoming notice.
            if (document.IsPlayerAuthored || document.LegacyNoticeSuppressed) continue;
            bool showOnMap = false;
            try
            {
                string title = WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document, DiplomacyUiLocalization.Text);
                string description = DiplomacyUiLocalization.Format("AF_DIP_UI_Notice_EventDeclaration",
                        "【Diplomatic Event: {TITLE}】\n【Speaker | {SPEAKER}】\n{BODY}",
                        ("TITLE", _eventTitles.TryGetValue(document.RoundId ?? "", out string eventTitle) ? eventTitle : title),
                        ("SPEAKER", BuildNoticeSpeaker(document)),
                        ("BODY", DiplomacyReplyTextRules.CleanNpcText(document.Body ?? "")));
                sink.ShowNotice(new WorldDiplomacyNotice(document.DocumentId, title, description, showOnMap));
                _notifiedDocumentIdsThisSession.Add(document.DocumentId);
                if (document.ActualPlayerNoticeHour < 0) document.ActualPlayerNoticeHour = sink.CurrentHour;
                sink.RecordShownTime(document, document.ActualPlayerNoticeHour);
                document.IsNotified = true;
                _updatingOwnFlags = true;
                document.FormalNoticeShown = true;
                _updatingOwnFlags = false;
                sink.Log("formal-court-notice.shown document=" + document.DocumentId + " realm=" + playerKingdomId
                    + " route=" + (showOnMap ? "map" : "text") + " author=" + document.AuthorKingdomId
                    + " target=" + document.TargetKingdomId
                    + " targets=" + string.Join(",", WorldDiplomacyStructureRules.GetDocumentTargetIds(document))
                    + " addressed=" + string.Join(",", document.AddressedKingdomIds ?? Enumerable.Empty<string>())
                    + " player=" + playerKingdomId + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                _updatingOwnFlags = false;
                _notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
                _pendingFormal.Enqueue(document);
                sink.Log("notification publish failed: " + ex.Message);
                break;
            }
        }
    }

    private static string BuildNoticeSpeaker(WorldDiplomacyDocument document)
    {
        // Publication snapshots remain authoritative after a succession or kingdom rename.
        // Old incomplete records must not be attributed to the current ruler.
        string kingdom = (document.AuthorKingdomName ?? "").Trim();
        string ruler = (document.AuthorRulerName ?? "").Trim();
        if (kingdom.Length > 0 && ruler.Length > 0) return kingdom + " · " + ruler;
        if (kingdom.Length > 0) return kingdom;
        if (ruler.Length > 0) return ruler;
        return DiplomacyUiLocalization.Text("AF_DIP_UI_Notice_UnknownSpeaker", "Unrecorded issuer");
    }
}
