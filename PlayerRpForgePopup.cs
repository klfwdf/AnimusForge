using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using System.Threading;

namespace AnimusForge;

public sealed class PlayerRpForgePopup
{
	private enum PendingAction
	{
		None,
		Submit,
		Cancel
	}

	private static PlayerRpForgePopup _activePopup;

	private static string _draftItemName = "";

	private static int _draftInvestmentDenars;

	private static bool _draftForgeAsWeapon;

	private static long _nextSessionId;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly PlayerRpForgePopupVM _dataSource;

	private readonly Action<long, string, int, bool> _onSubmit;

	private readonly Action<long> _onCancelRequested;

	private readonly Action<long> _onCancel;

	private readonly long _sessionId;

	private PendingAction _pendingAction;

	private string _pendingItemName;

	private int _pendingInvestmentDenars;

	private bool _pendingForgeAsWeapon;

	private bool _isClosed;

	private bool _pauseRequestRegistered;

	private bool _cancelRequestNotified;

	private DateTime _ignoreEscapeUntilUtc;

	private DateTime _ignoreSubmitUntilUtc;

	private DateTime _pendingActionNotBeforeUtc;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private PlayerRpForgePopup(
		ScreenBase screen,
		int maxInvestmentDenars,
		Action<long, string, int, bool> onSubmit,
		Action<long> onCancelRequested,
		Action<long> onCancel)
	{
		_screen = screen;
		_onSubmit = onSubmit;
		_onCancelRequested = onCancelRequested;
		_onCancel = onCancel;
		_sessionId = Interlocked.Increment(ref _nextSessionId);
		_dataSource = new PlayerRpForgePopupVM(
			_draftItemName,
			_draftInvestmentDenars,
			_draftForgeAsWeapon,
			maxInvestmentDenars,
			HandleSubmitRequested,
			HandleCancelRequested);
		_layer = new GauntletLayer("PlayerRpForgePopup", 4100, false);
		DateTime now = DateTime.UtcNow;
		_ignoreEscapeUntilUtc = now.AddMilliseconds(300d);
		_ignoreSubmitUntilUtc = now.AddMilliseconds(GetInteractionGuardMilliseconds());
	}

