using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using DailyMaintenanceTaskKind = AnimusForge.MyBehavior.DailyMaintenanceTaskKind;
using DailyMaintenanceJob = AnimusForge.MyBehavior.DailyMaintenanceJob;
using System.Collections.Generic;
using System;
namespace AnimusForge;

// Main-thread daily queue and budget orchestration; effects bind to the existing domain authorities.
internal sealed class CampaignDailyMaintenanceController
{
 internal const double DailyMaintenanceDefaultFrameBudgetMs = 3.0;
 internal const int DailyMaintenanceMaxJobsPerTick = 8;
 internal const int DailyMemorySealMetadataPerSlice = 128;
internal static bool IsDeferredDailyMaintenanceEnabled()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableDeferredDailyMaintenance != false;
		}
		catch
		{
			return true;
		}
	}

internal static double GetDailyMaintenanceFrameBudgetMs()
	{
		try
		{
			return Math.Max(1.0, Math.Min(10.0, (DuelSettings.GetSettings()?.DailyMaintenanceFrameBudgetMs).GetValueOrDefault((int)DailyMaintenanceDefaultFrameBudgetMs)));
		}
		catch
		{
			return DailyMaintenanceDefaultFrameBudgetMs;
		}
	}

 private readonly CampaignDailyMaintenanceCapabilities _port;
 private readonly MemoryBusinessStateOwner _memory;
 private readonly Func<bool> _hasPendingWeeklyBuild;
 internal readonly Queue<DailyMaintenanceJob> Jobs = new Queue<DailyMaintenanceJob>();
 internal readonly HashSet<string> JobKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 internal CampaignDailyMaintenanceController(MemoryBusinessStateOwner memory, Func<bool> hasPendingWeeklyBuild, CampaignDailyMaintenanceCapabilities port = null) { _memory=memory;_hasPendingWeeklyBuild=hasPendingWeeklyBuild;_port=port; }
internal void EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind kind, int dayIndex = -1, int weekIndex = 0, int startDay = 0, int endDay = 0, string reason = "")
	{
		string key = BuildDailyMaintenanceJobKey(kind, dayIndex, weekIndex, startDay, endDay);
		if (!JobKeys.Add(key))
		{
			return;
		}
		Jobs.Enqueue(new DailyMaintenanceJob
		{
			Kind = kind,
			DayIndex = dayIndex,
			WeekIndex = weekIndex,
			StartDay = startDay,
			EndDay = endDay,
			Reason = (reason ?? "").Trim()
		});
	}
internal static string BuildDailyMaintenanceJobKey(DailyMaintenanceTaskKind kind, int dayIndex, int weekIndex, int startDay, int endDay)
	{
		return kind + ":" + dayIndex + ":" + weekIndex + ":" + startDay + ":" + endDay;
	}
internal bool HasPendingDeferredDailyMaintenanceWork()
	{
		return Jobs.Count > 0 || _memory.OverviewCandidateIds.Count > 0 || _hasPendingWeeklyBuild();
	}

internal void RunDailyMaintenanceSynchronously()
	{
		_port.Seal(0L, double.MaxValue);
		_port.QueueDirty();
		_port.Scan(0L, double.MaxValue);
		_port.StartSummary();
		_port.DiscontinueRebels("daily_tick");
		_port.EnsureOpening();
		_port.RecordMissedEvents();
		_port.ApplyRelations();
		int currentGameDayIndexSafe = _port.CurrentDay();
		int weekIndex = (currentGameDayIndexSafe > 0) ? (currentGameDayIndexSafe / 7) : 0;
		if (currentGameDayIndexSafe > 0 && currentGameDayIndexSafe % 7 == 0)
		{
			_port.WeeklyRebellions(weekIndex);
		}
		if (_port.WeeklyRunning())
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		int missingWeek = _port.Schedule.SelectWeek(_port.LastAutoWeek(),
			currentGameDayIndexSafe, weekIndex, _port.WeeklyRunning(),
			settings != null && settings.IsLegacyWeeklyAutoGenerationActive(), _port.RebellionFlow());
		if (missingWeek <= 0)
		{
			return;
		}
		_port.StartAutoWeekly(missingWeek, currentGameDayIndexSafe);
	}

