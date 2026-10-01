using System;using System.Collections.Generic;using System.Linq;using System.Diagnostics;using System.Threading.Tasks;using Newtonsoft.Json;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class WeeklyReportRuntimePort
{
 internal Func<bool> IsMainThread, IsCurrentOwner;
 internal Func<long> CaptureGeneration; internal Func<long,bool> IsCurrentGeneration;
 internal Func<long,string,bool> IsStale; internal Func<string,bool> IsKingdomEligible;
 internal Func<int> BatchSize,RequestsPerMinute,CurrentDay; internal Func<string> CurrentDate;
 internal Action<string> DisplayMessage; internal Action OpenEditor,OpenViewer,NotifyTimeline;
 internal Func<List<EventRecordEntry>> GetRecords; internal Action<List<EventRecordEntry>> SetRecords;
 internal Func<List<DevWeeklyReportBatchPreviewEntry>> GetPreviews; internal Action<List<DevWeeklyReportBatchPreviewEntry>> SetPreviews;
 internal Func<EventRecordEntry,string> ProductState;
 internal Action<WeeklyEventMaterialPreviewGroup,string,string> ApplyStability;
 internal Action<string,EventRecordEntry> NotifyProduct;
 internal Func<List<EventRecordEntry>,List<EventRecordEntry>> SanitizeRecords;
 internal Func<IEnumerable<string>,string> ResolveNearestKingdom; internal Action<string> QueueNotice;
 internal Action<WeeklyReportRetryContext,bool> QueueFailurePopup;
 internal Func<List<WeeklyReportBatchRequest>,int,int,int,int,int,int,string,long,WeeklyReportMaterialRevisionOwner.Snapshot,Task<List<Task<WeeklyReportBatchExecutionResult>>>> LaunchWave;
}
// Runs the existing detached generation and budgeted commit algorithms. Queue/revision authorities are injected unchanged.
internal sealed class WeeklyReportRuntimeOwner
{
 private readonly WeeklyReportRuntimePort _port;
 private readonly WeeklyReportMaterialRevisionOwner _weeklyReportMaterialRevisions;
 private readonly WeeklyReportCommitQueueOwner<PendingWeeklyReportCommitContext,WeeklyReportGenerationResult> _weeklyReportCommitQueue;
 private readonly WeeklyReportCommitQueueOwner<PendingWeeklyPromptPreparationContext,WeeklyPromptPreparationResult> _weeklyPromptPreparationQueue;
 private List<EventRecordEntry> _eventRecordEntries { get=>_port.GetRecords(); set=>_port.SetRecords(value); }
 private List<DevWeeklyReportBatchPreviewEntry> _latestWeeklyReportBatchDevPreviews { get=>_port.GetPreviews(); set=>_port.SetPreviews(value); }
 internal WeeklyReportRuntimeOwner(WeeklyReportRuntimePort port,WeeklyReportMaterialRevisionOwner revisions,
  WeeklyReportCommitQueueOwner<PendingWeeklyReportCommitContext,WeeklyReportGenerationResult> commits,
  WeeklyReportCommitQueueOwner<PendingWeeklyPromptPreparationContext,WeeklyPromptPreparationResult> prompts)
 { _port=port;_weeklyReportMaterialRevisions=revisions;_weeklyReportCommitQueue=commits;_weeklyPromptPreparationQueue=prompts; }
	internal static bool IsDailyMaintenanceBudgetExceeded(long startTimestamp, double budgetMs)
	{
		if (startTimestamp <= 0L || budgetMs <= 0.0 || double.IsInfinity(budgetMs) || double.IsNaN(budgetMs))
		{
			return false;
		}
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		return elapsedMs >= budgetMs;
	}

