using SandBox.GauntletUI.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;

namespace SandBox.GauntletUI;

public class SandBoxGauntletUISubModule : MBSubModuleBase
{
	private class SandBoxGameStateManagerListener : IGameStateManagerListener
	{
		void IGameStateManagerListener.OnCleanStates()
		{
			UpdateCampaignMission();
		}

		void IGameStateManagerListener.OnCreateState(GameState gameState)
		{
		}

		void IGameStateManagerListener.OnPopState(GameState gameState)
		{
			UpdateCampaignMission();
			if (gameState is MissionState || gameState is MapState)
			{
				CampaignInformationManager.ClearAllDialogNotifications(fadeOut: false);
			}
		}

		void IGameStateManagerListener.OnPushState(GameState gameState, bool isTopGameState)
		{
			UpdateCampaignMission();
			if (gameState is MissionState || gameState is MapState)
			{
				CampaignInformationManager.ClearAllDialogNotifications(fadeOut: false);
			}
		}

		void IGameStateManagerListener.OnSavedGameLoadFinished()
		{
		}

		private void UpdateCampaignMission()
		{
			CampaignMission.Current?.OnGameStateChanged();
		}
	}

	private bool _gameStarted;

	private bool _initialized;

	private GameStateManager _registeredGameStateManager;

	private bool _initializedConversationHandler;

	private SandBoxGameStateManagerListener _conversationListener;

	public SandBoxGauntletUISubModule()
	{
		_conversationListener = new SandBoxGameStateManagerListener();
	}

	public override void OnCampaignStart(Game game, object starterObject)
	{
		base.OnCampaignStart(game, starterObject);
		if (!_gameStarted && game.GameType is Campaign)
		{
			_gameStarted = true;
		}
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		base.OnGameStart(game, gameStarterObject);
		if (!_gameStarted && game.GameType is Campaign)
		{
			_gameStarted = true;
			SandBoxGauntletGameNotification.Initialize();
		}
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		if (_gameStarted && game.GameType is Campaign)
		{
			_gameStarted = false;
			GauntletGameNotification.Initialize();
		}
	}

	public override void BeginGameStart(Game game)
	{
		base.BeginGameStart(game);
		if (Campaign.Current != null)
		{
			Campaign.Current.VisualCreator.MapEventVisualCreator = new GauntletMapEventVisualCreator();
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		if (!_initializedConversationHandler && Game.Current?.GameStateManager != null)
		{
			Game.Current.GameStateManager.RegisterListener(_conversationListener);
			_registeredGameStateManager = Game.Current.GameStateManager;
			_initializedConversationHandler = true;
		}
		else if (_initializedConversationHandler && Game.Current?.GameStateManager == null)
		{
			_registeredGameStateManager.UnregisterListener(_conversationListener);
			_initializedConversationHandler = false;
			_registeredGameStateManager = null;
		}
		if (!_initialized && GauntletSceneNotification.Current != null)
		{
			if (!Utilities.CommandLineArgumentExists("VisualTests"))
			{
				GauntletSceneNotification.Current.RegisterContextProvider(new SandboxSceneNotificationContextProvider());
			}
			_initialized = true;
		}
	}
}
