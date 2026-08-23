using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class CourierLetterReplyPopup
{
	private enum PendingCloseAction
	{
		None,
		Close,
		Reply
	}

	private static CourierLetterReplyPopup _activePopup;

	private readonly ScreenBase _screen;
	private readonly GauntletLayer _layer;
	private readonly CourierLetterReplyPopupVM _dataSource;
	private readonly Action _onClose;
	private readonly Action _onReply;
	private PendingCloseAction _pendingCloseAction;
	private bool _isClosed;
	private bool _pauseRequestRegistered;
	// Encyclopedia navigation keeps this popup alive, but its high-priority layer must be inactive while the native page is open.
	private bool _isSuspendedForEncyclopediaNavigation;
	// Swallow the Escape release that closed the encyclopedia instead of immediately closing the restored letter.
	private long _resumeInputGuardUntilUtcTicks;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private CourierLetterReplyPopup(ScreenBase screen, string titleText, string subtitleText, string bodyText, Action onClose, string closeText, Action onReply, string replyText, string impactText)
	{
		_screen = screen;
		_onClose = onClose;
		_onReply = onReply;
		_dataSource = new CourierLetterReplyPopupVM(titleText, subtitleText, bodyText, 22, HandleCloseRequested, closeText, onReply == null ? null : HandleReplyRequested, replyText, impactText, HandleOpenEncyclopediaLink);
		_layer = new GauntletLayer("CourierLetterReplyPopup", 4100, false);
	}

	public static bool Show(string senderName, string bodyText, Action onClose = null)
	{
		string name = string.IsNullOrWhiteSpace(senderName) ? "NPC" : senderName.Trim();
		return Show("信使带回了回信", name + "写道：", bodyText, onClose, "");
	}

	public static bool Show(string titleText, string subtitleText, string bodyText, Action onClose = null, string closeText = null)
	{
		return ShowInternal(titleText, subtitleText, bodyText, onClose, closeText, null, null, null);
	}

	public static bool ShowWithReply(string titleText, string subtitleText, string bodyText, Action onReply, string replyText = null, Action onClose = null, string closeText = null, string impactText = null)
	{
		return ShowInternal(titleText, subtitleText, bodyText, onClose, closeText, onReply, replyText, impactText);
	}

	private static bool ShowInternal(string titleText, string subtitleText, string bodyText, Action onClose, string closeText, Action onReply, string replyText, string impactText)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			_activePopup?.Close(silent: true);
			CourierLetterReplyPopup popup = new CourierLetterReplyPopup(topScreen, titleText, subtitleText, bodyText, onClose, closeText, onReply, replyText, impactText);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CourierLetterReplyPopup", "[ERROR] Failed to open popup: " + ex);
			_activePopup?.Close(silent: true);
			_activePopup = null;
			return false;
		}
	}

	public static void ProcessDeferredCloseIfNeeded()
	{
		CourierLetterReplyPopup popup = _activePopup;
		if (popup == null || popup._isClosed || popup._isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		if (popup.ShouldCloseForEscapeKey())
		{
			popup.HandleCloseRequested();
		}
		popup.ProcessPendingCloseAction();
	}

	private void Open()
	{
		AnimusForgeCourierUiSprites.EnsureInstalled();
		_layer.LoadMovie("CourierLetterReplyPopup", _dataSource);
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
		if (_isSuspendedForEncyclopediaNavigation || IsResumeInputGuardActive())
		{
			return false;
		}
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
		RequestDeferredClose(PendingCloseAction.Close);
	}

	private void HandleReplyRequested()
	{
		RequestDeferredClose(PendingCloseAction.Reply);
	}

	private void HandleOpenEncyclopediaLink(string link)
	{
		if (!_isClosed)
		{
			EncyclopediaEntityLinkNavigationCoordinator.Request(link, SuspendForEncyclopediaNavigation, ResumeAfterEncyclopediaNavigation);
		}
	}

	private void SuspendForEncyclopediaNavigation()
	{
		if (_isClosed || _isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		try
		{
			// Keep the loaded VM and pause request intact so the exact letter can be restored without replaying callbacks.
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			ScreenManager.SetSuspendLayer(_layer, isSuspended: true);
			_isSuspendedForEncyclopediaNavigation = true;
		}
		catch (Exception ex)
		{
			Logger.Log("CourierLetterReplyPopup", "[WARN] Failed to suspend popup for encyclopedia: " + ex.Message);
		}
	}

	private void ResumeAfterEncyclopediaNavigation()
	{
		if (_isClosed || !_isSuspendedForEncyclopediaNavigation)
		{
			return;
		}
		if (!ReferenceEquals(ScreenManager.TopScreen, _screen))
		{
			// Never revive a modal across a changed game screen; regular cleanup still releases its pause request.
			Close(silent: true);
			return;
		}
		try
		{
			// Official layer suspension preserves the movie, view-model, and visual scroll state across the encyclopedia.
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			_layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_layer);
			_isSuspendedForEncyclopediaNavigation = false;
			_resumeInputGuardUntilUtcTicks = DateTime.UtcNow.AddMilliseconds(350.0).Ticks;
		}
		catch (Exception ex)
		{
			Logger.Log("CourierLetterReplyPopup", "[WARN] Failed to restore popup after encyclopedia: " + ex.Message);
			Close(silent: true);
		}
	}

	private bool IsResumeInputGuardActive()
	{
		return _resumeInputGuardUntilUtcTicks > DateTime.UtcNow.Ticks;
	}

	private void RequestDeferredClose(PendingCloseAction closeAction)
	{
		if (_isClosed || _pendingCloseAction != PendingCloseAction.None)
		{
			return;
		}
		_pendingCloseAction = closeAction;
	}

	private void ProcessPendingCloseAction()
	{
		if (_isClosed || _pendingCloseAction == PendingCloseAction.None)
		{
			return;
		}
		PendingCloseAction action = _pendingCloseAction;
		_pendingCloseAction = PendingCloseAction.None;
		Close(silent: true);
		if (action == PendingCloseAction.Reply)
		{
			_onReply?.Invoke();
		}
		else
		{
			_onClose?.Invoke();
		}
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		// A permanently closed popup must never remain marked as an encyclopedia return target.
		_isSuspendedForEncyclopediaNavigation = false;
		try
		{
			// Release this modal input mask before the encyclopedia layer becomes interactive.
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
				Logger.Log("CourierLetterReplyPopup", "[WARN] Failed to remove popup layer: " + ex.Message);
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
			Logger.Log("CourierLetterReplyPopup", "[WARN] Failed to register pause request: " + ex.Message);
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
			Logger.Log("CourierLetterReplyPopup", "[WARN] Failed to unregister pause request: " + ex.Message);
		}
		_pauseRequestRegistered = false;
	}
}
