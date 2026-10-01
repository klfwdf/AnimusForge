using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal enum CastleAftermathMissionEntryPumpResult
{
	Idle,
	Waiting,
	Failed
}

/// <summary>
/// Defers castle mission entry until Bannerlord has completely popped the custom PartyState.
/// This is a lifecycle-only adapter; castle selection policy and scene behavior live elsewhere.
/// </summary>
internal static class CastleAftermathMissionEntryBridge
{
	private static readonly TimeSpan PartyStateCloseTimeout = TimeSpan.FromSeconds(15.0);

	private static readonly TimeSpan MissionStartTimeout = TimeSpan.FromMinutes(2.0);

	private static readonly TimeSpan WaitLogInterval = TimeSpan.FromSeconds(10.0);

	private static Location _pendingLocation;

	private static string _pendingSettlementId = "";

	private static string _pendingSource = "";

	private static long _queuedAtUtcTicks;

	private static long _notBeforeUtcTicks;

	private static long _openRequestedAtUtcTicks;

	private static long _nextWaitLogUtcTicks;

	private static bool _openRequested;

	internal static bool IsPending => _pendingLocation != null;

	internal static void Queue(Location location, string settlementId, string source)
	{
		if (location == null)
		{
			return;
		}

		long now = DateTime.UtcNow.Ticks;
		_pendingLocation = location;
		_pendingSettlementId = settlementId?.Trim() ?? "";
		_pendingSource = string.IsNullOrWhiteSpace(source) ? "castle_roster_selection_done" : source.Trim();
		_queuedAtUtcTicks = now;
		_notBeforeUtcTicks = now + TimeSpan.FromMilliseconds(350.0).Ticks;
		_openRequestedAtUtcTicks = 0L;
		_nextWaitLogUtcTicks = 0L;
		_openRequested = false;
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenQueued source=" + _pendingSource
			+ " settlement=" + (_pendingSettlementId.Length > 0 ? _pendingSettlementId : "N/A")
			+ " activeState=" + GetActiveGameStateName());
		Logger.Log("CastleAftermath", "Queued castle mission entry after PartyState closes. Source=" + _pendingSource);
	}

	internal static CastleAftermathMissionEntryPumpResult Pump(
		Settlement settlement,
		Func<Location, string, bool> openMission)
	{
		Location location = _pendingLocation;
		if (location == null)
		{
			return CastleAftermathMissionEntryPumpResult.Idle;
		}

		try
		{
			if (Mission.Current != null)
			{
				Complete("mission_detected");
				return CastleAftermathMissionEntryPumpResult.Idle;
			}

			long now = DateTime.UtcNow.Ticks;
			string stateName = GetActiveGameStateName();
			if (!_openRequested)
			{
				bool mapStateReady = stateName.IndexOf("MapState", StringComparison.OrdinalIgnoreCase) >= 0;
				if (now < _notBeforeUtcTicks || !mapStateReady)
				{
					LogWaitingIfDue(now, "partyStateClose", stateName);
					if (now - _queuedAtUtcTicks < PartyStateCloseTimeout.Ticks)
					{
						return CastleAftermathMissionEntryPumpResult.Waiting;
					}

					return Fail("party_state_close_timeout activeState=" + stateName);
				}

				if (!CanOpen(settlement, location) || openMission == null)
				{
					return Fail("mission_context_unavailable activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
				}

				string source = _pendingSource + "_deferred_after_party_state";
				GcczDiagnosticLog.Log("CastleMissionEntry", "directOpenAfterPartyState activeState=" + stateName
					+ " source=" + source + " location=" + (location.StringId ?? "N/A")
					+ " currentMenu=" + GetCurrentGameMenuId());
				if (!openMission(location, source))
				{
					return Fail("direct_mission_open_rejected activeState=" + stateName);
				}

				if (!IsPending)
				{
					return CastleAftermathMissionEntryPumpResult.Idle;
				}

				_openRequested = true;
				_openRequestedAtUtcTicks = now;
				_nextWaitLogUtcTicks = now + WaitLogInterval.Ticks;
				return CastleAftermathMissionEntryPumpResult.Waiting;
			}

			LogWaitingIfDue(now, "missionStart", stateName);
			if (now - _openRequestedAtUtcTicks < MissionStartTimeout.Ticks)
			{
				return CastleAftermathMissionEntryPumpResult.Waiting;
			}

			return Fail("mission_start_timeout activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
		}
		catch (Exception ex)
		{
			return Fail("exception=" + ex);
		}
	}

	internal static void Complete(string source)
	{
		if (!IsPending)
		{
			return;
		}

		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenCompleted source=" + (source ?? "N/A")
			+ " elapsedMs=" + ElapsedMilliseconds(_queuedAtUtcTicks)
			+ " location=" + (_pendingLocation?.StringId ?? "N/A"));
		ClearPending();
	}

	internal static void Reset(string source)
	{
		if (!IsPending)
		{
			return;
		}

		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenReset source=" + (source ?? "N/A")
			+ " elapsedMs=" + ElapsedMilliseconds(_queuedAtUtcTicks)
			+ " activeState=" + GetActiveGameStateName());
		ClearPending();
	}

	private static bool CanOpen(Settlement settlement, Location location)
	{
		return location != null
			&& settlement?.IsCastle == true
			&& (string.IsNullOrWhiteSpace(_pendingSettlementId)
				|| string.Equals(settlement.StringId, _pendingSettlementId, StringComparison.OrdinalIgnoreCase))
			&& PlayerEncounter.LocationEncounter != null;
	}

	private static void LogWaitingIfDue(long now, string phase, string stateName)
	{
		if (now < _nextWaitLogUtcTicks)
		{
			return;
		}

		_nextWaitLogUtcTicks = now + WaitLogInterval.Ticks;
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenWaiting phase=" + phase
			+ " elapsedMs=" + ElapsedMilliseconds(_queuedAtUtcTicks)
			+ " activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
	}

	private static CastleAftermathMissionEntryPumpResult Fail(string reason)
	{
		string detail = reason ?? "unknown";
		Logger.Log("CastleAftermath", "Deferred castle mission entry failed. " + detail);
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenFailed " + detail);
		ClearPending();
		return CastleAftermathMissionEntryPumpResult.Failed;
	}

	private static void ClearPending()
	{
		_pendingLocation = null;
		_pendingSettlementId = "";
		_pendingSource = "";
		_queuedAtUtcTicks = 0L;
		_notBeforeUtcTicks = 0L;
		_openRequestedAtUtcTicks = 0L;
		_nextWaitLogUtcTicks = 0L;
		_openRequested = false;
	}

	private static long ElapsedMilliseconds(long startedAtUtcTicks)
	{
		if (startedAtUtcTicks <= 0L)
		{
			return 0L;
		}

		return Math.Max(0L, (DateTime.UtcNow.Ticks - startedAtUtcTicks) / TimeSpan.TicksPerMillisecond);
	}

	private static string GetActiveGameStateName()
	{
		try
		{
			return Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "N/A";
		}
		catch
		{
			return "N/A";
		}
	}

	private static string GetCurrentGameMenuId()
	{
		try
		{
			return Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "N/A";
		}
		catch
		{
			return "N/A";
		}
	}
}
