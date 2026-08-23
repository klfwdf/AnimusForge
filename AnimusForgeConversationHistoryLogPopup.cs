using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogPopup
{
	private static AnimusForgeConversationHistoryLogPopup _activePopup;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private readonly AnimusForgeConversationHistoryLogVM _dataSource;

	private readonly Action _onClose;

	private bool _isClosed;

	private bool _escapeWasDown;

	public static bool IsOpen => _activePopup != null && !_activePopup._isClosed;

	private AnimusForgeConversationHistoryLogPopup(ScreenBase screen, string targetName, Hero targetHero, CharacterObject targetCharacter, IReadOnlyList<AnimusForgeDialogueHistoryEntry> entries, Action onClose)
	{
		_screen = screen;
		_onClose = onClose;
		_dataSource = new AnimusForgeConversationHistoryLogVM(targetName, entries, targetHero, targetCharacter, HandleCloseRequested, HandleOpenEncyclopediaLink);
		_layer = new GauntletLayer("AnimusForgeConversationHistoryLog", 1200, false);
	}

	public static bool ShowForNativeConversation(Action onClose = null)
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen == null)
		{
			return false;
		}
		try
		{
			CloseActive();
			ShoutBehavior.TryGetNativeConversationPersistentHistoryTargetForExternal(out Hero targetHero, out string targetName, out string memoryId);
			ShoutBehavior.TryGetNativeConversationLinkTargetForExternal(out Hero resolvedTargetHero, out CharacterObject targetCharacter);
			targetHero = targetHero ?? resolvedTargetHero;
			List<AnimusForgeDialogueHistoryEntry> entries = targetHero != null ? MyBehavior.GetDialogueHistoryEntriesForExternal(targetHero, 260) : new List<AnimusForgeDialogueHistoryEntry>();
			if (entries.Count == 0 && targetHero == null && !string.IsNullOrWhiteSpace(memoryId))
			{
				// 读档后非 hero 原生自由对话的 session 历史不存在，右上角必须读取同一个 af_nonhero 持久历史。
				entries = MyBehavior.GetDialogueHistoryEntriesByIdForExternal(memoryId, 260);
				Logger.Log("NativeConversationHistory", "open source=persistent_nonhero memoryId=" + memoryId + " entries=" + entries.Count);
			}
			if (entries.Count == 0)
			{
				entries = ShoutBehavior.GetNativeConversationSessionHistoryEntriesForExternal(260);
				Logger.Log("NativeConversationHistory", "open source=session target=" + (targetHero?.StringId ?? targetName ?? "") + " entries=" + entries.Count);
			}
			AnimusForgeConversationHistoryLogPopup popup = new AnimusForgeConversationHistoryLogPopup(topScreen, targetName, targetHero, targetCharacter, entries, onClose);
			popup.Open();
			_activePopup = popup;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationHistory", "[ERROR] Failed to open history log: " + ex);
			CloseActive();
			return false;
		}
	}

	public static void CloseActive()
	{
		_activePopup?.Close(silent: true);
	}

	public static void OnApplicationTick()
	{
		_activePopup?.Tick();
	}

	private void Open()
	{
		_layer.LoadMovie("AnimusForgeConversationHistoryLog", _dataSource);
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

	private void Tick()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			if (_layer?.Input != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape)))
			{
				HandleCloseRequested();
				return;
			}
		}
		catch
		{
		}
		try
		{
			bool escapeDown = Input.IsKeyDown(InputKey.Escape);
			bool escapeReleased = Input.IsKeyReleased(InputKey.Escape);
			bool shouldClose = escapeReleased || (!_escapeWasDown && escapeDown);
			_escapeWasDown = escapeDown;
			if (shouldClose)
			{
				HandleCloseRequested();
			}
		}
		catch
		{
		}
	}

	private void HandleCloseRequested()
	{
		Close(silent: true);
		_onClose?.Invoke();
	}

	private void HandleOpenEncyclopediaLink(string link)
	{
		if (!_isClosed)
		{
			EncyclopediaEntityLinkNavigationCoordinator.Request(link, CloseForEncyclopediaNavigation);
		}
	}

	private void CloseForEncyclopediaNavigation()
	{
		// Do not invoke _onClose here: it restores the native overlay, which would otherwise cover the encyclopedia layer.
		Close(silent: true);
		AnimusForgeNativeConversationOverlay.CloseActive();
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
				Logger.Log("NativeConversationHistory", "[WARN] Failed to remove history log layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activePopup, this))
		{
			_activePopup = null;
		}
	}
}
