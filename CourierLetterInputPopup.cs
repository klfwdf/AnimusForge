using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class CourierLetterInputPopup
{
	private enum PendingCloseAction
	{
		None,
		Submit,
		Cancel
	}

	private static CourierLetterInputPopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly CourierLetterInputPopupVM _dataSource;
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private PendingCloseAction _pendingCloseAction;
	private string _pendingSubmitText;
	private bool _isClosed;
	private bool _pauseRequestRegistered;

	private CourierLetterInputPopup(ScreenBase screen, string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_dataSource = new CourierLetterInputPopupVM(titleText, subtitleText, inputHintText, initialText, HandleSubmitRequested, HandleCancelRequested);
		_layer = new GauntletLayer("CourierLetterInputPopup", 4100, false);
	}

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	public static bool Show(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CourierLetterInputPopup popup = new CourierLetterInputPopup(topScreen, titleText, subtitleText, inputHintText, initialText, onSubmit, onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CourierLetterInputPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		CourierLetterInputPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCancelForEscapeKey())
		{
			popup.HandleCancelRequested();
		}
		popup.ProcessPendingCloseAction();
	}

	private void Open()
	{
		AnimusForgeCourierUiSprites.EnsureInstalled();
		_layer.LoadMovie("CourierLetterInputPopup", _dataSource);
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

	private bool ShouldCancelForEscapeKey()
	{
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

	private void HandleSubmitRequested(string inputText)
	{
		RequestDeferredClose(PendingCloseAction.Submit, inputText ?? "");
	}

	private void HandleCancelRequested()
	{
		RequestDeferredClose(PendingCloseAction.Cancel, null);
	}

	private void RequestDeferredClose(PendingCloseAction closeAction, string submitText)
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = closeAction;
		_pendingSubmitText = submitText;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction closeAction = _pendingCloseAction;
		string submitText = _pendingSubmitText ?? "";
		_pendingCloseAction = PendingCloseAction.None;
		_pendingSubmitText = null;
		Close(silent: true);
		if (closeAction == PendingCloseAction.Submit)
		{
			_onSubmit?.Invoke(submitText);
		}
		else if (closeAction == PendingCloseAction.Cancel)
		{
			_onCancel?.Invoke();
		}
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
				Logger.Log("CourierLetterInputPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
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
			Logger.Log("CourierLetterInputPopup", "[WARN] Failed to register pause request: " + ex.Message);
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
			Logger.Log("CourierLetterInputPopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
