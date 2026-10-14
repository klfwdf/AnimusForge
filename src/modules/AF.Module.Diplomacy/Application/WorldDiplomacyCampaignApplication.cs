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
		using var dailyScope = FreezeWatchdog.Scope("WorldDiplomacy.DailyTick");
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.NormalizeStorage")) orchestration.NormalizeStorage(allowWorldValidation: true);
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.ReconcileAllNationalPrestigeVassalRelations")) orchestration.ReconcileAllNationalPrestigeVassalRelations();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDeferredCanonicalHistoryEntries")) orchestration.RetryDeferredCanonicalHistoryEntries();
        using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.MaintainDialogueAndMemory")) (orchestration as WorldDiplomacyOrchestration)?.MaintainDialogueAndMemory();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDiplomaticThreatDomesticPenalties")) orchestration.RetryDiplomaticThreatDomesticPenalties();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDiplomaticThreatComplianceConsequences")) orchestration.RetryDiplomaticThreatComplianceConsequences();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDiplomaticThreatHistoryResults")) orchestration.RetryDiplomaticThreatHistoryResults();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RefreshRoundIntervalScheduleIfNeeded")) orchestration.RefreshRoundIntervalScheduleIfNeeded();
		source.ClearDailyCaches();
		source.ResetDailyGenerationBudget();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RecalculatePendingPropagationIfNeeded")) orchestration.RecalculatePendingPropagationIfNeeded();
		source.LastSchedulerDay = source.CurrentDay;
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.EnsureActiveWarLedgers")) orchestration.EnsureActiveWarLedgers();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.TrimRecentBattleFacts")) orchestration.TrimRecentBattleFacts();
		if (!source.IsEnabled)
		{
			using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.AnchorInternationalReputationNaturalChangeDays")) orchestration.AnchorInternationalReputationNaturalChangeDays();
			if (!source.DisabledStateApplied) orchestration.HandleDisabledState();
			return;
		}
		source.DisabledStateApplied = false;
		source.RemoveQueuedNativeDiplomacyDecisions();
		source.NativeQueueSanitized = true;
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.ProcessInternationalReputationNaturalChange")) orchestration.ProcessInternationalReputationNaturalChange();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.DecayWarPressure")) orchestration.DecayWarPressure();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RefreshPolicyDiplomacySignals")) orchestration.RefreshPolicyDiplomacySignals();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDeferredDocumentPropagation")) orchestration.RetryDeferredDocumentPropagation();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.ProcessPropagationArrivals")) orchestration.ProcessPropagationArrivals();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.ProcessRelayArrivals")) orchestration.ProcessRelayArrivals();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.RetryDeferredRoundProgress")) orchestration.RetryDeferredRoundProgress();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.ProcessRoundLifecycle")) orchestration.ProcessRoundLifecycle();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.TryScheduleTokenCompression")) orchestration.TryScheduleTokenCompression();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.TrySchedulePolicyTriggeredRound")) orchestration.TrySchedulePolicyTriggeredRound();
		using (FreezeWatchdog.Scope("WorldDiplomacy.DailyTick.TryScheduleNormalRound")) orchestration.TryScheduleNormalRound();
	}
}
