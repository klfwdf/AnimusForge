using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.AuxiliaryKeys;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionOptionsUIHandler))]
public class MissionGauntletOptionsUIHandler : MissionView
{
	private GauntletLayer _gauntletLayer;

	private OptionsVM _dataSource;

	private GauntletMovieIdentifier _movie;

	private KeybindingPopup _keybindingPopup;

	private KeyOptionVM _currentKey;

	private SpriteCategory _optionsSpriteCategory;

	private bool _initialClothSimValue;

	public bool IsEnabled { get; private set; }

	public MissionGauntletOptionsUIHandler()
	{
		ViewOrderPriority = 49;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.Mission.GetMissionBehavior<MissionOptionsComponent>().OnOptionsAdded += OnShowOptions;
		_keybindingPopup = new KeybindingPopup(SetHotKey, base.MissionScreen);
	}

	public override void OnMissionScreenFinalize()
	{
		base.Mission.GetMissionBehavior<MissionOptionsComponent>().OnOptionsAdded -= OnShowOptions;
		base.OnMissionScreenFinalize();
		_dataSource?.OnFinalize();
		_dataSource = null;
		_movie = null;
		_keybindingPopup?.OnToggle(isActive: false);
		_keybindingPopup = null;
		_gauntletLayer = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_gauntletLayer != null && !_keybindingPopup.IsActive)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				if (_dataSource.ExposurePopUp.Visible)
				{
					_dataSource.ExposurePopUp.ExecuteCancel();
				}
				else if (_dataSource.BrightnessPopUp.Visible)
				{
					_dataSource.BrightnessPopUp.ExecuteCancel();
				}
				else
				{
					_dataSource.ExecuteCancel();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				if (_dataSource.ExposurePopUp.Visible)
				{
					_dataSource.ExposurePopUp.ExecuteConfirm();
				}
				else if (_dataSource.BrightnessPopUp.Visible)
				{
					_dataSource.BrightnessPopUp.ExecuteConfirm();
				}
				else
				{
					_dataSource.ExecuteDone();
				}
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("SwitchToPreviousTab"))
			{
				UISoundsHelper.PlayUISound("event:/ui/tab");
				_dataSource.SelectPreviousCategory();
			}
			else if (_gauntletLayer.Input.IsHotKeyPressed("SwitchToNextTab"))
			{
				UISoundsHelper.PlayUISound("event:/ui/tab");
				_dataSource.SelectNextCategory();
			}
		}
		_keybindingPopup?.Tick();
	}

	public override bool OnEscape()
	{
		if (_dataSource != null)
		{
			_dataSource.ExecuteCloseOptions();
			return true;
		}
		return base.OnEscape();
	}

	private void OnShowOptions()
	{
		IsEnabled = true;
		OnEscapeMenuToggled(isOpened: true);
		_initialClothSimValue = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ClothSimulation) == 0f;
		InformationManager.HideAllMessages();
	}

	private void OnCloseOptions()
	{
		IsEnabled = false;
		OnEscapeMenuToggled(isOpened: false);
		bool flag = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ClothSimulation) == 0f;
		if (_initialClothSimValue != flag)
		{
			InformationManager.ShowInquiry(new InquiryData(Module.CurrentModule.GlobalTextManager.FindText("str_option_wont_take_effect_mission_title").ToString(), Module.CurrentModule.GlobalTextManager.FindText("str_option_wont_take_effect_mission_desc").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Module.CurrentModule.GlobalTextManager.FindText("str_ok").ToString(), string.Empty, null, null), pauseGameActiveState: true);
		}
	}

	public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return _gauntletLayer == null;
	}

	private void OnEscapeMenuToggled(bool isOpened)
	{
		if (isOpened)
		{
			if (!GameNetwork.IsMultiplayer)
			{
				MBCommon.PauseGameEngine();
			}
		}
		else
		{
			MBCommon.UnPauseGameEngine();
		}
		if (isOpened)
		{
			OptionsVM.OptionsMode optionsMode = ((!GameNetwork.IsMultiplayer) ? OptionsVM.OptionsMode.Singleplayer : OptionsVM.OptionsMode.Multiplayer);
			_dataSource = new OptionsVM(optionsMode, OnCloseOptions, OnKeybindRequest);
			_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			_dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
			_dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
			_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			_gauntletLayer = new GauntletLayer("MissionOptions", ++ViewOrderPriority);
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_optionsSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_options");
			_movie = _gauntletLayer.LoadMovie("Options", _dataSource);
			base.MissionScreen.AddLayer(_gauntletLayer);
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
			Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.OptionsScreen));
		}
		else
		{
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_keybindingPopup?.OnToggle(isActive: false);
			_optionsSpriteCategory.Unload();
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
			_gauntletLayer = null;
			Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.Mission));
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
			if (_dataSource.GameKeyOptionGroups.GameKeyGroups.FirstOrDefault((GameKeyGroupVM g) => g.GameKeys.Contains(gameKey)) == null)
			{
				Debug.FailedAssert("Could not find GameKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\MissionGauntletOptionsUIHandler.cs", "SetHotKey", 246);
				MBInformationManager.AddQuickInformation(new TextObject("{=oZrVNUOk}Error"));
				_keybindingPopup.OnToggle(isActive: false);
				return;
			}
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || key.InputKey == gameKey.CurrentKey.InputKey)
			{
				_keybindingPopup.OnToggle(isActive: false);
			}
			else
			{
				gameKey?.Set(key.InputKey);
				gameKey = null;
				_keybindingPopup.OnToggle(isActive: false);
			}
		}
		else if ((auxiliaryKey = _currentKey as AuxiliaryKeyOptionVM) != null)
		{
			if (_dataSource.GameKeyOptionGroups.AuxiliaryKeyGroups.FirstOrDefault((AuxiliaryKeyGroupVM g) => g.HotKeys.Contains(auxiliaryKey)) == null)
			{
				Debug.FailedAssert("Could not find AuxiliaryKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\MissionGauntletOptionsUIHandler.cs", "SetHotKey", 269);
				MBInformationManager.AddQuickInformation(new TextObject("{=oZrVNUOk}Error"));
				_keybindingPopup.OnToggle(isActive: false);
				return;
			}
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || key.InputKey == auxiliaryKey.CurrentKey.InputKey)
			{
				_keybindingPopup.OnToggle(isActive: false);
			}
			else
			{
				auxiliaryKey?.Set(key.InputKey);
				auxiliaryKey = null;
				_keybindingPopup.OnToggle(isActive: false);
			}
		}
		_dataSource?.GameKeyOptionGroups?.RefreshValues();
	}
}
