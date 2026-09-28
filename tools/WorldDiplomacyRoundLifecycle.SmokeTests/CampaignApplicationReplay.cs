using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

internal static class CampaignApplicationReplay
{
    private struct Source : IWorldDiplomacyCampaignSource
    {
        internal List<string> Events;
        public bool IsEnabled { get; set; }
        public int CurrentDay { get; set; }
        public bool DisabledStateApplied { get; set; }
        public bool NativeQueueSanitized { get; set; }
        public int LastSchedulerDay { get; set; }
        public void TryApplyInitialNewGamePeace() { Events.Add("TryApplyInitialNewGamePeace"); }
        public void HandleDisabledState() { Events.Add("HandleDisabledState"); DisabledStateApplied = true; }
        public void RemoveQueuedNativeDiplomacyDecisions() { Events.Add("RemoveQueuedNativeDiplomacyDecisions"); }
        public void RefreshPolicyDiplomacySignals() { Events.Add("RefreshPolicyDiplomacySignals"); }
        public void ProcessRelayArrivals() { Events.Add("ProcessRelayArrivals"); }
        public void ProcessRoundLifecycle() { Events.Add("ProcessRoundLifecycle"); }
        public void TrySchedulePolicyTriggeredRound() { Events.Add("TrySchedulePolicyTriggeredRound"); }
        public void TryScheduleNormalRound() { Events.Add("TryScheduleNormalRound"); }
        public void ReconcileAllNationalPrestigeVassalRelations() { Events.Add("ReconcileAllNationalPrestigeVassalRelations"); }
        public void RetryDeferredCanonicalHistoryEntries() { Events.Add("RetryDeferredCanonicalHistoryEntries"); }
        public void RetryDiplomaticThreatDomesticPenalties() { Events.Add("RetryDiplomaticThreatDomesticPenalties"); }
        public void RetryDiplomaticThreatComplianceConsequences() { Events.Add("RetryDiplomaticThreatComplianceConsequences"); }
        public void RetryDiplomaticThreatHistoryResults() { Events.Add("RetryDiplomaticThreatHistoryResults"); }
        public void RefreshRoundIntervalScheduleIfNeeded() { Events.Add("RefreshRoundIntervalScheduleIfNeeded"); }
        public void RecalculatePendingPropagationIfNeeded() { Events.Add("RecalculatePendingPropagationIfNeeded"); }
        public void EnsureActiveWarLedgersAndRemoveEndedWars() { Events.Add("EnsureActiveWarLedgersAndRemoveEndedWars"); }
        public void TrimRecentBattleFacts() { Events.Add("TrimRecentBattleFacts"); }
        public void AnchorInternationalReputationNaturalChangeDays() { Events.Add("AnchorInternationalReputationNaturalChangeDays"); }
        public void ProcessInternationalReputationNaturalChange() { Events.Add("ProcessInternationalReputationNaturalChange"); }
        public void RetryDeferredDocumentPropagation() { Events.Add("RetryDeferredDocumentPropagation"); }
        public void ProcessPropagationArrivals() { Events.Add("ProcessPropagationArrivals"); }
        public void TryScheduleTokenCompression() { Events.Add("TryScheduleTokenCompression"); }
        public void NormalizeStorage() { Events.Add("NormalizeStorage"); }
        public void ClearDailyCaches() { Events.Add("ClearDailyCaches"); }
        public void ResetDailyGenerationBudget() { Events.Add("ResetDailyGenerationBudget"); }
        public void DecayWarPressure() { Events.Add("DecayWarPressure"); }
        public void RetryDeferredRoundProgress() { Events.Add("RetryDeferredRoundProgress"); }

    }
    internal static void Run()
    {
        var source = new Source { Events = new(), IsEnabled = true, CurrentDay = 7 };
        WorldDiplomacyCampaignApplication.CampaignTick(ref source);
        Test.True(string.Join(",", source.Events) == "TryApplyInitialNewGamePeace,RemoveQueuedNativeDiplomacyDecisions,RefreshPolicyDiplomacySignals,ProcessRelayArrivals,ProcessRoundLifecycle,TrySchedulePolicyTriggeredRound,TryScheduleNormalRound"
            && source.NativeQueueSanitized && source.LastSchedulerDay == 7, "campaign entry preserves sanitation and once-per-day scheduling order");
        source.Events.Clear();
        WorldDiplomacyCampaignApplication.CampaignTick(ref source);
        Test.True(source.Events.SequenceEqual(new[] { "TryApplyInitialNewGamePeace" }), "same-day campaign ticks do not rescan queues or schedule rounds");
        source.IsEnabled = false; source.Events.Clear();
        WorldDiplomacyCampaignApplication.CampaignTick(ref source);
        Test.True(string.Join(",", source.Events) == "TryApplyInitialNewGamePeace,HandleDisabledState", "disabled campaign applies closure once");
        source.Events.Clear();
        WorldDiplomacyCampaignApplication.DailyTick(ref source);
        const string maintenance = "NormalizeStorage,ReconcileAllNationalPrestigeVassalRelations,RetryDeferredCanonicalHistoryEntries,RetryDiplomaticThreatDomesticPenalties,RetryDiplomaticThreatComplianceConsequences,RetryDiplomaticThreatHistoryResults,RefreshRoundIntervalScheduleIfNeeded,ClearDailyCaches,ResetDailyGenerationBudget,RecalculatePendingPropagationIfNeeded,EnsureActiveWarLedgersAndRemoveEndedWars,TrimRecentBattleFacts";
        Test.True(string.Join(",", source.Events) == maintenance + ",AnchorInternationalReputationNaturalChangeDays", "disabled daily tick still performs recovery and pins the reputation anchor");
        source.IsEnabled = true; source.Events.Clear(); source.CurrentDay = 8;
        WorldDiplomacyCampaignApplication.DailyTick(ref source);
        Test.True(string.Join(",", source.Events) == maintenance + ",RemoveQueuedNativeDiplomacyDecisions,ProcessInternationalReputationNaturalChange,DecayWarPressure,RefreshPolicyDiplomacySignals,RetryDeferredDocumentPropagation,ProcessPropagationArrivals,ProcessRelayArrivals,RetryDeferredRoundProgress,ProcessRoundLifecycle,TryScheduleTokenCompression,TrySchedulePolicyTriggeredRound,TryScheduleNormalRound"
            && !source.DisabledStateApplied && source.NativeQueueSanitized && source.LastSchedulerDay == 8, "enabled daily tick preserves complete recovery/publication/scheduling order");
    }
}
