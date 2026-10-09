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
		// Resolve only the named/captured parent at open time; never scan layers per tick.
        int order = inputOwner == null ? 4000 : Math.Max(4000, inputOwner.InputRestrictions.Order + 1);
        _layer = new GauntletLayer("DevHistoryEditPopup", order, false);
	}

	public static bool Show(string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText = null, string saveText = null, string cancelText = null)
	{
        // Replacement must release the previous editor before capturing its restored parent.
        _activePopup?.Close(silent: true);
        ScreenBase screen = ScreenManager.TopScreen;
        ScreenLayer parent = CaptureInputOwner(screen);
        return TryShowOwned(titleText, dateText, originalContentText, editedText, onSave, onCancel,
            inputHintText, saveText, cancelText, parent, null, null, out _);
    }

    internal static ScreenLayer CaptureInputOwner(ScreenBase screen)
    {
        // Mouse commands name their hit layer; keyboard/MCM commands name their focused layer.
        // Global inquiry layers are not owned by this screen and must keep their own lifecycle.
        ScreenLayer hit = ScreenManager.FirstHitLayer;
        ScreenLayer focused = ScreenManager.FocusedLayer;
        bool hitValid = IsUsableInputOwner(screen, hit);
        bool focusValid = IsUsableInputOwner(screen, focused);
        if (hitValid && (!focusValid || hit.InputRestrictions.Order >= focused.InputRestrictions.Order)) return hit;
        return focusValid ? focused : null;
    }

    private static bool IsUsableInputOwner(ScreenBase screen, ScreenLayer layer) =>
        screen != null && !screen.IsFinalized && layer != null && !layer.IsFinalized && layer.IsActive
        && screen.HasLayer(layer);

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
        ScreenManager.OnPushScreen += OnScreenChanged;
        ScreenManager.OnPopScreen += OnScreenChanged;
        _screenEventsRegistered = true;
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
        if (ReferenceEquals(screen, _screen) || !ReferenceEquals(ScreenManager.TopScreen, _screen))
            Close(silent: true); // A screen change must not reopen a business menu on another screen.
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
