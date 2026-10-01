using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class DevHistoryEditPopup
{
	private static DevHistoryEditPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly DevHistoryEditPopupVM _dataSource;

	private readonly Action<string> _onSave;

	private readonly Action _onCancel;

	private bool _isClosed;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private DevHistoryEditPopup(ScreenBase screen, string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText, string saveText, string cancelText)
	{
		_screen = screen;
		_onSave = onSave;
		_onCancel = onCancel;
		_dataSource = new DevHistoryEditPopupVM(titleText, dateText, originalContentText, editedText, HandleSaveRequested, HandleCancelRequested, inputHintText, saveText, cancelText);
		_layer = new GauntletLayer("DevHistoryEditPopup", 4000, false);
	}

	public static bool Show(string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText = null, string saveText = null, string cancelText = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			DevHistoryEditPopup devHistoryEditPopup = new DevHistoryEditPopup(topScreen, titleText, dateText, originalContentText, editedText, onSave, onCancel, inputHintText, saveText, cancelText);
			devHistoryEditPopup.Open();
			_activePopup = devHistoryEditPopup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DevHistoryPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	private void Open()
	{
		_layer.LoadMovie("DevHistoryEditPopup", _dataSource);
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

	private void HandleSaveRequested(string editedText)
	{
		Close(silent: true);
		_onSave?.Invoke(editedText ?? "");
	}

	private void HandleCancelRequested()
	{
		Close(silent: true);
		_onCancel?.Invoke();
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
				Logger.Log("DevHistoryPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
