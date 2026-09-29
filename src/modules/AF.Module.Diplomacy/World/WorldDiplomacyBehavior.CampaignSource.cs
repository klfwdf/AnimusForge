using AnimusForge.Refactor.Domain;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal struct CampaignSource : IWorldDiplomacyCampaignSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal CampaignSource(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool IsEnabled => IsWorldDiplomacyEnabled();
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public bool DisabledStateApplied { get => _owner._disabledStateApplied; set => _owner._disabledStateApplied = value; }
        public bool NativeQueueSanitized { get => _owner._nativeDiplomacyDecisionQueueSanitized; set => _owner._nativeDiplomacyDecisionQueueSanitized = value; }
        public int LastSchedulerDay { get => _owner._lastSchedulerDay; set => _owner._lastSchedulerDay = value; }
        public void TryApplyInitialNewGamePeace() => _owner.TryApplyInitialNewGamePeace();
        public void HandleDisabledState() => _owner.HandleDisabledState();
        public void RemoveQueuedNativeDiplomacyDecisions() => _owner.RemoveQueuedNativeDiplomacyDecisions();
        public void RefreshPolicyDiplomacySignals() => _owner.RefreshPolicyDiplomacySignals();
        public void ProcessRelayArrivals() => _owner.ProcessRelayArrivals();
        public void ProcessRoundLifecycle() => _owner.ProcessRoundLifecycle();
        public void TrySchedulePolicyTriggeredRound() => _owner.TrySchedulePolicyTriggeredRound();
        public void TryScheduleNormalRound() => _owner.TryScheduleNormalRound();
        public void ReconcileAllNationalPrestigeVassalRelations() => _owner.ReconcileAllNationalPrestigeVassalRelations();
        public void RetryDeferredCanonicalHistoryEntries() => _owner.RetryDeferredCanonicalHistoryEntries();
        public void RetryDiplomaticThreatDomesticPenalties() => _owner.RetryDiplomaticThreatDomesticPenalties();
        public void RetryDiplomaticThreatComplianceConsequences() => _owner.RetryDiplomaticThreatComplianceConsequences();
        public void RetryDiplomaticThreatHistoryResults() => _owner.RetryDiplomaticThreatHistoryResults();
        public void RefreshRoundIntervalScheduleIfNeeded() => _owner.RefreshRoundIntervalScheduleIfNeeded();
        public void RecalculatePendingPropagationIfNeeded() => _owner.RecalculatePendingPropagationIfNeeded();
        public void EnsureActiveWarLedgersAndRemoveEndedWars() => _owner.EnsureActiveWarLedgersAndRemoveEndedWars();
        public void TrimRecentBattleFacts() => _owner.TrimRecentBattleFacts();
        public void AnchorInternationalReputationNaturalChangeDays() => _owner.AnchorInternationalReputationNaturalChangeDays();
        public void ProcessInternationalReputationNaturalChange() => _owner.ProcessInternationalReputationNaturalChange();
        public void RetryDeferredDocumentPropagation() => _owner.RetryDeferredDocumentPropagation();
        public void ProcessPropagationArrivals() => _owner.ProcessPropagationArrivals();
        public void TryScheduleTokenCompression() => _owner.TryScheduleTokenCompression();
        public void NormalizeStorage() => _owner.NormalizeStorage (allowWorldValidation: true);
        public void ClearDailyCaches()
        {
            _owner._warSituationCache.Clear();
            _owner._realmRelationProfileCache.Clear();
            _owner._courtSettlementCache.Clear();
            _owner._kingdomBorderCache.Clear();
            _owner._kingdomBorderCacheDay = -1;
        }
        public void ResetDailyGenerationBudget() => WorldDiplomacyRoundLifecycleRules.ResetDailyGenerationBudget(
            ref _owner._aiDocumentsStartedDay, ref _owner._aiDocumentsStartedToday, CurrentDay);
        public void DecayWarPressure() => WorldDiplomacyWarPressureRules.DecayWarPressure(_owner._storage?.WarPressure, CurrentDay);
        public void RetryDeferredRoundProgress() => WorldDiplomacyRoundProgressApplication.RetryDeferredRoundProgress(_owner._storage, _owner.HandleRoundDocumentProcessed, Log);
    }
}
