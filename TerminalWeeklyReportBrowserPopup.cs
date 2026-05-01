using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class TerminalWeeklyReportBrowserPopup
{
	private static TerminalWeeklyReportBrowserPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly TerminalWeeklyReportBrowserPopupVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private TerminalWeeklyReportBrowserPopup(ScreenBase screen, List<MyBehavior.WeeklyReportBrowserCountryData> countries, string selectedCountryId, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new TerminalWeeklyReportBrowserPopupVM(countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>(), selectedCountryId, HandleCloseRequested);
		_layer = new GauntletLayer("TerminalWeeklyReportBrowserPopup", 4000, false);
	}

	public static bool Show(List<MyBehavior.WeeklyReportBrowserCountryData> countries, Action onClose = null, string selectedCountryId = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			TerminalWeeklyReportBrowserPopup terminalWeeklyReportBrowserPopup = new TerminalWeeklyReportBrowserPopup(topScreen, countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>(), selectedCountryId, onClose);
			terminalWeeklyReportBrowserPopup.Open();
			_activePopup = terminalWeeklyReportBrowserPopup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TerminalWeeklyReports", "[ERROR] Failed to open browser popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("TerminalWeeklyReportBrowserPopup", _dataSource);
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
				Logger.Log("TerminalWeeklyReports", "[WARN] Failed to remove browser popup layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
