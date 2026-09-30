using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;

internal static class CampaignApplicationReplay
{
    private sealed class Flags
    {
        internal readonly List<string> Events = new();
        internal bool DisabledStateApplied;
    }
    private struct Source : IWorldDiplomacyCampaignSource
    {
        private readonly Flags _f;
        internal Source(Flags f) : this() { _f = f; }
        public bool IsEnabled { get; set; }
        public int CurrentDay { get; set; }
        public bool DisabledStateApplied { get => _f.DisabledStateApplied; set => _f.DisabledStateApplied = value; }
        public bool NativeQueueSanitized { get; set; }
        public int LastSchedulerDay { get; set; }
        public void RemoveQueuedNativeDiplomacyDecisions() { _f.Events.Add("RemoveQueuedNativeDiplomacyDecisions"); }
        public void EnsureActiveWarLedgersAndRemoveEndedWars() { _f.Events.Add("EnsureActiveWarLedgersAndRemoveEndedWars"); }
        public void TrimRecentBattleFacts() { _f.Events.Add("TrimRecentBattleFacts"); }
        public void ClearDailyCaches() { _f.Events.Add("ClearDailyCaches"); }
        public void ResetDailyGenerationBudget() { _f.Events.Add("ResetDailyGenerationBudget"); }
        public void DecayWarPressure() { _f.Events.Add("DecayWarPressure"); }
    }
    private sealed class Orch : FakeOrchestration
    {
        private readonly Flags _f;
        internal Orch(Flags f) { _f = f; }
        public override void TryApplyInitialNewGamePeace() { _f.Events.Add("TryApplyInitialNewGamePeace"); }
        public override void HandleDisabledState() { _f.Events.Add("HandleDisabledState"); _f.DisabledStateApplied = true; }
        public override void RefreshPolicyDiplomacySignals() { _f.Events.Add("RefreshPolicyDiplomacySignals"); }
        public override void ProcessRelayArrivals() { _f.Events.Add("ProcessRelayArrivals"); }
        public override void ProcessRoundLifecycle() { _f.Events.Add("ProcessRoundLifecycle"); }
        public override void TrySchedulePolicyTriggeredRound() { _f.Events.Add("TrySchedulePolicyTriggeredRound"); }
        public override void TryScheduleNormalRound() { _f.Events.Add("TryScheduleNormalRound"); }
        public override void ReconcileAllNationalPrestigeVassalRelations() { _f.Events.Add("ReconcileAllNationalPrestigeVassalRelations"); }
        public override void RetryDeferredCanonicalHistoryEntries(int maxAttempts) { _f.Events.Add("RetryDeferredCanonicalHistoryEntries"); }
        public override void RetryDiplomaticThreatDomesticPenalties() { _f.Events.Add("RetryDiplomaticThreatDomesticPenalties"); }
        public override void RetryDiplomaticThreatComplianceConsequences() { _f.Events.Add("RetryDiplomaticThreatComplianceConsequences"); }
        public override void RetryDiplomaticThreatHistoryResults() { _f.Events.Add("RetryDiplomaticThreatHistoryResults"); }
        public override void RefreshRoundIntervalScheduleIfNeeded() { _f.Events.Add("RefreshRoundIntervalScheduleIfNeeded"); }
        public override void RecalculatePendingPropagationIfNeeded() { _f.Events.Add("RecalculatePendingPropagationIfNeeded"); }
        public override void AnchorInternationalReputationNaturalChangeDays() { _f.Events.Add("AnchorInternationalReputationNaturalChangeDays"); }
        public override void ProcessInternationalReputationNaturalChange() { _f.Events.Add("ProcessInternationalReputationNaturalChange"); }
        public override void RetryDeferredDocumentPropagation() { _f.Events.Add("RetryDeferredDocumentPropagation"); }
        public override void ProcessPropagationArrivals() { _f.Events.Add("ProcessPropagationArrivals"); }
        public override void TryScheduleTokenCompression() { _f.Events.Add("TryScheduleTokenCompression"); }
        public override void NormalizeStorage(bool allowWorldValidation) { _f.Events.Add("NormalizeStorage"); }
        public override void RetryDeferredRoundProgress() { _f.Events.Add("RetryDeferredRoundProgress"); }
    }
    internal static void Run()
    {
        var flags = new Flags();
        var orch = new Orch(flags);
        var source = new Source(flags) { IsEnabled = true, CurrentDay = 7 };
        WorldDiplomacyCampaignApplication.CampaignTick(ref source, orch);
        Test.True(string.Join(",", flags.Events) == "TryApplyInitialNewGamePeace,RemoveQueuedNativeDiplomacyDecisions,RefreshPolicyDiplomacySignals,ProcessRelayArrivals,ProcessRoundLifecycle,TrySchedulePolicyTriggeredRound,TryScheduleNormalRound"
            && source.NativeQueueSanitized && source.LastSchedulerDay == 7, "campaign entry preserves sanitation and once-per-day scheduling order");
        flags.Events.Clear();
        WorldDiplomacyCampaignApplication.CampaignTick(ref source, orch);
        Test.True(flags.Events.SequenceEqual(new[] { "TryApplyInitialNewGamePeace" }), "same-day campaign ticks do not rescan queues or schedule rounds");
        source.IsEnabled = false; flags.Events.Clear();
        WorldDiplomacyCampaignApplication.CampaignTick(ref source, orch);
        Test.True(string.Join(",", flags.Events) == "TryApplyInitialNewGamePeace,HandleDisabledState", "disabled campaign applies closure once");
        flags.Events.Clear();
        WorldDiplomacyCampaignApplication.DailyTick(ref source, orch);
        const string maintenance = "NormalizeStorage,ReconcileAllNationalPrestigeVassalRelations,RetryDeferredCanonicalHistoryEntries,RetryDiplomaticThreatDomesticPenalties,RetryDiplomaticThreatComplianceConsequences,RetryDiplomaticThreatHistoryResults,RefreshRoundIntervalScheduleIfNeeded,ClearDailyCaches,ResetDailyGenerationBudget,RecalculatePendingPropagationIfNeeded,EnsureActiveWarLedgersAndRemoveEndedWars,TrimRecentBattleFacts";
        Test.True(string.Join(",", flags.Events) == maintenance + ",AnchorInternationalReputationNaturalChangeDays", "disabled daily tick still performs recovery and pins the reputation anchor");
        source.IsEnabled = true; flags.Events.Clear(); source.CurrentDay = 8;
        WorldDiplomacyCampaignApplication.DailyTick(ref source, orch);
        Test.True(string.Join(",", flags.Events) == maintenance + ",RemoveQueuedNativeDiplomacyDecisions,ProcessInternationalReputationNaturalChange,DecayWarPressure,RefreshPolicyDiplomacySignals,RetryDeferredDocumentPropagation,ProcessPropagationArrivals,ProcessRelayArrivals,RetryDeferredRoundProgress,ProcessRoundLifecycle,TryScheduleTokenCompression,TrySchedulePolicyTriggeredRound,TryScheduleNormalRound"
            && !source.DisabledStateApplied && source.NativeQueueSanitized && source.LastSchedulerDay == 8, "enabled daily tick preserves complete recovery/publication/scheduling order");
    }
}
