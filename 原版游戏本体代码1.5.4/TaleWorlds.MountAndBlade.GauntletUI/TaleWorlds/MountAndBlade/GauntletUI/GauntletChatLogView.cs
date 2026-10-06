using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletChatLogView : GlobalLayer
{
	private MPChatVM _dataSource;

	private ChatLogMessageManager _chatLogMessageManager;

	private bool _canFocusWhileInMission = true;

	private bool _isTeamChatAvailable;

	private GauntletMovieIdentifier _movie;

	private bool _isEnabled = true;

	private const int MaxHistoryCountForSingleplayer = 250;

	private const int MaxHistoryCountForMultiplayer = 100;

	public static GauntletChatLogView Current { get; private set; }

	public GauntletChatLogView()
	{
		_dataSource = new MPChatVM();
		_dataSource.SetGetKeyTextFromKeyIDFunc(GetToggleChatKeyText);
		_dataSource.SetGetCycleChannelKeyTextFunc(GetCycleChannelsKeyText);
		_dataSource.SetGetSendMessageKeyTextFunc(GetSendMessageKeyText);
		_dataSource.SetGetCancelSendingKeyTextFunc(GetCancelSendingKeyText);
		_dataSource.SetChatDisabledStateChangedCallback(OnChatDisabledStateChanged);
		GauntletLayer gauntletLayer = new GauntletLayer("ChatLog", 15300);
		_movie = gauntletLayer.LoadMovie("SPChatLog", _dataSource);
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ChatLogHotKeyCategory"));
		base.Layer = gauntletLayer;
		_chatLogMessageManager = new ChatLogMessageManager(_dataSource);
		MessageManager.SetMessageManager(_chatLogMessageManager);
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionsChanged));
	}

	public static void Initialize()
	{
		if (Current == null)
		{
			Current = new GauntletChatLogView();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
		}
	}

	private void OnManagedOptionsChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		bool num = changedManagedOptionsType == ManagedOptions.ManagedOptionsType.HideBattleUI && TaleWorlds.MountAndBlade.Mission.Current != null && BannerlordConfig.HideBattleUI;
		bool flag = changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableSingleplayerChatBox && !GameNetwork.IsMultiplayer && !BannerlordConfig.EnableSingleplayerChatBox;
		bool flag2 = changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableMultiplayerChatBox && GameNetwork.IsMultiplayer && !BannerlordConfig.EnableMultiplayerChatBox;
		if (num || flag || flag2)
		{
			_dataSource.Clear();
			CloseChat();
		}
	}

	private void CloseChat()
	{
		if (_dataSource.IsTypingText || _dataSource.IsInspectingMessages || base.Layer.IsFocusLayer)
		{
			if (_dataSource.IsInspectingMessages)
			{
				_dataSource.StopInspectingMessages();
			}
			else if (_dataSource.IsTypingText)
			{
				_dataSource.StopTyping(resetWrittenText: true);
			}
			UpdateFocusLayer();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_dataSource.IsChatAllowedByOptions())
		{
			_chatLogMessageManager.Update();
		}
		_dataSource.UpdateObjects(Game.Current, TaleWorlds.MountAndBlade.Mission.Current);
		_dataSource.Tick(dt);
		_dataSource.ShouldHaveOffset = GetShouldHaveOffset();
	}

	protected override void OnLateTick(float dt)
	{
		base.OnLateTick(dt);
		bool chatOpened = false;
		bool chatClosed = false;
		if (!_isEnabled || _dataSource.IsChatDisabled)
		{
			MPChatVM dataSource = _dataSource;
			if (dataSource != null && dataSource.IsInspectingMessages)
			{
				chatClosed = true;
				_dataSource.StopTyping(_dataSource.IsChatDisabled);
			}
		}
		if (_isEnabled)
		{
			MPChatVM dataSource2 = _dataSource;
			if (dataSource2 != null && dataSource2.IsChatAllowedByOptions())
			{
				HandleInput(ref chatOpened, ref chatClosed);
			}
		}
		MPChatVM dataSource3 = _dataSource;
		if ((dataSource3 == null || !dataSource3.IsInspectingMessages) && base.Layer.InputRestrictions.MouseVisibility)
		{
			base.Layer.InputRestrictions.SetMouseVisibility(isVisible: false);
		}
		if (chatOpened || chatClosed)
		{
			OnChatOpenedOrClosed(chatOpened, chatClosed);
		}
	}

	private bool GetShouldHaveOffset()
	{
		if (!_dataSource.IsTypingText && !_dataSource.IsInspectingMessages)
		{
			TaleWorlds.MountAndBlade.Mission current = TaleWorlds.MountAndBlade.Mission.Current;
			if (current != null && current.IsOrderMenuOpen && TaleWorlds.MountAndBlade.Mission.Current.Mode != MissionMode.Deployment)
			{
				return !Input.IsGamepadActive;
			}
		}
		return false;
	}

	private void HandleInput(ref bool chatOpened, ref bool chatClosed)
	{
		bool inputEnabled = false;
		bool isToggleChatHintAvailable = false;
		bool flag = true;
		bool isMouseVisible = true;
		InputContext inputContext = null;
		_isTeamChatAvailable = true;
		if (ScreenManager.TopScreen is IChatLogHandlerScreen chatLogHandlerScreen)
		{
			chatLogHandlerScreen.TryUpdateChatLogLayerParameters(ref _isTeamChatAvailable, ref inputEnabled, ref isToggleChatHintAvailable, ref isMouseVisible, ref inputContext);
			_dataSource.ShowHideShowHint = isToggleChatHintAvailable;
		}
		if (isMouseVisible != base.Layer.InputRestrictions.MouseVisibility)
		{
			base.Layer.InputRestrictions.SetMouseVisibility(isMouseVisible);
		}
		if (ScreenManager.FocusedLayer is GauntletLayer gauntletLayer && gauntletLayer != base.Layer && gauntletLayer.UIContext.EventManager.FocusedWidget is EditableTextWidget)
		{
			inputEnabled = false;
		}
		if (inputEnabled)
		{
			GameKeyContext category = HotKeyManager.GetCategory("ChatLogHotKeyCategory");
			if (inputContext != null && !inputContext.IsCategoryRegistered(category))
			{
				inputContext.RegisterHotKeyCategory(category);
			}
			if (flag)
			{
				if (_dataSource.IsInspectingMessages)
				{
					if (base.Layer.Input.IsHotKeyReleased("ToggleEscapeMenu") || base.Layer.Input.IsHotKeyReleased("Exit"))
					{
						bool isGamepadActive = Input.IsGamepadActive;
						_dataSource.StopTyping(isGamepadActive);
						chatClosed = true;
					}
					else if (base.Layer.Input.IsGameKeyReleased(8) || base.Layer.Input.IsHotKeyReleased("FinalizeChatAlternative") || base.Layer.Input.IsHotKeyReleased("SendMessage"))
					{
						if ((Input.IsGamepadActive && base.Layer.Input.IsHotKeyReleased("SendMessage")) || !Input.IsGamepadActive)
						{
							_dataSource.SendCurrentlyTypedMessage();
						}
						_dataSource.StopTyping();
						chatClosed = true;
					}
					if (base.Layer.Input.IsHotKeyReleased("CycleChatTypes"))
					{
						if (_dataSource.ActiveChannelType == ChatChannelType.Team)
						{
							_dataSource.TypeToChannelAll();
						}
						else if (_dataSource.ActiveChannelType == ChatChannelType.All && _isTeamChatAvailable)
						{
							_dataSource.TypeToChannelTeam();
						}
					}
				}
				else
				{
					if (inputContext == null)
					{
						return;
					}
					if (_canFocusWhileInMission && inputContext.IsGameKeyReleased(6))
					{
						_dataSource.TypeToChannelAll(startTyping: true);
						chatOpened = true;
					}
					else if (_canFocusWhileInMission && _isTeamChatAvailable && inputContext.IsGameKeyReleased(7))
					{
						_dataSource.TypeToChannelTeam(startTyping: true);
						chatOpened = true;
					}
					if (_canFocusWhileInMission && (inputContext.IsGameKeyReleased(8) || inputContext.IsHotKeyReleased("FinalizeChatAlternative")))
					{
						if (_dataSource.ActiveChannelType == ChatChannelType.None)
						{
							_dataSource.TypeToChannelAll(startTyping: true);
						}
						else
						{
							_dataSource.StartTyping();
						}
						chatOpened = true;
					}
				}
			}
			else if (_canFocusWhileInMission && inputContext != null && (inputContext.IsGameKeyReleased(8) || inputContext.IsHotKeyReleased("FinalizeChatAlternative")))
			{
				if (!_dataSource.IsInspectingMessages)
				{
					_dataSource.StartInspectingMessages();
					chatOpened = true;
				}
				else
				{
					_dataSource.StopInspectingMessages();
					chatClosed = true;
				}
			}
		}
		else if (_dataSource.IsTypingText)
		{
			_dataSource.StopTyping();
			chatClosed = true;
		}
		else if (_dataSource.IsInspectingMessages)
		{
			_dataSource.StopInspectingMessages();
			chatClosed = true;
		}
	}

	private void OnChatOpenedOrClosed(bool chatOpened, bool chatClosed)
	{
		UpdateFocusLayer();
		if (ScreenManager.TopScreen is MissionScreen { SceneLayer: not null } missionScreen)
		{
			missionScreen.Mission.GetMissionBehavior<MissionMainAgentController>().IsChatOpen = chatOpened && !chatClosed;
		}
	}

	private void UpdateFocusLayer()
	{
		if (_dataSource.IsTypingText || _dataSource.IsInspectingMessages)
		{
			if (_dataSource.IsTypingText && !base.Layer.IsFocusLayer)
			{
				base.Layer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(base.Layer);
			}
			base.Layer.InputRestrictions.SetInputRestrictions();
		}
		else
		{
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			base.Layer.InputRestrictions.ResetInputRestrictions();
		}
	}

	public void SetCanFocusWhileInMission(bool canFocusInMission)
	{
		_canFocusWhileInMission = canFocusInMission;
	}

	public void OnSupportedFeaturesReceived(SupportedFeatures supportedFeatures)
	{
		SetEnabled(supportedFeatures.SupportsFeatures(Features.TextChat));
	}

	public void SetEnabled(bool isEnabled)
	{
		if (_isEnabled != isEnabled)
		{
			_isEnabled = isEnabled;
		}
	}

	public void LoadMovie(bool forMultiplayer)
	{
		if (_movie != null)
		{
			(base.Layer as GauntletLayer)?.ReleaseMovie(_movie);
		}
		if (forMultiplayer)
		{
			Game.Current?.GetGameHandler<ChatBox>()?.InitializeForMultiplayer();
			_movie = (base.Layer as GauntletLayer)?.LoadMovie("MPChatLog", _dataSource);
			_dataSource.SetMessageHistoryCapacity(100);
			return;
		}
		SetEnabled(isEnabled: true);
		Game.Current?.GetGameHandler<ChatBox>().InitializeForSinglePlayer();
		_movie = (base.Layer as GauntletLayer)?.LoadMovie("SPChatLog", _dataSource);
		_dataSource.ChatBoxSizeX = BannerlordConfig.ChatBoxSizeX;
		_dataSource.ChatBoxSizeY = BannerlordConfig.ChatBoxSizeY;
		_dataSource.SetMessageHistoryCapacity(250);
	}

	private TextObject GetToggleChatKeyText()
	{
		if (Input.IsGamepadActive)
		{
			return Game.Current?.GameTextManager?.GetHotKeyGameTextFromKeyID("controllerloption");
		}
		return Game.Current?.GameTextManager?.GetHotKeyGameTextFromKeyID("enter");
	}

	private TextObject GetCycleChannelsKeyText()
	{
		return Game.Current?.GameTextManager?.GetHotKeyGameText("ChatLogHotKeyCategory", "CycleChatTypes") ?? TextObject.GetEmpty();
	}

	private TextObject GetSendMessageKeyText()
	{
		return Game.Current?.GameTextManager?.GetHotKeyGameText("ChatLogHotKeyCategory", "SendMessage") ?? TextObject.GetEmpty();
	}

	private TextObject GetCancelSendingKeyText()
	{
		return Game.Current?.GameTextManager?.GetHotKeyGameText("GenericPanelGameKeyCategory", "Exit") ?? TextObject.GetEmpty();
	}

	private void OnChatDisabledStateChanged(bool chatDisabled)
	{
		if (!chatDisabled)
		{
			_dataSource.StopTyping(resetWrittenText: true);
			OnChatOpenedOrClosed(chatOpened: false, chatClosed: true);
		}
	}
}
