using System;using System.Collections.Concurrent;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Threading.Tasks;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed partial class WorldBulletinStateOwner {
 private WorldBulletinPort _port;internal void Bind(WorldBulletinPort port){_port=port;}
 internal WorldBulletinSaveState State;internal string CorruptRaw;internal bool InFlight;internal string LatestEventId="";internal int LastPruneDay=-1;
 internal readonly ConcurrentQueue<Action> MainThreadActions=new();
 private const string WorldBulletinBulletinIdMarker=":bulletin:";
internal WorldBulletinSaveState EnsureWorldBulletinState()
	{
		if (State == null)
		{
			int day = _port.CurrentDay();
			State = new WorldBulletinSaveState
			{
				TrackingStartDay = day,
				LastKingdomWeek = day / 7,
				PreservedUnreadableState = CorruptRaw
			};
		}
		State.Events ??= new List<WorldBulletinEvent>();
		State.World ??= new WorldBulletinScopeState();
		State.Player ??= new WorldBulletinScopeState();
		State.WeeklyStability ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		State.PendingNoticeEventIds ??= new List<string>();
		return State;
	}
internal static bool IsWorldBulletinEventId(string eventId)
	{
		return (eventId ?? "").IndexOf(WorldBulletinBulletinIdMarker, StringComparison.OrdinalIgnoreCase) >= 0;
	}
internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, params string[] kingdomIds)
	{
		return CaptureWorldBulletinEvent(kind, key, score, sentence, involvesPlayer, group, detail, Array.Empty<WorldBulletinParticipant>(), kingdomIds);
	}
internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, WorldBulletinParticipant[] participants, params string[] kingdomIds)
	{
		if (!_port.Enabled())
		{
			return false;
		}
		string text = (_port.Render(sentence) ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string normalizedKey = (key ?? "").Trim();
		if (text.Length == 0 || normalizedKey.Length == 0)
		{
			return false;
		}
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		List<WorldBulletinEvent> events = state.Events;
		// Duplicate callbacks for one fact arrive back to back; only the recent tail is checked.
		for (int i = events.Count - 1, checkedCount = 0; i >= 0 && checkedCount < 64; i--, checkedCount++)
		{
			if (string.Equals(events[i]?.Key, normalizedKey, StringComparison.Ordinal))
			{
				return false;
			}
		}
		double now = _port.CurrentHour();
		WorldBulletinEvent e = new WorldBulletinEvent
		{
			Key = normalizedKey,
			Kind = kind ?? "",
			Day = _port.CurrentDay(),
			Hour = now,
			Score = score,
			Sentence = text,
			Group = (group ?? "").Trim(),
			Detail = kind == "execution_last_words" ? (detail ?? "") : WorldBulletinPolicy.Truncate((_port.Render(detail) ?? "").Replace("\r", " ").Replace("\n", " "), 220),
			InvolvesPlayer = involvesPlayer,
			Participants = new List<WorldBulletinParticipant>(participants ?? Array.Empty<WorldBulletinParticipant>()),
			KingdomIds = (kingdomIds ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
		};
		events.Add(e);
		if (events.Count > WorldBulletinPolicy.MaxEvents)
		{
			events.RemoveAt(0);
		}
		if (WorldBulletinPolicy.IsTrigger(e, _port.Focus()) && !WorldBulletinPolicy.TryOpenWindow(state.World, e.Hour, now))
		{
			state.World.PendingTrigger = true;
		}
		_port.Log("WorldBulletin", "[Capture] kind=" + e.Kind + " score=" + score + " player=" + involvesPlayer + " group=" + e.Group + " key=" + normalizedKey);
		return true;
	}
internal static void PruneWorldBulletinWeeklyStability(WorldBulletinSaveState state, int currentWeek)
	{
		foreach (string key in state.WeeklyStability.Keys.ToList())
		{
			int bar = key.IndexOf('|');
			if (bar <= 0 || !int.TryParse(key.Substring(0, bar), NumberStyles.Integer, CultureInfo.InvariantCulture, out int week) || week < currentWeek - 1)
			{
				state.WeeklyStability.Remove(key);
			}
		}
	}
internal void AdvanceWorldBulletinScope(WorldBulletinSaveState state, double now)
	{
		if (InFlight)
		{
			return;
		}
		WorldBulletinScopeState scope = state.World;
		WorldBulletinFocus focus = _port.Focus();
		if (scope.WindowEndHour < 0)
		{
			if (!scope.PendingTrigger || now < scope.CooldownUntilHour)
			{
				return;
			}
			scope.PendingTrigger = false;
			double triggerHour = WorldBulletinPolicy.FindPendingTriggerHour(state.Events, scope, focus, now);
			if (triggerHour >= 0)
			{
				WorldBulletinPolicy.TryOpenWindow(scope, triggerHour, now);
			}
			return;
		}
		if (!WorldBulletinPolicy.IsWindowDue(scope, now))
		{
			return;
		}
		WorldBulletinSelection selection = WorldBulletinPolicy.Select(state.Events, scope, focus, now);
		if (selection?.Major == null)
		{
			WorldBulletinPolicy.AbandonWindow(scope);
			return;
		}
		if (!WorldBulletinPolicy.HasEnoughMinorNews(selection))
		{
			// Keep gathering real, deduplicated groups. Neither text nor image API starts yet.
			scope.WindowEndHour = now + 1.0;
			return;
		}
		long generation = SaveRuntimeGuard.CaptureGeneration();
        WorldBulletinPromptFacts facts = _port.CapturePromptFacts(selection, focus);
        WorldBulletinText template = WorldBulletinPolicy.BuildTemplate(selection);
        string userPrompt = WorldBulletinPolicy.BuildUserPrompt(facts.ScopeLine, facts.Date, selection, facts.KingdomContext);
        string systemPrompt = WorldBulletinPolicy.BuildSystemPrompt(selection.MajorFacts.Count, selection.Minors.Count);
		WorldBulletinIllustrationPlan illustrationPlan = WorldBulletinPolicy.BuildIllustrationPlan(selection,
			"selection:" + (scope.Sequence + 1).ToString(CultureInfo.InvariantCulture) + ":" + scope.WindowEndHour.ToString("R", CultureInfo.InvariantCulture),
			_port.CurrentDate());
		_port.Log("WorldBulletin", "[Select] home=" + focus.PlayerKingdomId + " majorFacts=" + selection.MajorFacts.Count + " minors=" + selection.Minors.Count + " minorEvents=" + selection.Minors.Sum(x => x.Events.Count));
		// Set last: if anything above throws, the flag stays clear and the next tick retries instead of blocking forever.
		InFlight = true;
		try { _port.PrepareSelection(illustrationPlan); }
		catch (Exception ex) { _port.Log("WorldBulletin", "[Illustration] selected-event preparation failed: " + ex.Message); }
		_ = RunWorldBulletinRequestAsync(scope.WindowEndHour, generation, selection, template, systemPrompt, userPrompt, illustrationPlan);
	}
internal async Task RunWorldBulletinRequestAsync(double windowEndHour, long generation, WorldBulletinSelection selection, WorldBulletinText template, string systemPrompt, string userPrompt, WorldBulletinIllustrationPlan illustrationPlan)
	{
		WorldBulletinText result = null;
		try
		{
			ApiCallResult api = await _port.CallApi(systemPrompt, userPrompt).ConfigureAwait(false);
			if (api != null && api.Success)
			{
				result = WorldBulletinPolicy.ParseResponse(api.Content, selection.Minors.Count);
				if (result == null)
				{
					_port.Log("WorldBulletin", "[WARN] response unparsable, using template. raw=" + WorldBulletinPolicy.Truncate(api.Content, 200));
				}
			}
			else
			{
				_port.Log("WorldBulletin", "[WARN] request failed, using template: " + (api?.ErrorMessage ?? "null"));
			}
		}
		catch (Exception ex)
		{
			_port.Log("WorldBulletin", "[WARN] request threw, using template: " + ex.Message);
		}
		MainThreadActions.Enqueue(delegate
		{
			CompleteWorldBulletin(windowEndHour, generation, selection, template, result, illustrationPlan);
		});
	}
internal void ProcessWorldBulletinMainThreadActions()
	{
		int processed = 0;
		while (processed < 4 && MainThreadActions.TryDequeue(out Action action))
		{
			processed++;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				_port.Log("WorldBulletin", "[ERROR] main-thread action failed: " + ex);
			}
		}
	}
internal void CompleteWorldBulletin(double windowEndHour, long generation, WorldBulletinSelection selection, WorldBulletinText template, WorldBulletinText generated, WorldBulletinIllustrationPlan illustrationPlan)
	{
		if (SaveRuntimeGuard.IsStale(generation, "world_bulletin_complete"))
		{
			return;
		}
		InFlight = false;
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		WorldBulletinScopeState scope = state.World;
		if (Math.Abs(scope.WindowEndHour - windowEndHour) > 0.001)
		{
			_port.CancelIllustration(illustrationPlan);
			return;
		}
		// Switched off (or auto reports disabled) while the request was out: close the window, publish nothing.
		if (!_port.PublishingEnabled())
		{
			WorldBulletinPolicy.AbandonWindow(scope);
			_port.CancelIllustration(illustrationPlan);
			_port.Log("WorldBulletin", "[Publish] skipped: bulletin publishing turned off during the request");
			return;
		}
		try
		{
			PublishWorldBulletin(scope, selection, template, generated, illustrationPlan);
		}
		catch (Exception ex)
		{
			// Never leave the window due after a failure, or every hourly tick would re-request the LLM.
			if (Math.Abs(scope.WindowEndHour - windowEndHour) <= 0.001)
			{
				WorldBulletinPolicy.AbandonWindow(scope);
			}
			_port.Log("WorldBulletin", "[ERROR] publish failed, window closed: " + ex);
			_port.CancelIllustration(illustrationPlan);
		}
	}
internal void PublishWorldBulletin(WorldBulletinScopeState scope, WorldBulletinSelection selection, WorldBulletinText template, WorldBulletinText generated, WorldBulletinIllustrationPlan illustrationPlan)
	{
		WorldBulletinText text = generated ?? template;
		string title = string.IsNullOrWhiteSpace(text.Title) ? template.Title : text.Title;
		int polishedMinors = 0;
		List<string> minors = generated != null ? WorldBulletinPolicy.MergeMinors(generated.Minors, selection.Minors, out polishedMinors) : template.Minors;
		if (generated != null && polishedMinors < selection.Minors.Count)
		{
			_port.Log("WorldBulletin", "[WARN] model returned " + polishedMinors + "/" + selection.Minors.Count + " minors; template lines fill the rest");
		}
		string body = WorldBulletinPolicy.BuildBody(text.Major, minors);
		string shortText = !string.IsNullOrWhiteSpace(text.Short) ? WorldBulletinPolicy.Truncate(text.Short, 140) : template.Short;
		double now = _port.CurrentHour();
		WorldBulletinPolicy.CompletePublish(scope, now, selection.Major.Key);
		int day = _port.CurrentDay();
		string seq = scope.Sequence.ToString(CultureInfo.InvariantCulture);
		// The single bulletin: world-kind record (diplomacy history and the NPC "world" layer read it), and it pops.
		string eventId = "weekly_report:world" + WorldBulletinBulletinIdMarker + seq + ":" + day;
		UpsertWorldBulletinRecord(eventId, "world", "", title, shortText, body, day, WeeklyReportArchivePolicy.CaptureKingdomIds(selection));
		LatestEventId = eventId;
		RecordWorldBulletinLayout(eventId, selection);
		WorldBulletinLayout publishedLayout = FindWorldBulletinLayout(eventId);
		if (publishedLayout != null) publishedLayout.IllustrationPlan = illustrationPlan;
        try { if (illustrationPlan == null) _port.PrepareIssue(eventId); }
        catch (Exception ex) { _port.Log("WorldBulletin", "[Illustration] preparation failed: " + ex.Message); }
		QueueNoticeAfterIllustration(eventId, illustrationPlan);
		_port.Log("WorldBulletin", "[Publish] id=" + eventId + " llm=" + (generated != null) + " major=" + selection.Major.Key + " majorFacts=" + selection.MajorFacts.Count + " minors=" + polishedMinors + "/" + selection.Minors.Count + " majorChars=" + (text.Major ?? "").Length);
	}
// The record is already saved; only the map notice waits so the player opens the issue with its art.
	// One callback per issue, no polling. A load/new campaign in between makes the release a no-op.
	internal void QueueNoticeAfterIllustration(string eventId, WorldBulletinIllustrationPlan illustrationPlan)
	{
		WorldBulletinSaveState state = EnsureWorldBulletinState();
		if (!state.PendingNoticeEventIds.Contains(eventId)) state.PendingNoticeEventIds.Add(eventId);
		long generation = SaveRuntimeGuard.CaptureGeneration();
		bool released = false;
		void Release()
		{
			if (released || SaveRuntimeGuard.IsStale(generation, "world_bulletin_notice") || !ReferenceEquals(State, state)) return;
			ReleasePendingWorldBulletinNotice(state, eventId);
			released = true;
		}
		bool waiting = false;
		try { waiting = illustrationPlan != null && _port.AwaitIllustration != null && _port.AwaitIllustration(illustrationPlan, Release); }
		catch (Exception ex) { _port.Log("WorldBulletin", "[Illustration] notice wait failed, notifying now: " + ex.Message); }
		if (waiting) _port.Log("WorldBulletin", "[Notice] id=" + eventId + " waiting for illustration");
		else Release();
	}
internal void UpsertWorldBulletinRecord(string eventId, string eventKind, string scopeKingdomId, string title, string shortSummary, string summary, int day, List<string> bulletinKingdomIds = null)
	{
		List<EventRecordEntry> records = _port.Records();
		EventRecordEntry entry = _port.FindRecord(eventId);
		string previous = _port.ProductState(entry);
		if (entry == null)
		{
			entry = new EventRecordEntry { EventId = eventId };
			records.Add(entry);
		}
		entry.EventKind = eventKind;
		entry.ScopeKingdomId = scopeKingdomId ?? "";
        if (bulletinKingdomIds != null) entry.BulletinKingdomIds = bulletinKingdomIds;
		entry.WeekIndex = Math.Max(0, day / 7);
		entry.Title = WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(title);
		entry.Summary = WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(summary);
		entry.ShortSummary = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(shortSummary);
		if (string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			entry.ShortSummary = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(entry.Summary);
		}
		entry.TagText = "";
		entry.PromptText = "";
		entry.CreatedDay = day;
		entry.CreatedDate = _port.CurrentDate();
		entry.Materials = new List<EventMaterialReference>();
		_port.NotifyProductChanged(previous, entry);
		_port.NotifyTimeline();
	}
internal void WriteWorldBulletinKingdomBriefs(WorldBulletinSaveState state, int day)
	{
		int week = day / 7;
		if (week < 1 || week <= state.LastKingdomWeek)
		{
			return;
		}
		state.LastKingdomWeek = week;
		int startDay = week * 7 - 7;
		bool fullWindow = state.TrackingStartDay >= 0 && state.TrackingStartDay <= startDay;
		int written = 0;
		foreach (KeyValuePair<string,string> kingdom in _port.EligibleKingdoms())
		{
			string kingdomId = kingdom.Key;
			List<WorldBulletinEvent> facts = state.Events.Where(e => e != null && e.Day >= startDay && WorldBulletinPolicy.InvolvesKingdom(e, kingdomId)).ToList();
			if (facts.Count == 0 && !fullWindow)
			{
				continue;
			}
			string name = kingdom.Value;
			string template = WorldBulletinPolicy.BuildKingdomTemplate(name, facts);
			string eventId = "weekly_report:kingdom:" + week + ":" + kingdomId + ":brief";
			UpsertWorldBulletinRecord(eventId, "kingdom", kingdomId, name + "第" + week + "周局势提要", template, template, day);
			written++;
		}
		_port.Log("WorldBulletin", "[KingdomBrief] week=" + week + " written=" + written);
	}
internal void OnWorldBulletinHourlyTick()
	{
		if (!_port.Enabled())
		{
			return;
		}
		try
		{
			WorldBulletinSaveState state = EnsureWorldBulletinState();
			int day = _port.CurrentDay();
			// Keep the legacy cursor current, so switching back to weekly reports resumes at this week
			// instead of replaying every week the bulletin covered.
			_port.SetAutoWeek(Math.Max(_port.AutoWeek(), day / 7));
			if (day != LastPruneDay)
			{
				LastPruneDay = day;
				WorldBulletinPolicy.Prune(state.Events, day);
				PruneWorldBulletinWeeklyStability(state, day / 7);
			}
			if (!_port.PublishingEnabled())
			{
				return;
			}
			AdvanceWorldBulletinScope(state, _port.CurrentHour());
			WriteWorldBulletinKingdomBriefs(state, day);
		}
		catch (Exception ex)
		{
			_port.Log("WorldBulletin", "[ERROR] hourly tick: " + ex);
		}
	}
internal void ApplyStability(string kingdomId,int delta,int day,int currentValue,Action<int> applyValue) { var state=EnsureWorldBulletinState();string key=(day/7).ToString(CultureInfo.InvariantCulture)+"|"+kingdomId;state.WeeklyStability.TryGetValue(key,out int currentTotal);int applied=WorldBulletinPolicy.ClampWeeklyStability(currentTotal,delta,out int newTotal);if(applied==0)return;state.WeeklyStability[key]=newTotal;applyValue(currentValue+applied);}
 // Queue transfers only run on the existing main-thread drain, after save records/notices have loaded.
 // Reset clears stale work before rebuilding; no archive scan or tick polling is added.
 private void ReleasePendingWorldBulletinNotice(WorldBulletinSaveState state, string eventId)
 {
     if (!state.PendingNoticeEventIds.Contains(eventId)) return;
     if (_port.FindRecord(eventId) != null) _port.QueueNotice(eventId);
     state.PendingNoticeEventIds.Remove(eventId);
 }
 internal void ResetTransient()
 {
     InFlight=false;LastPruneDay=-1;LatestEventId="";CachedRecords=null;CachedRecordCount=-1;CachedRecordIndex=-1;
     while(MainThreadActions.TryDequeue(out _)){}
     var state = State;
     if (state?.PendingNoticeEventIds == null || state.PendingNoticeEventIds.Count == 0) return;
     long generation = SaveRuntimeGuard.CaptureGeneration();
     foreach (string eventId in state.PendingNoticeEventIds.Distinct(StringComparer.Ordinal).ToArray())
         MainThreadActions.Enqueue(() =>
         {
             if (SaveRuntimeGuard.IsStale(generation, "world_bulletin_notice_recovery") || !ReferenceEquals(State, state)) return;
             ReleasePendingWorldBulletinNotice(state, eventId);
         });
 }
 internal void ResetRuntime(string reason)
 {
     if(string.Equals(reason,"new_game_created",StringComparison.Ordinal)){State=null;CorruptRaw=null;}
     ResetTransient();
 }

internal bool TryRecordCoupOutcomeForBulletin(string coupId, bool success, string sentence, string detail, string kingdomId, string actorKingdomId)
	{
		return TryRecordCoupOutcomeWithParticipantsForBulletin(coupId, success, sentence, detail, kingdomId, actorKingdomId, "", "");
	}
internal bool TryRecordCoupOutcomeWithParticipantsForBulletin(string coupId, bool success, string sentence, string detail, string kingdomId, string actorKingdomId, string actorId, string formerKingId)
	{
		if (string.IsNullOrWhiteSpace(coupId) || string.IsNullOrWhiteSpace(sentence)) return false;
		if (!_port.Enabled()) return true;
		string key = "coup:" + coupId.Trim() + ":bulletin";
		// Cold outcome/retry path; inspect the bounded retained set, not only the 64-event tail.
		if (EnsureWorldBulletinState().Events.Any(e => e != null && string.Equals(e.Key, key, StringComparison.Ordinal))) return true;
		return CaptureWorldBulletinEvent(success ? "coup_success" : "coup_failure", key, success ? 95 : 80,
			sentence, true, "coup:" + coupId.Trim(), detail, new[] {
				new WorldBulletinParticipant { HeroId = actorId ?? "", Role = "政变发动者" },
				new WorldBulletinParticipant { HeroId = formerKingId ?? "", Role = "政变针对的原国王" }
			}, kingdomId, actorKingdomId);
	}
internal string ExportJson(){if(State!=null&&CorruptRaw!=null)State.PreservedUnreadableState=CorruptRaw;return State!=null?Newtonsoft.Json.JsonConvert.SerializeObject(State):(CorruptRaw??"");}
 internal void ImportJson(string loaded){CorruptRaw=null;try{State=string.IsNullOrWhiteSpace(loaded)?null:Newtonsoft.Json.JsonConvert.DeserializeObject<WorldBulletinSaveState>(loaded);}catch(Exception ex){State=null;CorruptRaw=loaded;_port.Log("WorldBulletin","[ERROR] saved state unreadable, preserved raw ("+loaded.Length+" chars) key=_af_worldBulletin_v1: "+ex.Message);}}

}
internal sealed class WorldBulletinPromptFacts {internal string ScopeLine,Date;internal List<string> KingdomContext;}
internal sealed class WorldBulletinPort {
 internal Func<string,string> ResolveKingdom;internal Func<EventRecordEntry,string> NoticeTitle,PopupSubtitle,PopupBody;
 internal Func<int> AutoWeek;internal Action<int> SetAutoWeek;
 internal Func<bool> Enabled,PublishingEnabled;internal Func<int> CurrentDay;internal Func<double> CurrentHour;internal Func<string> CurrentDate;internal Func<WorldBulletinFocus> Focus;internal Func<string,string> Render;
 internal Func<WorldBulletinSelection,WorldBulletinFocus,WorldBulletinPromptFacts> CapturePromptFacts;
 internal Action<WorldBulletinIllustrationPlan> PrepareSelection,CancelIllustration;internal Action<string> PrepareIssue,QueueNotice;
 internal Func<WorldBulletinIllustrationPlan,Action,bool> AwaitIllustration;
 internal Func<List<EventRecordEntry>> Records;internal Func<string,EventRecordEntry> FindRecord;internal Func<EventRecordEntry,string> ProductState;internal Action<string,EventRecordEntry> NotifyProductChanged;internal Action NotifyTimeline;
 internal Func<List<KeyValuePair<string,string>>> EligibleKingdoms;internal Func<string,string,Task<ApiCallResult>> CallApi;internal Action<string,string> Log;
}
