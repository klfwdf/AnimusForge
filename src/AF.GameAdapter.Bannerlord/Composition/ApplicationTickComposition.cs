using System;
using AnimusForge.PolicyEffects;

namespace AnimusForge;

// Ordered game-tick dispatch; fast path has no per-frame phase list or delegate allocation.
internal static class ApplicationTickComposition
{
	internal static void Run(SubModule host, float dt)
	{
		long perfFrame = PerfProbe.BeginFrame(dt);
		FreezeWatchdog.BeginFrame(dt);
		CampaignTickDiagnosticsPatch.RefreshCheckpointWriteBudget();
		try
		{
			if (!FreezeWatchdog.IsScopeRecordingActive() && !PerfProbe.IsDetailedScopeRecordingActive())
			{
				RunFastApplicationTickPhases(host);
			}
			else
			{
				RunWatchedApplicationTickPhases(host);
			}
			host.TickWarStatsMapButton(dt);
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark("SubModule.OnApplicationTick.exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			throw;

		}
		finally
		{
			using (FreezeWatchdog.Scope("SubModule.PerfProbe.EndFrame"))
			{
				PerfProbe.EndFrame(perfFrame, "SubModule.OnApplicationTick.total");
			}
			FreezeWatchdog.EndFrame();
		}
	}

	private static void RunFastApplicationTickPhases(SubModule host)
	{
		ShoutTextInputPopup.ProcessDeferredCloseIfNeeded();
		ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded();
		ShoutTextInputPopup.KeepMissionPausedIfOpen();
		DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded();
		// RichText handlers queue layer changes so encyclopedia navigation never mutates layers during widget event dispatch.
		EncyclopediaEntityLinkNavigationCoordinator.ProcessPending();
		PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded();
		PlayerRpForgePopup.ProcessDeferredCloseIfNeeded();
		AnimusForgeApiOnboardingPopup.ProcessDeferredCloseIfNeeded();
		PolicyEffectModuleManagerPopup.ProcessDeferredCloseIfNeeded();
		AnimusForgeConversationHistoryLogPopup.OnApplicationTick();
		AnimusForgeNativeConversationOverlay.OnApplicationTick();
		AiErrorAnalysisInquiry.OnApplicationTick();
		ShoutBehavior.OnApplicationTickForMainThreadActionsExternal();
		NativeConversationAnswerAreaController.OnApplicationTick();
		ShoutBehavior.OnApplicationTickForNativeConversationTtsExternal();
		ConversationHelper.Tick();
		host.ProcessPendingInitialApiGuideNotice();
		Logger.OnApplicationTick();
		BannerlordExceptionSentinel.OnApplicationTick();
		McmDropdownRuntimeRefresh.OnApplicationTick();
		EncyclopediaHeroPersonaPatch.OnApplicationTick();
		EncyclopediaTownRuleMemoryPatch.OnApplicationTick();
		SiegeAiInterventionBehavior.OnEngineTickForExternal();
		ModOnboardingBehavior.Instance?.OnEngineTick();
		MyBehavior.Instance?.OnEngineTick();
		CourierDeliveryBehavior.Instance?.OnEngineTick();
		DuelBehavior.Instance?.OnEngineTick();
		RewardSystemBehavior.Instance?.OnEngineTick();
		LordEncounterBehavior.OnEngineTick();
		AnimusForgeTerminalBehavior.Instance?.OnEngineTick();
		CustomPolicyBehavior.Instance?.OnEngineTick();
		NpcRulerPolicyBehavior.Instance?.OnEngineTick();
		WorldDiplomacyBehavior.Instance?.OnEngineTick();
		PolicySystemUi.OnApplicationTick();
		NobleGatheringBehavior.Instance?.OnEngineTick();
		VassalageBehavior.Instance?.OnEngineTick();
	}

	private static void RunWatchedApplicationTickPhases(SubModule host)
	{
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.ProcessDeferredCloseIfNeeded", () => ShoutTextInputPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded", () => ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded());
			RunWatchedTickPhase("SubModule.ShoutTextInputPopup.KeepMissionPausedIfOpen", () => ShoutTextInputPopup.KeepMissionPausedIfOpen());
			RunWatchedTickPhase("SubModule.DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded", () => DevWeeklyReportPopup.ProcessDeferredCloseIfNeeded());
			// Preserve the same next-frame RichText navigation ordering while detailed performance scopes are enabled.
			RunWatchedTickPhase("SubModule.EncyclopediaEntityLinkNavigationCoordinator.ProcessPending", () => EncyclopediaEntityLinkNavigationCoordinator.ProcessPending());
			RunWatchedTickPhase("SubModule.PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded", () => PlayerNotorietyPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.PlayerRpForgePopup.ProcessDeferredCloseIfNeeded", () => PlayerRpForgePopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.AnimusForgeApiOnboardingPopup.ProcessDeferredCloseIfNeeded", () => AnimusForgeApiOnboardingPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.PolicyEffectModuleManagerPopup.ProcessDeferredCloseIfNeeded", () => PolicyEffectModuleManagerPopup.ProcessDeferredCloseIfNeeded());
			RunWatchedTickPhase("SubModule.AnimusForgeConversationHistoryLogPopup.OnApplicationTick", () => AnimusForgeConversationHistoryLogPopup.OnApplicationTick());
			RunWatchedTickPhase("SubModule.AnimusForgeNativeConversationOverlay.OnApplicationTick", () => AnimusForgeNativeConversationOverlay.OnApplicationTick());
			RunWatchedTickPhase("SubModule.AiErrorAnalysisInquiry.OnApplicationTick", () => AiErrorAnalysisInquiry.OnApplicationTick());
			RunWatchedTickPhase("SubModule.ShoutBehavior.MainThreadActions.OnApplicationTick", () => ShoutBehavior.OnApplicationTickForMainThreadActionsExternal());
			RunWatchedTickPhase("SubModule.NativeConversationAnswerAreaController.OnApplicationTick", () => NativeConversationAnswerAreaController.OnApplicationTick());
			RunWatchedTickPhase("SubModule.ShoutBehavior.NativeConversationTts.OnApplicationTick", () => ShoutBehavior.OnApplicationTickForNativeConversationTtsExternal());
			RunWatchedTickPhase("SubModule.ConversationHelper.Tick", () => ConversationHelper.Tick());
			RunWatchedTickPhase("SubModule.ProcessPendingInitialApiGuideNotice", host.ProcessPendingInitialApiGuideNotice);
			RunWatchedTickPhase("SubModule.Logger.OnApplicationTick", () => Logger.OnApplicationTick());
			RunWatchedTickPhase("SubModule.BannerlordExceptionSentinel.OnApplicationTick", () => BannerlordExceptionSentinel.OnApplicationTick());
			RunWatchedTickPhase("SubModule.McmDropdownRuntimeRefresh.OnApplicationTick", () => McmDropdownRuntimeRefresh.OnApplicationTick());
			RunWatchedTickPhase("SubModule.EncyclopediaHeroPersonaPatch.OnApplicationTick", () => EncyclopediaHeroPersonaPatch.OnApplicationTick());
			RunWatchedTickPhase("SubModule.EncyclopediaTownRuleMemoryPatch.OnApplicationTick", () => EncyclopediaTownRuleMemoryPatch.OnApplicationTick());
			RunWatchedTickPhase("SubModule.SiegeAiInterventionBehavior.OnEngineTick", () => SiegeAiInterventionBehavior.OnEngineTickForExternal());
			RunWatchedTickPhase("SubModule.ModOnboardingBehavior.OnEngineTick", () => ModOnboardingBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.MyBehavior.OnEngineTick", () => MyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.CourierDeliveryBehavior.OnEngineTick", () => CourierDeliveryBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.DuelBehavior.OnEngineTick", () => DuelBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.RewardSystemBehavior.OnEngineTick", () => RewardSystemBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.LordEncounterBehavior.OnEngineTick", () => LordEncounterBehavior.OnEngineTick());
			RunWatchedTickPhase("SubModule.AnimusForgeTerminalBehavior.OnEngineTick", () => AnimusForgeTerminalBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.CustomPolicyBehavior.OnEngineTick", () => CustomPolicyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.NpcRulerPolicyBehavior.OnEngineTick", () => NpcRulerPolicyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.WorldDiplomacyBehavior.OnEngineTick", () => WorldDiplomacyBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.PolicySystemUi.OnApplicationTick", () => PolicySystemUi.OnApplicationTick());
			RunWatchedTickPhase("SubModule.NobleGatheringBehavior.OnEngineTick", () => NobleGatheringBehavior.Instance?.OnEngineTick());
			RunWatchedTickPhase("SubModule.VassalageBehavior.OnEngineTick", () => VassalageBehavior.Instance?.OnEngineTick());
	}

	private static void RunWatchedTickPhase(string name, Action action)
	{
		if (!FreezeWatchdog.IsScopeRecordingActive() && !PerfProbe.IsDetailedScopeRecordingActive())
		{
			action?.Invoke();
			return;
		}
		using (FreezeWatchdog.Scope(name))
		using (PerfProbe.Scope(name))
		{
			action?.Invoke();
		}
	}
}
