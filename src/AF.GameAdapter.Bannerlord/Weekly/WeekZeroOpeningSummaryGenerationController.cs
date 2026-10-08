using WeeklyPromptSnapshot = AnimusForge.MyBehavior.WeeklyPromptSnapshot;
using TaleWorlds.Library;
using WeeklyPromptReportSnapshot = AnimusForge.MyBehavior.WeeklyPromptReportSnapshot;
using TaleWorlds.CampaignSystem;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;
using WeekZeroShortSummaryRequest = AnimusForge.MyBehavior.WeekZeroShortSummaryRequest;
using ApiCallResult = AnimusForge.MyBehavior.ApiCallResult;
using static AnimusForge.WeeklyGenerationRules;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
namespace AnimusForge;

// Runtime-only queue. Legacy record and request identities remain in MyBehavior.
internal sealed class WeekZeroOpeningSummaryGenerationController
{
 private readonly WeeklyEventRecordStateOwner _records;
 private readonly Func<int> _interval;
 private readonly Func<bool> _bulletinEnabled, _ownerCurrent;
 private readonly Func<string,string,string,string> _systemPrompt;
 private readonly Func<string,string> _userPrompt;
 private readonly Func<string,string,Task<ApiCallResult>> _call;
 private readonly Action _notify;
 private readonly Func<string,EventRecordEntry> _findRecord;
 private List<EventRecordEntry> _eventRecordEntries { get => _records.Records; set => _records.Records=value; }
 private readonly object _weekZeroShortSummaryQueueLock = new object();
 private readonly HashSet<string> _weekZeroShortSummaryGenerationInFlight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 private readonly HashSet<string> _weekZeroShortSummaryGenerationAttempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 private readonly List<WeekZeroShortSummaryRequest> _weekZeroShortSummaryPendingQueue = new List<WeekZeroShortSummaryRequest>();
 private readonly ConcurrentQueue<Action> _weekZeroShortSummaryMainThreadActions = new ConcurrentQueue<Action>();
 private bool _weekZeroShortSummaryQueueProcessing;
 private long _weekZeroShortSummaryLastRequestUtcTicks;
 private long _revision, _generation;
 private readonly Dictionary<WeekZeroShortSummaryRequest, Submission> _submissions = new Dictionary<WeekZeroShortSummaryRequest, Submission>();
 private sealed class Submission
 {
  internal long Generation, Revision;
  internal string SystemPrompt, UserPrompt;
 }
 internal WeekZeroOpeningSummaryGenerationController(WeeklyEventRecordStateOwner records, Func<int> interval,
  Func<bool> bulletinEnabled, Func<bool> ownerCurrent, Func<string,string,string,string> systemPrompt,
  Func<string,string> userPrompt, Func<string,string,Task<ApiCallResult>> call, Action notify, Func<string,EventRecordEntry> findRecord = null)
 {
  _records=records; _interval=interval; _bulletinEnabled=bulletinEnabled; _ownerCurrent=ownerCurrent;
  _systemPrompt=systemPrompt; _userPrompt=userPrompt; _call=call; _notify=notify; _findRecord=findRecord;
  _generation=SaveRuntimeGuard.CaptureGeneration();
 }
 private bool IsCurrent(Submission submission) => submission != null
  && submission.Revision == Interlocked.Read(ref _revision)
  && SaveRuntimeGuard.IsCurrentGeneration(submission.Generation) && _ownerCurrent();
 private Submission CaptureSubmission(WeekZeroShortSummaryRequest request = null)
 {
  return new Submission { Generation=SaveRuntimeGuard.CaptureGeneration(), Revision=Interlocked.Read(ref _revision),
   SystemPrompt=request == null ? null : _systemPrompt(request.EventKind,request.KingdomId,request.Title),
   UserPrompt=request == null ? null : _userPrompt(request.Summary) };
 }
 internal void ResetTransientRuntime()
 {
  var retired = new List<Action>();
  lock (_weekZeroShortSummaryQueueLock)
  {
   Interlocked.Increment(ref _revision);
   _generation=SaveRuntimeGuard.CaptureGeneration();
   _weekZeroShortSummaryGenerationInFlight.Clear(); _weekZeroShortSummaryGenerationAttempted.Clear();
   _weekZeroShortSummaryPendingQueue.Clear(); _submissions.Clear();
   _weekZeroShortSummaryQueueProcessing=false; _weekZeroShortSummaryLastRequestUtcTicks=0L;
   while (_weekZeroShortSummaryMainThreadActions.TryDequeue(out var action)) retired.Add(action);
  }
  // Retired callbacks only resolve their completion tickets to false; guards precede every record read.
  foreach (var action in retired) action?.Invoke();
 }
 internal void RemovePendingOpeningRequests(Func<string,bool> isOpening)
 {
  lock (_weekZeroShortSummaryQueueLock)
  {
   foreach(var request in _weekZeroShortSummaryPendingQueue.Where(r=>isOpening(r?.EventId))) _submissions.Remove(request);
   _weekZeroShortSummaryPendingQueue.RemoveAll(r=>isOpening(r?.EventId));
   _weekZeroShortSummaryGenerationAttempted.RemoveWhere(id=>isOpening(id));
  }
 }
internal static string ComputeWeekZeroShortSummarySourceHash(string sourceText)
	{
		string text = (sourceText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			byte[] array = SHA1.Create().ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder(12);
			for (int i = 0; i < 6 && i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return text.Length.ToString();
		}
	}

internal static string BuildWeekZeroPromptText(string sourceHash, bool llmGenerated)
	{
		return "[BOOTSTRAP][WEEK0_SHORT=" + (llmGenerated ? "LLM" : "FALLBACK") + "][SOURCE=" + ((sourceHash ?? "").Trim()) + "]";
	}

internal static bool HasWeekZeroLlmShortSummary(EventRecordEntry entry, string sourceHash)
	{
		if (entry == null)
		{
			return false;
		}
		string text = (entry.PromptText ?? "").Trim();
		string text2 = (sourceHash ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf("[BOOTSTRAP][WEEK0_SHORT=LLM][SOURCE=" + text2 + "]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string NormalizeWeekZeroShortSummaryResponse(string rawResponse, string fallbackSource)
	{
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return BuildFallbackWeeklyReportShortSummary(fallbackSource);
		}
		Match match = Regex.Match(text, "\\[SHORT\\]\\s*(?<short>[\\s\\S]+)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			text = (match.Groups["short"]?.Value ?? "").Trim();
		}
		if (text.StartsWith("[") && text.IndexOf(']') > 0)
		{
			int num = text.IndexOf(']');
			if (num >= 0 && num + 1 < text.Length)
			{
				text = text.Substring(num + 1).Trim();
			}
		}
		text = text.Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		text = NeutralizeWeeklyReportScenarioName(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return BuildFallbackWeeklyReportShortSummary(fallbackSource);
		}
		if (text.Length > 140)
		{
			text = BuildFallbackWeeklyReportShortSummary(text);
		}
		if (text.Length < 20)
		{
			string fallbackWeeklyReportShortSummary = BuildFallbackWeeklyReportShortSummary(fallbackSource);
			if (!string.IsNullOrWhiteSpace(fallbackWeeklyReportShortSummary))
			{
				return fallbackWeeklyReportShortSummary;
			}
		}
		return text.Trim();
	}

internal static int GetWeekZeroShortSummaryQueuePriority(WeekZeroShortSummaryRequest request, Dictionary<string, int> kingdomOrder)
	{
		if (request == null)
		{
			return 5000;
		}
		if (string.Equals((request.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			string text = (request.KingdomId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && kingdomOrder != null && kingdomOrder.TryGetValue(text, out var value))
			{
				return (value == 0) ? 0 : (value + 1);
			}
			return 1000;
		}
		if (string.Equals((request.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}
		return 3000;
	}

internal void SortWeekZeroShortSummaryPendingQueue()
	{
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(_weekZeroShortSummaryPendingQueue.Where((WeekZeroShortSummaryRequest x) => x != null && string.Equals((x.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeekZeroShortSummaryRequest x) => x.KingdomId));
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < kingdomIdsByPlayerProximity.Count; i++)
		{
			string text = (kingdomIdsByPlayerProximity[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = i;
			}
		}
		_weekZeroShortSummaryPendingQueue.Sort(delegate(WeekZeroShortSummaryRequest a, WeekZeroShortSummaryRequest b)
		{
			int weekZeroShortSummaryQueuePriority = GetWeekZeroShortSummaryQueuePriority(a, dictionary);
			int weekZeroShortSummaryQueuePriority2 = GetWeekZeroShortSummaryQueuePriority(b, dictionary);
			int num = weekZeroShortSummaryQueuePriority.CompareTo(weekZeroShortSummaryQueuePriority2);
			if (num != 0)
			{
				return num;
			}
			return string.Compare((a?.Title ?? "").Trim(), (b?.Title ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
		});
	}

internal void EnsureWeekZeroShortSummaryQueueWorker()
	{
		bool flag = false;
		lock (_weekZeroShortSummaryQueueLock)
		{
			if (!_weekZeroShortSummaryQueueProcessing && _weekZeroShortSummaryPendingQueue.Count > 0)
			{
				_weekZeroShortSummaryQueueProcessing = true;
				flag = true;
			}
		}
		if (flag)
		{
			_ = ProcessWeekZeroShortSummaryQueueAsync(CaptureSubmission());
		}
	}

internal Task WaitForWeekZeroShortSummaryRequestSlotAsync() => WaitForWeekZeroShortSummaryRequestSlotAsync(CaptureSubmission());

private async Task WaitForWeekZeroShortSummaryRequestSlotAsync(Submission stamp)
	{
		int weeklyReportRequestIntervalMs = _interval();
		if (weeklyReportRequestIntervalMs <= 0)
		{
			lock (_weekZeroShortSummaryQueueLock)
			{
                if (!IsCurrent(stamp)) return;
				_weekZeroShortSummaryLastRequestUtcTicks = DateTime.UtcNow.Ticks;
			}
			return;
		}
		while (true)
		{
			int num = 0;
			lock (_weekZeroShortSummaryQueueLock)
			{
                if (!IsCurrent(stamp)) return;
				long ticks = DateTime.UtcNow.Ticks;
				long num2 = TimeSpan.FromMilliseconds(weeklyReportRequestIntervalMs).Ticks;
				long num3 = _weekZeroShortSummaryLastRequestUtcTicks + num2;
				if (_weekZeroShortSummaryLastRequestUtcTicks <= 0 || ticks >= num3)
				{
					_weekZeroShortSummaryLastRequestUtcTicks = ticks;
					return;
				}
				num = Math.Max(40, (int)Math.Ceiling(new TimeSpan(num3 - ticks).TotalMilliseconds));
			}
			await Task.Delay(num);
		}
	}

internal async Task<bool> GenerateWeekZeroShortSummaryWithRetriesAsync(WeekZeroShortSummaryRequest request)
	{
		if (request == null || string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.Summary))
		{
			return false;
		}
		Submission stamp;
        lock (_weekZeroShortSummaryQueueLock)
        {
            if (!_submissions.TryGetValue(request, out stamp)) stamp = CaptureSubmission(request);
        }
        if (!IsCurrent(stamp)) return false;
        List<string> failureDetails = new List<string>();
		for (int i = 1; i <= 3; i++)
		{
			await WaitForWeekZeroShortSummaryRequestSlotAsync(stamp);
            if (!IsCurrent(stamp)) return false;
			ApiCallResult apiCallResult = await _call(stamp.SystemPrompt, stamp.UserPrompt);
			if (!IsCurrent(stamp)) return false;
            if (apiCallResult.Success)
			{
				string text = NormalizeWeekZeroShortSummaryResponse(apiCallResult.Content, request.Summary);
				if (string.IsNullOrWhiteSpace(text))
				{
					Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报解析为空 " + request.EventId + "，尝试 " + i + "/3");
					failureDetails.Add("第 " + i + " 次尝试：\n" + LlmRetryPrompt.BuildFailureDetail("第0周短周报模型回复解析为空。", apiCallResult.Content, apiCallResult.ResponseBody));
				}
				else
				{
					return await ApplyWeekZeroShortSummaryOnMainThreadAsync(request.EventId, request.SourceHash, text, stamp).ConfigureAwait(false);
				}
			}
			else
			{
				Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报生成失败 " + request.EventId + "，尝试 " + i + "/3 -> " + (apiCallResult.ErrorMessage ?? "未知错误"));
				failureDetails.Add("第 " + i + " 次尝试：\n" + (apiCallResult.ErrorMessage ?? LlmRetryPrompt.BuildFailureDetail("未知错误", apiCallResult.Content, apiCallResult.ResponseBody)));
			}
			if (i < 3)
			{
				int num = Math.Max(1200, _interval());
				if (apiCallResult != null && apiCallResult.IsRateLimit)
				{
					num = Math.Max(num, _interval());
				}
				if (apiCallResult?.RetryAfterSeconds != null)
				{
					num = Math.Max(num, apiCallResult.RetryAfterSeconds.Value * 1000);
				}
				await Task.Delay(num);
			}
		}
		if (!IsCurrent(stamp)) return false;
        LlmRetryPrompt.ShowFailurePopup("第0周短周报生成失败", failureDetails.Count > 0 ? string.Join("\n\n", failureDetails) : LlmRetryPrompt.BuildFailureDetail("三次尝试均失败。", ""));
		return false;
	}

internal Task<bool> ApplyWeekZeroShortSummaryOnMainThreadAsync(string eventId,string sourceHash,string shortSummary) => ApplyWeekZeroShortSummaryOnMainThreadAsync(eventId,sourceHash,shortSummary,CaptureSubmission());

private Task<bool> ApplyWeekZeroShortSummaryOnMainThreadAsync(string eventId, string sourceHash, string shortSummary, Submission stamp)
	{
		TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		string text = (eventId ?? "").Trim();
		string text2 = (sourceHash ?? "").Trim();
		string text3 = (shortSummary ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3))
		{
			taskCompletionSource.TrySetResult(false);
			return taskCompletionSource.Task;
		}
		lock (_weekZeroShortSummaryQueueLock)
        {
        if (!IsCurrent(stamp)) { taskCompletionSource.TrySetResult(false); return taskCompletionSource.Task; }
        _weekZeroShortSummaryMainThreadActions.Enqueue(delegate
		{
			try
			{
				if (!IsCurrent(stamp)) { taskCompletionSource.TrySetResult(false); return; }
                EventRecordEntry eventRecordEntry = _eventRecordEntries?.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
				if (eventRecordEntry == null || !string.Equals(ComputeWeekZeroShortSummarySourceHash(eventRecordEntry.Summary ?? ""), text2, StringComparison.OrdinalIgnoreCase))
				{
					taskCompletionSource.TrySetResult(false);
					return;
				}
				eventRecordEntry.ShortSummary = text3;
				eventRecordEntry.PromptText = BuildWeekZeroPromptText(text2, llmGenerated: true);
				// The generated short text replaces the list/detail preview while the timeline may already be open.
				_notify();
				taskCompletionSource.TrySetResult(true);
			}
			catch (Exception ex)
			{
				Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] main-thread result apply failed: " + ex.Message);
				taskCompletionSource.TrySetResult(false);
			}
		});
        }
		FreezeWatchdog.Mark("WeeklyPrompt.Week0Short.apply_queued", "event=" + text + " pending=" + _weekZeroShortSummaryMainThreadActions.Count, immediate: true);
		return taskCompletionSource.Task;
	}

internal void ProcessWeekZeroShortSummaryMainThreadActions()
	{
		int num = 0;
		while (num < 2 && _weekZeroShortSummaryMainThreadActions.TryDequeue(out var action))
		{
			num++;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] main-thread action failed: " + ex.Message);
			}
		}
		if (num > 0)
		{
			FreezeWatchdog.Mark("WeeklyPrompt.Week0Short.apply_done", "processed=" + num + " pending=" + _weekZeroShortSummaryMainThreadActions.Count, immediate: true);
		}
	}

internal Task ProcessWeekZeroShortSummaryQueueAsync() => ProcessWeekZeroShortSummaryQueueAsync(CaptureSubmission());

private async Task ProcessWeekZeroShortSummaryQueueAsync(Submission worker)
	{
		try
		{
			while (IsCurrent(worker))
			{
				WeekZeroShortSummaryRequest weekZeroShortSummaryRequest = null;
				lock (_weekZeroShortSummaryQueueLock)
				{
					if (IsCurrent(worker) && _weekZeroShortSummaryPendingQueue.Count > 0)
					{
						weekZeroShortSummaryRequest = _weekZeroShortSummaryPendingQueue[0];
						_weekZeroShortSummaryPendingQueue.RemoveAt(0);
					}
				}
				if (weekZeroShortSummaryRequest == null)
				{
					break;
				}
				try
				{
					await GenerateWeekZeroShortSummaryWithRetriesAsync(weekZeroShortSummaryRequest);
				}
				catch (Exception ex)
				{
					Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] " + ex.Message);
				}
				finally
				{
					lock (_weekZeroShortSummaryQueueLock)
					{
						if (IsCurrent(worker)) { _weekZeroShortSummaryGenerationInFlight.Remove((weekZeroShortSummaryRequest.GenerationKey ?? "").Trim()); _submissions.Remove(weekZeroShortSummaryRequest); }
					}
				}
			}
		}
		finally
		{
			lock (_weekZeroShortSummaryQueueLock)
			{
				if (IsCurrent(worker)) _weekZeroShortSummaryQueueProcessing = false;
				if (IsCurrent(worker) && _weekZeroShortSummaryPendingQueue.Count > 0)
				{
					_weekZeroShortSummaryQueueProcessing = true;
					_ = ProcessWeekZeroShortSummaryQueueAsync(worker);
				}
			}
		}
	}

internal void TryQueueWeekZeroShortSummaryGeneration(EventRecordEntry entry, string sourceHash)
	{
		if (entry == null)
		{
			return;
		}
		string text = (sourceHash ?? "").Trim();
		string text2 = (entry.EventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || HasWeekZeroLlmShortSummary(entry, text))
		{
			return;
		}
		if (GetCurrentGameDayIndexSafe() < 7 || _bulletinEnabled())
		{
			return;
		}
		if (_generation != SaveRuntimeGuard.CaptureGeneration()) ResetTransientRuntime();
        string text3 = text2 + "|" + text;
		lock (_weekZeroShortSummaryQueueLock)
		{
			if (_weekZeroShortSummaryGenerationInFlight.Contains(text3) || _weekZeroShortSummaryGenerationAttempted.Contains(text3))
			{
				return;
			}
			_weekZeroShortSummaryGenerationInFlight.Add(text3);
			_weekZeroShortSummaryGenerationAttempted.Add(text3);
			var request = new WeekZeroShortSummaryRequest
			{
				EventId = text2,
				EventKind = (entry.EventKind ?? "").Trim(),
				KingdomId = (entry.ScopeKingdomId ?? "").Trim(),
				Title = (entry.Title ?? "").Trim(),
				Summary = (entry.Summary ?? "").Trim(),
				SourceHash = text,
				GenerationKey = text3
			};
            try { _submissions[request] = CaptureSubmission(request); }
            catch (Exception ex)
            {
                _weekZeroShortSummaryGenerationInFlight.Remove(text3);
                Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] " + ex.Message);
                return;
            }
            _weekZeroShortSummaryPendingQueue.Add(request);
			SortWeekZeroShortSummaryPendingQueue();
		}
		EnsureWeekZeroShortSummaryQueueWorker();
	}
internal void EnsureWeekZeroOpeningSummaryEvents(bool sanitizeAfter = true)
	{
		FreezeWatchdog.Mark("WeeklyPrompt.EnsureWeek0.start", "entries=" + (_eventRecordEntries?.Count ?? 0) + " thread=" + Thread.CurrentThread.ManagedThreadId);
		UpsertWeekZeroOpeningSummaryEvent("world", "", "第0天世界开局概要", _records.WorldOpening, "世界开局概要", "world_opening_summary", sanitizeAfter: false);
		FreezeWatchdog.Mark("WeeklyPrompt.EnsureWeek0.world_done", "entries=" + (_eventRecordEntries?.Count ?? 0));
		int kingdomIndex = 0;
		foreach (Kingdom devEditableKingdom in EventEditorProjection.GetDevEditableKingdoms())
		{
			kingdomIndex++;
			string diagnosticKingdomId = devEditableKingdom?.StringId ?? "";
			FreezeWatchdog.Mark("WeeklyPrompt.EnsureWeek0.kingdom_start", "index=" + kingdomIndex + " kingdom=" + diagnosticKingdomId);
			string kingdomOpeningSummary = WeeklyEventDataImportOwner.GetKingdomOpeningSummary(devEditableKingdom.StringId, _records.KingdomOpenings);
			if (!string.IsNullOrWhiteSpace(kingdomOpeningSummary))
			{
				string text = devEditableKingdom.Name?.ToString() ?? (devEditableKingdom.StringId ?? "王国");
				UpsertWeekZeroOpeningSummaryEvent("kingdom", devEditableKingdom.StringId ?? "", text + "第0天开局概要", kingdomOpeningSummary, text + " 开局概要", "kingdom_opening_summary", sanitizeAfter: false);
			}
			FreezeWatchdog.Mark("WeeklyPrompt.EnsureWeek0.kingdom_done", "index=" + kingdomIndex + " kingdom=" + diagnosticKingdomId + " entries=" + (_eventRecordEntries?.Count ?? 0));
		}
		FreezeWatchdog.Mark("WeeklyPrompt.EnsureWeek0.done", "kingdoms=" + kingdomIndex + " entries=" + (_eventRecordEntries?.Count ?? 0) + " thread=" + Thread.CurrentThread.ManagedThreadId);
	}

internal bool UpsertWeekZeroOpeningSummaryEvent(string eventKind, string kingdomId, string title, string summary, string materialLabel, string materialType, bool sanitizeAfter = true)
	{
		FreezeWatchdog.Mark("WeeklyPrompt.UpsertWeek0.start", "kind=" + (eventKind ?? "") + " kingdom=" + (kingdomId ?? "") + " entries=" + (_eventRecordEntries?.Count ?? 0));
		// Compare, store and publish the same representation as archive normalization.
		// Otherwise daily maintenance restores the raw scenario name on every pass.
		string rawSummary = (summary ?? "").Trim();
		string text = NeutralizeWeeklyReportScenarioName(rawSummary);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		string text2 = (eventKind ?? "").Trim();
		string text3 = (kingdomId ?? "").Trim();
		string text4 = NeutralizeWeeklyReportScenarioName(title);
		materialLabel = NeutralizeWeeklyReportScenarioName(materialLabel);
		materialType = (materialType ?? "").Trim();
		string text5 = "weekly_report:" + text2.ToLowerInvariant() + ":0:" + text3;
		EventRecordEntry eventRecordEntry = _eventRecordEntries.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text5, StringComparison.OrdinalIgnoreCase));
		bool flag = eventRecordEntry == null;
		if (eventRecordEntry == null)
		{
			eventRecordEntry = new EventRecordEntry
			{
				EventId = text5
			};
			_eventRecordEntries.Add(eventRecordEntry);
		}
		string text6 = ComputeWeekZeroShortSummarySourceHash(text);
		// Old saves can have a normalized body but an LLM marker hashed from the raw input.
		// Retain that successful short summary and upgrade its marker without another request.
		bool sameSummary = string.Equals(NeutralizeWeeklyReportScenarioName(eventRecordEntry.Summary), text, StringComparison.Ordinal);
		bool flag2 = sameSummary && (HasWeekZeroLlmShortSummary(eventRecordEntry, text6)
			|| HasWeekZeroLlmShortSummary(eventRecordEntry, ComputeWeekZeroShortSummarySourceHash(rawSummary))
			|| HasWeekZeroLlmShortSummary(eventRecordEntry, ComputeWeekZeroShortSummarySourceHash(eventRecordEntry.Summary)));
		string text7 = flag2 ? BuildFallbackWeeklyReportShortSummary(eventRecordEntry.ShortSummary) : BuildFallbackWeeklyReportShortSummary(text);
		if (string.IsNullOrWhiteSpace(text7)) text7 = BuildFallbackWeeklyReportShortSummary(text);
		string text8 = BuildWeekZeroPromptText(text6, llmGenerated: flag2);
		EventMaterialReference eventMaterialReference = ((eventRecordEntry.Materials?.Count == 1) ? eventRecordEntry.Materials[0] : null);
		bool flag3 = eventMaterialReference != null
			&& string.Equals((eventMaterialReference.MaterialType ?? "").Trim(), (materialType ?? "").Trim(), StringComparison.Ordinal)
			&& string.Equals((eventMaterialReference.Label ?? "").Trim(), (materialLabel ?? "").Trim(), StringComparison.Ordinal)
			&& string.Equals((eventMaterialReference.SnapshotText ?? "").Trim(), text, StringComparison.Ordinal)
			&& string.Equals((eventMaterialReference.KingdomId ?? "").Trim(), text3, StringComparison.Ordinal);
		bool flag4 = flag
			|| !string.Equals((eventRecordEntry.EventId ?? "").Trim(), text5, StringComparison.Ordinal)
			|| !string.Equals((eventRecordEntry.EventKind ?? "").Trim(), text2, StringComparison.Ordinal)
			|| !string.Equals((eventRecordEntry.ScopeKingdomId ?? "").Trim(), text3, StringComparison.Ordinal)
			|| eventRecordEntry.WeekIndex != 0
			|| !string.Equals((eventRecordEntry.Title ?? "").Trim(), text4, StringComparison.Ordinal)
			|| !string.Equals(eventRecordEntry.ShortSummary ?? "", text7, StringComparison.Ordinal)
			|| !string.Equals((eventRecordEntry.Summary ?? "").Trim(), text, StringComparison.Ordinal)
			|| !string.Equals(eventRecordEntry.PromptText ?? "", text8, StringComparison.Ordinal)
			|| !string.Equals(eventRecordEntry.TagText ?? "", "", StringComparison.Ordinal)
			|| eventRecordEntry.CreatedDay != 0
			|| !string.Equals((eventRecordEntry.CreatedDate ?? "").Trim(), "第 0 日", StringComparison.Ordinal)
			|| !flag3;
		if (!flag4)
		{
			TryQueueWeekZeroShortSummaryGeneration(eventRecordEntry, text6);
			FreezeWatchdog.Mark("WeeklyPrompt.UpsertWeek0.queue_done", "kind=" + (eventKind ?? "") + " kingdom=" + (kingdomId ?? "") + " entries=" + (_eventRecordEntries?.Count ?? 0) + " changed=false");
			return false;
		}
		string previousPublishedProductState = WeeklyEventRecordStateOwner.BuildPublishedWorldWeeklyProductState(eventRecordEntry);
		eventRecordEntry.EventId = text5;
		eventRecordEntry.EventKind = text2;
		eventRecordEntry.ScopeKingdomId = text3;
		eventRecordEntry.WeekIndex = 0;
		eventRecordEntry.Title = text4;
		eventRecordEntry.ShortSummary = text7;
		eventRecordEntry.PromptText = text8;
		eventRecordEntry.Summary = text;
		eventRecordEntry.TagText = "";
		eventRecordEntry.CreatedDay = 0;
		eventRecordEntry.CreatedDate = "第 0 日";
		eventRecordEntry.Materials = new List<EventMaterialReference>
		{
			new EventMaterialReference
			{
				MaterialType = materialType,
				Label = materialLabel,
				SnapshotText = text,
				KingdomId = (kingdomId ?? "").Trim()
			}
		};
		// Every field above is already normalized. Never sanitize unrelated report history here.
		_records.NotifyPublishedWorldWeeklyProductChanged(previousPublishedProductState, eventRecordEntry);
		// Week-zero entries are visible in the same timeline as normal weekly reports.
		_notify();
		TryQueueWeekZeroShortSummaryGeneration(eventRecordEntry, text6);
		FreezeWatchdog.Mark("WeeklyPrompt.UpsertWeek0.queue_done", "kind=" + (eventKind ?? "") + " kingdom=" + (kingdomId ?? "") + " entries=" + (_eventRecordEntries?.Count ?? 0));
		return true;
	}
 private static List<EventRecordEntry> SanitizeEventRecordEntries(List<EventRecordEntry> source)
  => WeeklyEventDataImportOwner.SanitizeEventRecordEntries(source,NeutralizeWeeklyReportScenarioName,BuildFallbackWeeklyReportShortSummary,NormalizeWeeklyReportTagText);
internal EventRecordEntry FindLatestWeeklyReportRecord(string eventKind, string kingdomId = null)
	{
		string text = (eventKind ?? "").Trim();
		string text2 = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || !TWParallel.IsMainThread())
		{
			return null;
		}
		WeeklyPromptReportSnapshot weeklyPromptReportSnapshot = null;
		if (string.Equals(text, "world", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(text2))
		{
			AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter.CaptureLatestWeeklyPromptReportSnapshots(_records.Records, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out weeklyPromptReportSnapshot);
		}
		else if (string.Equals(text, "kingdom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(text2))
		{
			Dictionary<string, WeeklyPromptReportSnapshot> dictionary = AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter.CaptureLatestWeeklyPromptReportSnapshots(_records.Records, new HashSet<string>(new[] { text2 }, StringComparer.OrdinalIgnoreCase), out var _);
			dictionary.TryGetValue(text2, out weeklyPromptReportSnapshot);
		}
		if (weeklyPromptReportSnapshot == null)
		{
			return null;
		}
		return new EventRecordEntry
		{
			EventKind = text,
			ScopeKingdomId = text2,
			WeekIndex = weeklyPromptReportSnapshot.WeekIndex,
			CreatedDay = weeklyPromptReportSnapshot.CreatedDay,
			Title = weeklyPromptReportSnapshot.Title,
			ShortSummary = weeklyPromptReportSnapshot.ShortSummary,
			Summary = weeklyPromptReportSnapshot.Summary
		};
	}

internal string GetLatestKingdomWeeklyShortSummaryInternal(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (!TWParallel.IsMainThread())
		{
			return "";
		}
		EnsureWeekZeroOpeningSummaryEvents();
		EventRecordEntry latestWeeklyReportRecord = FindLatestWeeklyReportRecord("kingdom", text);
		return BuildFallbackWeeklyReportShortSummary(latestWeeklyReportRecord?.ShortSummary ?? latestWeeklyReportRecord?.Summary);
	}
internal static bool ShouldExcludeNpcShortReportFromWeeklyShortLayer(string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
	{
		if (PromptRuleBlockText.Has(triggeredRuleInstructions, "npc_major_actions"))
		{
			return true;
		}
		if (PromptRuleBlockText.Has(triggeredRuleInstructions, "surroundings"))
		{
			if (weeklyPromptSnapshot != null)
			{
				return !string.IsNullOrWhiteSpace(weeklyPromptSnapshot.NpcKingdomId) && string.Equals(weeklyPromptSnapshot.NpcKingdomId, weeklyPromptSnapshot.SurroundingsKingdomId, StringComparison.OrdinalIgnoreCase);
			}
			if (!TWParallel.IsMainThread())
			{
				return false;
			}
			string weeklyReportNpcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			string weeklyReportSurroundingsKingdomId = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			if (!string.IsNullOrWhiteSpace(weeklyReportNpcKingdomId) && string.Equals(weeklyReportNpcKingdomId, weeklyReportSurroundingsKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

internal bool _weekZeroOpeningSummaryMaintenanceWorldProcessed;
internal List<Kingdom> _weekZeroOpeningSummaryMaintenanceKingdoms;
internal int _weekZeroOpeningSummaryMaintenanceCursor;
internal bool _weekZeroOpeningSummaryMaintenanceChanged;
internal bool ProcessWeekZeroOpeningSummaryEventsSlice()
	{
		try
		{
			if (!_weekZeroOpeningSummaryMaintenanceWorldProcessed)
			{
				UpsertWeekZeroOpeningSummaryEvent("world", "", "第0天世界开局概要", _records.WorldOpening, "世界开局概要", "world_opening_summary", sanitizeAfter: false);
				_weekZeroOpeningSummaryMaintenanceWorldProcessed = true;
				return false;
			}
			if (_weekZeroOpeningSummaryMaintenanceKingdoms == null)
			{
				_weekZeroOpeningSummaryMaintenanceKingdoms = EventEditorProjection.GetDevEditableKingdoms();
				_weekZeroOpeningSummaryMaintenanceCursor = 0;
			}
			if (_weekZeroOpeningSummaryMaintenanceCursor < _weekZeroOpeningSummaryMaintenanceKingdoms.Count)
			{
				Kingdom kingdom = _weekZeroOpeningSummaryMaintenanceKingdoms[_weekZeroOpeningSummaryMaintenanceCursor++];
				string summary = WeeklyEventDataImportOwner.GetKingdomOpeningSummary(kingdom?.StringId, _records.KingdomOpenings);
				if (!string.IsNullOrWhiteSpace(summary))
				{
					string kingdomName = kingdom?.Name?.ToString() ?? (kingdom?.StringId ?? "王国");
					UpsertWeekZeroOpeningSummaryEvent("kingdom", kingdom?.StringId ?? "", kingdomName + "第0天开局概要", summary, kingdomName + " 开局概要", "kingdom_opening_summary", sanitizeAfter: false);
				}
				return false;
			}
			FinalizeWeekZeroOpeningSummaryMaintenance();
			return true;
		}
		catch (Exception ex)
		{
			FinalizeWeekZeroOpeningSummaryMaintenance();
			Logger.Log("EventWeeklyReport", "[ERROR] deferred week-zero opening summary maintenance failed: " + ex.Message);
			return true;
		}
	}

internal void FinalizeWeekZeroOpeningSummaryMaintenance()
	{
		_weekZeroOpeningSummaryMaintenanceWorldProcessed = false;
		_weekZeroOpeningSummaryMaintenanceKingdoms = null;
		_weekZeroOpeningSummaryMaintenanceCursor = 0;
	}
}
