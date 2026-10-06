using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.AuxiliaryKeys;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TaleWorlds.PlayerServices;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

[GameStateScreen(typeof(LobbyState))]
public class MultiplayerLobbyGauntletScreen : ScreenBase, IGameStateListener, ILobbyStateHandler, IChatLogHandlerScreen
{
	private List<KeyValuePair<string, InquiryData>> _feedbackInquiries;

	private string _activeFeedbackId;

	private KeybindingPopup _keybindingPopup;

	private KeyOptionVM _currentKey;

	private SpriteCategory _optionsSpriteCategory;

	private SpriteCategory _multiplayerSpriteCategory;

	private GauntletLayer _gauntletBrightnessLayer;

	private BrightnessOptionVM _brightnessOptionDataSource;

	private GauntletMovieIdentifier _brightnessOptionMovie;

	private LobbyState _lobbyState;

	private BasicCharacterObject _playerCharacter;

	private bool _isFacegenOpen;

	private SoundEvent _musicSoundEvent;

	private bool _isNavigationRestricted;

	private bool _wasChatRestricted;

	private MPCustomGameSortControllerVM.CustomServerSortOption? _cachedCustomServerSortOption;

	private MPCustomGameSortControllerVM.SortState _cachedCustomServerSortState;

	private MPCustomGameSortControllerVM.CustomServerSortOption? _cachedPremadeGameSortOption;

	private MPCustomGameSortControllerVM.SortState _cachedPremadeGameSortState;

	private bool _isLobbyActive;

	private GauntletLayer _lobbyLayer;

	private MPLobbyVM _lobbyDataSource;

	private SpriteCategory _mplobbyCategory;

	private SpriteCategory _bannerIconsCategory;

	private SpriteCategory _badgesCategory;

	public MPLobbyVM.LobbyPage CurrentPage
	{
		get
		{
			if (_lobbyDataSource != null)
			{
				return _lobbyDataSource.CurrentPage;
			}
			return MPLobbyVM.LobbyPage.NotAssigned;
		}
	}

	public MPLobbyVM DataSource => _lobbyDataSource;

	public GauntletLayer LobbyLayer => _lobbyLayer;

