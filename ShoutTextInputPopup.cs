using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class ShoutTextInputPopup
{
	private enum PendingCloseAction
	{
		None,
		Submit,
		Cancel
	}

	private static ShoutTextInputPopup _activePopup;

	private static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	private static readonly FieldInfo _screenLayersField = typeof(ScreenBase).GetField("_layers", BindingFlags.Instance | BindingFlags.NonPublic);

	private const double FocusGuardGraceSeconds = 0.35;

	private const double RestoreInputGuardGraceSeconds = 0.35;

	private const int VirtualKeyEscape = 0x1B;

	private const string EncyclopediaLayerName = "EncyclopediaBar";

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private GauntletMovieIdentifier _movieIdentifier;

	private readonly ShoutTextInputPopupVM _dataSource;

	private readonly Action<string> _onSubmit;

	private readonly Action _onCancel;

	private readonly Action _onTitleLink;

	private readonly long _openedUtcTicks;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private bool _rawEscapeWasDown;

	private bool _allowTemporaryScreenSwitch;

	private bool _isHiddenForTemporaryScreenSwitch;

	private bool _temporaryScreenSwitchObserved;

	private long _restoredFromTemporarySwitchUtcTicks;

	private PendingCloseAction _pendingCloseAction;

	private string _pendingSubmitText;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private ShoutTextInputPopup(ScreenBase screen, string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel, Action onTitleLink)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_onTitleLink = onTitleLink;
		_openedUtcTicks = DateTime.UtcNow.Ticks;
		_dataSource = new ShoutTextInputPopupVM(titleText, subtitleText, inputHintText, initialText, HandleSubmitRequested, HandleCancelRequested, onTitleLink != null ? HandleTitleLinkRequested : (Action)null);
		_layer = new GauntletLayer("ShoutTextInputPopup", 1000, false);
	}

	public static bool Show(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel, Action onTitleLink = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			ShoutTextInputPopup popup = new ShoutTextInputPopup(topScreen, titleText, subtitleText, inputHintText, initialText, onSubmit, onCancel, onTitleLink);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutTextInputPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static bool CancelActiveForEscapeMenu()
	{
		return CancelActiveForSystemMenu();
	}

	public static bool CancelActiveForSystemMenu()
	{
		ShoutTextInputPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return false;
		}
		popup.HandleCancelRequested();
		return true;
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		ShoutTextInputPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		popup.ProcessPendingCloseAction();
	}

	public static void CloseForSystemInterruptionIfNeeded()
	{
		ShoutTextInputPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCancelForEscapeKey())
		{
			popup.HandleCancelRequested();
			return;
		}
		if (popup.ShouldCancelForSystemInterruption())
		{
			popup.HandleCancelRequested();
		}
	}

	public static void KeepMissionPausedIfOpen()
	{
		ShoutTextInputPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCancelForEscapeKey())
		{
			popup.HandleCancelRequested();
			return;
		}
		if (popup.ShouldCancelForSystemInterruption())
		{
			popup.HandleCancelRequested();
			return;
		}
		PauseCurrentMission();
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int virtualKey);

	private static bool IsGameWindowFocused()
	{
		try
		{
			IntPtr foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return true;
			}
			GetWindowThreadProcessId(foregroundWindow, out uint processId);
			if (processId == 0)
			{
				return true;
			}
			return processId == _currentProcessId;
		}
		catch
		{
			return true;
		}
	}

	private bool IsInFocusGuardGrace()
	{
		try
		{
			return (DateTime.UtcNow - new DateTime(_openedUtcTicks, DateTimeKind.Utc)).TotalSeconds < FocusGuardGraceSeconds;
		}
		catch
		{
			return false;
		}
	}

	private bool ShouldCancelForEscapeKey()
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return false;
		}
		if (_allowTemporaryScreenSwitch)
		{
			return false;
		}
		if (IsInRestoreInputGuardGrace())
		{
			return false;
		}
		try
		{
			if (_layer?.Input != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape)))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Input.IsKeyPressed(InputKey.Escape) || Input.IsKeyReleased(InputKey.Escape))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			bool rawEscapeDown = IsRawEscapeDown();
			bool shouldCancel = rawEscapeDown && !_rawEscapeWasDown;
			_rawEscapeWasDown = rawEscapeDown;
			return shouldCancel;
		}
		catch
		{
			return false;
		}
	}

	private bool IsInRestoreInputGuardGrace()
	{
		if (_restoredFromTemporarySwitchUtcTicks <= 0L)
		{
			return false;
		}
		try
		{
			return (DateTime.UtcNow - new DateTime(_restoredFromTemporarySwitchUtcTicks, DateTimeKind.Utc)).TotalSeconds < RestoreInputGuardGraceSeconds;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRawEscapeDown()
	{
		if (!IsGameWindowFocused())
		{
			return false;
		}
		return (GetAsyncKeyState(VirtualKeyEscape) & unchecked((short)0x8000)) != 0;
	}

	private bool ShouldCancelForSystemInterruption()
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return false;
		}
		if (!IsGameWindowFocused())
		{
			return true;
		}
		try
		{
			ScreenBase topScreen = ScreenManager.TopScreen;
			if (_allowTemporaryScreenSwitch)
			{
				if (IsEncyclopediaOpenForTemporarySwitch(topScreen))
				{
					_temporaryScreenSwitchObserved = true;
					if (!_isHiddenForTemporaryScreenSwitch)
					{
						HidePopupForTemporaryScreenSwitch();
					}
					return false;
				}
				if (!ReferenceEquals(topScreen, _screen))
				{
					_temporaryScreenSwitchObserved = true;
					return false;
				}
				if (_temporaryScreenSwitchObserved)
				{
					_allowTemporaryScreenSwitch = false;
					RestorePopupAfterTemporaryScreenSwitch();
				}
				return false;
			}
			if (!ReferenceEquals(topScreen, _screen))
			{
				return true;
			}
		}
		catch
		{
		}
		if (IsInFocusGuardGrace())
		{
			return false;
		}
		try
		{
			ScreenLayer focusedLayer = ScreenManager.FocusedLayer;
			if (focusedLayer != null && !ReferenceEquals(focusedLayer, _layer))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool IsWaitingBehindTemporaryScreen()
	{
		try
		{
			return _allowTemporaryScreenSwitch && !ReferenceEquals(ScreenManager.TopScreen, _screen);
		}
		catch
		{
			return false;
		}
	}

	private bool IsEncyclopediaOpenForTemporarySwitch(ScreenBase topScreen)
	{
		return IsMapEncyclopediaOpen(topScreen) || HasLayerNamed(topScreen, EncyclopediaLayerName) || HasLayerNamed(_screen, EncyclopediaLayerName);
	}

	private static bool IsMapEncyclopediaOpen(ScreenBase screen)
	{
		try
		{
			MapScreen mapScreen = screen as MapScreen;
			return mapScreen?.EncyclopediaScreenManager?.IsEncyclopediaOpen == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasLayerNamed(ScreenBase screen, string layerName)
	{
		if (screen == null || string.IsNullOrEmpty(layerName))
		{
			return false;
		}
		try
		{
			if (_screenLayersField?.GetValue(screen) is not IEnumerable layers)
			{
				return false;
			}
			foreach (object item in layers)
			{
				if (item is ScreenLayer layer && string.Equals(layer.Name, layerName, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void RestorePopupFocus()
	{
		try
		{
			_layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_layer);
		}
		catch
		{
		}
	}

	private void HidePopupForTemporaryScreenSwitch()
	{
		if (_isHiddenForTemporaryScreenSwitch)
		{
			return;
		}
		_isHiddenForTemporaryScreenSwitch = true;
		_temporaryScreenSwitchObserved = false;
		try
		{
			_movieIdentifier?.Movie?.RootWidget?.Hide();
		}
		catch
		{
		}
		try
		{
			_layer.TwoDimensionView.SetEnable(false);
			_layer.TwoDimensionView.Clear();
		}
		catch
		{
		}
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
		}
		catch
		{
		}
		try
		{
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
	}

	private void RestorePopupAfterTemporaryScreenSwitch()
	{
		try
		{
			_layer.TwoDimensionView.SetEnable(true);
			_movieIdentifier?.Movie?.RootWidget?.Show();
		}
		catch
		{
		}
		try
		{
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		}
		catch
		{
		}
		_isHiddenForTemporaryScreenSwitch = false;
		_temporaryScreenSwitchObserved = false;
		_restoredFromTemporarySwitchUtcTicks = DateTime.UtcNow.Ticks;
		_rawEscapeWasDown = IsRawEscapeDown();
		RestorePopupFocus();
	}

	private static void PauseCurrentMission()
	{
		try
		{
			if (Mission.Current != null)
			{
				Mission.Current.Scene.TimeSpeed = 0f;
			}
		}
		catch
		{
		}
	}

	private void Open()
	{
		PauseCurrentMission();
		_movieIdentifier = _layer.LoadMovie("ShoutTextInputPopup", _dataSource);
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
		PauseCurrentMission();
	}

	private void HandleSubmitRequested(string inputText)
	{
		RequestDeferredClose(PendingCloseAction.Submit, inputText ?? "");
	}

	private void HandleCancelRequested()
	{
		RequestDeferredClose(PendingCloseAction.Cancel, null);
	}

	private void HandleTitleLinkRequested()
	{
		if (_isClosed || _onTitleLink == null)
		{
			return;
		}
		_allowTemporaryScreenSwitch = true;
		HidePopupForTemporaryScreenSwitch();
		try
		{
			_onTitleLink.Invoke();
		}
		catch (Exception ex)
		{
			_allowTemporaryScreenSwitch = false;
			RestorePopupAfterTemporaryScreenSwitch();
			Logger.Log("ShoutTextInputPopup", "[WARN] Failed to open title link: " + ex.Message);
		}
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
				Logger.Log("ShoutTextInputPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
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
			Logger.Log("ShoutTextInputPopup", "[WARN] Failed to register pause request: " + ex.Message);
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
			Logger.Log("ShoutTextInputPopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