internal void EnqueueDailyMaintenanceForCurrentDay(string reason)
	{
		int currentGameDayIndexSafe = _port.CurrentDay();
		int weekIndex = (currentGameDayIndexSafe > 0) ? (currentGameDayIndexSafe / 7) : 0;
		if (currentGameDayIndexSafe > 0 && currentGameDayIndexSafe % 7 == 0)
		{
			EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.ProcessWeeklyKingdomRebellions, currentGameDayIndexSafe, weekIndex, reason: reason);
		}
		QueueDeferredAutoWeeklyReportsForWeek(weekIndex, currentGameDayIndexSafe, reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.SealPastDailyMemoryDrafts, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.QueueDirtyMemoryOverviewScan, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.StartMemorySummaryQueue, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.DiscontinueLandlessRebelKingdoms, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.EnsureWeekZeroOpeningSummaryEvents, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.RecordMissedStrategicWorldEvents, currentGameDayIndexSafe, reason: reason);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.ApplyKingdomStabilityRelationAdjustments, currentGameDayIndexSafe, reason: reason);
	}

internal void QueueDeferredAutoWeeklyReportsForWeek(int weekIndex, int currentGameDayIndexSafe, string reason)
	{
		_port.SynchronizeNewsCollectionMode?.Invoke();
		if (_port.HasRestartedWeeklyCollection?.Invoke() == true) return;
		DuelSettings settings = DuelSettings.GetSettings();
		int missingWeek = _port.Schedule.SelectWeek(_port.LastAutoWeek(),
			currentGameDayIndexSafe, weekIndex, _port.WeeklyRunning(),
			settings != null && settings.IsLegacyWeeklyAutoGenerationActive(), _port.RebellionFlow());
		if (missingWeek <= 0)
		{
			return;
		}
		int startDay = WeeklyReportSchedulePolicy.GetStartDay(missingWeek);
		int endDay = WeeklyReportSchedulePolicy.GetEndDay(missingWeek);
		EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.PrepareAutoWeeklyReports, endDay, missingWeek, startDay, endDay, reason);
	}

internal void ProcessDeferredDailyMaintenance()
	{
		if ((!IsDeferredDailyMaintenanceEnabled() && _port.HasRestartedWeeklyCollection?.Invoke() != true) || !HasPendingDeferredDailyMaintenanceWork())
		{
			return;
		}
		_memory.ResolveMaintenanceBudget(GetDailyMaintenanceFrameBudgetMs, DailyMemorySealMetadataPerSlice, DailyMaintenanceMaxJobsPerTick, out long startTimestamp, out double budgetMs);
		if (WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs)) return;
		if (_hasPendingWeeklyBuild() && !_port.RebellionFlow())
		{
			using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.ProcessPendingAutoWeeklyReportBuild"))
			{
				_port.ProcessPendingWeekly(startTimestamp, budgetMs);
			}
			if (_hasPendingWeeklyBuild() || WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
			{
				return;
			}
		}
		int processedScanCount;
		using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.ProcessMemoryOverviewCandidateScan"))
		{
			processedScanCount = _port.Scan(startTimestamp, budgetMs);
		}
		int processedJobCount = 0;
		while (Jobs.Count > 0 && processedJobCount < DailyMaintenanceMaxJobsPerTick && !WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			DailyMaintenanceJob job = Jobs.Dequeue();
			JobKeys.Remove(BuildDailyMaintenanceJobKey(job.Kind, job.DayIndex, job.WeekIndex, job.StartDay, job.EndDay));
			bool completed;
			using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.Job." + job.Kind))
			{
				completed = ExecuteDailyMaintenanceJob(job, startTimestamp, budgetMs);
			}
			if (!completed)
			{
				EnqueueDailyMaintenanceJob(job.Kind, job.DayIndex, job.WeekIndex, job.StartDay, job.EndDay, job.Reason);
			}
			processedJobCount++;
			using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.ProcessMemoryOverviewCandidateScan"))
			{
				processedScanCount += _port.Scan(startTimestamp, budgetMs);
			}
		}
		if (!WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.ProcessPendingAutoWeeklyReportBuild"))
			{
				_port.ProcessPendingWeekly(startTimestamp, budgetMs);
			}
		}
		using (PerfProbe.Scope("MyBehavior.DeferredDailyMaintenance.ProcessMemoryOverviewCandidateScan"))
		{
			processedScanCount += _port.Scan(startTimestamp, budgetMs);
		}
		if (processedScanCount > 0 && !_port.SummaryRunning() && !WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			_port.StartSummary();
		}
	}

