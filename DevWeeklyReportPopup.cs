using System;
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

	private readonly DevWeeklyReportPopupVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private DevWeeklyReportPopup(ScreenBase screen, string titleText, string subtitleText, string bodyText, Action onClose, string closeText)
	{
		_screen = screen;
		_onClose = onClose;
		int bodyFontSize = DuelSettings.GetSettings()?.WeeklyReportPopupBodyFontSize ?? 18;
		_dataSource = new DevWeeklyReportPopupVM(titleText, subtitleText, bodyText, bodyFontSize, HandleCloseRequested, closeText);
		_layer = new GauntletLayer("DevWeeklyReportPopup", 4000, false);
	}

	public static bool Show(string titleText, string subtitleText, string bodyText, Action onClose = null, string closeText = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			DevWeeklyReportPopup devWeeklyReportPopup = new DevWeeklyReportPopup(topScreen, titleText, subtitleText, bodyText, onClose, closeText);
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

	private void Open()
	{
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

	private void HandleCloseRequested()
	{
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
		try
		{
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
