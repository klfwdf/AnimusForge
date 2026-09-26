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
    private bool? _lastMapNotificationsEnabled;
    private DateTime _nextNotificationPollUtc = DateTime.MinValue;
    internal void Reset()
    {
        ResetView();
        _lastMapNotificationsEnabled = null;
        _nextNotificationPollUtc = DateTime.MinValue;
    }
    internal void ResetView() => _notifiedDocumentIdsThisSession.Clear();
    internal void Poll(WorldDiplomacyStorage storage, DateTime nowUtc, IWorldDiplomacyNotificationSink sink)
    {
        if (nowUtc < _nextNotificationPollUtc) return;
        _nextNotificationPollUtc = nowUtc.AddSeconds(1d);
        foreach (WorldDiplomacyDocument rumor in WorldDiplomacyPropagationApplication.SelectPendingRumors(storage, 3))
        {
            WorldDiplomacyPropagationApplication.MarkRumorNotified(rumor);
            sink.ShowRumor(WorldDiplomacyTextRules.BuildDiplomacyRumor(rumor, sink.KingdomName));
            sink.Log("diplomacy-rumor.shown document=" + rumor.DocumentId + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
        }
        bool enabled = sink.MapNotificationsEnabled;
        if (!enabled)
        {
            foreach (WorldDiplomacyDocument document in storage.Documents.Where(x => x != null
                && !x.IsPlayerAuthored && x.IsReadyForPublication && x.HasReachedPlayerCourt && !x.FormalNoticeShown))
            {
                document.FormalNoticeShown = true;
                document.IsNotified = true;
            }
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
        foreach (WorldDiplomacyDocument document in WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(storage.Documents
                .Where(x => x != null
                    && !x.IsPlayerAuthored
                    && x.IsReadyForPublication
                    && x.HasReachedPlayerCourt
                    && !x.IsRead
                    && !x.FormalNoticeShown
                    && !_notifiedDocumentIdsThisSession.Contains(x.DocumentId ?? ""))).Take(3))
        {
            try
            {
                _notifiedDocumentIdsThisSession.Add(document.DocumentId);
                sink.ShowNotice(new WorldDiplomacyNotice(
                    document.DocumentId,
                    WorldDiplomacyTextRules.BuildDisplayedDocumentTitle(document),
                    WorldDiplomacyTextRules.BuildNotificationDescription(document, sink.FormatCampaignDate)));
                document.IsNotified = true;
                document.FormalNoticeShown = true;
                sink.Log("formal-court-notice.shown document=" + document.DocumentId + " realm=" + sink.PlayerKingdomId
                    + " day=" + sink.CurrentDay.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                _notifiedDocumentIdsThisSession.Remove(document.DocumentId ?? "");
                sink.Log("notification publish failed: " + ex.Message);
                break;
            }
        }
    }
}
