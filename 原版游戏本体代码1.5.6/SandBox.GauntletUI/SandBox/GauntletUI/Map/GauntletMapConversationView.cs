using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Barter;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapConversationView))]
public class GauntletMapConversationView : MapConversationView, IConversationStateHandler
{
	private GauntletMovieIdentifier _conversationMovie;

	private GauntletLayer _layerAsGauntletLayer;

	private MapConversationVM _dataSource;

	private SpriteCategory _conversationCategory;

	private MapConversationTableauData _tableauData;

	private BarterManager _barterManager;

	private GauntletMapConversationBarterView _barterView;

	private ConversationCharacterData _playerCharacterData;

	private ConversationCharacterData _conversationPartnerData;

	private bool _isSwitchingConversations;

	private int _minimumAvailableConversationInstallFrame;

	public GauntletMapConversationView()
	{
		_barterManager = Campaign.Current.BarterManager;
		_conversationCategory = UIResourceManager.GetSpriteCategory("ui_conversation");
	}

	private void OnBarterActiveStateChanged(bool isBarterActive)
	{
		_dataSource.IsBarterActive = isBarterActive;
	}

	protected override void InitializeConversation(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData)
	{
		base.InitializeConversation(playerCharacterData, conversationPartnerData);
		_playerCharacterData = playerCharacterData;
		_conversationPartnerData = conversationPartnerData;
		DestroyConversationTableau();
		DestroyConversationMission();
		CreateConversationMissionIfMissing();
		if (!base.IsConversationActive)
		{
			CreateConversationView();
			CreateConversationTableau();
		}
		else
		{
			_minimumAvailableConversationInstallFrame = Utilities.EngineFrameNo + 2;
			_isSwitchingConversations = true;
		}
		base.IsConversationActive = true;
	}