	internal static bool IsWeeklyReportGroupEligible(WeeklyEventMaterialPreviewGroup group, Func<string,bool> kingdomEligible)
	{
		if (group == null)
		{
			return false;
		}
		string text = (group.GroupKind ?? "").Trim();
		if (string.Equals(text, "world", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.Equals(text, "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return kingdomEligible(group.KingdomId);
	}

	internal static string BuildWeeklyReportGroupReportId(WeeklyEventMaterialPreviewGroup group) => WeeklyGenerationRules.BuildWeeklyReportGroupReportId(group);

	internal static Dictionary<string, WeeklyEventMaterialPreviewGroup> BuildWeeklyReportGroupMap(IEnumerable<WeeklyEventMaterialPreviewGroup> groups) => WeeklyGenerationRules.BuildWeeklyReportGroupMap(groups);

	internal static List<WeeklyReportBatchRequest> BuildWeeklyReportBatchRequests(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay, Func<string,bool> kingdomEligible, int batchSize)
	{
		List<WeeklyEventMaterialPreviewGroup> list2 = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x,kingdomEligible)).ToList();
		return WeeklyMaterialBatchPlanner.BuildBatches(list2, weekIndex, startDay, endDay, batchSize);
	}

	internal static string BuildWeeklyReportBatchDisplayLabel(WeeklyReportBatchRequest batch)
	{
		string preparedLabel = (batch?.DisplayLabel ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(preparedLabel))
		{
			return preparedLabel;
		}
		List<string> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupDisplayLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		string text = ((batch?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly) ? "短" : "全";
		return (list.Count == 0) ? ("未命名周报批次[" + text + "]") : ("批次[" + text + "]：" + string.Join(" | ", list));
	}

	internal void CaptureWeeklyReportBatchDevPreview(WeeklyReportBatchRequest batch, WeeklyReportBatchRequestResult result)
	{
		if (batch == null || result == null)
		{
			return;
		}
		if (_latestWeeklyReportBatchDevPreviews == null)
		{
			_latestWeeklyReportBatchDevPreviews = new List<DevWeeklyReportBatchPreviewEntry>();
		}
		string text = BuildWeeklyReportBatchPreviewKey(batch);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		DevWeeklyReportBatchPreviewEntry devWeeklyReportBatchPreviewEntry = _latestWeeklyReportBatchDevPreviews.FirstOrDefault((DevWeeklyReportBatchPreviewEntry x) => x != null && string.Equals(x.PreviewKey, text, StringComparison.OrdinalIgnoreCase));
		if (devWeeklyReportBatchPreviewEntry == null)
		{
			devWeeklyReportBatchPreviewEntry = new DevWeeklyReportBatchPreviewEntry();
			_latestWeeklyReportBatchDevPreviews.Add(devWeeklyReportBatchPreviewEntry);
		}
		devWeeklyReportBatchPreviewEntry.PreviewKey = text;
		devWeeklyReportBatchPreviewEntry.BatchLabel = BuildWeeklyReportBatchDisplayLabel(batch);
		devWeeklyReportBatchPreviewEntry.WeekIndex = batch.WeekIndex;
		devWeeklyReportBatchPreviewEntry.StartDay = batch.StartDay;
		devWeeklyReportBatchPreviewEntry.EndDay = batch.EndDay;
		devWeeklyReportBatchPreviewEntry.ReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
		devWeeklyReportBatchPreviewEntry.PromptPreview = result.PromptPreview ?? "";
		devWeeklyReportBatchPreviewEntry.ResponsePreview = result.RawResponse ?? "";
		devWeeklyReportBatchPreviewEntry.Success = result.Success;
		devWeeklyReportBatchPreviewEntry.FailureReason = result.FailureReason ?? "";
		devWeeklyReportBatchPreviewEntry.AttemptsUsed = result.AttemptsUsed;
		while (_latestWeeklyReportBatchDevPreviews.Count > 24)
		{
			_latestWeeklyReportBatchDevPreviews.RemoveAt(0);
		}
	}

	internal Dictionary<string, string> CaptureWeeklyReportCommitRecordStates(Dictionary<string, WeeklyEventMaterialPreviewGroup> groups, int weekIndex)
	{
		Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> reportIdsByEventId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, WeeklyEventMaterialPreviewGroup> pair in groups)
		{
			WeeklyEventMaterialPreviewGroup group = pair.Value;
			string eventId = BuildWeeklyReportEventId(group.GroupKind, weekIndex, group.KingdomId);
			reportIdsByEventId[eventId] = pair.Key;
			states[pair.Key] = null;
		}
		foreach (EventRecordEntry entry in _eventRecordEntries ?? new List<EventRecordEntry>())
		{
			if (entry != null && reportIdsByEventId.TryGetValue((entry.EventId ?? "").Trim(), out string reportId) && states[reportId] == null)
			{
				states[reportId] = BuildWeeklyReportCommitRecordState(entry);
			}
		}
		return states;
	}

	internal bool IsWeeklyReportCommitRecordUnchanged(PendingWeeklyReportCommitContext context, string reportId, WeeklyEventMaterialPreviewGroup group)
	{
		if (context.CapturedRecordStates == null || !context.CapturedRecordStates.TryGetValue(reportId, out string captured))
		{
			return false;
		}
		string eventId = BuildWeeklyReportEventId(group.GroupKind, context.WeekIndex, group.KingdomId);
		return string.Equals(captured, BuildWeeklyReportCommitRecordState(FindWeeklyReportRecordById(eventId)), StringComparison.Ordinal);
	}

	internal bool HasWeeklyReportCommitWinner(PendingWeeklyReportCommitContext context, WeeklyEventMaterialPreviewGroup group)
	{
		string eventId = BuildWeeklyReportEventId(group.GroupKind, context.WeekIndex, group.KingdomId);
		return IsWeeklyReportCommitWinner(FindWeeklyReportRecordById(eventId), group, context.WeekIndex);
	}

	internal static bool AreWeeklyReportCommitRecordStatesCurrent(Dictionary<string, WeeklyEventMaterialPreviewGroup> groups, Dictionary<string, string> captured, Dictionary<string, string> current)
	{
		return groups != null && captured != null && current != null && groups.Keys.All((string id) => captured.TryGetValue(id, out string original) && current.TryGetValue(id, out string now) && string.Equals(original, now, StringComparison.Ordinal));
	}

	internal static WeeklyReportRetryContext CreateWeeklyReportRetryContext(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool isAutoGeneration, WeeklyEventMaterialPreviewGroup failedGroup, WeeklyReportRequestResult requestResult, IEnumerable<string> popupCandidateKingdomIds = null, Dictionary<string, string> capturedRecordStates = null, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot = null)
	{
		WeeklyReportRetryContext weeklyReportRetryContext = new WeeklyReportRetryContext
		{
			WeekIndex = weekIndex,
			StartDay = startDay,
			EndDay = endDay,
			DisplayLabel = (displayLabel ?? "").Trim(),
			OpenViewerWhenDone = openViewerWhenDone,
			IsAutoGeneration = isAutoGeneration,
			CapturedRecordStates = capturedRecordStates,
			SourceSnapshot = sourceSnapshot,
			FailedGroupTitle = BuildWeeklyReportGroupDisplayLabel(failedGroup),
			FailedReason = (requestResult?.FailureReason ?? "").Trim(),
			AttemptsUsed = requestResult?.AttemptsUsed ?? 0,
			IsRateLimit = requestResult?.IsRateLimit ?? false,
			IsRequestsPerMinuteLimit = requestResult?.IsRequestsPerMinuteLimit ?? false,
			IsQuotaLimit = requestResult?.IsQuotaLimit ?? false,
			RetryAfterSeconds = requestResult?.RetryAfterSeconds
		};
		foreach (WeeklyEventMaterialPreviewGroup item in groups ?? new List<WeeklyEventMaterialPreviewGroup>())
		{
			if (item != null)
			{
				weeklyReportRetryContext.Groups.Add(item);
			}
		}
		foreach (string item2 in popupCandidateKingdomIds ?? Enumerable.Empty<string>())
		{
			string text = (item2 ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !weeklyReportRetryContext.PopupCandidateKingdomIds.Contains(text, StringComparer.OrdinalIgnoreCase))
			{
				weeklyReportRetryContext.PopupCandidateKingdomIds.Add(text);
			}
		}
		return weeklyReportRetryContext;
	}

	internal void UpsertWeeklyReportEventRecord(WeeklyEventMaterialPreviewGroup group, int weekIndex, string title, string shortSummary, string report, string tagText, string promptText, List<EventMaterialReference> materials, bool sanitizeAfter)
	{
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		string text = (group?.GroupKind ?? "").Trim().ToLowerInvariant();
		string text2 = (group?.KingdomId ?? "").Trim();
		string text3 = BuildWeeklyReportEventId(text, weekIndex, text2);
		EventRecordEntry eventRecordEntry = _eventRecordEntries.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
		if (eventRecordEntry == null)
		{
			eventRecordEntry = new EventRecordEntry
			{
				EventId = text3
			};
			_eventRecordEntries.Add(eventRecordEntry);
		}
		string previousPublishedProductState = _port.ProductState(eventRecordEntry);
		eventRecordEntry.EventKind = text;
		eventRecordEntry.ScopeKingdomId = text2;
		eventRecordEntry.WeekIndex = weekIndex;
		eventRecordEntry.Title = WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(title);
		eventRecordEntry.ShortSummary = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(shortSummary);
		eventRecordEntry.Summary = WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(report);
		if (string.IsNullOrWhiteSpace(eventRecordEntry.ShortSummary))
		{
			eventRecordEntry.ShortSummary = WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(eventRecordEntry.Summary);
		}
		eventRecordEntry.TagText = WeeklyGenerationRules.NormalizeWeeklyReportTagText(tagText);
		eventRecordEntry.PromptText = WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(promptText);
		eventRecordEntry.CreatedDay = _port.CurrentDay();
		eventRecordEntry.CreatedDate = _port.CurrentDate();
		eventRecordEntry.Materials = materials ?? new List<EventMaterialReference>();
		_port.ApplyStability(group, eventRecordEntry.EventId, eventRecordEntry.TagText);
		EventRecordEntry publishedProductEntry = eventRecordEntry;
		if (sanitizeAfter)
		{
			_eventRecordEntries = _port.SanitizeRecords(_eventRecordEntries);
			publishedProductEntry = FindWeeklyReportRecordById(text3) ?? eventRecordEntry;
		}
		_port.NotifyProduct(previousPublishedProductState, publishedProductEntry);
		// A completed weekly upsert can append a new short report, so wake only the active timeline projection.
		_port.NotifyTimeline();
	}

	internal async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsMinuteBurstAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration, IEnumerable<string> popupCandidateKingdomIdsOverride = null, List<WeeklyReportBatchRequest> preparedBatches = null, long runtimeGeneration = 0L, Dictionary<string, string> capturedRecordStatesOverride = null, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshotOverride = null)
	{
		WeeklyReportGenerationResult generationResult = new WeeklyReportGenerationResult();
		if (!_port.IsMainThread() || !_port.IsCurrentOwner())
		{
			Logger.Log("EventWeeklyReport", "[BATCH] rejected non-current main-thread owner before capture");
			return generationResult;
		}
		if (runtimeGeneration <= 0L)
		{
			runtimeGeneration = _port.CaptureGeneration();
		}
		if (_port.IsStale(runtimeGeneration, "weekly_report_before_prepare"))
		{
			return generationResult;
		}
		list = (list ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x,_port.IsKingdomEligible)).ToList();
		if (list.Count == 0)
		{
			_port.DisplayMessage("当前没有可生成周报的分组。");
			if (openViewerWhenDone)
			{
				_port.OpenEditor();
			}
			generationResult.Completed = true;
			return generationResult;
		}
		List<string> failureMessages = new List<string>();
		List<string> list2 = (popupCandidateKingdomIdsOverride ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list2.Count == 0)
		{
			list2 = list.Where((WeeklyEventMaterialPreviewGroup x) => x != null && string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase) && x.OutputMode != WeeklyReportOutputMode.TitleShortTagsOnly).Select((WeeklyEventMaterialPreviewGroup x) => (x.KingdomId ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap = BuildWeeklyReportGroupMap(list);
		WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot = sourceSnapshotOverride ?? _weeklyReportMaterialRevisions.Capture(startDay, endDay);
		Dictionary<string, string> currentRecordStates = CaptureWeeklyReportCommitRecordStates(groupMap, weekIndex);
		if (!_weeklyReportMaterialRevisions.IsCurrent(sourceSnapshot))
		{
			generationResult.FailureCount = groupMap.Count;
			generationResult.BlockedByFatalFailure = true;
			generationResult.BlockedByChangedRecord = true;
			generationResult.RetryContext = CreateWeeklyReportRetryContext(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, list[0], new WeeklyReportRequestResult { Success = false, FailureReason = "Weekly source materials changed before dispatch." }, list2, currentRecordStates, sourceSnapshot);
			generationResult.RetryContext.RequiresFreshMaterials = true;
			if (queueBlockingPopupOnFatalFailure)
			{
				_port.QueueFailurePopup(generationResult.RetryContext, true);
			}
			return generationResult;
		}
		if (capturedRecordStatesOverride != null && !AreWeeklyReportCommitRecordStatesCurrent(groupMap, capturedRecordStatesOverride, currentRecordStates))
		{
			generationResult.BlockedByFatalFailure = true;
			generationResult.BlockedByChangedRecord = true;
			generationResult.FailureCount = groupMap.Count;
			_port.DisplayMessage("周报目标在失败后已变更，旧素材重试已取消；请重新生成本周周报。");
			return generationResult;
		}
		Dictionary<string, string> capturedRecordStates = capturedRecordStatesOverride ?? currentRecordStates;
		List<WeeklyReportBatchRequest> batches = (preparedBatches ?? new List<WeeklyReportBatchRequest>()).Where((WeeklyReportBatchRequest x) => x != null && x.Groups != null && x.Groups.Count > 0).ToList();
		if (batches.Count == 0)
		{
			batches = BuildWeeklyReportBatchRequests(list, weekIndex, startDay, endDay,_port.IsKingdomEligible,_port.BatchSize());
		}
		WeeklyPromptPreparationResult promptPreparation = await EnqueueWeeklyPromptPreparationAsync(batches, runtimeGeneration);
		if (promptPreparation == WeeklyPromptPreparationResult.Canceled || _port.IsStale(runtimeGeneration, "weekly_report_after_prompt_prepare"))
		{
			return generationResult;
		}
		if (promptPreparation != WeeklyPromptPreparationResult.Prepared)
		{
			generationResult.FailureCount = list.Count;
			generationResult.BlockedByFatalFailure = true;
			generationResult.RetryContext = CreateWeeklyReportRetryContext(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, list[0], new WeeklyReportRequestResult { Success = false, FailureReason = "Weekly batch prompt preparation failed before dispatch." }, list2, capturedRecordStates, sourceSnapshot);
			if (queueBlockingPopupOnFatalFailure)
			{
				_port.QueueFailurePopup(generationResult.RetryContext, true);
			}
			return generationResult;
		}
		int burstSize = Math.Max(1, _port.RequestsPerMinute());
		WeeklyReportBatchExecutionResult[] completed = await CoordinateWeeklyReportWavesAsync(batches, burstSize, list.Count, displayLabel, runtimeGeneration, sourceSnapshot);
		if (completed == null)
		{
			return generationResult;
		}
		return await EnqueueWeeklyReportCommitAsync(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, queueBlockingPopupOnFatalFailure, isAutoGeneration, list2, groupMap, capturedRecordStates, sourceSnapshot, completed, runtimeGeneration);

	}

	internal Task<WeeklyReportBatchExecutionResult[]> CoordinateWeeklyReportWavesAsync(List<WeeklyReportBatchRequest> batches, int burstSize, int totalTargets, string displayLabel, long runtimeGeneration, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot, Func<int, Task> delay = null)
	{
		return WeeklyReportWaveCoordinator.RunAsync<WeeklyReportBatchRequest, WeeklyReportBatchExecutionResult>(
			batches, burstSize,
			batch => batch != null && batch.Groups != null && batch.Groups.Count > 0,
			phase => !_port.IsStale(runtimeGeneration, phase) && _port.IsCurrentOwner(),
			(wave, first, index, total) => _port.LaunchWave(wave, first, index, total, totalTargets, batches.Count, burstSize, displayLabel, runtimeGeneration, sourceSnapshot),
			(batch, index) => new WeeklyReportBatchExecutionResult
			{
				BatchIndex = index,
				Batch = batch,
				Result = new WeeklyReportBatchRequestResult
				{
					FailureReason = "Weekly batch wave was not launched.",
					MissingReportIds = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch)
				}
			}, delay);
	}

