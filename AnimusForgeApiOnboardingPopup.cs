using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeApiOnboardingPopup
{
	private enum PendingAction
	{
		None,
		Completed,
		Cancelled
	}

	private static AnimusForgeApiOnboardingPopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly AnimusForgeApiOnboardingVM _dataSource;
	private readonly Action _onCompleted;
	private readonly Action _onCancelled;

	private bool _isClosed;
	private bool _pauseRequestRegistered;
	private PendingAction _pendingAction = PendingAction.None;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;
	public static AnimusForgeApiOnboardingPopup ActivePopup => _activePopup;

	private AnimusForgeApiOnboardingPopup(ScreenBase screen, bool isApiOnlyFlow, Action onCompleted, Action onCancelled)
	{
		_screen = screen;
		_onCompleted = onCompleted;
		_onCancelled = onCancelled;
		_dataSource = new AnimusForgeApiOnboardingVM(isApiOnlyFlow, HandleCompletedRequested, HandleCancelledRequested);
		_layer = new GauntletLayer("AnimusForgeApiOnboardingPopup", 4000, false);
	}

	public static bool Show(bool isApiOnlyFlow, Action onCompleted, Action onCancelled)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}

		try
		{
			_activePopup?.Close(silent: true);
			AnimusForgeApiOnboardingPopup popup = new AnimusForgeApiOnboardingPopup(topScreen, isApiOnlyFlow, onCompleted, onCancelled);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ApiOnboardingPopup", "[ERROR] Failed to open API onboarding popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void CloseActive(bool silent = true)
	{
		_activePopup?.Close(silent);
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		if (_activePopup == null || _activePopup._isClosed)
		{
			return;
		}

		_activePopup._dataSource?.OnTick();

		if (_activePopup._pendingAction == PendingAction.None)
		{
			return;
		}

		PendingAction action = _activePopup._pendingAction;
		_activePopup._pendingAction = PendingAction.None;

		Action onCompleted = _activePopup._onCompleted;
		Action onCancelled = _activePopup._onCancelled;

		_activePopup.Close(silent: true);
		_activePopup = null;

		if (action == PendingAction.Completed)
		{
			try
			{
				onCompleted?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("ApiOnboardingPopup", "[ERROR] Exception in onCompleted: " + ex);
			}
		}
		else if (action == PendingAction.Cancelled)
		{
			try
			{
				onCancelled?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("ApiOnboardingPopup", "[ERROR] Exception in onCancelled: " + ex);
			}
		}
	}

	private void Open()
	{
		_layer.LoadMovie("AnimusForgeApiOnboardingPopup", _dataSource);
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

		try
		{
			Game.Current?.GameStateManager?.RegisterActiveStateDisableRequest(this);
			_pauseRequestRegistered = true;
		}
		catch
		{
		}
	}

	private void HandleCompletedRequested()
	{
		_pendingAction = PendingAction.Completed;
	}

	private void HandleCancelledRequested()
	{
		_pendingAction = PendingAction.Cancelled;
	}

	public void Close(bool silent)
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
		catch
		{
		}

		try
		{
			_dataSource?.OnFinalize();
		}
		catch
		{
		}

		if (_pauseRequestRegistered)
		{
			_pauseRequestRegistered = false;
			try
			{
				Game.Current?.GameStateManager?.UnregisterActiveStateDisableRequest(this);
			}
			catch
			{
			}
		}

		if (!silent)
		{
			_onCancelled?.Invoke();
		}
	}
}
