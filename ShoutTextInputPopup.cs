using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class ShoutTextInputPopup
{
	private static ShoutTextInputPopup _activePopup;

	private static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	private const double FocusGuardGraceSeconds = 0.35;

	private const int VirtualKeyEscape = 0x1B;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly ShoutTextInputPopupVM _dataSource;

	private readonly Action<string> _onSubmit;

	private readonly Action _onCancel;

	private readonly long _openedUtcTicks;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private bool _rawEscapeWasDown;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private ShoutTextInputPopup(ScreenBase screen, string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		_openedUtcTicks = DateTime.UtcNow.Ticks;
		_dataSource = new ShoutTextInputPopupVM(titleText, subtitleText, inputHintText, initialText, HandleSubmitRequested, HandleCancelRequested);
		_layer = new GauntletLayer("ShoutTextInputPopup", 1000, false);
	}

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
			ShoutTextInputPopup popup = new ShoutTextInputPopup(topScreen, titleText, subtitleText, inputHintText, initialText, onSubmit, onCancel);
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
		if (_isClosed)
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
		if (_isClosed)
		{
			return false;
		}
		if (!IsGameWindowFocused())
		{
			return true;
		}
		try
		{
			if (!ReferenceEquals(ScreenManager.TopScreen, _screen))
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
		_layer.LoadMovie("ShoutTextInputPopup", _dataSource);
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
		Close(silent: true);
		_onSubmit?.Invoke(inputText ?? "");
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