internal bool ExecuteDailyMaintenanceJob(DailyMaintenanceJob job, long startTimestamp, double budgetMs)
	{
		if (job == null)
		{
			return true;
		}
		try
		{
			if (WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
			{
				return false;
			}
			switch (job.Kind)
			{
			case DailyMaintenanceTaskKind.SealPastDailyMemoryDrafts:
				if (!_port.Seal(startTimestamp, budgetMs))
				{
					return false;
				}
				break;
			case DailyMaintenanceTaskKind.StartMemorySummaryQueue:
				_port.StartSummary();
				break;
			case DailyMaintenanceTaskKind.QueueDirtyMemoryOverviewScan:
				_port.QueueDirty();
				break;
			case DailyMaintenanceTaskKind.QueueFullMemoryOverviewScan:
				_port.QueueAll();
				break;
			case DailyMaintenanceTaskKind.DiscontinueLandlessRebelKingdoms:
				_port.DiscontinueRebels(string.IsNullOrWhiteSpace(job.Reason) ? "deferred_daily_tick" : job.Reason);
				break;
			case DailyMaintenanceTaskKind.EnsureWeekZeroOpeningSummaryEvents:
				if (!_port.OpeningSlice())
				{
					return false;
				}
				break;
			case DailyMaintenanceTaskKind.RecordMissedStrategicWorldEvents:
				if (!_port.MissedEventsSlice())
				{
					return false;
				}
				break;
			case DailyMaintenanceTaskKind.ApplyKingdomStabilityRelationAdjustments:
				if (!_port.RelationSlice())
				{
					return false;
				}
				break;
			case DailyMaintenanceTaskKind.ProcessWeeklyKingdomRebellions:
				return _port.RebellionSlice(job.WeekIndex);
			case DailyMaintenanceTaskKind.PrepareAutoWeeklyReports:
				_port.InitializePendingWeekly(job);
				break;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DailyMaintenance", "[ERROR] " + job.Kind + " failed: " + ex.Message);
			return true;
		}
	}

internal void ClearQueuedJobs() { Jobs.Clear(); JobKeys.Clear(); }

 internal bool SummaryStartPending;
 internal long SummaryStartGeneration;
 internal int LastMemoryMaintenanceObservedGameDay = -1;
 private void ResolveDailyMaintenanceBudget(out long startTimestamp, out double budgetMs) => _memory.ResolveMaintenanceBudget(GetDailyMaintenanceFrameBudgetMs, DailyMemorySealMetadataPerSlice, DailyMaintenanceMaxJobsPerTick, out startTimestamp, out budgetMs);
internal void RunCampaignMemoryMaintenanceCycle(bool processedWeeklyReportCommits)
	{
		var previous = _memory.MaintenanceBudget;
		bool previousActive = _memory.MaintenanceCycleActive;
		_memory.MaintenanceCycleActive = true;
		try
		{
			using (PerfProbe.Scope("MyBehavior.OnCampaignTick.TryRunCampaignMemoryMaintenance"))
			{
				TryRunCampaignMemoryMaintenance();
			}
			if (!processedWeeklyReportCommits && HasPendingDeferredDailyMaintenanceWork())
			{
				using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessDeferredDailyMaintenance"))
				{
					ProcessDeferredDailyMaintenance();
				}
			}
		}
		finally
		{
			_memory.MaintenanceBudget = previous;
			_memory.MaintenanceCycleActive = previousActive;
		}
	}
internal void TryRunCampaignMemoryMaintenance()
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		if (SummaryStartPending && SummaryStartGeneration != generation)
			SummaryStartPending = false;
		if (_port.SummaryRunning()) return;
		if (SummaryStartPending)
		{
			ResolveDailyMaintenanceBudget(out long pendingStart, out double pendingBudget);
			if (!MemoryEntityIdentityBannerlordAdapter.IsDialogueOrLetterChainBusyForMemorySummary() && !WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(pendingStart, pendingBudget))
			{
				// This is a deferred admission request, not a replay of accepted side effects.
				SummaryStartPending = false;
				_port.StartSummary();
			}
			return;
		}
		int currentDay = 0;
		try { currentDay = (int)CampaignTime.Now.ToDays; } catch { currentDay = 0; }
		// A paused seal is work even before its first summary job exists. It must
		// resume on the same day rather than depending on the raw queue counts.
		bool hasQueuedWork = (_memory.DailyQueue?.Count ?? 0) > 0 || (_memory.MajorQueue?.Count ?? 0) > 0 || (_memory.OverviewQueue?.Count ?? 0) > 0;
		bool sealActive = _memory.Sealing.IsActive;
		if (!sealActive && !hasQueuedWork && currentDay == LastMemoryMaintenanceObservedGameDay) return;
		if (MemoryEntityIdentityBannerlordAdapter.IsDialogueOrLetterChainBusyForMemorySummary()) return;
		ResolveDailyMaintenanceBudget(out long startTimestamp, out double budgetMs);
		if (WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs)) return;
		int observedDay = sealActive ? _memory.Sealing.TargetDay : currentDay;
		using (PerfProbe.Scope("MyBehavior.TryRunCampaignMemoryMaintenance.TrySealPastDailyMemoryDrafts"))
		{
			// The old whole-source HasPast predicate is now a non-mutating phase
			// sharing this deadline. No sealing effects run when that probe is empty.
			if (!_port.SealWithPendingProbe(startTimestamp, budgetMs)) return;
		}
		if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) return;
		LastMemoryMaintenanceObservedGameDay = observedDay;
		if (!hasQueuedWork && !_memory.Sealing.CompletedPass) return;
		// Sealing can consume the final raw job just as its window expires.
		// Keep its one start request across ticks without running sealing again.
		SummaryStartPending = true;
		SummaryStartGeneration = generation;
		if (!WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			SummaryStartPending = false;
			using (PerfProbe.Scope("MyBehavior.TryRunCampaignMemoryMaintenance.TryStartMemorySummaryQueue"))
			{
				_port.StartSummary();
			}
		}
	}
}

internal sealed class CampaignDailyMaintenanceCapabilities
{
 internal WeeklyAutoScheduleOwner Schedule;
 internal Func<int> CurrentDay, LastAutoWeek;
 internal Func<bool> WeeklyRunning, SummaryRunning, RebellionFlow;
 internal Action SynchronizeNewsCollectionMode;
 internal Func<bool> HasRestartedWeeklyCollection;
 internal Func<long,double,bool> Seal, SealWithPendingProbe;
 internal Action QueueDirty, QueueAll, StartSummary, EnsureOpening, RecordMissedEvents, ApplyRelations;
 internal Func<long,double,int> Scan;
 internal Action<string> DiscontinueRebels;
 internal Action<int> WeeklyRebellions;
 internal Func<bool> OpeningSlice, MissedEventsSlice, RelationSlice;
 internal Func<int,bool> RebellionSlice;
 internal Action<DailyMaintenanceJob> InitializePendingWeekly;
 internal Action<long,double> ProcessPendingWeekly;
 internal Action<int,int> StartAutoWeekly;
}
