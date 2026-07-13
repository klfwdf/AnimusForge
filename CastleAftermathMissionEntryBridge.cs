using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
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
	private const int StateWaitTimeoutTicks = 120;

	private const int RoutedMissionTimeoutTicks = 90;

	private static Location _pendingLocation;

	private static string _pendingSettlementId = "";

	private static string _pendingSource = "";

	private static int _pendingTicks;

	private static bool _routedThroughCastleMenu;

	private static int _routedTicks;

	internal static bool IsPending => _pendingLocation != null;

	internal static void Queue(Location location, string settlementId, string source)
	{
		if (location == null)
		{
			return;
		}

		_pendingLocation = location;
		_pendingSettlementId = settlementId?.Trim() ?? "";
		_pendingSource = string.IsNullOrWhiteSpace(source) ? "castle_roster_selection_done" : source.Trim();
		_pendingTicks = 0;
		_routedThroughCastleMenu = false;
		_routedTicks = 0;
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenQueued source=" + _pendingSource
			+ " settlement=" + (_pendingSettlementId.Length > 0 ? _pendingSettlementId : "N/A")
			+ " activeState=" + GetActiveGameStateName());
		Logger.Log("CastleAftermath", "Queued castle mission entry after PartyState closes. Source=" + _pendingSource);
	}

	internal static CastleAftermathMissionEntryPumpResult Pump(Settlement settlement)
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
				Complete("mission_already_open");
				return CastleAftermathMissionEntryPumpResult.Idle;
			}

			_pendingTicks++;
			string stateName = GetActiveGameStateName();
			if (!_routedThroughCastleMenu)
			{
				bool mapStateReady = stateName.IndexOf("MapState", StringComparison.OrdinalIgnoreCase) >= 0;
				if (_pendingTicks <= 1 || !mapStateReady)
				{
					if (_pendingTicks == 1 || _pendingTicks % 30 == 0)
					{
						GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenWaiting ticks=" + _pendingTicks
							+ " activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
					}
					if (_pendingTicks < StateWaitTimeoutTicks)
					{
						return CastleAftermathMissionEntryPumpResult.Waiting;
					}

					return Fail("party_state_close_timeout activeState=" + stateName);
				}

				if (!CanRoute(settlement, location))
				{
					return Fail("route_context_unavailable activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
				}

				Campaign.Current.GameMenuManager.NextLocation = location;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
				_routedThroughCastleMenu = true;
				_routedTicks = 0;
				GcczDiagnosticLog.Log("CastleMissionEntry", "routeThroughCastleMenu ticks=" + _pendingTicks
					+ " activeState=" + stateName + " source=" + _pendingSource
					+ " location=" + (location.StringId ?? "N/A"));
				Logger.Log("CastleAftermath", "Routing deferred castle mission through vanilla castle menu. Source=" + _pendingSource);
				GameMenu.SwitchToMenu("castle");
				return CastleAftermathMissionEntryPumpResult.Waiting;
			}

			_routedTicks++;
			if (_routedTicks < RoutedMissionTimeoutTicks)
			{
				if (_routedTicks % 30 == 0)
				{
					GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenRoutedWaiting ticks=" + _routedTicks
						+ " activeState=" + stateName + " currentMenu=" + GetCurrentGameMenuId());
				}
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
			+ " ticks=" + _pendingTicks + " location=" + (_pendingLocation?.StringId ?? "N/A"));
		ClearPending();
	}

	internal static void Reset(string source)
	{
		if (!IsPending)
		{
			return;
		}

		if (_routedThroughCastleMenu)
		{
			ClearMenuLocationHints();
		}
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenReset source=" + (source ?? "N/A")
			+ " ticks=" + _pendingTicks + " activeState=" + GetActiveGameStateName());
		ClearPending();
	}

	private static bool CanRoute(Settlement settlement, Location location)
	{
		return location != null
			&& settlement?.IsCastle == true
			&& (string.IsNullOrWhiteSpace(_pendingSettlementId)
				|| string.Equals(settlement.StringId, _pendingSettlementId, StringComparison.OrdinalIgnoreCase))
			&& PlayerEncounter.LocationEncounter != null
			&& Campaign.Current?.GameMenuManager != null;
	}

	private static CastleAftermathMissionEntryPumpResult Fail(string reason)
	{
		string detail = reason ?? "unknown";
		Logger.Log("CastleAftermath", "Deferred castle mission entry failed. " + detail);
		GcczDiagnosticLog.Log("CastleMissionEntry", "deferredOpenFailed " + detail);
		if (_routedThroughCastleMenu)
		{
			ClearMenuLocationHints();
		}
		ClearPending();
		return CastleAftermathMissionEntryPumpResult.Failed;
	}

	private static void ClearPending()
	{
		_pendingLocation = null;
		_pendingSettlementId = "";
		_pendingSource = "";
		_pendingTicks = 0;
		_routedThroughCastleMenu = false;
		_routedTicks = 0;
	}

	private static void ClearMenuLocationHints()
	{
		try
		{
			if (Campaign.Current?.GameMenuManager != null)
			{
				Campaign.Current.GameMenuManager.NextLocation = null;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
			}
		}
		catch
		{
		}
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
