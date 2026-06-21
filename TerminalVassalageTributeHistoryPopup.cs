using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class TerminalVassalageTributeHistoryPopup
{
	private static TerminalVassalageTributeHistoryPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly TerminalVassalageTributeHistoryPopupVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private TerminalVassalageTributeHistoryPopup(ScreenBase screen, TerminalTributaryPaymentHistoryData data, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new TerminalVassalageTributeHistoryPopupVM(data, HandleCloseRequested);
		_layer = new GauntletLayer("TerminalVassalageTributeHistoryPopup", 4100, false);
	}

	public static bool Show(TerminalTributaryPaymentHistoryData data, Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			TerminalVassalageTributeHistoryPopup popup = new TerminalVassalageTributeHistoryPopup(topScreen, data ?? new TerminalTributaryPaymentHistoryData(), onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("TerminalVassalageTributeHistory", "[ERROR] Failed to open tribute history popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("TerminalVassalageTributeHistoryPopup", _dataSource);
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
				Logger.Log("TerminalVassalageTributeHistory", "[WARN] Failed to remove tribute history popup layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
