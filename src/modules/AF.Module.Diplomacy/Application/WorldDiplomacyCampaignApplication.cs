namespace AnimusForge;

internal interface IWorldDiplomacyCampaignSource
{
    bool IsEnabled { get; }
    int CurrentDay { get; }
    bool DisabledStateApplied { get; set; }
    bool NativeQueueSanitized { get; set; }
    int LastSchedulerDay { get; set; }
    void RemoveQueuedNativeDiplomacyDecisions();
    void ClearDailyCaches();
    void ResetDailyGenerationBudget();
}

internal static class WorldDiplomacyCampaignApplication
{
    internal static void CampaignTick<TSource>(ref TSource source, IWorldDiplomacyOrchestration orchestration)
        where TSource : struct, IWorldDiplomacyCampaignSource
	{
		if (orchestration == null) return;
		orchestration.TryApplyInitialNewGamePeace();
		if (!source.IsEnabled)
		{
			if (!source.DisabledStateApplied) orchestration.HandleDisabledState();
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
			orchestration.RefreshPolicyDiplomacySignals();
			orchestration.ProcessRelayArrivals();
			orchestration.ProcessRoundLifecycle();
			orchestration.TrySchedulePolicyTriggeredRound();
			orchestration.TryScheduleNormalRound();
		}
	}

    internal static void DailyTick<TSource>(ref TSource source, IWorldDiplomacyOrchestration orchestration)
        where TSource : struct, IWorldDiplomacyCampaignSource
	{
		if (orchestration == null) return;
		orchestration.NormalizeStorage(allowWorldValidation: true);
		orchestration.ReconcileAllNationalPrestigeVassalRelations();
		orchestration.RetryDeferredCanonicalHistoryEntries();
		orchestration.RetryDiplomaticThreatDomesticPenalties();
		orchestration.RetryDiplomaticThreatComplianceConsequences();
		orchestration.RetryDiplomaticThreatHistoryResults();
		orchestration.RefreshRoundIntervalScheduleIfNeeded();
		source.ClearDailyCaches();
		source.ResetDailyGenerationBudget();
		orchestration.RecalculatePendingPropagationIfNeeded();
		source.LastSchedulerDay = source.CurrentDay;
		orchestration.EnsureActiveWarLedgers();
		orchestration.TrimRecentBattleFacts();
		if (!source.IsEnabled)
		{
			orchestration.AnchorInternationalReputationNaturalChangeDays();
			if (!source.DisabledStateApplied) orchestration.HandleDisabledState();
			return;
		}
		source.DisabledStateApplied = false;
		source.RemoveQueuedNativeDiplomacyDecisions();
		source.NativeQueueSanitized = true;
		orchestration.ProcessInternationalReputationNaturalChange();
		orchestration.DecayWarPressure();
		orchestration.RefreshPolicyDiplomacySignals();
		orchestration.RetryDeferredDocumentPropagation();
		orchestration.ProcessPropagationArrivals();
		orchestration.ProcessRelayArrivals();
		orchestration.RetryDeferredRoundProgress();
		orchestration.ProcessRoundLifecycle();
		orchestration.TryScheduleTokenCompression();
		orchestration.TrySchedulePolicyTriggeredRound();
		orchestration.TryScheduleNormalRound();
	}
}