	protected override void FinalizeConversation()
	{
		base.FinalizeConversation();
		DestroyConversationTableau();
		DestroyConversationView();
		DestroyConversationMission();
		_minimumAvailableConversationInstallFrame = Utilities.EngineFrameNo + 2;
		base.IsConversationActive = false;
		if (!base.MapScreen.IsReady)
		{
			LoadingWindow.EnableGlobalLoadingWindow();
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
		if (base.IsConversationActive)
		{
			_conversationMovie = _layerAsGauntletLayer.LoadMovie("MapConversation", _dataSource);
			if (_barterView.IsCreated && !_barterView.IsActive)
			{
				_barterView.Activate();
			}
			_conversationCategory.Load();
			_dataSource.TableauData = _tableauData;
		}
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
		if (base.IsConversationActive)
		{
			_dataSource.TableauData = null;
			_layerAsGauntletLayer.ReleaseMovie(_conversationMovie);
			if (_barterView.IsCreated && _barterView.IsActive)
			{
				_barterView.Deactivate();
			}
			_conversationCategory.Unload();
		}
	}

	private void Tick(float dt)
	{
		if (!base.IsConversationActive || _layerAsGauntletLayer == null)
		{
			return;
		}
		if (_isSwitchingConversations)
		{
			_isSwitchingConversations = false;
		}
		if (base.IsConversationActive && ScreenManager.TopScreen == base.MapScreen && ScreenManager.FocusedLayer != base.Layer)
		{
			ScreenManager.TrySetFocus(base.Layer);
		}
		_dataSource?.Tick(dt);
		MapConversationVM dataSource = _dataSource;
		if (dataSource != null && dataSource.DialogController?.AnswerList.Count <= 0 && !_barterView.IsCreated && base.IsConversationActive && _layerAsGauntletLayer.Input.IsHotKeyReleased("ContinueKey"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			((IConversationStateHandler)this).ExecuteConversationContinue();
		}
		if (!base.IsConversationActive || _layerAsGauntletLayer == null)
		{
			return;
		}
		if (_barterView.IsCreated)
		{
			_barterView.TickInput();
		}
		else
		{
			if (base.IsConversationActive && _tableauData == null && Utilities.EngineFrameNo > _minimumAvailableConversationInstallFrame)
			{
				CreateConversationTableau();
			}
			if (!ScreenFadeController.IsFadeActive && _layerAsGauntletLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
			{
				MapScreen mapScreen = base.MapScreen;
				if (mapScreen != null && mapScreen.IsEscapeMenuOpened)
				{
					base.MapScreen.CloseEscapeMenu();
				}
				else
				{
					base.MapScreen?.OpenEscapeMenu();
				}
			}
		}
		BarterItemVM.IsFiveStackModifierActive = _layerAsGauntletLayer.Input.IsHotKeyDown("FiveStackModifier");
		BarterItemVM.IsEntireStackModifierActive = _layerAsGauntletLayer.Input.IsHotKeyDown("EntireStackModifier");
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (base.IsConversationActive)
		{
			FinalizeConversation();
		}
	}

	private void CreateConversationView()
	{
		base.Layer = new GauntletLayer("MapConversation", 205);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		_barterView = new GauntletMapConversationBarterView(_layerAsGauntletLayer, OnBarterActiveStateChanged);
		BarterManager barterManager = _barterManager;
		barterManager.BarterBegin = (BarterManager.BarterBeginEventDelegate)Delegate.Combine(barterManager.BarterBegin, new BarterManager.BarterBeginEventDelegate(_barterView.CreateBarterView));
		BarterManager barterManager2 = _barterManager;
		barterManager2.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Combine(barterManager2.Closed, new BarterManager.BarterCloseEventDelegate(_barterView.DestroyBarterView));
		_dataSource = new MapConversationVM(OnContinue, GetContinueKeyText);
		_conversationMovie = _layerAsGauntletLayer.LoadMovie("MapConversation", _dataSource);
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ConversationHotKeyCategory"));
		base.MapScreen.AddLayer(base.Layer);
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		_conversationCategory.Load();
		Campaign.Current.ConversationManager.Handler = this;
		Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
	}

	private void OnContinue()
	{
		if (base.IsConversationActive)
		{
			MapConversationVM dataSource = _dataSource;
			if (dataSource != null && dataSource.DialogController?.AnswerList.Count <= 0 && !_barterView.IsCreated)
			{
				((IConversationStateHandler)this).ExecuteConversationContinue();
			}
		}
	}

	private void DestroyConversationView()
	{
		base.Layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(base.Layer);
		if (_barterView.IsCreated)
		{
			_barterView.DestroyBarterView();
		}
		_dataSource.OnFinalize();
		base.MapScreen.RemoveLayer(base.Layer);
		SpriteCategory conversationCategory = _conversationCategory;
		if (conversationCategory != null && conversationCategory.IsLoaded)
		{
			_conversationCategory.Unload();
		}
		BarterManager barterManager = _barterManager;
		barterManager.BarterBegin = (BarterManager.BarterBeginEventDelegate)Delegate.Remove(barterManager.BarterBegin, new BarterManager.BarterBeginEventDelegate(_barterView.CreateBarterView));
		BarterManager barterManager2 = _barterManager;
		barterManager2.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Remove(barterManager2.Closed, new BarterManager.BarterCloseEventDelegate(_barterView.DestroyBarterView));
		base.Layer = null;
		_layerAsGauntletLayer = null;
		_dataSource = null;
		Campaign.Current.ConversationManager.Handler = null;
		Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
	}

	protected override bool IsEscaped()
	{
		return base.IsConversationActive;
	}

	protected override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return true;
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		Tick(dt);
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		Tick(dt);
	}

	protected override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		Tick(dt);
	}

	private void CreateConversationTableau()
	{
		float timeOfDay = CampaignTime.Now.CurrentHourInDay * (float)(24 / CampaignTime.HoursInDay);
		MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(MobileParty.MainParty.Position.ToVec2());
		bool isCurrentTerrainUnderSnow = weatherEventInPosition == MapWeatherModel.WeatherEvent.Snowy || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard;
		string locationId = null;
		if (_conversationPartnerData.Character.HeroObject != null)
		{
			locationId = LocationComplex.Current?.GetLocationOfCharacter(_conversationPartnerData.Character.HeroObject)?.StringId;
		}
		_tableauData = MapConversationTableauData.CreateFrom(_playerCharacterData, _conversationPartnerData, Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace), timeOfDay, isCurrentTerrainUnderSnow, Hero.MainHero.CurrentSettlement, locationId, weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain, weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard);
		_dataSource.TableauData = _tableauData;
		_layerAsGauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(1, null);
	}

	private void DestroyConversationTableau()
	{
		if (_dataSource != null)
		{
			_dataSource.TableauData = null;
		}
		_tableauData = null;
	}

	void IConversationStateHandler.OnConversationUninstall()
	{
		if (!_isSwitchingConversations)
		{
			Game.Current.GameStateManager.LastOrDefault<MapState>()?.OnMapConversationOver();
		}
	}

	private static string GetContinueKeyText()
	{
		if (Input.IsGamepadActive)
		{
			return GameTexts.FindText("str_click_to_continue_console").SetTextVariable("CONSOLE_KEY_NAME", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ConversationHotKeyCategory", "ContinueClick"))).ToString();
		}
		return GameTexts.FindText("str_click_to_continue").ToString();
	}

	void IConversationStateHandler.OnConversationInstall()
	{
	}

	void IConversationStateHandler.OnConversationActivate()
	{
	}

	void IConversationStateHandler.OnConversationDeactivate()
	{
		MBInformationManager.HideInformations();
	}

	void IConversationStateHandler.OnConversationContinue()
	{
		_dataSource.DialogController.OnConversationContinue();
	}

	void IConversationStateHandler.ExecuteConversationContinue()
	{
		_dataSource.DialogController.ExecuteContinue();
	}
}
