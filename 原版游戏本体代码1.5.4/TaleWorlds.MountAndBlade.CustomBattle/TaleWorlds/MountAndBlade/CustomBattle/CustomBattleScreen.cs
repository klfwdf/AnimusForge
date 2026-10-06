using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.CustomBattle;

[GameStateScreen(typeof(CustomBattleState))]
public class CustomBattleScreen : ScreenBase, IGameStateListener
{
	private CustomBattleState _customBattleState;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _gauntletMovie;

	private CustomBattleVM _dataSource;

	private bool _isMovieLoaded;

	private int _isFirstFrameCounter;

	public CustomBattleScreen(CustomBattleState customBattleState)
	{
		_customBattleState = customBattleState;
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		_dataSource.OnFinalize();
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new CustomBattleVM(_customBattleState);
		_dataSource.SetStartInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.SetRandomizeInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Randomize"));
		_dataSource.TroopTypeSelectionPopUp?.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_gauntletLayer = new GauntletLayer("CustomBattle", 1, shouldClear: true);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		LoadMovie();
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_dataSource.SetActiveState(isActive: true);
		AddLayer(_gauntletLayer);
		InformationManager.HideAllMessages();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_isFirstFrameCounter >= 0)
		{
			if (_isFirstFrameCounter == 0)
			{
				LoadingWindow.DisableGlobalLoadingWindow();
			}
			_isFirstFrameCounter--;
		}
		if (_gauntletLayer.IsFocusedOnInput())
		{
			return;
		}
		TroopTypeSelectionPopUpVM troopTypeSelectionPopUp = _dataSource.TroopTypeSelectionPopUp;
		if (troopTypeSelectionPopUp != null && troopTypeSelectionPopUp.IsOpen)
		{
			if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.TroopTypeSelectionPopUp.ExecuteCancel();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.TroopTypeSelectionPopUp.ExecuteDone();
			}
			else if (_gauntletLayer.Input.IsHotKeyReleased("Reset"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.TroopTypeSelectionPopUp.ExecuteReset();
			}
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteBack();
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Randomize"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteRandomize();
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteStart();
		}
	}

	protected override void OnFinalize()
	{
		UnloadMovie();
		RemoveLayer(_gauntletLayer);
		_dataSource = null;
		_gauntletLayer = null;
		base.OnFinalize();
	}

	protected override void OnActivate()
	{
		LoadMovie();
		_dataSource?.SetActiveState(isActive: true);
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		_isFirstFrameCounter = 2;
		base.OnActivate();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		UnloadMovie();
		_dataSource?.SetActiveState(isActive: false);
	}

	public override void UpdateLayout()
	{
		base.UpdateLayout();
		if (!_isMovieLoaded)
		{
			_dataSource?.RefreshValues();
		}
	}

	private void LoadMovie()
	{
		if (!_isMovieLoaded)
		{
			_gauntletMovie = _gauntletLayer.LoadMovie("CustomBattleScreen", _dataSource);
			_isMovieLoaded = true;
		}
	}

	private void UnloadMovie()
	{
		if (_isMovieLoaded)
		{
			_gauntletLayer.ReleaseMovie(_gauntletMovie);
			_gauntletMovie = null;
			_isMovieLoaded = false;
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
	}
}
