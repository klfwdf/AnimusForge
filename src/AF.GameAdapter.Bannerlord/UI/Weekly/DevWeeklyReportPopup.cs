using System;
using AnimusForge.Refactor.Modules;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class DevWeeklyReportPopup
{
	private static DevWeeklyReportPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;
    private DevPopupInputLease _illustrationInput;

	private readonly ViewModel _dataSource;

	private readonly string _movieName = "DevWeeklyReportPopup";

	private WorldBulletinPanelVM _bulletinPanel;

	private readonly Action _onClose;

	private readonly Action _onMinimumDwellMet;

	private readonly WeeklyReportPopupSessionOwner _session;

	private bool _pauseRequestRegistered;

	private bool _illustrationPromptEditing;

	private DevWeeklyReportPopup(ScreenBase screen, string titleText, string subtitleText, string bodyText, Action onClose, string closeText, bool useChronicleColumns, bool useShortReportLayout, bool showCloseButton, double minimumDwellSeconds, Action onMinimumDwellMet)
	{
		_screen = screen;
		_onClose = onClose;
		_onMinimumDwellMet = onMinimumDwellMet;
		_session = new WeeklyReportPopupSessionOwner(DateTime.UtcNow, minimumDwellSeconds);
		int bodyFontSize = DuelSettings.GetSettings()?.WeeklyReportPopupBodyFontSize ?? 18;
		_dataSource = new DevWeeklyReportPopupVM(titleText, subtitleText, bodyText, bodyFontSize, HandleCloseRequested, HandleOpenEncyclopediaLink, closeText, useChronicleColumns, useShortReportLayout, showCloseButton);
		_layer = new GauntletLayer("DevWeeklyReportPopup", 4000, false);
	}

	private DevWeeklyReportPopup(ScreenBase screen, WorldBulletinPanelData bulletin, double minimumDwellSeconds, Action onMinimumDwellMet)
	{
		_screen = screen;
		_onMinimumDwellMet = onMinimumDwellMet;
		_session = new WeeklyReportPopupSessionOwner(DateTime.UtcNow, minimumDwellSeconds);
		int bodyFontSize = DuelSettings.GetSettings()?.WeeklyReportPopupBodyFontSize ?? 18;
		_bulletinPanel = new WorldBulletinPanelVM(bulletin, bodyFontSize, HandleCloseRequested, HandleOpenEncyclopediaLink);
		_dataSource = _bulletinPanel;
		_movieName = "WorldBulletinPanel";
		_layer = new GauntletLayer("DevWeeklyReportPopup", 4000, false);
		_bulletinPanel.Illustration.SetPromptEditing = SetIllustrationPromptEditing;
	}

	private void SetIllustrationPromptEditing(bool editing)
	{
        if (editing && (_session.IsClosed || _session.IsSuspended)) return;
        _illustrationPromptEditing = editing;
        if (editing)
            _illustrationInput ??= new DevPopupInputLease(_screen, _layer, () => !_session.IsClosed && !_session.IsSuspended);
        else
        {
            var lease = _illustrationInput;
            _illustrationInput = null;
            lease?.Dispose();
        }
        if (!_session.IsClosed && !_session.IsSuspended && _layer?.UIContext?.Root != null)
            _layer.UIContext.Root.IsVisible = !editing;
	}

	// Separate name from Show so name-based Harmony hooks on Show keep resolving to the weekly-report overload only.
	internal static bool ShowWorldBulletin(WorldBulletinPanelData bulletin, double minimumDwellSeconds = 0.0, Action onMinimumDwellMet = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null || bulletin == null)
		{
			return false;
		}
		DevWeeklyReportPopup popup = null;
		try
		{
			_activePopup?.Close(silent: true);
			popup = new DevWeeklyReportPopup(topScreen, bulletin, minimumDwellSeconds, onMinimumDwellMet);
			popup.Open();
			_activePopup = popup;
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[ERROR] Failed to open world bulletin panel: " + ex);
			popup?.Close(silent: true);
			return false;
		}
		try
		{
			var attach = WorldBulletinPanelIllustrationBridge.AttachSlot;
			if (attach != null)
			{
				attach(popup._bulletinPanel.Illustration, bulletin.EventId ?? "", bulletin.HeadlineText ?? "", bulletin.IllustrationSubtitle ?? "", bulletin.IllustrationBody ?? "", bulletin.IllustrationPlan);
			}
		}
		catch (Exception ex)
		{
			// Illustration is decorative; the panel stays open with the column collapsed.
			Logger.Log("DevWeeklyReportPopup", "[WARN] World bulletin illustration attach failed: " + ex.Message);
		}
		return true;
	}

	public static bool Show(string titleText, string subtitleText, string bodyText, Action onClose = null, string closeText = null, bool useChronicleColumns = false, bool useShortReportLayout = false, bool showCloseButton = true, double minimumDwellSeconds = 0.0, Action onMinimumDwellMet = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		DevWeeklyReportPopup devWeeklyReportPopup = null;
		try
		{
			_activePopup?.Close(silent: true);
			devWeeklyReportPopup = new DevWeeklyReportPopup(topScreen, titleText, subtitleText, bodyText, onClose, closeText, useChronicleColumns, useShortReportLayout, showCloseButton, minimumDwellSeconds, onMinimumDwellMet);
			devWeeklyReportPopup.Open();
			_activePopup = devWeeklyReportPopup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[ERROR] Failed to open popup: " + ex);
			devWeeklyReportPopup?.Close(silent: true);
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		DevWeeklyReportPopup popup = _activePopup;
		if (popup == null || !popup._session.CanProcessTick)
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
			if (_bulletinPanel != null)
			{
				AnimusForgeWeeklyReportUiSprites.EnsureInstalledForWorldBulletinUi();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to install popup sprites: " + ex.Message);
		}
		_layer.LoadMovie(_movieName, _dataSource);
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
		if (!_session.TryClaimMinimumDwell(DateTime.UtcNow, _onMinimumDwellMet != null))
		{
			return;
		}
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
		if (_illustrationPromptEditing) return false;
		if (!_session.CanHandleEscape(DateTime.UtcNow))
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
		if (!_session.IsClosed)
		{
			EncyclopediaEntityLinkNavigationCoordinator.Request(link, SuspendForEncyclopediaNavigation, ResumeAfterEncyclopediaNavigation);
		}
	}

	private void SuspendForEncyclopediaNavigation()
	{
		if (_session.IsClosed || _session.IsSuspended)
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
			_session.Suspend(DateTime.UtcNow);
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to suspend popup for encyclopedia: " + ex.Message);
		}
	}

	private void ResumeAfterEncyclopediaNavigation()
	{
		if (_session.IsClosed || !_session.IsSuspended)
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
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			_layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_layer);
			_session.Resume(restoredAtUtc);
		}
		catch (Exception ex)
		{
			Logger.Log("DevWeeklyReportPopup", "[WARN] Failed to restore popup after encyclopedia: " + ex.Message);
			Close(silent: true);
		}
	}

	private void RequestDeferredClose()
	{
		_session.RequestClose();
	}

	private void ProcessPendingCloseAction()
	{
		if (!_session.TryTakePendingClose())
		{
			return;
		}
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void Close(bool silent)
	{
		if (!_session.Close())
		{
			return;
		}
        try { _illustrationInput?.Dispose(); } catch (Exception ex) { Logger.Log("DevWeeklyReportPopup", "[WARN] Illustration input cleanup failed: " + ex.Message); } finally { _illustrationInput = null; }
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
		try
		{
			_dataSource?.OnFinalize();
		}
		catch
		{
		}
		finally
		{
			if (ReferenceEquals(_activePopup, this))
			{
				_activePopup = null;
			}
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
