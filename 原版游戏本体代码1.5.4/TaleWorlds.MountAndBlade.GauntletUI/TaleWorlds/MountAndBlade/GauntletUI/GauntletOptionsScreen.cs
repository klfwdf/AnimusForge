using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.AuxiliaryKeys;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

[OverrideView(typeof(OptionsScreen))]
public class GauntletOptionsScreen : ScreenBase
{
	private GauntletLayer _gauntletLayer;

	private OptionsVM _dataSource;

	private GauntletMovieIdentifier _gauntletMovie;

	private KeybindingPopup _keybindingPopup;

	private KeyOptionVM _currentKey;

	private SpriteCategory _optionsSpriteCategory;

	private bool _isFromMainMenu;

	public GauntletOptionsScreen(bool isFromMainMenu)
	{
		_isFromMainMenu = isFromMainMenu;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_optionsSpriteCategory = UIResourceManager.LoadSpriteCategory("ui_options");
		OptionsVM.OptionsMode optionsMode = ((!_isFromMainMenu) ? OptionsVM.OptionsMode.Singleplayer : OptionsVM.OptionsMode.MainMenu);
		_dataSource = new OptionsVM(autoHandleClose: true, optionsMode, OnKeybindRequest);
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetPreviousTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_dataSource.SetNextTabInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.ExposurePopUp.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.ExposurePopUp.SetConfirmInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.BrightnessPopUp.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.BrightnessPopUp.SetConfirmInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_gauntletLayer = new GauntletLayer("OptionsScreen", 4000);
		_gauntletMovie = _gauntletLayer.LoadMovie("Options", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		_keybindingPopup = new KeybindingPopup(SetHotKey, this);
		AddLayer(_gauntletLayer);
		ScreenManager.TrySetFocus(_gauntletLayer);
		if (BannerlordConfig.ForceVSyncInMenus)
		{
			Utilities.SetForceVsync(value: true);
		}
		Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.OptionsScreen));
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_dataSource.OnFinalize();
		_optionsSpriteCategory.Unload();
		Utilities.SetForceVsync(value: false);
		Game.Current?.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
	}

	protected override void OnDeactivate()
	{
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
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
		_keybindingPopup.Tick();
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
				Debug.FailedAssert("Could not find GameKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletOptionsScreen.cs", "SetHotKey", 169);
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
				Debug.FailedAssert("Could not find AuxiliaryKeyGroup during SetHotKey", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletOptionsScreen.cs", "SetHotKey", 192);
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