	public static bool Show(
		int maxInvestmentDenars,
		Action<long, string, int, bool> onSubmit,
		Action<long> onCancelRequested,
		Action<long> onCancel)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		PlayerRpForgePopup popup = null;
		try
		{
			_activePopup?.Close(silent: true);
			popup = new PlayerRpForgePopup(
				topScreen,
				Math.Max(0, maxInvestmentDenars),
				onSubmit,
				onCancelRequested,
				onCancel);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerRpForgePopup", "[ERROR] Failed to open popup: " + ex);
			popup?.Close(silent: true);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ClearDraft()
	{
		_activePopup?.Close(silent: true);
		_draftItemName = "";
		_draftInvestmentDenars = 0;
		_draftForgeAsWeapon = false;
	}

	public static bool TrySetBusyStatus(long sessionId, string statusText)
	{
		PlayerRpForgePopup popup = _activePopup;
		if (popup == null
			|| popup._isClosed
			|| popup._sessionId != sessionId)
		{
			return false;
		}
		popup._dataSource?.SetBusy(true, statusText);
		return true;
	}

	public static bool TryRestoreEditing(long sessionId)
	{
		PlayerRpForgePopup popup = _activePopup;
		if (popup == null
			|| popup._isClosed
			|| popup._sessionId != sessionId)
		{
			return false;
		}
		popup._ignoreSubmitUntilUtc = DateTime.UtcNow.AddMilliseconds(
			GetInteractionGuardMilliseconds());
		popup._dataSource?.SetBusy(false, "");
		return true;
	}

	public static bool TryCloseForPreview(long sessionId)
	{
		PlayerRpForgePopup popup = _activePopup;
		if (popup == null
			|| popup._isClosed
			|| popup._sessionId != sessionId)
		{
			return false;
		}
		popup.Close(silent: true);
		return true;
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		PlayerRpForgePopup popup = _activePopup;
		if (popup == null || popup._isClosed)
		{
			return;
		}
		if (popup.ShouldCancelForEscapeKey())
		{
			popup.HandleCancelRequested();
		}
		popup.ProcessPendingAction();
	}

	private void Open()
	{
		AnimusForgePlayerRpForgeUiSprites.EnsureInstalled();
		_layer.LoadMovie("PlayerRpForgePopup", _dataSource);
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
		if (DateTime.UtcNow < _ignoreEscapeUntilUtc)
		{
			return false;
		}
		try
		{
			return _layer?.Input != null
				&& (_layer.Input.IsHotKeyReleased("Exit")
					|| _layer.Input.IsKeyReleased(InputKey.Escape));
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

	private void HandleSubmitRequested(string itemName, int investmentDenars, bool forgeAsWeapon)
	{
		if (_isClosed
			|| _pendingAction != PendingAction.None
			|| _dataSource?.IsBusy == true
			|| DateTime.UtcNow < _ignoreSubmitUntilUtc)
		{
			return;
		}
		_pendingItemName = itemName ?? "";
		_pendingInvestmentDenars = Math.Max(0, investmentDenars);
		_pendingForgeAsWeapon = forgeAsWeapon;
		_pendingAction = PendingAction.Submit;
		_dataSource?.SetBusy(
			true,
			"正在确认输入并整理安全模板候选……（Esc 可取消）");
		// Keep the original popup alive through the platform double-click window and
		// through the Enter key release, so neither input can confirm the next inquiry.
		_pendingActionNotBeforeUtc = DateTime.UtcNow.AddMilliseconds(
			GetInteractionGuardMilliseconds());
	}

	private void HandleCancelRequested()
	{
		if (_isClosed || _pendingAction == PendingAction.Cancel)
		{
			return;
		}
		if (!_cancelRequestNotified)
		{
			_cancelRequestNotified = true;
			try
			{
				_onCancelRequested?.Invoke(_sessionId);
			}
			catch (Exception ex)
			{
				Logger.Log(
					"PlayerRpForgePopup",
					"[WARN] Early cancel callback failed: " + ex.Message);
			}
		}
		// Escape may arrive while Submit is still waiting for the double-click
		// release guard. Cancellation must replace that pending submit.
		_pendingAction = PendingAction.Cancel;
		_pendingActionNotBeforeUtc = DateTime.UtcNow.AddMilliseconds(180d);
	}

	private void ProcessPendingAction()
	{
		if (_isClosed || _pendingAction == PendingAction.None)
		{
			return;
		}
		if (DateTime.UtcNow < _pendingActionNotBeforeUtc || HasPendingTriggerInput())
		{
			return;
		}
		PendingAction action = _pendingAction;
		string itemName = _pendingItemName ?? "";
		int investmentDenars = _pendingInvestmentDenars;
		bool forgeAsWeapon = _pendingForgeAsWeapon;
		_pendingAction = PendingAction.None;
		_pendingItemName = null;
		_pendingInvestmentDenars = 0;
		_pendingForgeAsWeapon = false;
		CaptureDraft();
		if (action == PendingAction.Submit)
		{
			_dataSource?.SetBusy(
				true,
				"正在整理 Top 50 模板并调用前处理 AI……（Esc 可取消）");
			try
			{
				_onSubmit?.Invoke(
					_sessionId,
					itemName,
					investmentDenars,
					forgeAsWeapon);
			}
			catch (Exception ex)
			{
				_dataSource?.SetBusy(false, "");
				Logger.Log(
					"PlayerRpForgePopup",
					"[ERROR] Submit callback failed: " + ex);
			}
		}
		else
		{
			Close(silent: true);
			try
			{
				_onCancel?.Invoke(_sessionId);
			}
			catch (Exception ex)
			{
				Logger.Log(
					"PlayerRpForgePopup",
					"[WARN] Cancel callback failed: " + ex.Message);
			}
		}
	}

	private bool HasPendingTriggerInput()
	{
		try
		{
			return IsTriggerInputActive(_layer?.Input);
		}
		catch
		{
		}
		try
		{
			return Input.IsKeyDown(InputKey.Enter)
				|| Input.IsKeyReleased(InputKey.Enter)
				|| Input.IsKeyDown(InputKey.NumpadEnter)
				|| Input.IsKeyReleased(InputKey.NumpadEnter)
				|| Input.IsKeyDown(InputKey.LeftMouseButton)
				|| Input.IsKeyReleased(InputKey.LeftMouseButton)
				|| Input.IsKeyDown(InputKey.Escape)
				|| Input.IsKeyReleased(InputKey.Escape);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsTriggerInputActive(InputContext input)
	{
		if (input == null)
		{
			return false;
		}
		return input.IsKeyDown(InputKey.Enter)
			|| input.IsKeyReleased(InputKey.Enter)
			|| input.IsKeyDown(InputKey.NumpadEnter)
			|| input.IsKeyReleased(InputKey.NumpadEnter)
			|| input.IsKeyDown(InputKey.LeftMouseButton)
			|| input.IsKeyReleased(InputKey.LeftMouseButton)
			|| input.IsKeyDown(InputKey.Escape)
			|| input.IsKeyReleased(InputKey.Escape);
	}

	private static int GetInteractionGuardMilliseconds()
	{
		try
		{
			return Math.Max(
				550,
				System.Windows.Forms.SystemInformation.DoubleClickTime + 100);
		}
		catch
		{
			return 650;
		}
	}

	private void CaptureDraft()
	{
		if (_dataSource == null)
		{
			return;
		}
		_draftItemName = _dataSource.ItemName ?? "";
		_draftInvestmentDenars = Math.Max(0, _dataSource.InvestmentDenars);
		_draftForgeAsWeapon = _dataSource.ForgeAsWeapon;
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		CaptureDraft();
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
				Logger.Log("PlayerRpForgePopup", "[WARN] Failed to remove popup layer: " + ex.Message);
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
			Logger.Log("PlayerRpForgePopup", "[WARN] Failed to register pause request: " + ex.Message);
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
			Logger.Log("PlayerRpForgePopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
