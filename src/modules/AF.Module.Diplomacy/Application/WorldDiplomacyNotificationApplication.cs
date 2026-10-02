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
    string PlayerKingdomId { get; }
    void ShowRumor(string text);
    void ShowNotice(WorldDiplomacyNotice notice);
    void Log(string text);
}

// One instance per campaign owner; Poll allocates nothing between the existing one-second deadlines.
internal sealed class WorldDiplomacyNotificationApplication
{
    private readonly HashSet<string> _notifiedDocumentIdsThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private List<WorldDiplomacyDocument> _pendingRumors = new List<WorldDiplomacyDocument>();
    private List<WorldDiplomacyDocument> _pendingFormal = new List<WorldDiplomacyDocument>();
    private List<WorldDiplomacyDocument> _pendingFormalIncludingRead = new List<WorldDiplomacyDocument>();
    private int _nextRumorIndex;
    private int _nextFormalIndex;
    private List<WorldDiplomacyDocument> _knownDocuments;
    private int _knownDocumentCount = -1;
    private bool _viewDirty = true;
    private bool _updatingOwnFlags;
    private bool? _lastMapNotificationsEnabled;
    private DateTime _nextNotificationPollUtc = DateTime.MinValue;
    internal int RebuildCount { get; private set; }

    private void InvalidateView()
    {
        if (!_updatingOwnFlags) _viewDirty = true;
    }

    private void EnsureView(WorldDiplomacyStorage storage)
    {
        List<WorldDiplomacyDocument> documents = storage?.Documents;
        int count = documents?.Count ?? 0;
        if (!_viewDirty && ReferenceEquals(_knownDocuments, documents) && _knownDocumentCount == count) return;
        _knownDocuments = documents;
        _knownDocumentCount = count;
        _viewDirty = false;
        RebuildCount++;
        _pendingRumors.Clear();
        _pendingFormal.Clear();
        _pendingFormalIncludingRead.Clear();
        _nextRumorIndex = 0;
        _nextFormalIndex = 0;
        if (documents == null) return;
        foreach (WorldDiplomacyDocument document in documents)
        {
            if (document == null) continue;
            document.NotificationSelectionChanged = InvalidateView;
            if (document.IsPlayerAuthored || !document.IsReadyForPublication) continue;
            if (!document.RumorNotified) _pendingRumors.Add(document);
            if (!document.HasReachedPlayerCourt || document.FormalNoticeShown) continue;
            _pendingFormalIncludingRead.Add(document);
            if (!document.IsRead && !_notifiedDocumentIdsThisSession.Contains(document.DocumentId ?? ""))
                _pendingFormal.Add(document);
        }
        // LINQ ordering is stable for equal dates, matching the original polling queries.
        _pendingRumors = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(_pendingRumors).ToList();
        _pendingFormal = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(_pendingFormal).ToList();
    }
    internal void Reset()
    {
        ResetView();
        _lastMapNotificationsEnabled = null;
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
        EnsureView(storage);
        for (int i = 0; i < 3 && _nextRumorIndex < _pendingRumors.Count; i++)
        {
            WorldDiplomacyDocument rumor = _pendingRumors[_nextRumorIndex++];
            _updatingOwnFlags = true;
            WorldDiplomacyPropagationApplication.MarkRumorNotified(rumor);
            _updatingOwnFlags = false;
            sink.ShowRumor(WorldDiplomacyTextRules.BuildDiplomacyRumor(rumor, sink.KingdomName));
            sink.Log("diplomacy-rumor.shown document=" + rumor.DocumentId + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
        }
        bool enabled = sink.MapNotificationsEnabled;
        if (!enabled)
        {
            _updatingOwnFlags = true;
            foreach (WorldDiplomacyDocument document in _pendingFormalIncludingRead)
            {
                if (document.FormalNoticeShown) continue;
                document.FormalNoticeShown = true;
                document.IsNotified = true;
            }
            _updatingOwnFlags = false;
            _pendingFormalIncludingRead.Clear();
            _pendingFormal.Clear();
            _nextFormalIndex = 0;
            if (_lastMapNotificationsEnabled != false)
            {
                _notifiedDocumentIdsThisSession.Clear();
            }
            _lastMapNotificationsEnabled = false;
            return;
        }
        _lastMapNotificationsEnabled = true;
        if (!sink.CanPublishMapNotification() || !sink.EnsureMapNotificationRegistered())
        {
            return;
        }
        for (int i = 0; i < 3 && _nextFormalIndex < _pendingFormal.Count; i++)
        {
            WorldDiplomacyDocument document = _pendingFormal[_nextFormalIndex];
            try
            {
                _notifiedDocumentIdsThisSession.Add(document.DocumentId);
                sink.ShowNotice(new WorldDiplomacyNotice(
                    document.DocumentId,
                    WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document),
                    WorldDiplomacyTextRules.BuildNotificationDescription(document, sink.FormatCampaignDate)));
                document.IsNotified = true;
                _updatingOwnFlags = true;
                document.FormalNoticeShown = true;
                _updatingOwnFlags = false;
                _nextFormalIndex++;
                sink.Log("formal-court-notice.shown document=" + document.DocumentId + " realm=" + sink.PlayerKingdomId
                    + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                _updatingOwnFlags = false;
                _notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
                sink.Log("notification publish failed: " + ex.Message);
                break;
            }
        }
    }
}
