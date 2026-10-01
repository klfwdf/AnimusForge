using System;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public partial class MyBehavior
{
    private bool _dailyMemorySealCompletedPass => _memoryBusinessState.Sealing.CompletedPass;
    private object _dailyMemorySealState => _memoryBusinessState.Sealing.IsActive ? _memoryBusinessState.Sealing : null;
    private MemorySealingPort _memorySealingPort;
    private bool ContinueDailyMemorySeal(long startTimestamp, double budgetMs, bool requirePendingProbe) =>
        _memoryBusinessState.Sealing.ContinueDailyMemorySeal(startTimestamp, budgetMs, requirePendingProbe,
            _memorySealingPort ??= new MemorySealingPort
            {
                Log = message => Logger.Log("CompressedMemory", message),
                CurrentDay = () => (int)CampaignTime.Now.ToDays,
                SharedBudget = () => _campaignMemoryMaintenanceBudget,
                MetadataPerSlice = DailyMemorySealMetadataPerSlice,
                MaxJobs = DailyMaintenanceMaxJobsPerTick,
                IsEntityEligible = IsMemoryEntityEligibleForCompressedMemory,
                IsDailyPending = HasMemorySummaryJobStillPending,
                IsMajorPending = HasMajorActionSummaryJobStillPending,
                CancelUnavailable = (id, reason) => CancelUnavailableHeroCompressionWorkById(id, reason),
                EnqueueMajor = TryEnqueueMajorActionSummaryForDraft
            });
}
