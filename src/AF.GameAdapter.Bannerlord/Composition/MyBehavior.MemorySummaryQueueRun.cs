using System;using System.Threading.Tasks;using AnimusForge.Refactor.Runtime;using TaleWorlds.Library;
namespace AnimusForge;
public partial class MyBehavior
{
    private MemorySummaryQueueRunRuntime<MemorySummaryExecutionResult, MajorActionSummaryExecutionResult, MemoryOverviewExecutionResult> CreateMemorySummaryQueueRunRuntime() =>
        new MemorySummaryQueueRunRuntime<MemorySummaryExecutionResult, MajorActionSummaryExecutionResult, MemoryOverviewExecutionResult>(_memorySummaryRunOwner,
        new MemorySummaryQueueRunPort<MemorySummaryExecutionResult, MajorActionSummaryExecutionResult, MemoryOverviewExecutionResult> {
            BuildPlanAsync = (generation, overview, excluded, cleanup, run) => BuildMemorySummaryPlanAsync(generation, overview, excluded, cleanup, run),
            RunPhaseAsync = RunMemorySummaryRunPhaseAsync,
            ExecuteItemAsync = async (item, run) => {
                var result = await ExecuteDailySummaryQueueItemAsync(item, run);
                return result == null ? null : new MemorySummaryWaveResult<MemorySummaryExecutionResult, MajorActionSummaryExecutionResult, MemoryOverviewExecutionResult> {
                    Daily = result.MemoryResult, Major = result.MajorActionResult, Overview = result.MemoryOverviewResult
                };
            },
            ShouldScanOverview = ShouldScanMemoryOverviewCandidates,
            QueueAllOverview = QueueAllMemoryOverviewCandidatesForDeferredScan,
            QueueDirtyOverview = QueueDirtyMemoryOverviewCandidatesForDeferredScan,
            RequestsPerMinute = GetMemorySummaryRequestsPerMinuteFromSettings,
            IsHostCurrent = () => ReferenceEquals(Instance, this),
            Notify = text => InformationManager.DisplayMessage(new InformationMessage(text)),
            BlockingPopup = (title, message, generation, display) => MemoryFailureNotices.PublishMemoryFailureNotice(title, message, generation, display),
            LogFailure = error => Logger.Log("CompressedMemory", "[ERROR] ProcessMemorySummaryQueueAsync failed: " + error),
            DelayAsync = milliseconds => Task.Delay(milliseconds),
            Daily = new MemorySummaryResultPort<MemorySummaryExecutionResult> {
                HasJob = result => result?.Job != null,
                IsObsolete = result => result?.IsObsolete ?? true,
                IsSourceCurrent = result => IsMemorySummaryInputCurrent(result.Source),
                Succeeded = result => result?.Success ?? false,
                ApplySuccess = result => ApplyMemorySummarySuccess(result.Job, result.Block),
                MarkFailure = result => MarkMemorySummaryFailure(result.Job, result.Error),
                FailureText = result => (result.Job.HeroName ?? result.Job.HeroId) + AdapterPromptCaptureTexts.PcapCompat("Host_MemoryDayPrefix") + result.Job.GameDayIndex + AdapterPromptCaptureTexts.PcapCompat("Host_MemoryDaySuffix") + (result.Error ?? AdapterPromptCaptureTexts.PcapCompat("Host_UnknownError")),
                FailureDisplayText = result => MemoryFailureUiText.T("AF_MEMORY_UI_DailyFailureLine", "{NAME}, day {DAY}: {ERROR}", ("NAME", result.Job.HeroName ?? result.Job.HeroId), ("DAY", result.Job.GameDayIndex), ("ERROR", result.ErrorDisplay ?? MemoryFailureUiText.Error(result.Error)))
            },
            Major = new MemorySummaryResultPort<MajorActionSummaryExecutionResult> {
                HasJob = result => result?.Job != null,
                IsObsolete = result => result?.IsObsolete ?? true,
                IsSourceCurrent = result => IsMemorySummaryInputCurrent(result.Source),
                Succeeded = result => result?.Success ?? false,
                ApplySuccess = result => ApplyMajorActionSummarySuccess(result.Job, result.State),
                MarkFailure = result => MarkMajorActionSummaryFailure(result.Job, result.Error),
                FailureText = result => (result.Job.HeroName ?? result.Job.HeroId) + AdapterPromptCaptureTexts.PcapCompat("Host_MajorHistoryFailure") + (result.Error ?? AdapterPromptCaptureTexts.PcapCompat("Host_UnknownError")),
                FailureDisplayText = result => MemoryFailureUiText.T("AF_MEMORY_UI_MajorFailureLine", "{NAME}, major history: {ERROR}", ("NAME", result.Job.HeroName ?? result.Job.HeroId), ("ERROR", result.ErrorDisplay ?? MemoryFailureUiText.Error(result.Error)))
            },
            Overview = new MemorySummaryResultPort<MemoryOverviewExecutionResult> {
                HasJob = result => result?.Job != null,
                IsObsolete = result => result?.IsObsolete ?? true,
                IsSourceCurrent = result => IsMemorySummaryInputCurrent(result.Source),
                Succeeded = result => result?.Success ?? false,
                ApplySuccess = result => ApplyMemoryOverviewSuccess(result.Job, result.State),
                MarkFailure = result => MarkMemoryOverviewFailure(result.Job, result.Error),
                FailureText = result => (result.Job.HeroName ?? result.Job.HeroId) + AdapterPromptCaptureTexts.PcapCompat("Host_OverviewFailure") + (result.Error ?? AdapterPromptCaptureTexts.PcapCompat("Host_UnknownError")),
                FailureDisplayText = result => MemoryFailureUiText.T("AF_MEMORY_UI_OverviewFailureLine", "{NAME}, memory overview: {ERROR}", ("NAME", result.Job.HeroName ?? result.Job.HeroId), ("ERROR", result.ErrorDisplay ?? MemoryFailureUiText.Error(result.Error)))
            }
        });
}
