using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class DevHistoryEditPopup : IDisposable
{
	private static DevHistoryEditPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly DevHistoryEditPopupVM _dataSource;

	private readonly Action<string> _onSave;

	private readonly Action _onCancel;

	private bool _isClosed;
    private readonly ScreenLayer _inputOwner;
    private readonly Func<bool> _isInputOwnerAlive;
    private readonly Action _onDismissed;
    private DevPopupInputLease _parentInput;
    private bool _screenEventsRegistered;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private DevHistoryEditPopup(ScreenBase screen, string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText, string saveText, string cancelText, ScreenLayer inputOwner, Func<bool> isInputOwnerAlive, Action onDismissed)
	{
		_screen = screen;
		_onSave = onSave;
		_onCancel = onCancel;
        _inputOwner = inputOwner;
        _isInputOwnerAlive = isInputOwnerAlive;
        _onDismissed = onDismissed;
		_dataSource = new DevHistoryEditPopupVM(titleText, dateText, originalContentText, editedText, HandleSaveRequested, HandleCancelRequested, inputHintText, saveText, cancelText);
		// Owned nested editors sit just above their parent; ordinary history editors retain their original order.
        int order = inputOwner == null ? 4000 : Math.Max(4000, inputOwner.InputRestrictions.Order + 1);
        _layer = new GauntletLayer("DevHistoryEditPopup", order, false);
	}

	public static bool Show(string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText = null, string saveText = null, string cancelText = null)
	{
        return TryShowOwned(titleText, dateText, originalContentText, editedText, onSave, onCancel,
            inputHintText, saveText, cancelText, null, null, null, out _);
    }

    internal static bool TryShowOwned(string titleText, string dateText, string originalContentText, string editedText,
        Action<string> onSave, Action onCancel, string inputHintText, string saveText, string cancelText,
        ScreenLayer inputOwner, Func<bool> isInputOwnerAlive, Action onDismissed, out IDisposable session)
    {
        session = null;
        ScreenBase topScreen = ScreenManager.TopScreen;
        if (topScreen == null || topScreen.IsFinalized) return false;
        if (inputOwner != null && (inputOwner.IsFinalized || !inputOwner.IsActive
            || !topScreen.HasLayer(inputOwner) || isInputOwnerAlive?.Invoke() == false)) return false;
        DevHistoryEditPopup popup = null;
        try
        {
            _activePopup?.Close(silent: true);
            popup = new DevHistoryEditPopup(topScreen, titleText, dateText, originalContentText, editedText,
                onSave, onCancel, inputHintText, saveText, cancelText, inputOwner, isInputOwnerAlive, onDismissed);
            _activePopup = popup;
            popup.Open();
            session = popup;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("DevHistoryPopup", "[ERROR] Failed to open popup: " + ex);
            popup?.Close(silent: true);
            return false;
        }
    }

	private void Open()
	{
        _parentInput = new DevPopupInputLease(_screen, _inputOwner, _isInputOwnerAlive);
        if (_onDismissed != null)
        {
            ScreenManager.OnPushScreen += OnScreenChanged;
            ScreenManager.OnPopScreen += OnScreenChanged;
            _screenEventsRegistered = true;
        }
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
		if (_isClosed) return;
        Close(silent: true, notifyDismissed: false);
		_onSave?.Invoke(editedText ?? "");
	}

	private void HandleCancelRequested()
	{
		if (_isClosed) return;
        Close(silent: true, notifyDismissed: false);
		_onCancel?.Invoke();
	}

	public void Dispose() => HandleCancelRequested();

    private void OnScreenChanged(ScreenBase screen)
    {
        if (ReferenceEquals(screen, _screen) || !ReferenceEquals(ScreenManager.TopScreen, _screen)) Dispose();
    }

	private void Close(bool silent, bool notifyDismissed = true)
	{
		if (_isClosed)
		{
			return;
		}
        _isClosed = true;
        if (_screenEventsRegistered)
        {
            ScreenManager.OnPushScreen -= OnScreenChanged;
            ScreenManager.OnPopScreen -= OnScreenChanged;
            _screenEventsRegistered = false;
        }
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
				Logger.Log("DevHistoryPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
			}
		}
        try { _dataSource?.OnFinalize(); } catch { }
        try { _parentInput?.Dispose(); } catch (Exception ex) { Logger.Log("DevHistoryPopup", "[WARN] Parent input restore failed: " + ex.Message); }
        _parentInput = null;
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
        if (notifyDismissed) _onDismissed?.Invoke();
	}
}
