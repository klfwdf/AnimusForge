namespace AnimusForge;

internal interface IWorldDiplomacyCampaignSource
{
    bool IsEnabled { get; }
    int CurrentDay { get; }
    bool DisabledStateApplied { get; set; }
    bool NativeQueueSanitized { get; set; }
    int LastSchedulerDay { get; set; }
    void TryApplyInitialNewGamePeace();
    void HandleDisabledState();
    void RemoveQueuedNativeDiplomacyDecisions();
    void RefreshPolicyDiplomacySignals();
    void ProcessRelayArrivals();
    void ProcessRoundLifecycle();
    void TrySchedulePolicyTriggeredRound();
    void TryScheduleNormalRound();
    void ReconcileAllNationalPrestigeVassalRelations();
    void RetryDeferredCanonicalHistoryEntries();
    void RetryDiplomaticThreatDomesticPenalties();
    void RetryDiplomaticThreatComplianceConsequences();
    void RetryDiplomaticThreatHistoryResults();
    void RefreshRoundIntervalScheduleIfNeeded();
    void RecalculatePendingPropagationIfNeeded();
    void EnsureActiveWarLedgersAndRemoveEndedWars();
    void TrimRecentBattleFacts();
    void AnchorInternationalReputationNaturalChangeDays();
    void ProcessInternationalReputationNaturalChange();
    void RetryDeferredDocumentPropagation();
    void ProcessPropagationArrivals();
    void TryScheduleTokenCompression();
    void NormalizeStorage();
    void ClearDailyCaches();
    void ResetDailyGenerationBudget();
    void DecayWarPressure();
    void RetryDeferredRoundProgress();
}

internal static class WorldDiplomacyCampaignApplication
{
    internal static void CampaignTick<TSource>(ref TSource source) where TSource : struct, IWorldDiplomacyCampaignSource
	{
		source.TryApplyInitialNewGamePeace();
		if (!source.IsEnabled)
		{
			if (!source.DisabledStateApplied) source.HandleDisabledState();
			return;
		}
		source.DisabledStateApplied = false;
		if (!source.NativeQueueSanitized)
		{
			source.RemoveQueuedNativeDiplomacyDecisions();
			source.NativeQueueSanitized = true;
		}
		int day = source.CurrentDay;
		if (source.LastSchedulerDay != day)
		{
			source.LastSchedulerDay = day;
			source.RefreshPolicyDiplomacySignals();
			source.ProcessRelayArrivals();
			source.ProcessRoundLifecycle();
			source.TrySchedulePolicyTriggeredRound();
			source.TryScheduleNormalRound();
		}
	}

    internal static void DailyTick<TSource>(ref TSource source) where TSource : struct, IWorldDiplomacyCampaignSource
	{
		source.NormalizeStorage();
		source.ReconcileAllNationalPrestigeVassalRelations();
		source.RetryDeferredCanonicalHistoryEntries();
		source.RetryDiplomaticThreatDomesticPenalties();
		source.RetryDiplomaticThreatComplianceConsequences();
		source.RetryDiplomaticThreatHistoryResults();
		source.RefreshRoundIntervalScheduleIfNeeded();
		source.ClearDailyCaches();
		source.ResetDailyGenerationBudget();
		source.RecalculatePendingPropagationIfNeeded();
		source.LastSchedulerDay = source.CurrentDay;
		source.EnsureActiveWarLedgersAndRemoveEndedWars();
		source.TrimRecentBattleFacts();
		if (!source.IsEnabled)
		{
			source.AnchorInternationalReputationNaturalChangeDays();
			if (!source.DisabledStateApplied) source.HandleDisabledState();
			return;
		}
		source.DisabledStateApplied = false;
		source.RemoveQueuedNativeDiplomacyDecisions();
		source.NativeQueueSanitized = true;
		source.ProcessInternationalReputationNaturalChange();
		source.DecayWarPressure();
		source.RefreshPolicyDiplomacySignals();
		source.RetryDeferredDocumentPropagation();
		source.ProcessPropagationArrivals();
		source.ProcessRelayArrivals();
		source.RetryDeferredRoundProgress();
		source.ProcessRoundLifecycle();
		source.TryScheduleTokenCompression();
		source.TrySchedulePolicyTriggeredRound();
		source.TryScheduleNormalRound();
	}
}
