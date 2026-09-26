using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	public void OnEngineTick()
	{
		LlmRetryPrompt.CaptureMainThreadContext();
		try
		{
			CourierLetterInputPopup.ProcessDeferredCloseIfNeeded();
			CourierLetterReplyPopup.ProcessDeferredCloseIfNeeded();
		}
		catch (Exception ex)
		{
			Log("courier letter popup tick failed: " + ex.Message);
		}
		while (MainThreadActions.TryDequeue(out var action))
		{
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Log("main action failed: " + ex);
			}
		}
	}

	public static void ResetTransientRuntimeForLoadedSaveExternal(string reason)
	{
		try
		{
			if (Instance != null) Instance.ResetPendingOwnerPhases();
			else while (MainThreadActions.TryDequeue(out var _)) { }
			Instance?.ResetTransientRuntimeForLoadedSave(reason);
			Log("transient runtime cleared reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Log("transient runtime clear failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void ResetTransientRuntimeForLoadedSave(string reason)
	{
		try
		{
			_pendingFlow = null;
			_npcInitiatedLetterScan = null;
			_lastCampaignTickUtcTicks = 0L;
			_courierInboundCompletionScanCursor = string.Empty;
			_courierReplyWaitTimeLocked = false;
			_courierReplyWaitPreviousMode = CampaignTimeControlMode.Stop;
			_courierReplyWaitPreviousLock = false;
			_courierLetterInventoryRestoreRetryRemaining = 0;
			_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;
			_courierPartyCache.Clear();
			_activeCourierPartyIdsSnapshot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_courierRuntimeIndexesReady = false;
			UpdateAiCourierPresenceFlag(_activeCourierPartyIdsSnapshot, _courierRuntimeIndexesReady);
		}
		catch (Exception ex)
		{
			Log("instance transient clear failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void EnqueueMainThreadActionForGeneration(long generation, Action action, string source)
	{
		MainThreadActions.Enqueue(() =>
		{
			if (SaveRuntimeGuard.IsCurrentGeneration(generation))
			{
				action?.Invoke();
				return;
			}
			SaveRuntimeGuard.IsStale(generation, "courier_main_thread:" + (source ?? ""));
		});
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("CourierDelivery.OnCampaignTick"))
		{
		try
		{
			if (_npcInitiatedLetterScan != null)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.ProcessNpcInitiatedLetterScan"))
				{
					ProcessNpcInitiatedLetterScan();
				}
			}
			long now = DateTime.UtcNow.Ticks;
			if (now - _lastCampaignTickUtcTicks < TimeSpan.FromSeconds(CampaignTickThrottleSeconds).Ticks)
			{
				return;
			}
			_lastCampaignTickUtcTicks = now;
			if (!_partyNameplatePatchApplied && !_partyNameplatePatchFailed)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.PatchPartyNameplate"))
				{
					TryPatchPartyNameplateForCourierBanner();
				}
			}
			if (!_mapTrackerProviderPatchApplied && !_mapTrackerProviderPatchFailed)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.PatchMapTrackerProvider"))
				{
					TryPatchMapTrackerProviderForCourierDiagnostics();
				}
			}
			ProcessCourierLetterInventoryRestoreRetry();
			ProcessOneCourierInboundCompletionReceipt();
			List<CourierSession> snapshot;
			using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.BuildSnapshot"))
			{
				lock (_sessionLock)
				{
					snapshot = _sessions.Values.Where(x => x != null && !IsTerminalStage(x)).ToList();
				}
			}
			foreach (CourierSession session in snapshot)
			{
				using (PerfProbe.Scope("CourierDelivery.OnCampaignTick.ProcessSession"))
				{
					ProcessSession(session);
				}
			}
		}
		catch (Exception ex)
		{
			Log("campaign tick failed: " + ex);
		}
		}
	}

	private void OnHourlyTick()
	{
		try
		{
			TryStartNpcInitiatedLetterScan();
		}
		catch (Exception ex)
		{
			Log("npc initiated letter hourly tick failed: " + ex);
		}
	}
}