	internal Task<WeeklyPromptPreparationResult> EnqueueWeeklyPromptPreparationAsync(List<WeeklyReportBatchRequest> batches, long runtimeGeneration)
	{
		TaskCompletionSource<WeeklyPromptPreparationResult> completionSource = new TaskCompletionSource<WeeklyPromptPreparationResult>();
		_weeklyPromptPreparationQueue.EnqueueIfCurrent(new PendingWeeklyPromptPreparationContext
		{
			RuntimeGeneration = runtimeGeneration,
			Cursor = new WeeklyMaterialStageCursor<WeeklyReportBatchRequest>(batches),
			CompletionSource = completionSource
		}, () => _port.IsCurrentOwner() && _port.IsCurrentGeneration(runtimeGeneration));
		return completionSource.Task;
	}

	internal Task<WeeklyReportGenerationResult> EnqueueWeeklyReportCommitAsync(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration, List<string> popupCandidateKingdomIds, Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap, Dictionary<string, string> capturedRecordStates, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot, IEnumerable<WeeklyReportBatchExecutionResult> executions, long runtimeGeneration)
	{
		TaskCompletionSource<WeeklyReportGenerationResult> completionSource = new TaskCompletionSource<WeeklyReportGenerationResult>();
		PendingWeeklyReportCommitContext context = new PendingWeeklyReportCommitContext
		{
			RuntimeGeneration = runtimeGeneration,
			SourceSnapshot = sourceSnapshot,
			WeekIndex = weekIndex,
			StartDay = startDay,
			EndDay = endDay,
			DisplayLabel = (displayLabel ?? "").Trim(),
			OpenViewerWhenDone = openViewerWhenDone,
			QueueBlockingPopupOnFatalFailure = queueBlockingPopupOnFatalFailure,
			IsAutoGeneration = isAutoGeneration,
			PopupCandidateKingdomIds = (popupCandidateKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Groups = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList(),
			GroupMap = groupMap ?? BuildWeeklyReportGroupMap(groups),
			CapturedRecordStates = capturedRecordStates,
			Executions = (executions ?? Enumerable.Empty<WeeklyReportBatchExecutionResult>()).Where((WeeklyReportBatchExecutionResult x) => x != null).OrderBy((WeeklyReportBatchExecutionResult x) => x.BatchIndex).ToList(),
			CompletionSource = completionSource
		};
		_weeklyReportCommitQueue.EnqueueIfCurrent(context, () => _port.IsCurrentOwner() && _port.IsCurrentGeneration(runtimeGeneration));
		return completionSource.Task;
	}

	internal bool ProcessPendingWeeklyReportCommitContext(PendingWeeklyReportCommitContext context, long startTimestamp, double budgetMs)
	{
		if (context == null)
		{
			return true;
		}
		try
		{
			if (!_port.IsMainThread() || !_port.IsCurrentOwner() || (context.RuntimeGeneration > 0L && _port.IsStale(context.RuntimeGeneration, "weekly_report_commit")))
			{
				_weeklyReportCommitQueue.Complete(context, new WeeklyReportGenerationResult());
				return true;
			}
			if (!_weeklyReportMaterialRevisions.IsCurrent(context.SourceSnapshot))
			{
				context.RequiresFreshMaterials = true;
			}
			if (context.GroupMap == null)
			{
				using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.BuildGroupMap"))
				{
					context.GroupMap = BuildWeeklyReportGroupMap(context.Groups);
				}
			}
			List<WeeklyReportBatchExecutionResult> executions = context.Executions ?? new List<WeeklyReportBatchExecutionResult>();
			while (context.ExecutionIndex < executions.Count && !IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
			{
				WeeklyReportBatchExecutionResult execution = executions[context.ExecutionIndex];
				WeeklyReportBatchRequest batch = execution?.Batch;
				WeeklyReportBatchRequestResult batchResult = execution?.Result ?? new WeeklyReportBatchRequestResult
				{
					Success = false,
					FailureReason = "Batch execution returned no result.",
					Blocks = new List<WeeklyReportBatchBlockResult>(),
					MissingReportIds = new List<string>()
				};
				if (!context.CurrentPreviewCaptured)
				{
					using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.CapturePreview"))
					{
						CaptureWeeklyReportBatchDevPreview(batch, batchResult);
					}
					context.CurrentPreviewCaptured = true;
					if (IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
					{
						return false;
					}
				}
				if (context.CurrentParsedReportIds == null)
				{
					context.CurrentParsedReportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				}
				List<WeeklyReportBatchBlockResult> blocks = batchResult.Blocks ?? new List<WeeklyReportBatchBlockResult>();
				while (context.BlockIndex < blocks.Count && !IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
				{
					WeeklyReportBatchBlockResult block = blocks[context.BlockIndex];
					if (block != null && block.Parsed && !string.IsNullOrWhiteSpace(block.ReportId) && !context.CurrentParsedReportIds.Contains(block.ReportId) && !context.Targets.IsSettled(block.ReportId) && context.GroupMap.TryGetValue(block.ReportId, out var group) && group != null)
					{
						if (!IsWeeklyReportCommitRecordUnchanged(context, block.ReportId, group) || !_weeklyReportMaterialRevisions.IsCurrent(context.SourceSnapshot))
						{
							context.CurrentParsedReportIds.Add(block.ReportId);
							context.Targets.Settle(block.ReportId);
							if (HasWeeklyReportCommitWinner(context, group))
							{
								context.SuccessCount++;
							}
							else
							{
								context.RequiresFreshMaterials = true;
								context.FailureCount++;
								context.FailedGroups.Add(group);
								context.FailureMessages.Add("周报目标或同周源素材在请求期间已变更，旧回包未覆盖：" + block.ReportId);
							}
							context.BlockIndex++;
							if (IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
							{
								return false;
							}
							continue;
						}
						if (context.CurrentBlockCommit == null)
						{
							using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.PrepareRecordMaterials"))
							{
								context.CurrentBlockCommit = CreatePendingWeeklyReportBlockCommit(group, block, batchResult.PromptPreview);
							}
						}
						if (!ProcessPendingWeeklyReportBlockCommit(context, startTimestamp, budgetMs))
						{
							return false;
						}
						context.CurrentParsedReportIds.Add(block.ReportId);
						context.Targets.Settle(block.ReportId);
						if (context.CurrentBlockRejected)
						{
							context.CurrentBlockRejected = false;
							if (HasWeeklyReportCommitWinner(context, group))
							{
								context.SuccessCount++;
							}
							else
							{
								context.RequiresFreshMaterials = true;
								context.FailureCount++;
								context.FailedGroups.Add(group);
								context.FailureMessages.Add("周报目标或同周源素材在提交期间已变更，旧回包未覆盖：" + block.ReportId);
							}
						}
						else
						{
							TryQueueWeeklyReportMapNoticeForGeneratedReport(group, context.WeekIndex, ResolveWeeklyReportNoticeNearestKingdomId(context,_port.ResolveNearestKingdom), context.WeeklyReportNoticeEventIdsQueued);
							context.SuccessCount++;
						}
					}
					context.BlockIndex++;
					if (IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
					{
						return false;
					}
				}
				if (IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
				{
					return false;
				}
				using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.FinalizeBatch"))
				{
					FinalizePendingWeeklyReportCommitBatch(context, batch, batchResult);
				}
				context.ExecutionIndex++;
				context.BlockIndex = 0;
				context.CurrentPreviewCaptured = false;
				context.CurrentParsedReportIds = null;
				context.CurrentBlockCommit = null;
			}
			if (context.ExecutionIndex < executions.Count)
			{
				return false;
			}
			using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.FinalizeContext"))
			{
				FinalizePendingWeeklyReportCommitContext(context);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[ERROR] deferred weekly report commit failed: " + ex);
			WeeklyReportGenerationResult failure = new WeeklyReportGenerationResult
			{
				FailureCount = Math.Max(1, context.FailureCount),
				BlockedByFatalFailure = true
			};
			try
			{
				failure = BuildWeeklyReportCommitExceptionResult(context);
				if (context.QueueBlockingPopupOnFatalFailure && failure.RetryContext != null)
				{
					_port.QueueFailurePopup(failure.RetryContext, true);
				}
			}
			catch (Exception recoveryException)
			{
				Logger.Log("EventWeeklyReport", "[ERROR] weekly report commit recovery failed: " + recoveryException);
			}
			_weeklyReportCommitQueue.Complete(context, failure);
			return true;
		}
	}

	internal WeeklyReportGenerationResult BuildWeeklyReportCommitExceptionResult(PendingWeeklyReportCommitContext context)
	{
		List<WeeklyEventMaterialPreviewGroup> unfinished = new List<WeeklyEventMaterialPreviewGroup>();
		HashSet<string> seenReportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int completed = 0;
		foreach (WeeklyEventMaterialPreviewGroup group in context.Groups ?? new List<WeeklyEventMaterialPreviewGroup>())
		{
			string reportId = BuildWeeklyReportGroupReportId(group);
			if (string.IsNullOrWhiteSpace(reportId) || !seenReportIds.Add(reportId))
			{
				continue;
			}
			bool hasWinner = false;
			try
			{
				hasWinner = HasWeeklyReportCommitWinner(context, group);
			}
			catch (Exception ex)
			{
				Logger.Log("EventWeeklyReport", "[WARN] weekly report winner check failed during commit recovery: " + ex.Message);
			}
			if (hasWinner)
			{
				completed++;
				if (context.AttemptedWriteReportIds?.Contains(reportId) == true)
				{
					try
					{
						TryQueueWeeklyReportMapNoticeForGeneratedReport(group, context.WeekIndex, ResolveWeeklyReportNoticeNearestKingdomId(context,_port.ResolveNearestKingdom), context.WeeklyReportNoticeEventIdsQueued);
					}
					catch (Exception ex)
					{
						Logger.Log("EventWeeklyReport", "[WARN] weekly report notice recovery failed: " + ex.Message);
					}
				}
			}
			else
			{
				unfinished.Add(group);
			}
		}
		if (seenReportIds.Count == 0)
		{
			return new WeeklyReportGenerationResult { BlockedByFatalFailure = true, FailureCount = Math.Max(1, context.FailureCount) };
		}
		WeeklyReportGenerationResult result = new WeeklyReportGenerationResult
		{
			SuccessCount = completed,
			FailureCount = unfinished.Count,
			BlockedByFatalFailure = unfinished.Count > 0,
			Completed = unfinished.Count == 0
		};
		if (unfinished.Count > 0)
		{
			WeeklyReportRequestResult failure = new WeeklyReportRequestResult { Success = false, FailureReason = "Weekly commit failed; current materials must be collected again." };
			result.RetryContext = CreateWeeklyReportRetryContext(unfinished, context.WeekIndex, context.StartDay, context.EndDay, context.DisplayLabel, context.OpenViewerWhenDone, context.IsAutoGeneration, unfinished[0], failure, context.PopupCandidateKingdomIds, context.CapturedRecordStates, context.SourceSnapshot);
			result.RetryContext.RequiresFreshMaterials = true;
		}
		return result;
	}

	internal static PendingWeeklyReportBlockCommit CreatePendingWeeklyReportBlockCommit(WeeklyEventMaterialPreviewGroup group, WeeklyReportBatchBlockResult block, string promptText)
	{
		return new PendingWeeklyReportBlockCommit
		{
			Group = group,
			ReportId = (block?.ReportId ?? "").Trim(),
			Title = block?.Title ?? "",
			ShortSummary = block?.ShortSummary ?? "",
			Report = block?.Report ?? "",
			TagText = block?.TagText ?? "",
			PromptText = promptText ?? "",
			MaterialCursor = new WeeklyReportBlockMaterialCursor<EventMaterialReference>(
				WeeklyPromptMaterialOwner.OrderWeeklyPreviewMaterials(group?.Materials).Where((EventMaterialReference x) => x != null).ToList())
		};
	}

	internal bool ProcessPendingWeeklyReportBlockCommit(PendingWeeklyReportCommitContext context, long startTimestamp, double budgetMs)
	{
		PendingWeeklyReportBlockCommit pending = context?.CurrentBlockCommit;
		if (pending == null)
		{
			return true;
		}
		while (!pending.MaterialCursor.Complete && !IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.CloneMaterial"))
			{
				pending.MaterialCursor.Advance(WeeklyPromptMaterialOwner.CloneEventMaterialReference);
			}
		}
		if (!pending.MaterialCursor.Complete)
		{
			return false;
		}
		if (IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			return false;
		}
		if (!IsWeeklyReportCommitRecordUnchanged(context, pending.ReportId, pending.Group) || !_weeklyReportMaterialRevisions.IsCurrent(context.SourceSnapshot))
		{
			context.CurrentBlockRejected = true;
			context.CurrentBlockCommit = null;
			return true;
		}
		using (PerfProbe.Scope("MyBehavior.WeeklyReportCommit.WriteRecord"))
		{
			context.AttemptedWriteReportIds.Add(pending.ReportId);
			UpsertWeeklyReportEventRecord(pending.Group, context.WeekIndex, pending.Title, pending.ShortSummary, pending.Report, pending.TagText, pending.PromptText, pending.MaterialCursor.Cloned, sanitizeAfter: false);
		}
		context.CurrentBlockCommit = null;
		return true;
	}

	internal void FinalizePendingWeeklyReportCommitBatch(PendingWeeklyReportCommitContext context, WeeklyReportBatchRequest batch, WeeklyReportBatchRequestResult batchResult)
	{
		if (context == null)
		{
			return;
		}
		HashSet<string> parsedReportIds = context.CurrentParsedReportIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap = context.GroupMap ?? BuildWeeklyReportGroupMap(context.Groups);
		string reason = null;
		bool hasReportedMissing = false;
		foreach (string reportId in batchResult?.MissingReportIds ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(reportId) && !parsedReportIds.Contains(reportId) && !context.Targets.IsSettled(reportId) && groupMap.TryGetValue(reportId, out var missingGroup) && missingGroup != null)
			{
				hasReportedMissing = true;
				reason ??= BuildWeeklyReportBatchDisplayLabel(batch) + "：批量请求未恢复出可用周报区块 - " + (batchResult?.FailureReason ?? "未知错误");
				context.Targets.RecordMissing(reportId, missingGroup, reason);
			}
		}
		if (batchResult != null && !batchResult.Success && !hasReportedMissing)
		{
			foreach (WeeklyEventMaterialPreviewGroup group in batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>())
			{
				string reportId = BuildWeeklyReportGroupReportId(group);
				if (!string.IsNullOrWhiteSpace(reportId) && !parsedReportIds.Contains(reportId) && !context.Targets.IsSettled(reportId))
				{
					reason ??= BuildWeeklyReportBatchDisplayLabel(batch) + "：批量请求未恢复出可用周报区块 - " + (batchResult?.FailureReason ?? "未知错误");
					context.Targets.RecordMissing(reportId, group, reason);
				}
			}
		}
	}

	internal void FinalizePendingWeeklyReportCommitContext(PendingWeeklyReportCommitContext context)
	{
		if (context == null)
		{
			return;
		}
		foreach (WeeklyReportCommitTargetOwner<WeeklyEventMaterialPreviewGroup>.PendingMissing missing in context.Targets.PendingMissingTargets)
		{
			context.FailureCount++;
			context.FailedGroups.Add(missing.Group);
			context.FailureMessages.Add(missing.Reason + " [" + missing.ReportId + "]");
		}
		List<WeeklyEventMaterialPreviewGroup> failedGroups = (context.FailedGroups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).Distinct().ToList();
		if (context.FailureMessages != null && context.FailureMessages.Count > 0)
		{
			Logger.Log("EventWeeklyReport", string.Join("\n", context.FailureMessages));
		}
		WeeklyReportGenerationResult result = new WeeklyReportGenerationResult
		{
			SuccessCount = context.SuccessCount,
			FailureCount = context.FailureCount
		};
		if (failedGroups.Count > 0)
		{
			WeeklyEventMaterialPreviewGroup firstFailedGroup = failedGroups.FirstOrDefault();
			WeeklyReportRequestResult failedRequest = BuildWeeklyReportFailedRequest(context, firstFailedGroup);
			result.BlockedByFatalFailure = true;
			result.RetryContext = CreateWeeklyReportRetryContext(failedGroups, context.WeekIndex, context.StartDay, context.EndDay, context.DisplayLabel, context.OpenViewerWhenDone, context.IsAutoGeneration, firstFailedGroup, failedRequest, context.PopupCandidateKingdomIds, context.CapturedRecordStates, context.SourceSnapshot);
			result.RetryContext.RequiresFreshMaterials = context.RequiresFreshMaterials;
			_port.DisplayMessage(context.DisplayLabel + " generation paused: " + context.FailureCount + " weekly report target(s) failed.");
			if (context.QueueBlockingPopupOnFatalFailure)
			{
				_port.QueueFailurePopup(result.RetryContext, true);
			}
			_weeklyReportCommitQueue.Complete(context, result);
			return;
		}
		_port.DisplayMessage(context.DisplayLabel + " generation completed: success " + context.SuccessCount + ", failed " + context.FailureCount + ".");
		if (context.OpenViewerWhenDone)
		{
			_port.OpenViewer();
		}
		result.Completed = true;
		_weeklyReportCommitQueue.Complete(context, result);
	}

	internal static string ResolveWeeklyReportNoticeNearestKingdomId(PendingWeeklyReportCommitContext context, Func<IEnumerable<string>,string> resolveNearest)
	{
		if (context == null)
		{
			return "";
		}
		if (!context.WeeklyReportNoticeNearestKingdomResolved)
		{
			context.WeeklyReportNoticeNearestKingdomId = resolveNearest(context.PopupCandidateKingdomIds);
			context.WeeklyReportNoticeNearestKingdomResolved = true;
		}
		return context.WeeklyReportNoticeNearestKingdomId ?? "";
	}

	internal void TryQueueWeeklyReportMapNoticeForGeneratedReport(WeeklyEventMaterialPreviewGroup group, int weekIndex, string nearestKingdomId, HashSet<string> queuedEventIds)
	{
		if (group == null || weekIndex <= 0)
		{
			return;
		}
		if (!string.Equals((group.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase) || group.OutputMode == WeeklyReportOutputMode.TitleShortTagsOnly)
		{
			return;
		}
		string kingdomId = (group.KingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(kingdomId) || string.IsNullOrWhiteSpace(nearestKingdomId) || !string.Equals(kingdomId, nearestKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string eventId = BuildWeeklyReportEventId("kingdom", weekIndex, kingdomId);
		if (queuedEventIds != null && !queuedEventIds.Add(eventId))
		{
			return;
		}
		_port.QueueNotice(eventId);
	}

internal static string BuildWeeklyReportCommitRecordState(EventRecordEntry entry)
	{
		return entry == null ? null : JsonConvert.SerializeObject(entry);
	}
internal static bool IsWeeklyReportCommitWinner(EventRecordEntry entry, WeeklyEventMaterialPreviewGroup group, int weekIndex)
	{
		if (entry == null || group == null || entry.WeekIndex != weekIndex
			|| !string.Equals((entry.EventKind ?? "").Trim(), (group.GroupKind ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
			|| !string.Equals((entry.ScopeKingdomId ?? "").Trim(), (group.KingdomId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return group.OutputMode == WeeklyReportOutputMode.TitleShortTagsOnly
			? !string.IsNullOrWhiteSpace(entry.ShortSummary)
			: !string.IsNullOrWhiteSpace(entry.Summary);
	}
internal static WeeklyReportRequestResult BuildWeeklyReportFailedRequest(PendingWeeklyReportCommitContext context, WeeklyEventMaterialPreviewGroup failedGroup)
	{
		string reportId = BuildWeeklyReportGroupReportId(failedGroup);
		WeeklyReportBatchRequestResult failedBatch = null;
		WeeklyReportBatchRequestResult fallback = null;
		foreach (WeeklyReportBatchExecutionResult execution in context?.Executions ?? new List<WeeklyReportBatchExecutionResult>())
		{
			WeeklyReportBatchRequestResult candidate = execution?.Result;
			if (candidate == null || candidate.Success)
			{
				continue;
			}
			if (candidate.MissingReportIds?.Contains(reportId, StringComparer.OrdinalIgnoreCase) == true)
			{
				failedBatch = candidate;
			}
			else if (execution.Batch?.Groups?.Any((WeeklyEventMaterialPreviewGroup group) => string.Equals(BuildWeeklyReportGroupReportId(group), reportId, StringComparison.OrdinalIgnoreCase)) == true)
			{
				fallback = candidate;
			}
		}
		failedBatch ??= fallback;
		return new WeeklyReportRequestResult
		{
			Success = false,
			FailureReason = context?.FailureMessages?.FirstOrDefault() ?? "Batch request failed.",
			AttemptsUsed = failedBatch?.AttemptsUsed ?? 0,
			IsRateLimit = failedBatch?.IsRateLimit ?? false,
			IsRequestsPerMinuteLimit = failedBatch?.IsRequestsPerMinuteLimit ?? false,
			IsQuotaLimit = failedBatch?.IsQuotaLimit ?? false,
			RetryAfterSeconds = failedBatch?.RetryAfterSeconds
		};
	}
internal static string BuildWeeklyReportBatchPreviewKey(WeeklyReportBatchRequest batch)
	{
		List<string> list = WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);
		return ((batch != null) ? batch.WeekIndex : 0) + "|" + string.Join("|", list);
	}
internal static string BuildWeeklyReportGroupDisplayLabel(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "未命名分组";
		}
		string text = (group.Title ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界周报" : "王国周报";
	}
internal static string BuildWeeklyReportEventId(string eventKind, int weekIndex, string scopeKingdomId)
	{
		return "weekly_report:" + (eventKind ?? "").Trim().ToLowerInvariant() + ":" + Math.Max(0, weekIndex) + ":" + ((scopeKingdomId ?? "").Trim());
	}
internal EventRecordEntry FindWeeklyReportRecordById(string eventId)
	{
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return (_eventRecordEntries ?? new List<EventRecordEntry>()).FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

 internal static int ClampRequestsPerMinute(int value) => Math.Max(1,Math.Min(20,value));
}
