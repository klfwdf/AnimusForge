using SandBox.View;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(CharacterDeveloperState))]
public class GauntletCharacterDeveloperScreen : ScreenBase, IGameStateListener, IChangeableScreen, ICharacterDeveloperStateHandler
{
	private CharacterDeveloperVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _characterdeveloper;

	private readonly CharacterDeveloperState _characterDeveloperState;

	public GauntletCharacterDeveloperScreen(CharacterDeveloperState clanState)
	{
		_characterDeveloperState = clanState;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		InformationManager.HideAllMessages();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || _gauntletLayer.Input.IsGameKeyPressed(37))
		{
			if (_dataSource.CurrentCharacter.IsInspectingAnAttribute)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.CurrentCharacter.ExecuteStopInspectingCurrentAttribute();
			}
			else if (_dataSource.CurrentCharacter.PerkSelection.IsActive)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.CurrentCharacter.PerkSelection.ExecuteDeactivate();
			}
			else
			{
				CloseCharacterDeveloperScreen();
			}
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			ExecuteConfirm();
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Reset"))
		{
			ExecuteReset();
		}
		else if (_gauntletLayer.Input.IsHotKeyPressed("SwitchToPreviousTab"))
		{
			ExecuteSwitchToPreviousTab();
		}
		else if (_gauntletLayer.Input.IsHotKeyPressed("SwitchToNextTab"))
		{
			ExecuteSwitchToNextTab();
		}
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_characterdeveloper = UIResourceManager.LoadSpriteCategory("ui_characterdeveloper");
		_dataSource = new CharacterDeveloperVM(CloseCharacterDeveloperScreen);
		_dataSource.SetGetKeyTextFromKeyIDFunc(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID);
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.SetPreviousCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToPreviousTab"));
		_dataSource.SetNextCharacterInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("SwitchToNextTab"));
		if (_characterDeveloperState.InitialSelectedHero != null)
		{
			_dataSource.SelectHero(_characterDeveloperState.InitialSelectedHero);
		}
		_gauntletLayer = new GauntletLayer("CharacterDeveloper", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.LoadMovie("CharacterDeveloper", _dataSource);
		AddLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.CharacterScreen));
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_character_open");
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		RemoveLayer(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		_characterdeveloper.Unload();
	}

	private void CloseCharacterDeveloperScreen()
	{
		UISoundsHelper.PlayUISound("event:/ui/default");
		Game.Current.GameStateManager.PopState();
	}

	private void ExecuteConfirm()
	{
		UISoundsHelper.PlayUISound("event:/ui/default");
		_dataSource.ExecuteDone();
	}

	private void ExecuteReset()
	{
		UISoundsHelper.PlayUISound("event:/ui/default");
		_dataSource.ExecuteReset();
	}

	private void ExecuteSwitchToPreviousTab()
	{
		MBBindingList<SelectorItemVM> itemList = _dataSource.CharacterList.ItemList;
		if (itemList != null && itemList.Count > 1)
		{
			UISoundsHelper.PlayUISound("event:/ui/checkbox");
		}
		_dataSource.CharacterList.ExecuteSelectPreviousItem();
	}

	private void ExecuteSwitchToNextTab()
	{
		MBBindingList<SelectorItemVM> itemList = _dataSource.CharacterList.ItemList;
		if (itemList != null && itemList.Count > 1)
		{
			UISoundsHelper.PlayUISound("event:/ui/checkbox");
		}
		_dataSource.CharacterList.ExecuteSelectNextItem();
	}

	bool IChangeableScreen.AnyUnsavedChanges()
	{
		return _dataSource.IsThereAnyChanges();
	}

	bool IChangeableScreen.CanChangesBeApplied()
	{
		return true;
	}

	void IChangeableScreen.ApplyChanges()
	{
		_dataSource.ApplyAllChanges();
	}

	void IChangeableScreen.ResetChanges()
	{
		_dataSource.ExecuteReset();
	}
}
