using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class PlayerNotorietyPopup
{
	private enum PendingAction
	{
		None,
		Close,
		Edit
	}

	private static PlayerNotorietyPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly PlayerNotorietyPopupVM _dataSource;

	private readonly Action _onEdit;

	private readonly Func<PlayerNotorietyPopupData> _onToggleLowProfile;

	private PendingAction _pendingAction;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private PlayerNotorietyPopup(ScreenBase screen, PlayerNotorietyPopupData data, Action onEdit, Func<PlayerNotorietyPopupData> onToggleLowProfile)
	{
		_screen = screen;
		_onEdit = onEdit;
		_onToggleLowProfile = onToggleLowProfile;
		_dataSource = new PlayerNotorietyPopupVM(data, HandleCloseRequested, HandleEditRequested, HandleToggleLowProfileRequested);
		_layer = new GauntletLayer("PlayerNotorietyPopup", 4000, false);
	}

	public static bool Show(PlayerNotorietyPopupData data, Action onEdit, Func<PlayerNotorietyPopupData> onToggleLowProfile = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			PlayerNotorietyPopup popup = new PlayerNotorietyPopup(topScreen, data, onEdit, onToggleLowProfile);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotorietyPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		PlayerNotorietyPopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCloseForEscapeKey())
		{
			popup.HandleCloseRequested();
		}
		popup.ProcessPendingAction();
	}

	private void Open()
	{
		try
		{
			AnimusForgePlayerNotorietyUiSprites.EnsureInstalled();
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotorietyPopup", "[WARN] Failed to install player notoriety sprites: " + ex.Message);
		}
		_layer.LoadMovie("PlayerNotorietyPopup", _dataSource);
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

	private bool ShouldCloseForEscapeKey()
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

	private void HandleCloseRequested()
	{
		RequestPendingAction(PendingAction.Close);
	}

	private void HandleEditRequested()
	{
		RequestPendingAction(PendingAction.Edit);
	}

	private void HandleToggleLowProfileRequested()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			PlayerNotorietyPopupData data = _onToggleLowProfile?.Invoke();
			if (data != null)
			{
				_dataSource.ApplyData(data);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotorietyPopup", "[WARN] Failed to toggle low profile mode: " + ex.Message);
		}
	}

	private void RequestPendingAction(PendingAction action)
	{
		if (_isClosed || _pendingAction != PendingAction.None)
		{
			return;
		}
		_pendingAction = action;
	}

	private void ProcessPendingAction()
	{
		if (_isClosed || _pendingAction == PendingAction.None)
		{
			return;
		}
		PendingAction action = _pendingAction;
		_pendingAction = PendingAction.None;
		Close(silent: true);
		if (action == PendingAction.Edit)
		{
			_onEdit?.Invoke();
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
				Logger.Log("PlayerNotorietyPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
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
			Logger.Log("PlayerNotorietyPopup", "[WARN] Failed to register pause request: " + ex.Message);
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
			Logger.Log("PlayerNotorietyPopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
