using SandBox.ViewModelCollection.GameOver;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(GameOverState))]
public class GauntletGameOverScreen : ScreenBase, IGameOverStateHandler, IGameStateListener
{
	private SpriteCategory _gameOverCategory;

	private GameOverVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private readonly GameOverState _gameOverState;

	public GauntletGameOverScreen(GameOverState gameOverState)
	{
		_gameOverState = gameOverState;
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			CloseGameOverScreen();
		}
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_gameOverCategory = UIResourceManager.LoadSpriteCategory("ui_gameover");
		_gauntletLayer = new GauntletLayer("GameOverScreen", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		AddLayer(_gauntletLayer);
		_dataSource = new GameOverVM(_gameOverState.Reason, CloseGameOverScreen);
		_dataSource.SetCloseInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_gauntletLayer.LoadMovie("GameOverScreen", _dataSource);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.GameOverScreen));
		switch (_gameOverState.Reason)
		{
		case GameOverState.GameOverReason.ClanDestroyed:
			UISoundsHelper.PlayUISound("event:/ui/endgame/end_clan_destroyed");
			break;
		case GameOverState.GameOverReason.Retirement:
			UISoundsHelper.PlayUISound("event:/ui/endgame/end_retirement");
			break;
		case GameOverState.GameOverReason.Victory:
			UISoundsHelper.PlayUISound("event:/ui/endgame/end_victory");
			break;
		}
		LoadingWindow.DisableGlobalLoadingWindow();
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		RemoveLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		_gameOverCategory.Unload();
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
	}

	private void CloseGameOverScreen()
	{
		bool flag = false;
		if (flag || Game.Current.IsDevelopmentMode || _gameOverState.Reason == GameOverState.GameOverReason.Victory)
		{
			Game.Current.GameStateManager.PopState();
			if ((flag || Game.Current.IsDevelopmentMode) && _gameOverState.Reason == GameOverState.GameOverReason.Retirement)
			{
				PlayerEncounter.Finish();
			}
		}
		else
		{
			MBGameManager.EndGame();
		}
	}
}
