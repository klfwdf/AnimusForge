using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class DevWeeklyReportPopup
{
	private enum PendingCloseAction
	{
		None,
		Close
	}

	private static DevWeeklyReportPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly DevWeeklyReportPopupVM _dataSource;

	private readonly Action _onClose;

	private readonly Action _onMinimumDwellMet;

	// The reading timer is advanced on return so encyclopedia browsing never counts as report-reading dwell time.
	private DateTime _openedAtUtc;

	private readonly double _minimumDwellSeconds;

	private PendingCloseAction _pendingCloseAction;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private bool _minimumDwellCallbackInvoked;

	// The report stays allocated during encyclopedia navigation, but its modal layer must be inactive underneath the native page.
	private bool _isSuspendedForEncyclopediaNavigation;
	private DateTime _suspendedAtUtc;
	// The same Escape that closes the encyclopedia must not close the restored report on that frame.
	private long _resumeInputGuardUntilUtcTicks;

	private DevWeeklyReportPopup(ScreenBase screen, string titleText, string subtitleText, string bodyText, Action onClose, string closeText, bool useChronicleColumns, bool useShortReportLayout, bool showCloseButton, double minimumDwellSeconds, Action onMinimumDwellMet)
	{
		_screen = screen;
		_onClose = onClose;
		_onMinimumDwellMet = onMinimumDwellMet;
		_minimumDwellSeconds = Math.Max(0.0, minimumDwellSeconds);
		_openedAtUtc = DateTime.UtcNow;
		int bodyFontSize = DuelSettings.GetSettings()?.WeeklyReportPopupBodyFontSize ?? 18;
		_dataSource = new DevWeeklyReportPopupVM(titleText, subtitleText, bodyText, bodyFontSize, HandleCloseRequested, HandleOpenEncyclopediaLink, closeText, useChronicleColumns, useShortReportLayout, showCloseButton);
		_layer = new GauntletLayer("DevWeeklyReportPopup", 4000, false);
	}

	public static bool Show(string titleText, string subtitleText, string bodyText, Action onClose = null, string closeText = null, bool useChronicleColumns = false, bool useShortReportLayout = false, bool showCloseButton = true, double minimumDwellSeconds = 0.0, Action onMinimumDwellMet = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			DevWeeklyReportPopup devWeeklyReportPopup = new DevWeeklyReportPopup(topScreen, titleText, subtitleText, bodyText, onClose, closeText, useChronicleColumns, useShortReportLayout, showCloseButton, minimumDwellSeconds, onMinimumDwellMet);
			devWeeklyReportPopup.Open();
			_activePopup = devWeeklyReportPopup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		DevWeeklyReportPopup popup = _activePopup;
		if (popup == null || popup._isClosed || popup._isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		popup.ProcessMinimumDwellCallbackIfNeeded();
		if (popup.ShouldCloseForEscapeKey())
		{
			popup.HandleCloseRequested();
		}
		popup.ProcessPendingCloseAction();
	}

	private void Open()
	{
		try
		{
			AnimusForgeCourierUiSprites.EnsureInstalled();
			AnimusForgeWeeklyReportUiSprites.EnsureInstalledForPopupUi();
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to install popup sprites: " + ex.Message);
		}
		_layer.LoadMovie("DevWeeklyReportPopup", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		try
		{
			_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}
		catch
		{
		}
		_screen.AddLayer(_layer);
		_layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layer);
		RegisterPauseRequest();
	}

	private void ProcessMinimumDwellCallbackIfNeeded()
	{
		if (_isSuspendedForEncyclopediaNavigation || _minimumDwellCallbackInvoked || _onMinimumDwellMet == null || _minimumDwellSeconds <= 0.0)
		{
			return;
		}
		if ((DateTime.UtcNow - _openedAtUtc).TotalSeconds < _minimumDwellSeconds)
		{
			return;
		}
		_minimumDwellCallbackInvoked = true;
		try
		{
			_onMinimumDwellMet();
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Minimum dwell callback failed: " + ex.Message);
		}
	}

	private bool ShouldCloseForEscapeKey()
	{
		if (_isSuspendedForEncyclopediaNavigation || IsResumeInputGuardActive())
		{
			return false;
		}
		try
		{
			return _layer?.Input != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape));
		}
		catch
		{
		}
		try
		{
			return Input.IsKeyReleased(InputKey.Escape);
		}
		catch
		{
			return false;
		}
	}

	private void HandleCloseRequested()
	{
		RequestDeferredClose();
	}

	private void HandleOpenEncyclopediaLink(string link)
	{
		if (!_isClosed)
		{
			EncyclopediaEntityLinkNavigationCoordinator.Request(link, SuspendForEncyclopediaNavigation, ResumeAfterEncyclopediaNavigation);
		}
	}

	private void SuspendForEncyclopediaNavigation()
	{
		if (_isClosed || _isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		try
		{
			// Preserve the report VM, its selected layout, and the pause request without granting dwell credit while hidden.
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			ScreenManager.SetSuspendLayer(_layer, isSuspended: true);
			_isSuspendedForEncyclopediaNavigation = true;
			_suspendedAtUtc = DateTime.UtcNow;
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to suspend popup for encyclopedia: " + ex.Message);
		}
	}

	private void ResumeAfterEncyclopediaNavigation()
	{
		if (_isClosed || !_isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		if (!ReferenceEquals(ScreenManager.TopScreen, _screen))
		{
			// A changed game screen invalidates the original modal; close it silently instead of restoring across states.
			Close(silent: true);
			return;
		}
		try
		{
			DateTime restoredAtUtc = DateTime.UtcNow;
			if (_suspendedAtUtc != default(DateTime))
			{
				// Shift the baseline by the hidden duration so a long encyclopedia visit cannot satisfy reading XP dwell time.
				_openedAtUtc = _openedAtUtc.Add(restoredAtUtc - _suspendedAtUtc);
			}
			_suspendedAtUtc = default(DateTime);
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			_layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_layer);
			_isSuspendedForEncyclopediaNavigation = false;
			_resumeInputGuardUntilUtcTicks = restoredAtUtc.AddMilliseconds(350.0).Ticks;
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to restore popup after encyclopedia: " + ex.Message);
			Close(silent: true);
		}
	}

	private bool IsResumeInputGuardActive()
	{
		return _resumeInputGuardUntilUtcTicks > DateTime.UtcNow.Ticks;
	}

	private void RequestDeferredClose()
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = PendingCloseAction.Close;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = PendingCloseAction.None;
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		// Do not retain a suspension marker once the report has been permanently finalized.
		_isSuspendedForEncyclopediaNavigation = false;
		_suspendedAtUtc = default(DateTime);
		try
		{
			// Release the modal input mask before opening the lower-priority encyclopedia layer.
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
			}
		}
		UnregisterPauseRequest();
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}

	private void RegisterPauseRequest()
	{
		if (_pauseRequestRegistered)
		{
			return;
		}
		try
		{
			GameStateManager gameStateManager = Game.Current?.GameStateManager;
			if (gameStateManager != null)
			{
				gameStateManager.RegisterActiveStateDisableRequest(this);
				_pauseRequestRegistered = true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to register pause request: " + ex.Message);
		}
	}

	private void UnregisterPauseRequest()
	{
		if (!_pauseRequestRegistered)
		{
			return;
		}
		try
		{
			Game.Current?.GameStateManager?.UnregisterActiveStateDisableRequest(this);
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