	public MultiplayerLobbyGauntletScreen(LobbyState lobbyState)
	{
		AvatarThumbnailCache.Current?.FlushCache();
		_feedbackInquiries = new List<KeyValuePair<string, InquiryData>>();
		_lobbyState = lobbyState;
		_lobbyState.Handler = this;
		GauntletFullScreenNoticeView.Initialize();
		MultiplayerGauntletGameNotification.Initialize();
		GauntletChatLogView.Current?.LoadMovie(forMultiplayer: true);
		GauntletChatLogView.Current?.SetEnabled(isEnabled: false);
		MultiplayerAdminInformationScreen.OnInitialize();
		MultiplayerReportPlayerScreen.OnInitialize();
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		LoadingWindow.DisableGlobalLoadingWindow();
		_keybindingPopup = new KeybindingPopup(SetHotKey, this);
		_lobbyDataSource?.RefreshPlayerData(_lobbyState.LobbyClient.PlayerData);
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		InformationManager.HideAllMessages();
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableGenericAvatars)
		{
			_lobbyDataSource?.OnEnableGenericAvatarsChanged();
		}
		if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableGenericNames)
		{
			_lobbyDataSource?.OnEnableGenericNamesChanged();
		}
	}

	void IChatLogHandlerScreen.TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref bool isToggleChatHintAvailable, ref bool isMouseVisible, ref InputContext inputContext)
	{
		if (LobbyLayer != null)
		{
			MPLobbyVM lobbyDataSource = _lobbyDataSource;
			int num;
			if (lobbyDataSource == null || lobbyDataSource.Options?.IsEnabled != true)
			{
				MPLobbyVM lobbyDataSource2 = _lobbyDataSource;
				num = ((lobbyDataSource2 != null && !lobbyDataSource2.HasNoPopupOpen()) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool flag = (byte)num != 0;
			inputEnabled = !flag && !_wasChatRestricted;
			inputContext = LobbyLayer.Input;
			_wasChatRestricted = flag;
		}
	}

	protected override void OnFinalize()
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Lobby screen finalized from non-main thread", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\MultiplayerLobbyGauntletScreen.cs", "OnFinalize", 133);
		}
		AvatarThumbnailCache.Current?.FlushCache();
		if (_lobbyDataSource != null)
		{
			_lobbyDataSource.OnFinalize();
			_lobbyDataSource = null;
		}
		_mplobbyCategory?.Unload();
		_optionsSpriteCategory.Unload();
		_multiplayerSpriteCategory.Unload();
		_badgesCategory?.Unload();
		GauntletGameNotification.Initialize();
		MultiplayerReportPlayerScreen.OnFinalize();
		MultiplayerAdminInformationScreen.OnRemove();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		_lobbyState.Handler = null;
		_lobbyState = null;
		base.OnFinalize();
	}

	protected override void OnActivate()
	{
		if (_lobbyDataSource != null && _isFacegenOpen)
		{
			OnFacegenClosed(updateCharacter: true);
		}
		_lobbyDataSource?.OnActivate();
		_lobbyDataSource?.RefreshPlayerData(_lobbyState.LobbyClient.PlayerData);
		_lobbyDataSource?.RefreshRecentGames();
		LoadingWindow.DisableGlobalLoadingWindow();
	}

	private void CreateView()
	{
		_musicSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/lobby_music", null);
		_musicSoundEvent.Play();
		if (!(GameStateManager.Current.ActiveState is MissionState))
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		_mplobbyCategory = UIResourceManager.LoadSpriteCategory("ui_mplobby");
		_bannerIconsCategory = UIResourceManager.LoadSpriteCategory("ui_bannericons");
		_badgesCategory = UIResourceManager.LoadSpriteCategory("ui_mpbadges");
		_lobbyDataSource = new MPLobbyVM(_lobbyState, OnOpenFacegen, OnForceCloseFacegen, OnLogout, OnLobbyBusyStateChanged, OnKeybindRequest, GetContinueKeyText, SetNavigationRestriction);
		GameKeyContext category = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
		_lobbyDataSource.CreateInputKeyVisuals(category.GetHotKey("Exit"), category.GetHotKey("Confirm"), category.GetHotKey("SwitchToPreviousTab"), category.GetHotKey("SwitchToNextTab"), category.GetHotKey("TakeAll"), category.GetHotKey("GiveAll"));
		_lobbyLayer = new GauntletLayer("LobbyScreen", 10, shouldClear: true);
		_lobbyLayer.LoadMovie("Lobby", _lobbyDataSource);
		_lobbyLayer.InputRestrictions.SetInputRestrictions();
		_lobbyLayer.IsFocusLayer = true;
		AddLayer(_lobbyLayer);
		ScreenManager.TrySetFocus(_lobbyLayer);
		_lobbyLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_lobbyLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
		GameKeyContext category2 = HotKeyManager.GetCategory("MultiplayerHotkeyCategory");
		GameKeyContext genericPanelCategory = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
		_lobbyDataSource.BadgeSelectionPopup.RefreshKeyBindings(category2.GetHotKey("InspectBadgeProgression"));
		_lobbyDataSource.BadgeProgressionInformation.SetPreviousTabInputKey(genericPanelCategory.GetHotKey("SwitchToPreviousTab"));
		_lobbyDataSource.BadgeProgressionInformation.SetNextTabInputKey(genericPanelCategory.GetHotKey("SwitchToNextTab"));
		_lobbyDataSource.Armory.Cosmetics.RefreshKeyBindings(category2.GetHotKey("PerformActionOnCosmeticItem"), category2.GetHotKey("PreviewCosmeticItem"));
		_lobbyDataSource.Armory.Cosmetics.TauntSlots.ApplyActionOnAllItems(delegate(MPArmoryCosmeticTauntSlotVM t)
		{
			t.SetSelectKeyVisual(genericPanelCategory.GetHotKey("GiveAll"));
		});
		_lobbyDataSource.Armory.Cosmetics.TauntSlots.ApplyActionOnAllItems(delegate(MPArmoryCosmeticTauntSlotVM t)
		{
			t.SetEmptySlotKeyVisual(genericPanelCategory.GetHotKey("TakeAll"));
		});
		_lobbyDataSource.Friends.SetToggleFriendListKey(category2.RegisteredHotKeys.FirstOrDefault((HotKey g) => g?.Id == "ToggleFriendsList"));
		_lobbyDataSource.Matchmaking.CustomServer.SortController.InitializeWithSortState(_cachedCustomServerSortOption, _cachedCustomServerSortState);
		_lobbyDataSource.Matchmaking.PremadeMatches.SortController.InitializeWithSortState(_cachedPremadeGameSortOption, _cachedPremadeGameSortState);
		_lobbyDataSource.Options.SetDoneInputKey(genericPanelCategory.GetHotKey("Confirm"));
		_lobbyDataSource.Options.SetCancelInputKey(genericPanelCategory.GetHotKey("Exit"));
		_lobbyDataSource.Options.SetResetInputKey(genericPanelCategory.GetHotKey("Reset"));
		_lobbyDataSource.Options.SetPreviousTabInputKey(genericPanelCategory.GetHotKey("TakeAll"));
		_lobbyDataSource.Options.SetNextTabInputKey(genericPanelCategory.GetHotKey("GiveAll"));
		_lobbyDataSource.Matchmaking.CustomServer.SetRefreshInputKey(genericPanelCategory.GetHotKey("Reset"));
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessCalibrated) < 2f)
		{
			_brightnessOptionDataSource = new BrightnessOptionVM(OnCloseBrightness)
			{
				Visible = true
			};
			_gauntletBrightnessLayer = new GauntletLayer("MultiplayerBrightness", 11);
			_gauntletBrightnessLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Mouse);
			_brightnessOptionMovie = _gauntletBrightnessLayer.LoadMovie("BrightnessOption", _brightnessOptionDataSource);
			AddLayer(_gauntletBrightnessLayer);
		}
		_optionsSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_options");
		_multiplayerSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_mpmission");
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_lobbyDataSource?.OnDeactivate();
	}

	private void OnCloseBrightness(bool isConfirm)
	{
		_gauntletBrightnessLayer.ReleaseMovie(_brightnessOptionMovie);
		RemoveLayer(_gauntletBrightnessLayer);
		_brightnessOptionDataSource = null;
		_gauntletBrightnessLayer = null;
		NativeOptions.SaveConfig();
	}

	private void OnOpenFacegen(BasicCharacterObject character)
	{
		_isFacegenOpen = true;
		_playerCharacter = character;
		LoadingWindow.EnableGlobalLoadingWindow();
		ScreenManager.PushScreen(ViewCreator.CreateMBFaceGeneratorScreen(character, openedFromMultiplayer: true));
	}

	private void OnForceCloseFacegen()
	{
		if (_isFacegenOpen)
		{
			OnFacegenClosed(updateCharacter: false);
			ScreenManager.PopScreen();
		}
	}

	private async void OnFacegenClosed(bool updateCharacter)
	{
		if (updateCharacter)
		{
			await NetworkMain.GameClient.UpdateCharacter(_playerCharacter.GetBodyPropertiesMin(), _playerCharacter.IsFemale);
		}
		ScreenManager.TrySetFocus(_lobbyLayer);
		_lobbyDataSource.RefreshPlayerData(_lobbyState.LobbyClient.PlayerData);
		_isFacegenOpen = false;
		_playerCharacter = null;
	}

	private string GetContinueKeyText()
	{
		if (Input.IsGamepadActive)
		{
			GameTexts.SetVariable("CONSOLE_KEY_NAME", Game.Current.GameTextManager.GetHotKeyGameText("GenericPanelGameKeyCategory", "Exit"));
			return GameTexts.FindText("str_click_to_exit_console").ToString();
		}
		return GameTexts.FindText("str_click_to_exit").ToString();
	}

	private void OnLogout()
	{
		GauntletChatLogView.Current?.SetEnabled(isEnabled: false);
	}

	private void OnLobbyBusyStateChanged(bool isBusy)
	{
		if (isBusy)
		{
			_lobbyLayer.InputRestrictions.ResetInputRestrictions();
		}
		else
		{
			_lobbyLayer.InputRestrictions.SetInputRestrictions();
		}
	}

	private void SetNavigationRestriction(bool isRestricted)
	{
		if (_isNavigationRestricted != isRestricted)
		{
			_isNavigationRestricted = isRestricted;
		}
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		TickInternal(dt);
	}

	private void TickInternal(float dt)
	{
		_lobbyDataSource?.OnTick(dt);
		if (_activeFeedbackId == null && _feedbackInquiries.Count > 0)
		{
			ShowNextFeedback();
		}
		if (_lobbyLayer != null)
		{
			MPLobbyVM lobbyDataSource = _lobbyDataSource;
			if (lobbyDataSource != null && lobbyDataSource.Options?.IsEnabled == true)
			{
				MPOptionsVM options = _lobbyDataSource.Options;
				KeybindingPopup keybindingPopup = _keybindingPopup;
				options.AreHotkeysEnabled = (keybindingPopup == null || !keybindingPopup.IsActive) && !_lobbyLayer.IsFocusedOnInput() && !InformationManager.IsAnyInquiryActive() && _lobbyDataSource.HasNoPopupOpen();
			}
			KeybindingPopup keybindingPopup2 = _keybindingPopup;
			if (keybindingPopup2 != null && keybindingPopup2.IsActive)
			{
				_keybindingPopup.Tick();
			}
			else if (_lobbyDataSource != null && !_lobbyState.IsLoggingIn && !_lobbyDataSource.BlockerState.IsEnabled && !_lobbyLayer.IsFocusedOnInput())
			{
				HandleInput(dt);
			}
		}
	}

	private void HandleInput(float dt)
	{
		bool flag = _lobbyLayer.Input.IsHotKeyReleased("Confirm");
		bool flag2 = _lobbyLayer.Input.IsHotKeyReleased("ToggleFriendsList");
		if (flag || flag2)
		{
			if (_lobbyDataSource.Login.IsEnabled && flag)
			{
				_lobbyDataSource.Login.ExecuteLogin();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			else if (_lobbyDataSource.Options.IsEnabled && _lobbyDataSource.Options.AreHotkeysEnabled && flag)
			{
				_lobbyDataSource.Options.ExecuteApply();
				UISoundsHelper.PlayUISound("event:/ui/default");
			}
			else if (!_lobbyDataSource.HasNoPopupOpen() && flag)
			{
				_lobbyDataSource.OnConfirm();
			}
			else if (flag2)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_lobbyDataSource.Friends.IsListEnabled = !_lobbyDataSource.Friends.IsListEnabled;
				_lobbyDataSource.ForceCloseContextMenus();
			}
		}
		else if (_lobbyLayer.Input.IsHotKeyReleased("Exit"))
		{
			_lobbyDataSource.OnEscape();
		}
		else if (_lobbyLayer.Input.IsHotKeyPressed("TakeAll"))
		{
			if (_lobbyDataSource.RankLeaderboard.IsEnabled)
			{
				if (_lobbyDataSource.RankLeaderboard.IsPreviousPageAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
				}
				_lobbyDataSource.RankLeaderboard.ExecuteLoadFirstPage();
			}
			else if (_lobbyDataSource.Armory.IsEnabled)
			{
				if (Input.IsGamepadActive && _lobbyDataSource.Armory.IsManagingTaunts)
				{
					_lobbyDataSource.Armory.ExecuteEmptyFocusedSlot();
				}
			}
			else if (_lobbyDataSource.Options.IsEnabled && _lobbyDataSource.Options.AreHotkeysEnabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_lobbyDataSource.Options.SelectPreviousCategory();
			}
		}
		else if (_lobbyLayer.Input.IsHotKeyPressed("GiveAll"))
		{
			if (_lobbyDataSource.RankLeaderboard.IsEnabled)
			{
				if (_lobbyDataSource.RankLeaderboard.IsNextPageAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
				}
				_lobbyDataSource.RankLeaderboard.ExecuteLoadLastPage();
			}
			else if (_lobbyDataSource.Armory.IsEnabled)
			{
				if (Input.IsGamepadActive && _lobbyDataSource.Armory.IsManagingTaunts)
				{
					_lobbyDataSource.Armory.ExecuteSelectFocusedSlot();
				}
			}
			else if (_lobbyDataSource.Options.IsEnabled && _lobbyDataSource.Options.AreHotkeysEnabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_lobbyDataSource.Options.SelectNextCategory();
			}
		}
		else if (_lobbyLayer.Input.IsHotKeyPressed("SwitchToPreviousTab"))
		{
			if (_lobbyDataSource.RankLeaderboard.IsEnabled)
			{
				if (_lobbyDataSource.RankLeaderboard.IsPreviousPageAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
				}
				_lobbyDataSource.RankLeaderboard.ExecuteLoadPreviousPage();
			}
			else if (_lobbyDataSource.BadgeProgressionInformation.IsEnabled)
			{
				if (_lobbyDataSource.BadgeProgressionInformation.CanDecreaseBadgeIndices)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
					_lobbyDataSource.BadgeProgressionInformation.ExecuteDecreaseActiveBadgeIndices();
				}
			}
			else if (!_isNavigationRestricted)
			{
				SelectPreviousPage();
			}
		}
		else if (_lobbyLayer.Input.IsHotKeyPressed("SwitchToNextTab"))
		{
			if (_lobbyDataSource.RankLeaderboard.IsEnabled)
			{
				if (_lobbyDataSource.RankLeaderboard.IsNextPageAvailable)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
				}
				_lobbyDataSource.RankLeaderboard.ExecuteLoadNextPage();
			}
			else if (_lobbyDataSource.BadgeProgressionInformation.IsEnabled)
			{
				if (_lobbyDataSource.BadgeProgressionInformation.CanIncreaseBadgeIndices)
				{
					UISoundsHelper.PlayUISound("event:/ui/checkbox");
					_lobbyDataSource.BadgeProgressionInformation.ExecuteIncreaseActiveBadgeIndices();
				}
			}
			else if (!_isNavigationRestricted)
			{
				SelectNextPage();
			}
		}
		else if (_lobbyLayer.Input.IsHotKeyReleased("Reset"))
		{
			if (_lobbyDataSource.HasNoPopupOpen() && _lobbyDataSource.Options.IsEnabled && _lobbyDataSource.Options.AreHotkeysEnabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_lobbyDataSource.Options.ExecuteCancel();
			}
			else if (_lobbyDataSource.Matchmaking.CustomServer.IsEnabled && !_lobbyDataSource.Matchmaking.CustomServer.IsCreateGamePanelActive && !_lobbyDataSource.Matchmaking.CustomServer.IsRefreshing)
			{
				_lobbyDataSource.Matchmaking.CustomServer.ExecuteRefresh();
			}
		}
	}

	private void ShowNextFeedback()
	{
		KeyValuePair<string, InquiryData> item = _feedbackInquiries[0];
		_feedbackInquiries.Remove(item);
		_activeFeedbackId = item.Key;
		InformationManager.ShowInquiry(item.Value);
	}

	[Conditional("DEBUG")]
	private void TickDebug(float dt)
	{
	}

	void ILobbyStateHandler.SetConnectionState(bool isAuthenticated)
	{
		if (_lobbyDataSource == null)
		{
			CreateView();
		}
		if (isAuthenticated && _lobbyState.LobbyClient.PlayerData != null)
		{
			_lobbyDataSource.RefreshPlayerData(_lobbyState.LobbyClient.PlayerData);
			if (_lobbyDataSource.CurrentPage == MPLobbyVM.LobbyPage.NotAssigned || _lobbyDataSource.CurrentPage == MPLobbyVM.LobbyPage.Authentication)
			{
				_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Home);
			}
		}
		else
		{
			if (_isFacegenOpen)
			{
				OnForceCloseFacegen();
			}
			_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Authentication);
		}
		_lobbyDataSource.ConnectionStateUpdated(isAuthenticated);
	}

	void ILobbyStateHandler.OnRequestedToSearchBattle()
	{
		_musicSoundEvent.SetParameter("mpMusicSwitcher", 1f);
		_lobbyDataSource?.OnRequestedToSearchBattle();
	}

	void ILobbyStateHandler.OnUpdateFindingGame(MatchmakingWaitTimeStats matchmakingWaitTimeStats, string[] gameTypeInfo)
	{
		_lobbyDataSource?.OnUpdateFindingGame(matchmakingWaitTimeStats, gameTypeInfo);
	}

	void ILobbyStateHandler.OnRequestedToCancelSearchBattle()
	{
		_lobbyDataSource?.OnRequestedToCancelSearchBattle();
	}

	void ILobbyStateHandler.OnSearchBattleCanceled()
	{
		_musicSoundEvent.SetParameter("mpMusicSwitcher", 0f);
		_lobbyDataSource?.OnSearchBattleCanceled();
	}

	void ILobbyStateHandler.OnPause()
	{
	}

	void ILobbyStateHandler.OnResume()
	{
		_lobbyDataSource?.RefreshPlayerData(_lobbyState.LobbyClient.PlayerData);
	}

	void ILobbyStateHandler.OnDisconnected()
	{
		_lobbyDataSource?.OnDisconnected();
	}

	void ILobbyStateHandler.OnPlayerDataReceived(PlayerData playerData)
	{
		_lobbyDataSource?.RefreshPlayerData(playerData);
		GauntletChatLogView.Current?.OnSupportedFeaturesReceived(_lobbyState.LobbyClient.SupportedFeatures);
	}

	void ILobbyStateHandler.OnPendingRejoin()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Rejoin);
	}

	void ILobbyStateHandler.OnEnterBattleWithParty(string[] selectedGameTypes)
	{
	}

	void ILobbyStateHandler.OnPartyInvitationReceived(PlayerId playerID)
	{
		if (_lobbyState.LobbyClient.SupportedFeatures.SupportsFeatures(Features.Party))
		{
			_lobbyDataSource?.PartyInvitationPopup.OpenWith(playerID);
		}
		else
		{
			_lobbyState.LobbyClient.DeclinePartyInvitation();
		}
	}

	void ILobbyStateHandler.OnPartyJoinRequestReceived(PlayerId joingPlayerId, PlayerId viaPlayerId, string viaPlayerName, bool newParty)
	{
		if (_lobbyState.LobbyClient.SupportedFeatures.SupportsFeatures(Features.Party))
		{
			if (_lobbyDataSource != null)
			{
				if (newParty)
				{
					_lobbyDataSource.PartyJoinRequestPopup.OpenWithNewParty(joingPlayerId);
				}
				else
				{
					_lobbyDataSource.PartyJoinRequestPopup.OpenWith(joingPlayerId, viaPlayerId, viaPlayerName);
				}
			}
		}
		else
		{
			_lobbyState.LobbyClient.DeclinePartyJoinRequest(joingPlayerId, PartyJoinDeclineReason.NoFeature);
		}
	}

	void ILobbyStateHandler.OnPartyInvitationInvalidated()
	{
		_lobbyDataSource?.PartyInvitationPopup.Close();
	}

	void ILobbyStateHandler.OnPlayerInvitedToParty(PlayerId playerId)
	{
		_lobbyDataSource?.Friends.OnPlayerInvitedToParty(playerId);
	}

	void ILobbyStateHandler.OnPlayerAddedToParty(PlayerId playerId, string playerName, bool isPartyLeader)
	{
		_lobbyDataSource?.OnPlayerAddedToParty(playerId);
	}

	void ILobbyStateHandler.OnPlayerRemovedFromParty(PlayerId playerId, PartyRemoveReason reason)
	{
		_lobbyDataSource?.OnPlayerRemovedFromParty(playerId, reason);
	}

	void ILobbyStateHandler.OnGameClientStateChange(LobbyClient.State state)
	{
	}

	void ILobbyStateHandler.OnAdminMessageReceived(string message)
	{
		InformationManager.AddSystemNotification(message);
	}

	public void OnBattleServerInformationReceived(BattleServerInformationForClient battleServerInformation)
	{
		UISoundsHelper.PlayUISound("event:/ui/multiplayer/match_ready");
		_lobbyDataSource.Matchmaking.IsFindingMatch = false;
	}

	string ILobbyStateHandler.ShowFeedback(string title, string feedbackText)
	{
		string id = Guid.NewGuid().ToString();
		InquiryData value = new InquiryData(title, feedbackText, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", new TextObject("{=dismissnotification}Dismiss").ToString(), null, delegate
		{
			((ILobbyStateHandler)this).DismissFeedback(id);
		});
		_feedbackInquiries.Add(new KeyValuePair<string, InquiryData>(id, value));
		return id;
	}

	string ILobbyStateHandler.ShowFeedback(InquiryData inquiryData)
	{
		string text = Guid.NewGuid().ToString();
		_feedbackInquiries.Add(new KeyValuePair<string, InquiryData>(text, inquiryData));
		return text;
	}

	void ILobbyStateHandler.DismissFeedback(string feedbackId)
	{
		if (_activeFeedbackId != null && _activeFeedbackId.Equals(feedbackId))
		{
			InformationManager.HideInquiry();
			_activeFeedbackId = null;
			return;
		}
		KeyValuePair<string, InquiryData> item = _feedbackInquiries.FirstOrDefault((KeyValuePair<string, InquiryData> q) => q.Key.Equals(feedbackId));
		if (item.Key != null)
		{
			_feedbackInquiries.Remove(item);
		}
	}

	private void SelectPreviousPage(MPLobbyVM.LobbyPage currentPage = MPLobbyVM.LobbyPage.NotAssigned)
	{
		MPLobbyVM lobbyDataSource = _lobbyDataSource;
		if (lobbyDataSource == null || !lobbyDataSource.HasNoPopupOpen())
		{
			return;
		}
		if (currentPage == MPLobbyVM.LobbyPage.NotAssigned)
		{
			currentPage = _lobbyDataSource.CurrentPage;
		}
		int num;
		switch (currentPage)
		{
		default:
			return;
		case MPLobbyVM.LobbyPage.Home:
		case MPLobbyVM.LobbyPage.Armory:
		case MPLobbyVM.LobbyPage.Matchmaking:
		case MPLobbyVM.LobbyPage.Profile:
			num = (int)(currentPage - 1);
			break;
		case MPLobbyVM.LobbyPage.Options:
			num = 7;
			break;
		}
		MPLobbyVM.LobbyPage lobbyPage = (MPLobbyVM.LobbyPage)num;
		if (_lobbyDataSource.DisallowedPages.Contains(lobbyPage))
		{
			SelectPreviousPage(lobbyPage);
			return;
		}
		if (lobbyPage == MPLobbyVM.LobbyPage.Options)
		{
			UISoundsHelper.PlayUISound("event:/ui/checkbox");
		}
		else
		{
			UISoundsHelper.PlayUISound("event:/ui/tab");
		}
		_lobbyDataSource.SetPage(lobbyPage);
	}

	private void SelectNextPage(MPLobbyVM.LobbyPage currentPage = MPLobbyVM.LobbyPage.NotAssigned)
	{
		MPLobbyVM lobbyDataSource = _lobbyDataSource;
		if (lobbyDataSource == null || !lobbyDataSource.HasNoPopupOpen())
		{
			return;
		}
		if (currentPage == MPLobbyVM.LobbyPage.NotAssigned)
		{
			currentPage = _lobbyDataSource.CurrentPage;
		}
		int num;
		switch (currentPage)
		{
		default:
			return;
		case MPLobbyVM.LobbyPage.Options:
		case MPLobbyVM.LobbyPage.Home:
		case MPLobbyVM.LobbyPage.Armory:
		case MPLobbyVM.LobbyPage.Matchmaking:
			num = (int)(currentPage + 1);
			break;
		case MPLobbyVM.LobbyPage.Profile:
			num = 3;
			break;
		}
		MPLobbyVM.LobbyPage lobbyPage = (MPLobbyVM.LobbyPage)num;
		if (_lobbyDataSource.DisallowedPages.Contains(lobbyPage))
		{
			SelectNextPage(lobbyPage);
			return;
		}
		if (lobbyPage == MPLobbyVM.LobbyPage.Options)
		{
			UISoundsHelper.PlayUISound("event:/ui/checkbox");
		}
		else
		{
			UISoundsHelper.PlayUISound("event:/ui/tab");
		}
		_lobbyDataSource.SetPage(lobbyPage);
	}

	void ILobbyStateHandler.OnActivateCustomServer()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Matchmaking, MPMatchmakingVM.MatchmakingSubPages.CustomGameList);
	}

	void ILobbyStateHandler.OnActivateHome()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Home);
	}

	void ILobbyStateHandler.OnActivateMatchmaking()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Matchmaking);
	}

	void ILobbyStateHandler.OnActivateArmory()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Armory);
	}

	void ILobbyStateHandler.OnActivateProfile()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Profile);
	}

	void ILobbyStateHandler.OnClanInvitationReceived(string clanName, string clanTag, bool isCreation)
	{
		_lobbyDataSource.ClanInvitationPopup.Open(clanName, clanTag, isCreation);
	}

	void ILobbyStateHandler.OnClanInvitationAnswered(PlayerId playerId, ClanCreationAnswer answer)
	{
		_lobbyDataSource?.ClanCreationPopup.UpdateConfirmation(playerId, answer);
		_lobbyDataSource?.ClanInvitationPopup.UpdateConfirmation(playerId, answer);
	}

	void ILobbyStateHandler.OnClanCreationSuccessful()
	{
		_lobbyDataSource?.OnClanCreationFinished();
	}

	void ILobbyStateHandler.OnClanCreationFailed()
	{
		_lobbyDataSource?.OnClanCreationFinished();
	}

	void ILobbyStateHandler.OnClanCreationStarted()
	{
		_lobbyDataSource.ClanCreationPopup.ExecuteSwitchToWaiting();
	}

	void ILobbyStateHandler.OnClanInfoChanged()
	{
		_lobbyDataSource?.OnClanInfoChanged();
	}

	void ILobbyStateHandler.OnPremadeGameEligibilityStatusReceived(bool isEligible)
	{
		_lobbyDataSource?.Matchmaking.OnPremadeGameEligibilityStatusReceived(isEligible);
	}

	void ILobbyStateHandler.OnPremadeGameCreated()
	{
		_lobbyDataSource?.OnPremadeGameCreated();
	}

	void ILobbyStateHandler.OnPremadeGameListReceived()
	{
		PremadeGameEntry[] array = NetworkMain.GameClient?.AvailablePremadeGames?.PremadeGameEntries;
		if (array != null)
		{
			_lobbyDataSource?.Matchmaking.PremadeMatches.SetPremadeGameList(array);
		}
	}

	void ILobbyStateHandler.OnPremadeGameCreationCancelled()
	{
		_musicSoundEvent.SetParameter("mpMusicSwitcher", 0f);
		_lobbyDataSource?.OnSearchBattleCanceled();
	}

	void ILobbyStateHandler.OnJoinPremadeGameRequested(string clanName, string clanSigilCode, Guid partyId, PlayerId[] challengerPlayerIDs, PlayerId challengerPartyLeaderID, PremadeGameType premadeGameType)
	{
		_lobbyDataSource.ClanMatchmakingRequestPopup.OpenWith(clanName, clanSigilCode, partyId, challengerPlayerIDs, challengerPartyLeaderID, premadeGameType);
	}

	void ILobbyStateHandler.OnJoinPremadeGameRequestSuccessful()
	{
		if (!_lobbyDataSource.GameSearch.IsEnabled)
		{
			_lobbyDataSource.OnPremadeGameCreated();
		}
		_lobbyDataSource.GameSearch.OnJoinPremadeGameRequestSuccessful();
	}

	void ILobbyStateHandler.OnSigilChanged()
	{
		if (_lobbyDataSource != null)
		{
			_lobbyDataSource.OnSigilChanged(_lobbyDataSource.ChangeSigilPopup.SelectedSigil.IconID);
		}
	}

	void ILobbyStateHandler.OnActivateOptions()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Options);
	}

	void ILobbyStateHandler.OnDeactivateOptions()
	{
		_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Home);
	}

	void ILobbyStateHandler.OnCustomGameServerListReceived(AvailableCustomGames customGameServerList)
	{
		_lobbyDataSource.Matchmaking.CustomServer.SetCustomGameServerList(customGameServerList);
	}

	void ILobbyStateHandler.OnMatchmakerGameOver(int oldExperience, int newExperience, List<string> badgesEarned, int lootGained, RankBarInfo oldRankBarInfo, RankBarInfo newRankBarInfo, BattleCancelReason battleCancelReason)
	{
		switch (battleCancelReason)
		{
		case BattleCancelReason.None:
			_lobbyDataSource?.AfterBattlePopup.OpenWith(oldExperience, newExperience, badgesEarned, lootGained, oldRankBarInfo, newRankBarInfo);
			break;
		case BattleCancelReason.PlayerLeaveDuringWarmup:
		{
			TextObject textObject = new TextObject("{=CtMEl2NP}Game is cancelled");
			InformationManager.ShowInquiry(new InquiryData(text: new TextObject("{=A6OFgTIU}Game is cancelled due to a player leaving during warmup.").ToString(), titleText: textObject.ToString(), isAffirmativeOptionShown: false, isNegativeOptionShown: true, affirmativeText: "", negativeText: GameTexts.FindText("str_dismiss").ToString(), affirmativeAction: null, negativeAction: null));
			break;
		}
		}
	}

	void ILobbyStateHandler.OnBattleServerLost()
	{
		TextObject title = new TextObject("{=wLpJEkKY}Battle Server Crashed");
		TextObject message = new TextObject("{=EzeFJo65}You have been disconnected from server!");
		_lobbyDataSource.QueryPopup.ShowMessage(title, message);
	}

	void ILobbyStateHandler.OnRemovedFromMatchmakerGame(DisconnectType disconnectType)
	{
		ShowDisconnectMessage(disconnectType);
	}

	void ILobbyStateHandler.OnRemovedFromCustomGame(DisconnectType disconnectType)
	{
		ShowDisconnectMessage(disconnectType);
	}

	void ILobbyStateHandler.OnPlayerAssignedPartyLeader(PlayerId partyLeaderId)
	{
		_lobbyDataSource?.OnPlayerAssignedPartyLeader(partyLeaderId);
	}

	void ILobbyStateHandler.OnPlayerSuggestedToParty(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName)
	{
		_lobbyDataSource?.OnPlayerSuggestedToParty(playerId, playerName, suggestingPlayerId, suggestingPlayerName);
	}

	void ILobbyStateHandler.OnNotificationsReceived(LobbyNotification[] notifications)
	{
		_lobbyDataSource?.OnNotificationsReceived(notifications);
	}

	void ILobbyStateHandler.OnJoinCustomGameFailureResponse(CustomGameJoinResponse response)
	{
		TextObject message = new TextObject("{=4mMySbxI}Unspecified error");
		switch (response)
		{
		case CustomGameJoinResponse.ServerCapacityIsFull:
			message = new TextObject("{=IzJ7f5SQ}Server capacity is full");
			break;
		case CustomGameJoinResponse.ErrorOnGameServer:
			message = new TextObject("{=vkpMgobZ}Game server error");
			break;
		case CustomGameJoinResponse.GameServerAccessError:
			message = new TextObject("{=JQVixeQs}Couldn't access game server");
			break;
		case CustomGameJoinResponse.CustomGameServerNotAvailable:
			message = new TextObject("{=T8IniCKU}Game server is not available");
			break;
		case CustomGameJoinResponse.CustomGameServerFinishing:
			message = new TextObject("{=KRNdlbkq}Custom game is ending");
			break;
		case CustomGameJoinResponse.IncorrectPassword:
			message = new TextObject("{=Mm1Kb1bS}Incorrect password");
			break;
		case CustomGameJoinResponse.PlayerBanned:
			message = new TextObject("{=srAJw3Tg}Player is banned from server");
			break;
		case CustomGameJoinResponse.AlreadyRequestedWaitingForServerResponse:
			message = new TextObject("{=ivKntfNA}Already requested to join, waiting for server response");
			break;
		case CustomGameJoinResponse.NotAllPlayersReady:
			message = new TextObject("{=tlsmbvQX}Not all players are ready to join");
			break;
		case CustomGameJoinResponse.IncorrectPlayerState:
			message = new TextObject("{=KO2adj2I}You need to be in Lobby to join a custom game");
			break;
		case CustomGameJoinResponse.RequesterIsNotPartyLeader:
			message = new TextObject("{=KQrpWV1n}You need be the party leader to join a custom game");
			break;
		case CustomGameJoinResponse.NotAllPlayersModulesMatchWithServer:
			message = new TextObject("{=LCzAvLUB}Not all players' modules match with the server");
			break;
		case CustomGameJoinResponse.SpectatorCapacityIsFull:
			message = new TextObject("{=K4vBc692}All spectator slots for this game are full.");
			break;
		case CustomGameJoinResponse.SpectatorsNotAllowed:
			message = new TextObject("{=Twssd4aa}This server does not allow spectators.");
			break;
		}
		TextObject title = new TextObject("{=mO9bh5sy}Couldn't join custom game");
		_lobbyDataSource.QueryPopup.ShowMessage(title, message);
	}

	void ILobbyStateHandler.OnServerStatusReceived(ServerStatus serverStatus)
	{
		_lobbyDataSource?.OnServerStatusReceived(serverStatus);
		if (serverStatus.Announcement != null)
		{
			if (serverStatus.Announcement.Type == AnnouncementType.Alert)
			{
				InformationManager.AddSystemNotification(new TextObject(serverStatus.Announcement.Text).ToString());
			}
			else if (serverStatus.Announcement.Type == AnnouncementType.Chat)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject(serverStatus.Announcement.Text).ToString()));
			}
		}
	}

	void ILobbyStateHandler.OnRejoinBattleRequestAnswered(bool isSuccessful)
	{
		_lobbyDataSource?.OnRejoinBattleRequestAnswered(isSuccessful);
	}

	void ILobbyStateHandler.OnFriendListUpdated()
	{
		_lobbyDataSource?.OnFriendListUpdated();
	}

	void ILobbyStateHandler.OnPlayerNameUpdated(string playerName)
	{
		_lobbyDataSource?.OnPlayerNameUpdated(playerName);
	}

	private void ShowDisconnectMessage(DisconnectType disconnectType)
	{
		if (disconnectType != DisconnectType.QuitFromGame && disconnectType != DisconnectType.GameEnded)
		{
			TextObject title = new TextObject("{=JluTW3Qw}Game Ended");
			TextObject message = new TextObject("{=aKjpbRP5}Unknown reason");
			switch (disconnectType)
			{
			case DisconnectType.ServerNotResponding:
				message = new TextObject("{=tKSxGy5p}Server not responding");
				break;
			case DisconnectType.KickedByPoll:
				message = new TextObject("{=wbFB3N72}You are kicked from game by poll");
				break;
			case DisconnectType.BannedByPoll:
				message = new TextObject("{=OhF7NqSb}You are banned from game by poll");
				break;
			case DisconnectType.Inactivity:
				message = new TextObject("{=074YAjOk}You are kicked due to inactivity");
				break;
			case DisconnectType.KickedByHost:
				message = new TextObject("{=a0IHtkoa}You are kicked by game host");
				break;
			case DisconnectType.TimedOut:
				message = new TextObject("{=WvGviFgt}Your connection with the server timed out");
				break;
			case DisconnectType.KickedDueToFriendlyDamage:
				message = new TextObject("{=InUAmnX4}You are kicked due to friendly damage");
				break;
			case DisconnectType.PlayStateMismatch:
				message = new TextObject("{=O1bGoaE8}Server state could not be retrieved. Please try again.");
				break;
			}
			_lobbyDataSource.QueryPopup.ShowMessage(title, message);
		}
	}

	private void DisableLobby()
	{
		if (_isLobbyActive)
		{
			_isLobbyActive = false;
			_mplobbyCategory?.Unload();
			_bannerIconsCategory?.Unload();
			RemoveLayer(_lobbyLayer);
			_lobbyDataSource = null;
			_lobbyLayer = null;
		}
	}

	private void OnKeybindRequest(KeyOptionVM requestedHotKeyToChange)
	{
		_currentKey = requestedHotKeyToChange;
		_keybindingPopup.OnToggle(isActive: true);
	}

	private void SetHotKey(Key key)
	{
		if (key.IsControllerInput)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=B41vvGuo}Invalid key"));
			_keybindingPopup.OnToggle(isActive: false);
			return;
		}
		GameKeyOptionVM gameKey;
		AuxiliaryKeyOptionVM auxiliaryKey;
		if ((gameKey = _currentKey as GameKeyOptionVM) != null)
		{
			if (_lobbyDataSource?.Options.GameKeyOptionGroups.GameKeyGroups.FirstOrDefault((GameKeyGroupVM g) => g.GameKeys.Contains(gameKey)) == null)
			{
				TaleWorlds.Library.Debug.FailedAssert("Could not find GameKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\MultiplayerLobbyGauntletScreen.cs", "SetHotKey", 1319);
				MBInformationManager.AddQuickInformation(new TextObject("{=oZrVNUOk}Error"));
				_keybindingPopup.OnToggle(isActive: false);
				return;
			}
			if (key.InputKey != gameKey.CurrentKey.InputKey)
			{
				GauntletLayer lobbyLayer = _lobbyLayer;
				if (lobbyLayer == null || !lobbyLayer.Input.IsHotKeyReleased("Exit"))
				{
					gameKey?.Set(key.InputKey);
					gameKey = null;
					_keybindingPopup.OnToggle(isActive: false);
					goto IL_0222;
				}
			}
			_keybindingPopup.OnToggle(isActive: false);
		}
		else if ((auxiliaryKey = _currentKey as AuxiliaryKeyOptionVM) != null)
		{
			if (_lobbyDataSource.Options.GameKeyOptionGroups.AuxiliaryKeyGroups.FirstOrDefault((AuxiliaryKeyGroupVM g) => g.HotKeys.Contains(auxiliaryKey)) == null)
			{
				TaleWorlds.Library.Debug.FailedAssert("Could not find AuxiliaryKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\MultiplayerLobbyGauntletScreen.cs", "SetHotKey", 1342);
				MBInformationManager.AddQuickInformation(new TextObject("{=oZrVNUOk}Error"));
				_keybindingPopup.OnToggle(isActive: false);
				return;
			}
			if (key.InputKey != auxiliaryKey.CurrentKey.InputKey)
			{
				GauntletLayer lobbyLayer2 = _lobbyLayer;
				if (lobbyLayer2 == null || !lobbyLayer2.Input.IsHotKeyReleased("Exit"))
				{
					auxiliaryKey?.Set(key.InputKey);
					auxiliaryKey = null;
					_keybindingPopup.OnToggle(isActive: false);
					goto IL_0222;
				}
			}
			_keybindingPopup.OnToggle(isActive: false);
		}
		goto IL_0222;
		IL_0222:
		_lobbyDataSource?.Options?.GameKeyOptionGroups?.RefreshValues();
	}

	void IGameStateListener.OnActivate()
	{
		if (_lobbyDataSource == null)
		{
			CreateView();
			_lobbyDataSource.SetPage(MPLobbyVM.LobbyPage.Authentication);
		}
		else
		{
			_optionsSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_options");
			_multiplayerSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_mpmission");
		}
	}

	void IGameStateListener.OnDeactivate()
	{
		if (_lobbyDataSource != null)
		{
			MPCustomGameSortControllerVM sortController = _lobbyDataSource.Matchmaking.CustomServer.SortController;
			_cachedCustomServerSortOption = sortController.CurrentSortOption;
			_cachedCustomServerSortState = (MPCustomGameSortControllerVM.SortState)sortController.CurrentSortState;
			MPCustomGameSortControllerVM sortController2 = _lobbyDataSource.Matchmaking.PremadeMatches.SortController;
			_cachedPremadeGameSortOption = sortController2.CurrentSortOption;
			_cachedPremadeGameSortState = (MPCustomGameSortControllerVM.SortState)sortController2.CurrentSortState;
		}
		if (_lobbyState.GameStateManager.LastOrDefault<LobbyPracticeState>() == null)
		{
			RemoveLayer(_lobbyLayer);
			if (_lobbyDataSource != null)
			{
				_lobbyDataSource.OnFinalize();
				_lobbyDataSource = null;
			}
			_lobbyLayer = null;
			_mplobbyCategory?.Unload();
			_bannerIconsCategory?.Unload();
			_musicSoundEvent.Stop();
			_musicSoundEvent = null;
		}
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
